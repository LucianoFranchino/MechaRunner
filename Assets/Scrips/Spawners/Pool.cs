using System.Collections.Generic;
using UnityEngine;

public class Pool
{
    private GameObject prefab;
    private Queue<PooledObject> availableObjects;
    private HashSet<PooledObject> activeObjects;
    private readonly Dictionary<GameObject, PooledObject> lookup = new();
    private Transform container;
    private string poolId;
    private int maxSize;
    private bool expandable;
    private bool limitWarningShown;

    public int AvailableCount => availableObjects.Count;
    public int ActiveCount => activeObjects.Count;
    public int TotalCount => AvailableCount + ActiveCount;

    public Pool(GameObject prefab, string poolId, int initialSize, int maxSize, bool expandable, Transform container)
    {
        this.prefab = prefab;
        this.poolId = poolId;
        this.maxSize = maxSize;
        this.expandable = expandable;
        this.container = container;

        availableObjects = new Queue<PooledObject>(initialSize);
        activeObjects = new HashSet<PooledObject>();

        // Pre-warm: crear objetos iniciales
        for (int i = 0; i < initialSize; i++)
        {
            availableObjects.Enqueue(CreateNewObject());
        }
    }

    private PooledObject CreateNewObject()
    {
        GameObject obj = Object.Instantiate(prefab, container);
        obj.name = $"{prefab.name}_{TotalCount}";
        obj.SetActive(false);

        // Asignar PooledObject si no existe
        PooledObject pooledObj = obj.GetComponent<PooledObject>();
        if (pooledObj == null)
        {
            pooledObj = obj.AddComponent<PooledObject>();
        }
        pooledObj.Initialize(poolId);

        lookup[obj] = pooledObj;
        return pooledObj;
    }

    public GameObject Spawn(Vector3 position, Quaternion rotation)
    {
        PooledObject pooled = null;

        // Descartar objetos destruidos por fuera del pool (ej. un Destroy() accidental)
        while (availableObjects.Count > 0 && availableObjects.Peek() == null)
        {
            availableObjects.Dequeue();
        }

        // Intentar obtener un objeto disponible
        if (availableObjects.Count > 0)
        {
            pooled = availableObjects.Dequeue();
        }
        else if (expandable && (maxSize <= 0 || TotalCount < maxSize))
        {
            // Crear nuevo objeto si el pool es expandible
            pooled = CreateNewObject();
        }
        else
        {
            // Avisar una sola vez: si no, en un pool lleno se loguea en cada spawn
            if (!limitWarningShown)
            {
                Debug.LogWarning($"Pool '{poolId}' alcanzó su límite máximo ({maxSize}). Considerá subir su Max Size.");
                limitWarningShown = true;
            }
            return null;
        }

        // Configurar el objeto
        GameObject obj = pooled.gameObject;
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        activeObjects.Add(pooled);

        // Llamar a OnSpawn en todos los IPooleable (cacheados en PooledObject)
        foreach (var poolable in pooled.Poolables)
        {
            poolable.OnSpawn();
        }

        return obj;
    }

    public void Despawn(GameObject obj)
    {
        if (!lookup.TryGetValue(obj, out var pooled) || !activeObjects.Remove(pooled))
        {
            Debug.LogWarning($"Objeto {obj.name} no pertenece a este pool o ya fue retornado.");
            return;
        }

        // Llamar a OnDespawn en todos los IPooleable
        foreach (var poolable in pooled.Poolables)
        {
            poolable.OnDespawn();
        }

        obj.SetActive(false);
        if (obj.transform.parent != container) obj.transform.SetParent(container);
        availableObjects.Enqueue(pooled);
    }

    public void Clear()
    {
        foreach (var pooled in activeObjects)
        {
            if (pooled == null) continue;

            foreach (var poolable in pooled.Poolables)
            {
                poolable.OnDespawn();
            }
            pooled.gameObject.SetActive(false);
            availableObjects.Enqueue(pooled);
        }
        activeObjects.Clear();
    }

    public void DestroyAll()
    {
        while (availableObjects.Count > 0)
        {
            var pooled = availableObjects.Dequeue();
            if (pooled != null)
                Object.Destroy(pooled.gameObject);
        }

        foreach (var pooled in activeObjects)
        {
            if (pooled != null)
                Object.Destroy(pooled.gameObject);
        }
        activeObjects.Clear();
        lookup.Clear();
    }
}
