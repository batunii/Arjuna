using UnityEngine;
using UnityEditor;
using System.IO;

public class CreateFrostNoiseTexture
{
    [MenuItem("Tools/Create Frost Noise Texture")]
    static void Create()
    {
        const int size  = 256;
        const float scale = 4f; // lower = larger blobs, higher = finer grain

        var tex = new Texture2D(size, size, TextureFormat.R8, false);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float n = Mathf.PerlinNoise(x * scale / size, y * scale / size);
                tex.SetPixel(x, y, new Color(n, n, n, 1f));
            }
        tex.Apply();

        const string path = "Assets/PassthroughCameraApiSamples/ShaderSample/Textures/FrostNoise.png";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.Refresh();

        // Set import settings: Repeat wrap, no compression artifacts on a noise texture
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null)
        {
            importer.wrapMode        = TextureWrapMode.Repeat;
            importer.filterMode      = FilterMode.Bilinear;
            importer.textureType     = TextureImporterType.Default;
            importer.alphaSource     = TextureImporterAlphaSource.None;
            importer.mipmapEnabled   = true;
            importer.SaveAndReimport();
        }

        Debug.Log($"[FrostNoise] Created {path}");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
