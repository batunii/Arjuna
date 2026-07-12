using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-shot build trigger invoked via coplay execute_script. Reloads the active scene
// from disk first (so external text edits to the open scene are picked up), then runs
// the study build (all enabled Build Settings scenes).
public static class DoBuildNow
{
    public static void Execute()
    {
        var active = EditorSceneManager.GetActiveScene();
        if (!active.isDirty && !string.IsNullOrEmpty(active.path))
        {
            Debug.Log($"[DoBuildNow] Reloading active scene from disk: {active.path}");
            EditorSceneManager.OpenScene(active.path, OpenSceneMode.Single);
        }
        else
        {
            Debug.Log($"[DoBuildNow] Active scene dirty or unsaved — building without reload.");
        }

        // BuildPlayer cannot run inside coplay's editor sync-context tick
        // ("cannot be executed while inside the player loop"). Defer to the next
        // editor update, which is a valid build context (same as a menu click).
        EditorApplication.delayCall += () =>
        {
            Debug.Log("[DoBuildNow] delayCall firing — starting build.");
            StudyBuild.BuildStudyApk();
        };
    }
}
