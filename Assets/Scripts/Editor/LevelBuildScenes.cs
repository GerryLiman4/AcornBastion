using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds the campaign's level scenes to Build Settings after the existing entries, so MainMenu
/// stays at index 0. Safe to run more than once.
/// </summary>
public static class LevelBuildScenes
{
    private static readonly string[] LevelScenes =
    {
        "Assets/Scenes/Level 1.unity", "Assets/Scenes/Level 2.unity",
        "Assets/Scenes/Level 3.unity", "Assets/Scenes/Level 4.unity",
        "Assets/Scenes/Level 5.unity",
    };

    [MenuItem("FUR/Add Level Scenes To Build Settings")]
    private static void AddLevelScenes()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (string path in LevelScenes)
        {
            if (scenes.All(scene => scene.path != path)) scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("[LevelBuildScenes] Build Settings: " + string.Join(" | ", scenes.Select(scene => scene.path)));
    }
}
