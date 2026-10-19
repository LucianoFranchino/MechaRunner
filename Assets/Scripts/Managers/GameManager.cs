using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Playing, Paused, GameOver }

/// <summary>
/// Estado de la partida (jugando, en pausa, terminada) y sus transiciones: tiempo, puntaje,
/// guardado (SaveSystem) y cambio de escena. Las pantallas (pausa, resultados, controles)
/// solo escuchan StateChanged; el input y las armas consultan IsPlaying.
/// Reiniciar recarga la escena, así pools, dificultad y puntaje vuelven a su estado inicial.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    /// <summary>Estático para consultarlo sin referencia. Vuelve a Playing con cada escena.</summary>
    public static GameState State { get; private set; } = GameState.Playing;
    public static bool IsPlaying => State == GameState.Playing;
    public static bool IsPaused => State == GameState.Paused;
    public static bool IsGameOver => State == GameState.GameOver;

    public event Action<GameState> StateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        State = GameState.Playing;
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        // Que una escena sin GameManager (menú, tutorial) no herede la pausa o el Game Over
        State = GameState.Playing;
    }

    private void Update()
    {
        // El botón/gesto "Atrás" de Android llega como Escape: pausa o reanuda
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (IsPaused) Resume();
            else Pause();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        // Pausar automáticamente al minimizar la app en el celular
        if (paused) Pause();
    }

    public void Pause()
    {
        if (!IsPlaying) return;
        Time.timeScale = 0f;
        SetScoring(false);
        SaveSystem.SaveIfDirty(); // monedas de la partida en curso
        SetState(GameState.Paused);
    }

    public void Resume()
    {
        if (!IsPaused) return;
        Time.timeScale = 1f;
        SetScoring(true);
        SetState(GameState.Playing);
    }

    /// <summary>Fin de la partida: congela el juego y guarda el récord antes de avisar a las pantallas.</summary>
    public void GameOver()
    {
        if (IsGameOver) return;
        SetScoring(false);
        if (ScoreManager.instance != null) ScoreManager.instance.Save();
        else SaveSystem.SaveIfDirty();
        Time.timeScale = 0f;
        SetState(GameState.GameOver);
    }

    public void Restart()
    {
        SaveSystem.SaveIfDirty();
        Time.timeScale = 1f;
        var scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
        // En el Editor se puede jugar una escena desactivada en Build Settings (no tiene índice)
        if (scene.buildIndex < 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        SceneManager.LoadScene(scene.buildIndex);
    }

    public void GoToMenu()
    {
        SaveSystem.SaveIfDirty();
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    private static void SetScoring(bool increasing)
    {
        if (ScoreManager.instance != null) ScoreManager.instance.scoreIncreasing = increasing;
    }

    private void SetState(GameState state)
    {
        State = state;
        StateChanged?.Invoke(state);
    }
}
