using UnityEngine;

[RequireComponent(typeof(PooledObject))]
public class Bullet : MonoBehaviour, IPoolable
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private int damage = 2;
    [Tooltip("Segundos hasta volver al pool si no choca con nada (si no, el pool se agota y el arma deja de disparar).")]
    [SerializeField] private float maxLifetime = 3f;

    private PooledObject pooledObject;
    private float despawnTime;

    private void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
    }

    private void Update()
    {
        if (Time.time >= despawnTime)
            pooledObject.Despawn();
    }

    public void OnSpawn()
    {
        rb.linearVelocity = transform.right * speed;
        despawnTime = Time.time + maxLifetime;
    }

    public void OnDespawn()
    {
        rb.linearVelocity = Vector2.zero;
    }

    private void OnTriggerEnter2D(Collider2D hitInfo)
    {
        if (hitInfo.gameObject.CompareTag("Enemy"))
        {
            hitInfo.GetComponent<Enemy>().TakeDamage(damage);
            pooledObject.Despawn();
        }
        else if (hitInfo.gameObject.CompareTag("Obstacle"))
        {
            pooledObject.Despawn();
        }
    }
}