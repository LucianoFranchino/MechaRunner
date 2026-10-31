using UnityEngine;
using UnityEngine.Serialization;

public class Enemy : MonoBehaviour, IPoolable, IDamageable
{
    [FormerlySerializedAs("explocionSound")]
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private int maxHealth = 2;
    [SerializeField] private int enemyDamage = 1;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject deathEffectPrefab;

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
            AudioManager.instance.PlayAudio(explosionSound);
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

    /// <summary>Un enemigo fue destruido (vibración sutil, etc.).</summary>
    public static event System.Action Killed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Killed = null;

    private void Die()
    {
        resolved = true;
        Killed?.Invoke();
        // Con reglas de GameLab el enemigo puede soltar un power-up (o nada) en vez de una moneda
        if (LevelRules.Current != null)
        {
            var drop = LevelRules.Current.RollEnemyDrop();
            if (drop != null) PoolManager.Instance.Spawn(drop, transform.position);
        }
        else
        {
            PoolManager.Instance.Spawn(coinPrefab, transform.position);
        }
        PoolManager.Instance.Spawn(deathEffectPrefab, transform.position);
        pooledObject.Despawn();
    }
}