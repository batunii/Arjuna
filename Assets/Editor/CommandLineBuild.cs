using UnityEditor;
using UnityEditor.Build.Reporting;
using System.Linq;

public static class CommandLineBuild
{
    /// <summary>
    /// Called from the command line via -executeMethod CommandLineBuild.BuildAndroid
    /// Produces an APK at Builds/build.apk using the scenes already configured
    /// in EditorBuildSettings.
    /// </summary>
    public static void BuildAndroid()
    {
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Builds/build.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            UnityEngine.Debug.LogError($"Build failed: {report.summary.result}");
            EditorApplication.Exit(1);
        }
        else
        {
            UnityEngine.Debug.Log($"Build succeeded: {report.summary.totalSize} bytes");
            EditorApplication.Exit(0);
        }
    }
}
