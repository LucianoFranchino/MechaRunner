using UnityEngine;

public class DeadthCreen : MonoBehaviour
{
    public static bool IsGameOver { get; private set; }

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
        gameObject.SetActive(true);
        Time.timeScale = 0f;
    }
}
