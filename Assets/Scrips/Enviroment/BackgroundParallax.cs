using UnityEngine;

public class BackgroundParallax : MonoBehaviour
{
    [Header("Parallax Settings")]
    [SerializeField] private float scrollSpeed = 2f;
    [SerializeField] private float parallaxMultiplier = 0.5f; // 0-1: 0 = fondo lejano (lento), 1 = cerca (rápido)

    [Header("Seamless Loop")]
    [SerializeField] private bool useSeamlessLoop = true;
    [SerializeField] private Transform[] backgroundLayers; // Asignar 2 copias del mismo sprite

    private float spriteWidth;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteWidth = spriteRenderer.bounds.size.x;
        }

        // Si no hay capas asignadas y se quiere seamless, intentar auto-setup
        if (useSeamlessLoop && (backgroundLayers == null || backgroundLayers.Length == 0))
        {
            SetupSeamlessLoop();
        }
    }

    private void Update()
    {
        float movement = scrollSpeed * parallaxMultiplier * Time.deltaTime;

        if (useSeamlessLoop && backgroundLayers != null && backgroundLayers.Length >= 2)
        {
            // Sistema de loop seamless con 2+ sprites
            foreach (Transform layer in backgroundLayers)
            {
                if (layer == null) continue;

                layer.Translate(Vector3.left * movement);

                // Si el sprite salió completamente de la pantalla por la izquierda
                if (layer.position.x <= -spriteWidth)
                {
                    // Encontrar la capa más a la derecha
                    float maxX = float.MinValue;
                    foreach (Transform otherLayer in backgroundLayers)
                    {
                        if (otherLayer != null && otherLayer.position.x > maxX)
                            maxX = otherLayer.position.x;
                    }

                    // Posicionar justo después de la última capa
                    Vector3 newPos = layer.position;
                    newPos.x = maxX + spriteWidth;
                    layer.position = newPos;
                }
            }
        }
        else
        {
            // Sistema simple (puede tener cortes)
            transform.Translate(Vector3.left * movement);

            if (spriteRenderer != null && transform.position.x <= -spriteWidth)
            {
                Vector3 newPos = transform.position;
                newPos.x += spriteWidth * 2;
                transform.position = newPos;
            }
        }
    }

    // Auto-crear segunda capa para seamless loop
    private void SetupSeamlessLoop()
    {
        if (spriteRenderer == null) return;

        // Crear un GameObject duplicado
        GameObject duplicate = new GameObject(gameObject.name + "_Duplicate");
        duplicate.transform.parent = transform.parent;
        duplicate.transform.position = transform.position + Vector3.right * spriteWidth;
        duplicate.transform.localScale = transform.localScale;
        duplicate.layer = gameObject.layer;

        // Copiar SpriteRenderer
        SpriteRenderer dupSR = duplicate.AddComponent<SpriteRenderer>();
        dupSR.sprite = spriteRenderer.sprite;
        dupSR.sortingLayerName = spriteRenderer.sortingLayerName;
        dupSR.sortingOrder = spriteRenderer.sortingOrder;
        dupSR.material = spriteRenderer.material;
        dupSR.color = spriteRenderer.color;

        // Asignar las capas
        backgroundLayers = new Transform[] { transform, duplicate.transform };

        Debug.Log($"Seamless loop configurado automáticamente para {gameObject.name}");
    }
}
