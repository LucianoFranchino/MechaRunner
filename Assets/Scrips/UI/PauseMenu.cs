using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused { get; private set; }

    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private ScoreManager scoreManager;
    [Tooltip("Controles táctiles de la partida (FIRE, pausa): se ocultan mientras está la pausa.")]
    [SerializeField] private GameObject gameplayControls;

    private void Awake()
    {
        // Es estático: si se reinició desde la pausa quedaría en true.
        GameIsPaused = false;
    }

    private void OnDestroy()
    {
        GameIsPaused = false;
    }

    private void Update()
    {
        // El botón/gesto "Atrás" de Android llega como Escape: pausa o reanuda
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (GameIsPaused) Resume(); else Pause();
        }
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
        if (gameplayControls != null) gameplayControls.SetActive(true);
        Time.timeScale = 1f;
        GameIsPaused = false;
        if (scoreManager != null) scoreManager.scoreIncreasing = true;
    }

    public void Pause()
    {
        if (DeadthCreen.IsGameOver) return;

        pauseMenuUI.SetActive(true);
        if (gameplayControls != null) gameplayControls.SetActive(false);
        Time.timeScale = 0f;
        GameIsPaused = true;
        if (scoreManager != null) scoreManager.scoreIncreasing = false;
    }
}
