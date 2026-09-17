using UnityEngine;

public enum MuteChannel { Sfx, Music }

/// <summary>
/// Botón de mute genérico: activa/desactiva dos GameObjects distintos
/// (uno por estado) en vez de intercambiar un sprite, para que cada ícono
/// pueda tener su propio diseño (shadow, escala, etc.) sin restricciones.
/// </summary>
public class MuteToggleButton : MonoBehaviour
{
    [SerializeField] private MuteChannel channel;
    [SerializeField] private GameObject unmutedIcon;
    [SerializeField] private GameObject mutedIcon;

    private void Start()
    {
        bool muted = channel == MuteChannel.Sfx
            ? AudioManager.instance.SfxMuted
            : AudioManager.instance.MusicMuted;

        UpdateIcon(muted);
    }

    public void Toggle()
    {
        bool muted;
        if (channel == MuteChannel.Sfx)
        {
            AudioManager.instance.ToggleSfxMute();
            muted = AudioManager.instance.SfxMuted;
        }
        else
        {
            AudioManager.instance.ToggleMusicMute();
            muted = AudioManager.instance.MusicMuted;
        }

        UpdateIcon(muted);
    }

    private void UpdateIcon(bool muted)
    {
        if (unmutedIcon != null) unmutedIcon.SetActive(!muted);
        if (mutedIcon != null) mutedIcon.SetActive(muted);
    }
}