using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class spawer_2 : MonoBehaviour
{
    public GameObject obstacle;

    private float timeBtwSpawn;
    public float starTime, decreaseTime;
    public float minTime = 0.65f;
    [Header("Min - Max")]
    public float min; public float max;

    void Start()
    {
        timeBtwSpawn = Random.Range(min, max);
    }

    private void Update()
    {
        if(timeBtwSpawn <= 0)
        {
            Instantiate(obstacle, transform.position, Quaternion.identity);
            timeBtwSpawn = Random.Range(min, max);
            if(starTime > minTime)
            {
                starTime -= decreaseTime;
            }
            
        }
        else
        {
            timeBtwSpawn -= Time.deltaTime;
        }
    }
    
}
