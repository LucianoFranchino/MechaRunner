using UnityEngine;

/// <summary>
/// Respuesta a los golpes: al recibir daño congela el juego unos fotogramas (hitstop, arranca junto con el
/// sonido de golpe), sacude la cámara (animación ShakeCam sobre el padre de la cámara, así no pelea con
/// CameraFraming) y vibra el celular; al destruir un enemigo, una sacudida suave y una vibración sutil.
/// </summary>
public class HitFeedback : MonoBehaviour
{
    [Tooltip("Animator del objeto padre de la cámara, con el estado de sacudida.")]
    [SerializeField] private Animator shake;
    [SerializeField] private string shakeState = "ShakeCam";
    [Tooltip("Sacudida más suave al destruir un enemigo.")]
    [SerializeField] private string killShakeState = "ShakeSmall";

    [Header("Hitstop")]
    [Tooltip("Segundos reales que se congela el juego al recibir daño (el golpe del sonido Hurt cae a los ~30 ms).")]
    [SerializeField] private float hitStopSeconds = 0.09f;

    [Header("Vibración (ms / intensidad 1-255)")]
    [SerializeField] private int hurtMs = 60;
    [SerializeField] private int hurtAmplitude = 200;
    [SerializeField] private int deathMs = 120;
    [SerializeField] private int deathAmplitude = 255;
    [SerializeField] private int killMs = 18;
    [SerializeField] private int killAmplitude = 70;

    private int shakeHash;
    private int killShakeHash;
    private bool hitStopping;
    private float hitStopUntil;
    private float scaleBeforeHitStop = 1f;

    public bool IsHitStopping => hitStopping;

    private void Awake()
    {
        shakeHash = Animator.StringToHash(shakeState);
        killShakeHash = Animator.StringToHash(killShakeState);
    }

    private void OnEnable()
    {
        PlayerHealth.Hurt += OnHurt;
        Enemy.Killed += OnEnemyKilled;
    }

    private void OnHurt(bool died)
    {
        // Al morir el Game Over ya congela el juego
        if (!died) StartHitStop();
        if (shake != null) shake.Play(shakeHash, 0, 0f);
        if (died) Haptics.Vibrate(deathMs, deathAmplitude);
        else Haptics.Vibrate(hurtMs, hurtAmplitude);
    }

    private void OnEnemyKilled()
    {
        // No cortar una sacudida de daño que esté sonando
        if (shake != null && !shake.GetCurrentAnimatorStateInfo(0).IsName(shakeState)) shake.Play(killShakeHash, 0, 0f);
        Haptics.Vibrate(killMs, killAmplitude);
    }

    private void StartHitStop()
    {
        if (hitStopSeconds <= 0f || !GameManager.IsPlaying) return;
        if (!hitStopping)
        {
            scaleBeforeHitStop = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            hitStopping = true;
        }
        hitStopUntil = Time.unscaledTime + hitStopSeconds;
    }

    // Update corre aunque timeScale sea 0
    private void Update()
    {
        if (!hitStopping || Time.unscaledTime < hitStopUntil) return;
        hitStopping = false;
        // No pisar una pausa ni el Game Over que hayan ocurrido durante el congelamiento
        if (GameManager.IsPlaying && Time.timeScale == 0f) Time.timeScale = scaleBeforeHitStop;
    }

    private void OnDisable()
    {
        PlayerHealth.Hurt -= OnHurt;
        Enemy.Killed -= OnEnemyKilled;
        if (hitStopping && GameManager.IsPlaying && Time.timeScale == 0f) Time.timeScale = scaleBeforeHitStop;
        hitStopping = false;
    }
}
