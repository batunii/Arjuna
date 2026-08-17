// One-click study data sync: Meta > Study > Pull Study Data From Headset.
//
// Pulls every authored_results_*.csv and authored_points_*.csv plus session_ledger.csv from the
// device's persistentDataPath into Dissertation/authored/raw/, skipping files already present at
// the same byte size, then runs Tools/analysis/authored_report.py and prints the aggregate to
// the Console. Nothing is deleted from the device here.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class StudyDataPuller
{
    private const string k_remoteDir = "/sdcard/Android/data/com.samples.passthroughcamera/files";
    private static readonly string[] k_patterns = { "authored_results_", "authored_points_" };
    private const string k_ledger = "session_ledger.csv";

    [MenuItem("Meta/Study/Pull Study Data From Headset")]
    public static void Pull()
    {
        string adb = FindAdb();
        if (adb == null)
        {
            EditorUtility.DisplayDialog("Pull Study Data",
                "adb not found (Android SDK platform-tools, ANDROID_SDK_ROOT, or PATH).", "OK");
            return;
        }

        string rawDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Dissertation/authored/raw"));
        Directory.CreateDirectory(rawDir);

        if (!Run(adb, $"shell ls -l {k_remoteDir}", out string ls))
        {
            EditorUtility.DisplayDialog("Pull Study Data",
                "adb shell ls failed — is the headset connected and authorised?", "OK");
            return;
        }

        int pulled = 0, skipped = 0, failed = 0;
        var pulledNames = new List<string>();
        foreach (string line in ls.Split('\n'))
        {
            // toybox ls -l: perms links owner group SIZE date time NAME
            var tok = line.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (tok.Length < 8 || !long.TryParse(tok[4], out long size)) continue;
            string name = tok[tok.Length - 1];
            bool wanted = name == k_ledger;
            foreach (var p in k_patterns) wanted |= name.StartsWith(p) && name.EndsWith(".csv");
            if (!wanted) continue;

            string local = Path.Combine(rawDir, name);
            if (name != k_ledger && File.Exists(local) && new FileInfo(local).Length == size)
                { skipped++; continue; }   // ledger always re-pulled: it's append-only and grows

            if (Run(adb, $"pull \"{k_remoteDir}/{name}\" \"{local}\"", out _))
                { pulled++; pulledNames.Add(name); }
            else failed++;
        }

        Debug.Log($"[StudyDataPuller] pulled {pulled}, up-to-date {skipped}, failed {failed} -> {rawDir}"
                + (pulledNames.Count > 0 ? "\n  " + string.Join("\n  ", pulledNames) : ""));

        RunReport(rawDir);

        EditorUtility.DisplayDialog("Pull Study Data",
            $"Pulled {pulled} new/updated file(s), {skipped} already up to date"
            + (failed > 0 ? $", {failed} FAILED" : "")
            + ".\n\nAggregate report is in the Console.", "OK");
    }

    // Aggregate immediately so "pull" always ends with current numbers in the Console.
    private static void RunReport(string rawDir)
    {
        string repo = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string script = Path.Combine(repo, "Tools/analysis/authored_report.py");
        if (!File.Exists(script)) return;
        foreach (string py in new[] { "python", "py" })
        {
            if (!Run(py, $"\"{script}\" \"{rawDir}\"", out string report, repo)) continue;
            Debug.Log($"[StudyDataPuller] authored_report:\n{report}");
            return;
        }
        Debug.LogWarning("[StudyDataPuller] python not found — run Tools/analysis/authored_report.py manually.");
    }

    private static string FindAdb()
    {
        var candidates = new List<string>();
        foreach (var env in new[] { "ANDROID_SDK_ROOT", "ANDROID_HOME" })
        {
            string root = Environment.GetEnvironmentVariable(env);
            if (!string.IsNullOrEmpty(root))
                candidates.Add(Path.Combine(root, "platform-tools", "adb.exe"));
        }
        // Unity's own Android SDK ships adb; sniff it via the editor install location.
        string unitySdk = Path.Combine(EditorApplication.applicationContentsPath,
            "PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe");
        candidates.Add(unitySdk);
        foreach (var c in candidates)
            if (File.Exists(c)) return c;
        return Run("adb", "version", out _) ? "adb" : null;   // PATH fallback
    }

    private static bool Run(string exe, string args, out string stdout, string workDir = null)
    {
        try
        {
            using var p = new Process();
            p.StartInfo = new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workDir ?? "",
            };
            p.Start();
            stdout = p.StandardOutput.ReadToEnd();
            string err = p.StandardError.ReadToEnd();
            if (!p.WaitForExit(60000)) { p.Kill(); return false; }
            if (p.ExitCode != 0)
            {
                Debug.LogWarning($"[StudyDataPuller] {exe} {args} exited {p.ExitCode}: {err}");
                return false;
            }
            return true;
        }
        catch (Exception e)
        {
            stdout = "";
            if (exe != "adb" && exe != "python" && exe != "py")   // probes fail quietly
                Debug.LogWarning($"[StudyDataPuller] failed to run {exe}: {e.Message}");
            return false;
        }
    }
}
