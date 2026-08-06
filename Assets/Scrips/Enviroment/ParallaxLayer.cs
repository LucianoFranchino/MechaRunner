using UnityEngine;
using MechaRunner.Environment;

/// <summary>
/// Una capa de parallax genérica: sirve para un fondo con sprites repetidos
/// o para un grupo de props (como las columnas) que scrollea en loop.
/// No define su propia velocidad: la recibe de ParallaxController en cada Tick,
/// así todas las capas quedan sincronizadas entre sí y con DificultyManager.
/// </summary>
public class ParallaxLayer : MonoBehaviour
{
    [Header("Segmentos")]
    [Tooltip("Los objetos que forman esta capa, ya ubicados uno al lado del otro en la escena (mínimo 2, para que el loop no se note).")]
    [SerializeField] private Transform[] segments;

    [Tooltip("Distancia horizontal entre el origen de un segmento y el siguiente.")]
    [SerializeField] private float segmentWidth = 10f;

    [Tooltip("Posición X (mundo) a partir de la cual un segmento se recicla al final de la fila. Debe quedar fuera de cámara por la izquierda.")]
    [SerializeField] private float recycleX = -12f;

    [Header("Profundidad")]
    [Range(0f, 1f)]
    [Tooltip("0 = capa fija/muy lejana, 1 = se mueve a la misma velocidad que los obstáculos.")]
    [SerializeField] private float parallaxMultiplier = 0.5f;

    private ParallaxLoopState loopState;

    private void Awake()
    {
        float[] initialPositions = new float[segments.Length];
        for (int i = 0; i < segments.Length; i++)
        {
            initialPositions[i] = segments[i].position.x;
        }

        loopState = new ParallaxLoopState(initialPositions, segmentWidth, recycleX);
    }

    /// <summary>
    /// Llamado por ParallaxController con la velocidad base del juego ya resuelta.
    /// </summary>
    public void Tick(float baseSpeed)
    {
        float movement = baseSpeed * parallaxMultiplier * Time.deltaTime;
        loopState.Advance(movement);

        for (int i = 0; i < segments.Length; i++)
        {
            Vector3 pos = segments[i].position;
            pos.x = loopState.SegmentX[i];
            segments[i].position = pos;
        }
    }
}