using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menú de debug oculto para probar: monedas, puntos, récord, resetear progreso, invulnerable,
/// power-ups, saltar zona, velocidad, morir, overlay y cambiar de escena.
/// Solo existe en el Editor y en builds con "Development Build" (en la build final no se crea).
/// Se crea solo al arrancar el juego y sobrevive a los cambios de escena: no hay que ponerlo en ninguna escena.
/// Cómo se abre:
///   - Teclado (Editor/PC): F1 o la tecla º/` .
///   - Celular: 4 toques rápidos (en menos de 1,5 s) en la esquina superior izquierda de la pantalla.
/// Mientras está abierto la partida queda congelada.
/// </summary>
public class DevMenu : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private const int TapsToOpen = 4;
    private const float TapWindow = 1.5f;
    private static readonly Rect SecretCorner = new(0f, 0.85f, 0.15f, 0.15f); // en proporción de pantalla (abajo-izq = 0,0)

    private GameObject root;
    private TextMeshProUGUI status;
    private TextMeshProUGUI invulnerableLabel, speedLabel, overlayLabel, resetLabel;
    private int taps;
    private float firstTapTime;
    private bool resetArmed;
    private float speed = 1f;
    private float timeScaleBeforeOpen = 1f;

    public static DevMenu Instance { get; private set; }
    public bool IsOpen => root != null && root.activeSelf;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (Instance != null) return;
        var go = new GameObject("DevMenu");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<DevMenu>();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.f1Key.wasPressedThisFrame || keyboard.backquoteKey.wasPressedThisFrame)) Toggle();

        var pointer = Pointer.current;
        if (pointer != null && pointer.press.wasPressedThisFrame)
        {
            Vector2 p = pointer.position.ReadValue();
            var n = new Vector2(p.x / Screen.width, p.y / Screen.height);
            if (SecretCorner.Contains(n))
            {
                if (taps == 0 || Time.unscaledTime - firstTapTime > TapWindow) { taps = 0; firstTapTime = Time.unscaledTime; }
                if (++taps >= TapsToOpen) { taps = 0; Toggle(); }
            }
        }

        // La velocidad elegida se mantiene aunque la pausa/reanudar vuelva el tiempo a 1
        if (!IsOpen && speed != 1f && GameManager.IsPlaying && Mathf.Approximately(Time.timeScale, 1f)) Time.timeScale = speed;
    }

    public void Toggle()
    {
        if (root == null) Build();
        bool open = !root.activeSelf;
        root.SetActive(open);
        if (open)
        {
            timeScaleBeforeOpen = Time.timeScale;
            Time.timeScale = 0f; // congela la partida mientras se usa el menú
            resetArmed = false;
            RefreshLabels();
        }
        else
        {
            Time.timeScale = GameManager.IsPlaying ? speed : timeScaleBeforeOpen;
        }
    }

    // ------------------------------------------------------------------ acciones

    private void AddCoins(int amount)
    {
        if (CurrencyService.Instance != null) CurrencyService.Instance.Add(amount);
        else SaveSystem.Data.progress.coins += amount;
        SaveSystem.Save();
    }

    private void AddScore(float amount)
    {
        if (ScoreManager.instance != null) ScoreManager.instance.scoreCount += amount;
    }

    private void AddBest(float amount)
    {
        SaveSystem.Data.progress.bestScore += amount;
        SaveSystem.Save();
        if (ScoreManager.instance != null) ScoreManager.instance.SyncBestFromSave();
    }

    private void ResetProgress()
    {
        if (!resetArmed) { resetArmed = true; RefreshLabels(); return; } // pide confirmación con un segundo toque
        resetArmed = false;
        SaveSystem.ResetProgress();
        if (CurrencyService.Instance != null) CurrencyService.Instance.SyncFromSave();
        if (ScoreManager.instance != null) ScoreManager.instance.SyncBestFromSave();
        RefreshLabels();
    }

    private void ToggleInvulnerable()
    {
        PlayerHealth.DevInvulnerable = !PlayerHealth.DevInvulnerable;
        RefreshLabels();
    }

    private void ActivatePowerUps()
    {
        if (PowerUpManager.Instance == null) return;
        PowerUpManager.Instance.Activate(PowerUpType.CoinMultiplier);
        PowerUpManager.Instance.Activate(PowerUpType.ScoreMultiplier);
        PowerUpManager.Instance.Activate(PowerUpType.SuperShoot);
    }

    private void SkipZone()
    {
        if (DifficultyManager.Instance != null) DifficultyManager.Instance.SkipToNextZone();
        RefreshLabels();
    }

    private void CycleSpeed()
    {
        speed = speed >= 4f ? 1f : speed * 2f;
        RefreshLabels();
    }

    private void Die()
    {
        var health = FindFirstObjectByType<PlayerHealth>();
        if (health == null) return;
        Toggle(); // cerrar primero para que el Game Over corra
        health.Death();
    }

    private void ToggleOverlay()
    {
        var overlay = FindFirstObjectByType<DevOverlay>();
        if (overlay != null) overlay.SetVisible(!overlay.IsVisible);
        RefreshLabels();
    }

    private void LoadScene(string scene)
    {
        Toggle();
        Time.timeScale = 1f;
        SaveSystem.SaveIfDirty();
        SceneManager.LoadScene(scene);
    }

    private void RefreshLabels()
    {
        if (root == null) return;
        var d = DifficultyManager.Instance;
        var p = SaveSystem.Data.progress;
        status.SetText($"{SceneManager.GetActiveScene().name}  |  monedas {p.coins}  |  récord {Mathf.RoundToInt(p.bestScore)}" +
                       (d != null ? $"  |  zona {d.ZoneIndex + 1}  vel {d.GameSpeed:0.0}" : ""));
        invulnerableLabel.SetText(PlayerHealth.DevInvulnerable ? "INVULNERABLE: SÍ" : "INVULNERABLE: NO");
        speedLabel.SetText($"VELOCIDAD x{speed:0}");
        var overlay = FindFirstObjectByType<DevOverlay>();
        overlayLabel.SetText(overlay != null && overlay.IsVisible ? "OVERLAY: SÍ" : "OVERLAY: NO");
        resetLabel.SetText(resetArmed ? "¿SEGURO? TOCÁ DE NUEVO" : "RESETEAR PROGRESO");
    }

    // ------------------------------------------------------------------ UI (se arma en código la primera vez)

    private void Build()
    {
        root = new GameObject("DevMenuCanvas");
        root.transform.SetParent(transform, false);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;
        root.AddComponent<GraphicRaycaster>();

        var dim = Rect("Dim", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        dim.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.03f, 0.06f, 0.82f);

        var title = Text("Title", root.transform, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1600f, 80f), 60f);
        title.SetText("MENÚ DE DEBUG");
        status = Text("Status", root.transform, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1800f, 50f), 30f);

        // Grilla de botones: 3 columnas
        var actions = new (string label, UnityEngine.Events.UnityAction action)[]
        {
            ("+100 MONEDAS", () => { AddCoins(100); RefreshLabels(); }),
            ("+1000 MONEDAS", () => { AddCoins(1000); RefreshLabels(); }),
            ("+500 PUNTOS (PARTIDA)", () => AddScore(500f)),
            ("RÉCORD +1000", () => { AddBest(1000f); RefreshLabels(); }),
            ("RESETEAR PROGRESO", ResetProgress),
            ("INVULNERABLE", ToggleInvulnerable),
            ("ACTIVAR POWER-UPS", ActivatePowerUps),
            ("SALTAR ZONA", SkipZone),
            ("VELOCIDAD", CycleSpeed),
            ("MORIR (GAME OVER)", Die),
            ("OVERLAY", ToggleOverlay),
            ("IR AL MENÚ", () => LoadScene("MainMenu")),
            ("IR A GAMELAB", () => LoadScene("GameLab")),
            ("IR A GAME", () => LoadScene("Game")),
            ("CERRAR", Toggle),
        };
        const int columns = 3;
        var size = new Vector2(520f, 110f);
        int slot = 0;
        for (int i = 0; i < actions.Length; i++)
        {
            // Escenas desactivadas en Build Settings no se pueden cargar: no mostrar su botón
            if (actions[i].label == "IR A GAME" && !Application.CanStreamedLevelBeLoaded("Game")) continue;
            if (actions[i].label == "IR A GAMELAB" && !Application.CanStreamedLevelBeLoaded("GameLab")) continue;
            int col = slot % columns, row = slot / columns;
            slot++;
            var pos = new Vector2((col - 1) * (size.x + 30f), -230f - row * (size.y + 24f));
            var label = Button(actions[i].label, root.transform, pos, size, actions[i].action);
            if (actions[i].label == "INVULNERABLE") invulnerableLabel = label;
            if (actions[i].label == "VELOCIDAD") speedLabel = label;
            if (actions[i].label == "OVERLAY") overlayLabel = label;
            if (actions[i].label == "RESETEAR PROGRESO") resetLabel = label;
        }
        root.SetActive(false);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    private static TextMeshProUGUI Text(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize)
    {
        var rt = Rect(name, parent, anchor, anchor, pos, size);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.fontSize = fontSize;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        return t;
    }

    private static TextMeshProUGUI Button(string label, Transform parent, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        var rt = Rect(label, parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, size);
        var image = rt.gameObject.AddComponent<Image>();
        image.color = new Color(0.22f, 0.27f, 0.45f, 1f);
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        var text = Text("Label", rt, new Vector2(0.5f, 0.5f), Vector2.zero, size, 32f);
        text.SetText(label);
        return text;
    }
#endif
}
