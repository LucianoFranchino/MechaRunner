using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    public const string HighscoreKey = "highscore";

    [Header("Vistas del puntaje (HUD, pausa, resultados)")]
    [Tooltip("Textos que muestran el puntaje actual. Se actualizan solo cuando cambia el número.")]
    [SerializeField] private TMP_Text[] scoreTexts;
    [Tooltip("Textos que muestran el récord guardado (pausa, resultados).")]
    [SerializeField] private TMP_Text[] bestTexts;
    [Tooltip("Se muestra en resultados cuando la partida supera el récord anterior.")]
    [SerializeField] private GameObject newRecordBadge;

    public float scoreCount;
    public float pointsPerSecond;
    public bool scoreIncreasing;
    [SerializeField] private float multiplierValue = 2f;

    private float multiplier = 1f;
    private int lastShownScore = -1;
    private float bestAtStart;

    public float Best => PlayerPrefs.GetFloat(HighscoreKey, 0f);

    private void Awake()
    {
        // Sin DontDestroyOnLoad: referencia textos de la escena, así que tiene que
        // recrearse con ella. El highscore persiste en PlayerPrefs.
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Start()
    {
        bestAtStart = Best;
        SetAll(bestTexts, Mathf.RoundToInt(bestAtStart));
        if (newRecordBadge != null) newRecordBadge.SetActive(false);

        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.Activated += OnPowerUpActivated;
            PowerUpManager.Instance.Expired += OnPowerUpExpired;
        }
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;

        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.Activated -= OnPowerUpActivated;
            PowerUpManager.Instance.Expired -= OnPowerUpExpired;
        }
    }

    private void Update()
    {
        if (scoreIncreasing)
            scoreCount += (pointsPerSecond * multiplier) * Time.deltaTime;

        // Solo rearmar el texto cuando cambia el número mostrado: armar un string
        // por frame genera basura que el GC termina cobrando en tirones.
        int shown = Mathf.RoundToInt(scoreCount);
        if (shown != lastShownScore)
        {
            lastShownScore = shown;
            SetAll(scoreTexts, shown);
        }
    }

    private static void SetAll(TMP_Text[] texts, int value)
    {
        if (texts == null) return;
        // SetText con formato numérico no genera strings intermedios
        foreach (var t in texts) if (t != null) t.SetText("{0}", value);
    }

    private void OnPowerUpActivated(PowerUpType type, float duration)
    {
        if (type == PowerUpType.ScoreMultiplier)
            multiplier = multiplierValue;
    }

    private void OnPowerUpExpired(PowerUpType type)
    {
        if (type == PowerUpType.ScoreMultiplier)
            multiplier = 1f;
    }

    public void Save()
    {
        bool newRecord = !PlayerPrefs.HasKey(HighscoreKey) || scoreCount > PlayerPrefs.GetFloat(HighscoreKey);
        if (newRecord)
            PlayerPrefs.SetFloat(HighscoreKey, scoreCount);

        // Forzar escritura a disco: en Android, si el SO mata la app no se llama a OnApplicationQuit.
        PlayerPrefs.Save();

        // Resultados: récord actualizado e insignia NEW si se superó el anterior
        SetAll(bestTexts, Mathf.RoundToInt(Best));
        if (newRecordBadge != null) newRecordBadge.SetActive(newRecord && scoreCount > bestAtStart);
    }
}
