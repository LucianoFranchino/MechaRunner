using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Panel OPTIONS del menú principal (música y sonido con MuteToggleButton).
/// </summary>
public class OptionsScreen : MonoBehaviour
{
    private void Update()
    {
        // Atrás de Android / Escape cierra el panel
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) Close();
    }

    public void Open() => gameObject.SetActive(true);

    public void Close() => gameObject.SetActive(false);
}
