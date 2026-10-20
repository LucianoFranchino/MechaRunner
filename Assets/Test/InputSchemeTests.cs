using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

/// <summary>
/// El salto por swipe no se pierde cuando otro dispositivo (un gamepad conectado a la PC o al teléfono)
/// manda input: antes PlayerInput cambiaba solo al esquema Gamepad y el swipe quedaba sin controles.
/// </summary>
public class InputSchemeTests
{
    private Gamepad pad;
    private Touchscreen addedTouch;

    [SetUp]
    public void IsolateSave() => TestSaveIsolation.Begin();

    [TearDown]
    public void Cleanup()
    {
        if (pad != null) InputSystem.RemoveDevice(pad);
        if (addedTouch != null) InputSystem.RemoveDevice(addedTouch);
        pad = null; addedTouch = null;
        Time.timeScale = 1f;
        TestSaveIsolation.End();
        if (MusicTransition.Source != null) Object.Destroy(MusicTransition.Source.gameObject);
    }

    private int driftFrame;

    // Stick con drift: manda valores que cambian un poco en cada frame, como un control descalibrado
    private void Drift()
    {
        driftFrame++;
        InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(0.55f + 0.05f * (driftFrame % 3), 0.1f) });
    }

    private IEnumerator Touch(Touchscreen touch, UnityEngine.InputSystem.TouchPhase phase, Vector2 position)
    {
        Drift();
        InputSystem.QueueStateEvent(touch, new TouchState { touchId = 1, phase = phase, position = position });
        Drift();
        InputSystem.Update();
        yield return null;
    }

    [UnityTest]
    public IEnumerator GamepadConectado_NoDesactivaElSaltoPorSwipe()
    {
        var touch = Touchscreen.current;
        if (touch == null) touch = addedTouch = InputSystem.AddDevice<Touchscreen>();
        pad = InputSystem.AddDevice<Gamepad>();

        TestScenes.Load("GameLab");
        yield return null;
        yield return null;

        var input = Object.FindFirstObjectByType<PlayerInput>();
        input.SwitchCurrentControlScheme("Touch", touch);
        Assert.AreEqual("Touch", input.currentControlScheme);

        // Un toque del gamepad (stick con ruido o un botón)
        InputSystem.QueueStateEvent(pad, new GamepadState { leftStick = new Vector2(0.8f, 0f) }.WithButton(GamepadButton.South));
        InputSystem.Update();
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(pad, new GamepadState());
        InputSystem.Update();
        yield return null;

        // Esperar a estar en el piso y hacer un swipe hacia arriba mientras el stick sigue con drift
        var player = Object.FindFirstObjectByType<Player>();
        var rb = player.GetComponent<Rigidbody2D>();
        for (int i = 0; i < 60 && Mathf.Abs(rb.linearVelocity.y) > 0.01f; i++) { Drift(); InputSystem.Update(); yield return null; }
        yield return Touch(touch, UnityEngine.InputSystem.TouchPhase.Began, new Vector2(400f, 200f));
        yield return Touch(touch, UnityEngine.InputSystem.TouchPhase.Moved, new Vector2(400f, 500f));
        yield return Touch(touch, UnityEngine.InputSystem.TouchPhase.Ended, new Vector2(400f, 500f));
        yield return new WaitForFixedUpdate();

        Assert.Greater(rb.linearVelocity.y, 1f, "El swipe hacia arriba no hizo saltar al Robot (esquema " + input.currentControlScheme + ")");
        Assert.AreEqual("Touch", input.currentControlScheme, "El gamepad cambió el esquema de control");
    }
}
