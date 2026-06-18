using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FinaliseCameraSphereScene
{
    public static void Execute()
    {
        // Disable passthrough on OVRManager so the OS doesn't composite it underneath.
        var ovrManager = Object.FindObjectOfType<OVRManager>();
        if (ovrManager != null)
        {
            var so = new SerializedObject(ovrManager);
            // Try both known internal field names.
            var prop = so.FindProperty("_enablePassthrough")
                    ?? so.FindProperty("enablePassthrough");
            if (prop != null)
            {
                prop.boolValue = false;
                so.ApplyModifiedProperties();
                Debug.Log("[Finalise] OVRManager passthrough disabled via serialized property.");
            }
            else
            {
                // Fall back: directly set the public field via reflection.
                var field = typeof(OVRManager).GetField("enablePassthrough",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(ovrManager, false);
                    EditorUtility.SetDirty(ovrManager);
                    Debug.Log("[Finalise] OVRManager passthrough disabled via reflection.");
                }
                else
                {
                    Debug.LogWarning("[Finalise] Could not find enablePassthrough on OVRManager — disable it manually in the Inspector.");
                }
            }
        }

        // Set all cameras in the scene to clear to solid black so nothing shows behind the sphere.
        foreach (var cam in Object.FindObjectsOfType<Camera>())
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            EditorUtility.SetDirty(cam);
        }
        Debug.Log("[Finalise] Camera clear flags set to solid black.");

        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[Finalise] Scene saved.");
    }
}
