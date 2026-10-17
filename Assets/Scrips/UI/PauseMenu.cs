using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused { get; private set; }

    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private ScoreManager scoreManager;

    private void Awake()
    {
        // Es estático: si se reinició desde la pausa quedaría en true.
        GameIsPaused = false;
    }

    private void OnDestroy()
    {
        GameIsPaused = false;
    }

    private void OnApplicationPause(bool paused)
    {
        // Pausar automáticamente al minimizar la app en el celular
        if (paused && !GameIsPaused) Pause();
    }

    public void Resume()
    {
        if (DeadthCreen.IsGameOver) return;

        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
        if (scoreManager != null) scoreManager.scoreIncreasing = true;
    }

    public void Pause()
    {
        if (DeadthCreen.IsGameOver) return;

        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        GameIsPaused = true;
        if (scoreManager != null) scoreManager.scoreIncreasing = false;
    }
}
