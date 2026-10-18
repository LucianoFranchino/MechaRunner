using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    [Header("Vistas del puntaje (HUD y pausa; resultados lo anima GameOverSequence)")]
    [Tooltip("Textos que muestran el puntaje actual. Se actualizan solo cuando cambia el número.")]
    [SerializeField] private TMP_Text[] scoreTexts;
    [Tooltip("Textos que muestran el récord guardado (pausa, resultados).")]
    [SerializeField] private TMP_Text[] bestTexts;
    [Tooltip("Se muestra en resultados cuando la partida supera el récord anterior.")]
    [SerializeField] private GameObject newRecordBadge;

    public enum ScoreSource { Time, Distance }

    public float scoreCount;
    [Tooltip("Time = puntos por segundo (Game). Distance = puntos por unidad recorrida (GameLab): premia ir más rápido.")]
    [SerializeField] private ScoreSource source = ScoreSource.Time;
    public float pointsPerSecond;
    [Tooltip("Puntos por unidad del mundo recorrida (modo Distance).")]
    [SerializeField] private float pointsPerUnit = 1f;
    public bool scoreIncreasing;
    [SerializeField] private float multiplierValue = 2f;

    private float multiplier = 1f;
    private int lastShownScore = -1;
    private float bestAtStart;

    public float Best => SaveSystem.Data.progress.bestScore;

    /// <summary>La partida terminada superó el récord con el que empezó (lo fija Save).</summary>
    public bool IsNewRecord { get; private set; }

    private void Awake()
    {
        // Sin DontDestroyOnLoad: referencia textos de la escena, así que tiene que
        // recrearse con ella. El récord persiste en SaveSystem.
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
            scoreCount += PointsPerSecondNow() * multiplier * Time.deltaTime;

        // Solo rearmar el texto cuando cambia el número mostrado: armar un string
        // por frame genera basura que el GC termina cobrando en tirones.
        int shown = Mathf.RoundToInt(scoreCount);
        if (shown != lastShownScore)
        {
            lastShownScore = shown;
            SetAll(scoreTexts, shown);
        }
    }

    private float PointsPerSecondNow()
    {
        if (source == ScoreSource.Distance && DifficultyManager.Instance != null)
            return DifficultyManager.Instance.GameSpeed * pointsPerUnit;
        return pointsPerSecond;
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
        var progress = SaveSystem.Data.progress;
        bool newRecord = scoreCount > progress.bestScore;
        if (newRecord)
            progress.bestScore = scoreCount;

        // Fin de partida: se escribe todo (récord y monedas ganadas). En Android, si el SO
        // mata la app después, no se llama a OnApplicationQuit.
        SaveSystem.Save();

        // Resultados: récord actualizado e insignia NEW si se superó el anterior
        SetAll(bestTexts, Mathf.RoundToInt(Best));
        IsNewRecord = newRecord && scoreCount > bestAtStart;
        if (newRecordBadge != null) newRecordBadge.SetActive(IsNewRecord);
    }
}
