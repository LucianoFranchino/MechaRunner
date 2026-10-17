using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager instance;

    public Text scoreText, finalScoreText, hiScoreText;
    public float scoreCount;
    public float pointsPerSecond;
    public bool scoreIncreasing;
    [SerializeField] private float multiplierValue = 2f;

    private float multiplier = 1f;
    private int lastShownScore = -1;

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
        if (PlayerPrefs.HasKey("highscore"))
            hiScoreText.text = "Highscore: " + Mathf.Round(PlayerPrefs.GetFloat("highscore"));

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
            scoreText.text = finalScoreText.text = "YOUR SCORE: " + shown;
        }
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
        if (PlayerPrefs.HasKey("highscore"))
        {
            if (scoreCount > PlayerPrefs.GetFloat("highscore"))
                PlayerPrefs.SetFloat("highscore", scoreCount);
        }
        else
            PlayerPrefs.SetFloat("highscore", scoreCount);

        // Forzar escritura a disco: en Android, si el SO mata la app no se llama a OnApplicationQuit.
        PlayerPrefs.Save();
    }
}
