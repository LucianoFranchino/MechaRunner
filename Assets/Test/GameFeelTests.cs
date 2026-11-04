using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Game feel del salto (coyote time y jump buffer) y sacudida de cámara al recibir daño (GameLab).
/// </summary>
public class GameFeelTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private Player player;
    private Rigidbody2D rb;
    private float groundTop;

    [SetUp]
    public void IsolateSave() => TestSaveIsolation.Begin();

    [TearDown]
    public void Cleanup()
    {
        Time.timeScale = 1f;
        TestSaveIsolation.End();
        if (MusicTransition.Source != null) Object.Destroy(MusicTransition.Source.gameObject);
    }

    private IEnumerator LoadAndLand()
    {
        TestScenes.Load("GameLab");
        yield return null;
        yield return null;
        Object.FindFirstObjectByType<LevelDirector>().enabled = false; // sin obstáculos que interfieran
        var health = Object.FindFirstObjectByType<PlayerHealth>();
        typeof(PlayerHealth).GetField("playerHealth", Private).SetValue(health, 1000);
        player = Object.FindFirstObjectByType<Player>();
        rb = player.GetComponent<Rigidbody2D>();
        groundTop = GameObject.Find("_plat").GetComponent<Collider2D>().bounds.max.y;
        for (int i = 0; i < 120 && !player.IsGrounded; i++) yield return null;
        yield return new WaitForSeconds(0.2f);
        Assert.IsTrue(player.IsGrounded, "El Robot no llegó al piso");
    }

    private void Lift(float height, float verticalSpeed)
    {
        var p = rb.position; p.y += height; rb.position = p;
        player.transform.position = p;
        rb.linearVelocity = new Vector2(0f, verticalSpeed);
        Physics2D.SyncTransforms();
    }

    [UnityTest]
    public IEnumerator CoyoteTime_SaltoNormalJustoDespuesDeDejarElPiso()
    {
        yield return LoadAndLand();
        // Recién salió del piso (como al pasar el borde de un hueco): en el aire, sin haber saltado
        Lift(0.6f, 0f);
        yield return null;
        Assert.IsFalse(player.IsGrounded, "Tiene que estar en el aire");
        player.JumpButton();
        Assert.Greater(rb.linearVelocity.y, 1f, "Dentro del margen de coyote tiene que saltar");
        Assert.IsFalse(player.HasDoubleJumped, "Tiene que ser el salto normal, no el doble");

        yield return new WaitForFixedUpdate();
        player.JumpButton();
        Assert.IsTrue(player.HasDoubleJumped, "Después del salto de coyote todavía queda el doble salto");
    }

    [UnityTest]
    public IEnumerator JumpBuffer_SaltoPedidoJustoAntesDeAterrizarSeHaceAlTocarElPiso()
    {
        yield return LoadAndLand();
        // Sin saltos disponibles y cayendo, muy cerca del piso
        Lift(0.35f, -4f);
        typeof(Player).GetField("hasDoubleJumped", Private).SetValue(player, true);
        typeof(Player).GetField("groundJumpUsed", Private).SetValue(player, true);
        typeof(Player).GetField("lastGroundedTime", Private).SetValue(player, float.NegativeInfinity);
        player.JumpButton();

        bool jumped = false;
        for (float t = 0f; t < 0.5f && !jumped; t += Time.deltaTime)
        {
            yield return null;
            jumped = rb.linearVelocity.y > 1f;
        }
        Assert.IsTrue(jumped, "El salto pedido justo antes de aterrizar no se hizo al tocar el piso");
    }

    [UnityTest]
    public IEnumerator JumpBuffer_PedidoMuyAntesNoSalta()
    {
        yield return LoadAndLand();
        Lift(3f, 0f); // lejos del piso: tarda más que el buffer en aterrizar
        typeof(Player).GetField("hasDoubleJumped", Private).SetValue(player, true);
        typeof(Player).GetField("groundJumpUsed", Private).SetValue(player, true);
        typeof(Player).GetField("lastGroundedTime", Private).SetValue(player, float.NegativeInfinity);
        player.JumpButton();

        for (int i = 0; i < 200 && !player.IsGrounded; i++) yield return new WaitForFixedUpdate();
        yield return new WaitForSeconds(0.2f);
        Assert.LessOrEqual(rb.linearVelocity.y, 0.1f, "Un salto pedido mucho antes de aterrizar no se tiene que hacer");
    }

    [UnityTest]
    public IEnumerator Danio_SacudeLaCamaraYVuelveASuLugar()
    {
        yield return LoadAndLand();
        var cam = Camera.main.transform;
        Assert.IsNotNull(cam.parent, "La cámara tiene que colgar del objeto de sacudida");
        Vector3 rest = cam.position;

        Object.FindFirstObjectByType<PlayerHealth>().PlayerDamage(1);
        float maxOffset = 0f;
        for (float t = 0f; t < 0.4f; t += Time.unscaledDeltaTime)
        {
            yield return null;
            maxOffset = Mathf.Max(maxOffset, Vector3.Distance(cam.position, rest));
        }
        Assert.Greater(maxOffset, 0.03f, "La cámara no se sacudió al recibir daño");
        Assert.Less(Vector3.Distance(cam.position, rest), 0.001f, "La cámara no volvió a su lugar después de la sacudida");
    }

    [UnityTest]
    public IEnumerator Danio_HitstopCongelaUnInstanteYVuelveALaVelocidadPrevia()
    {
        yield return LoadAndLand();
        Assert.AreEqual(1f, Time.timeScale, 1e-4f);
        Object.FindFirstObjectByType<PlayerHealth>().PlayerDamage(1);
        Assert.AreEqual(0f, Time.timeScale, "Al recibir daño el juego se tiene que congelar en el mismo instante del golpe");

        yield return new WaitForSecondsRealtime(0.04f);
        Assert.AreEqual(0f, Time.timeScale, "El congelamiento es más corto que el golpe del sonido");
        yield return new WaitForSecondsRealtime(0.15f);
        Assert.AreEqual(1f, Time.timeScale, 1e-4f, "El juego no volvió a su velocidad después del hitstop");
    }

    [UnityTest]
    public IEnumerator Enemigos_HitFlashBlancoYVuelveASuMaterial()
    {
        yield return LoadAndLand();
        float ground = GameObject.Find("_plat").GetComponent<Collider2D>().bounds.max.y;
        foreach (var path in new[] { "Enemies/Enemigo", "Lab/Lab_Guard" })
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + path + ".prefab");
            var enemy = PoolManager.Instance.Spawn(prefab, new Vector3(6f, ground + 1f, 0f));
            var flash = enemy.GetComponent<HitFlash>();
            Assert.IsNotNull(flash, path + " no tiene HitFlash");
            var part = enemy.GetComponentInChildren<SpriteRenderer>();
            var original = part.sharedMaterial;

            enemy.GetComponent<Enemy>().TakeDamage(1);
            Assert.IsTrue(flash.IsFlashing, path + " no destelló al recibir daño");
            Assert.AreEqual("MechaRunner/SpriteFlash", part.sharedMaterial.shader.name);
            Assert.AreEqual(Color.white, part.sharedMaterial.GetColor("_FlashColor"), "El destello tiene que ser blanco");

            yield return new WaitForSeconds(0.15f);
            Assert.IsFalse(flash.IsFlashing);
            Assert.AreSame(original, part.sharedMaterial, path + " no volvió a su material");
            enemy.GetComponent<PooledObject>().Despawn();
