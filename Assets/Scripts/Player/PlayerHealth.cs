using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int playerHealth = 2;

    [Header("Invulnerabilidad tras recibir daño")]
    [SerializeField] private float invulnerabilityTime = 1f;
    [SerializeField] private float blinkInterval = 0.1f;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;

    [Header("Animación (opcional)")]
    [SerializeField] private Animator animator;

    private static readonly int DamageState = Animator.StringToHash("Damage");

    public bool IsDead { get; private set; }

    private float invulnerableUntil;
    // Todas las partes del personaje (el Robot está armado por piezas, sin sprite en la raíz)
    private SpriteRenderer[] spriteRenderers;
    private bool visible = true;

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void Update()
    {
        bool invulnerable = !IsDead && Time.time < invulnerableUntil;
        SetVisible(!invulnerable || Mathf.Repeat(Time.time, blinkInterval * 2f) >= blinkInterval);
    }

    // Solo toca los renderers cuando cambia el estado (no en cada frame)
    private void SetVisible(bool value)
    {
        if (value == visible) return;
        visible = value;
        foreach (var sr in spriteRenderers) sr.enabled = value;
    }

    // El daño por contacto lo aplica quien golpea (Enemy, Obstacle); acá no se
    // escucha OnTriggerEnter2D para no contar dos veces el mismo golpe.
    public void PlayerDamage(int damage)
    {
        if (IsDead || Time.time < invulnerableUntil) return;

        playerHealth -= damage;
        if (playerHealth <= 0)
        {
            Death();
            return;
        }

        AudioManager.instance.PlayAudio(hurtSound);
        invulnerableUntil = Time.time + invulnerabilityTime;
        if (animator != null) animator.Play(DamageState, 0, 0f);
    }

    public void Death()
    {
        if (IsDead) return;
        IsDead = true;
        SetVisible(true);

        AudioManager.instance.PlayAudio(deathSound);
        // Guarda el récord, congela el juego y muestra los resultados
        if (GameManager.Instance != null) GameManager.Instance.GameOver();
    }
}
