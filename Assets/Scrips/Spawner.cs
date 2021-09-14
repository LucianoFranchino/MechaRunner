using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public float delay, destroyDelay, speed, difficultyIncrease;
    public int limit;
    public GameObject[] obstacles;
    public ParalaxBG pbg, pbga;
    private int delta = 0;

    private void Start()
    {
        StartCoroutine(Spawn());
    }

    public IEnumerator Spawn()
    {
        GameObject prefab = obstacles[Random.Range(0, obstacles.Length)];
        Vector2 pos = new Vector2(transform.position.x, prefab.transform.position.y);
        GameObject x = Instantiate<GameObject>(prefab, pos, prefab.transform.rotation);
        x.GetComponent<Obstacle>().speed = speed;
        Destroy(x, destroyDelay);
        yield return new WaitForSeconds(delay);

        if (delta < limit)
        {
            speed += difficultyIncrease;
            pbg.speedBG += difficultyIncrease;
            pbga.speedBG += difficultyIncrease;
            delta++;
        }

        StartCoroutine(Spawn());
    }
}
