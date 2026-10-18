using UnityEngine;

/// <summary>
/// Único punto de entrada para el movimiento de todas las capas de parallax.
/// Lee la velocidad de DifficultyManager una vez por frame y se la reparte
/// a cada ParallaxLayer, que la escala según su propio parallaxMultiplier.
/// </summary>
public class ParallaxController : MonoBehaviour
{
    [SerializeField] private ParallaxLayer[] layers;

    [Tooltip("Velocidad a usar si DifficultyManager todavía no existe (ej. en el menú principal).")]
    [SerializeField] private float fallbackSpeed = 5f;

    private void Update()
    {
        float baseSpeed = DifficultyManager.Instance != null
            ? DifficultyManager.Instance.GameSpeed
            : fallbackSpeed;

        foreach (var layer in layers)
        {
            layer.Tick(baseSpeed);
        }
    }
}