using UnityEngine;
using UnityEngine.VFX;

public class AudioManager: MonoBehaviour
{
    public static AudioManager instance;
    private AudioSource audioSource;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }else
        {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
    }

    public void PlayAudio(AudioClip sound)
    {
        audioSource.PlayOneShot(sound);
    }
}
