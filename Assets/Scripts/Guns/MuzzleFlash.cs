using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Destello en la boca del cañón al disparar: un estallido de luz que se apaga en unos fotogramas y una
/// luz 2D breve que ilumina alrededor. Con SUPER SHOT es más grande, más intenso y dura un poco más.
/// Rotación y tamaño varían en cada disparo. Sin basura de memoria.
/// </summary>
public class MuzzleFlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer flash;
    [SerializeField] private Light2D glow;

    [Header("Normal")]
    [SerializeField] private float duration = 0.06f;
    [SerializeField] private float scale = 1f;
    [SerializeField] private Color color = new(1f, 0.92f, 0.55f, 1f);
    [SerializeField] private float lightIntensity = 1.2f;
    [SerializeField] private float lightRadius = 1.2f;

    [Header("Super Shot")]
    [SerializeField] private float superDuration = 0.1f;
    [SerializeField] private float superScale = 1.8f;
    [SerializeField] private Color superColor = new(1f, 0.55f, 0.2f, 1f);
    [SerializeField] private float superLightIntensity = 2.6f;
    [SerializeField] private float superLightRadius = 2.4f;

    private float until;
    private float length;
    private float peakIntensity;
    private Color peakColor;
    private Vector3 baseScale;
    private bool showing;

    public bool IsShowing => showing;

    private void Awake()
    {
        baseScale = transform.localScale;
        Hide();
    }

    public void Play(bool super)
    {
        length = super ? superDuration : duration;
        until = Time.time + length;
        peakColor = super ? superColor : color;
        peakIntensity = super ? superLightIntensity : lightIntensity;

        float s = (super ? superScale : scale) * Random.Range(0.85f, 1.15f);
        transform.localScale = baseScale * s;
        transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        if (flash != null) { flash.enabled = true; flash.color = peakColor; }
        if (glow != null)
        {
            glow.enabled = true;
            glow.color = peakColor;
            glow.intensity = peakIntensity;
            glow.pointLightOuterRadius = super ? superLightRadius : lightRadius;
        }
        showing = true;
    }

    private void Update()
    {
        if (!showing) return;
        float k = (until - Time.time) / Mathf.Max(0.0001f, length);
        if (k <= 0f) { Hide(); return; }
        if (flash != null) { var c = peakColor; c.a = k; flash.color = c; }
        if (glow != null) glow.intensity = peakIntensity * k;
    }

    private void Hide()
    {
        showing = false;
        if (flash != null) flash.enabled = false;
        if (glow != null) glow.enabled = false;
    }
}
