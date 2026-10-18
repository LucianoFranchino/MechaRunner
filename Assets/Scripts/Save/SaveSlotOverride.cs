using System.IO;
using UnityEngine;

/// <summary>
/// Usa una carpeta de guardado propia mientras la escena está cargada (ej. GameLab), para que
/// sus récords y monedas de prueba no se mezclen con el guardado real. Al salir de la escena
/// guarda lo pendiente y vuelve al guardado normal.
/// Corre antes que el resto para que AudioManager, ScoreManager, etc. ya lean este guardado.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class SaveSlotOverride : MonoBehaviour
{
    [Tooltip("Subcarpeta dentro de persistentDataPath.")]
    [SerializeField] private string slotFolder = "Lab";

    // Al reiniciar la escena, el Awake de la nueva instancia puede llegar antes que el OnDestroy de la vieja
    private static SaveSlotOverride active;

    private string SlotPath => Path.Combine(Application.persistentDataPath, slotFolder);

    private void Awake()
    {
        SaveSystem.SaveIfDirty();
        SaveSystem.UseDirectory(SlotPath);
        active = this;
    }

    private void OnDestroy()
    {
        if (active != this) return;
        active = null;
        // Si otro ya cambió la carpeta (ej. un test), no pisarlo
        if (SaveSystem.Directory != SlotPath) return;
        SaveSystem.SaveIfDirty();
        SaveSystem.UseDirectory(null);
    }
}
