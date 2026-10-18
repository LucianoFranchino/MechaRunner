using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Tests del rediseño de UI: cableado de botones, pausa/resultados con los controles,
/// récord nuevo y una sola pista de música del menú al juego.
/// Usan un guardado temporal propio (TestSaveIsolation): no tocan el del jugador.
/// </summary>
public class UiRegressionTests
{
    [SetUp]
    public void IsolateSave() => TestSaveIsolation.Begin();

    [TearDown]
    public void RestoreSave()
    {
        TestSaveIsolation.End();
        Time.timeScale = 1f;
        // La música del menú es DontDestroyOnLoad: no debe filtrarse a otros tests
        if (MusicTransition.Source != null) Object.Destroy(MusicTransition.Source.gameObject);
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
            Assert.Greater(b.onClick.GetPersistentEventCount(), 0, b.name + " no tiene acción");
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
        var game = GameManager.Instance;

        game.Pause();
        Assert.IsTrue(GameManager.IsPaused);
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsFalse(controls.activeSelf, "FIRE/pausa siguen visibles detrás de la pausa");
        Assert.IsTrue(GameCanvas.Find("PauseMenu").gameObject.activeSelf);

        game.Resume();
        Assert.IsTrue(GameManager.IsPlaying);
        Assert.IsTrue(controls.activeSelf);
        Assert.IsFalse(GameCanvas.Find("PauseMenu").gameObject.activeSelf);
        Assert.AreEqual(1f, Time.timeScale);
    }

    // ------------------------------------------------------------------ Game Over animado

    private static IEnumerator StartGame(float best)
    {
        SaveSystem.Data.progress.bestScore = best;
        yield return Load("Game");
        foreach (var spawner in Object.FindObjectsByType<EntitySpawner>(FindObjectsSortMode.None)) spawner.enabled = false;
    }

    private static GameOverSequence Die(float score)
    {
        ScoreManager.instance.scoreCount = score;
        Object.FindFirstObjectByType<PlayerHealth>().PlayerDamage(999);
        return GameCanvas.Find("GameOver").GetComponent<GameOverSequence>();
    }

    /// <summary>Espera en tiempo real (el Game Over deja timeScale en 0).</summary>
    private static IEnumerator WaitFor(System.Func<bool> condition, float timeout, string what)
    {
        float end = Time.realtimeSinceStartup + timeout;
        while (!condition())
        {
            if (Time.realtimeSinceStartup > end) Assert.Fail("Timeout esperando: " + what);
            yield return null;
        }
    }

    private static string Text(string path) => GameCanvas.Find("GameOver/" + path).GetComponent<TMP_Text>().text;
    private static bool Active(string path) => GameCanvas.Find("GameOver/" + path).gameObject.activeSelf;

    [UnityTest]
    public IEnumerator GameOver_ElPuntajeCuentaDesdeCeroHastaElFinal()
    {
        yield return StartGame(9999f);
        var seq = Die(80f);

        Assert.IsTrue(GameCanvas.Find("GameOver").gameObject.activeSelf, "No se abrió la pantalla de resultados");
        Assert.IsFalse(GameCanvas.Find("Controls").gameObject.activeSelf, "Los controles quedaron sobre los resultados");
        Assert.AreEqual("0", Text("Results/ScoreValue"), "El conteo no arranca en 0");
        Assert.IsFalse(GameCanvas.Find("GameOver/Actions").GetComponent<CanvasGroup>().interactable, "Los botones se pueden tocar antes de tiempo");

        yield return WaitFor(() => seq.Finished, 8f, "fin de la secuencia");
        Assert.AreEqual("80", Text("Results/ScoreValue"));
        Assert.AreEqual("9999", Text("Results/BestValue"));
        Assert.IsTrue(GameCanvas.Find("GameOver/Actions").GetComponent<CanvasGroup>().interactable);
        Assert.IsFalse(Active("Results/NewBadge"), "Insignia NEW sin récord");
        Assert.IsFalse(Active("Celebration"), "Celebración sin récord");
    }

