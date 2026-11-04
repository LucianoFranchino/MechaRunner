using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private AudioClip shootSound;
    [Tooltip("Boca del cañón. Si es hijo del brazo animado, la bala sale de donde está el cañón en la pose de disparo.")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject bulletSuperPrefab;

    [Header("Game feel")]
    [Tooltip("Desvío máximo de cada bala en grados (recoil sutil, arriba o abajo).")]
    [SerializeField, Range(0f, 10f)] private float spreadDegrees = 1.5f;
    [Tooltip("Destello en la boca del cañón (opcional).")]
    [SerializeField] private MuzzleFlash muzzleFlash;

    private static readonly int FireState = Animator.StringToHash("Fire");

    public void Shoot()
    {
        // El botón de disparo sigue recibiendo clicks con el juego en pausa/muerte
        if (!GameManager.IsPlaying) return;

        if (animator != null)
        {
            // Aplica la pose de disparo en este mismo frame (brazo-cañón al frente)
            // para que la bala nazca en la boca del cañón y no donde estaba el brazo antes.
            animator.Play(FireState, 0, 0f);
            animator.Update(0f);
        }

        bool superActive = PowerUpManager.Instance != null
            && PowerUpManager.Instance.IsActive(PowerUpType.SuperShoot);

        GameObject bullet = superActive ? bulletSuperPrefab : bulletPrefab;
        // Hacia adelante con un desvío leve al azar: los disparos se ven menos mecánicos
        // (el brazo queda inclinado en la pose, por eso no se usa su rotación).
        float angle = Random.Range(-spreadDegrees, spreadDegrees);
        PoolManager.Instance.Spawn(bullet, firePoint.position, Quaternion.Euler(0f, 0f, angle));
        AudioManager.instance.PlayShot(shootSound);
        if (muzzleFlash != null) muzzleFlash.Play(superActive);
    }
}
