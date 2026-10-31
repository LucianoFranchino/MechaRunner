using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

/// <summary>
/// Botones del HUD (GameLab): JUMP a la derecha y FIRE a la izquierda, ambos responden al apretar.
/// El salto por swipe quedó apagado.
/// </summary>
public class HudControlsTests
{
    [SetUp]
    public void IsolateSave() => TestSaveIsolation.Begin();

    [TearDown]
    public void Cleanup()
    {
        Time.timeScale = 1f;
        TestSaveIsolation.End();
        if (MusicTransition.Source != null) Object.Destroy(MusicTransition.Source.gameObject);
    }

    public static void Press(GameObject button)
    {
        var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        ExecuteEvents.Execute(button, data, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(button, data, ExecuteEvents.pointerUpHandler);
    }

    [UnityTest]
    public IEnumerator Botones_JumpSaltaYDobleSalto_FireDisparaAlApretar()
    {
        TestScenes.Load("GameLab");
        yield return null;
        yield return null;

        var controls = GameObject.Find("Canvas").transform.Find("Controls");
        var jump = controls.Find("JumpButton");
        var fire = controls.Find("FireButton");
        Assert.IsNotNull(jump, "Falta el botón JUMP");
        var canvasRect = (RectTransform)jump.GetComponentInParent<Canvas>().transform;
        Assert.Greater(jump.position.x, canvasRect.position.x, "JUMP tiene que estar a la derecha");
        Assert.Less(fire.position.x, canvasRect.position.x, "FIRE tiene que estar a la izquierda");

        var player = Object.FindFirstObjectByType<Player>();
        var rb = player.GetComponent<Rigidbody2D>();
        for (int i = 0; i < 90 && !player.IsGrounded; i++) yield return null;
        Assert.IsTrue(player.IsGrounded, "El Robot no llegó al piso");

        Press(jump.gameObject);
        Assert.Greater(rb.linearVelocity.y, 1f, "JUMP no hizo saltar al Robot");

        // En el aire, bajando: un segundo toque es el doble salto
        for (int i = 0; i < 120 && rb.linearVelocity.y > -0.5f; i++) yield return new WaitForFixedUpdate();
        Assert.IsFalse(player.IsGrounded);
        Press(jump.gameObject);
        Assert.Greater(rb.linearVelocity.y, 1f, "El segundo toque no hizo el doble salto");
        Assert.IsTrue(player.HasDoubleJumped);

        int before = Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None).Length;
        var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        ExecuteEvents.Execute(fire.gameObject, data, ExecuteEvents.pointerDownHandler);
        Assert.Greater(Object.FindObjectsByType<Bullet>(FindObjectsSortMode.None).Length, before, "FIRE no disparó al apretar (sin soltar)");
    }

    [UnityTest]
    public IEnumerator Encuadre_PisoElevadoYDobleSaltoDentroDeCamara()
    {
        TestScenes.Load("GameLab");
        yield return null;
        yield return null;

        var cam = Camera.main;
        float camBottom = cam.transform.position.y - cam.orthographicSize, camTop = cam.transform.position.y + cam.orthographicSize;
        float groundTop = GameObject.Find("_plat").GetComponent<Collider2D>().bounds.max.y;
        Assert.Greater((groundTop - camBottom) / (camTop - camBottom), 0.15f, "El piso sigue pegado al borde inferior");

        var player = Object.FindFirstObjectByType<Player>();
        var rb = player.GetComponent<Rigidbody2D>();
        var jump = GameObject.Find("Canvas").transform.Find("Controls/JumpButton").gameObject;
        for (int i = 0; i < 90 && !player.IsGrounded; i++) yield return null;
        Press(jump);
        yield return new WaitForFixedUpdate();
        while (rb.linearVelocity.y > 0f) yield return new WaitForFixedUpdate();
        Press(jump); // segundo salto en el punto más alto: el peor caso

        float maxTop = float.MinValue;
        for (float t = 0; t < 1.5f; t += Time.fixedDeltaTime)
        {
            yield return new WaitForFixedUpdate();
            foreach (var sr in player.GetComponentsInChildren<SpriteRenderer>()) maxTop = Mathf.Max(maxTop, sr.bounds.max.y);
        }
        Assert.Less(maxTop, camTop, $"El doble salto sale de cámara (tope {maxTop:F2}, cámara {camTop:F2})");
    }
}
