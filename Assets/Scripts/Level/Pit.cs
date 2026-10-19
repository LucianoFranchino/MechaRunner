using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hueco en el piso. Se mueve con el scroll como cualquier objeto de sección; su ancho es el de su
/// SpriteRenderer (escala con la velocidad vía StretchWithSpeed). LevelRules consulta si hay un hueco
/// bajo el Robot para dejarlo caer.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Pit : MonoBehaviour
{
    private static readonly List<Pit> active = new();

    private SpriteRenderer view;

    private void Awake() => view = GetComponent<SpriteRenderer>();
    private void OnEnable() => active.Add(this);
    private void OnDisable() => active.Remove(this);

    /// <summary>Algún hueco activo cubre la x dada, con un margen hacia adentro de cada borde.</summary>
    public static bool Covers(float x, float margin)
    {
        for (int i = 0; i < active.Count; i++)
        {
            var b = active[i].view.bounds;
            if (x > b.min.x + margin && x < b.max.x - margin) return true;
        }
        return false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => active.Clear();
}
