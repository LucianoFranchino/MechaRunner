using UnityEngine;

/// <summary>
/// Réplica exacta del salto del Robot (Player.Jump + ApplyBetterJump + física de Unity con paso fijo),
/// para diseñar con números reales: arcos de monedas que siempre se pueden agarrar y huecos que
/// siempre se pueden saltar. Los valores por defecto son los del prefab Robot.
/// </summary>
public struct JumpModel
{
    public float jumpForce;
    public float gravityScale;
    public float fallMultiplier;
    public float lowJumpMultiplier;
    public float fixedDeltaTime;

    public static JumpModel Default => new JumpModel
    {
        jumpForce = 10f,
        gravityScale = 2f,
        fallMultiplier = 2.5f,
        lowJumpMultiplier = 2f,
        fixedDeltaTime = 0.02f,
    };

    /// <summary>
    /// Altura de los pies sobre el punto de salto en el instante <paramref name="time"/>.
    /// <paramref name="doubleJumpAt"/> &lt; 0 = sin doble salto. Devuelve 0 después de aterrizar.
    /// </summary>
    public float HeightAt(float time, float doubleJumpAt = -1f)
    {
        float g = Physics2D.gravity.y == 0f ? -9.81f : Physics2D.gravity.y;
        float y = 0f, v = jumpForce, t = 0f;
        bool doubled = doubleJumpAt < 0f;
        while (t < time)
        {
            // FixedUpdate de Player (antes del paso de física)
            if (v < 0f) v += g * (fallMultiplier - 1f) * fixedDeltaTime;
            else if (v > 0f) v += g * (lowJumpMultiplier - 1f) * fixedDeltaTime;
            // Paso de física: Euler semi-implícito
            v += g * gravityScale * fixedDeltaTime;
            y += v * fixedDeltaTime;
            t += fixedDeltaTime;
            if (!doubled && t >= doubleJumpAt) { v = jumpForce; doubled = true; }
            if (y <= 0f && t > fixedDeltaTime * 2f) return 0f;
        }
        return Mathf.Max(0f, y);
    }

    /// <summary>Tiempo total en el aire (segundos).</summary>
    public float AirTime(float doubleJumpAt = -1f)
    {
        float g = Physics2D.gravity.y == 0f ? -9.81f : Physics2D.gravity.y;
        float y = 0f, v = jumpForce, t = 0f;
        bool doubled = doubleJumpAt < 0f;
        while (t < 10f)
        {
            if (v < 0f) v += g * (fallMultiplier - 1f) * fixedDeltaTime;
            else if (v > 0f) v += g * (lowJumpMultiplier - 1f) * fixedDeltaTime;
            v += g * gravityScale * fixedDeltaTime;
            y += v * fixedDeltaTime;
            t += fixedDeltaTime;
            if (!doubled && t >= doubleJumpAt) { v = jumpForce; doubled = true; }
            if (y <= 0f && t > fixedDeltaTime * 2f) return t;
        }
        return t;
    }

    /// <summary>Momento del punto más alto de un salto simple (para hacer el doble salto ahí).</summary>
    public float ApexTime()
    {
        float g = Physics2D.gravity.y == 0f ? -9.81f : Physics2D.gravity.y;
        float accel = g * gravityScale + g * (lowJumpMultiplier - 1f);
        return -jumpForce / accel;
    }
}
