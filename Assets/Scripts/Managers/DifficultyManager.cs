using System;
using UnityEngine;

/// <summary>
/// Velocidad del juego y su progresión. Todo lo que se mueve (scroll, parallax, spawners) lee GameSpeed.
/// Dos modos:
/// - Steps (el de Game hoy): +speedIncreaseAmount cada speedIncreaseInterval segundos hasta maxGameSpeed.
/// - Zones (en prueba en GameLab): tramos de duración fija con su velocidad y densidad; la velocidad
///   pasa de una zona a la siguiente con una rampa suave y, después de la última, sigue subiendo despacio.
/// También acumula la distancia recorrida (para spawn y puntaje por distancia).
/// </summary>
public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    public enum ProgressionMode { Steps, Zones }

    [Serializable]
    public class Zone
    {
        public string name = "Zona";
        [Tooltip("Segundos que dura la zona.")]
        public float duration = 45f;
        [Tooltip("Velocidad que se alcanza al entrar en la zona.")]
        public float speed = 6f;
        [Tooltip("Multiplica la densidad de spawn (>1 = obstáculos más juntos). Solo para spawners por distancia.")]
        public float density = 1f;
    }

    [Tooltip("Steps = progresión original por escalones. Zones = progresión por zonas (GameLab).")]
    [SerializeField] private ProgressionMode mode = ProgressionMode.Steps;

    [Header("Difficulty Progression (Steps)")]
    [SerializeField] private float startGameSpeed = 5f;
    [SerializeField] private float maxGameSpeed = 15f;
    [SerializeField] private float speedIncreaseInterval = 10f; // Cada cuántos segundos aumenta
    [SerializeField] private float speedIncreaseAmount = 0.5f;

    [Header("Spawn Rate Progression (Steps)")]
    [SerializeField] private float startSpawnInterval = 2f;
    [SerializeField] private float minSpawnInterval = 0.8f;
    [SerializeField] private float spawnIntervalDecrease = 0.05f;

    [Header("Zones")]
    [SerializeField] private Zone[] zones;
    [Tooltip("Segundos que tarda la velocidad en pasar de una zona a la siguiente.")]
    [SerializeField] private float zoneRampTime = 3f;
    [Tooltip("Después de la última zona la velocidad sigue subiendo esto por minuto...")]
    [SerializeField] private float endlessSpeedPerMinute = 0.5f;
    [Tooltip("...hasta este máximo.")]
    [SerializeField] private float zoneMaxSpeed = 12f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private float currentGameSpeed;
    private float currentSpawnInterval;
    private float gameTime;
    private float lastSpeedIncrease;

    public ProgressionMode Mode => mode;
    public float GameSpeed => currentGameSpeed;
    public float SpawnInterval => currentSpawnInterval;
    public float GameTime => gameTime;
    /// <summary>Distancia recorrida en unidades del mundo desde el inicio de la partida.</summary>
    public float Distance { get; private set; }
    /// <summary>Zona actual (modo Zones). En Steps siempre 0.</summary>
    public int ZoneIndex { get; private set; }
    public string ZoneName => mode == ProgressionMode.Zones && zones != null && zones.Length > 0
        ? zones[Mathf.Min(ZoneIndex, zones.Length - 1)].name : "-";
    /// <summary>Multiplicador de densidad de spawn de la zona actual (1 en Steps).</summary>
    public float Density { get; private set; } = 1f;

    /// <summary>Se dispara al entrar en una zona nueva (índice). Para avisos visuales/sonoros.</summary>
    public event Action<int> ZoneChanged;

    private bool UsesZones => mode == ProgressionMode.Zones && zones != null && zones.Length > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        ResetDifficulty();
    }

    private void Update()
    {
        gameTime += Time.deltaTime;
        Distance += currentGameSpeed * Time.deltaTime;

        if (UsesZones)
        {
            UpdateZones();
            return;
        }

        // Aumentar dificultad progresivamente
        if (gameTime - lastSpeedIncrease >= speedIncreaseInterval)
        {
            IncreaseDifficulty();
            lastSpeedIncrease = gameTime;
        }
    }

    private void UpdateZones()
    {
        currentGameSpeed = EvaluateZones(zones, gameTime, zoneRampTime, endlessSpeedPerMinute, zoneMaxSpeed, out int zone);
        if (zone != ZoneIndex)
        {
            ZoneIndex = zone;
            Density = zones[Mathf.Min(zone, zones.Length - 1)].density;
            if (showDebugInfo) Debug.Log($"Zona {zone + 1}: {ZoneName}");
            ZoneChanged?.Invoke(zone);
        }
    }

    /// <summary>
    /// Velocidad en el instante <paramref name="time"/> para una lista de zonas (función pura, testeable).
    /// Al entrar en una zona la velocidad va de la de la zona anterior a la nueva en <paramref name="rampTime"/>
    /// con una curva suave. Pasada la última zona, sube <paramref name="endlessPerMinute"/> por minuto hasta <paramref name="maxSpeed"/>.
    /// </summary>
    public static float EvaluateZones(Zone[] zones, float time, float rampTime, float endlessPerMinute, float maxSpeed, out int zoneIndex)
    {
        float start = 0f;
        for (int i = 0; i < zones.Length; i++)
        {
            float end = start + Mathf.Max(0.01f, zones[i].duration);
            if (time < end)
            {
                zoneIndex = i;
                float from = i == 0 ? zones[0].speed : zones[i - 1].speed;
                float k = rampTime > 0f ? Mathf.SmoothStep(0f, 1f, (time - start) / rampTime) : 1f;
                return Mathf.Lerp(from, zones[i].speed, k);
            }
            start = end;
        }

        zoneIndex = zones.Length - 1;
        float extraMinutes = (time - start) / 60f;
        return Mathf.Min(zones[zones.Length - 1].speed + endlessPerMinute * extraMinutes, Mathf.Max(maxSpeed, zones[zones.Length - 1].speed));
    }

    /// <summary>Menú de debug: salta al inicio de la próxima zona (o un minuto más si ya es la última).</summary>
    public void SkipToNextZone()
    {
        if (!UsesZones) { gameTime += speedIncreaseInterval; return; }
        float start = 0f;
        for (int i = 0; i <= ZoneIndex && i < zones.Length; i++) start += Mathf.Max(0.01f, zones[i].duration);
        gameTime = ZoneIndex + 1 < zones.Length ? start : gameTime + 60f;
    }

    private void IncreaseDifficulty()
    {
        // Aumentar velocidad del juego
        if (currentGameSpeed < maxGameSpeed)
        {
            currentGameSpeed += speedIncreaseAmount;
            currentGameSpeed = Mathf.Min(currentGameSpeed, maxGameSpeed);

            if (showDebugInfo)
                Debug.Log($"Velocidad aumentada a: {currentGameSpeed:F2}");
        }

        // Disminuir intervalo de spawn
        if (currentGameSpeed < maxGameSpeed && currentSpawnInterval > minSpawnInterval)
        {
            currentSpawnInterval -= spawnIntervalDecrease;
            currentSpawnInterval = Mathf.Max(currentSpawnInterval, minSpawnInterval);

            if (showDebugInfo)
                Debug.Log($"Intervalo de spawn reducido a: {currentSpawnInterval:F2}");
        }
    }

    public void ResetDifficulty()
    {
        gameTime = 0f;
        lastSpeedIncrease = 0f;
        Distance = 0f;
        ZoneIndex = 0;
        currentSpawnInterval = startSpawnInterval;

        if (UsesZones)
        {
            currentGameSpeed = EvaluateZones(zones, 0f, zoneRampTime, endlessSpeedPerMinute, zoneMaxSpeed, out _);
            Density = zones[0].density;
        }
        else
        {
            currentGameSpeed = startGameSpeed;
            Density = 1f;
        }
    }
}