    [UnityTest]
    public IEnumerator GameOver_MonedasDeLaPartidaVuelanYSumanAlTotal()
    {
        SaveSystem.Data.progress.coins = 10;
        yield return StartGame(9999f);
        for (int i = 0; i < 3; i++) CurrencyService.Instance.CollectCoin();
        var seq = Die(5f);

        Assert.AreEqual("10", Text("TotalCoins/TotalValue"), "El total tiene que arrancar sin las monedas de la partida");
        yield return WaitFor(() => GameCanvas.Find("GameOver/FlyingCoins").GetComponentsInChildren<Image>(false).Length > 0, 6f, "monedas volando");
        yield return WaitFor(() => seq.Finished, 8f, "fin de la secuencia");

        Assert.AreEqual("+3", Text("Results/CoinsRow/RunCoins"));
        Assert.AreEqual("13", Text("TotalCoins/TotalValue"));
        Assert.AreEqual(0, GameCanvas.Find("GameOver/FlyingCoins").GetComponentsInChildren<Image>(false).Length, "Quedaron monedas volando");
        Assert.AreEqual(13, SaveSystem.Data.progress.coins, "Cambió el guardado de monedas");
        Assert.IsFalse(SaveSystem.IsDirty, "El fin de partida no escribió el guardado");
        SaveSystem.UseDirectory(TestSaveIsolation.Directory); // releer del disco
        Assert.AreEqual(13, SaveSystem.Data.progress.coins, "Las monedas no quedaron en el archivo");
    }

    [UnityTest]
    public IEnumerator GameOver_RecordMuestraCelebracionYDespuesLaInsignia()
    {
        yield return StartGame(10f);
        var seq = Die(50f);

        Assert.IsFalse(Active("Results/NewBadge"), "La insignia tiene que esperar a la celebración");
        yield return WaitFor(() => Active("Celebration"), 6f, "celebración de récord");
        Assert.AreEqual("50", Text("Celebration/RecordLabel/Score"));

        seq.OnPointerClick(null); // tocar la cierra
        yield return WaitFor(() => !Active("Celebration"), 2f, "cierre de la celebración");
        yield return WaitFor(() => seq.Finished, 8f, "fin de la secuencia");
        Assert.IsTrue(Active("Results/NewBadge"), "Falta la insignia de récord nuevo");
        Assert.AreEqual("50", Text("Results/BestValue"));
        Assert.AreEqual("50", Text("Results/ScoreValue"));
    }

    [UnityTest]
    public IEnumerator GameOver_TocarSaltaAlFinalSinPerderLaCelebracion()
    {
        SaveSystem.Data.progress.coins = 0;
        yield return StartGame(10f);
        CurrencyService.Instance.CollectCoin();
        var seq = Die(500f);
        yield return null;

        seq.OnPointerClick(null);
        Assert.IsTrue(seq.Finished);
        Assert.AreEqual("500", Text("Results/ScoreValue"));
        Assert.AreEqual("1", Text("TotalCoins/TotalValue"));
        Assert.AreEqual("+1", Text("Results/CoinsRow/RunCoins"));
        Assert.IsTrue(GameCanvas.Find("GameOver/Actions").GetComponent<CanvasGroup>().interactable);
        Assert.IsTrue(Active("Celebration"), "El récord tiene que celebrarse aunque se salte el conteo");

        seq.OnPointerClick(null);
        yield return WaitFor(() => !Active("Celebration"), 2f, "cierre de la celebración");
        Assert.IsTrue(Active("Results/NewBadge"));
    }

    [UnityTest]
    public IEnumerator Audio_UnaSolaMusicaDelMenuAlJuegoYElMuteLaApaga()
    {
        SaveSystem.Data.settings.musicMuted = false;
        yield return Load("MainMenu");
        Assert.IsNotNull(MusicTransition.Source, "El menú no creó la música persistente");

        yield return Load("Game");
        int playing = 0;
        foreach (var s in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            if (s.isPlaying && s.loop && s.clip != null && s.enabled) playing++;
        Assert.AreEqual(1, playing, "Suenan varias pistas de música a la vez");

        AudioManager.instance.ToggleMusicMute();
        Assert.IsTrue(MusicTransition.Source.mute, "El mute no silencia la música que viene del menú");
        AudioManager.instance.ToggleMusicMute();
        Assert.IsFalse(MusicTransition.Source.mute);
    }
}
