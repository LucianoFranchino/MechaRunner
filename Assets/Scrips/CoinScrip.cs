using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;

public class CoinScrip : MonoBehaviour
{
    [SerializeField] private AudioClip coinSound;
    [SerializeField] private AudioClip powerSound;
    private enum Type { Coin, CoinMultiplierPU, ScoreMultiplierPU, SuperShoot, SecondChance }
    [SerializeField] private Type type;
    public void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.tag == "Player")
        {
            switch (type)
            {
                case Type.Coin:
                col.GetComponent<CoinCounter>().GetCoin();
                AudioManager.instance.PlayAudio(coinSound);
                break;

                case Type.CoinMultiplierPU:
                col.GetComponent<CoinCounter>().PUPMult();
                AudioManager.instance.PlayAudio(powerSound);
                break;

                case Type.ScoreMultiplierPU:
                FindObjectOfType<ScoreManager>().PUPMult();
                AudioManager.instance.PlayAudio(powerSound);
                break;

                case Type.SuperShoot:
                col.GetComponent<Weapon>().superFire = true;
                AudioManager.instance.PlayAudio(powerSound);
                break;

                case Type.SecondChance:
                col.GetComponent<Player>().secondChance = true;
                break;
            }
            print("TENGO LA OBJECTO" + type);
            

            Destroy(gameObject);
        }
    }
}
