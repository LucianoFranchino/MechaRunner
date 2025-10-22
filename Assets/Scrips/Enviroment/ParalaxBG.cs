using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParalaxBG : MonoBehaviour
{
    public float speedBG;
    [SerializeField] private float endX;
    [SerializeField] private float startX;

    void Update()
    {
        transform.Translate(Vector2.left * speedBG * Time.deltaTime);

        if(transform.position.x <= endX)
        {
            Vector2 pos = new Vector2(startX, transform.position.y);
            transform.position = pos;
        }

    }
}
