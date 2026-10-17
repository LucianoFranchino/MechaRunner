using UnityEngine;

public class AudioManager: MonoBehaviour
{
    public static AudioManager instance;

    private const string SfxMutedKey = "SfxMuted";
    private const string MusicMutedKey = "MusicMuted";

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
        // tenía efecto). El mute se guarda en PlayerPrefs para sobrevivir al reinicio.
        instance = this;

        SfxMuted = PlayerPrefs.GetInt(SfxMutedKey, 0) == 1;
        MusicMuted = PlayerPrefs.GetInt(MusicMutedKey, 0) == 1;
        sfxSource.mute = SfxMuted;
        musicSource.mute = MusicMuted;

        musicSource.loop = true;
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }
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
        PlayerPrefs.SetInt(SfxMutedKey, SfxMuted ? 1 : 0);
    }

    public void ToggleMusicMute()
    {
        MusicMuted = !MusicMuted;
        musicSource.mute = MusicMuted;
        PlayerPrefs.SetInt(MusicMutedKey, MusicMuted ? 1 : 0);
    }
}
