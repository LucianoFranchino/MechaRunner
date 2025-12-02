using UnityEngine;

public class PooledObject : MonoBehaviour
{
    [Header("Pool Settings")]
    [SerializeField] private bool autoReturnToPool = false;
    [SerializeField] private float autoReturnTime = 5f;

    private string poolId;
    private float spawnTime;

    public string PoolId
    {
        get => poolId;
        set => poolId = value;
    }

    private void Update()
    {
        if (autoReturnToPool && Time.time - spawnTime >= autoReturnTime)
        {
            ReturnToPool();
        }
    }

    public void Initialize(string id)
    {
        poolId = id;
        spawnTime = Time.time;
    }

    public void ReturnToPool()
    {
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
