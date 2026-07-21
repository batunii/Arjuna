// Custom inspector for AuthoredTargetPresenter: the three run-configuration dropdowns are
// orthogonal, but not every combination is meaningful — this editor greys out what doesn't
// apply (no authored data in the passthrough environment; BASELINE tagging only exists for
// no-filter video passes; the filter-mode picker is video-only since passthrough is always
// Hard Dark) and prints a one-line summary of exactly what the current configuration will run.
// The same rules are enforced at the data level in AuthoredTargetPresenter.OnValidate().

using UnityEditor;
using UnityEngine;
using PassthroughCameraSamples.ShaderSample.Study;

[CustomEditor(typeof(AuthoredTargetPresenter))]
public class AuthoredTargetPresenterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var env      = serializedObject.FindProperty("m_environment");
        var set      = serializedObject.FindProperty("m_targetSet");
        var filter   = serializedObject.FindProperty("m_filter");
        var baseline = serializedObject.FindProperty("m_baselineScreening");
        var filterFx = serializedObject.FindProperty("m_filterMode");
        var pid      = serializedObject.FindProperty("m_participantId");

        bool passthrough = env.enumValueIndex == (int)AuthoredTargetPresenter.StudyEnvironment.MetaPassthrough;
        bool withFilter  = filter.enumValueIndex == (int)AuthoredTargetPresenter.FilterCondition.WithFilter;

        EditorGUILayout.PropertyField(pid);
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Run configuration", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(env);
        EditorGUILayout.PropertyField(filter);

        using (new EditorGUI.DisabledScope(passthrough))
        {
            EditorGUILayout.PropertyField(set, new GUIContent("Target Set (data)"));
            using (new EditorGUI.DisabledScope(withFilter))
                EditorGUILayout.PropertyField(baseline);
            using (new EditorGUI.DisabledScope(!withFilter))
                EditorGUILayout.PropertyField(filterFx, new GUIContent("Filter Mode (video)"));
        }

        if (passthrough)
            EditorGUILayout.HelpBox(
                "Meta Passthrough (Block A): there is no authored data here — the participant "
                + "paints the focus window instead. Filter = world-anchored Hard Dark; "
                + "No Filter = identical procedure with the effect suppressed.",
                MessageType.Info);

        EditorGUILayout.HelpBox(Summary(passthrough, withFilter, set, baseline, filterFx, pid), MessageType.None);
        EditorGUILayout.Space(6);

        DrawPropertiesExcluding(serializedObject,
            "m_Script", "m_participantId", "m_environment", "m_targetSet",
            "m_filter", "m_baselineScreening", "m_filterMode");
        serializedObject.ApplyModifiedProperties();
    }

    private static string Summary(bool passthrough, bool withFilter,
        SerializedProperty set, SerializedProperty baseline, SerializedProperty filterFx,
        SerializedProperty pid)
    {
        string who = pid.intValue < 0 ? "PILOT" : $"P{pid.intValue}";
        if (passthrough)
            return $"Will run:  {who} · Block A (passthrough) · "
                 + (withFilter ? "HARD DARK, world-anchored window" : "NO FILTER baseline");

        string setName = set.enumValueIndex switch
        {
            (int)AuthoredTargetPresenter.TargetSet.A => "set A (forced)",
            (int)AuthoredTargetPresenter.TargetSet.B => "set B (forced)",
            _ => $"set {(withFilter == (pid.intValue % 2 == 0) ? "A" : "B")} (auto from pid)",
        };
        string cond = withFilter ? $"FILTER ({filterFx.enumDisplayNames[filterFx.enumValueIndex]})"
                    : baseline.boolValue ? "BASELINE screening (no filter)"
                    : "NO FILTER";
        return $"Will run:  {who} · driving video · {setName} · {cond}";
    }
}
