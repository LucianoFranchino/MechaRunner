using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private Transform firePoint;
    [SerializeField] private Animator animator;
    [SerializeField] private string bulletPoolId = "Bullet";
    [SerializeField] private string bulletSuperPoolId = "BulletSuper";

    public bool superFire;

    public void Shoot()
    {
        string poolId = superFire ? bulletSuperPoolId : bulletPoolId;
        PoolManager.Instance.Spawn(poolId, firePoint.position, firePoint.rotation);
        AudioManager.instance.PlayAudio(shootSound);
        superFire = false;
        animator.Play("Fire");
    }
}