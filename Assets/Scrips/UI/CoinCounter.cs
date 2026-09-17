using UnityEngine;
using UnityEngine.UI;

public class CoinCounter : MonoBehaviour
{
    public int coinValue;
    public Text coinScore;
    [SerializeField] private float multiplierValue = 2f;

    private float multiplier = 1f;

    private void Start()
    {
        if (PlayerPrefs.HasKey("Coins"))
            coinValue = PlayerPrefs.GetInt("Coins");

        coinScore.text = "" + coinValue;

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

    public void GetCoin()
    {
        coinValue += 1 * (int)multiplier;
        coinScore.text = " " + coinValue;
        PlayerPrefs.SetInt("Coins", coinValue);
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
