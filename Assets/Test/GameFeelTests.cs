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
}
