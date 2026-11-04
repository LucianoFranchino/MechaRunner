using UnityEngine;

/// <summary>
/// Destello de golpe: durante un instante todas las partes del enemigo se pintan de blanco plano
/// (material MechaRunner/SpriteFlash) y después vuelven a su material. Sin basura de memoria.
/// </summary>
public class HitFlash : MonoBehaviour
{
    [SerializeField] private Material flashMaterial;
    [SerializeField] private float duration = 0.08f;

    private SpriteRenderer[] parts;
    private Material[] originals;
    private float flashUntil;
    private bool flashing;

    public bool IsFlashing => flashing;

    private void Awake()
    {
        parts = GetComponentsInChildren<SpriteRenderer>(true);
        originals = new Material[parts.Length];
        for (int i = 0; i < parts.Length; i++) originals[i] = parts[i].sharedMaterial;
    }

    public void Flash()
    {
        if (flashMaterial == null) return;
        if (!flashing)
        {
            for (int i = 0; i < parts.Length; i++) parts[i].sharedMaterial = flashMaterial;
            flashing = true;
        }
        flashUntil = Time.time + duration;
    }

    private void Update()
    {
        if (flashing && Time.time >= flashUntil) Restore();
    }

    // Vuelve al pool sin quedar blanco
    private void OnDisable()
    {
        if (flashing) Restore();
    }

    private void Restore()
    {
        for (int i = 0; i < parts.Length; i++) parts[i].sharedMaterial = originals[i];
        flashing = false;
    }
}
