using System;
using UnityEngine;

/// <summary>
/// Monedas del jugador (modelo): total acumulado, monedas ganadas en esta partida y el
/// multiplicador del power-up. No conoce la UI: las vistas escuchan Changed.
/// La persistencia va a PlayerPrefs hasta que exista el sistema de guardado central.
/// </summary>
public class CurrencyService : MonoBehaviour
{
    public static CurrencyService Instance { get; private set; }

    public const string CoinsKey = "Coins";

    [Tooltip("Multiplicador mientras dura el power-up de monedas.")]
    [SerializeField] private float multiplierValue = 2f;

    private float multiplier = 1f;
    private int startCoins;

    /// <summary>Total de monedas del jugador.</summary>
    public int Coins { get; private set; }

    /// <summary>Monedas ganadas en esta partida.</summary>
    public int RunCoins => Coins - startCoins;

    /// <summary>Se dispara con el nuevo total cada vez que cambia.</summary>
    public event Action<int> Changed;

    /// <summary>Total guardado, para pantallas sin partida (menú).</summary>
    public static int SavedCoins => PlayerPrefs.GetInt(CoinsKey, 0);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        Coins = SavedCoins;
        startCoins = Coins;
    }

    private void Start()
    {
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

    /// <summary>Una moneda recogida (aplica el multiplicador activo).</summary>
    public void CollectCoin()
    {
        Add(Mathf.RoundToInt(multiplier));
    }

    public void Add(int amount)
    {
        if (amount == 0) return;
        Coins += amount;
        PlayerPrefs.SetInt(CoinsKey, Coins);
        Changed?.Invoke(Coins);
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
