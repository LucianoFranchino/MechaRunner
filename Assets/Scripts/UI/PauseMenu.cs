using UnityEngine;

/// <summary>
/// Vista del estado de la partida: muestra el panel de pausa en Paused y los controles
/// táctiles (FIRE, pausa) solo mientras se juega. Los botones llaman a GameManager.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuUI;
    [Tooltip("Controles táctiles de la partida (FIRE, pausa): solo visibles en Playing.")]
    [SerializeField] private GameObject gameplayControls;

    private GameManager manager;

    private void Start()
    {
        manager = GameManager.Instance;
        if (manager != null) manager.StateChanged += Apply;
        Apply(GameManager.State);
    }

    private void OnDestroy()
    {
        if (manager != null) manager.StateChanged -= Apply;
    }

    private void Apply(GameState state)
    {
        if (pauseMenuUI != null) pauseMenuUI.SetActive(state == GameState.Paused);
        if (gameplayControls != null) gameplayControls.SetActive(state == GameState.Playing);
    }
}
