using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private AudioClip shootSound;
    public Transform firePoint;
    public Animator animator;
    public GameObject bullet, bulletSuper;
    public bool superFire;

    public void Shoot()
    {
        Instantiate(superFire ? bulletSuper : bullet, firePoint.position, firePoint.rotation);
        AudioManager.instance.PlayAudio(shootSound);
        superFire = false;
        animator.Play("Fire");
    }
}
