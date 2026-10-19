using UnityEngine;

/// <summary>
/// Columnas que sostienen una plataforma desde abajo hasta el piso (dos patas en cada extremo).
/// Son decorado: no tienen colisión y se dibujan detrás del Robot. El largo sale de la altura de
/// la plataforma, así sirven para cualquier sección. Se recalculan solo si la plataforma cambia de
/// escala o de altura (no por frame).
/// </summary>
[ExecuteAlways]
public class SupportColumns : MonoBehaviour
{
    [SerializeField] private Sprite sprite;
    [SerializeField] private Color color = new(0.24f, 0.27f, 0.32f);
    [Tooltip("Altura del piso en el mundo (borde superior del suelo).")]
    [SerializeField] private float groundY = -0.704f;
    [SerializeField] private float legWidth = 0.07f;
    [Tooltip("Distancia de cada pata al borde de la plataforma (dos patas por lado).")]
    [SerializeField] private Vector2 insets = new(0.1f, 0.24f);
    [SerializeField] private int sortingOrder = -3;

    private readonly Transform[] legs = new Transform[4];
    private Vector3 appliedScale = new(float.NaN, 0f, 0f);
    private float appliedY = float.NaN;

    private void OnEnable()
    {
        EnsureLegs();
        Apply();
    }

    private void LateUpdate()
    {
        if (transform.lossyScale != appliedScale || !Mathf.Approximately(transform.position.y, appliedY)) Apply();
    }

    private void EnsureLegs()
    {
        for (int i = 0; i < legs.Length; i++)
        {
            if (legs[i] != null) continue;
            var existing = transform.Find("Leg" + i);
            if (existing == null)
            {
                var go = new GameObject("Leg" + i);
                go.hideFlags = HideFlags.DontSave; // se generan solas: no se guardan en el prefab
                go.transform.SetParent(transform, false);
                var r = go.AddComponent<SpriteRenderer>();
                existing = go.transform;
            }
            legs[i] = existing;
            var view = legs[i].GetComponent<SpriteRenderer>();
            view.sprite = sprite;
            view.color = color;
            view.sortingOrder = sortingOrder;
        }
    }

    public void Apply()
    {
        Vector3 size = transform.lossyScale;
        appliedScale = size;
        appliedY = transform.position.y;
        if (size.x == 0f || size.y == 0f) return;

        float bottom = transform.position.y - Mathf.Abs(size.y) * 0.5f;
        float height = Mathf.Max(0f, bottom - groundY);
        float half = Mathf.Abs(size.x) * 0.5f;
        for (int i = 0; i < legs.Length; i++)
        {
            if (legs[i] == null) continue;
            float side = i < 2 ? -1f : 1f;
            float inset = (i % 2 == 0) ? insets.x : insets.y;
            float worldX = side * (half - inset);
            float worldY = bottom - height * 0.5f - transform.position.y;
            legs[i].localPosition = new Vector3(worldX / size.x, worldY / size.y, 0f);
            legs[i].localScale = new Vector3(legWidth / size.x, height / size.y, 1f);
            legs[i].gameObject.SetActive(height > 0.01f);
        }
    }
}
