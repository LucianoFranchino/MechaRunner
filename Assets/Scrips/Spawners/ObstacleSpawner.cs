using System.Collections.Generic;
using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [System.Serializable]
    public class ObstacleData
    {
        public string poolId;
        public int weight = 1; // Mayor peso = más probabilidad de aparecer
        public float minDifficulty = 0f; // A partir de qué velocidad aparece
        public float maxDifficulty = 999f; // Hasta qué velocidad aparece
    }

    [System.Serializable]
    public class ObstaclePattern
    {
        public string patternName;
        public List<Vector2> relativePositions; // Posiciones relativas al spawn point
        public List<string> obstacleIds; // Qué obstáculo en cada posición
        public int weight = 1;
        public float minDifficulty = 0f;
    }

    [Header("Spawn Settings")]
    [SerializeField] private float spawnX = 12f;
    [SerializeField] private float groundY = 0f;

    [Header("Obstacles Configuration")]
    [SerializeField] private ObstacleData[] obstacles;

    [Header("Spawn Control")]
    [SerializeField] private float minTimeBetweenSpawns = 0.5f;
    [SerializeField] private float maxTimeBetweenSpawns = 3f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private float nextSpawnTime;
    private int totalSpawned = 0;

    private void Start()
    {
        ScheduleNextSpawn();
    }

    private void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            SpawnObstacle();
            ScheduleNextSpawn();
        }
    }

    private void SpawnObstacle()
    {
        SpawnSingleObstacle();
        totalSpawned++;
    }

    private void SpawnSingleObstacle()
    {
        ObstacleData selectedObstacle = SelectRandomObstacle();

        if (selectedObstacle == null)
        {
            Debug.LogWarning("No hay obstáculos válidos para la dificultad actual.");
            return;
        }

        Vector3 spawnPos = new Vector3(spawnX, groundY, 0);

        GameObject obstacle = PoolManager.Instance.Spawn(selectedObstacle.poolId, spawnPos);

        if (obstacle != null)
        {
            // Aplicar velocidad actual del juego
            Obstacle obstacleScript = obstacle.GetComponent<Obstacle>();
            if (obstacleScript != null && DificultyManager.Instance != null)
            {
                obstacleScript.SetSpeed(DificultyManager.Instance.GameSpeed);
            }
        }
    }
    
    private ObstacleData SelectRandomObstacle()
    {
        float currentDifficulty = DificultyManager.Instance != null
            ? DificultyManager.Instance.GameSpeed
            : 5f;

        // Filtrar obstáculos válidos para la dificultad actual
        List<ObstacleData> validObstacles = new List<ObstacleData>();
        int totalWeight = 0;

        foreach (var obstacle in obstacles)
        {
            if (currentDifficulty >= obstacle.minDifficulty &&
                currentDifficulty <= obstacle.maxDifficulty)
            {
                validObstacles.Add(obstacle);
                totalWeight += obstacle.weight;
            }
        }

        if (validObstacles.Count == 0)
            return null;

        // Selección ponderada
        int randomWeight = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var obstacle in validObstacles)
        {
            currentWeight += obstacle.weight;
            if (randomWeight < currentWeight)
            {
                return obstacle;
            }
        }

        return validObstacles[0];
    }

    private void ScheduleNextSpawn()
    {
        float spawnInterval = DificultyManager.Instance != null
            ? DificultyManager.Instance.SpawnInterval
            : 2f;

        // Agregar variación aleatoria
        float variation = Random.Range(-0.3f, 0.3f);
        float finalInterval = Mathf.Clamp(
            spawnInterval + variation,
            minTimeBetweenSpawns,
            maxTimeBetweenSpawns
        );

        nextSpawnTime = Time.time + finalInterval;
    }
}

