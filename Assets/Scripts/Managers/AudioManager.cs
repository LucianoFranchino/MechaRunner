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
    [Tooltip("Al llegar arriba, la racha sube y baja entre estas últimas notas de la escala (nunca más agudo que el tope).")]
    [SerializeField, Min(2)] private int coinTopNotes = 4;

    [Header("Disparo: variación de tono")]
    [Tooltip("Tonos posibles del disparo (levemente graves); se elige uno al azar sin repetir el anterior.")]
    [SerializeField] private float[] shotPitches = { 0.92f, 0.95f, 0.98f };

    public bool SfxMuted { get; private set; }
    public bool MusicMuted { get; private set; }

    private readonly Dictionary<AudioClip, float> volumes = new();
    // Varias fuentes para las monedas: cambiar el tono de una no desafina la cola de la anterior
    private AudioSource[] coinSources;
    private int nextCoinSource;
    // Fuentes propias del disparo: el cambio de tono no afecta a los demás efectos
    private AudioSource[] shotSources;
    private int nextShotSource;
    private int lastShotPitch = -1;

    /// <summary>Tono del último disparo. Para tests.</summary>
    public float LastShotPitch { get; private set; } = 1f;
    private int coinStep;
    private float lastCoinTime = float.NegativeInfinity;

    /// <summary>Paso de la racha de monedas actual (0 = primera; sigue creciendo pasado el tope). Para tests.</summary>
    public int CoinStep => coinStep;

    /// <summary>
    /// Nota de la escala para el paso de la racha: sube hasta el tope y después va y viene entre las
    /// últimas notas (arpegio), así una lluvia de monedas no repite la más aguda.
    /// </summary>
    public static int CoinNoteIndex(int step, int scaleLength, int topNotes)
    {
        if (step < scaleLength) return step;
        int top = Mathf.Clamp(topNotes, 2, scaleLength);
        int span = top - 1;                                   // ej. 4 notas: 3 pasos entre la más baja y el tope
        int k = (step - (scaleLength - 1)) % (2 * span);      // 0 = tope, baja span pasos y vuelve a subir
        int down = k <= span ? k : 2 * span - k;
        return scaleLength - 1 - down;
    }

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

        coinSources = CreateSources(3);
        shotSources = CreateSources(3);

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

    private AudioSource[] CreateSources(int count)
    {
        var sources = new AudioSource[count];
        for (int i = 0; i < count; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
            source.volume = sfxSource.volume;
            source.priority = sfxSource.priority;
            source.mute = SfxMuted;
            sources[i] = source;
        }
        return sources;
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
        coinStep = now - lastCoinTime <= coinChainWindow ? coinStep + 1 : 0;
        lastCoinTime = now;

        var source = coinSources[nextCoinSource];
        nextCoinSource = (nextCoinSource + 1) % coinSources.Length;
        source.pitch = coinBasePitch * Mathf.Pow(2f, coinScale[CoinNoteIndex(coinStep, coinScale.Length, coinTopNotes)] / 12f);
        source.PlayOneShot(sound, VolumeOf(sound));
    }

    /// <summary>Disparo con uno de los tonos de shotPitches (al azar, sin repetir el anterior).</summary>
    public void PlayShot(AudioClip sound)
    {
        if (sound == null) return;
        int index = 0;
        if (shotPitches.Length > 1)
        {
            index = UnityEngine.Random.Range(0, shotPitches.Length - 1);
            if (index >= lastShotPitch && lastShotPitch >= 0) index++;
        }
        lastShotPitch = index;
        LastShotPitch = shotPitches.Length > 0 ? shotPitches[index] : 1f;

        var source = shotSources[nextShotSource];
        nextShotSource = (nextShotSource + 1) % shotSources.Length;
        source.pitch = LastShotPitch;
        source.PlayOneShot(sound, VolumeOf(sound));
    }

    public void ToggleSfxMute()
    {
        SfxMuted = !SfxMuted;
        sfxSource.mute = SfxMuted;
        foreach (var source in coinSources) source.mute = SfxMuted;
        foreach (var source in shotSources) source.mute = SfxMuted;
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
