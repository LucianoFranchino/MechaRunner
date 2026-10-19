using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Datos de tuning en pantalla (zona, velocidad, distancia, densidad, puntaje, FPS, sección) para ajustar
/// el diseño de nivel. Solo existe en el Editor y en builds de desarrollo; en la build final no hace nada.
/// Sin basura de memoria: textos TMP actualizados 4 veces por segundo con SetText numérico (no arma strings)
/// y los nombres solo cuando cambian. (Antes usaba OnGUI, que reserva ~2 KB por frame y adelanta el GC.)
/// </summary>
public class DevOverlay : MonoBehaviour
{
    [SerializeField] private bool visible = true;
    [SerializeField] private float fontSize = 26f;

#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
    public void SetVisible(bool show) { }
    public bool IsVisible => false;
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private TextMeshProUGUI numbers;
    private TextMeshProUGUI labels;
    private readonly StringBuilder labelText = new(128);
    private LevelDirector director;
    private string shownZone, shownSection;
    private float nextRefresh;
    private int frames;
    private float frameTime;

    private void Start()
    {
        if (!visible) return;
        director = FindFirstObjectByType<LevelDirector>();
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;
        numbers = CreateText(canvas.transform, "DevOverlay_Numbers", new Vector2(0f, 150f));
        labels = CreateText(canvas.transform, "DevOverlay_Labels", new Vector2(0f, 30f));
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(900f, 120f);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = fontSize;
        t.alignment = TextAlignmentOptions.Bottom;
        t.color = new Color(1f, 1f, 1f, 0.9f);
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        return t;
    }

    /// <summary>Menú de debug: mostrar u ocultar el overlay.</summary>
    public void SetVisible(bool show)
    {
        visible = show;
        if (numbers != null) numbers.gameObject.SetActive(show);
        if (labels != null) labels.gameObject.SetActive(show);
    }

    public bool IsVisible => visible;

    private void Update()
    {
        if (numbers == null) return;
        frames++;
        frameTime += Time.unscaledDeltaTime;
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.25f;

        float fps = frameTime > 0f ? frames / frameTime : 0f;
        frames = 0;
        frameTime = 0f;

        var d = DifficultyManager.Instance;
        var s = ScoreManager.instance;
        if (d == null) { numbers.SetText("FPS {0:0}", fps); return; }
        numbers.SetText("zona {0:0}  vel {1:2}  dens {2:2}\ndist {3:0}  t {4:0}s  score {5:0}  FPS {6:0}",
            d.ZoneIndex + 1, d.GameSpeed, d.Density, d.Distance, d.GameTime, s != null ? s.scoreCount : 0f, fps);

        // Nombres: solo se reescriben cuando cambian (sin concatenar strings)
        string zone = d.ZoneName;
        string section = director != null && director.CurrentSection != null ? director.CurrentSection.name : "";
        if (!ReferenceEquals(zone, shownZone) || !ReferenceEquals(section, shownSection))
        {
            shownZone = zone;
            shownSection = section;
            labelText.Clear().Append(zone).Append("  |  ").Append(section);
            labels.SetText(labelText);
        }
    }
#endif
}
