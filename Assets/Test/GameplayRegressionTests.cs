using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Tests de regresión de los bugs corregidos en pool, vida, monedas, power-ups y reinicio.
/// Cargan la escena Game real; cualquier Debug.LogError o excepción hace fallar el test.
/// Usan un guardado temporal propio (TestSaveIsolation): no tocan el del jugador.
/// </summary>
public class GameplayRegressionTests
{
    [SetUp]
    public void IsolateSave() => TestSaveIsolation.Begin();

    [TearDown]
    public void RestoreSave()
    {
        TestSaveIsolation.End();
        Time.timeScale = 1f;
    }

    private static IEnumerator LoadGame()
    {
        TestScenes.Load("Game");
        yield return null;
        yield return null; // Start() de todos los objetos (creación de pools)

        // Que los spawners automáticos no interfieran con el test
        foreach (var spawner in Object.FindObjectsByType<EntitySpawner>(FindObjectsSortMode.None))
            spawner.enabled = false;
    }

    [UnityTest]
    public IEnumerator Encuadre_DobleSaltoNoSaleDeLaCamara()
    {
        yield return LoadGame();
        var player = PlayerObject();
        var rb = player.GetComponent<Rigidbody2D>();
        var jump = typeof(Player).GetMethod("Jump", BindingFlags.NonPublic | BindingFlags.Instance);
        var cam = Camera.main;
        float camTop = cam.transform.position.y + cam.orthographicSize;

        yield return new WaitForSeconds(0.3f); // que quede apoyado
        jump.Invoke(player.GetComponent<Player>(), null);
        yield return new WaitForFixedUpdate();
        while (rb.linearVelocity.y > 0f) yield return new WaitForFixedUpdate();
        jump.Invoke(player.GetComponent<Player>(), null); // segundo salto en el apex: el peor caso

        float maxTop = float.MinValue;
        for (float t = 0; t < 1.5f; t += Time.fixedDeltaTime)
        {
            yield return new WaitForFixedUpdate();
            maxTop = Mathf.Max(maxTop, VisualTop(player));
        }
        Assert.Less(maxTop, camTop, $"El doble salto sale de cámara (tope {maxTop:F2}, cámara {camTop:F2})");
    }

    [UnityTest]
    public IEnumerator Encuadre_EnemigoTerrestreLlegaAlJugadorSobreElPiso()
    {
        yield return LoadGame();
        var framing = Object.FindFirstObjectByType<CameraFraming>();
        var player = PlayerObject().transform;
        Assert.Greater(framing.RightEdge, player.position.x);

        // Se spawnea en el punto real (fuera del borde derecho), apoyado como lo hace el spawner
        var enemy = SpawnOnFloor("Enemigo", framing.RightEdge + 2f).transform;
        float minBase = float.MaxValue;
        for (float t = 0; t < 4f && enemy.gameObject.activeInHierarchy && enemy.position.x > player.position.x + 1.5f; t += Time.deltaTime)
        {
            minBase = Mathf.Min(minBase, SolidBottom(enemy.gameObject));
            yield return null;
        }
        Assert.IsTrue(enemy.gameObject.activeInHierarchy, "El enemigo desapareció antes de llegar");
        Assert.LessOrEqual(enemy.position.x, player.position.x + 1.5f, "El enemigo no llegó al jugador");
        Assert.Greater(minBase, GroundTop() - 0.03f, "El enemigo se hundió en el piso");
    }

    [UnityTest]
    public IEnumerator Encuadre_ParallaxCubreSiempreLaVista()
    {
        yield return LoadGame();
        var cam = Camera.main;
        float halfW = cam.orthographicSize * cam.aspect;
        float left = cam.transform.position.x - halfW, right = cam.transform.position.x + halfW;
        var background = GameObject.Find("Background").transform;
        var columns = GameObject.Find("Columns").transform;
        // Las columnas tienen una separación de diseño entre segmentos (ya existía): ese hueco se permite.
        var colSeg = columns.GetChild(0).GetComponent<SpriteRenderer>();
        float colSpacing = Mathf.Abs(columns.GetChild(1).position.x - columns.GetChild(0).position.x);
        float allowedColumnGap = colSpacing - colSeg.bounds.size.x + 0.01f;

        Time.timeScale = 6f; // varios ciclos de reciclado en poco tiempo real
        for (float t = 0; t < 3f; t += Time.unscaledDeltaTime)
        {
            float maxGapBg = LargestGapInView(background, left, right);
            float maxGapCol = LargestGapInView(columns, left, right);
            Assert.LessOrEqual(maxGapBg, 0.01f, "Background: queda un hueco visible");
            Assert.LessOrEqual(maxGapCol, allowedColumnGap, "Columns: hueco mayor que la separación de diseño");
            yield return null;
        }
        Time.timeScale = 1f;
    }

