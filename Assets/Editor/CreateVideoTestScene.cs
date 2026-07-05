using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using PassthroughCameraSamples.ShaderSample;

/// <summary>
/// Creates VideoTestScene.unity by duplicating the main CameraSphereVignette scene,
/// swapping the manager for VideoTestSceneManager, and removing the passthrough layer.
/// Run via: Tools > Create Video Test Scene
/// </summary>
public class CreateVideoTestScene
{
    private const string k_srcScene  = "Assets/CameraSphereVignette.unity";
    private const string k_dstScene  = "Assets/VideoTestScene.unity";
    private const string k_matPath   = "Assets/PassthroughCameraApiSamples/ShaderSample/Materials/VideoSphereEQMat.mat";
    private const string k_vignetteSphereObj = "CameraSphereSphere";

    [MenuItem("Tools/Create Video Test Scene")]
    public static void Execute()
    {
        // 1. Duplicate the source scene asset so we keep all OVR wiring
        if (!File.Exists(k_srcScene))
        {
            Debug.LogError($"[CreateVideoTestScene] Source scene not found: {k_srcScene}");
            return;
        }

        if (File.Exists(k_dstScene))
            AssetDatabase.DeleteAsset(k_dstScene);

        AssetDatabase.CopyAsset(k_srcScene, k_dstScene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 2. Open the duplicate
        var scene = EditorSceneManager.OpenScene(k_dstScene, OpenSceneMode.Single);

        // 3. Create / load the VideoSphereEQ material
        var eqMat = AssetDatabase.LoadAssetAtPath<Material>(k_matPath);
        if (eqMat == null)
        {
            var shader = Shader.Find("Meta/PCA/VideoSphereEQ");
            if (shader == null)
            {
                Debug.LogError("[CreateVideoTestScene] VideoSphereEQ shader not found. " +
                    "Make sure VideoSphereEQ.shader has been imported.");
                return;
            }
            eqMat = new Material(shader) { name = "VideoSphereEQMat" };
            Directory.CreateDirectory(Path.GetDirectoryName(k_matPath));
            AssetDatabase.CreateAsset(eqMat, k_matPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            eqMat = AssetDatabase.LoadAssetAtPath<Material>(k_matPath);
            Debug.Log($"[CreateVideoTestScene] Created {k_matPath}");
        }

        // 4. Find the vignette sphere
        var sphereGO = GameObject.Find(k_vignetteSphereObj);
        if (sphereGO == null)
        {
            Debug.LogError($"[CreateVideoTestScene] Could not find '{k_vignetteSphereObj}' in duplicated scene.");
            return;
        }

        // 5. Remove old manager, add VideoTestSceneManager
        var oldMgr = sphereGO.GetComponent<CameraSphereVignetteManager>();
        if (oldMgr != null)
        {
            // Copy dot material reference before removing
            var soOld = new SerializedObject(oldMgr);
            var dotMatProp = soOld.FindProperty("m_dotMaterialTemplate");
            Material dotMat = dotMatProp != null
                ? (Material)dotMatProp.objectReferenceValue
                : null;

            Object.DestroyImmediate(oldMgr, true);
            Debug.Log("[CreateVideoTestScene] Removed CameraSphereVignetteManager.");

            var newMgr = sphereGO.AddComponent<VideoTestSceneManager>();

            // Wire up the vignette sphere's MeshRenderer
            var soNew = new SerializedObject(newMgr);
            var rendProp = soNew.FindProperty("m_vignetteSphereRenderer");
            if (rendProp != null)
                rendProp.objectReferenceValue = sphereGO.GetComponent<MeshRenderer>();

            if (dotMat != null)
            {
                var dotProp = soNew.FindProperty("m_dotMaterialTemplate");
                if (dotProp != null) dotProp.objectReferenceValue = dotMat;
            }

            soNew.ApplyModifiedProperties();
            EditorUtility.SetDirty(sphereGO);
            Debug.Log("[CreateVideoTestScene] Added VideoTestSceneManager.");
        }
        else
        {
            Debug.LogWarning("[CreateVideoTestScene] CameraSphereVignetteManager not found on " +
                k_vignetteSphereObj + ". Skipping manager swap.");
        }

        // 6. Wire up OVRCameraRig references on VideoTestSceneManager
        WireCameraRigReferences(sphereGO);

        // 7. Remove or disable any camera access components (no physical camera in test scene)
        RemoveComponentsByType(sphereGO, "PassthroughCameraAccess");

        // 8. Save
        EditorSceneManager.SaveScene(scene, k_dstScene);
        AssetDatabase.Refresh();

        Debug.Log($"[CreateVideoTestScene] Saved {k_dstScene}. " +
            "Open it in the Editor, assign FrostNoise texture and any missing refs in the inspector, then build.");
    }

    private static void WireCameraRigReferences(GameObject sphere)
    {
        var mgr = sphere.GetComponent<VideoTestSceneManager>();
        if (mgr == null) return;

        var so = new SerializedObject(mgr);

        // Try to find OVRCameraRig in the scene
        var rigGO = GameObject.Find("OVRCameraRig");
        if (rigGO != null)
        {
            var rigProp = so.FindProperty("m_cameraRig");
            if (rigProp != null) rigProp.objectReferenceValue = rigGO.transform;

            // Try to find right controller anchor
            var rightAnchor = rigGO.transform.Find(
                "TrackingSpace/RightHandAnchor/RightControllerAnchor") ??
                rigGO.transform.Find("TrackingSpace/RightHandAnchor");
            var anchorProp = so.FindProperty("m_rightControllerAnchor");
            if (anchorProp != null && rightAnchor != null)
                anchorProp.objectReferenceValue = rightAnchor;
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(sphere);
    }

    private static void RemoveComponentsByType(GameObject go, string typeName)
    {
        var comps = go.GetComponents<Component>();
        foreach (var c in comps)
        {
            if (c == null) continue;
            if (c.GetType().Name == typeName)
            {
                Object.DestroyImmediate(c, true);
                Debug.Log($"[CreateVideoTestScene] Removed {typeName} from {go.name}");
            }
        }
    }
}
