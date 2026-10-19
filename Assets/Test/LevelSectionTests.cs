using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Etapa 2 (GameLab): secciones, director y mecánicas nuevas (huecos, cajas pisables, láser,
/// barril explosivo). Cargan GameLab, apagan el director y arman cada situación a mano.
/// </summary>
public class LevelSectionTests
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

    private static IEnumerator LoadLab()
    {
        SceneManager.LoadScene("GameLab");
        yield return null;
        yield return null;
        Object.FindFirstObjectByType<LevelDirector>().enabled = false;
        foreach (var po in Object.FindObjectsByType<PooledObject>(FindObjectsSortMode.None))
            if (po.gameObject.activeInHierarchy && po.SourcePrefab != null) po.Despawn();
    }

    private static LevelSection[] Sections()
    {
        var director = Object.FindFirstObjectByType<LevelDirector>(FindObjectsInactive.Include);
        return (LevelSection[])typeof(LevelDirector).GetField("sections", Private).GetValue(director);
    }

    private static GameObject Prefab(string name)
    {
        foreach (var s in Sections())
            foreach (var e in s.Elements)
                if (e.prefab != null && e.prefab.name == name) return e.prefab;
        Assert.Fail("Ninguna sección usa " + name);
        return null;
    }

    /// <summary>Coloca una sección con el director apagado y la va spawneando a medida que avanza (como en el juego).</summary>
    private static IEnumerator PlaceAndPump(string sectionName, float seconds, System.Func<bool> until = null)
    {
        LevelSection section = null;
        foreach (var s in Sections()) if (s.name == sectionName) section = s;
        Assert.IsNotNull(section, sectionName);
        var director = Object.FindFirstObjectByType<LevelDirector>();
        typeof(LevelDirector).GetField("difficulty", Private).SetValue(director, DifficultyManager.Instance);
        typeof(LevelDirector).GetField("nextStartDistance", Private).SetValue(director, DifficultyManager.Instance.Distance);
        typeof(LevelDirector).GetMethod("Place", Private).Invoke(director, new object[] { section });
        var spawnDue = typeof(LevelDirector).GetMethod("SpawnDue", Private);
        float end = Time.time + seconds;
        while (Time.time < end && (until == null || !until()))
        {
            spawnDue.Invoke(director, null);
            yield return null;
        }
    }

    private static PlayerHealth Health => Object.FindFirstObjectByType<PlayerHealth>();
    private static float GroundTop => GameObject.Find("_plat").GetComponent<Collider2D>().bounds.max.y;
    private static float PlayerX => Health.transform.position.x;
    private static void Jump() => typeof(Player).GetMethod("Jump", Private).Invoke(Object.FindFirstObjectByType<Player>(), null);

    // ------------------------------------------------------------------ datos de las secciones

    [UnityTest]
    public IEnumerator Secciones_HorneadasYJustasConLaFisicaDelSalto()
    {
        yield return LoadLab();
        var jump = JumpModel.Default;
        float jumpDistance = jump.AirTime() * LevelSection.ReferenceSpeed;
        float doubleApex = jump.HeightAt(jump.ApexTime() * 2f, jump.ApexTime());

        Assert.GreaterOrEqual(Sections().Length, 10);
        foreach (var s in Sections())
        {
            Assert.Greater(s.Elements.Count, 0, s.name + " no tiene elementos horneados");
            foreach (var e in s.Elements)
            {
                Assert.IsNotNull(e.prefab, s.name + " tiene un elemento sin prefab");
                if (e.prefab.GetComponent<Pit>() != null)
                    Assert.Less(e.localScale.x, jumpDistance - 0.8f, s.name + ": hueco más ancho que un salto con margen");
                if (e.prefab.name == "Moneda")
                    Assert.LessOrEqual(e.localPosition.y, doubleApex + 0.65f + 0.4f, s.name + ": moneda inalcanzable");
            }
        }
    }

    [UnityTest]
    public IEnumerator Director_RespetaZonasPresentaPeligrosYRespiraAlCambiarDeZona()
    {
        yield return LoadLab();
        var director = Object.FindFirstObjectByType<LevelDirector>();

        // Zona 1: nada de zonas posteriores
        for (int i = 0; i < 30; i++)
        {
            var s = director.Choose(1);
            Assert.IsTrue(s.AllowedInZone(1), s.name + " no es de la zona 1");
        }

        // Zona 2: lo primero que no es respiro son las presentaciones de esa zona
        var intros = new List<string>();
        foreach (var s in Sections()) if (s.IntroducesThreat && s.MinZone == 2) intros.Add(s.name);
        typeof(LevelDirector).GetMethod("OnZoneChanged", Private).Invoke(director, new object[] { 1 });
        Assert.AreEqual(SectionIntensity.Breather, director.Choose(2).Intensity, "Al cambiar de zona va primero un respiro");
        // Las presentaciones salen antes que cualquier otra sección no-respiro (el ritmo puede intercalar respiros)
        var seen = new List<string>();
        for (int i = 0; i < 12 && seen.Count < intros.Count; i++)
        {
            var s = director.Choose(2);
            if (s.Intensity != SectionIntensity.Breather) seen.Add(s.name);
        }
        CollectionAssert.AreEquivalent(intros, seen, "Las presentaciones de la zona 2 salen primero");
    }

    // ------------------------------------------------------------------ mecánicas

    [UnityTest]
    public IEnumerator Hueco_SinSaltarEsGameOver()
    {
        yield return LoadLab();
        var pit = PoolManager.Instance.Spawn(Prefab("Lab_Pit"), new Vector3(PlayerX + 3f, GroundTop - 0.65f, 0f));
        pit.transform.localScale = new Vector3(2.4f, 1.3f, 1f);

        float end = Time.time + 3f;
        while (!GameManager.IsGameOver && Time.time < end) yield return null;
        Assert.IsTrue(GameManager.IsGameOver, "Caer en el hueco tiene que terminar la partida");
    }

    [UnityTest]
    public IEnumerator Hueco_SaltandoATiempoSeCruza()
    {
        yield return LoadLab();
        var pit = PoolManager.Instance.Spawn(Prefab("Lab_Pit"), new Vector3(PlayerX + 4f, GroundTop - 0.65f, 0f));
        pit.transform.localScale = new Vector3(2.4f, 1.3f, 1f);
        var view = pit.GetComponent<SpriteRenderer>();

        bool jumped = false;
        float end = Time.time + 4f;
        while (Time.time < end && view.bounds.max.x > PlayerX - 1f)
        {
            // Saltar cuando el borde está a ~0.12 s del Robot (como marca el arco de monedas)
            if (!jumped && view.bounds.min.x - PlayerX < DifficultyManager.Instance.GameSpeed * 0.12f) { Jump(); jumped = true; }
            yield return null;
        }
        Assert.IsTrue(jumped);
        Assert.IsFalse(GameManager.IsGameOver, "Saltando a tiempo el hueco se cruza");
    }

    [UnityTest]
    public IEnumerator Caja_ArribaSePisaDeFrenteDania()
    {
        yield return LoadLab();
        var hpField = typeof(PlayerHealth).GetField("playerHealth", Private);
        hpField.SetValue(Health, 10);

        // Encima: la caja pasa por debajo del Robot que cae sobre ella
        var player = Health.transform;
        var box = PoolManager.Instance.Spawn(Prefab("Obs_Box"), new Vector3(PlayerX, GroundTop + 0.3f, 0f));
        typeof(ScrollMover).GetField("speedMultiplier", Private).SetValue(box.GetComponent<ScrollMover>(), 0f);
        player.position += Vector3.up * 1.5f;
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(0.8f);
        Assert.AreEqual(10, (int)hpField.GetValue(Health), "Caer encima de la caja no tiene que dañar");
        box.GetComponent<PooledObject>().Despawn();
        typeof(ScrollMover).GetField("speedMultiplier", Private).SetValue(box.GetComponent<ScrollMover>(), 1f);
        yield return new WaitForSeconds(0.6f);

        // De frente: la caja llega rodando por el piso
        PoolManager.Instance.Spawn(Prefab("Obs_Box"), new Vector3(PlayerX + 3f, GroundTop + 0.3f, 0f));
        yield return new WaitForSeconds(1.2f);
        Assert.Less((int)hpField.GetValue(Health), 10, "Chocar la caja de frente tiene que dañar");
    }

    [UnityTest]
    public IEnumerator Laser_QuedarseAbajoEsSeguroSaltarDania()
    {
        yield return LoadLab();
        var hpField = typeof(PlayerHealth).GetField("playerHealth", Private);
        hpField.SetValue(Health, 10);

        PoolManager.Instance.Spawn(Prefab("Lab_LaserLow"), new Vector3(PlayerX + 3f, GroundTop + 1.6f, 0f));
        yield return new WaitForSeconds(1.6f);
        Assert.AreEqual(10, (int)hpField.GetValue(Health), "Pasar por debajo del láser sin saltar es seguro");

        PoolManager.Instance.Spawn(Prefab("Lab_LaserLow"), new Vector3(PlayerX + 3f, GroundTop + 1.6f, 0f));
        yield return new WaitForSeconds(0.45f);
        Jump();
        yield return new WaitForSeconds(0.8f);
        Assert.Less((int)hpField.GetValue(Health), 10, "Saltar contra el láser tiene que dañar");
    }

    [UnityTest]
    public IEnumerator BarrilExplosivo_LimpiaEnemigosCercanosYSueltaMonedas()
    {
        yield return LoadLab();
        float x = PlayerX + 8f, y = GroundTop + 0.5f;
        var barrel = PoolManager.Instance.Spawn(Prefab("Lab_ExplosiveBarrel"), new Vector3(x, y, 0f));
        var near = PoolManager.Instance.Spawn(Prefab("Enemigo"), new Vector3(x + 1.5f, y, 0f));
        var far = PoolManager.Instance.Spawn(Prefab("Enemigo"), new Vector3(x + 7f, y, 0f));
        int coinsBefore = PoolManager.Instance.GetPoolInfo(Prefab("Moneda")).active;
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate();

        barrel.GetComponent<ExplosiveBarrel>().TakeDamage(1);
        Assert.IsFalse(barrel.activeSelf, "El barril tiene que explotar");
        Assert.IsFalse(near.activeSelf, "El enemigo cercano tiene que morir");
        Assert.IsTrue(far.activeSelf, "El enemigo lejano no se toca");
        Assert.Greater(PoolManager.Instance.GetPoolInfo(Prefab("Moneda")).active, coinsBefore, "La explosión suelta monedas");
    }

    [UnityTest]
    public IEnumerator Enemigo_SueltaPowerUpSegunLaChanceYYaNoMonedas()
    {
        yield return LoadLab();
        var rules = LevelRules.Current;
        var chance = typeof(LevelRules).GetField("enemyDropChance", Private);
        var slot = (PowerUpSlot)typeof(LevelRules).GetField("enemyDrops", Private).GetValue(rules);
        var options = (GameObject[])typeof(PowerUpSlot).GetField("options", Private).GetValue(slot);
        int Active(GameObject p) => PoolManager.Instance.GetPoolInfo(p).active;
        int PowerUps() { int n = 0; foreach (var o in options) n += Active(o); return n; }
        var coin = Prefab("Moneda");

        chance.SetValue(rules, 1f);
        var e1 = PoolManager.Instance.Spawn(Prefab("Enemigo"), new Vector3(PlayerX + 9f, GroundTop + 0.5f, 0f));
        e1.GetComponent<Enemy>().TakeDamage(999);
        Assert.AreEqual(1, PowerUps(), "Con chance 1 tiene que soltar un power-up");
        Assert.AreEqual(0, Active(coin), "Los enemigos ya no sueltan monedas");

        chance.SetValue(rules, 0f);
        var e2 = PoolManager.Instance.Spawn(Prefab("Enemigo"), new Vector3(PlayerX + 9f, GroundTop + 0.5f, 0f));
        e2.GetComponent<Enemy>().TakeDamage(999);
        Assert.AreEqual(1, PowerUps(), "Con chance 0 no suelta nada");
        Assert.AreEqual(0, Active(coin));
    }

    [UnityTest]
    public IEnumerator Seccion_PowerUpConSuTamanioOriginal()
    {
        yield return LoadLab();
        yield return PlaceAndPump("S_LluviaMonedas", 3f);

        int found = 0;
        foreach (var po in Object.FindObjectsByType<PooledObject>(FindObjectsSortMode.None))
            if (po.gameObject.activeInHierarchy && po.SourcePrefab != null && po.SourcePrefab.GetComponent<ScrollMover>() != null && po.SourcePrefab.name.StartsWith("Lab_") && po.SourcePrefab.GetComponent<Pit>() == null)
            {
                found++;
                Assert.AreEqual(po.SourcePrefab.transform.localScale.x, po.transform.localScale.x, 1e-4f, "El power-up salió con otra escala");
            }
        Assert.AreEqual(1, found, "La sección tiene que poner un power-up");
    }

    [UnityTest]
    public IEnumerator Plataforma_DeFrenteDaniaDesdeAbajoSoloFrena()
    {
        yield return LoadLab();
        var hpField = typeof(PlayerHealth).GetField("playerHealth", Private);
        hpField.SetValue(Health, 10);
        var platform = Prefab("Lab_Platform");
        var speedField = typeof(ScrollMover).GetField("speedMultiplier", Private);

        // Desde abajo: plataforma quieta sobre la cabeza, el Robot salta y la golpea
        var above = PoolManager.Instance.Spawn(platform, new Vector3(PlayerX, GroundTop + 1.85f, 0f));
        above.transform.localScale = new Vector3(3f, 0.3f, 1f);
        speedField.SetValue(above.GetComponent<ScrollMover>(), 0f);
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(0.2f);
        Jump();
        yield return new WaitForSeconds(0.7f);
        Assert.AreEqual(10, (int)hpField.GetValue(Health), "Golpear la plataforma desde abajo no daña");
        speedField.SetValue(above.GetComponent<ScrollMover>(), 1f);
        above.GetComponent<PooledObject>().Despawn();
        yield return new WaitForSeconds(1.2f); // termina la invulnerabilidad si la hubiera

        // De frente: plataforma baja que llega hasta el Robot
        var front = PoolManager.Instance.Spawn(platform, new Vector3(PlayerX + 3f, GroundTop + 0.85f, 0f));
        front.transform.localScale = new Vector3(2.6f, 0.3f, 1f);
        yield return new WaitForSeconds(1.2f);
        Assert.Less((int)hpField.GetValue(Health), 10, "Chocar la plataforma de frente tiene que dañar");
    }

    [UnityTest]
    public IEnumerator BarrilExplosivo_AlcanzaALosEnemigosDeSuSeccion()
    {
        yield return LoadLab();
        // Avanzar hasta que el barril esté cerca del Robot (los enemigos se van quedando atrás)
        GameObject barrel = null;
        yield return PlaceAndPump("S_BarrilExplosivo", 6f, () =>
        {
            if (barrel == null)
                foreach (var po in Object.FindObjectsByType<PooledObject>(FindObjectsSortMode.None))
                    if (po.gameObject.activeInHierarchy && po.GetComponent<ExplosiveBarrel>() != null) barrel = po.gameObject;
            return barrel != null && barrel.transform.position.x <= PlayerX + 2.5f;
        });
        Assert.IsNotNull(barrel);
        Assert.AreEqual(2, Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None).Length, "Los dos enemigos tienen que haber aparecido sobre el piso");

        barrel.GetComponent<ExplosiveBarrel>().TakeDamage(1);
        int enemies = 0;
        foreach (var e in Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None)) if (e.gameObject.activeInHierarchy) enemies++;
        Assert.AreEqual(0, enemies, "La explosión tiene que alcanzar a los dos enemigos de la sección");
    }

    [UnityTest]
    public IEnumerator Escalera_AlBajarDelSegundoEscalonSeCaeSobreElTercero()
    {
        yield return LoadLab();
        var hpField = typeof(PlayerHealth).GetField("playerHealth", Private);
        hpField.SetValue(Health, 10);
        var body = Health.transform;

        // Avanzar hasta que el segundo escalón (el más alto) esté bajo el Robot y subirlo ahí.
        // El primer escalón se saca: este test prueba solo la bajada del segundo al tercero.
        GameObject high = null, first = null;
        yield return PlaceAndPump("S_EscaleraPlataformas", 6f, () =>
        {
            foreach (var po in Object.FindObjectsByType<PooledObject>(FindObjectsSortMode.None))
                if (po.gameObject.activeInHierarchy && po.SourcePrefab != null && po.SourcePrefab.name == "Lab_Platform")
                {
                    var b = po.GetComponent<Collider2D>().bounds;
                    if (first == null && b.max.y < GroundTop + 1.5f) { first = po.gameObject; po.Despawn(); continue; }
                    if (b.max.y > GroundTop + 1.5f && b.min.x < PlayerX && b.max.x > PlayerX + 0.6f) high = po.gameObject;
                }
            return high != null;
        });
        Assert.IsNotNull(high, "No llegó el escalón alto");
        float top = high.GetComponent<Collider2D>().bounds.max.y;
        body.position = new Vector3(body.position.x, top + 0.8f, 0f);
        Physics2D.SyncTransforms();

        // Seguir hasta que el tercer escalón haya pasado
        yield return PlaceAndPumpOnly(4f);
        Assert.AreEqual(10, (int)hpField.GetValue(Health), "Bajar por la escalera no tiene que dañar");
        Assert.IsFalse(GameManager.IsGameOver);
    }

    [UnityTest]
    public IEnumerator Sierra_SinSaltarLasCajasCierranElPasoDeAbajo()
    {
        yield return LoadLab();
        var hpField = typeof(PlayerHealth).GetField("playerHealth", Private);
        hpField.SetValue(Health, 10);
        yield return PlaceAndPump("S_SierrasPlataforma", 5f);
        Assert.Less((int)hpField.GetValue(Health), 10, "Sin saltar, el Robot tiene que chocar contra las cajas de abajo");
    }

    /// <summary>Sigue spawneando lo pendiente del director sin colocar secciones nuevas.</summary>
    private static IEnumerator PlaceAndPumpOnly(float seconds)
    {
        var director = Object.FindFirstObjectByType<LevelDirector>();
        var spawnDue = typeof(LevelDirector).GetMethod("SpawnDue", Private);
        float end = Time.time + seconds;
        while (Time.time < end)
        {
            spawnDue.Invoke(director, null);
            yield return null;
        }
    }
}
