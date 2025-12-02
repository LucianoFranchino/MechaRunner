using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour, IPooleable
{
    [SerializeField] private float movSpeed;
    [SerializeField] private int obstacleDamage = 3;
    [SerializeField] private float despawnDistanceX = -15f;

    private float currentSpeed;

    private void Awake()
    {
        currentSpeed = movSpeed;
    }

    private void Update()
    {
        transform.Translate(Vector2.left * Time.deltaTime * currentSpeed);

        if(transform.position.x <= despawnDistanceX)
        {
            GetComponent<PooledObject>().Despawn();
        }
    }
    public void SetSpeed(float speed)
    {
        currentSpeed = speed;
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<PlayerHealth>().PlayerDamage(obstacleDamage);
        }
    }

    public void OnSpawn()
    {
        Debug.Log($"{gameObject.name} spawned!");
        if (DificultyManager.Instance != null)
        {
            currentSpeed = DificultyManager.Instance.GameSpeed;
        }
        else
        {
            currentSpeed = movSpeed;
        }
    }

    public void OnDespawn()
    {
        Debug.Log($"{gameObject.name} despawned!");
        currentSpeed = movSpeed;
    }
}
