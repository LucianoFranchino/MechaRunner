using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;

public class Enemy : MonoBehaviour
{

    [SerializeField]
    private CoinScrip coinM;
    public GameObject explocionSound;
    public int enemyHealth = 2;
    public GameObject deathEfect;
    private float destroyTime =1;
    public int enemyDamage = 1;
    public GameObject coin;

    public void TakeDamage(int damage)
    {
        enemyHealth -= damage;
        if(enemyHealth <= 0)
        {
            Instantiate(explocionSound, transform.position, Quaternion.identity);
            Instantiate(deathEfect, transform.position, Quaternion.identity);
            Die();
        }
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<Player>().EnemyDamage(enemyDamage);
            
            Destroy(gameObject);
        }
    }
    void DestroyObjectDelayed()
    {
        // Kills the game object in 5 seconds after loading the object
        Destroy(deathEfect, 1);
    }
    void Die()
    {
        Instantiate(coin, transform.position, Quaternion.identity);
        Instantiate(deathEfect, transform.position, Quaternion.identity);
        //coinM = FindObjectOfType<CoinScrip>();
        Destroy(gameObject);
        //Destroy(deathEfect); 
    }
    
}
