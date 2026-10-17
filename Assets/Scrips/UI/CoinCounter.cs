using TMPro;
using UnityEngine;

/// <summary>
/// Contador de monedas de la escena. Vive en un objeto de la escena (no en el
/// jugador): las monedas lo encuentran por CoinCounter.Instance.
/// </summary>
public class CoinCounter : MonoBehaviour
{
    public static CoinCounter Instance { get; private set; }

    public const string CoinsKey = "Coins";

    public int coinValue;
    public TMP_Text coinScore;
    [SerializeField] private float multiplierValue = 2f;

    private float multiplier = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        coinValue = PlayerPrefs.GetInt(CoinsKey, coinValue);
        coinScore.SetText("{0}", coinValue);

        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.Activated += OnPowerUpActivated;
            PowerUpManager.Instance.Expired += OnPowerUpExpired;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

        if (PowerUpManager.Instance != null)
        {
            PowerUpManager.Instance.Activated -= OnPowerUpActivated;
            PowerUpManager.Instance.Expired -= OnPowerUpExpired;
        }
    }

    public void GetCoin()
    {
        coinValue += Mathf.RoundToInt(multiplier);
        coinScore.SetText("{0}", coinValue);
        PlayerPrefs.SetInt(CoinsKey, coinValue);
    }

    private void OnPowerUpActivated(PowerUpType type, float duration)
    {
        if (type == PowerUpType.CoinMultiplier)
            multiplier = multiplierValue;
    }

    private void OnPowerUpExpired(PowerUpType type)
    {
        if (type == PowerUpType.CoinMultiplier)
            multiplier = 1f;
    }
}
