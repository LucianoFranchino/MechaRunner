using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arma el nivel encadenando LevelSections diseñadas a mano (reemplaza a los EntitySpawner al azar).
/// - Por distancia recorrida: cada sección empieza donde terminó la anterior más un hueco.
/// - Ritmo: secuencia de intensidades (ej. normal, normal, respiro); al cambiar de zona, primero un respiro.
/// - Zonas: solo usa secciones habilitadas para la zona actual; las que presentan un peligro nuevo
///   salen primero, una vez.
/// - No repite ninguna de las últimas secciones.
/// - Escala las distancias con la velocidad (las secciones se diseñan a LevelSection.ReferenceSpeed).
/// </summary>
public class LevelDirector : MonoBehaviour
{
    [SerializeField] private LevelSection[] sections;
    [Tooltip("Donde empieza cada sección (fuera de pantalla, a la derecha).")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("Collider del piso: su borde superior es y = 0 de las secciones.")]
    [SerializeField] private Collider2D ground;

    [Header("Ritmo")]
    [SerializeField] private SectionIntensity[] earlyRhythm = { SectionIntensity.Normal, SectionIntensity.Normal, SectionIntensity.Breather };
    [SerializeField] private SectionIntensity[] lateRhythm = { SectionIntensity.Normal, SectionIntensity.Normal, SectionIntensity.Peak, SectionIntensity.Breather };
    [Tooltip("Zona desde la que se usa el ritmo con picos.")]
    [SerializeField] private int lateRhythmFromZone = 3;
    [Tooltip("Segundos libres entre secciones (a la velocidad actual), antes de la densidad de la zona.")]
    [SerializeField] private float gapSeconds = 0.9f;
    [Tooltip("Piso de justicia: nunca menos que esto entre secciones.")]
    [SerializeField] private float minGapSeconds = 0.6f;
    [SerializeField] private float firstSectionDelay = 1.5f;
    [SerializeField, Min(0)] private int noRepeatCount = 3;

    // Objetos de las secciones ya elegidas que todavía no llegaron al punto de spawn (en orden)
    private struct Pending
    {
        public GameObject prefab;
        public float triggerDistance;   // distancia recorrida a la que su borde izquierdo llega al spawn
        public float leadWorld;          // pivot -> borde izquierdo, en el mundo
        public float y;
        public Vector3 scale;
    }
    private readonly List<Pending> pending = new(96);
    // Comparador como struct cacheado: List.Sort(Comparison) reserva un envoltorio en cada llamada
    private struct ByTrigger : IComparer<Pending>
    {
        public int Compare(Pending a, Pending b) => a.triggerDistance.CompareTo(b.triggerDistance);
    }
    private static readonly IComparer<Pending> byTrigger = new ByTrigger();

    private float nextStartDistance;
    private int rhythmStep;
    private bool breatherNext;
    private readonly List<LevelSection> recent = new();
    private readonly HashSet<LevelSection> introsShown = new();
    private readonly List<LevelSection> candidates = new();
    private DifficultyManager difficulty;

    public LevelSection CurrentSection { get; private set; }
    public SectionIntensity NextIntensity { get; private set; }

    private float GroundTop => ground != null ? ground.bounds.max.y : -0.704f;

    private void Start()
    {
        difficulty = DifficultyManager.Instance;
        if (difficulty == null) { enabled = false; return; }
        difficulty.ZoneChanged += OnZoneChanged;
        nextStartDistance = difficulty.Distance + difficulty.GameSpeed * firstSectionDelay;
    }

    private void OnDestroy()
    {
        if (difficulty != null) difficulty.ZoneChanged -= OnZoneChanged;
    }

    private void OnZoneChanged(int zone)
    {
        breatherNext = true;
        rhythmStep = 0;
    }

    private void Update()
    {
        SpawnDue();
        if (difficulty.Distance < nextStartDistance) return;
        var section = Choose(difficulty.ZoneIndex + 1);
        if (section == null)
        {
            nextStartDistance = difficulty.Distance + difficulty.GameSpeed * gapSeconds;
            return;
        }
        Place(section);
    }

