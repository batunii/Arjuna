// Wires the study tooling (Scripts/Study/) into both study scenes. Idempotent — safe to
// re-run; it finds an existing StudyRig and re-asserts references rather than duplicating.
//
//   Meta > Study > Wire VideoTestScene (Blocks B+C)
//   Meta > Study > Wire CameraSphereVignette (Block A)
//   Meta > Study > Wire Both Scenes
//
// What it does per scene:
//   - creates/finds a "StudyRig" GameObject
//   - adds StudyLogger + ConditionSequencer (+ ProbeScheduler for the video scene,
//     CPTPanel for the passthrough scene) and assigns all serialized references
//   - video scene: copies StreamingAssets/StudySchedules/probes_B?.json into
//     Assets/StudySchedules/ so they import as TextAssets, assigns them, and assigns
//     SelectionDotMat as the probe material template (Shader.Find is stripped on Android)
//   - saves the scene and prints a wiring summary
//
// See Scripts/Study/README.md for the session runbook.

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PassthroughCameraSamples.ShaderSample;
using PassthroughCameraSamples.ShaderSample.Study;

public static class SetupStudyRig
{
    private const string k_videoScenePath = "Assets/VideoTestScene.unity";
    private const string k_passthroughScenePath = "Assets/CameraSphereVignette.unity";
    private const string k_scheduleDstDir = "Assets/StudySchedules";

    [MenuItem("Meta/Study/Wire VideoTestScene (Blocks B+C)")]
    public static void WireVideoScene() => WireScene(k_videoScenePath, isVideoPlan: true);

    [MenuItem("Meta/Study/Wire CameraSphereVignette (Block A)")]
    public static void WirePassthroughScene() => WireScene(k_passthroughScenePath, isVideoPlan: false);

    [MenuItem("Meta/Study/Wire Both Scenes")]
    public static void WireBothScenes()
    {
        WireVideoScene();
        WirePassthroughScene();
    }

    private static void WireScene(string scenePath, bool isVideoPlan)
    {
        if (!File.Exists(scenePath))
        {
            Debug.LogError($"[SetupStudyRig] Scene not found: {scenePath}");
            return;
        }

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        MonoBehaviour manager;
        if (isVideoPlan)
            manager = Object.FindObjectOfType<VideoTestSceneManager>();
        else
            manager = Object.FindObjectOfType<CameraSphereVignetteManager>();

        if (manager == null)
        {
            Debug.LogError($"[SetupStudyRig] No {(isVideoPlan ? "VideoTestSceneManager" : "CameraSphereVignetteManager")} in {scenePath} — aborting.");
            return;
        }

        var rigGO = GameObject.Find("StudyRig");
        bool created = rigGO == null;
        if (created) rigGO = new GameObject("StudyRig");

        var logger = GetOrAdd<StudyLogger>(rigGO);
        var sequencer = GetOrAdd<ConditionSequencer>(rigGO);
        GetOrAdd<SceneSwitcher>(rigGO); // hold Y / key V: passthrough <-> video environment

        var so = new SerializedObject(sequencer);
        SetEnum(so, "m_plan", isVideoPlan ? (int)StudyPlan.VideoScene_BlocksBC : (int)StudyPlan.PassthroughScene_BlockA);
        SetRef(so, "m_vignetteManagerBehaviour", manager);
        SetRef(so, "m_logger", logger);

        string extras;
        if (isVideoPlan)
        {
            var probes = GetOrAdd<ProbeScheduler>(rigGO);
            var pso = new SerializedObject(probes);
            var b1 = ImportScheduleAsTextAsset("probes_B1.json");
            var b2 = ImportScheduleAsTextAsset("probes_B2.json");
            var dotMat = FindDotMaterial();
            SetRef(pso, "m_scheduleB1", b1);
            SetRef(pso, "m_scheduleB2", b2);
            SetRef(pso, "m_probeMaterialTemplate", dotMat);
            pso.ApplyModifiedPropertiesWithoutUndo();
            SetRef(so, "m_probeScheduler", probes);

            // Click-capture harness (left controller clicks lights/signs).
            var click = GetOrAdd<ClickProbeTest>(rigGO);
            var cso = new SerializedObject(click);
            SetRef(cso, "m_video", manager);
            SetRef(cso, "m_logger", logger);
            SetRef(cso, "m_reticleTemplate", dotMat);
            cso.ApplyModifiedPropertiesWithoutUndo();

            extras = $"ProbeScheduler(B1:{Name(b1)} B2:{Name(b2)} mat:{Name(dotMat)}), ClickProbeTest";
        }
        else
        {
            var cpt = GetOrAdd<CPTPanel>(rigGO);
            SetRef(so, "m_cptPanel", cpt);
            extras = "CPTPanel";
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"[SetupStudyRig] {scenePath}: StudyRig {(created ? "created" : "updated")} — " +
                  $"StudyLogger, ConditionSequencer(plan={(isVideoPlan ? "VideoScene_BlocksBC" : "PassthroughScene_BlockA")}, " +
                  $"manager={manager.GetType().Name}), SceneSwitcher, {extras}. Scene saved.");

        EnsureBuildScenes();
        WarnIfNewInputSystemOnly();
    }

