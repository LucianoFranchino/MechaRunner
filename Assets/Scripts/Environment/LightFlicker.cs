using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Tubo de luz a punto de quemarse: la mayor parte del tiempo queda fijo y, cada tantos segundos,
/// titila en una ráfaga corta (intensidad y haz volumétrico). Sin basura de memoria.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class LightFlicker : MonoBehaviour
{
    [Tooltip("Segundos de luz estable entre ráfagas (mínimo, máximo).")]
    [SerializeField] private Vector2 calmSeconds = new(12f, 30f);
    [Tooltip("Duración de cada ráfaga de parpadeo (mínimo, máximo).")]
    [SerializeField] private Vector2 burstSeconds = new(0.2f, 0.7f);
    [Tooltip("Intensidad más baja durante el parpadeo, relativa a la normal.")]
    [SerializeField, Range(0f, 1f)] private float minFactor = 0.1f;
    [Tooltip("Cambios de intensidad por segundo durante la ráfaga.")]
    [SerializeField] private float flickerRate = 16f;

    private Light2D target;
    private float baseIntensity;
    private float baseVolume;
    private bool inBurst;
    private float nextEvent;
    private float nextChange;

    private void Awake()
    {
        target = GetComponent<Light2D>();
        baseIntensity = target.intensity;
        baseVolume = target.volumeIntensity;
        nextEvent = Time.time + Random.Range(calmSeconds.x, calmSeconds.y);
    }

    private void Update()
    {
        float t = Time.time;
        if (!inBurst)
        {
            if (t < nextEvent) return;
            inBurst = true;
            nextEvent = t + Random.Range(burstSeconds.x, burstSeconds.y);
            nextChange = t;
        }

        if (t >= nextEvent)
        {
            inBurst = false;
            Set(1f);
            nextEvent = t + Random.Range(calmSeconds.x, calmSeconds.y);
            return;
        }

        if (t >= nextChange)
        {
            Set(Random.value < 0.5f ? Random.Range(minFactor, 0.5f) : 1f);
            nextChange = t + 1f / flickerRate;
        }
    }

    private void Set(float factor)
    {
        target.intensity = baseIntensity * factor;
        target.volumeIntensity = baseVolume * factor;
    }
}
