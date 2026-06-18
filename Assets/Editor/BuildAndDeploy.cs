using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildAndDeploy
{
    private const string OutputDir = "Builds/Android";
    private const string ApkName  = "passthroughcamera.apk";

    public static string ApkPath => Path.GetFullPath(Path.Combine(OutputDir, ApkName));

    public static void Execute()
    {
        // Fix build settings to contain only our scene.
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/CameraSphereVignette.unity", true),
        };
        Debug.Log("[Build] Build settings updated — one scene: CameraSphereVignette. Now use File → Build And Run.");
    }

    public static void RunBuild()
    {
        Directory.CreateDirectory(OutputDir);

        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

        var options = new BuildPlayerOptions
        {
            scenes           = new[] { "Assets/CameraSphereVignette.unity" },
            locationPathName = Path.Combine(OutputDir, ApkName),
            target           = BuildTarget.Android,
            options          = BuildOptions.None,
        };

        var report  = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
            Debug.Log($"[Build] SUCCESS — {ApkPath}  ({summary.totalSize / 1024 / 1024} MB)");
        else
            Debug.LogError($"[Build] FAILED — {summary.result}  errors={summary.totalErrors}");
    }
}
