using UnityEngine;
using UnityEngine.Rendering.Universal;

public class AlarmLights : MonoBehaviour
{
    private Light2D alarmLight;
    public float minIntensity = 0.5f;
    public float maxIntensity = 3.0f;
    public float speed = 5.0f;

    void Start()
    {
        alarmLight = GetComponent<Light2D>();
    }

    void Update()
    {
        // Crea un efecto de parpadeo suave usando una onda senoidal
        float noise = Mathf.PingPong(Time.time * speed, 1f);
        alarmLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}
