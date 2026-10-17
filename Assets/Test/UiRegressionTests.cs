using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Tests del rediseÃ±o de UI: cableado de botones, pausa/resultados con los controles,
/// rÃ©cord nuevo y una sola pista de mÃºsica del menÃº al juego.
/// Guardan y restauran los PlayerPrefs del jugador.
/// </summary>
public class UiRegressionTests
{
    private static readonly string[] IntKeys = { CoinCounter.CoinsKey, AudioManager.SfxMutedKey, AudioManager.MusicMutedKey };
    private readonly System.Collections.Generic.Dictionary<string, int> savedInts = new();
    private bool hadHighscore;
    private float savedHighscore;

    [SetUp]
    public void SavePrefs()
    {
        savedInts.Clear();
        foreach (var key in IntKeys)
            if (PlayerPrefs.HasKey(key)) savedInts[key] = PlayerPrefs.GetInt(key);
        hadHighscore = PlayerPrefs.HasKey(ScoreManager.HighscoreKey);
        savedHighscore = PlayerPrefs.GetFloat(ScoreManager.HighscoreKey);
    }

    [TearDown]
    public void RestorePrefs()
    {
        foreach (var key in IntKeys)
            if (savedInts.TryGetValue(key, out int value)) PlayerPrefs.SetInt(key, value); else PlayerPrefs.DeleteKey(key);
        if (hadHighscore) PlayerPrefs.SetFloat(ScoreManager.HighscoreKey, savedHighscore); else PlayerPrefs.DeleteKey(ScoreManager.HighscoreKey);
        PlayerPrefs.Save();
        Time.timeScale = 1f;
        // La mÃºsica del menÃº es DontDestroyOnLoad: no debe filtrarse a otros tests
        if (MusicTransicion.Source != null) Object.Destroy(MusicTransicion.Source.gameObject);
    }

    private static IEnumerator Load(string scene)
    {
        SceneManager.LoadScene(scene);
        yield return null;
        yield return null;
    }

    private static Transform GameCanvas => GameObject.Find("Canvas").transform;

    [UnityTest]
    public IEnumerator UI_TodasLasEscenasTienenBotonesConectadosYSinTextoLegacy([Values("MainMenu", "Tutorial", "Game")] string scene)
    {
        yield return Load(scene);

        var canvas = GameObject.Find("Canvas").GetComponent<Canvas>();
        Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode, "La UI debe ir en Overlay (las luces 2D no la tapan)");
        var scaler = canvas.GetComponent<CanvasScaler>();
        Assert.AreEqual(new Vector2(1920, 1080), scaler.referenceResolution);

        var buttons = canvas.GetComponentsInChildren<Button>(true);
        Assert.Greater(buttons.Length, 0);
        foreach (var b in buttons)
        {
            Assert.Greater(b.onClick.GetPersistentEventCount(), 0, b.name + " no tiene acciÃ³n");
            for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                Assert.IsNotNull(b.onClick.GetPersistentTarget(i), b.name + " apunta a un objeto inexistente");
        }
        Assert.AreEqual(0, canvas.GetComponentsInChildren<Text>(true).Length, "Quedaron textos legacy");
    }

    [UnityTest]
    public IEnumerator UI_PausaOcultaLosControlesYResumeLosDevuelve()
    {
        yield return Load("Game");
        var controls = GameCanvas.Find("Controls").gameObject;
        var pause = Object.FindFirstObjectByType<PauseMenu>();

        pause.Pause();
        Assert.IsTrue(PauseMenu.GameIsPaused);
        Assert.IsFalse(controls.activeSelf, "FIRE/pausa siguen visibles detrÃ¡s de la pausa");
        Assert.IsTrue(GameCanvas.Find("PauseMenu").gameObject.activeSelf);

        pause.Resume();
        Assert.IsTrue(controls.activeSelf);
        Assert.IsFalse(GameCanvas.Find("PauseMenu").gameObject.activeSelf);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [UnityTest]
    public IEnumerator UI_GameOverMuestraPuntajeYRecordNuevo()
    {
        PlayerPrefs.SetFloat(ScoreManager.HighscoreKey, 10f);
        yield return Load("Game");
        foreach (var spawner in Object.FindObjectsByType<EntitySpawner>(FindObjectsSortMode.None)) spawner.enabled = false;

        ScoreManager.instance.scoreCount = 50f;
        yield return null; // el HUD refleja el puntaje
        Object.FindFirstObjectByType<PlayerHealth>().PlayerDamage(999);

        var results = GameCanvas.Find("GameOver");
        Assert.IsTrue(results.gameObject.activeSelf);
        Assert.IsFalse(GameCanvas.Find("Controls").gameObject.activeSelf, "Los controles quedaron sobre los resultados");
        Assert.AreEqual("50", results.Find("Results/ScoreValue").GetComponent<TMP_Text>().text);
        Assert.AreEqual("50", results.Find("Results/BestValue").GetComponent<TMP_Text>().text);
        Assert.IsTrue(results.Find("Results/NewBadge").gameObject.activeSelf, "Falta la insignia de rÃ©cord nuevo");
    }

    [UnityTest]
    public IEnumerator UI_SinRecordNoApareceLaInsignia()
    {
        PlayerPrefs.SetFloat(ScoreManager.HighscoreKey, 9999f);
        yield return Load("Game");
        foreach (var spawner in Object.FindObjectsByType<EntitySpawner>(FindObjectsSortMode.None)) spawner.enabled = false;

        ScoreManager.instance.scoreCount = 50f;
        Object.FindFirstObjectByType<PlayerHealth>().PlayerDamage(999);

        Assert.IsFalse(GameCanvas.Find("GameOver/Results/NewBadge").gameObject.activeSelf);
        Assert.AreEqual("9999", GameCanvas.Find("GameOver/Results/BestValue").GetComponent<TMP_Text>().text);
    }

    [UnityTest]
    public IEnumerator Audio_UnaSolaMusicaDelMenuAlJuegoYElMuteLaApaga()
    {
        PlayerPrefs.SetInt(AudioManager.MusicMutedKey, 0);
        yield return Load("MainMenu");
        Assert.IsNotNull(MusicTransicion.Source, "El menÃº no creÃ³ la mÃºsica persistente");

        yield return Load("Game");
        int playing = 0;
        foreach (var s in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            if (s.isPlaying && s.loop && s.clip != null && s.enabled) playing++;
        Assert.AreEqual(1, playing, "Suenan varias pistas de mÃºsica a la vez");

        AudioManager.instance.ToggleMusicMute();
        Assert.IsTrue(MusicTransicion.Source.mute, "El mute no silencia la mÃºsica que viene del menÃº");
        AudioManager.instance.ToggleMusicMute();
        Assert.IsFalse(MusicTransicion.Source.mute);
    }
}
