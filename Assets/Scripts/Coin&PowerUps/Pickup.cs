using UnityEngine;

public class Pickup : MonoBehaviour
{
    [SerializeField] private AudioClip coinSound;
    [SerializeField] private AudioClip powerSound;
    [Tooltip("Desactivado: moneda. Activado: activa el power-up elegido.")]
    [SerializeField] private bool isPowerUp;
    [SerializeField] private PowerUpType powerUp;

    private PooledObject pooledObject;
    // El Robot tiene dos colliders (sólido + trigger): ambos entran en el mismo paso de física
    private bool collected;

    private void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
    }

    private void OnEnable()
    {
        collected = false;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (collected || !col.CompareTag("Player")) return;
        collected = true;

        if (isPowerUp)
        {
            PowerUpManager.Instance.Activate(powerUp);
            AudioManager.instance.PlayAudio(powerSound);
        }
        else
        {
            if (CurrencyService.Instance != null) CurrencyService.Instance.CollectCoin();
            AudioManager.instance.PlayAudio(coinSound);
        }

        pooledObject.Despawn();
    }
}
