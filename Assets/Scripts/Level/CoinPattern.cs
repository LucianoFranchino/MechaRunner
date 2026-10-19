using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Grupo de monedas dentro de una LevelSection. Los arcos salen del modelo real del salto (JumpModel):
/// si el jugador salta en la posición del patrón, las agarra todas. En la vista de escena se dibujan
/// como círculos amarillos; al guardar la sección se convierten en monedas.
/// </summary>
public class CoinPattern : MonoBehaviour
{
    public enum Shape { Line, JumpArc, DoubleJumpArc, Wave }

    [SerializeField] private Shape shape = Shape.Line;
    [SerializeField] private GameObject coinPrefab;
    [SerializeField, Min(1)] private int count = 6;
    [Tooltip("Separación entre monedas (Line y Wave).")]
    [SerializeField] private float spacing = 0.8f;
    [Tooltip("Altura de la onda (Wave).")]
    [SerializeField] private float waveHeight = 0.6f;
    [Tooltip("Largo de una onda completa (Wave).")]
    [SerializeField] private float waveLength = 4f;
    [Tooltip("Arcos: altura de las monedas sobre los pies del Robot (≈ centro del cuerpo).")]
    [SerializeField] private float bodyOffset = 0.65f;

    public GameObject CoinPrefab => coinPrefab;

    /// <summary>Posiciones relativas al patrón. Arcos: el patrón marca el punto de despegue (pies en el piso).</summary>
    public IEnumerable<Vector2> LocalPoints()
    {
        var jump = JumpModel.Default;
        switch (shape)
        {
            case Shape.Line:
                for (int i = 0; i < count; i++) yield return new Vector2(i * spacing, 0f);
                break;
            case Shape.Wave:
                for (int i = 0; i < count; i++)
                {
                    float x = i * spacing;
                    yield return new Vector2(x, Mathf.Sin(x / waveLength * Mathf.PI * 2f) * waveHeight);
                }
                break;
            case Shape.JumpArc:
            case Shape.DoubleJumpArc:
                float doubleAt = shape == Shape.DoubleJumpArc ? jump.ApexTime() : -1f;
                float air = jump.AirTime(doubleAt);
                for (int i = 0; i < count; i++)
                {
                    // Repartidas en el tiempo de vuelo, sin la primera y última (en el piso)
                    float t = air * (i + 1) / (count + 1);
                    yield return new Vector2(t * LevelSection.ReferenceSpeed, jump.HeightAt(t, doubleAt) + bodyOffset);
                }
                break;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.9f);
        var parent = transform.parent;
        foreach (var p in LocalPoints())
        {
            Vector3 local = transform.localPosition + (Vector3)p;
            Gizmos.DrawWireSphere(parent != null ? parent.TransformPoint(local) : local, 0.22f);
        }
    }
}
