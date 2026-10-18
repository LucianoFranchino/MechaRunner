using System;

/// <summary>
/// Todo lo que se guarda del jugador, en un único JSON. Cada sistema tiene su sección:
/// las funciones nuevas (tienda, personajes) agregan la suya sin tocar las existentes.
/// Al cambiar el formato de una sección existente hay que subir CurrentVersion y
/// migrar en SaveSystem.Upgrade.
/// </summary>
[Serializable]
public class SaveData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public PlayerProgress progress = new PlayerProgress();
    public GameSettings settings = new GameSettings();
}

[Serializable]
public class PlayerProgress
{
    public int coins;
    public float bestScore;
}

[Serializable]
public class GameSettings
{
    public bool musicMuted;
    public bool sfxMuted;
}
