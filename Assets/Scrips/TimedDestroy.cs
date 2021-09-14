using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimedDestroy : MonoBehaviour
{
    private void Start()
    {
        Destroy(gameObject, 0.33f);
    }
}
