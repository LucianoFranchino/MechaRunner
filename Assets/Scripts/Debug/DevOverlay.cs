using UnityEngine;

/// <summary>
/// Datos de tuning en pantalla (zona, velocidad, distancia, densidad, puntaje, FPS) para ajustar
/// el diseño de nivel. Solo existe en el Editor y en builds de desarrollo; en la build final no hace nada.
/// Arma el texto 4 veces por segundo para no generar basura en cada frame.
/// </summary>
public class DevOverlay : MonoBehaviour
{
    [SerializeField] private bool visible = true;
    [SerializeField] private int fontSize = 26;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private string text = "";
    private float nextRefresh;
    private int frames;
    private float frameTime;
    private GUIStyle style;

    private void Update()
    {
        frames++;
        frameTime += Time.unscaledDeltaTime;
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.25f;

        float fps = frameTime > 0f ? frames / frameTime : 0f;
        frames = 0;
        frameTime = 0f;

        var d = DifficultyManager.Instance;
        var s = ScoreManager.instance;
        text = d == null
            ? $"FPS {fps:0}"
            : $"{d.Mode}  zona {d.ZoneIndex + 1} ({d.ZoneName})\n" +
              $"vel {d.GameSpeed:0.00}  dens {d.Density:0.00}\n" +
              $"dist {d.Distance:0}  t {d.GameTime:0}s\n" +
              $"score {(s != null ? s.scoreCount : 0f):0}  FPS {fps:0}";
    }

    private void OnGUI()
    {
        if (!visible) return;
        style ??= new GUIStyle(GUI.skin.box) { fontSize = fontSize, alignment = TextAnchor.UpperLeft, normal = { textColor = Color.white } };
        GUI.Box(new Rect(Screen.width * 0.5f - 230f, Screen.height - 190f, 460f, 170f), text, style);
    }
#endif
}