    private static TextAsset ImportScheduleAsTextAsset(string fileName)
    {
        string src = Path.Combine(Application.streamingAssetsPath, "StudySchedules", fileName);
        if (!File.Exists(src))
        {
            Debug.LogError($"[SetupStudyRig] Missing {src} — run Tools/make_probe_schedule.py first.");
            return null;
        }
        if (!Directory.Exists(k_scheduleDstDir))
        {
            Directory.CreateDirectory(k_scheduleDstDir);
            AssetDatabase.ImportAsset(k_scheduleDstDir);
        }
        string dst = $"{k_scheduleDstDir}/{fileName}";
        File.Copy(src, dst, overwrite: true);
        AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<TextAsset>(dst);
    }

    private static Material FindDotMaterial()
    {
        foreach (string guid in AssetDatabase.FindAssets("SelectionDotMat t:Material"))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (mat != null) return mat;
        }
        Debug.LogWarning("[SetupStudyRig] SelectionDotMat not found — assign ProbeScheduler's material template manually (required for Android builds).");
        return null;
    }

    // Scene switching loads by name at runtime — both scenes must be in Build Settings.
    private static void EnsureBuildScenes()
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool changed = false;
        foreach (string path in new[] { k_videoScenePath, k_passthroughScenePath })
        {
            int idx = list.FindIndex(s => s.path == path);
            if (idx < 0) { list.Add(new EditorBuildSettingsScene(path, true)); changed = true; }
            else if (!list[idx].enabled) { list[idx] = new EditorBuildSettingsScene(path, true); changed = true; }
        }
        if (changed)
        {
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log($"[SetupStudyRig] Build Settings updated — boot scene: {(list.Count > 0 ? list[0].path : "none")}; " +
                      $"both study scenes enabled ({list.Count} total).");
        }
    }

    // Experimenter keys arrive via UnityEngine.Input — the old Input Manager must be active.
    private static void WarnIfNewInputSystemOnly()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (assets == null || assets.Length == 0) return;
        var pso = new SerializedObject(assets[0]);
        var prop = pso.FindProperty("activeInputHandler");
        if (prop != null && prop.intValue == 1) // 0 = old, 1 = new only, 2 = both
            Debug.LogWarning("[SetupStudyRig] Active Input Handling is 'Input System Package (New)' only — " +
                             "experimenter keyboard control (adb keyevents via UnityEngine.Input) will NOT work. " +
                             "Set Player Settings > Active Input Handling to 'Both' or 'Input Manager (Old)'.");
    }

    private static string Name(Object o) => o != null ? o.name : "MISSING";

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    private static void SetRef(SerializedObject so, string prop, Object value)
    {
        var p = so.FindProperty(prop);
        if (p == null) { Debug.LogError($"[SetupStudyRig] Property {prop} not found on {so.targetObject.GetType().Name}"); return; }
        p.objectReferenceValue = value;
    }

    private static void SetEnum(SerializedObject so, string prop, int value)
    {
        var p = so.FindProperty(prop);
        if (p == null) { Debug.LogError($"[SetupStudyRig] Property {prop} not found on {so.targetObject.GetType().Name}"); return; }
        p.enumValueIndex = value;
    }
}
