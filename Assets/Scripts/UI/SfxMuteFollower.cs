using UnityEngine;

/// <summary>
/// Efectos de sonido de escenas sin AudioManager (menú, tutorial): respetan el mute
/// de sonido guardado y se actualizan cuando se cambia desde OPTIONS.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class SfxMuteFollower : MonoBehaviour
{
    private AudioSource source;

    private void Awake() => source = GetComponent<AudioSource>();

    private void OnEnable()
    {
        Apply();
        SaveSystem.Saved += Apply;
    }

    private void OnDisable() => SaveSystem.Saved -= Apply;

    private void Apply() => source.mute = SaveSystem.Data.settings.sfxMuted;
}
