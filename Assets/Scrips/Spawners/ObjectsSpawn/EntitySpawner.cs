using UnityEngine;
using MechaRunner.Spawning;

[System.Serializable]
public class SpawnPoint
{
    [Tooltip("Solo para identificarlo en el Inspector, ej: 'Suelo', 'Plataforma', 'Aire'.")]
    public string label;
    public Transform point;
}

/// <summary>
/// Spawner genérico configurable: se instancia varias veces en escena
/// (una para obstáculos+enemigos de suelo, otra para enemigos voladores, etc.)
/// con distintas entradas y spawn points, todas sobre el mismo PoolManager.
/// </summary>
public class EntitySpawner : MonoBehaviour
{
    [Header("Puntos de spawn (por altura)")]
    [SerializeField] private SpawnPoint[] spawnPoints;

    [Header("Entidades que puede spawnear")]
    [SerializeField] private SpawnableEntry[] entries;

    [Header("Cadencia")]
    [SerializeField] private float minTimeBetweenSpawns = 0.5f;
    [SerializeField] private float maxTimeBetweenSpawns = 3f;
    [Tooltip("Multiplica el SpawnInterval base del DificultyManager. >1 = este spawner spawnea menos seguido que el resto.")]
    [SerializeField] private float spawnIntervalMultiplier = 1f;

    private float nextSpawnTime;

    private void Start() => ScheduleNextSpawn();

    private void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            SpawnEntity();
            ScheduleNextSpawn();
        }
    }

    private void SpawnEntity()
    {
        float currentDifficulty = DificultyManager.Instance != null ? DificultyManager.Instance.GameSpeed : 5f;
        SpawnableEntry entry = WeightedSpawnSelector.SelectEntry(entries, currentDifficulty);

        if (entry == null) return;

        Transform spawnPoint = spawnPoints[entry.spawnPointIndex].point;
        PoolManager.Instance.Spawn(entry.poolId, spawnPoint.position);
    }

    private void ScheduleNextSpawn()
    {
        float baseInterval = DificultyManager.Instance != null ? DificultyManager.Instance.SpawnInterval : 2f;
        float variation = Random.Range(-0.3f, 0.3f);
        float finalInterval = Mathf.Clamp(
            baseInterval * spawnIntervalMultiplier + variation,
            minTimeBetweenSpawns,
            maxTimeBetweenSpawns);

        nextSpawnTime = Time.time + finalInterval;
    }
}