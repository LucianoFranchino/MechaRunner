using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private AudioClip shootSound;
    [Tooltip("Boca del cañón. Si es hijo del brazo animado, la bala sale de donde está el cañón en la pose de disparo.")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject bulletSuperPrefab;

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
        // Dirección siempre horizontal hacia adelante: el brazo queda levemente inclinado
        // en la pose, pero el juego necesita disparos rectos.
        PoolManager.Instance.Spawn(bullet, firePoint.position, Quaternion.identity);
        AudioManager.instance.PlayAudio(shootSound);
    }
}
