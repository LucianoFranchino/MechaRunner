using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public GameObject shootSound;
    public Transform firePoint;
    public Animator animator;
    public GameObject bullet, bulletSuper;
    public bool superFire;

    public void Shoot()
    {
        Instantiate(superFire ? bulletSuper : bullet, firePoint.position, firePoint.rotation);
        Instantiate(shootSound, transform.position, Quaternion.identity);
        superFire = false;
        animator.Play("Fire");
    }
}
