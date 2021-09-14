using System.Collections;
using System.Collections.Generic;
using UnityEngine.Audio;
using UnityEngine;

public class CoinScrip : MonoBehaviour
{

    public GameObject coinSound;

    public GameObject powerSound;
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
                Instantiate(coinSound, transform.position, Quaternion.identity);
                break;

                case Type.CoinMultiplierPU:
                col.GetComponent<CoinCounter>().PUPMult();
                Instantiate(powerSound, transform.position, Quaternion.identity);
                break;

                case Type.ScoreMultiplierPU:
                FindObjectOfType<ScoreManager>().PUPMult();
                Instantiate(powerSound, transform.position, Quaternion.identity);
                break;

                case Type.SuperShoot:
                col.GetComponent<Weapon>().superFire = true;
                Instantiate(powerSound, transform.position, Quaternion.identity);
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
