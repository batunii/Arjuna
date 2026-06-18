using UnityEngine;
using UnityEditor;
using PassthroughCameraSamples.ShaderSample;

public class WireCameraSphereScene
{
    public static void Execute()
    {
        var sphere = GameObject.Find("CameraSphereSphere");
        if (sphere == null) { Debug.LogError("[Wire] CameraSphereSphere not found"); return; }

        var mgr = sphere.GetComponent<CameraSphereVignetteManager>();
        if (mgr == null) { Debug.LogError("[Wire] CameraSphereVignetteManager not found"); return; }

        var so = new SerializedObject(mgr);

        var markersProp = so.FindProperty("m_cornerMarkers");
        markersProp.arraySize = 2;

        var m0 = GameObject.Find("CS_CornerMarker0");
        var m1 = GameObject.Find("CS_CornerMarker1");
        markersProp.GetArrayElementAtIndex(0).objectReferenceValue = m0 != null ? m0.transform : null;
        markersProp.GetArrayElementAtIndex(1).objectReferenceValue = m1 != null ? m1.transform : null;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(sphere);

        // Start corner markers inactive — manager activates them when corners are placed.
        if (m0 != null) m0.SetActive(false);
        if (m1 != null) m1.SetActive(false);

        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Wire] CameraSphere scene wired and saved.");
    }
}
