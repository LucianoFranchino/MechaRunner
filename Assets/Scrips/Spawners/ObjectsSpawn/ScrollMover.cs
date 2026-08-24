using UnityEngine;

/// <summary>
/// Movimiento genérico de scroll hacia la izquierda a la velocidad del juego,
/// con auto-despawn al salir de pantalla. Se agrega por composición a cualquier
/// prefab pooleado: obstáculos, enemigos, monedas, power-ups.
/// </summary>
[RequireComponent(typeof(PooledObject))]
public class ScrollMover : MonoBehaviour, IPooleable
{
    [Tooltip("Multiplica la velocidad base del juego. 1 = misma velocidad que el resto del scroll.")]
    [SerializeField] private float speedMultiplier = 1f;
    [SerializeField] private float despawnDistanceX = -15f;

    private float currentSpeed;
    private PooledObject pooledObject;

    private void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
    }

    private void Update()
    {
        transform.Translate(Vector2.left * Time.deltaTime * currentSpeed);

        if (transform.position.x <= despawnDistanceX)
        {
            pooledObject.Despawn();
        }
    }

    public void OnSpawn()
    {
        float baseSpeed = DificultyManager.Instance != null ? DificultyManager.Instance.GameSpeed : 5f;
        currentSpeed = baseSpeed * speedMultiplier;
    }

    public void OnDespawn() { }
}