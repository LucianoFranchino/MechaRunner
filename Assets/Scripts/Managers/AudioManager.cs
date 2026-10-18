using UnityEngine;

public class AudioManager: MonoBehaviour
{
    public static AudioManager instance;

    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip backgroundMusic;

    public bool SfxMuted { get; private set; }
    public bool MusicMuted { get; private set; }

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

    public void PlayAudio(AudioClip sound)
    {
        if (sound != null) sfxSource.PlayOneShot(sound);
    }

    public void ToggleSfxMute()
    {
        SfxMuted = !SfxMuted;
        sfxSource.mute = SfxMuted;
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
