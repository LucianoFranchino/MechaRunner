using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

/// <summary>
/// Partida larga en GameLab con piloto automático: salta con swipes táctiles reales lo que tiene adelante,
/// dispara, usa SUPER SHOT y cada tanto llega ruido de un gamepad (como uno conectado a la PC).
/// Busca estados en los que el Robot no puede saltar: apoyado y quieto pero "en el aire", o un salto pedido
/// desde el piso que no ocurre. Es lenta: solo corre si existe Temp/run_soak.flag en el proyecto.
/// </summary>
public class LongRunSoakTests
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly FieldInfo IsGroundedField = typeof(Player).GetField("isGrounded", Private);
    private static readonly FieldInfo HasDoubleJumpedField = typeof(Player).GetField("hasDoubleJumped", Private);
    private static readonly FieldInfo GroundCheckField = typeof(Player).GetField("groundCheck", Private);
    private static readonly FieldInfo HealthField = typeof(PlayerHealth).GetField("playerHealth", Private);

    private Gamepad pad;
    private Touchscreen addedTouch;

    [SetUp]
    public void IsolateSave() => TestSaveIsolation.Begin();

    [TearDown]
    public void RestoreSave()
    {
        if (pad != null) InputSystem.RemoveDevice(pad);
        if (addedTouch != null) InputSystem.RemoveDevice(addedTouch);
        pad = null; addedTouch = null;
        Time.timeScale = 1f;
        TestSaveIsolation.End();
        if (MusicTransition.Source != null) Object.Destroy(MusicTransition.Source.gameObject);
    }

    [UnityTest, Category("Soak"), Timeout(900000)]
    public IEnumerator PartidaLarga_ElRobotSiemprePuedeSaltar()
    {
        if (!System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "../Temp/run_soak.flag")))
            Assert.Ignore("Partida larga: crear Temp/run_soak.flag para correrla");
        var touch = Touchscreen.current;
        if (touch == null) touch = addedTouch = InputSystem.AddDevice<Touchscreen>();
        pad = InputSystem.AddDevice<Gamepad>();
        int touchId = 1;
        IEnumerator Swipe()
        {
            touchId++;
            foreach (var (phase, y) in new[] { (UnityEngine.InputSystem.TouchPhase.Began, 200f), (UnityEngine.InputSystem.TouchPhase.Moved, 500f), (UnityEngine.InputSystem.TouchPhase.Ended, 500f) })
            {
                InputSystem.QueueStateEvent(touch, new TouchState { touchId = touchId, phase = phase, position = new Vector2(400f, y) });
                InputSystem.Update();
                yield return null;
            }
        }

        TestScenes.Load("GameLab");
        yield return null;
        yield return null;

        var log = new StringBuilder();
        var anomalies = new List<string>();
        int deaths = 0, jumps = 0, failedJumps = 0;
        float stuck = 0f, nextShot = 0f, nextSuper = 5f, nextPad = 3f, maxTime = 330f;
        bool stuckReported = false;
        float realStart = Time.realtimeSinceStartup;
        var hits = new Collider2D[16];

        Player player = null; Rigidbody2D rb = null; Weapon weapon = null; PlayerHealth health = null; Collider2D ground = null;
        void Bind()
        {
            player = Object.FindFirstObjectByType<Player>();
            rb = player.GetComponent<Rigidbody2D>();
            weapon = Object.FindFirstObjectByType<Weapon>();
            health = Object.FindFirstObjectByType<PlayerHealth>();
            HealthField.SetValue(health, 1000000);
            ground = GameObject.Find("_plat").GetComponent<Collider2D>();
            Time.timeScale = 3f;
            var input = Object.FindFirstObjectByType<PlayerInput>();
            input.SwitchCurrentControlScheme("Touch", touch);
        }
        Bind();

        string Describe(string what)
        {
            var sb = new StringBuilder();
            var dm = DifficultyManager.Instance;
            var dir = Object.FindFirstObjectByType<LevelDirector>();
            var gc = (Transform)GroundCheckField.GetValue(player);
            var body = player.GetComponent<Collider2D>();
            sb.Append($"[{dm.GameTime:0.0}s z{dm.ZoneIndex + 1} {(dir && dir.CurrentSection ? dir.CurrentSection.name : "-")}] {what}: ");
            sb.Append($"pos={player.transform.position:F2} vel={rb.linearVelocity:F2} pies-piso={(body.bounds.min.y - ground.bounds.max.y):F2} ");
            sb.Append($"grounded={IsGroundedField.GetValue(player)} dobleUsado={HasDoubleJumpedField.GetValue(player)} sobreHueco={LevelRules.PlayerOverPit} ");
            sb.Append($"rbSim={rb.simulated} rbTipo={rb.bodyType} grav={rb.gravityScale} constraints={rb.constraints} rot={player.transform.eulerAngles.z:0} ");
            int n = Physics2D.OverlapCircle(gc.position, 0.35f, new ContactFilter2D { useTriggers = true }, hits);
            sb.Append($"groundCheck y={gc.position.y:F2} toca:");
            for (int i = 0; i < n; i++) sb.Append($" {hits[i].name}(capa {LayerMask.LayerToName(hits[i].gameObject.layer)}{(hits[i].isTrigger ? ",trigger" : "")})");
            var contacts = new List<ContactPoint2D>();
            rb.GetContacts(contacts);
            sb.Append(" | contactos:");
            foreach (var c in contacts) sb.Append($" {c.collider.name}/{c.otherCollider.name} n={c.normal:F1}");
            return sb.ToString();
        }

        while (DifficultyManager.Instance.GameTime < maxTime && Time.realtimeSinceStartup - realStart < 600f)
        {
            yield return null;

            if (GameManager.IsGameOver)
            {
                deaths++;
                log.AppendLine("MUERTE " + Describe("game over"));
                if (deaths > 6) break;
                GameManager.Instance.Restart();
                yield return null;
                yield return null;
                Bind();
                stuck = 0f; stuckReported = false; nextShot = 0f; nextSuper = 5f;
                continue;
            }
            if (!GameManager.IsPlaying) continue;

            float t = DifficultyManager.Instance.GameTime;
            bool grounded = (bool)IsGroundedField.GetValue(player);
            var bodyCol = player.GetComponent<Collider2D>();
            float feet = bodyCol.bounds.min.y;
            bool restingOnMainGround = Mathf.Abs(feet - ground.bounds.max.y) < 0.15f && Mathf.Abs(rb.linearVelocity.y) < 0.05f;

            // 1) Quieto y apoyado, pero "en el aire": no puede saltar
            if (!grounded && Mathf.Abs(rb.linearVelocity.y) < 0.05f)
            {
                stuck += Time.deltaTime;
                if (stuck > 0.6f && !stuckReported)
                {
                    stuckReported = true;
                    anomalies.Add(Describe("APOYADO PERO NO GROUNDED"));
                }
            }
            else { stuck = 0f; stuckReported = false; }

            // Piloto: saltar si hay algo adelante a la altura del cuerpo o un hueco
            float x = bodyCol.bounds.max.x;
            float speed = DifficultyManager.Instance.GameSpeed;
            float look = speed * 0.32f;
            bool danger = Pit.Covers(x + look * 0.6f, 0f);
            int n = Physics2D.OverlapArea(new Vector2(x + 0.1f, ground.bounds.max.y + 0.05f), new Vector2(x + look, ground.bounds.max.y + 1.1f), new ContactFilter2D { useTriggers = true }, hits);
            for (int i = 0; i < n && !danger; i++)
            {
                var h = hits[i];
                if (h == ground || h.GetComponentInParent<Player>() || h.GetComponentInParent<Pickup>() || h.GetComponentInParent<Bullet>()) continue;
                if (h.GetComponentInParent<Obstacle>() || h.GetComponentInParent<Enemy>() || h.GetComponentInParent<Hazard>() || h.GetComponentInParent<ExplosiveBarrel>()) danger = true;
            }
            if (danger && grounded && rb.linearVelocity.y <= 0.01f)
            {
                yield return Swipe();
                yield return new WaitForFixedUpdate();
                jumps++;
                if (rb.linearVelocity.y <= 0.1f && (bool)IsGroundedField.GetValue(player))
                {
                    failedJumps++;
                    anomalies.Add(Describe("SWIPE EN EL PISO QUE NO HIZO SALTAR (esquema " + Object.FindFirstObjectByType<PlayerInput>().currentControlScheme + ")"));
                }
                continue;
            }
            else if (danger && !grounded && restingOnMainGround)
            {
                anomalies.Add(Describe("PELIGRO ADELANTE, EN EL PISO Y SIN PODER SALTAR"));
            }

            // Ruido de un gamepad conectado (stick y botón)
            if (t >= nextPad)
            {
                InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(0.6f, 0.1f) }.WithButton(GamepadButton.South));
                InputSystem.Update();
                yield return null;
                InputSystem.QueueStateEvent(pad, new GamepadState());
                InputSystem.Update();
                nextPad = t + 20f;
            }
            if (t >= nextShot) { weapon.Shoot(); nextShot = t + 0.25f; }
            if (t >= nextSuper) { PowerUpManager.Instance.Activate(PowerUpType.SuperShoot); nextSuper = t + 22f; }

            if (anomalies.Count > 40) break;
        }

        log.Insert(0, $"Partida larga: t={DifficultyManager.Instance.GameTime:0}s saltos={jumps} fallidos={failedJumps} muertes={deaths} anomalías={anomalies.Count}\n");
        foreach (var a in anomalies) log.AppendLine(a);
        System.IO.File.WriteAllText(System.IO.Path.Combine(Application.temporaryCachePath, "soak_jump_log.txt"), log.ToString());
        Debug.Log(log.ToString());
        Assert.IsEmpty(anomalies, log.ToString());
    }
}
