using UnityEngine;

/// <summary>
/// Movimiento genérico de scroll hacia la izquierda a la velocidad del juego,
/// con auto-despawn al salir de pantalla. Se agrega por composición a cualquier
/// prefab pooleado: obstáculos, enemigos, monedas, power-ups.
/// Lee la velocidad actual cada frame, así cuando sube la dificultad todo lo que
/// está en pantalla acelera junto con el piso y el parallax.
/// </summary>
[RequireComponent(typeof(PooledObject))]
public class ScrollMover : MonoBehaviour
{
    [Tooltip("Multiplica la velocidad base del juego. 1 = misma velocidad que el resto del scroll.")]
    [SerializeField] private float speedMultiplier = 1f;
    [SerializeField] private float despawnDistanceX = -15f;

    private const float FallbackSpeed = 5f;

    private PooledObject pooledObject;
    private Transform cachedTransform;

    private void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
        cachedTransform = transform;
    }

    private void Update()
    {
        float baseSpeed = DifficultyManager.Instance != null ? DifficultyManager.Instance.GameSpeed : FallbackSpeed;
        Vector3 pos = cachedTransform.position;
        pos.x -= baseSpeed * speedMultiplier * Time.deltaTime;
        cachedTransform.position = pos;

        if (pos.x <= despawnDistanceX)
        {
            pooledObject.Despawn();
        }
    }
}
