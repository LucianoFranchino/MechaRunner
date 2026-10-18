using UnityEngine;

public class PooledObject : MonoBehaviour, IPoolable
{
    [Header("Pool Settings")]
    [SerializeField] private bool autoReturnToPool = false;
    [SerializeField] private float autoReturnTime = 5f;

    private Pool owner;
    private float spawnTime;
    private IPoolable[] poolables;

    /// <summary>Prefab del que salió este objeto (null si no vino de un pool).</summary>
    public GameObject SourcePrefab => owner != null ? owner.Prefab : null;

    /// <summary>
    /// Componentes IPoolable del objeto (incluido este), cacheados para no
    /// llamar a GetComponents en cada spawn/despawn.
    /// </summary>
    public IPoolable[] Poolables => poolables ??= GetComponents<IPoolable>();

    private void Awake()
    {
        // Sin auto-retorno no hace falta que corra Update en cada objeto del pool
        enabled = autoReturnToPool;
    }

    private void Update()
    {
        if (Time.time - spawnTime >= autoReturnTime)
        {
            ReturnToPool();
        }
    }

    public void Initialize(Pool pool)
    {
        owner = pool;
    }

    // El timer de auto-retorno arranca en cada spawn, no al crear el objeto.
    public void OnSpawn()
    {
        spawnTime = Time.time;
    }

    public void OnDespawn() { }

    public void ReturnToPool()
    {
        // Al descargar la escena el PoolManager puede destruirse antes que sus objetos.
        if (PoolManager.Instance == null) return;
        if (owner == null)
        {
            Debug.LogWarning($"{name} no salió de un pool. Destruyendo.");
            Destroy(gameObject);
            return;
        }
        owner.Despawn(gameObject);
    }

    /// <summary>
    /// Llamar desde código para retornar manualmente
    /// </summary>
    public void Despawn()
    {
        ReturnToPool();
    }
}
