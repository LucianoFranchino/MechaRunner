using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public float speed;
    public int obstacleDamage = 2;
   // private Shake shake;


    void Start()
    {
        //shake = GameObject.FindGameObjectWithTag("ScreenShake").GetComponent<Shake>();
    }

    private void Update()
    {
        transform.Translate(Vector2.left * Time.deltaTime * speed);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        
            if (collision.CompareTag("Player"))
            {
                collision.GetComponent<Player>().EnemyDamage(obstacleDamage);
            
                
            }
    }
}