    private void Place(LevelSection section)
    {
        float speed = difficulty.GameSpeed;
        float scale = speed / LevelSection.ReferenceSpeed;
        float sectionStart = nextStartDistance;
        float groundTop = GroundTop;

        var elements = section.Elements;
        for (int i = 0; i < elements.Count; i++)
        {
            var e = elements[i];
            var prefab = e.prefab;
            // Lugar para power-up: se elige uno y se usa su tamaño propio (no el del marcador)
            bool fromSlot = prefab != null && prefab.TryGetComponent(out PowerUpSlot slot) && (prefab = slot.Pick()) != null;
            if (prefab == null) continue;

            var s = fromSlot || e.localScale == Vector3.zero ? prefab.transform.localScale : e.localScale;
            if (e.stretchWithSpeed) s.x *= scale;
            float lead = e.leadingEdge * (e.stretchWithSpeed ? scale : 1f);
            // Se spawnea recién cuando su borde izquierdo llega al punto de spawn (fuera de pantalla):
            // nada aparece más allá del piso ni se ve aparecer de golpe
            pending.Add(new Pending
            {
                prefab = prefab,
                triggerDistance = sectionStart + e.localPosition.x * scale - lead,
                leadWorld = lead,
                y = groundTop + e.localPosition.y,
                scale = s,
            });
        }
        pending.Sort(byTrigger);
        SpawnDue();

        CurrentSection = section;
        RememberRecent(section);
        float density = Mathf.Max(0.1f, difficulty.Density);
        float gap = Mathf.Max(gapSeconds / density, minGapSeconds) * speed;
        nextStartDistance += section.Length * scale + gap;
    }

    private void SpawnDue()
    {
        float distance = difficulty.Distance;
        float spawnX = spawnPoint.position.x;
        while (pending.Count > 0 && pending[0].triggerDistance <= distance)
        {
            var p = pending[0];
            pending.RemoveAt(0);
            // Si el frame se pasó del punto exacto, correrlo lo recorrido de más
            float x = spawnX + p.leadWorld - (distance - p.triggerDistance);
            var go = PoolManager.Instance.Spawn(p.prefab, new Vector3(x, p.y, 0f));
            if (go != null) go.transform.localScale = p.scale;
        }
    }

    /// <summary>Elige la próxima sección según ritmo, zona, presentaciones y repeticiones. Público para tests.</summary>
    public LevelSection Choose(int zone)
    {
        var rhythm = zone >= lateRhythmFromZone ? lateRhythm : earlyRhythm;
        SectionIntensity wanted;
        if (breatherNext) { wanted = SectionIntensity.Breather; breatherNext = false; }
        else wanted = rhythm.Length == 0 ? SectionIntensity.Normal : rhythm[rhythmStep++ % rhythm.Length];
        NextIntensity = wanted;

        // Un peligro nuevo se presenta solo, antes que el resto (nunca en lugar de un respiro)
        if (wanted != SectionIntensity.Breather)
        {
            foreach (var s in sections)
                if (s != null && s.IntroducesThreat && s.AllowedInZone(zone) && !introsShown.Contains(s))
                {
                    introsShown.Add(s);
                    return s;
                }
        }

        return Pick(zone, wanted, avoidRecent: true)
            ?? Pick(zone, wanted, avoidRecent: false)
            ?? PickAnyIntensity(zone);
    }

    private LevelSection Pick(int zone, SectionIntensity intensity, bool avoidRecent)
    {
        candidates.Clear();
        int total = 0;
        foreach (var s in sections)
        {
            if (s == null || s.Intensity != intensity || !s.AllowedInZone(zone)) continue;
            if (avoidRecent && recent.Contains(s)) continue;
            candidates.Add(s);
            total += s.Weight;
        }
        return Weighted(total);
    }

    private LevelSection PickAnyIntensity(int zone)
    {
        candidates.Clear();
        int total = 0;
        foreach (var s in sections)
            if (s != null && s.AllowedInZone(zone)) { candidates.Add(s); total += s.Weight; }
        return Weighted(total);
    }

    private LevelSection Weighted(int total)
    {
        if (candidates.Count == 0) return null;
        int roll = Random.Range(0, total);
        foreach (var c in candidates)
        {
            roll -= c.Weight;
            if (roll < 0) return c;
        }
        return candidates[candidates.Count - 1];
    }

    private void RememberRecent(LevelSection section)
    {
        if (noRepeatCount <= 0) return;
        recent.Add(section);
        if (recent.Count > noRepeatCount) recent.RemoveAt(0);
    }
}
