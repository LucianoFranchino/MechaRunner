using UnityEngine;

/// <summary>Brillo pulsante del láser (solo visual).</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class LaserPulse : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.6f;

    private SpriteRenderer view;
    private Color baseColor;

    private void Awake()
    {
        view = GetComponent<SpriteRenderer>();
        baseColor = view.color;
    }

    private void Update()
    {
        var c = baseColor;
        c.a = baseColor.a * Mathf.Lerp(minAlpha, 1f, 0.5f + 0.5f * Mathf.Sin(Time.time * speed));
        view.color = c;
    }
}
