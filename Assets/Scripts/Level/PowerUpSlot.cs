using UnityEngine;

/// <summary>
/// Lugar para un power-up al azar dentro de una sección. El director spawnea una de las opciones.
/// </summary>
public class PowerUpSlot : MonoBehaviour
{
    [SerializeField] private GameObject[] options;

    public GameObject Pick() => options == null || options.Length == 0 ? null : options[Random.Range(0, options.Length)];
}
