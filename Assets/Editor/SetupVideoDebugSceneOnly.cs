using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using PassthroughCameraSamples.ShaderSample;

/// <summary>
/// Fast scene-setup only (no build). Creates VideoSphereMat and enables video debug mode.
/// Run via Coplay execute_script or Tools menu. Build separately.
/// </summary>
public class SetupVideoDebugSceneOnly
{
    private const string k_scenePath = "Assets/CameraSphereVignette.unity";
    private const string k_matPath   = "Assets/PassthroughCameraApiSamples/ShaderSample/Materials/VideoSphereMat.mat";

    [MenuItem("Tools/Setup Video Debug (Scene Only)")]
    public static void Execute()
    {
        // 1. Create or load VideoSphereMat
        var mat = AssetDatabase.LoadAssetAtPath<Material>(k_matPath);
        if (mat == null)
        {
            var shader = Shader.Find("Meta/PCA/VideoSphere");
            if (shader == null)
            {
                Debug.LogError("[SetupVideoDebugSceneOnly] VideoSphere shader not found.");
                return;
            }
            mat = new Material(shader) { name = "VideoSphereMat" };
            Directory.CreateDirectory(Path.GetDirectoryName(k_matPath));
            AssetDatabase.CreateAsset(mat, k_matPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            mat = AssetDatabase.LoadAssetAtPath<Material>(k_matPath);
            Debug.Log($"[SetupVideoDebugSceneOnly] Created {k_matPath}");
        }
        else
        {
            Debug.Log($"[SetupVideoDebugSceneOnly] Found {k_matPath}");
        }

        // 2. Open scene and configure manager
        var scene = EditorSceneManager.OpenScene(k_scenePath, OpenSceneMode.Single);

        var sphere = GameObject.Find("CameraSphereSphere");
        if (sphere == null) { Debug.LogError("[SetupVideoDebugSceneOnly] CameraSphereSphere not found"); return; }

        var mgr = sphere.GetComponent<CameraSphereVignetteManager>();
        if (mgr == null) { Debug.LogError("[SetupVideoDebugSceneOnly] Manager not found"); return; }

        var so = new SerializedObject(mgr);
        so.FindProperty("m_videoDebugMode").boolValue          = true;
        so.FindProperty("m_videoSphereMaterialTemplate").objectReferenceValue = mat;
        var applied = so.ApplyModifiedProperties();
        if (!applied)
        {
            Debug.Log("Effects not applied");
        }
        EditorUtility.SetDirty(sphere);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SetupVideoDebugSceneOnly] Done — videoDebugMode=true, VideoSphereMat assigned. " +
                  "Build via Tools > Setup Video Debug + Build + Deploy or File > Build And Run.");
    }
}
