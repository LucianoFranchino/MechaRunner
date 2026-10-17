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
    // Evita resolver dos veces el mismo enemigo (dos colliders del jugador o dos balas en un paso)
    private bool resolved;

    private void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
    }

    public void OnSpawn()
    {
        currentHealth = maxHealth;
        resolved = false;
    }

    public void OnDespawn() { }

    public void TakeDamage(int damage)
    {
        if (resolved) return;
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            AudioManager.instance.PlayAudio(explocionSound);
            Die();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (resolved || !other.CompareTag("Player")) return;

        var health = other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;
        resolved = true;
        health.PlayerDamage(enemyDamage);
        pooledObject.Despawn();
    }

    private void Die()
    {
        resolved = true;
        PoolManager.Instance.Spawn(coinPoolId, transform.position);
        PoolManager.Instance.Spawn(deathEffectPoolId, transform.position);
        pooledObject.Despawn();
    }
}