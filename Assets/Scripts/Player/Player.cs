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

    [Header("Swipe Settings")]
    [SerializeField] private float swipeThreshold = 50f;

    [Header("Double Jump")]
    [SerializeField] private bool doubleJumpUnlocked = false;

    [Header("Animación (opcional)")]
    [SerializeField] private Animator animator;

    private static readonly int JumpState = Animator.StringToHash("Jump");

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool hasDoubleJumped;
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
        touchPressAction.performed += OnTouchPress;
        touchPressAction.canceled += OnTouchRelease;
    }

    private void OnDisable()
    {
        touchPressAction.performed -= OnTouchPress;
        touchPressAction.canceled -= OnTouchRelease;
    }

    private void Update()
    {
        CheckGround();

        if (isGrounded)
        {
            hasDoubleJumped = false;
        }
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
        if (isGrounded)
        {
            // Salto normal desde el suelo
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            PlayJumpAnimation();
        }
        else if (doubleJumpUnlocked && !hasDoubleJumped)
        {
            // Doble salto (solo si está desbloqueado)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            hasDoubleJumped = true;
            PlayJumpAnimation();
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        else if (Mathf.Abs(rb.linearVelocity.y) < 0.05f)
        {
            // Quieto (apoyado) y el swipe no hizo saltar: estado a investigar
            Debug.LogWarning($"[Salto] Swipe arriba sin salto estando quieto: piso={isGrounded} dobleUsado={hasDoubleJumped} " +
                $"dobleDesbloqueado={doubleJumpUnlocked} pos={transform.position} sobreHueco={LevelRules.PlayerOverPit} esquema={ControlScheme}");
        }
#endif
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
