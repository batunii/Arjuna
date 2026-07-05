using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PassthroughCameraSamples.ShaderSample;

/// <summary>
/// Wires up missing references in VideoTestScene: FrostNoise texture,
/// cameraRig, rightControllerAnchor, and disables the passthrough layer.
/// </summary>
public class WireVideoTestScene
{
    private const string k_scene     = "Assets/VideoTestScene.unity";
    private const string k_sphereObj = "CameraSphereSphere";
    private const string k_frostTex  = "Assets/PassthroughCameraApiSamples/ShaderSample/Textures/FrostNoise.png";
    private const string k_eqMat     = "Assets/PassthroughCameraApiSamples/ShaderSample/Materials/VideoSphereEQMat.mat";

    [MenuItem("Tools/Wire Video Test Scene References")]
    public static void Execute()
    {
        var scene = EditorSceneManager.OpenScene(k_scene, OpenSceneMode.Single);

        var sphere = GameObject.Find(k_sphereObj);
        if (sphere == null) { Debug.LogError("[WireVideoTestScene] CameraSphereSphere not found"); return; }

        var mgr = sphere.GetComponent<VideoTestSceneManager>();
        if (mgr == null) { Debug.LogError("[WireVideoTestScene] VideoTestSceneManager not found"); return; }

        var so = new SerializedObject(mgr);

        // 1. FrostNoise texture
        var frostTex = AssetDatabase.LoadAssetAtPath<Texture2D>(k_frostTex);
        if (frostTex != null)
        {
            var p = so.FindProperty("m_frostTex");
            if (p != null) { p.objectReferenceValue = frostTex; Debug.Log("[WireVideoTestScene] FrostNoise assigned."); }
        }

        // 1b. VideoSphereEQ material template (critical — prevents shader stripping on Android)
        var eqMat = AssetDatabase.LoadAssetAtPath<Material>(k_eqMat);
        if (eqMat != null)
        {
            var p = so.FindProperty("m_videoSphereMatTemplate");
            if (p != null) { p.objectReferenceValue = eqMat; Debug.Log("[WireVideoTestScene] VideoSphereEQMat assigned."); }
        }
        else Debug.LogWarning($"[WireVideoTestScene] VideoSphereEQMat not found at {k_eqMat} — assign manually.");

        // 2. Camera rig — named "[BuildingBlock] Camera Rig" in this project
        string[] rigNames = { "[BuildingBlock] Camera Rig", "OVRCameraRig", "CameraRig" };
        GameObject rigGO = null;
        foreach (var name in rigNames)
        {
            rigGO = GameObject.Find(name);
            if (rigGO != null) break;
        }

        if (rigGO != null)
        {
            var rigProp = so.FindProperty("m_cameraRig");
            if (rigProp != null) { rigProp.objectReferenceValue = rigGO.transform; Debug.Log($"[WireVideoTestScene] cameraRig → {rigGO.name}"); }

            // Right controller anchor
            var anchor = rigGO.transform.Find("TrackingSpace/RightHandAnchor/RightControllerAnchor")
                      ?? rigGO.transform.Find("TrackingSpace/RightHandAnchor");
            if (anchor != null)
            {
                var ap = so.FindProperty("m_rightControllerAnchor");
                if (ap != null) { ap.objectReferenceValue = anchor; Debug.Log($"[WireVideoTestScene] rightControllerAnchor → {anchor.name}"); }
            }
        }
        else Debug.LogWarning("[WireVideoTestScene] Camera rig not found.");

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(sphere);

        // 3. Disable OVRPassthroughLayer — this is the video test scene, no real passthrough
        var ptGO = GameObject.Find("[BuildingBlock] Passthrough");
        if (ptGO != null)
        {
            ptGO.SetActive(false);
            EditorUtility.SetDirty(ptGO);
            Debug.Log("[WireVideoTestScene] Disabled [BuildingBlock] Passthrough.");
        }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[WireVideoTestScene] Done — VideoTestScene is ready.");
    }
}
