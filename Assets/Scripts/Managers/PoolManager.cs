using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pools de objetos de la escena, identificados por su prefab: quien spawnea referencia
/// el prefab directamente, así que no hay ids escritos a mano que puedan no coincidir.
/// </summary>
public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [Header("Pool Container")]
    [SerializeField] private Transform poolContainer;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;

    private readonly Dictionary<GameObject, Pool> pools = new Dictionary<GameObject, Pool>();

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        // Sin DontDestroyOnLoad: los pools viven lo mismo que la escena, así al
        // reiniciar no quedan enemigos/obstáculos activos de la partida anterior.
        Instance = this;

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
    public void CreatePool(GameObject prefab, int initialSize = 10, int maxSize = 100, bool expandable = true)
    {
        if (prefab == null)
        {
            Debug.LogError("CreatePool: prefab nulo.");
            return;
        }
        if (pools.ContainsKey(prefab))
        {
            Debug.LogWarning($"Pool '{prefab.name}' ya existe.");
            return;
        }

        // Crear contenedor específico para este pool
        GameObject poolObj = new GameObject($"Pool_{prefab.name}");
        poolObj.transform.SetParent(poolContainer);

        pools.Add(prefab, new Pool(prefab, initialSize, maxSize, expandable, poolObj.transform));

        if (showDebugInfo)
            Debug.Log($"Pool '{prefab.name}' creado con {initialSize} objetos.");
    }

    /// <summary>
    /// Obtiene un objeto del pool del prefab. Si el prefab no tiene pool configurado
    /// (ObjectSpawner) se crea uno con valores por defecto y se avisa.
    /// </summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("Spawn: prefab nulo (falta asignarlo en el Inspector).");
            return null;
        }
        if (!pools.TryGetValue(prefab, out var pool))
        {
            Debug.LogWarning($"'{prefab.name}' no tiene pool configurado en ObjectSpawner: se crea uno por defecto.");
            CreatePool(prefab);
            pool = pools[prefab];
        }

        return pool.Spawn(position, rotation);
    }

    /// <summary>
    /// Sobrecarga para spawn con solo posición
    /// </summary>
    public GameObject Spawn(GameObject prefab, Vector3 position)
    {
        return Spawn(prefab, position, Quaternion.identity);
    }

    /// <summary>
    /// Retorna todos los objetos activos de un pool
    /// </summary>
    public void ClearPool(GameObject prefab)
    {
        if (prefab != null && pools.TryGetValue(prefab, out var pool))
            pool.Clear();
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
    public void DestroyPool(GameObject prefab)
    {
        if (prefab != null && pools.TryGetValue(prefab, out var pool))
        {
            pool.DestroyAll();
            pools.Remove(prefab);
        }
    }

    /// <summary>
    /// Obtiene información de un pool
    /// </summary>
    public (int available, int active, int total) GetPoolInfo(GameObject prefab)
    {
        if (prefab != null && pools.TryGetValue(prefab, out var pool))
            return (pool.AvailableCount, pool.ActiveCount, pool.TotalCount);
        return (0, 0, 0);
    }

    /// <summary>
    /// Prefab de un pool existente por nombre. Solo para tests y herramientas de debug:
    /// el juego referencia los prefabs directamente.
    /// </summary>
    public GameObject FindPrefab(string prefabName)
    {
        foreach (var prefab in pools.Keys)
            if (prefab != null && prefab.name == prefabName) return prefab;
        return null;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

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

        foreach (var pool in pools.Values)
        {
            GUILayout.Label($"{pool.Name}: {pool.ActiveCount} active | {pool.AvailableCount} available");
        }

        GUILayout.EndArea();
    }
}
