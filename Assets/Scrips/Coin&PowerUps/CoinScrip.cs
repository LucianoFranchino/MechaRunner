using UnityEngine;

public class CoinScrip : MonoBehaviour
{
    [SerializeField] private AudioClip coinSound;
    [SerializeField] private AudioClip powerSound;
    private enum Type { Coin, CoinMultiplierPU, ScoreMultiplierPU, SuperShoot }
    [SerializeField] private Type type;

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

        switch (type)
        {
            case Type.Coin:
                if (CoinCounter.Instance != null) CoinCounter.Instance.GetCoin();
                AudioManager.instance.PlayAudio(coinSound);
                break;

            case Type.CoinMultiplierPU:
                PowerUpManager.Instance.Activate(PowerUpType.CoinMultiplier);
                AudioManager.instance.PlayAudio(powerSound);
                break;

            case Type.ScoreMultiplierPU:
                PowerUpManager.Instance.Activate(PowerUpType.ScoreMultiplier);
                AudioManager.instance.PlayAudio(powerSound);
                break;

            case Type.SuperShoot:
                PowerUpManager.Instance.Activate(PowerUpType.SuperShoot);
                AudioManager.instance.PlayAudio(powerSound);
                break;
        }

        pooledObject.Despawn();
    }
}
