using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused { get; private set; }

    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private ScoreManager scoreManager;

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
        if (scoreManager != null) scoreManager.scoreIncreasing = true;
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        GameIsPaused = true;
        if (scoreManager != null) scoreManager.scoreIncreasing = false;
    }
}
