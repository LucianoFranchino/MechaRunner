using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Tests de regresión de los bugs corregidos en pool, vida, monedas, power-ups y reinicio.
/// Cargan la escena Game real; cualquier Debug.LogError o excepción hace fallar el test.
/// Guardan y restauran los PlayerPrefs del jugador (monedas / highscore).
/// </summary>
public class GameplayRegressionTests
{
    private static readonly string[] IntKeys = { "Coins", "SfxMuted", "MusicMuted" };
    private readonly System.Collections.Generic.Dictionary<string, int> savedInts = new();
    private bool hadHighscore;
    private float savedHighscore;

    [SetUp]
    public void SavePrefs()
    {
        savedInts.Clear();
        foreach (var key in IntKeys)
            if (PlayerPrefs.HasKey(key)) savedInts[key] = PlayerPrefs.GetInt(key);
        hadHighscore = PlayerPrefs.HasKey("highscore");
        savedHighscore = PlayerPrefs.GetFloat("highscore");
    }

    [TearDown]
    public void RestorePrefs()
    {
        foreach (var key in IntKeys)
            if (savedInts.TryGetValue(key, out int value)) PlayerPrefs.SetInt(key, value); else PlayerPrefs.DeleteKey(key);
        if (hadHighscore) PlayerPrefs.SetFloat("highscore", savedHighscore); else PlayerPrefs.DeleteKey("highscore");
        PlayerPrefs.Save();
        Time.timeScale = 1f;
    }

