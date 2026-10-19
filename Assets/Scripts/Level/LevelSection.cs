using System;
using System.Collections.Generic;
using UnityEngine;

public enum SectionIntensity { Breather, Normal, Peak }

/// <summary>Un objeto de una sección: qué prefab spawnear y dónde, relativo al inicio de la sección.</summary>
[Serializable]
public struct SectionElement
{
    public GameObject prefab;
    [Tooltip("x: distancia desde el inicio de la sección (a la velocidad de referencia). y: altura sobre el piso.")]
    public Vector2 localPosition;
    public Vector3 localScale;
    [Tooltip("El ancho escala con la velocidad (huecos, plataformas): lo que se salta en el mismo tiempo.")]
    public bool stretchWithSpeed;
    [Tooltip("Distancia del pivot al borde izquierdo del dibujo: el objeto aparece cuando ese borde llega al punto de spawn.")]
    public float leadingEdge;
}

/// <summary>
/// Sección de nivel diseñada a mano. Se arma como prefab: la raíz está en el piso (y = 0 es la superficie)
/// y en x = 0 empieza la sección; cada hijo es una instancia del prefab a spawnear (caja, moneda, enemigo...)
/// o un CoinPattern. Al guardar el prefab se "hornea" la lista de elementos que usa el LevelDirector.
/// Las distancias se diseñan a la velocidad de referencia (6): a más velocidad todo se separa en
/// proporción, así los tiempos de salto quedan iguales y la sección es justa a cualquier velocidad.
/// </summary>
public class LevelSection : MonoBehaviour
{
    public const float ReferenceSpeed = 6f;

    [SerializeField] private SectionIntensity intensity = SectionIntensity.Normal;
    [Tooltip("Primera zona (1 = Calentamiento) en la que puede aparecer.")]
    [SerializeField, Min(1)] private int minZone = 1;
    [SerializeField, Min(1)] private int maxZone = 99;
    [SerializeField, Min(1)] private int weight = 1;
    [Tooltip("Presenta un peligro nuevo: el director la muestra primero, una vez, cuando se habilita su zona.")]
    [SerializeField] private bool introducesThreat;
    [Tooltip("Largo de la sección (a la velocidad de referencia).")]
    [SerializeField, Min(1f)] private float length = 12f;

    [SerializeField] private SectionElement[] elements = Array.Empty<SectionElement>();

    public SectionIntensity Intensity => intensity;
    public int MinZone => minZone;
    public int MaxZone => maxZone;
    public int Weight => weight;
    public bool IntroducesThreat => introducesThreat;
    public float Length => length;
    public IReadOnlyList<SectionElement> Elements => elements;

    public bool AllowedInZone(int zone) => zone >= minZone && zone <= maxZone;

#if UNITY_EDITOR
    /// <summary>Recorre los hijos y arma la lista de elementos. Lo llama SectionBaker al guardar el prefab.</summary>
    public void Bake()
    {
        var list = new List<SectionElement>();
        foreach (Transform child in transform)
        {
            if (!child.gameObject.activeSelf) continue;
            var pattern = child.GetComponent<CoinPattern>();
            if (pattern != null)
            {
                foreach (var p in pattern.LocalPoints())
                    list.Add(new SectionElement
                    {
                        prefab = pattern.CoinPrefab,
                        localPosition = (Vector2)child.localPosition + p,
                        localScale = pattern.CoinPrefab != null ? pattern.CoinPrefab.transform.localScale : Vector3.one,
                        leadingEdge = 0.3f,
                    });
                continue;
            }

            // El prefab más cercano (si es una variante, la variante y no su base)
            var source = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(child.gameObject);
            if (source == null || source == child.gameObject) continue; // solo instancias de prefabs
            list.Add(new SectionElement
            {
                prefab = source,
                localPosition = child.localPosition,
                localScale = child.localScale,
                stretchWithSpeed = source.GetComponent<StretchWithSpeed>() != null,
                leadingEdge = LeadingEdge(child),
            });
        }
        list.Sort((a, b) => a.localPosition.x.CompareTo(b.localPosition.x));
        elements = list.ToArray();
    }

    /// <summary>Distancia (en unidades de la sección) del pivot del hijo al borde izquierdo de sus sprites.</summary>
    private float LeadingEdge(Transform child)
    {
        float min = child.position.x;
        foreach (var r in child.GetComponentsInChildren<Renderer>()) min = Mathf.Min(min, r.bounds.min.x);
        return (child.position.x - min) / Mathf.Max(0.0001f, transform.lossyScale.x);
    }

    private void OnDrawGizmos()
    {
        var m = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        var jump = JumpModel.Default;
        float single = jump.HeightAt(jump.ApexTime());
        // Piso, altura del Robot parado y alturas máximas de salto (pies) a la velocidad de referencia
        Gizmos.color = Color.white; Gizmos.DrawLine(Vector3.zero, new Vector3(length, 0f));
        Gizmos.color = new Color(1f, 0.4f, 0.3f, 0.8f); Gizmos.DrawLine(new Vector3(0, 1.34f), new Vector3(length, 1.34f));
        Gizmos.color = new Color(0.4f, 0.9f, 1f, 0.6f); Gizmos.DrawLine(new Vector3(0, single), new Vector3(length, single));
        Gizmos.color = new Color(0.4f, 0.6f, 1f, 0.4f); Gizmos.DrawLine(new Vector3(0, single * 2f), new Vector3(length, single * 2f));
        Gizmos.color = Color.yellow; Gizmos.DrawLine(new Vector3(length, 0f), new Vector3(length, 3.5f));
        Gizmos.matrix = m;
    }
#endif
}
