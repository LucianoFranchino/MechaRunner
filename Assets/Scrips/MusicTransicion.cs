using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicTransicion : MonoBehaviour
{
    private static MusicTransicion instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(instance);
        }else{
            Destroy(gameObject);
        }
    }    
}
