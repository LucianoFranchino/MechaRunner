using UnityEngine;

[System.Serializable]
public class SpawnableEntry
{
    public string poolId;
    public int weight = 1;
    public float minDifficulty = 0f;
    public float maxDifficulty = 999f;

    [Tooltip("Índice dentro del array 'Spawn Points' del EntitySpawner que lo contiene.")]
    public int spawnPointIndex;
}