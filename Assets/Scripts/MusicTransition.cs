using UnityEngine;

/// <summary>
/// Música que nace en el menú y sigue sonando entre escenas (DontDestroyOnLoad).
/// Es la única pista de música mientras exista: AudioManager la adopta en vez de
/// reproducir una segunda copia (antes sonaban dos superpuestas y el mute no la apagaba).
/// </summary>
public class MusicTransition : MonoBehaviour
{
    private static MusicTransition instance;

    /// <summary>Fuente de música persistente, o null si no se pasó por el menú.</summary>
    public static AudioSource Source => instance != null ? instance.source : null;

    private AudioSource source;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            source = GetComponent<AudioSource>();
            // Respetar el mute de música guardado también en el menú
            if (source != null) source.mute = PlayerPrefs.GetInt(AudioManager.MusicMutedKey, 0) == 1;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
