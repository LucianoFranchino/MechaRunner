using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [SerializeField] private int obstacleDamage = 3;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Reglas de GameLab: aterrizar arriba es seguro, chocar de frente no
            if (LevelRules.WalkableTops && (LandedOnTop(collision) || HitFromBelow(collision))) return;
            collision.gameObject.GetComponent<PlayerHealth>().PlayerDamage(obstacleDamage);
        }
    }

    private bool LandedOnTop(Collision2D collision)
    {
        var own = collision.otherCollider; // el collider de este obstáculo
        return collision.collider.bounds.min.y >= own.bounds.max.y - LevelRules.TopTolerance;
    }

    // Plataformas: golpear la parte de abajo con la cabeza solo frena el salto
    private bool HitFromBelow(Collision2D collision)
    {
        var own = collision.otherCollider;
        return collision.collider.bounds.max.y <= own.bounds.min.y + LevelRules.TopTolerance;
    }
}
