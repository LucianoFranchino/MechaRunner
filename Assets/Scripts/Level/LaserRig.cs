using UnityEngine;

/// <summary>
/// Visual del láser bajo: emisores y cadenas que no se deforman cuando el láser se estira con la velocidad,
/// cadenas colgadas hasta el techo, y arcos eléctricos en zigzag que se regeneran varias veces por segundo.
/// No reserva memoria por frame (reusa los arrays de puntos).
/// </summary>
[ExecuteAlways]
public class LaserRig : MonoBehaviour
{
    [Tooltip("Hijos que conservan su ancho en el mundo aunque el láser se estire (emisores, cadenas, chispas).")]
    [SerializeField] private Transform[] keepWidth;
    [Tooltip("Cadenas (SpriteRenderer en modo Tiled) que van desde su posición hasta el techo.")]
    [SerializeField] private SpriteRenderer[] chains;
    [SerializeField] private float ceilingY = 5.4f;
    [Tooltip("Arcos eléctricos a lo largo del rayo (coordenadas locales, de -halfLength a +halfLength).")]
    [SerializeField] private LineRenderer[] arcs;
    [SerializeField] private float halfLength = 2f;
    [SerializeField] private float arcAmplitude = 0.09f;
    [SerializeField] private float arcRefresh = 0.05f;

    private float[] baseWidths;
    private Vector3 appliedScale = new(float.NaN, 0f, 0f);
    private float appliedY = float.NaN;
    private Vector3[] points;
    private float nextArc;

    private void OnEnable()
    {
        if (keepWidth != null && baseWidths == null)
        {
            baseWidths = new float[keepWidth.Length];
            for (int i = 0; i < keepWidth.Length; i++) baseWidths[i] = keepWidth[i] != null ? keepWidth[i].localScale.x : 1f;
        }
        Apply();
    }

    private void LateUpdate()
    {
        if (transform.lossyScale != appliedScale || !Mathf.Approximately(transform.position.y, appliedY)) Apply();
        if (Application.isPlaying && Time.time >= nextArc)
        {
            nextArc = Time.time + arcRefresh;
            RegenerateArcs();
        }
    }

    private void Apply()
    {
        appliedScale = transform.lossyScale;
        appliedY = transform.position.y;
        float sx = appliedScale.x == 0f ? 1f : appliedScale.x;

        if (keepWidth != null && baseWidths != null)
            for (int i = 0; i < keepWidth.Length; i++)
            {
                if (keepWidth[i] == null) continue;
                var s = keepWidth[i].localScale;
                s.x = baseWidths[i] / sx;
                keepWidth[i].localScale = s;
            }

        if (chains != null)
            foreach (var chain in chains)
            {
                if (chain == null) continue;
                float worldScaleY = Mathf.Abs(chain.transform.lossyScale.y);
                if (worldScaleY <= 0f) continue;
                chain.drawMode = SpriteDrawMode.Tiled;
                float length = Mathf.Max(0f, ceilingY - chain.transform.position.y);
                chain.size = new Vector2(chain.size.x > 0f ? chain.size.x : chain.sprite.bounds.size.x, length / worldScaleY);
            }
    }

    private void RegenerateArcs()
    {
        if (arcs == null) return;
        foreach (var arc in arcs)
        {
            if (arc == null) continue;
            int n = arc.positionCount;
            if (points == null || points.Length < n) points = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                float t = n > 1 ? i / (float)(n - 1) : 0f;
                // Los extremos quedan pegados a los emisores; el medio vibra
                float y = (i == 0 || i == n - 1) ? 0f : Random.Range(-arcAmplitude, arcAmplitude);
                points[i] = new Vector3(Mathf.Lerp(-halfLength, halfLength, t), y, 0f);
            }
            arc.SetPositions(points);
        }
    }
}
