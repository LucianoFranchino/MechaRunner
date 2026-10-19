using UnityEngine.SceneManagement;

/// <summary>
/// Carga escenas en los tests por su ruta, sin depender de que estén activas en Build Settings
/// (se puede desactivar una escena para la build y los tests la siguen usando).
/// </summary>
public static class TestScenes
{
    public static void Load(string sceneName)
    {
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
            "Assets/Scenes/" + sceneName + ".unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(sceneName);
#endif
    }
}
