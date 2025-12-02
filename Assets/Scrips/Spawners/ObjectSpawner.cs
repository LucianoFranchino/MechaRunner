using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [System.Serializable]
    public class PoolConfig
    {
        public string poolId;
        public GameObject prefab;
        public int initialSize = 10;
        public int maxSize = 100;
        public bool expandable = true;
    }

    [Header("Pool Configurations")]
    [SerializeField] private PoolConfig[] poolConfigs;

    private void Start()
    {
        InitializePools();
    }

    private void InitializePools()
    {
        foreach (var config in poolConfigs)
        {
            if (config.prefab != null)
            {
                PoolManager.Instance.CreatePool(
                    config.poolId,
                    config.prefab,
                    config.initialSize,
                    config.maxSize,
                    config.expandable
                );
            }
        }
    }

    /// <summary>
    /// Spawn rápido por ID
    /// </summary>
    public GameObject Spawn(string poolId, Vector3 position)
    {
        return PoolManager.Instance.Spawn(poolId, position);
    }

    public GameObject Spawn(string poolId, Vector3 position, Quaternion rotation)
    {
        return PoolManager.Instance.Spawn(poolId, position, rotation);
    }

    /// <summary>
    /// Despawn rápido
    /// </summary>
    public void Despawn(GameObject obj)
    {
        var pooledObj = obj.GetComponent<PooledObject>();
        if (pooledObj != null)
        {
            pooledObj.Despawn();
        }
        else
        {
            Debug.LogWarning("Objeto no tiene PooledObject component. Destruyendo en su lugar.");
            Destroy(obj);
        }
    }
}
