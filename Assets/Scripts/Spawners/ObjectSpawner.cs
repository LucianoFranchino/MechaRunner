using UnityEngine;

/// <summary>
/// Crea los pools de la escena al arrancar, con su tamaño inicial y máximo.
/// </summary>
public class ObjectSpawner : MonoBehaviour
{
    [System.Serializable]
    public class PoolConfig
    {
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
                    config.prefab,
                    config.initialSize,
                    config.maxSize,
                    config.expandable
                );
            }
        }
    }
}
