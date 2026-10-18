using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Guardado central del juego: un archivo JSON en Application.persistentDataPath.
/// - Los datos viven en memoria (Data); quien los cambia llama a MarkDirty o a Save.
/// - Se escribe a disco en momentos puntuales (fin de partida, pausa, salir de escena,
///   app minimizada o cerrada), nunca por frame.
/// - Escritura segura: primero un .tmp y después se reemplaza el archivo, dejando el
///   anterior como .bak. Si el principal está dañado se usa el .bak.
/// - La primera vez importa lo que había en PlayerPrefs (versiones anteriores del juego).
/// </summary>
public static class SaveSystem
{
    public const string FileName = "save.json";

    // Claves de PlayerPrefs de versiones anteriores: solo se leen para migrar
    private const string LegacyCoinsKey = "Coins";
    private const string LegacyHighscoreKey = "highscore";
    private const string LegacyMusicMutedKey = "MusicMuted";
    private const string LegacySfxMutedKey = "SfxMuted";

    private static SaveData data;
    private static string directory;

    /// <summary>Datos del jugador. Se cargan del disco la primera vez que se piden.</summary>
    public static SaveData Data
    {
        get
        {
            if (data == null) Load();
            return data;
        }
    }

    /// <summary>Hay cambios en memoria que todavía no se escribieron.</summary>
    public static bool IsDirty { get; private set; }

    public static string Directory => directory ?? Application.persistentDataPath;
    public static string FilePath => Path.Combine(Directory, FileName);
    private static string BackupPath => FilePath + ".bak";
    private static string TempPath => FilePath + ".tmp";

    public static void MarkDirty() => IsDirty = true;

    public static void SaveIfDirty()
    {
        if (IsDirty) Save();
    }

    public static void Save()
    {
        if (data == null) return; // nada cargado, nada que escribir
        try
        {
            System.IO.Directory.CreateDirectory(Directory);
            File.WriteAllText(TempPath, JsonUtility.ToJson(data, true));
            if (File.Exists(FilePath))
            {
                try
                {
                    File.Replace(TempPath, FilePath, BackupPath);
                }
                catch (Exception)
                {
                    // Algunos sistemas de archivos no soportan Replace: copia manual del respaldo
                    File.Copy(FilePath, BackupPath, true);
                    File.Delete(FilePath);
                    File.Move(TempPath, FilePath);
                }
            }
            else
            {
                File.Move(TempPath, FilePath);
            }
            IsDirty = false;
        }
        catch (Exception e)
        {
            // Sin espacio o sin permisos: el juego sigue y se reintenta en el próximo guardado
            Debug.LogWarning($"SaveSystem: no se pudo guardar ({e.Message}).");
        }
    }

    /// <summary>Lee el archivo (o su respaldo). Si no existe, migra PlayerPrefs o empieza de cero.</summary>
    public static void Load()
    {
        data = TryRead(FilePath) ?? TryRead(BackupPath);
        if (data == null)
        {
            data = FromLegacyPrefs() ?? new SaveData();
            IsDirty = true; // crear el archivo en el próximo guardado
        }
        else
        {
            IsDirty = Upgrade(data);
        }
    }

    // ------------------------------------------------------------------ herramientas y tests

    /// <summary>
    /// Cambia la carpeta del guardado (null = la normal) y descarta lo cargado en memoria.
    /// Para tests y herramientas: el juego usa la carpeta por defecto.
    /// </summary>
    public static void UseDirectory(string path)
    {
        directory = path;
        data = null;
        IsDirty = false;
    }

    /// <summary>Datos nuevos en memoria sin leer el disco ni migrar PlayerPrefs (tests).</summary>
    public static void ResetToDefaults()
    {
        data = new SaveData();
        IsDirty = true;
    }

    /// <summary>Borra el guardado de la carpeta actual (archivo, respaldo y temporal).</summary>
    public static void DeleteFiles()
    {
        foreach (var path in new[] { FilePath, BackupPath, TempPath })
            if (File.Exists(path)) File.Delete(path);
        data = null;
        IsDirty = false;
    }

    // ------------------------------------------------------------------ internos

    private static SaveData TryRead(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path);
            var loaded = JsonUtility.FromJson<SaveData>(json);
            if (loaded == null || loaded.version <= 0) throw new FormatException("contenido inválido");
            return loaded;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SaveSystem: no se pudo leer {Path.GetFileName(path)} ({e.Message}).");
            return null;
        }
    }

    /// <summary>Completa secciones faltantes y migra versiones viejas. Devuelve si cambió algo.</summary>
    private static bool Upgrade(SaveData loaded)
    {
        bool changed = false;
        if (loaded.progress == null) { loaded.progress = new PlayerProgress(); changed = true; }
        if (loaded.settings == null) { loaded.settings = new GameSettings(); changed = true; }
        // Las migraciones de formato van acá, una por versión: if (loaded.version < 2) { ... }
        if (loaded.version < SaveData.CurrentVersion) { loaded.version = SaveData.CurrentVersion; changed = true; }
        return changed;
    }

    private static SaveData FromLegacyPrefs()
    {
        if (!PlayerPrefs.HasKey(LegacyCoinsKey) && !PlayerPrefs.HasKey(LegacyHighscoreKey)
            && !PlayerPrefs.HasKey(LegacyMusicMutedKey) && !PlayerPrefs.HasKey(LegacySfxMutedKey))
            return null;

        // Las claves viejas quedan como están: si algo falla, la versión anterior sigue funcionando
        var migrated = new SaveData();
        migrated.progress.coins = PlayerPrefs.GetInt(LegacyCoinsKey, 0);
        migrated.progress.bestScore = PlayerPrefs.GetFloat(LegacyHighscoreKey, 0f);
        migrated.settings.musicMuted = PlayerPrefs.GetInt(LegacyMusicMutedKey, 0) == 1;
        migrated.settings.sfxMuted = PlayerPrefs.GetInt(LegacySfxMutedKey, 0) == 1;
        return migrated;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        // Con "Enter Play Mode" sin recarga de dominio los estáticos sobreviven entre ejecuciones
        data = null;
        directory = null;
        IsDirty = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateLifecycleHook()
    {
        var go = new GameObject("SaveSystem");
        go.hideFlags = HideFlags.HideInHierarchy;
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<SaveLifecycle>();
    }

    /// <summary>Guarda lo pendiente cuando la app se minimiza o se cierra (Android puede matarla después).</summary>
    private sealed class SaveLifecycle : MonoBehaviour
    {
        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveIfDirty();
        }

        private void OnApplicationQuit() => SaveIfDirty();
    }
}