#else
            yield break;
#endif
        }
    }

#if UNITY_EDITOR
    private static int ActiveFx(string prefix)
    {
        int n = 0;
        foreach (var po in Object.FindObjectsByType<PooledObject>(FindObjectsSortMode.None))
            if (po.gameObject.activeInHierarchy && po.name.StartsWith(prefix)) n++;
        return n;
    }

    [UnityTest]
    public IEnumerator Enemigo_ChispasAlImpactoPiezasYExplosionAlMorirYSacudidaSuave()
    {
        yield return LoadAndLand();
        float ground = GameObject.Find("_plat").GetComponent<Collider2D>().bounds.max.y;
        var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Enemigo.prefab");
        var hitSound = new UnityEditor.SerializedObject(prefab.GetComponent<Enemy>()).FindProperty("hitSound").objectReferenceValue;
        Assert.IsNotNull(hitSound, "El Enemigo no tiene sonido de golpe");
        Assert.IsNotNull(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Lab/Lab_Guard.prefab").GetComponent<Enemy>(), "Falta el guardia");

        var enemy = PoolManager.Instance.Spawn(prefab, new Vector3(player.transform.position.x + 4f, ground + 1f, 0f));
        var body = enemy.GetComponentInChildren<Collider2D>();
        Physics2D.SyncTransforms();

        // Bala real contra el enemigo: chispas en el impacto
        var bulletPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bullet.prefab");
        var bullet = PoolManager.Instance.Spawn(bulletPrefab, new Vector3(body.bounds.min.x - 0.6f, body.bounds.center.y, 0f));
        for (int i = 0; i < 30 && ActiveFx("Fx_HitSparks") == 0; i++) yield return new WaitForFixedUpdate();
        Assert.Greater(ActiveFx("Fx_HitSparks"), 0, "La bala no generó chispas en el impacto");

        // Golpe final: piezas de robot, explosión y sacudida suave
        var cam = Camera.main.transform; Vector3 rest = cam.position;
        enemy.GetComponent<Enemy>().TakeDamage(100);
        Assert.Greater(ActiveFx("Fx_RobotParts"), 0, "No salieron las piezas de robot");
        Assert.Greater(ActiveFx("Fx_Explosion"), 0, "No apareció la explosión");
        float maxOffset = 0f;
        for (float t = 0f; t < 0.3f; t += Time.unscaledDeltaTime) { yield return null; maxOffset = Mathf.Max(maxOffset, Vector3.Distance(cam.position, rest)); }
        Assert.Greater(maxOffset, 0.01f, "La cámara no se sacudió al destruir el enemigo");
        yield return new WaitForSeconds(0.7f);
        Assert.AreEqual(0, ActiveFx("Fx_Explosion"), "La explosión no volvió al pool al terminar");
    }
#endif

    [UnityTest]
    public IEnumerator Disparo_DesvioSutilTonoGraveYDestelloMayorConSuperShot()
    {
        yield return LoadAndLand();
        var weapon = Object.FindFirstObjectByType<Weapon>();
        var flash = Object.FindFirstObjectByType<MuzzleFlash>();
        Assert.IsNotNull(flash, "El Robot no tiene destello de cañón");
        var pitches = new System.Collections.Generic.HashSet<float>();
        var angles = new System.Collections.Generic.HashSet<int>();
        float lastPitch = -1f;
        for (int i = 0; i < 20; i++)
        {
            weapon.Shoot();
            float pitch = AudioManager.instance.LastShotPitch;
            Assert.Less(pitch, 1f, "El disparo tiene que sonar un poco más grave");
            Assert.AreNotEqual(lastPitch, pitch, "El tono no tiene que repetirse dos disparos seguidos");
            lastPitch = pitch; pitches.Add(pitch);
            yield return null;
        }
        Assert.AreEqual(3, pitches.Count, "Tienen que usarse las tres variantes de tono");
        foreach (var b in Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None))
        {
            if (!b.gameObject.activeInHierarchy) continue;
            float a = Mathf.DeltaAngle(0f, b.transform.eulerAngles.z);
            Assert.LessOrEqual(Mathf.Abs(a), 1.5f + 1e-3f, "Desvío más grande que el recoil sutil");
            angles.Add(Mathf.RoundToInt(a * 100f));
        }
        Assert.Greater(angles.Count, 3, "Las balas tienen que salir con pequeñas variaciones, no todas rectas");

        // Destello: aparece y se apaga solo; con SUPER SHOT es más grande
        yield return new WaitForSeconds(0.2f);
        Assert.IsFalse(flash.IsShowing, "El destello no se apagó");
        weapon.Shoot();
        Assert.IsTrue(flash.IsShowing);
        float normalScale = flash.transform.localScale.x;
        yield return new WaitForSeconds(0.2f);
        PowerUpManager.Instance.Activate(PowerUpType.SuperShoot);
        weapon.Shoot();
        Assert.Greater(flash.transform.localScale.x, normalScale * 1.2f, "Con SUPER SHOT el destello tiene que ser más grande");
    }

