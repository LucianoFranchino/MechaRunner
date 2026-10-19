using UnityEngine;

/// <summary>
/// Peligro por contacto (láser, sierra): daña al Robot al entrar en su trigger.
/// La invulnerabilidad de PlayerHealth evita el daño repetido.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Hazard : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        var health = other.GetComponentInParent<PlayerHealth>();
        if (health != null) health.PlayerDamage(damage);
    }
}
