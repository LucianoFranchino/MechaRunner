using System.Collections.Generic;
using UnityEngine;

public class Pool
{
    private GameObject prefab;
    private Queue<GameObject> availableObjects;
    private HashSet<GameObject> activeObjects;
    private Transform container;
    private string poolId;
    private int maxSize;
    private bool expandable;

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

        availableObjects = new Queue<GameObject>(initialSize);
        activeObjects = new HashSet<GameObject>();

        // Pre-warm: crear objetos iniciales
        for (int i = 0; i < initialSize; i++)
        {
            CreateNewObject();
        }
    }

    private GameObject CreateNewObject()
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

        availableObjects.Enqueue(obj);
        return obj;
    }

    public GameObject Spawn(Vector3 position, Quaternion rotation)
    {
        GameObject obj = null;

        // Intentar obtener un objeto disponible
        if (availableObjects.Count > 0)
        {
            obj = availableObjects.Dequeue();
        }
        else if (expandable && (maxSize <= 0 || TotalCount < maxSize))
        {
            // Crear nuevo objeto si el pool es expandible
            obj = CreateNewObject();
            availableObjects.Dequeue(); // Sacarlo inmediatamente
        }
        else
        {
            Debug.LogWarning($"Pool '{poolId}' alcanzó su límite máximo ({maxSize}) y no es expandible.");
            return null;
        }

        // Configurar el objeto
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);
        activeObjects.Add(obj);

        // Llamar a OnSpawn en todos los IPooleable
        var poolables = obj.GetComponents<IPooleable>();
        foreach (var poolable in poolables)
        {
            poolable.OnSpawn();
        }

        return obj;
    }

    public void Despawn(GameObject obj)
    {
        if (!activeObjects.Contains(obj))
        {
            Debug.LogWarning($"Objeto {obj.name} no pertenece a este pool o ya fue retornado.");
            return;
        }

        // Llamar a OnDespawn en todos los IPooleable
        var poolables = obj.GetComponents<IPooleable>();
        foreach (var poolable in poolables)
        {
            poolable.OnDespawn();
        }

        obj.SetActive(false);
        obj.transform.SetParent(container);
        activeObjects.Remove(obj);
        availableObjects.Enqueue(obj);
    }

    public void Clear()
    {
        foreach (var obj in activeObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
                availableObjects.Enqueue(obj);
            }
        }
        activeObjects.Clear();
    }

    public void DestroyAll()
    {
        while (availableObjects.Count > 0)
        {
            var obj = availableObjects.Dequeue();
            if (obj != null)
                Object.Destroy(obj);
        }

        foreach (var obj in activeObjects)
        {
            if (obj != null)
                Object.Destroy(obj);
        }
        activeObjects.Clear();
    }
}
