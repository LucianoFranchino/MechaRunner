using UnityEngine;

/// <summary>
/// Polvo flotando en el aire de la fábrica: las partículas avanzan con el mundo a una fracción de la
/// velocidad del juego (como una capa de parallax cercana). Solo ajusta la velocidad, sin basura.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class AmbientDust : MonoBehaviour
{
    [Tooltip("Fracción de la velocidad del juego con la que se mueve el polvo (1 = como los obstáculos).")]
    [SerializeField, Range(0f, 1f)] private float parallax = 0.85f;

    private ParticleSystem.VelocityOverLifetimeModule velocity;

    private void Awake()
    {
        velocity = GetComponent<ParticleSystem>().velocityOverLifetime;
        velocity.enabled = true;
    }

    private void Update()
    {
        var difficulty = DifficultyManager.Instance;
        float speed = difficulty != null ? difficulty.GameSpeed : 0f;
        velocity.x = new ParticleSystem.MinMaxCurve(-speed * parallax);
    }
}
