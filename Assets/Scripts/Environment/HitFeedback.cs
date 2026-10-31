using UnityEngine;

/// <summary>
/// Respuesta a los golpes: al recibir daño sacude la cámara (animación ShakeCam sobre el padre de la
/// cámara, así no pelea con CameraFraming) y vibra el celular; al destruir un enemigo, una vibración sutil.
/// </summary>
public class HitFeedback : MonoBehaviour
{
    [Tooltip("Animator del objeto padre de la cámara, con el estado de sacudida.")]
    [SerializeField] private Animator shake;
    [SerializeField] private string shakeState = "ShakeCam";

    [Header("Vibración (ms / intensidad 1-255)")]
    [SerializeField] private int hurtMs = 60;
    [SerializeField] private int hurtAmplitude = 200;
    [SerializeField] private int deathMs = 120;
    [SerializeField] private int deathAmplitude = 255;
    [SerializeField] private int killMs = 18;
    [SerializeField] private int killAmplitude = 70;

    private int shakeHash;

    private void Awake() => shakeHash = Animator.StringToHash(shakeState);

    private void OnEnable()
    {
        PlayerHealth.Hurt += OnHurt;
        Enemy.Killed += OnEnemyKilled;
    }

    private void OnDisable()
    {
        PlayerHealth.Hurt -= OnHurt;
        Enemy.Killed -= OnEnemyKilled;
    }

    private void OnHurt(bool died)
    {
        if (shake != null) shake.Play(shakeHash, 0, 0f);
        if (died) Haptics.Vibrate(deathMs, deathAmplitude);
        else Haptics.Vibrate(hurtMs, hurtAmplitude);
    }

    private void OnEnemyKilled() => Haptics.Vibrate(killMs, killAmplitude);
}
