using UnityEditor;

public class AddSceneToBuild
{
    public static void Execute()
    {
        const string scenePath = "Assets/CameraSphereVignette.unity";
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
            if (s.path == scenePath) { UnityEngine.Debug.Log("[Build] Scene already in build settings."); return; }

        var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
        scenes.CopyTo(newScenes, 0);
        newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = newScenes;
        UnityEngine.Debug.Log($"[Build] Added {scenePath} to build settings at index {scenes.Length}.");
    }
}
