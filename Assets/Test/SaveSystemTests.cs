using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Guardado central: ida y vuelta al disco, recuperación desde el respaldo,
/// migración desde PlayerPrefs y archivos viejos o dañados.
/// Trabajan en una carpeta temporal; las claves viejas de PlayerPrefs se respaldan y restauran.
/// </summary>
public class SaveSystemTests
{
    private static readonly string[] LegacyIntKeys = { "Coins", "MusicMuted", "SfxMuted" };
    private const string LegacyFloatKey = "highscore";

    private readonly System.Collections.Generic.Dictionary<string, int> savedInts = new();
    private bool hadHighscore;
    private float savedHighscore;

    [SetUp]
    public void SetUp()
    {
        savedInts.Clear();
        foreach (var key in LegacyIntKeys)
            if (PlayerPrefs.HasKey(key)) savedInts[key] = PlayerPrefs.GetInt(key);
        hadHighscore = PlayerPrefs.HasKey(LegacyFloatKey);
        savedHighscore = PlayerPrefs.GetFloat(LegacyFloatKey);

        SaveSystem.UseDirectory(TestSaveIsolation.Directory);
        SaveSystem.DeleteFiles();
    }

    [TearDown]
    public void TearDown()
    {
        TestSaveIsolation.End();
        foreach (var key in LegacyIntKeys)
            if (savedInts.TryGetValue(key, out int v)) PlayerPrefs.SetInt(key, v); else PlayerPrefs.DeleteKey(key);
        if (hadHighscore) PlayerPrefs.SetFloat(LegacyFloatKey, savedHighscore); else PlayerPrefs.DeleteKey(LegacyFloatKey);
        PlayerPrefs.Save();
    }

    private static void ClearLegacyPrefs()
    {
        foreach (var key in LegacyIntKeys) PlayerPrefs.DeleteKey(key);
        PlayerPrefs.DeleteKey(LegacyFloatKey);
    }

    private static void Reload() => SaveSystem.UseDirectory(TestSaveIsolation.Directory);

    [Test]
    public void Guardar_YVolverACargar_ConservaTodo()
    {
        SaveSystem.ResetToDefaults();
        SaveSystem.Data.progress.coins = 42;
        SaveSystem.Data.progress.bestScore = 1234.5f;
        SaveSystem.Data.settings.musicMuted = true;
        SaveSystem.Save();
        Assert.IsFalse(SaveSystem.IsDirty);
        Assert.IsTrue(File.Exists(SaveSystem.FilePath));

        Reload();
        Assert.AreEqual(42, SaveSystem.Data.progress.coins);
        Assert.AreEqual(1234.5f, SaveSystem.Data.progress.bestScore);
        Assert.IsTrue(SaveSystem.Data.settings.musicMuted);
        Assert.IsFalse(SaveSystem.Data.settings.sfxMuted);
        Assert.AreEqual(SaveData.CurrentVersion, SaveSystem.Data.version);
    }

    [Test]
    public void ArchivoDaniado_SeRecuperaDelRespaldo()
    {
        SaveSystem.ResetToDefaults();
        SaveSystem.Data.progress.coins = 10;
        SaveSystem.Save();
        SaveSystem.Data.progress.coins = 20;
        SaveSystem.Save(); // el de 10 queda como .bak

        File.WriteAllText(SaveSystem.FilePath, "{ esto no es json");
        LogAssert.Expect(LogType.Warning, new Regex("no se pudo leer save.json"));
        Reload();
        Assert.AreEqual(10, SaveSystem.Data.progress.coins, "No usó el respaldo");
    }

    [Test]
    public void SinArchivo_MigraLosDatosDePlayerPrefs()
    {
        ClearLegacyPrefs();
        PlayerPrefs.SetInt("Coins", 77);
        PlayerPrefs.SetFloat("highscore", 321f);
        PlayerPrefs.SetInt("MusicMuted", 1);

        Reload();
        Assert.AreEqual(77, SaveSystem.Data.progress.coins);
        Assert.AreEqual(321f, SaveSystem.Data.progress.bestScore);
        Assert.IsTrue(SaveSystem.Data.settings.musicMuted);
        Assert.IsFalse(SaveSystem.Data.settings.sfxMuted);
        Assert.IsTrue(SaveSystem.IsDirty, "La migración tiene que crear el archivo en el próximo guardado");

        // Una vez creado el archivo, PlayerPrefs ya no se vuelve a leer
        SaveSystem.Save();
        PlayerPrefs.SetInt("Coins", 1);
        Reload();
        Assert.AreEqual(77, SaveSystem.Data.progress.coins);
    }

    [Test]
    public void SinArchivoNiPlayerPrefs_EmpiezaDeCero()
    {
        ClearLegacyPrefs();
        Reload();
        Assert.AreEqual(0, SaveSystem.Data.progress.coins);
        Assert.AreEqual(0f, SaveSystem.Data.progress.bestScore);
        Assert.IsFalse(SaveSystem.Data.settings.musicMuted);
    }

    [Test]
    public void ArchivoConSeccionesFaltantes_SeCompletaConValoresPorDefecto()
    {
        System.IO.Directory.CreateDirectory(TestSaveIsolation.Directory);
        File.WriteAllText(SaveSystem.FilePath, "{\"version\":1,\"progress\":{\"coins\":5}}");
        Reload();
        Assert.AreEqual(5, SaveSystem.Data.progress.coins);
        Assert.IsNotNull(SaveSystem.Data.settings);
        Assert.IsFalse(SaveSystem.Data.settings.musicMuted);
    }

    [Test]
    public void ArchivoYRespaldoDaniados_EmpiezaDeCeroSinRomper()
    {
        ClearLegacyPrefs();
        System.IO.Directory.CreateDirectory(TestSaveIsolation.Directory);
        File.WriteAllText(SaveSystem.FilePath, "basura");
        File.WriteAllText(SaveSystem.FilePath + ".bak", "{\"version\":0}");
        LogAssert.Expect(LogType.Warning, new Regex("no se pudo leer save.json"));
        LogAssert.Expect(LogType.Warning, new Regex("no se pudo leer save.json.bak"));

        Reload();
        Assert.AreEqual(0, SaveSystem.Data.progress.coins);
        SaveSystem.Save();
        Reload();
        Assert.AreEqual(0, SaveSystem.Data.progress.coins, "El archivo nuevo tiene que ser válido");
    }
}
