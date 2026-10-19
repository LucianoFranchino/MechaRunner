using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Mantiene horneadas las LevelSection: al guardar un prefab de sección en Prefab Mode se recalcula
/// su lista de elementos. "MechaRunner/Hornear secciones" lo hace con todas.
/// </summary>
[InitializeOnLoad]
public static class SectionBaker
{
    public const string SectionsFolder = "Assets/Prefabs/Lab/Sections";

    static SectionBaker()
    {
        PrefabStage.prefabSaving += root =>
        {
            var section = root.GetComponent<LevelSection>();
            if (section == null) return;
            section.Bake();
            EditorUtility.SetDirty(section);
        };
    }

    [MenuItem("MechaRunner/Hornear secciones")]
    public static void BakeAll()
    {
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { SectionsFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            var section = root.GetComponent<LevelSection>();
            if (section != null)
            {
                section.Bake();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                n++;
            }
            PrefabUtility.UnloadPrefabContents(root);
        }
        Debug.Log($"Secciones horneadas: {n}");
    }
}
