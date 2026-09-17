using UnityEngine;

public class CoinScrip : MonoBehaviour
{
    [SerializeField] private AudioClip coinSound;
    [SerializeField] private AudioClip powerSound;
    private enum Type { Coin, CoinMultiplierPU, ScoreMultiplierPU, SuperShoot }
    [SerializeField] private Type type;

    private PooledObject pooledObject;

    private void Awake()
    {
        pooledObject = GetComponent<PooledObject>();
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("Player")) return;

        switch (type)
        {
            case Type.Coin:
                col.GetComponent<CoinCounter>().GetCoin();
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
