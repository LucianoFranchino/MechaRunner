using UnityEngine;

public class DeadthCreen : MonoBehaviour
{
    public static bool IsGameOver { get; private set; }

    [Tooltip("Controles táctiles de la partida (FIRE, pausa): se ocultan en la pantalla de resultados.")]
    [SerializeField] private GameObject gameplayControls;

    private void Awake()
    {
        IsGameOver = false;
    }

    private void OnDestroy()
    {
        // Al salir de la escena (Restart/Menu) el estado estático no debe arrastrarse
        IsGameOver = false;
    }

    void Start()
    {
        if (!IsGameOver) gameObject.SetActive(false);
    }

    public void ToggleEndMenu()
    {
        IsGameOver = true;
        if (gameplayControls != null) gameplayControls.SetActive(false);
        gameObject.SetActive(true);
        Time.timeScale = 0f;
    }
}
