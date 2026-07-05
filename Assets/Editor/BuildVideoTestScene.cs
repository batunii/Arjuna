using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Diagnostics;

public class BuildVideoTestScene
{
    private const string k_scene      = "Assets/VideoTestScene.unity";
    private const string k_outputDir  = "Builds/Android";
    private const string k_apkName    = "VideoTestScene.apk";
    private const string k_serial     = "2G0YC5ZG5F051R";

    [MenuItem("Tools/Build + Deploy VideoTestScene")]
    public static void Execute()
    {
        // 1. Point build settings at VideoTestScene only
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(k_scene, true)
        };

        // 2. Build
        Directory.CreateDirectory(k_outputDir);
        string apkPath = Path.GetFullPath(Path.Combine(k_outputDir, k_apkName));

        EditorUserBuildSettings.androidBuildSystem           = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

        var report  = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes           = new[] { k_scene },
            locationPathName = apkPath,
            target           = BuildTarget.Android,
            options          = BuildOptions.None,
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            UnityEngine.Debug.LogError(
                $"[BuildVideoTestScene] BUILD FAILED — errors={report.summary.totalErrors}");
            return;
        }

        long mb = (long)report.summary.totalSize / (1024 * 1024);
        UnityEngine.Debug.Log($"[BuildVideoTestScene] Build succeeded — {apkPath} ({mb} MB)");

        // 3. Install APK
        Adb($"-s {k_serial} install -r \"{apkPath}\"", "install");

        // 4. Launch
        Adb($"-s {k_serial} shell am start -n com.DefaultCompany.PassthroughCameraApiSamples/com.unity3d.player.UnityPlayerGameActivity", "launch");

        UnityEngine.Debug.Log("[BuildVideoTestScene] Deployed and launched.");
    }

    private static void Adb(string args, string label)
    {
        string[] candidates = {
            @"C:\Users\syson\AppData\Local\Android\Sdk\platform-tools\adb.exe",
            @"C:\Program Files\Unity\Hub\Editor\6000.0.61f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe",
        };
        string adb = null;
        foreach (var c in candidates) if (File.Exists(c)) { adb = c; break; }
        if (adb == null) { UnityEngine.Debug.LogWarning($"[BuildVideoTestScene] adb not found, skipping: {label}"); return; }

        var psi = new ProcessStartInfo(adb, args)
        {
            UseShellExecute = false, RedirectStandardOutput = true,
            RedirectStandardError = true, CreateNoWindow = true,
        };
        using var p = Process.Start(psi);
        string o = p.StandardOutput.ReadToEnd();
        string e = p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (!string.IsNullOrWhiteSpace(o)) UnityEngine.Debug.Log($"[adb {label}] {o.Trim()}");
        if (!string.IsNullOrWhiteSpace(e)) UnityEngine.Debug.LogWarning($"[adb {label}] {e.Trim()}");
    }

}
