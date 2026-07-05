using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using PassthroughCameraSamples.ShaderSample;

/// <summary>
/// Batchmode entry point: creates VideoSphereMat, enables video debug mode in the scene,
/// builds the Android APK, and deploys to a connected Quest 3.
///
/// Usage (batchmode):
///   Unity.exe -batchmode -quit -projectPath <path>
///             -executeMethod SetupVideoDebugBuild.Execute
///             -logFile build_log.txt
/// </summary>
public class SetupVideoDebugBuild
{
    private const string k_scenePath   = "Assets/CameraSphereVignette.unity";
    private const string k_matPath     = "Assets/PassthroughCameraApiSamples/ShaderSample/Materials/VideoSphereMat.mat";
    private const string k_outputDir   = "Builds/Android";
    private const string k_apkName     = "passthroughcamera.apk";
    private const string k_questSerial = "2G0YC5ZG5F051R";

    [MenuItem("Tools/Setup Video Debug + Build + Deploy")]
    public static void Execute()
    {
        // ── 1. Create VideoSphereMat ──────────────────────────────────────────
        var mat = AssetDatabase.LoadAssetAtPath<Material>(k_matPath);
        if (mat == null)
        {
            var shader = Shader.Find("Meta/PCA/VideoSphere");
            if (shader == null)
            {
                UnityEngine.Debug.LogError("[VideoDebugBuild] Meta/PCA/VideoSphere shader not found. " +
                    "Make sure VideoSphere.shader was imported (check Console for import errors).");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }
            mat = new Material(shader) { name = "VideoSphereMat" };
            Directory.CreateDirectory(Path.GetDirectoryName(k_matPath));
            AssetDatabase.CreateAsset(mat, k_matPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            mat = AssetDatabase.LoadAssetAtPath<Material>(k_matPath);
            UnityEngine.Debug.Log($"[VideoDebugBuild] Created {k_matPath}");
        }
        else
        {
            UnityEngine.Debug.Log($"[VideoDebugBuild] Found existing {k_matPath}");
        }

        // ── 2. Open scene + configure manager ────────────────────────────────
        var scene = EditorSceneManager.OpenScene(k_scenePath, OpenSceneMode.Single);

        var sphere = GameObject.Find("CameraSphereSphere");
        if (sphere == null)
        {
            UnityEngine.Debug.LogError("[VideoDebugBuild] CameraSphereSphere not found in scene.");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        var mgr = sphere.GetComponent<CameraSphereVignetteManager>();
        if (mgr == null)
        {
            UnityEngine.Debug.LogError("[VideoDebugBuild] CameraSphereVignetteManager not found.");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        var so = new SerializedObject(mgr);

        var videoDebugProp = so.FindProperty("m_videoDebugMode");
        var videoMatProp   = so.FindProperty("m_videoSphereMaterialTemplate");

        if (videoDebugProp == null || videoMatProp == null)
        {
            UnityEngine.Debug.LogError("[VideoDebugBuild] Could not find m_videoDebugMode or " +
                "m_videoSphereMaterialTemplate on manager. Did the script compile correctly?");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        videoDebugProp.boolValue              = true;
        videoMatProp.objectReferenceValue     = mat;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(sphere);
        EditorSceneManager.SaveScene(scene);
        UnityEngine.Debug.Log("[VideoDebugBuild] Scene saved — videoDebugMode=true, VideoSphereMat assigned.");

        // ── 3. Ensure build settings ──────────────────────────────────────────
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(k_scenePath, true)
        };

        // ── 4. Build APK ──────────────────────────────────────────────────────
        Directory.CreateDirectory(k_outputDir);
        string apkFullPath = Path.GetFullPath(Path.Combine(k_outputDir, k_apkName));

        EditorUserBuildSettings.androidBuildSystem          = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

        var buildOptions = new BuildPlayerOptions
        {
            scenes           = new[] { k_scenePath },
            locationPathName = apkFullPath,
            target           = BuildTarget.Android,
            options          = BuildOptions.None,
        };

        UnityEngine.Debug.Log($"[VideoDebugBuild] Starting build → {apkFullPath}");
        var report  = BuildPipeline.BuildPlayer(buildOptions);
        var summary = report.summary;

        if (summary.result != BuildResult.Succeeded)
        {
            UnityEngine.Debug.LogError($"[VideoDebugBuild] BUILD FAILED — result={summary.result}  " +
                $"errors={summary.totalErrors}  warnings={summary.totalWarnings}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        long sizeMB = (long)summary.totalSize / (1024 * 1024);
        UnityEngine.Debug.Log($"[VideoDebugBuild] BUILD SUCCEEDED — {apkFullPath}  ({sizeMB} MB)");

        // ── 5. Deploy to Quest 3 ─────────────────────────────────────────────
        Deploy(apkFullPath);
    }

    private static void Deploy(string apkPath)
    {
        // Try common adb locations
        string[] adbCandidates = {
            @"C:\Users\syson\AppData\Local\Android\Sdk\platform-tools\adb.exe",
            @"C:\Program Files\Unity\Hub\Editor\6000.0.61f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe",
        };
        string adb = null;
        foreach (var c in adbCandidates)
            if (File.Exists(c)) { adb = c; break; }

        if (adb == null)
        {
            UnityEngine.Debug.LogWarning("[VideoDebugBuild] adb.exe not found — skipping deploy. " +
                $"Install manually:\n  adb -s {k_questSerial} install -r \"{apkPath}\"");
            return;
        }

        Run(adb, $"-s {k_questSerial} install -r \"{apkPath}\"", "install");

        // Check for OBB alongside the APK (Unity split-binary builds produce one)
        string obbDir  = Path.ChangeExtension(apkPath, null); // strips .apk
        if (Directory.Exists(obbDir))
        {
            foreach (var obb in Directory.GetFiles(obbDir, "*.obb"))
            {
                string deviceObbDir = "/sdcard/Android/obb/com.DefaultCompany.PassthroughCameraApiSamples";
                Run(adb, $"-s {k_questSerial} shell mkdir -p \"{deviceObbDir}\"", "mkdir obb");
                Run(adb, $"-s {k_questSerial} push \"{obb}\" \"{deviceObbDir}/{Path.GetFileName(obb)}\"", "push obb");
            }
        }

        UnityEngine.Debug.Log("[VideoDebugBuild] Deploy complete. Launch on headset.");
    }

    private static void Run(string exe, string args, string label)
    {
        UnityEngine.Debug.Log($"[VideoDebugBuild] {label}: {exe} {args}");
        var psi = new ProcessStartInfo(exe, args)
        {
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true,
        };
        using var p = Process.Start(psi);
        string stdout = p.StandardOutput.ReadToEnd();
        string stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (!string.IsNullOrWhiteSpace(stdout)) UnityEngine.Debug.Log($"[adb {label}] {stdout.Trim()}");
        if (!string.IsNullOrWhiteSpace(stderr)) UnityEngine.Debug.LogWarning($"[adb {label}] {stderr.Trim()}");
    }
}
