using UnityEngine;

/// <summary>
/// Configuración de arranque para que el juego rinda parejo en celulares nuevos
/// y viejos. Corre sola antes de cargar la primera escena (no hay que ponerla en
/// ningún objeto).
/// </summary>
public static class AppBootstrap
{
    // Alto máximo del framebuffer en landscape. Un juego 2D no gana nada visible
    // renderizando a 1440p, y en celulares con pantallas grandes pero GPU modesta
    // es la diferencia entre 60 y 30 fps.
    private const int MaxRenderHeight = 1080;
    private const int LowEndRenderHeight = 720;
    private const int LowEndMemoryMB = 3072;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // En Android Unity corre a 30 fps salvo que se pida otra cosa.
        // vSyncCount = 0 hace que se respete targetFrameRate en todas las plataformas.
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        // Que la pantalla no se apague en medio de una partida (se juega sin tocar seguido)
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

#if UNITY_ANDROID && !UNITY_EDITOR
        CapRenderResolution();
#endif
    }

    private static void CapRenderResolution()
    {
        int maxHeight = SystemInfo.systemMemorySize > 0 && SystemInfo.systemMemorySize <= LowEndMemoryMB
            ? LowEndRenderHeight
            : MaxRenderHeight;

        int width = Screen.width;
        int height = Screen.height;
        int shortSide = Mathf.Min(width, height);
        if (shortSide <= maxHeight) return;

        float scale = (float)maxHeight / shortSide;
        Screen.SetResolution(Mathf.RoundToInt(width * scale), Mathf.RoundToInt(height * scale), FullScreenMode.FullScreenWindow);
    }
}
