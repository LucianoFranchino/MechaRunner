using UnityEngine;
#if !UNITY_EDITOR
using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using Firebase.Extensions;
#endif

/// <summary>
/// Arranque de Firebase (Analytics y Crashlytics). Corre solo antes de la primera escena, sin objeto en
/// ninguna escena. En el Editor no hace nada (no ensucia Play ni los tests); en el dispositivo comprueba
/// Google Play Services y, si está todo, activa Crashlytics para excepciones no capturadas.
/// Analytics registra sesiones, retención y dispositivos sin código adicional.
/// </summary>
public static class FirebaseService
{
    /// <summary>Firebase listo para usar (siempre false en el Editor).</summary>
    public static bool Ready { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
#if !UNITY_EDITOR
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.Result != DependencyStatus.Available)
            {
                Debug.LogError("Firebase no disponible: " + (task.IsFaulted ? task.Exception?.ToString() : task.Result.ToString()));
                return;
            }

            _ = FirebaseApp.DefaultInstance;
            Crashlytics.ReportUncaughtExceptionsAsFatal = true;
            FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
            Ready = true;
        });
#endif
    }
}
