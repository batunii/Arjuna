using UnityEngine;
using UnityEditor;

public class CreateVideoSphereMat
{
    [MenuItem("Tools/Create Video Sphere Material")]
    static void Create()
    {
        const string shaderName = "Meta/PCA/VideoSphere";
        const string matPath    = "Assets/PassthroughCameraApiSamples/ShaderSample/Materials/VideoSphereMat.mat";

        var shader = Shader.Find(shaderName);
        if (shader == null)
        {
            Debug.LogError($"[VideoSphereMat] Shader '{shaderName}' not found. Make sure VideoSphere.shader is in the project.");
            return;
        }

        var mat = new Material(shader);
        mat.name = "VideoSphereMat";

        AssetDatabase.CreateAsset(mat, matPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[VideoSphereMat] Created {matPath}");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Material>(matPath);
    }
}
