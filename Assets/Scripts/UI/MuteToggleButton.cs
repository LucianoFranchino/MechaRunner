using UnityEngine;

public enum MuteChannel { Sfx, Music }

/// <summary>
/// Botón de mute genérico: activa/desactiva dos GameObjects distintos
/// (uno por estado) en vez de intercambiar un sprite, para que cada ícono
/// pueda tener su propio diseño (shadow, escala, etc.) sin restricciones.
/// En escenas sin AudioManager (menú) cambia el ajuste guardado directamente.
/// </summary>
public class MuteToggleButton : MonoBehaviour
{
    [SerializeField] private MuteChannel channel;
    [SerializeField] private GameObject unmutedIcon;
    [SerializeField] private GameObject mutedIcon;

    private bool Muted
    {
        get
        {
            var audio = AudioManager.instance;
            if (audio != null) return channel == MuteChannel.Sfx ? audio.SfxMuted : audio.MusicMuted;
            var settings = SaveSystem.Data.settings;
            return channel == MuteChannel.Sfx ? settings.sfxMuted : settings.musicMuted;
        }
    }

    private void OnEnable() => UpdateIcon(Muted);

    public void Toggle()
    {
        var audio = AudioManager.instance;
        if (audio != null)
        {
            if (channel == MuteChannel.Sfx) audio.ToggleSfxMute();
            else audio.ToggleMusicMute();
        }
        else
        {
            // Menú: no hay AudioManager. Las fuentes con SfxMuteFollower se actualizan al guardar.
            var settings = SaveSystem.Data.settings;
            if (channel == MuteChannel.Sfx)
            {
                settings.sfxMuted = !settings.sfxMuted;
            }
            else
            {
                settings.musicMuted = !settings.musicMuted;
                if (MusicTransition.Source != null) MusicTransition.Source.mute = settings.musicMuted;
            }
            SaveSystem.Save();
        }

        UpdateIcon(Muted);
    }

    private void UpdateIcon(bool muted)
    {
        if (unmutedIcon != null) unmutedIcon.SetActive(!muted);
        if (mutedIcon != null) mutedIcon.SetActive(muted);
    }
}
