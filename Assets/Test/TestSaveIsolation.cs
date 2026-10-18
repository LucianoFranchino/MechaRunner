using System.IO;
using UnityEngine;

/// <summary>
/// Aísla el guardado durante los tests: cada test arranca con datos nuevos en una carpeta
/// temporal propia, así nunca lee ni pisa el guardado real del jugador.
/// </summary>
public static class TestSaveIsolation
{
    public static string Directory => Path.Combine(Application.temporaryCachePath, "SaveTests");

    public static void Begin()
    {
        SaveSystem.UseDirectory(Directory);
        SaveSystem.DeleteFiles();
        SaveSystem.ResetToDefaults();
    }

    public static void End()
    {
        SaveSystem.UseDirectory(Directory);
        SaveSystem.DeleteFiles();
        SaveSystem.UseDirectory(null); // vuelve al guardado real (se recarga cuando se pida)
    }
}
