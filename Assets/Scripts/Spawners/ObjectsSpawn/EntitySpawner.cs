using System.Collections.Generic;
using UnityEngine;
using MechaRunner.Spawning;

[System.Serializable]
public class SpawnPoint
{
    [Tooltip("Solo para identificarlo en el Inspector, ej: 'Suelo', 'Plataforma', 'Aire'.")]
    public string label;
    public Transform point;

    [Tooltip("Activado: la Y del punto es la superficie del piso y el objeto se apoya por su base " +
             "(borde inferior de su collider sólido). Desactivado: se coloca su pivot en el punto.")]
    public bool alignBottomToPoint;
}

/// <summary>
/// Spawner genérico configurable: se instancia varias veces en escena
/// (una para obstáculos+enemigos de suelo, otra para enemigos voladores, etc.)
/// con distintas entradas y spawn points, todas sobre el mismo PoolManager.
/// </summary>
public class EntitySpawner : MonoBehaviour
{
    [Header("Puntos de spawn (por altura)")]
    [SerializeField] private SpawnPoint[] spawnPoints;

    [Header("Entidades que puede spawnear")]
    [SerializeField] private SpawnableEntry[] entries;

    [Header("Cadencia")]
    [SerializeField] private float minTimeBetweenSpawns = 0.5f;
    [SerializeField] private float maxTimeBetweenSpawns = 3f;
    [Tooltip("Multiplica el SpawnInterval base del DifficultyManager. >1 = este spawner spawnea menos seguido que el resto.")]
    [SerializeField] private float spawnIntervalMultiplier = 1f;

    [Header("Cadencia por distancia (GameLab)")]
    [Tooltip("Separa los spawns por distancia recorrida en vez de por tiempo: la separación en el mundo no depende de la velocidad.")]
    [SerializeField] private bool distanceBased = false;
    [Tooltip("Hueco mínimo/máximo entre spawns, en unidades del mundo, antes de aplicar la densidad de la zona.")]
    [SerializeField] private float minGap = 8f;
    [SerializeField] private float maxGap = 14f;
    [Tooltip("Piso de justicia: nunca menos que estos segundos entre spawns a la velocidad actual (el salto dura 0,64 s).")]
    [SerializeField] private float minGapSeconds = 0.9f;

    private float nextSpawnTime;
    private float nextSpawnDistance;

    // Distancia pivot→base por pool: se calcula una vez (la geometría de un prefab no cambia)
    private readonly Dictionary<GameObject, float> bottomOffsets = new();

    private void Start() => ScheduleNextSpawn();

    private void Update()
    {
        var difficulty = DifficultyManager.Instance;
        if (distanceBased && difficulty != null)
        {
            if (difficulty.Distance >= nextSpawnDistance)
            {
                SpawnEntity();
                ScheduleNextSpawn();
            }
            return;
        }

        if (Time.time >= nextSpawnTime)
        {
            SpawnEntity();
            ScheduleNextSpawn();
        }
    }

    private void SpawnEntity()
    {
        float currentDifficulty = DifficultyManager.Instance != null ? DifficultyManager.Instance.GameSpeed : 5f;
        SpawnableEntry entry = WeightedSpawnSelector.SelectEntry(entries, currentDifficulty);

        if (entry == null) return;

        SpawnPoint spawnPoint = spawnPoints[entry.spawnPointIndex];
        GameObject spawned = PoolManager.Instance.Spawn(entry.prefab, spawnPoint.point.position);
        if (spawned != null && spawnPoint.alignBottomToPoint)
            AlignBottom(entry.prefab, spawned, spawnPoint.point.position.y);
    }

    private void AlignBottom(GameObject prefab, GameObject spawned, float surfaceY)
    {
        if (!bottomOffsets.TryGetValue(prefab, out float offset))
        {
            offset = SpawnAlignment.BottomOffsetFromPivot(spawned);
            bottomOffsets[prefab] = offset;
        }

        Vector3 pos = spawned.transform.position;
        pos.y = surfaceY + offset;
        spawned.transform.position = pos;
    }

    private void ScheduleNextSpawn()
    {
        var difficulty = DifficultyManager.Instance;
        if (distanceBased && difficulty != null)
        {
            nextSpawnDistance = difficulty.Distance + NextGap(difficulty.GameSpeed, difficulty.Density);
            return;
        }

        float baseInterval = DifficultyManager.Instance != null ? DifficultyManager.Instance.SpawnInterval : 2f;
        float variation = Random.Range(-0.3f, 0.3f);
        float finalInterval = Mathf.Clamp(
            baseInterval * spawnIntervalMultiplier + variation,
            minTimeBetweenSpawns,
            maxTimeBetweenSpawns);

        nextSpawnTime = Time.time + finalInterval;
    }

    /// <summary>Hueco hasta el próximo spawn (unidades): al azar entre min y max, más corto con más densidad,
    /// pero nunca menor que minGapSeconds a la velocidad actual.</summary>
    public float NextGap(float speed, float density)
    {
        float gap = Random.Range(minGap, maxGap) / Mathf.Max(0.1f, density);
        return Mathf.Max(gap, speed * minGapSeconds);
    }
}
