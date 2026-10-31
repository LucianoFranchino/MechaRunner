using System;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager: MonoBehaviour
{
    public static AudioManager instance;

    [Serializable]
    public class ClipVolume
    {
        public AudioClip clip;
        [Range(0f, 1.5f)] public float volume = 1f;
    }

    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip backgroundMusic;

    [Header("Balance de efectos")]
    [Tooltip("Volumen relativo de cada efecto (los que no están suenan a 1). Se multiplica por el volumen de sfxSource.")]
    [SerializeField] private ClipVolume[] clipVolumes = Array.Empty<ClipVolume>();

    [Header("Monedas: tono progresivo")]
    [Tooltip("Tono de la primera moneda de una racha (1 = tono original).")]
    [SerializeField] private float coinBasePitch = 0.85f;
    [Tooltip("Semitonos de cada moneda seguida: escala mayor ascendente hasta una octava.")]
    [SerializeField] private int[] coinScale = { 0, 2, 4, 5, 7, 9, 11, 12 };
    [Tooltip("Segundos máximos entre monedas para que la racha siga subiendo.")]
    [SerializeField] private float coinChainWindow = 0.4f;

    public bool SfxMuted { get; private set; }
    public bool MusicMuted { get; private set; }

    private readonly Dictionary<AudioClip, float> volumes = new();
    // Varias fuentes para las monedas: cambiar el tono de una no desafina la cola de la anterior
    private AudioSource[] coinSources;
    private int nextCoinSource;
    private int coinStep;
    private float lastCoinTime = float.NegativeInfinity;

    /// <summary>Paso de la racha de monedas actual (0 = primera). Para tests.</summary>
    public int CoinStep => coinStep;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Vive con la escena (cuelga de "Managers", así que DontDestroyOnLoad no
        // tenía efecto). El mute se guarda en SaveSystem (settings).
        instance = this;

        var settings = SaveSystem.Data.settings;
        SfxMuted = settings.sfxMuted;
        MusicMuted = settings.musicMuted;
        sfxSource.mute = SfxMuted;

        foreach (var cv in clipVolumes)
            if (cv != null && cv.clip != null) volumes[cv.clip] = cv.volume;

        coinSources = new AudioSource[3];
        for (int i = 0; i < coinSources.Length; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
            source.volume = sfxSource.volume;
            source.priority = sfxSource.priority;
            source.mute = SfxMuted;
            coinSources[i] = source;
        }

        // Una sola pista de música: si viene sonando desde el menú se adopta esa
        // (sin cortes ni copias superpuestas); si se arrancó directo en esta escena, se usa la propia.
        var persistent = MusicTransition.Source;
        if (persistent != null && persistent != musicSource)
        {
            persistent.volume = musicSource.volume;
            musicSource.Stop();
            musicSource.enabled = false;
            musicSource = persistent;
            if (backgroundMusic != null && musicSource.clip != backgroundMusic)
            {
                musicSource.clip = backgroundMusic;
                musicSource.Play();
            }
        }
        else
        {
            musicSource.loop = true;
            if (backgroundMusic != null)
            {
                musicSource.clip = backgroundMusic;
                musicSource.Play();
            }
        }
        musicSource.mute = MusicMuted;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private float VolumeOf(AudioClip clip) => volumes.TryGetValue(clip, out float v) ? v : 1f;

    public void PlayAudio(AudioClip sound)
    {
        if (sound != null) sfxSource.PlayOneShot(sound, VolumeOf(sound));
    }

    /// <summary>
    /// Moneda con tono progresivo: cada moneda juntada enseguida de la anterior sube un grado de la escala;
    /// si pasa un rato sin juntar, la racha vuelve a empezar desde el tono grave.
    /// </summary>
    public void PlayCoin(AudioClip sound)
    {
        if (sound == null) return;
        float now = Time.unscaledTime; // también en el Game Over (tiempo congelado)
        coinStep = now - lastCoinTime <= coinChainWindow ? Mathf.Min(coinStep + 1, coinScale.Length - 1) : 0;
        lastCoinTime = now;

        var source = coinSources[nextCoinSource];
        nextCoinSource = (nextCoinSource + 1) % coinSources.Length;
        source.pitch = coinBasePitch * Mathf.Pow(2f, coinScale[coinStep] / 12f);
        source.PlayOneShot(sound, VolumeOf(sound));
    }

    public void ToggleSfxMute()
    {
        SfxMuted = !SfxMuted;
        sfxSource.mute = SfxMuted;
        foreach (var source in coinSources) source.mute = SfxMuted;
        SaveSystem.Data.settings.sfxMuted = SfxMuted;
        SaveSystem.Save(); // cambio poco frecuente: se guarda en el momento
    }

    public void ToggleMusicMute()
    {
        MusicMuted = !MusicMuted;
        musicSource.mute = MusicMuted;
        SaveSystem.Data.settings.musicMuted = MusicMuted;
        SaveSystem.Save();
    }
}
