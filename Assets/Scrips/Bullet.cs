using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 20f;
    public Rigidbody2D rb;
    public int damage = 2;

    // Start is called before the first frame update
    void Start()
    {
        rb.velocity = transform.right * speed;
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        if (hitInfo.gameObject.CompareTag("Enemy"))
        {
            hitInfo.GetComponent<Enemy>().TakeDamage(damage);
            Destroy(gameObject);
        }
        if (hitInfo.gameObject.CompareTag("Obstacule"))
        {
            Destroy(gameObject);
        }
    }

}
