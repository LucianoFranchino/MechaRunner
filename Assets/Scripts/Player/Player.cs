using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Controles")]
    [Tooltip("Swipe arriba/abajo para saltar y caer rápido (control anterior). Apagado: se salta con el botón JUMP.")]
    [SerializeField] private bool swipeControls = false;
    [SerializeField] private float swipeThreshold = 50f;

    [Header("Double Jump")]
    [SerializeField] private bool doubleJumpUnlocked = false;

    [Header("Game feel")]
    [Tooltip("Segundos después de dejar el piso en los que todavía se puede hacer el salto normal (coyote time).")]
    [SerializeField] private float coyoteTime = 0.1f;
    [Tooltip("Si se pide un salto sin salto disponible, se guarda estos segundos y se hace apenas toca el piso (jump buffer).")]
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Header("Animación (opcional)")]
    [SerializeField] private Animator animator;

    private static readonly int JumpState = Animator.StringToHash("Jump");

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool hasDoubleJumped;
    // Coyote time y jump buffer
    private float lastGroundedTime = float.NegativeInfinity;
    private float jumpBufferedUntil = float.NegativeInfinity;
    private bool groundJumpUsed;
    private Vector2 swipeStartPos;
    private Vector2 swipeEndPos;
    private bool isSwiping;

    // Referencias del Input System
    private PlayerInput playerInput;
    private InputAction touchPositionAction;
    private InputAction touchPressAction;

    // Diagnóstico (DevOverlay): solo lectura
    public bool IsGrounded => isGrounded;
    public bool HasDoubleJumped => hasDoubleJumped;
    public float LastSwipeDeltaY { get; private set; }
    public string ControlScheme => playerInput != null ? playerInput.currentControlScheme : "";

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();

        // Obtener las acciones del Input System
        touchPositionAction = playerInput.actions["TouchPosition"];
        touchPressAction = playerInput.actions["TouchPress"];
    }

    private void OnEnable()
    {
        if (!swipeControls) return;
        touchPressAction.performed += OnTouchPress;
        touchPressAction.canceled += OnTouchRelease;
    }

    private void OnDisable()
    {
        touchPressAction.performed -= OnTouchPress;
        touchPressAction.canceled -= OnTouchRelease;
    }

    /// <summary>Botón JUMP del HUD (al apretar): salta o, en el aire, hace el doble salto.</summary>
    public void JumpButton()
    {
        if (!GameManager.IsPlaying) return;
        Jump();
    }

    private void Update()
    {
        CheckGround();

        if (isGrounded)
        {
            hasDoubleJumped = false;
            // Apoyado (no en el despegue): vuelve a tener salto normal y arranca el margen de coyote
            if (rb.linearVelocity.y <= 0.01f)
            {
                groundJumpUsed = false;
                lastGroundedTime = Time.time;
                if (Time.time <= jumpBufferedUntil)
                {
                    jumpBufferedUntil = float.NegativeInfinity;
                    GroundJump();
                }
            }
        }
    }

    /// <summary>Puede hacer el salto normal: en el piso o recién salido de él (coyote time).</summary>
    private bool CanGroundJump => !groundJumpUsed && (isGrounded || Time.time - lastGroundedTime <= coyoteTime);

    private void GroundJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        groundJumpUsed = true;
        lastGroundedTime = float.NegativeInfinity;
        PlayJumpAnimation();
    }

    private void FixedUpdate()
    {
        ApplyBetterJump();
    }

    private void OnTouchPress(InputAction.CallbackContext context)
    {
        swipeStartPos = touchPositionAction.ReadValue<Vector2>();
        isSwiping = true;
    }

    private void OnTouchRelease(InputAction.CallbackContext context)
    {
        if (!isSwiping) return;

        swipeEndPos = touchPositionAction.ReadValue<Vector2>();
        DetectSwipe();
        isSwiping = false;
    }

    private void DetectSwipe()
    {
        // Ignorar gestos con el juego en pausa o en la pantalla de muerte
        if (!GameManager.IsPlaying) return;

        Vector2 swipeDelta = swipeEndPos - swipeStartPos;
        LastSwipeDeltaY = swipeDelta.y;

        // Swipe vertical debe ser mayor al horizontal
        if (Mathf.Abs(swipeDelta.y) > Mathf.Abs(swipeDelta.x))
        {
            if (Mathf.Abs(swipeDelta.y) > swipeThreshold)
            {
                if (swipeDelta.y > 0)
                {
                    // Swipe Up - Saltar
                    Jump();
                }
                else
                {
                    // Swipe Down - Bajar rápido
                    FastFall();
                }
            }
        }
    }

    private void Jump()
    {
        if (CanGroundJump)
        {
            // Salto normal desde el suelo (o justo después de dejarlo)
            GroundJump();
        }
        else if (doubleJumpUnlocked && !hasDoubleJumped)
        {
            // Doble salto (solo si está desbloqueado)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            hasDoubleJumped = true;
            PlayJumpAnimation();
        }
        else
        {
            // Sin salto disponible: se guarda y se hace al tocar el piso (jump buffer)
            jumpBufferedUntil = Time.time + jumpBufferTime;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Tocando el piso pero sin "piso": el estado del bug del salto a investigar
            if (rb.IsTouchingLayers(groundLayer) && Mathf.Abs(rb.linearVelocity.y) < 0.05f)
                Debug.LogWarning($"[Salto] Salto pedido apoyado sin piso detectado: dobleUsado={hasDoubleJumped} " +
                    $"pos={transform.position} sobreHueco={LevelRules.PlayerOverPit} esquema={ControlScheme}");
#endif
        }
    }

    private void PlayJumpAnimation()
    {
        if (animator != null) animator.Play(JumpState, 0, 0f);
    }

    private void FastFall()
    {
        // Caída rápida en cualquier momento del aire (subiendo o bajando),
        // sin frenar si ya cae más rápido que eso.
        if (!isGrounded)
        {
            float fastFallSpeed = -jumpForce * 0.5f;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Min(rb.linearVelocity.y, fastFallSpeed));
        }
    }

    private void ApplyBetterJump()
    {
        // Hacer que la caída sea más pesada para un mejor "feel"
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    private void CheckGround()
    {
        Collider2D ground = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        // Sobre un hueco (GameLab) el piso de abajo no cuenta, pero una plataforma sí
        isGrounded = ground != null && !(LevelRules.PlayerOverPit && LevelRules.IsMainGround(ground));

        if (!isGrounded && LevelRules.WalkableTops) isGrounded = StandingOnObstacle();
    }

    private static readonly Collider2D[] groundHits = new Collider2D[4];

    private bool StandingOnObstacle()
    {
        var filter = new ContactFilter2D { useTriggers = false };
        int count = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, filter, groundHits);
        for (int i = 0; i < count; i++)
            if (groundHits[i].GetComponent<Obstacle>() != null) return true;
        return false;
    }

    // Método público para desbloquear el doble salto con power-up
    public void UnlockDoubleJump()
    {
        doubleJumpUnlocked = true;
        Debug.Log("¡Doble salto desbloqueado!");
    }

    // Método para bloquear el doble salto (opcional)
    public void LockDoubleJump()
    {
        doubleJumpUnlocked = false;
        hasDoubleJumped = false;
    }

    // Método para verificar si el doble salto está disponible
    public bool IsDoubleJumpUnlocked()
    {
        return doubleJumpUnlocked;
    }

    // Visualización del ground check en el editor
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

}
