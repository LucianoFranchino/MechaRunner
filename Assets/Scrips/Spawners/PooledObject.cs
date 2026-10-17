using UnityEngine;

public class PooledObject : MonoBehaviour, IPooleable
{
    [Header("Pool Settings")]
    [SerializeField] private bool autoReturnToPool = false;
    [SerializeField] private float autoReturnTime = 5f;

    private string poolId;
    private float spawnTime;
    private IPooleable[] poolables;

    public string PoolId
    {
        get => poolId;
        set => poolId = value;
    }

    /// <summary>
    /// Componentes IPooleable del objeto (incluido este), cacheados para no
    /// llamar a GetComponents en cada spawn/despawn.
    /// </summary>
    public IPooleable[] Poolables => poolables ??= GetComponents<IPooleable>();

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

    public void Initialize(string id)
    {
        poolId = id;
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
        PoolManager.Instance.ReturnToPool(poolId, gameObject);
    }

    /// <summary>
    /// Llamar desde código para retornar manualmente
    /// </summary>
    public void Despawn()
    {
        ReturnToPool();
    }
}
