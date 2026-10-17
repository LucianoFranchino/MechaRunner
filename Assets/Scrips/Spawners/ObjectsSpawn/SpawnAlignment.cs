using UnityEngine;

namespace MechaRunner.Spawning
{
    /// <summary>
    /// Calcula a qué distancia por debajo del pivot está la base de un objeto, para
    /// poder apoyarlo sobre una superficie sin importar dónde tenga el pivot ni su escala.
    /// La base es el borde inferior del collider sólido (no trigger) más bajo; si no
    /// tiene colliders sólidos se usa el de los renderers.
    /// </summary>
    public static class SpawnAlignment
    {
        public static float BottomOffsetFromPivot(GameObject go)
        {
            float pivotY = go.transform.position.y;
            float bottom = float.MaxValue;

            foreach (var col in go.GetComponentsInChildren<Collider2D>())
            {
                if (col.isTrigger || !col.enabled) continue;
                bottom = Mathf.Min(bottom, ColliderBottom(col));
            }

            if (bottom == float.MaxValue)
            {
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                    bottom = Mathf.Min(bottom, r.bounds.min.y);
            }

            return bottom == float.MaxValue ? 0f : pivotY - bottom;
        }

        // Geometría calculada desde el Transform (no desde Collider2D.bounds), porque los
        // bounds de un objeto recién reactivado por el pool pueden no estar sincronizados.
        private static float ColliderBottom(Collider2D col)
        {
            var t = col.transform;
            switch (col)
            {
                case BoxCollider2D box:
                {
                    Vector2 half = box.size * 0.5f;
                    float min = float.MaxValue;
                    for (int i = 0; i < 4; i++)
                    {
                        var corner = box.offset + new Vector2(i % 2 == 0 ? -half.x : half.x, i < 2 ? -half.y : half.y);
                        min = Mathf.Min(min, t.TransformPoint(corner).y);
                    }
                    return min - box.edgeRadius * Mathf.Abs(t.lossyScale.y);
                }
                case CircleCollider2D circle:
                {
                    Vector3 scale = t.lossyScale;
                    float radius = circle.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                    return t.TransformPoint(circle.offset).y - radius;
                }
                case CapsuleCollider2D capsule:
                {
                    return t.TransformPoint(capsule.offset).y - capsule.size.y * 0.5f * Mathf.Abs(t.lossyScale.y);
                }
                default:
                    return col.bounds.min.y;
            }
        }
    }
}
