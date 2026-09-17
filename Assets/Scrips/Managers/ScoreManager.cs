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

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
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
        scoreText.text = finalScoreText.text = "YOUR SCORE: " + Mathf.Round(scoreCount);
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
    }
}
