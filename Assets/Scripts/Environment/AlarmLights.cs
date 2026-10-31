using UnityEngine;
using UnityEngine.Rendering.Universal;

public class AlarmLights : MonoBehaviour
{
    private Light2D alarmLight;
    public float minIntensity = 0.5f;
    public float maxIntensity = 3.0f;
    public float speed = 5.0f;

    [Tooltip("Multiplicador de la intensidad máxima por zona (índice 0 = zona 1). Vacío = sin cambios por zona.")]
    public float[] zoneIntensity = new float[0];
    [Tooltip("Multiplicador de la velocidad del pulso por zona (índice 0 = zona 1). Vacío = sin cambios por zona.")]
    public float[] zoneSpeed = new float[0];

    void Start()
    {
        alarmLight = GetComponent<Light2D>();
    }

    void Update()
    {
        // La alarma se intensifica en las zonas difíciles (si está configurada por zona)
        int zone = DifficultyManager.Instance != null ? DifficultyManager.Instance.ZoneIndex : 0;
        float intensityScale = ForZone(zoneIntensity, zone);
        float speedScale = ForZone(zoneSpeed, zone);

        // Crea un efecto de parpadeo suave usando una onda senoidal
        float noise = Mathf.PingPong(Time.time * speed * speedScale, 1f);
        alarmLight.intensity = Mathf.Lerp(minIntensity, maxIntensity * intensityScale, noise);
    }

    private static float ForZone(float[] values, int zone)
    {
        if (values == null || values.Length == 0) return 1f;
        return values[Mathf.Clamp(zone, 0, values.Length - 1)];
    }
}
