using UnityEngine;

/// <summary>
/// Único punto de entrada para el movimiento de todas las capas de parallax.
/// Lee la velocidad de DificultyManager una vez por frame y se la reparte
/// a cada ParallaxLayer, que la escala según su propio parallaxMultiplier.
/// </summary>
public class ParallaxController : MonoBehaviour
{
    [SerializeField] private ParallaxLayer[] layers;

    [Tooltip("Velocidad a usar si DificultyManager todavía no existe (ej. en el menú principal).")]
    [SerializeField] private float fallbackSpeed = 5f;

    private void Update()
    {
        float baseSpeed = DificultyManager.Instance != null
            ? DificultyManager.Instance.GameSpeed
            : fallbackSpeed;

        foreach (var layer in layers)
        {
            layer.Tick(baseSpeed);
        }
    }
}