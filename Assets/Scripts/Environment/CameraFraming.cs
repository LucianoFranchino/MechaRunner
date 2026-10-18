using UnityEngine;

/// <summary>
/// Encuadre de la cámara de gameplay independiente de la relación de aspecto:
/// alto visible fijo, borde inferior fijo y el jugador siempre a la misma distancia
/// del borde izquierdo. En pantallas más anchas solo se gana anticipación hacia la
/// derecha. Además ubica los puntos de spawn justo fuera del borde derecho.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFraming : MonoBehaviour
{
    public readonly struct Framing
    {
        public readonly Vector2 CameraPosition;
        public readonly float LeftEdge;
        public readonly float RightEdge;

        public Framing(Vector2 cameraPosition, float leftEdge, float rightEdge)
        {
            CameraPosition = cameraPosition;
            LeftEdge = leftEdge;
            RightEdge = rightEdge;
        }
    }

    [Tooltip("Mitad del alto visible en unidades de mundo (Orthographic Size).")]
    [SerializeField] private float orthographicSize = 3f;
    [Tooltip("Coordenada Y del borde inferior de la cámara (mantiene el piso en el mismo lugar de pantalla).")]
    [SerializeField] private float bottomY = -1f;

    [Header("Jugador")]
    [SerializeField] private Transform player;
    [Tooltip("Distancia del centro del jugador al borde izquierdo de la pantalla.")]
    [SerializeField] private float playerLeftMargin = 2f;

    [Header("Spawn")]
    [Tooltip("Puntos de spawn que se ubican fuera del borde derecho.")]
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("Distancia de los spawns al borde derecho (mayor que la mitad del objeto más ancho).")]
    [SerializeField] private float spawnMarginX = 2f;

    private Camera cam;
    private float playerX;
    private int lastWidth;
    private int lastHeight;

    public float LeftEdge { get; private set; }
    public float RightEdge { get; private set; }

    /// <summary>
    /// Cálculo puro del encuadre (sin dependencias de escena): testeable con NUnit.
    /// </summary>
    public static Framing Compute(float aspect, float orthographicSize, float bottomY, float playerX, float playerLeftMargin)
    {
        float halfWidth = orthographicSize * aspect;
        float leftEdge = playerX - playerLeftMargin;
        var position = new Vector2(leftEdge + halfWidth, bottomY + orthographicSize);
        return new Framing(position, leftEdge, leftEdge + 2f * halfWidth);
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        // X inicial del jugador: la cámara no lo sigue aunque un choque lo desplace
        playerX = player != null ? player.position.x : transform.position.x;
        Refresh();
    }

    private void LateUpdate()
    {
        // Rotación, cambio de resolución (AppBootstrap) o ventana redimensionable
        if (Screen.width != lastWidth || Screen.height != lastHeight)
            Refresh();
    }

    /// <summary>Recalcula el encuadre con el aspecto actual de la cámara.</summary>
    public void Refresh()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;

        var framing = Compute(cam.aspect, orthographicSize, bottomY, playerX, playerLeftMargin);
        cam.orthographicSize = orthographicSize;
        transform.position = new Vector3(framing.CameraPosition.x, framing.CameraPosition.y, transform.position.z);
        LeftEdge = framing.LeftEdge;
        RightEdge = framing.RightEdge;

        if (spawnPoints == null) return;
        float spawnX = framing.RightEdge + spawnMarginX;
        foreach (var point in spawnPoints)
        {
            if (point == null) continue;
            Vector3 pos = point.position;
            pos.x = spawnX;
            point.position = pos;
        }
    }
}
