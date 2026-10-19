using UnityEngine;

/// <summary>
/// Dibuja un sprite repetido (modo Tiled) cubriendo exactamente el tamaño del padre (que es un cuadrado de
/// 1 x 1 escalado, como las plataformas), sin deformar el dibujo: lo escala parejo para que su alto entre en
/// el alto del padre y lo repite a lo largo del ancho. Se reajusta solo si el padre cambia de escala
/// (las plataformas se estiran con la velocidad).
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class FitTiledSprite : MonoBehaviour
{
    private SpriteRenderer view;
    private Vector3 appliedFor = new(float.NaN, 0f, 0f);

    private void OnEnable()
    {
        view = GetComponent<SpriteRenderer>();
        Apply();
    }

    private void LateUpdate()
    {
        if (transform.parent != null && transform.parent.lossyScale != appliedFor) Apply();
    }

    public void Apply()
    {
        var parent = transform.parent;
        if (parent == null || view == null || view.sprite == null) return;
        Vector3 size = parent.lossyScale;                 // tamaño del padre en el mundo (base 1 x 1)
        appliedFor = size;
        Vector2 native = view.sprite.bounds.size;
        float k = Mathf.Abs(size.y) / native.y;          // escala pareja: el alto del dibujo = alto del padre
        if (k <= 0f || size.x == 0f) return;

        view.drawMode = SpriteDrawMode.Tiled;
        view.tileMode = SpriteTileMode.Continuous;
        transform.localPosition = Vector3.zero;
        transform.localScale = new Vector3(k / size.x, k / size.y, 1f);
        view.size = new Vector2(Mathf.Abs(size.x) / k, native.y);
    }
}
