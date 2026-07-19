// Editor menu to drop the PointAuthoringTool into the open scene (and pull it back out).
// Wires the SelectionDotMat marker template and the VideoTestSceneManager so it runs on-device.
// When present the tool disables TestModeSequencer at runtime, so authoring and the normal test
// harness never run at once.

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PassthroughCameraSamples.ShaderSample;
using PassthroughCameraSamples.ShaderSample.Study;

public static class PointAuthoringMenu
{
    [MenuItem("Meta/Study/Point Authoring/Add To Open Scene")]
    public static void Add()
    {
        var existing = Object.FindObjectOfType<PointAuthoringTool>();
        if (existing != null)
        {
            Debug.Log("[PointAuthoringMenu] PointAuthoringTool already in the scene.");
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        var go = new GameObject("PointAuthoringTool");
        var tool = go.AddComponent<PointAuthoringTool>();

        var so = new SerializedObject(tool);
        var matProp = so.FindProperty("m_markerMaterialTemplate");
        if (matProp != null)
        {
            Material dot = FindDotMaterial();
            if (dot != null) matProp.objectReferenceValue = dot;
        }
        var videoProp = so.FindProperty("m_video");
        if (videoProp != null)
        {
            var video = Object.FindObjectOfType<VideoTestSceneManager>();
            if (video != null) videoProp.objectReferenceValue = video;
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log("[PointAuthoringMenu] Added PointAuthoringTool. Play/build the video scene to author points " +
                  "(left trigger = mark, X = play/pause, B = log CSV path).");
    }

    [MenuItem("Meta/Study/Point Authoring/Remove From Open Scene")]
    public static void Remove()
    {
        var tool = Object.FindObjectOfType<PointAuthoringTool>();
        if (tool == null) { Debug.Log("[PointAuthoringMenu] No PointAuthoringTool in the scene."); return; }
        var scene = tool.gameObject.scene;
        Object.DestroyImmediate(tool.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[PointAuthoringMenu] Removed PointAuthoringTool.");
    }

    [MenuItem("Meta/Study/Authored Study/Add Presenter To Open Scene")]
    public static void AddPresenter()
    {
        var existing = Object.FindObjectOfType<AuthoredTargetPresenter>();
        if (existing != null) { Selection.activeGameObject = existing.gameObject; return; }
        var go = new GameObject("AuthoredTargetPresenter");
        var pres = go.AddComponent<AuthoredTargetPresenter>();
        var so = new SerializedObject(pres);
        var videoProp = so.FindProperty("m_video");
        if (videoProp != null)
        {
            var video = Object.FindObjectOfType<VideoTestSceneManager>();
            if (video != null) videoProp.objectReferenceValue = video;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log("[PointAuthoringMenu] Added AuthoredTargetPresenter. Set m_participantId + m_mode, "
                + "push pool_split.csv to the device persistentDataPath, then build the video scene.");
    }

    [MenuItem("Meta/Study/Authored Study/Remove Presenter From Open Scene")]
    public static void RemovePresenter()
    {
        var pres = Object.FindObjectOfType<AuthoredTargetPresenter>();
        if (pres == null) { Debug.Log("[PointAuthoringMenu] No AuthoredTargetPresenter in the scene."); return; }
        var scene = pres.gameObject.scene;
        Object.DestroyImmediate(pres.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[PointAuthoringMenu] Removed AuthoredTargetPresenter.");
    }

    private static Material FindDotMaterial()
    {
        foreach (string guid in AssetDatabase.FindAssets("SelectionDotMat t:Material"))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (mat != null) return mat;
        }
        Debug.LogWarning("[PointAuthoringMenu] SelectionDotMat not found — assign the marker template manually for Android builds.");
        return null;
    }
}
