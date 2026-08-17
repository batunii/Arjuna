// Builds the study APK with every enabled Build Settings scene, in that order, so the first
// enabled scene is the boot scene. BuildAndDeploy.RunBuild hardcodes a single scene instead.
// Menu: Meta > Study > Build Study APK. Output: Builds/Android/study.apk.

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class StudyBuild
{
    private const string OutputDir = "Builds/Android";
    private const string ApkName = "study.apk";

    public static string ApkPath => Path.GetFullPath(Path.Combine(OutputDir, ApkName));

    [MenuItem("Meta/Study/Build Study APK (both scenes)")]
    public static void BuildStudyApk()
    {
        Directory.CreateDirectory(OutputDir);
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

        string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0)
        {
            Debug.LogError("[StudyBuild] No enabled scenes in Build Settings — run Meta > Study > Wire Both Scenes first.");
            return;
        }
        Debug.Log($"[StudyBuild] Building {scenes.Length} scenes ({string.Join(", ", scenes)}) -> {ApkPath}");

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.Combine(OutputDir, ApkName),
            target = BuildTarget.Android,
            options = BuildOptions.None,
        });

        var summary = report.summary;
        if (summary.result == BuildResult.Succeeded)
            Debug.Log($"[StudyBuild] SUCCESS — {ApkPath} ({summary.totalSize / 1024 / 1024} MB, {summary.totalTime.TotalMinutes:F1} min)");
        else
            Debug.LogError($"[StudyBuild] FAILED — {summary.result}, errors={summary.totalErrors}");
    }
}
