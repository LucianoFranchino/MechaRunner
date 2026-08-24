using UnityEngine;

[RequireComponent(typeof(PooledObject))]
public class Bullet : MonoBehaviour, IPooleable
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private int damage = 2;

    private PooledObject pooledObject;

    private void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
    }

    public void OnSpawn()
    {
        rb.linearVelocity = transform.right * speed;
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