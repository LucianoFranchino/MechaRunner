using UnityEngine;

/// <summary>
/// Sierra giratoria que va y viene sobre su plataforma (movimiento local, el scroll lo hace la raíz).
/// </summary>
public class SawBlade : MonoBehaviour
{
    [SerializeField] private float spinSpeed = -540f;
    [Tooltip("Distancia a cada lado del centro (local).")]
    [SerializeField] private float patrolRange = 1.2f;
    [SerializeField] private float patrolSpeed = 1.6f;

    private Vector3 origin;
    private float phase;

    private void Awake() => origin = transform.localPosition;

    private void OnEnable()
    {
        // Cada sierra arranca en un punto distinto del recorrido
        phase = Random.value * Mathf.PI * 2f;
    }

    private void Update()
    {
        phase += patrolSpeed * Time.deltaTime;
        transform.localPosition = origin + Vector3.right * Mathf.Sin(phase) * patrolRange;
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
    }
}
