using UnityEngine;
using UnityEngine.Serialization;

public class Enemy : MonoBehaviour, IPoolable, IDamageable
{
    [FormerlySerializedAs("explocionSound")]
    [SerializeField] private AudioClip explosionSound;
    [Tooltip("Suena en cada impacto de bala (opcional).")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private int maxHealth = 2;
    [SerializeField] private int enemyDamage = 1;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private GameObject deathEffectPrefab;
    [Tooltip("Explosión animada en el lugar donde muere (opcional, además de deathEffectPrefab).")]
    [SerializeField] private GameObject explosionEffectPrefab;

    private int currentHealth;
    private PooledObject pooledObject;
    private HitFlash hitFlash;
    // Evita resolver dos veces el mismo enemigo (dos colliders del jugador o dos balas en un paso)
    private bool resolved;

    private void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
        hitFlash = GetComponent<HitFlash>();
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
        if (hitFlash != null) hitFlash.Flash();
        if (hitSound != null) AudioManager.instance.PlayAudio(hitSound);
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
        Vector3 center = CenterOfParts();
        PoolManager.Instance.Spawn(deathEffectPrefab, center);
        if (explosionEffectPrefab != null) PoolManager.Instance.Spawn(explosionEffectPrefab, center);
        pooledObject.Despawn();
    }

    // Centro visible del enemigo (el pivot de los prefabs no está en el medio del dibujo)
    private Vector3 CenterOfParts()
    {
        var parts = visibleParts ??= GetComponentsInChildren<SpriteRenderer>();
        if (parts.Length == 0) return transform.position;
        var b = parts[0].bounds;
        for (int i = 1; i < parts.Length; i++) b.Encapsulate(parts[i].bounds);
        return new Vector3(b.center.x, b.center.y, transform.position.z);
    }

    private SpriteRenderer[] visibleParts;
}
