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

    [Header("Testing (Editor Only)")]
    [SerializeField] private bool useMouseForTesting = true;

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

        // Testing con mouse en el Editor
#if UNITY_EDITOR
        if (useMouseForTesting)
        {
            HandleMouseInput();
        }
#endif
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
        Vector2 swipeDelta = swipeEndPos - swipeStartPos;

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
        }
        else if (doubleJumpUnlocked && !hasDoubleJumped)
        {
            // Doble salto (solo si está desbloqueado)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            hasDoubleJumped = true;
        }
    }

    private void FastFall()
    {
        // Solo aplicar caída rápida si está en el aire
        if (!isGrounded && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -jumpForce * 0.5f);
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
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
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

#if UNITY_EDITOR
    // Método para testear con mouse en el Editor
    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            swipeStartPos = Input.mousePosition;
            isSwiping = true;
        }

        if (Input.GetMouseButtonUp(0) && isSwiping)
        {
            swipeEndPos = Input.mousePosition;
            DetectSwipe();
            isSwiping = false;
        }
    }
#endif
}
