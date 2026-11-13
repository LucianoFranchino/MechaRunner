using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Destello de luz breve para efectos pooleados (explosiones): al aparecer arranca a toda intensidad
/// y se apaga en unos instantes. Sin basura de memoria.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class PooledLightFlash : MonoBehaviour, IPoolable
{
    [SerializeField] private float intensity = 3f;
    [SerializeField] private float duration = 0.3f;

    private Light2D glow;
    private float startTime;

    private void Awake() => glow = GetComponent<Light2D>();

    public void OnSpawn()
    {
        startTime = Time.time;
        glow.intensity = intensity;
        glow.enabled = true;
    }

    public void OnDespawn() => glow.enabled = false;

    private void Update()
    {
        if (!glow.enabled) return;
        float k = 1f - (Time.time - startTime) / duration;
        if (k <= 0f) { glow.enabled = false; return; }
        glow.intensity = intensity * k * k;
    }
}
