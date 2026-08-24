using UnityEngine;

public class Enemy : MonoBehaviour, IPooleable
{
    [SerializeField] private AudioClip explocionSound;
    [SerializeField] private int maxHealth = 2;
    [SerializeField] private int enemyDamage = 1;
    [SerializeField] private string coinPoolId = "Coin";
    [SerializeField] private string deathEffectPoolId = "DeathFX";

    private int currentHealth;
    private PooledObject pooledObject;

    private void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
    }

    public void OnSpawn()
    {
        currentHealth = maxHealth;
    }

    public void OnDespawn() { }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            AudioManager.instance.PlayAudio(explocionSound);
            Die();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerHealth>().PlayerDamage(enemyDamage);
            pooledObject.Despawn();
        }
    }

    private void Die()
    {
        PoolManager.Instance.Spawn(coinPoolId, transform.position);
        PoolManager.Instance.Spawn(deathEffectPoolId, transform.position);
        pooledObject.Despawn();
    }
}