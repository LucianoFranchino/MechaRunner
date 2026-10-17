using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Animator animator;
    [SerializeField] private string bulletPoolId = "Bullet";
    [SerializeField] private string bulletSuperPoolId = "BulletSuper";

    public void Shoot()
    {
        // El botón de disparo sigue recibiendo clicks con el juego en pausa/muerte
        if (Time.timeScale == 0f) return;

        bool superActive = PowerUpManager.Instance != null
            && PowerUpManager.Instance.IsActive(PowerUpType.SuperShoot);

        string poolId = superActive ? bulletSuperPoolId : bulletPoolId;
        PoolManager.Instance.Spawn(poolId, firePoint.position, firePoint.rotation);
        AudioManager.instance.PlayAudio(shootSound);

        if (animator != null)
            animator.Play("Fire");
    }
}
