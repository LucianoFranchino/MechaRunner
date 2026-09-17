using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dueño único del timing de los power-ups: sabe cuánto dura cada uno y cuánto
/// falta, y avisa por evento cuándo se activan/expiran. No sabe QUÉ hace cada
/// power-up — eso lo decide quien escucha el evento (ScoreManager, CoinCounter, Weapon).
/// </summary>
public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance { get; private set; }

    [System.Serializable]
    public class PowerUpConfig
    {
        public PowerUpType type;
        public float baseDuration = 5f;
    }

    [SerializeField] private PowerUpConfig[] configs;

    public event Action<PowerUpType, float> Activated;       // type, duration
    public event Action<PowerUpType, float, float> Ticked;   // type, remaining, duration
    public event Action<PowerUpType> Expired;

    private readonly Dictionary<PowerUpType, float> remaining = new();
    private readonly Dictionary<PowerUpType, float> durations = new();
    private readonly List<PowerUpType> activeKeysBuffer = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (remaining.Count == 0) return;

        activeKeysBuffer.Clear();
        activeKeysBuffer.AddRange(remaining.Keys);

        foreach (var type in activeKeysBuffer)
        {
            float value = remaining[type] - Time.deltaTime;

            if (value <= 0f)
            {
                remaining.Remove(type);
                durations.Remove(type);
                Expired?.Invoke(type);
            }
            else
            {
                remaining[type] = value;
                Ticked?.Invoke(type, value, durations[type]);
            }
        }
    }

    public void Activate(PowerUpType type)
    {
        float duration = GetDuration(type);
        bool wasActive = remaining.ContainsKey(type);

        remaining[type] = duration;
        durations[type] = duration;

        if (!wasActive)
            Activated?.Invoke(type, duration);
    }

    public bool IsActive(PowerUpType type) => remaining.ContainsKey(type);

    private float GetDuration(PowerUpType type)
    {
        foreach (var config in configs)
        {
            if (config.type == type)
                return config.baseDuration * ShopUpgradeMultiplier(type);
        }
        return 5f;
    }

    // TODO: cuando exista la tienda, esto va a multiplicar según lo que el jugador
    // haya comprado (ej. 1.5f si compró "nivel 1" de duración para este power-up).
    // Por ahora siempre devuelve 1 (sin upgrades).
    private float ShopUpgradeMultiplier(PowerUpType type) => 1f;
}