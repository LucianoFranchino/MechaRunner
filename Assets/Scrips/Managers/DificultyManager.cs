using UnityEngine;

public class DificultyManager : MonoBehaviour
{
    public static DificultyManager Instance { get; private set; }

    [Header("Difficulty Progression")]
    [SerializeField] private float startGameSpeed = 5f;
    [SerializeField] private float maxGameSpeed = 15f;
    [SerializeField] private float speedIncreaseInterval = 10f; // Cada cuántos segundos aumenta
    [SerializeField] private float speedIncreaseAmount = 0.5f;

    [Header("Spawn Rate Progression")]
    [SerializeField] private float startSpawnInterval = 2f;
    [SerializeField] private float minSpawnInterval = 0.8f;
    [SerializeField] private float spawnIntervalDecrease = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private float currentGameSpeed;
    private float currentSpawnInterval;
    private float gameTime;
    private float lastSpeedIncrease;

    public float GameSpeed => currentGameSpeed;
    public float SpawnInterval => currentSpawnInterval;
    public float GameTime => gameTime;

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

        // Aumentar dificultad progresivamente
        if (gameTime - lastSpeedIncrease >= speedIncreaseInterval)
        {
            IncreaseDifficulty();
            lastSpeedIncrease = gameTime;
        }
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
        if (currentSpawnInterval > minSpawnInterval)
        {
            currentSpawnInterval -= spawnIntervalDecrease;
            currentSpawnInterval = Mathf.Max(currentSpawnInterval, minSpawnInterval);

            if (showDebugInfo)
                Debug.Log($"Intervalo de spawn reducido a: {currentSpawnInterval:F2}");
        }
    }

    public void ResetDifficulty()
    {
        currentGameSpeed = startGameSpeed;
        currentSpawnInterval = startSpawnInterval;
        gameTime = 0f;
        lastSpeedIncrease = 0f;
    }
}
