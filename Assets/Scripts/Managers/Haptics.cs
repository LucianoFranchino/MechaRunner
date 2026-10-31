using UnityEngine;

/// <summary>
/// Vibración del celular (Android). Usa el Vibrator nativo para poder hacer pulsos cortos y suaves
/// (Handheld.Vibrate siempre vibra ~0,5 s a fondo). Android 8+: duración e intensidad; Android 7: solo duración.
/// En el Editor y otras plataformas no hace nada. El permiso VIBRATE está en Plugins/Android/Haptics.androidlib.
/// </summary>
public static class Haptics
{
    public static bool Enabled = true;

#if UNITY_ANDROID && !UNITY_EDITOR
    private static bool initialized;
    private static AndroidJavaObject vibrator;
    private static AndroidJavaClass vibrationEffect;
    private static int sdk;

    private static void Initialize()
    {
        initialized = true;
        try
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            using var version = new AndroidJavaClass("android.os.Build$VERSION");
            sdk = version.GetStatic<int>("SDK_INT");
            if (sdk >= 26) vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Vibración no disponible: " + e.Message);
            vibrator = null;
        }
    }
#endif

    /// <param name="milliseconds">Duración del pulso.</param>
    /// <param name="amplitude">Intensidad 1-255 (Android 8+).</param>
    public static void Vibrate(long milliseconds, int amplitude)
    {
        if (!Enabled || milliseconds <= 0) return;
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!initialized) Initialize();
        if (vibrator == null) return;
        if (vibrationEffect != null)
        {
            using var effect = vibrationEffect.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, Mathf.Clamp(amplitude, 1, 255));
            vibrator.Call("vibrate", effect);
        }
        else
        {
            vibrator.Call("vibrate", milliseconds);
        }
#endif
    }
}
