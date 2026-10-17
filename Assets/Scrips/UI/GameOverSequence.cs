using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Animación de la pantalla de resultados. Corre con tiempo sin escala (el Game Over pausa el juego):
/// entrada de título y tarjeta, puntaje de 0 al final, celebración a pantalla completa si hubo récord,
/// monedas de la partida volando al total y botones. Tocar la pantalla salta al estado final.
/// No instancia nada: monedas voladoras y confeti son imágenes de la escena que se reutilizan.
/// </summary>
[RequireComponent(typeof(DeadthCreen))]
public class GameOverSequence : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Recuadro de puntaje/monedas del HUD: se oculta para que no muestre el total final antes que el contador animado.")]
    [SerializeField] private GameObject hudPlate;

    [Header("Tarjeta")]
    [SerializeField] private RectTransform title;
    [SerializeField] private RectTransform card;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private GameObject newBadge;

    [Header("Monedas")]
    [SerializeField] private RectTransform coinsRow;
    [SerializeField] private TMP_Text runCoinsText;
    [Tooltip("Ícono de la fila COINS: de ahí salen las monedas.")]
    [SerializeField] private RectTransform coinsSource;
    [SerializeField] private RectTransform totalCounter;
    [Tooltip("Ícono del contador total: ahí llegan las monedas.")]
    [SerializeField] private RectTransform totalTarget;
    [SerializeField] private TMP_Text totalText;
    [Tooltip("Monedas voladoras precreadas (se dibujan como máximo estas, el número cuenta todas).")]
    [SerializeField] private Image[] flyers;
    [Tooltip("Cuadros del giro de la moneda.")]
    [SerializeField] private Sprite[] spinFrames;

    [Header("Récord")]
    [SerializeField] private CanvasGroup celebration;
    [SerializeField] private Image flash;
    [SerializeField] private RectTransform medal;
    [SerializeField] private RectTransform recordLabel;
    [SerializeField] private TMP_Text recordScore;
    [SerializeField] private RectTransform[] confetti;

    [Header("Acciones")]
    [SerializeField] private CanvasGroup actions;

    [Header("Sonido")]
    [SerializeField] private AudioClip coinClip;
    [SerializeField] private AudioClip recordClip;

    [Header("Tiempos")]
    [SerializeField] private float countMin = 0.6f;
    [SerializeField] private float countMax = 1.6f;
    [Tooltip("Puntaje con el que el conteo dura countMax.")]
    [SerializeField] private float countFullAt = 1000f;
    [SerializeField] private float celebrationHold = 1.8f;

    private int finalScore, runCoins, totalCoins, shownTotal, arrived, flying;
    private bool record, celebrationShown, skipCelebration, finished;
    private float totalPunch = -1f;
    private Vector2[] confettiVelocity;
    private float[] confettiSpin;

    /// <summary>La secuencia llegó a su estado final (o se completó al tocar).</summary>
    public bool Finished => finished;

    private void Awake()
    {
        confettiVelocity = new Vector2[confetti.Length];
        confettiSpin = new float[confetti.Length];
    }

    private void OnEnable()
    {
        // La escena arranca con el objeto activo y DeadthCreen lo apaga: solo animar en un Game Over real
        if (!DeadthCreen.IsGameOver) return;
        ReadResults();
        ResetVisuals();
        StartCoroutine(Sequence());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!DeadthCreen.IsGameOver) return;
        if (celebration.gameObject.activeSelf) skipCelebration = true;
        else if (!finished) CompleteNow();
    }

    private void ReadResults()
    {
        var sm = ScoreManager.instance;
        finalScore = sm != null ? Mathf.RoundToInt(sm.scoreCount) : 0;
        record = sm != null && sm.IsNewRecord;
        var cc = CoinCounter.Instance;
        totalCoins = cc != null ? cc.coinValue : 0;
        runCoins = cc != null ? Mathf.Max(0, cc.RunCoins) : 0;
    }

    private void ResetVisuals()
    {
        finished = celebrationShown = skipCelebration = false;
        arrived = flying = 0;
        totalPunch = -1f;
        if (hudPlate != null) hudPlate.SetActive(false); // la escena se recarga al reiniciar
        title.localScale = Vector3.zero;
        card.localScale = Vector3.zero;
        scoreText.SetText("{0}", 0);
        newBadge.SetActive(false);
        coinsRow.gameObject.SetActive(false);
        shownTotal = totalCoins - runCoins;
        totalText.SetText("{0}", shownTotal);
        totalCounter.localScale = Vector3.one;
        foreach (var f in flyers) f.gameObject.SetActive(false);
        celebration.gameObject.SetActive(false);
        SetActions(0f, false);
    }

    // ------------------------------------------------------------------ línea de tiempo

    private IEnumerator Sequence()
    {
        yield return Pop(title, 0.26f);
        yield return Pop(card, 0.22f);

        // Puntaje: 0 → final, más largo cuanto más alto (con tope)
        float duration = Mathf.Lerp(countMin, countMax, Mathf.Clamp01(finalScore / countFullAt));
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            scoreText.SetText("{0}", Mathf.RoundToInt(finalScore * EaseOutCubic(t / duration)));
            yield return null;
        }
        scoreText.SetText("{0}", finalScore);
        yield return Punch(scoreText.rectTransform, 1.2f, 0.2f);

        // Los botones aparecen ya: el jugador nunca queda esperando
        StartCoroutine(ShowActions(0.2f));

        if (record) yield return Celebrate();
        yield return Coins();
        finished = true;
    }

    private IEnumerator Celebrate()
    {
        celebrationShown = true;
        skipCelebration = false;
        celebration.gameObject.SetActive(true);
        celebration.alpha = 1f;
        recordScore.SetText("{0}", finalScore);
        medal.localScale = Vector3.zero;
        recordLabel.localScale = Vector3.zero;
        if (AudioManager.instance != null) AudioManager.instance.PlayAudio(recordClip);

        float s = CanvasScale;
        for (int i = 0; i < confetti.Length; i++)
        {
            float angle = Random.Range(20f, 160f) * Mathf.Deg2Rad;
            confettiVelocity[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(700f, 1300f) * s;
            confettiSpin[i] = Random.Range(-540f, 540f);
            confetti[i].position = medal.position;
            confetti[i].gameObject.SetActive(true);
        }

        float total = 0.6f + celebrationHold;
        Color flashColor = flash.color;
        for (float t = 0f; t < total && !skipCelebration; t += Time.unscaledDeltaTime)
        {
            flashColor.a = 0.8f * (1f - Mathf.Clamp01(t / 0.3f));
            flash.color = flashColor;
            medal.localScale = Vector3.one * EaseOutBack(Mathf.Clamp01(t / 0.45f), 2.2f);
            medal.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 5f) * 7f * Mathf.Exp(-t * 1.2f));
            recordLabel.localScale = Vector3.one * EaseOutBack(Mathf.Clamp01((t - 0.15f) / 0.35f), 2.4f);

            Vector3 origin = medal.position;
            float gravity = -1800f * s;
            for (int i = 0; i < confetti.Length; i++)
            {
                Vector2 p = confettiVelocity[i] * t + 0.5f * gravity * t * t * Vector2.up;
                confetti[i].position = origin + (Vector3)p;
                confetti[i].localRotation = Quaternion.Euler(0f, 0f, confettiSpin[i] * t);
            }
            yield return null;
        }

        for (float t = 0f; t < 0.25f; t += Time.unscaledDeltaTime)
        {
            celebration.alpha = 1f - t / 0.25f;
            yield return null;
        }
        celebration.gameObject.SetActive(false);
        newBadge.SetActive(true);
        yield return Pop((RectTransform)newBadge.transform, 0.22f);
    }

    private IEnumerator Coins()
    {
        coinsRow.gameObject.SetActive(true);
        runCoinsText.SetText("+{0}", runCoins);
        yield return Pop(coinsRow, 0.2f);
        if (runCoins <= 0) yield break;

        int count = Mathf.Min(flyers.Length, runCoins);
        arrived = 0;
        flying = count;
        for (int i = 0; i < count; i++) StartCoroutine(Fly(i, count));
        while (flying > 0) yield return null;
    }

    private IEnumerator Fly(int index, int count)
    {
        var image = flyers[index];
        var rt = image.rectTransform;
        image.gameObject.SetActive(true);
        float s = CanvasScale;
        Vector3 start = coinsSource.position;
        Vector3 scatter = start + (Vector3)(Random.insideUnitCircle * 90f * s);
        Vector3 control = Vector3.Lerp(scatter, totalTarget.position, 0.5f) + Vector3.down * Random.Range(-160f, 160f) * s;
        float spinOffset = Random.value;

        // Abanico desde la fila COINS
        for (float t = 0f; t < 0.18f; t += Time.unscaledDeltaTime)
        {
            float k = EaseOutCubic(t / 0.18f);
            rt.position = Vector3.Lerp(start, scatter, k);
            rt.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, k);
            Spin(image, t, spinOffset);
            yield return null;
        }
        for (float t = 0f; t < index * 0.06f; t += Time.unscaledDeltaTime)
        {
            Spin(image, t, spinOffset);
            yield return null;
        }

        // Vuelo curvo que acelera hacia el contador
        const float flight = 0.5f;
        for (float t = 0f; t < flight; t += Time.unscaledDeltaTime)
        {
            float k = t / flight;
            k *= k;
            Vector3 end = totalTarget.position;
            rt.position = Vector3.Lerp(Vector3.Lerp(scatter, control, k), Vector3.Lerp(control, end, k), k);
            rt.localScale = Vector3.one * Mathf.Lerp(1f, 0.7f, k);
            Spin(image, t, spinOffset);
            yield return null;
        }

        image.gameObject.SetActive(false);
        arrived++;
        shownTotal = totalCoins - runCoins + Mathf.RoundToInt(runCoins * (float)arrived / count);
        totalText.SetText("{0}", shownTotal);
        totalPunch = 0f;
        if (AudioManager.instance != null) AudioManager.instance.PlayAudio(coinClip);
        flying--;
    }

    private void LateUpdate()
    {
        // Golpe del contador total en cada llegada (se reinicia con cada moneda)
        if (totalPunch < 0f) return;
        totalPunch += Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(totalPunch / 0.16f);
        totalCounter.localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(k * Mathf.PI));
        if (k >= 1f) totalPunch = -1f;
    }

    /// <summary>Salta al estado final. Si hubo récord y todavía no se celebró, la celebración se muestra igual.</summary>
    public void CompleteNow()
    {
        StopAllCoroutines();
        title.localScale = Vector3.one;
        card.localScale = Vector3.one;
        scoreText.SetText("{0}", finalScore);
        scoreText.rectTransform.localScale = Vector3.one;
        coinsRow.gameObject.SetActive(true);
        coinsRow.localScale = Vector3.one;
        runCoinsText.SetText("+{0}", runCoins);
        foreach (var f in flyers) f.gameObject.SetActive(false);
        flying = 0;
        shownTotal = totalCoins;
        totalText.SetText("{0}", shownTotal);
        totalPunch = -1f;
        totalCounter.localScale = Vector3.one;
        SetActions(1f, true);
        finished = true;

        if (record && !celebrationShown) StartCoroutine(Celebrate());
        else
        {
            celebration.gameObject.SetActive(false);
            newBadge.SetActive(record);
            newBadge.transform.localScale = Vector3.one;
        }
    }

    // ------------------------------------------------------------------ utilidades

    private float CanvasScale => ((RectTransform)transform).lossyScale.x;

    private void Spin(Image image, float t, float offset)
    {
        if (spinFrames == null || spinFrames.Length == 0) return;
        int frame = (int)((t / 0.07f) + offset * spinFrames.Length) % spinFrames.Length;
        image.sprite = spinFrames[frame];
    }

    private IEnumerator ShowActions(float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            SetActions(t / duration, false);
            yield return null;
        }
        SetActions(1f, true);
    }

    private void SetActions(float alpha, bool interactive)
    {
        actions.alpha = alpha;
        actions.interactable = interactive;
        actions.blocksRaycasts = interactive;
    }

    private static IEnumerator Pop(RectTransform target, float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            target.localScale = Vector3.one * Mathf.LerpUnclamped(0.4f, 1f, EaseOutBack(t / duration, 2.2f));
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    private static IEnumerator Punch(RectTransform target, float peak, float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            target.localScale = Vector3.one * (1f + (peak - 1f) * Mathf.Sin(t / duration * Mathf.PI));
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    private static float EaseOutCubic(float k)
    {
        k = 1f - Mathf.Clamp01(k);
        return 1f - k * k * k;
    }

    private static float EaseOutBack(float k, float overshoot)
    {
        k = Mathf.Clamp01(k) - 1f;
        return 1f + (overshoot + 1f) * k * k * k + overshoot * k * k;
    }
}