#if UNITY_EDITOR
    [Test]
    public void Animaciones_CaminataCierraElCicloYPinchosGiran360()
    {
        var walk = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/EnemyMove.anim");
        foreach (var b in UnityEditor.AnimationUtility.GetCurveBindings(walk))
        {
            var c = UnityEditor.AnimationUtility.GetEditorCurve(walk, b);
            Assert.AreEqual(c.Evaluate(0f), c.Evaluate(walk.length), 1e-3f, $"{b.path}.{b.propertyName} salta al cerrar el ciclo");
        }
        var spin = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/SpikesEnemMov.anim");
        var z = UnityEditor.AnimationUtility.GetEditorCurve(spin, UnityEditor.EditorCurveBinding.FloatCurve("Pinches", typeof(Transform), "localEulerAnglesRaw.z"));
        Assert.AreEqual(360f, z.Evaluate(spin.length) - z.Evaluate(0f), 1e-3f, "Los pinchos tienen que dar una vuelta completa");
        // Velocidad constante: a mitad de la animación, media vuelta
        Assert.AreEqual(180f, z.Evaluate(spin.length * 0.5f), 0.5f, "El giro no es parejo");
        Assert.AreEqual(90f, z.Evaluate(spin.length * 0.25f), 0.5f, "El giro no es parejo");
    }
#endif
}
