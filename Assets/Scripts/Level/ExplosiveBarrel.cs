using UnityEngine;

/// <summary>
/// Barril explosivo: un disparo lo hace explotar. La explosión elimina enemigos y obstáculos cercanos
/// y suelta monedas. Si el Robot lo choca de frente es un obstáculo más (componente Obstacle).
/// Necesita tag "Enemy" para que las balas lo detecten.
/// </summary>
[RequireComponent(typeof(PooledObject))]
public class ExplosiveBarrel : MonoBehaviour, IDamageable, IPoolable
{
    [SerializeField] private float radius = 2.6f;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField, Min(0)] private int coins = 3;
    [SerializeField] private AudioClip explosionSound;

    private static readonly Collider2D[] hits = new Collider2D[16];
    private PooledObject pooled;
    private bool exploded;

    private void Awake() => pooled = GetComponent<PooledObject>();

    public void OnSpawn() => exploded = false;
    public void OnDespawn() { }

    public void TakeDamage(int damage)
    {
        if (exploded) return;
        exploded = true;
        Vector3 center = transform.position;

        var filter = new ContactFilter2D { useTriggers = true };
        int count = Physics2D.OverlapCircle(center, radius, filter, hits);
        for (int i = 0; i < count; i++)
        {
            var hit = hits[i];
            if (hit == null || hit.transform == transform) continue;
            var other = hit.GetComponentInParent<ExplosiveBarrel>();
            if (other != null) { other.TakeDamage(damage); continue; } // reacción en cadena
            var enemy = hit.GetComponentInParent<Enemy>();
            if (enemy != null) { enemy.TakeDamage(999); continue; }
            if (hit.GetComponentInParent<Obstacle>() != null)
            {
                var po = hit.GetComponentInParent<PooledObject>();
                if (po != null && po.isActiveAndEnabled)
                {
                    if (explosionPrefab != null) PoolManager.Instance.Spawn(explosionPrefab, po.transform.position);
                    po.Despawn();
                }
            }
        }

        if (explosionPrefab != null) PoolManager.Instance.Spawn(explosionPrefab, center);
        if (coinPrefab != null)
            for (int i = 0; i < coins; i++)
                PoolManager.Instance.Spawn(coinPrefab, center + new Vector3((i - (coins - 1) * 0.5f) * 0.7f, 1.2f + (i % 2) * 0.4f, 0f));
        if (AudioManager.instance != null) AudioManager.instance.PlayAudio(explosionSound);
        pooled.Despawn();
    }
}
