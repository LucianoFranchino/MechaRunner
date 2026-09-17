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
        bool superActive = PowerUpManager.Instance != null
            && PowerUpManager.Instance.IsActive(PowerUpType.SuperShoot);

        string poolId = superActive ? bulletSuperPoolId : bulletPoolId;
        PoolManager.Instance.Spawn(poolId, firePoint.position, firePoint.rotation);
        AudioManager.instance.PlayAudio(shootSound);
        animator.Play("Fire");
    }
}