    private static IEnumerator LoadGame()
    {
        SceneManager.LoadScene("Game");
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
        var player = GameObject.Find("NEW PLAYER DEMO");
        var rb = player.GetComponent<Rigidbody2D>();
        var sr = player.GetComponent<SpriteRenderer>();
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
            maxTop = Mathf.Max(maxTop, sr.bounds.max.y);
        }
        Assert.Less(maxTop, camTop, $"El doble salto sale de cámara (tope {maxTop:F2}, cámara {camTop:F2})");
    }

    [UnityTest]
    public IEnumerator Encuadre_EnemigoTerrestreLlegaAlJugadorSobreElPiso()
    {
        yield return LoadGame();
        var framing = Object.FindFirstObjectByType<CameraFraming>();
        var player = GameObject.Find("NEW PLAYER DEMO").transform;
        Assert.Greater(framing.RightEdge, player.position.x);

        // Se spawnea en el punto real (fuera del borde derecho)
        var spawnX = framing.RightEdge + 2f;
        var enemy = PoolManager.Instance.Spawn("Enemy", new Vector3(spawnX, -0.34f, 0f)).transform;
        float minY = float.MaxValue;
        for (float t = 0; t < 4f && enemy.gameObject.activeInHierarchy && enemy.position.x > player.position.x + 1.5f; t += Time.deltaTime)
        {
            minY = Mathf.Min(minY, enemy.position.y);
            yield return null;
        }
        Assert.IsTrue(enemy.gameObject.activeInHierarchy, "El enemigo desapareció antes de llegar");
        Assert.LessOrEqual(enemy.position.x, player.position.x + 1.5f, "El enemigo no llegó al jugador");
        Assert.Greater(minY, -0.6f, "El enemigo se cayó por debajo del piso");
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

        PoolManager.Instance.CreatePool("Auto", prefab, 1, 1, false);

        // Primer uso: vuelve solo
        PoolManager.Instance.Spawn("Auto", Vector3.zero);
        yield return new WaitForSeconds(0.4f);
        Assert.AreEqual(0, PoolManager.Instance.GetPoolInfo("Auto").active, "No volvió al pool en el primer uso");

        // Segundo uso: no debe volver instantáneamente (antes usaba el tiempo de creación)
        PoolManager.Instance.Spawn("Auto", Vector3.zero);
        yield return null;
        Assert.AreEqual(1, PoolManager.Instance.GetPoolInfo("Auto").active, "Volvió al pool apenas spawneado");
        yield return new WaitForSeconds(0.4f);
        Assert.AreEqual(0, PoolManager.Instance.GetPoolInfo("Auto").active, "No volvió al pool en el segundo uso");

        Object.Destroy(managerGo);
        Object.Destroy(prefab);
    }

    [UnityTest]
    public IEnumerator Game_DisparoNormalYSuperShootSinErrores()
    {
        yield return LoadGame();
        var weapon = Object.FindFirstObjectByType<Weapon>();

        weapon.Shoot();
        Assert.AreEqual(1, PoolManager.Instance.GetPoolInfo("Bullet").active);

        PowerUpManager.Instance.Activate(PowerUpType.SuperShoot);
        weapon.Shoot();
        Assert.AreEqual(1, PoolManager.Instance.GetPoolInfo("BulletSuper").active, "El SuperShoot no spawneó bala");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Game_BalaQueNoChocaVuelveAlPool()
    {
        yield return LoadGame();

        // Disparo hacia arriba: no choca con nada
        PoolManager.Instance.Spawn("Bullet", new Vector3(0f, 20f, 0f), Quaternion.Euler(0f, 0f, 90f));
        Assert.AreEqual(1, PoolManager.Instance.GetPoolInfo("Bullet").active);

        yield return new WaitForSeconds(3.5f);
        Assert.AreEqual(0, PoolManager.Instance.GetPoolInfo("Bullet").active, "La bala perdida nunca volvió al pool");
    }

    [UnityTest]
    public IEnumerator Game_MuteSobreviveAlReinicio()
    {
        PlayerPrefs.SetInt("SfxMuted", 0);
        PlayerPrefs.SetInt("MusicMuted", 0);
        yield return LoadGame();

        AudioManager.instance.ToggleMusicMute();
        Assert.IsTrue(AudioManager.instance.MusicMuted);

        Object.FindFirstObjectByType<MenuManager>().Restart();
        yield return null;
        yield return null;

        Assert.IsTrue(AudioManager.instance.MusicMuted, "El mute de música se perdió al reiniciar");
        Assert.IsFalse(AudioManager.instance.SfxMuted);
    }

    [UnityTest]
    public IEnumerator Game_EnemigoMuereConExplosionQueVuelveAlPool()
    {
        yield return LoadGame();

        var enemyGo = PoolManager.Instance.Spawn("Enemy", new Vector3(30f, 0f, 0f));
        enemyGo.GetComponent<Enemy>().TakeDamage(999);

        Assert.AreEqual(0, PoolManager.Instance.GetPoolInfo("Enemy").active);
        Assert.AreEqual(1, PoolManager.Instance.GetPoolInfo("DeathFX").active, "No apareció la explosión");
        Assert.AreEqual(1, PoolManager.Instance.GetPoolInfo("Coin").active, "No soltó moneda");

        yield return new WaitForSeconds(3f); // duración + lifetime de las partículas
        var fx = PoolManager.Instance.GetPoolInfo("DeathFX");
        Assert.AreEqual(0, fx.active, "La explosión no volvió al pool");
        Assert.AreEqual(fx.total, fx.available, "La explosión se destruyó en vez de volver al pool");
    }

    [UnityTest]
    public IEnumerator Game_AgarrarMonedaSumaAlContador()
    {
        yield return LoadGame();
        var player = GameObject.FindWithTag("Player");
        int before = CoinCounter.Instance.coinValue;

        PoolManager.Instance.Spawn("Coin", player.transform.position);
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        yield return null;

        Assert.AreEqual(before + 1, CoinCounter.Instance.coinValue);
        Assert.AreEqual(0, PoolManager.Instance.GetPoolInfo("Coin").active, "La moneda no se despawneó");
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
        Assert.IsTrue(DeadthCreen.IsGameOver);
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsFalse(ScoreManager.instance.scoreIncreasing, "El puntaje siguió sumando muerto");

        // Golpes y pausa después de morir no hacen nada
        health.PlayerDamage(5);
        Object.FindFirstObjectByType<PauseMenu>().Pause();
        Assert.IsFalse(PauseMenu.GameIsPaused);
    }

    [UnityTest]
    public IEnumerator Game_ReiniciarDejaTodoLimpio()
    {
        yield return LoadGame();
        PoolManager.Instance.Spawn("Box", new Vector3(30f, 0f, 0f));
        ScoreManager.instance.scoreCount = 500f;
        Object.FindFirstObjectByType<PlayerHealth>().PlayerDamage(999);
        var oldPoolManager = PoolManager.Instance;

        Object.FindFirstObjectByType<MenuManager>().Restart();
        yield return null;
        yield return null;

        Assert.AreNotSame(oldPoolManager, PoolManager.Instance, "El PoolManager sobrevivió al reinicio");
        Assert.AreEqual(0, PoolManager.Instance.GetPoolInfo("Box").active, "Quedaron obstáculos de la partida anterior");
        Assert.Less(ScoreManager.instance.scoreCount, 1f, "El puntaje no se reinició");
        Assert.IsTrue(ScoreManager.instance.scoreIncreasing);
        Assert.IsFalse(DeadthCreen.IsGameOver);
        Assert.AreEqual(1f, Time.timeScale);

        yield return new WaitForSeconds(0.5f); // que corran varios Update sin MissingReferenceException
    }
}
