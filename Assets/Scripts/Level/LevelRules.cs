using UnityEngine;

/// <summary>
/// Reglas de nivel de la escena (GameLab). Sin este componente el juego se comporta como siempre.
/// - Cajas y barriles pisables: chocarlos de frente daña, caer encima es seguro y se puede saltar desde ahí.
/// - Huecos: mientras el Robot está sobre un Pit deja de chocar con el piso (solo él: los enemigos no se caen)
///   y si cae por debajo del piso es Game Over.
/// </summary>
[DefaultExecutionOrder(-500)]
public class LevelRules : MonoBehaviour
{
    public static LevelRules Current { get; private set; }

    [SerializeField] private bool walkableObstacleTops = true;
    [Tooltip("Margen para considerar que el Robot cayó encima y no de frente.")]
    [SerializeField] private float topTolerance = 0.2f;
    [SerializeField] private Collider2D ground;
    [Tooltip("Cuánto por debajo del piso tienen que quedar los pies para que la caída sea mortal.")]
    [SerializeField] private float fallDeathDepth = 0.5f;
    [Tooltip("El centro del Robot tiene que entrar esto en el hueco para caer (que no se caiga rozando el borde).")]
    [SerializeField] private float pitEdgeMargin = 0.12f;

    [Header("Botín de enemigos")]
    [Tooltip("Power-ups que puede soltar un enemigo al morir (en vez de una moneda).")]
    [SerializeField] private PowerUpSlot enemyDrops;
    [SerializeField, Range(0f, 1f)] private float enemyDropChance = 0.3f;

    private Collider2D playerBody;
    private PlayerHealth playerHealth;
    private bool ignoringGround;

    public static bool WalkableTops => Current != null && Current.walkableObstacleTops;

    /// <summary>Es el piso principal (el que se "abre" en los huecos), no una plataforma ni una caja.</summary>
    public static bool IsMainGround(Collider2D collider) => Current != null && collider == Current.ground;

    /// <summary>Lo que suelta un enemigo al morir: un power-up al azar con cierta chance, o nada.</summary>
    public GameObject RollEnemyDrop() => enemyDrops != null && Random.value < enemyDropChance ? enemyDrops.Pick() : null;
    public static float TopTolerance => Current != null ? Current.topTolerance : 0.2f;
    /// <summary>El Robot está sobre un hueco: el piso no cuenta como suelo.</summary>
    public static bool PlayerOverPit { get; private set; }

    private void Awake()
    {
        Current = this;
        PlayerOverPit = false;
    }

    private void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
            foreach (var c in playerHealth.GetComponents<Collider2D>())
                if (!c.isTrigger) playerBody = c;
    }

    private void OnDestroy()
    {
        if (Current != this) return;
        Current = null;
        PlayerOverPit = false;
    }

    private void Update()
    {
        if (playerBody == null || ground == null) return;

        float x = playerBody.bounds.center.x;
        bool over = Pit.Covers(x, pitEdgeMargin) && !playerHealth.IsDead;
        PlayerOverPit = over;
        if (over != ignoringGround)
        {
            Physics2D.IgnoreCollision(playerBody, ground, over);
            ignoringGround = over;
        }

        if (!playerHealth.IsDead && playerBody.bounds.min.y < ground.bounds.max.y - fallDeathDepth)
        {
            if (PlayerHealth.DevInvulnerable)
            {
                // Menú de debug: en vez de morir, vuelve arriba del piso
                var body = playerHealth.transform;
                body.position = new Vector3(body.position.x, ground.bounds.max.y + 2f, body.position.z);
                if (body.TryGetComponent(out Rigidbody2D rb)) rb.linearVelocity = Vector2.zero;
            }
            else playerHealth.Death();
        }
    }
}
