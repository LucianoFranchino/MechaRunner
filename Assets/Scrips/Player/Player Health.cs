using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int playerHealth = 2;
    [SerializeField] private DeadthCreen deathScreen;

    [Header("Invulnerabilidad tras recibir daño")]
    [SerializeField] private float invulnerabilityTime = 1f;
    [SerializeField] private float blinkInterval = 0.1f;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip deathSound;

    public bool IsDead { get; private set; }

    private float invulnerableUntil;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (spriteRenderer == null) return;

        bool invulnerable = !IsDead && Time.time < invulnerableUntil;
        spriteRenderer.enabled = !invulnerable || Mathf.Repeat(Time.time, blinkInterval * 2f) >= blinkInterval;
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
    }

    public void Death()
    {
        if (IsDead) return;
        IsDead = true;
        if (spriteRenderer != null) spriteRenderer.enabled = true;

        if (ScoreManager.instance != null)
        {
            ScoreManager.instance.scoreIncreasing = false;
            ScoreManager.instance.Save();
        }

        AudioManager.instance.PlayAudio(deathSound);
        deathScreen.ToggleEndMenu();
    }
}
