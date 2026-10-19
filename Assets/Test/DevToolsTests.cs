using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Menú principal actualizado con el guardado y menú de debug (monedas, reseteo, congelar partida).
/// </summary>
public class DevToolsTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    [SetUp]
    public void IsolateSave() => TestSaveIsolation.Begin();

    [TearDown]
    public void RestoreSave()
    {
        if (DevMenu.Instance != null && DevMenu.Instance.IsOpen) DevMenu.Instance.Toggle();
        PlayerHealth.DevInvulnerable = false;
        TestSaveIsolation.End();
        Time.timeScale = 1f;
        if (MusicTransition.Source != null) Object.Destroy(MusicTransition.Source.gameObject);
    }

    private static string MenuText(string field)
    {
        var stats = Object.FindFirstObjectByType<MenuStats>();
        return ((TMP_Text)typeof(MenuStats).GetField(field, Private).GetValue(stats)).text;
    }

    [UnityTest]
    public IEnumerator Menu_MuestraElGuardadoYSeActualizaAlGuardar()
    {
        SaveSystem.Data.progress.coins = 12;
        SaveSystem.Data.progress.bestScore = 340f;
        TestScenes.Load("MainMenu");
        yield return null;
        Assert.AreEqual("12", MenuText("coinsText"));
        Assert.AreEqual("340", MenuText("bestText"));

        SaveSystem.Data.progress.coins = 99;
        SaveSystem.Save();
        Assert.AreEqual("99", MenuText("coinsText"), "El menú no se refrescó al guardar");
    }

    [UnityTest]
    public IEnumerator DevMenu_SeAbreCongelaYSumaMonedasEnPartida()
    {
        SaveSystem.Data.progress.coins = 5;
        TestScenes.Load("GameLab");
        yield return null;
        yield return null;
        var dev = DevMenu.Instance;
        Assert.IsNotNull(dev, "El menú de debug tiene que existir en el Editor");

        dev.Toggle();
        Assert.IsTrue(dev.IsOpen);
        Assert.AreEqual(0f, Time.timeScale, "La partida se congela con el menú abierto");

        typeof(DevMenu).GetMethod("AddCoins", Private).Invoke(dev, new object[] { 100 });
        Assert.AreEqual(105, CurrencyService.Instance.Coins, "La partida en curso tiene que ver las monedas");
        Assert.AreEqual(105, SaveSystem.Data.progress.coins);
        Assert.IsFalse(SaveSystem.IsDirty, "Se guarda en el momento");

        dev.Toggle();
        Assert.AreEqual(1f, Time.timeScale);
    }

    [UnityTest]
    public IEnumerator DevMenu_ResetearPideConfirmacionYDejaTodoEnCero()
    {
        SaveSystem.Data.progress.coins = 50;
        SaveSystem.Data.progress.bestScore = 900f;
        SaveSystem.Data.settings.musicMuted = true;
        TestScenes.Load("GameLab");
        yield return null;
        yield return null;
        var dev = DevMenu.Instance;
        dev.Toggle();
        var reset = typeof(DevMenu).GetMethod("ResetProgress", Private);

        reset.Invoke(dev, null);
        Assert.AreEqual(50, SaveSystem.Data.progress.coins, "El primer toque solo pide confirmación");
        reset.Invoke(dev, null);
        Assert.AreEqual(0, SaveSystem.Data.progress.coins);
        Assert.AreEqual(0f, SaveSystem.Data.progress.bestScore);
        Assert.AreEqual(0, CurrencyService.Instance.Coins, "La partida en curso se resincroniza");
        Assert.IsTrue(SaveSystem.Data.settings.musicMuted, "Resetear el progreso no toca la configuración");
    }

    [UnityTest]
    public IEnumerator DevMenu_InvulnerableIgnoraElDanio()
    {
        TestScenes.Load("GameLab");
        yield return null;
        yield return null;
        var health = Object.FindFirstObjectByType<PlayerHealth>();
        PlayerHealth.DevInvulnerable = true;
        health.PlayerDamage(999);
        Assert.IsFalse(health.IsDead, "Invulnerable no muere");
        Assert.IsFalse(GameManager.IsGameOver);
    }
}