    // Mayor tramo de [left, right] que no cubre ningún segmento (sprites hijos directos) de la capa
    private static float LargestGapInView(Transform layer, float left, float right)
    {
        var spans = new System.Collections.Generic.List<Vector2>();
        foreach (Transform seg in layer)
        {
            var b = seg.GetComponent<SpriteRenderer>().bounds;
            spans.Add(new Vector2(b.min.x, b.max.x));
        }
        spans.Sort((a, b) => a.x.CompareTo(b.x));
        float covered = left, largest = 0f;
        foreach (var s in spans)
        {
            if (s.x > covered) largest = Mathf.Max(largest, Mathf.Min(s.x, right) - covered);
            covered = Mathf.Max(covered, s.y);
            if (covered >= right) break;
        }
        if (covered < right) largest = Mathf.Max(largest, right - covered);
        return largest;
    }

    // ---------- Helpers de escala / piso ----------

    private static GameObject PlayerObject() => Object.FindFirstObjectByType<Player>().gameObject;

    private static float GroundTop() => GameObject.Find("_plat").GetComponent<BoxCollider2D>().bounds.max.y;

    private static float VisualTop(GameObject go)
    {
        float top = float.MinValue;
        foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>()) top = Mathf.Max(top, sr.bounds.max.y);
        return top;
    }

    private static float SolidBottom(GameObject go)
    {
        float bottom = float.MaxValue;
        foreach (var c in go.GetComponentsInChildren<Collider2D>()) if (!c.isTrigger) bottom = Mathf.Min(bottom, c.bounds.min.y);
        return bottom;
    }

    /// <summary>Prefab de un pool de la escena por nombre (el juego usa referencias directas).</summary>
    private static GameObject Prefab(string prefabName)
    {
        var prefab = PoolManager.Instance.FindPrefab(prefabName);
        Assert.IsNotNull(prefab, "No hay pool para el prefab " + prefabName);
        return prefab;
    }

    private static GameObject SpawnOnFloor(string prefabName, float x)
    {
        var go = PoolManager.Instance.Spawn(Prefab(prefabName), new Vector3(x, GroundTop(), 0f));
        float offset = MechaRunner.Spawning.SpawnAlignment.BottomOffsetFromPivot(go);
        go.transform.position = new Vector3(x, GroundTop() + offset, 0f);
        Physics2D.SyncTransforms();
        return go;
    }

    // ---------- Etapa 2: piso, escala y Robot ----------

    [UnityTest]
    public IEnumerator Piso_ObjetosDeSueloSeApoyanSobreElPisoDesdeElSpawner()
    {
        yield return LoadGame();
        var down = GameObject.Find("DownSpawn").GetComponent<EntitySpawner>();
        var entriesField = typeof(EntitySpawner).GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance);
        var spawnMethod = typeof(EntitySpawner).GetMethod("SpawnEntity", BindingFlags.NonPublic | BindingFlags.Instance);
        float ground = GroundTop();

        foreach (var id in new[] { "Obs_Box", "Obs_Barrel", "Obs_Double", "Enemigo" })
        {
            // El spawner real, con una única entrada válida para cualquier dificultad
            entriesField.SetValue(down, new[] { new SpawnableEntry { prefab = Prefab(id), weight = 1, minDifficulty = 0f, maxDifficulty = 999f, spawnPointIndex = 0 } });
            spawnMethod.Invoke(down, null);
            yield return new WaitForFixedUpdate();

            GameObject spawned = null;
            foreach (var po in Object.FindObjectsByType<PooledObject>(FindObjectsSortMode.None))
                if (po.SourcePrefab == Prefab(id) && po.gameObject.activeInHierarchy) spawned = po.gameObject;
            Assert.IsNotNull(spawned, id + " no se spawneó");
            Assert.AreEqual(ground, SolidBottom(spawned), 0.02f, id + " no quedó apoyado sobre el piso");

            if (id == "Enemigo")
            {
                yield return new WaitForSeconds(0.5f);
                Assert.AreEqual(ground, SolidBottom(spawned), 0.03f, "El enemigo no se mantiene sobre el piso");
                Assert.AreEqual(0f, Mathf.DeltaAngle(0f, spawned.transform.eulerAngles.z), 0.01f, "El enemigo rotó");
            }
            PoolManager.Instance.ClearPool(Prefab(id));
        }
    }

    [UnityTest]
    public IEnumerator Robot_EnemigoTerrestreLoDaniaYDesaparece()
    {
        yield return LoadGame();
        var robot = PlayerObject();
        var health = robot.GetComponent<PlayerHealth>();
        var hpField = typeof(PlayerHealth).GetField("playerHealth", BindingFlags.NonPublic | BindingFlags.Instance);
        hpField.SetValue(health, 10);

        var enemy = SpawnOnFloor("Enemigo", robot.transform.position.x + 2.5f);
        for (float t = 0; t < 3f && enemy.activeInHierarchy; t += Time.deltaTime) yield return null;

        Assert.IsFalse(enemy.activeInHierarchy, "El enemigo no desapareció al tocar al Robot");
        Assert.AreEqual(9, (int)hpField.GetValue(health), "El enemigo debe hacer exactamente 1 de daño");
        Assert.AreEqual(-3.1f, robot.transform.position.x, 0.01f, "El Robot fue desplazado");
    }

    [UnityTest]
    public IEnumerator Robot_DisparoSaleDeLaBocaDelCanon()
    {
        yield return LoadGame();
        var robot = PlayerObject();
        var weapon = robot.GetComponent<Weapon>();
        var firePoint = (Transform)typeof(Weapon).GetField("firePoint", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(weapon);
        yield return new WaitForSeconds(0.2f);

        weapon.Shoot();
        // Posición del cañón en la pose de disparo, leída en el mismo frame del disparo
        Vector3 muzzle = firePoint.position;
        GameObject bullet = null;
        foreach (var po in Object.FindObjectsByType<PooledObject>(FindObjectsSortMode.None))
            if (po.SourcePrefab == Prefab("Bullet") && po.gameObject.activeInHierarchy) bullet = po.gameObject;

        Assert.IsNotNull(bullet);
        Assert.AreEqual(muzzle.x, bullet.transform.position.x, 0.001f);
        Assert.AreEqual(muzzle.y, bullet.transform.position.y, 0.001f);
        Assert.Greater(muzzle.x, robot.transform.position.x + 0.15f, "La bala debe nacer delante del Robot");
        float bottom = SolidBottom(robot), top = VisualTop(robot);
        Assert.That(muzzle.y, Is.InRange(bottom, top), "La boca del cañón debe estar a la altura del Robot");

        yield return new WaitForFixedUpdate();
        var v = bullet.GetComponent<Rigidbody2D>().linearVelocity;
        Assert.Greater(v.x, 0f);
        Assert.AreEqual(0f, v.y, 0.001f, "El disparo debe ser horizontal");
    }

    [UnityTest]
    public IEnumerator Robot_SaltoAnimadoNoDesplazaLaRaiz()
    {
        yield return LoadGame();
        var robot = PlayerObject();
        var animator = robot.GetComponent<Animator>();
        var jump = typeof(Player).GetMethod("Jump", BindingFlags.NonPublic | BindingFlags.Instance);
        yield return new WaitForSeconds(0.3f);
        float startY = robot.transform.position.y;

        jump.Invoke(robot.GetComponent<Player>(), null);
        yield return null;
        Assert.IsTrue(animator.GetCurrentAnimatorStateInfo(0).IsName("Jump"), "No se reprodujo la animación de salto");

        float maxY = startY;
        for (float t = 0; t < 0.4f; t += Time.deltaTime)
        {
            maxY = Mathf.Max(maxY, robot.transform.position.y);
            Assert.AreEqual(-3.1f, robot.transform.position.x, 0.01f, "El clip de salto movió la raíz en X");
            yield return null;
        }
        Assert.Greater(maxY, startY + 1f, "El Robot no subió (la animación pisa la física)");
    }

    [UnityTest]
    public IEnumerator Pool_AutoReturnFuncionaEnCadaReuso()
    {
        var managerGo = new GameObject("PoolManager_Test");
        managerGo.AddComponent<PoolManager>();

        var prefab = new GameObject("AutoReturnPrefab");
        prefab.SetActive(false);
        var pooled = prefab.AddComponent<PooledObject>();
        typeof(PooledObject).GetField("autoReturnToPool", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(pooled, true);
        typeof(PooledObject).GetField("autoReturnTime", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(pooled, 0.2f);

        PoolManager.Instance.CreatePool(prefab, 1, 1, false);

        // Primer uso: vuelve solo
        PoolManager.Instance.Spawn(prefab, Vector3.zero);
        yield return new WaitForSeconds(0.4f);
        Assert.AreEqual(0, PoolManager.Instance.GetPoolInfo(prefab).active, "No volvió al pool en el primer uso");

        // Segundo uso: no debe volver instantáneamente (antes usaba el tiempo de creación)
        PoolManager.Instance.Spawn(prefab, Vector3.zero);
        yield return null;
        Assert.AreEqual(1, PoolManager.Instance.GetPoolInfo(prefab).active, "Volvió al pool apenas spawneado");
        yield return new WaitForSeconds(0.4f);
        Assert.AreEqual(0, PoolManager.Instance.GetPoolInfo(prefab).active, "No volvió al pool en el segundo uso");

        Object.Destroy(managerGo);
        Object.Destroy(prefab);
    }

    [UnityTest]
    public IEnumerator Game_DisparoNormalYSuperShootSinErrores()
    {
        yield return LoadGame();
        var weapon = Object.FindFirstObjectByType<Weapon>();

        weapon.Shoot();
        Assert.AreEqual(1, PoolManager.Instance.GetPoolInfo(Prefab("Bullet")).active);

        PowerUpManager.Instance.Activate(PowerUpType.SuperShoot);
        weapon.Shoot();
        Assert.AreEqual(1, PoolManager.Instance.GetPoolInfo(Prefab("SuperBulet")).active, "El SuperShoot no spawneó bala");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Game_BalaQueNoChocaVuelveAlPool()
    {
        yield return LoadGame();

        // Disparo hacia arriba: no choca con nada
        PoolManager.Instance.Spawn(Prefab("Bullet"), new Vector3(0f, 20f, 0f), Quaternion.Euler(0f, 0f, 90f));
        Assert.AreEqual(1, PoolManager.Instance.GetPoolInfo(Prefab("Bullet")).active);

        yield return new WaitForSeconds(3.5f);
        Assert.AreEqual(0, PoolManager.Instance.GetPoolInfo(Prefab("Bullet")).active, "La bala perdida nunca volvió al pool");
    }

    [UnityTest]
    public IEnumerator Game_MuteSobreviveAlReinicio()
    {
        SaveSystem.Data.settings.sfxMuted = false;
        SaveSystem.Data.settings.musicMuted = false;
        yield return LoadGame();

        AudioManager.instance.ToggleMusicMute();
        Assert.IsTrue(AudioManager.instance.MusicMuted);

        GameManager.Instance.Restart();
        yield return null;
        yield return null;

        Assert.IsTrue(AudioManager.instance.MusicMuted, "El mute de música se perdió al reiniciar");
        Assert.IsFalse(AudioManager.instance.SfxMuted);
    }

    [UnityTest]
    public IEnumerator Game_EnemigoMuereConExplosionQueVuelveAlPool()
    {
        yield return LoadGame();

        var enemyGo = PoolManager.Instance.Spawn(Prefab("Enemigo"), new Vector3(30f, 0f, 0f));
        enemyGo.GetComponent<Enemy>().TakeDamage(999);

        Assert.AreEqual(0, PoolManager.Instance.GetPoolInfo(Prefab("Enemigo")).active);
        Assert.AreEqual(1, PoolManager.Instance.GetPoolInfo(Prefab("ExplocionEnem")).active, "No apareció la explosión");
        Assert.AreEqual(1, PoolManager.Instance.GetPoolInfo(Prefab("Moneda")).active, "No soltó moneda");

        yield return new WaitForSeconds(3f); // duración + lifetime de las partículas
        var fx = PoolManager.Instance.GetPoolInfo(Prefab("ExplocionEnem"));
        Assert.AreEqual(0, fx.active, "La explosión no volvió al pool");
        Assert.AreEqual(fx.total, fx.available, "La explosión se destruyó en vez de volver al pool");
    }

    [UnityTest]
    public IEnumerator Game_AgarrarMonedaSumaAlContador()
    {
        yield return LoadGame();
        var player = GameObject.FindWithTag("Player");
        int before = CurrencyService.Instance.Coins;

        PoolManager.Instance.Spawn(Prefab("Moneda"), player.transform.position);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return null;

        Assert.AreEqual(before + 1, CurrencyService.Instance.Coins);
        Assert.AreEqual(0, PoolManager.Instance.GetPoolInfo(Prefab("Moneda")).active, "La moneda no se despawneó");
    }

    [UnityTest]
    public IEnumerator Game_DanioConInvulnerabilidadYMuerteUnicaVez()
    {
        yield return LoadGame();
        var health = Object.FindFirstObjectByType<PlayerHealth>();

        health.PlayerDamage(1);
        health.PlayerDamage(1); // dentro de la invulnerabilidad: se ignora
        Assert.IsFalse(health.IsDead, "Dos golpes seguidos no deberían matar (invulnerabilidad)");

        yield return new WaitForSeconds(1.1f);
        health.PlayerDamage(1);
        Assert.IsTrue(health.IsDead);
        Assert.IsTrue(GameManager.IsGameOver);
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsFalse(ScoreManager.instance.scoreIncreasing, "El puntaje siguió sumando muerto");

        // Golpes y pausa después de morir no hacen nada
        health.PlayerDamage(5);
        GameManager.Instance.Pause();
        Assert.IsFalse(GameManager.IsPaused);
        Assert.IsTrue(GameManager.IsGameOver);
    }

    [UnityTest]
    public IEnumerator Game_ReiniciarDejaTodoLimpio()
    {
        yield return LoadGame();
        PoolManager.Instance.Spawn(Prefab("Obs_Box"), new Vector3(30f, 0f, 0f));
        ScoreManager.instance.scoreCount = 500f;
        Object.FindFirstObjectByType<PlayerHealth>().PlayerDamage(999);
        var oldPoolManager = PoolManager.Instance;

        GameManager.Instance.Restart();
        yield return null;
        yield return null;

        Assert.AreNotSame(oldPoolManager, PoolManager.Instance, "El PoolManager sobrevivió al reinicio");
        Assert.AreEqual(0, PoolManager.Instance.GetPoolInfo(Prefab("Obs_Box")).active, "Quedaron obstáculos de la partida anterior");
        Assert.Less(ScoreManager.instance.scoreCount, 1f, "El puntaje no se reinició");
        Assert.IsTrue(ScoreManager.instance.scoreIncreasing);
        Assert.IsFalse(GameManager.IsGameOver);
        Assert.IsTrue(GameManager.IsPlaying);
        Assert.AreEqual(1f, Time.timeScale);

        yield return new WaitForSeconds(0.5f); // que corran varios Update sin MissingReferenceException
    }

    // ---------- Arquitectura: estados, monedas y pools ----------

    [UnityTest]
    public IEnumerator GameManager_PausaAlMinimizarYNoReanudaSolo()
    {
        yield return LoadGame();
        GameManager.Instance.SendMessage("OnApplicationPause", true);
        Assert.IsTrue(GameManager.IsPaused, "No se pausó al minimizar la app");
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsFalse(ScoreManager.instance.scoreIncreasing);

        GameManager.Instance.SendMessage("OnApplicationPause", false);
        Assert.IsTrue(GameManager.IsPaused, "Al volver a la app tiene que esperar a que el jugador toque RESUME");

        GameManager.Instance.Resume();
        Assert.IsTrue(GameManager.IsPlaying);
        Assert.AreEqual(1f, Time.timeScale);
        Assert.IsTrue(ScoreManager.instance.scoreIncreasing);
    }

    [UnityTest]
    public IEnumerator Monedas_ModeloConMultiplicadorYVistaSincronizada()
    {
        SaveSystem.Data.progress.coins = 5;
        yield return LoadGame();
        var wallet = CurrencyService.Instance;
        int changes = 0;
        wallet.Changed += _ => changes++;

        wallet.CollectCoin();
        PowerUpManager.Instance.Activate(PowerUpType.CoinMultiplier);
        wallet.CollectCoin();

        Assert.AreEqual(8, wallet.Coins, "5 guardadas + 1 + 2 con el multiplicador");
        Assert.AreEqual(3, wallet.RunCoins);
        Assert.AreEqual(8, SaveSystem.Data.progress.coins, "No se guardó el total");
        Assert.AreEqual(2, changes);
        var hudText = GameObject.Find("Canvas").transform.Find("HUD/ScorePlate/CoinValue").GetComponent<TMPro.TMP_Text>();
        Assert.AreEqual("8", hudText.text, "La vista del HUD no refleja el modelo");
    }

    [UnityTest]
    public IEnumerator Pool_PrefabSinConfigurarSeCreaSoloYVuelve()
    {
        var managerGo = new GameObject("PoolManager_Test");
        managerGo.AddComponent<PoolManager>();
        var prefab = new GameObject("SinConfigurar");
        prefab.SetActive(false);

        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no tiene pool configurado"));
        var go = PoolManager.Instance.Spawn(prefab, Vector3.zero);
        Assert.IsNotNull(go);
        Assert.AreEqual(1, PoolManager.Instance.GetPoolInfo(prefab).active);
        Assert.AreSame(prefab, go.GetComponent<PooledObject>().SourcePrefab);

        go.GetComponent<PooledObject>().Despawn();
        Assert.AreEqual(0, PoolManager.Instance.GetPoolInfo(prefab).active, "No volvió a su pool");
        yield return null;

        Object.Destroy(managerGo);
        Object.Destroy(prefab);
    }
}
