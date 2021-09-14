using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResolutionRatio : MonoBehaviour
{

    public float aspect;
    public float rounded;
    UnityEngine.UI.CanvasScaler cv;
    void Start()
    {
        cv = this.GetComponent<UnityEngine.UI.CanvasScaler>();
        UpdateResolution();
    }

    void UpdateResolution()
    {
        aspect = Camera.main.aspect;
        rounded = (int)(aspect * 100.0f) / 100.0f;

        if(rounded == 1.65f || rounded == 1.66f || rounded == 1.57f)
            Addratios (0, 5.64f);
        else if(rounded == 2.04f || rounded == 2.05f || rounded == 2.06f)
            Addratios (0.88f, 4.86f);
        else if(rounded == 1.70f || rounded == 1.71f || rounded == 1.69f)
            Addratios (0f, 5.21f);
        else if(rounded == 1.33f || rounded == 1.32f || rounded == 1.34f)
            Addratios (0f, 6.77f);
        else if(rounded == 1.85f )
            Addratios (1, 5);
        else if(rounded == 2.11f)
            Addratios (1f, 4.50f);
        else
            Addratios (0, 5);
        
    }

    void Addratios(float m, float sz )
    {
        if(cv!=null)cv.matchWidthOrHeight = m;
        Camera.main.orthographicSize = sz;
    }
    
}
