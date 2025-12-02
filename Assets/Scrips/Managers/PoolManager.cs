using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [Header("Pool Container")]
    [SerializeField] private Transform poolContainer;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private Dictionary<string, Pool> pools = new Dictionary<string, Pool>();

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Crear contenedor si no existe
        if (poolContainer == null)
        {
            GameObject container = new GameObject("PoolContainer");
            container.transform.SetParent(transform);
            poolContainer = container.transform;
        }
    }

    /// <summary>
    /// Crea un pool para un prefab específico
    /// </summary>
    public void CreatePool(string poolId, GameObject prefab, int initialSize = 10, int maxSize = 100, bool expandable = true)
    {
        if (pools.ContainsKey(poolId))
        {
            Debug.LogWarning($"Pool '{poolId}' ya existe.");
            return;
        }

        // Crear contenedor específico para este pool
        GameObject poolObj = new GameObject($"Pool_{poolId}");
        poolObj.transform.SetParent(poolContainer);

        Pool pool = new Pool(prefab, poolId, initialSize, maxSize, expandable, poolObj.transform);
        pools.Add(poolId, pool);

        if (showDebugInfo)
            Debug.Log($"Pool '{poolId}' creado con {initialSize} objetos.");
    }

    /// <summary>
    /// Obtiene un objeto del pool
    /// </summary>
    public GameObject Spawn(string poolId, Vector3 position, Quaternion rotation)
    {
        if (!pools.ContainsKey(poolId))
        {
            Debug.LogError($"Pool '{poolId}' no existe. Créalo primero con CreatePool().");
            return null;
        }

        return pools[poolId].Spawn(position, rotation);
    }

    /// <summary>
    /// Sobrecarga para spawn con solo posición
    /// </summary>
    public GameObject Spawn(string poolId, Vector3 position)
    {
        return Spawn(poolId, position, Quaternion.identity);
    }

    /// <summary>
    /// Retorna un objeto al pool
    /// </summary>
    public void ReturnToPool(string poolId, GameObject obj)
    {
        if (!pools.ContainsKey(poolId))
        {
            Debug.LogWarning($"Pool '{poolId}' no existe. Destruyendo objeto.");
            Destroy(obj);
            return;
        }

        pools[poolId].Despawn(obj);
    }

    /// <summary>
    /// Retorna todos los objetos activos de un pool
    /// </summary>
    public void ClearPool(string poolId)
    {
        if (pools.ContainsKey(poolId))
        {
            pools[poolId].Clear();
        }
    }

    /// <summary>
    /// Retorna todos los objetos de todos los pools
    /// </summary>
    public void ClearAllPools()
    {
        foreach (var pool in pools.Values)
        {
            pool.Clear();
        }
    }

    /// <summary>
    /// Destruye completamente un pool
    /// </summary>
    public void DestroyPool(string poolId)
    {
        if (pools.ContainsKey(poolId))
        {
            pools[poolId].DestroyAll();
            pools.Remove(poolId);
        }
    }

    /// <summary>
    /// Obtiene información de un pool
    /// </summary>
    public (int available, int active, int total) GetPoolInfo(string poolId)
    {
        if (pools.ContainsKey(poolId))
        {
            var pool = pools[poolId];
            return (pool.AvailableCount, pool.ActiveCount, pool.TotalCount);
        }
        return (0, 0, 0);
    }

    private void OnDestroy()
    {
        foreach (var pool in pools.Values)
        {
            pool.DestroyAll();
        }
        pools.Clear();
    }

    // Debug
    private void OnGUI()
    {
        if (!showDebugInfo) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 500));
        GUILayout.Label("=== POOL MANAGER DEBUG ===");

        foreach (var kvp in pools)
        {
            var info = GetPoolInfo(kvp.Key);
            GUILayout.Label($"{kvp.Key}: {info.active} active | {info.available} available");
        }

        GUILayout.EndArea();
    }
}
