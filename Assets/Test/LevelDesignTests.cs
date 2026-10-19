using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Etapa 1 del nuevo diseño de nivel (GameLab): curva por zonas, spawn por distancia con piso de
/// justicia, puntaje por distancia, y que la escena Game siga con el comportamiento original.
/// </summary>
public class LevelDesignTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    [SetUp]
    public void IsolateSave() => TestSaveIsolation.Begin();

    [TearDown]
    public void RestoreSave()
    {
        TestSaveIsolation.End();
        Time.timeScale = 1f;
    }

    private static DifficultyManager.Zone Z(float duration, float speed) => new DifficultyManager.Zone { duration = duration, speed = speed };

    [Test]
    public void Zonas_RampaSuaveEntreZonasYCrecimientoFinalConTope()
    {
        var zones = new[] { Z(10, 6), Z(10, 8) };
        Assert.AreEqual(6f, DifficultyManager.EvaluateZones(zones, 0f, 2f, 1f, 9f, out int z0), 1e-4f);
        Assert.AreEqual(0, z0);

        Assert.AreEqual(6f, DifficultyManager.EvaluateZones(zones, 10f, 2f, 1f, 9f, out int z1), 1e-4f, "Al entrar a la zona 2 todavía no saltó");
        Assert.AreEqual(1, z1);
        float mid = DifficultyManager.EvaluateZones(zones, 11f, 2f, 1f, 9f, out _);
        Assert.That(mid, Is.GreaterThan(6f).And.LessThan(8f), "La rampa tiene que ser gradual");
        Assert.AreEqual(8f, DifficultyManager.EvaluateZones(zones, 12f, 2f, 1f, 9f, out _), 1e-4f);

        // Pasada la última zona: +1 por minuto hasta el tope de 9
        Assert.AreEqual(8.5f, DifficultyManager.EvaluateZones(zones, 50f, 2f, 1f, 9f, out int zEnd), 1e-4f);
        Assert.AreEqual(1, zEnd);
        Assert.AreEqual(9f, DifficultyManager.EvaluateZones(zones, 600f, 2f, 1f, 9f, out _), 1e-4f);
    }

    [Test]
    public void SpawnPorDistancia_NuncaMenosQueElTiempoMinimoDeReaccion()
    {
        var go = new GameObject("Spawner_Test");
        var spawner = go.AddComponent<EntitySpawner>();
        typeof(EntitySpawner).GetField("minGap", Private).SetValue(spawner, 4f);
        typeof(EntitySpawner).GetField("maxGap", Private).SetValue(spawner, 5f);
        typeof(EntitySpawner).GetField("minGapSeconds", Private).SetValue(spawner, 0.9f);

        for (int i = 0; i < 50; i++)
        {
            // A velocidad 12 y densidad alta el hueco al azar (~3 u) sería injusto: manda el piso de 0,9 s
            Assert.GreaterOrEqual(spawner.NextGap(12f, 1.5f), 12f * 0.9f - 1e-4f);
            float slow = spawner.NextGap(2f, 1f);
            Assert.That(slow, Is.InRange(4f, 5f), "A velocidad baja se usa el hueco configurado");
        }
        Object.DestroyImmediate(go);
    }

    [UnityTest]
    public IEnumerator Game_SigueConLaProgresionYElPuntajeOriginales()
    {
        TestScenes.Load("Game");
        yield return null;
        yield return null;

        var dm = DifficultyManager.Instance;
        Assert.AreEqual(DifficultyManager.ProgressionMode.Steps, dm.Mode, "Game no tiene que usar el modo nuevo hasta que se apruebe");
        Assert.AreEqual(4f, dm.GameSpeed, 1e-4f);
        Assert.AreEqual(ScoreManager.ScoreSource.Time, typeof(ScoreManager).GetField("source", Private).GetValue(ScoreManager.instance));
        foreach (var s in Object.FindObjectsByType<EntitySpawner>(FindObjectsSortMode.None))
            Assert.IsFalse((bool)typeof(EntitySpawner).GetField("distanceBased", Private).GetValue(s), s.name + " pasó a spawn por distancia");
    }

    [UnityTest]
    public IEnumerator GameLab_ZonasDistanciaYMismoGuardado()
    {
        TestScenes.Load("GameLab");
        yield return null;
        yield return null;

        var dm = DifficultyManager.Instance;
        Assert.AreEqual(DifficultyManager.ProgressionMode.Zones, dm.Mode);
        Assert.AreEqual(6f, dm.GameSpeed, 1e-4f, "GameLab arranca a velocidad 6");
        Assert.AreEqual(TestSaveIsolation.Directory, SaveSystem.Directory, "GameLab usa el mismo guardado que el resto del juego (lo muestra el menú)");

        float d0 = dm.Distance, s0 = ScoreManager.instance.scoreCount;
        yield return new WaitForSeconds(1f);
        Assert.Greater(dm.Distance - d0, 5f, "La distancia no avanza con la velocidad");
        Assert.AreEqual(dm.Distance - d0, ScoreManager.instance.scoreCount - s0, 0.5f, "El puntaje tiene que seguir a la distancia");
    }
}
