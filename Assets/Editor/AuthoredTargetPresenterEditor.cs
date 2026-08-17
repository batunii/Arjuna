// Custom inspector for AuthoredTargetPresenter. The run-configuration dropdowns are orthogonal
// but not every combination is meaningful, so this greys out what does not apply and prints a
// one-line summary of what the current configuration will run. Disabled fields:
//   - the whole manual configuration in AutoSession, where the ledger decides
//   - target set in the passthrough environment, which has no authored data
//   - BASELINE tagging outside no-filter video passes
//   - the filter-mode picker in passthrough, which is always Hard Dark
// The same rules are enforced in AuthoredTargetPresenter.OnValidate().

using UnityEditor;
using UnityEngine;
using PassthroughCameraSamples.ShaderSample.Study;

[CustomEditor(typeof(AuthoredTargetPresenter))]
public class AuthoredTargetPresenterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var session  = serializedObject.FindProperty("m_sessionMode");
        var env      = serializedObject.FindProperty("m_environment");
        var set      = serializedObject.FindProperty("m_targetSet");
        var filter   = serializedObject.FindProperty("m_filter");
        var baseline = serializedObject.FindProperty("m_baselineScreening");
        var filterFx = serializedObject.FindProperty("m_filterMode");
        var pid      = serializedObject.FindProperty("m_participantId");

        bool auto        = session.enumValueIndex == (int)AuthoredTargetPresenter.SessionMode.AutoSession;
        bool passthrough = env.enumValueIndex == (int)AuthoredTargetPresenter.StudyEnvironment.MetaPassthrough;
        bool withFilter  = filter.enumValueIndex == (int)AuthoredTargetPresenter.FilterCondition.WithFilter;

        EditorGUILayout.PropertyField(pid);
        EditorGUILayout.PropertyField(session);
        EditorGUILayout.Space(6);

        if (auto)
        {
            EditorGUILayout.HelpBox(
                "Auto session: one participant = 4 X-gated blocks (video filter/no-filter on "
                + "opposite sets + Block A no-filter/filter, ABBA order). Assignment is "
                + "greedy-balanced across participants from the device's session_ledger.csv; "
                + "pid -1 auto-assigns the next id (relaunch per participant, no rebuild). "
                + "The manual dropdowns below are ignored.",
                MessageType.Info);
            EditorGUILayout.PropertyField(filterFx, new GUIContent("Filter Mode (video blocks)"));
        }

        using (new EditorGUI.DisabledScope(auto))
        {
            EditorGUILayout.LabelField("Manual run configuration (the override)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(env);
            EditorGUILayout.PropertyField(filter);
            using (new EditorGUI.DisabledScope(passthrough))
            {
                EditorGUILayout.PropertyField(set, new GUIContent("Target Set (data)"));
                using (new EditorGUI.DisabledScope(withFilter))
                    EditorGUILayout.PropertyField(baseline);
                if (!auto)
                    using (new EditorGUI.DisabledScope(!withFilter))
                        EditorGUILayout.PropertyField(filterFx, new GUIContent("Filter Mode (video)"));
            }
            if (!auto && passthrough)
                EditorGUILayout.HelpBox(
                    "Meta Passthrough (Block A): there is no authored data here — the participant "
                    + "paints the focus window instead. Filter = world-anchored Hard Dark; "
                    + "No Filter = identical procedure with the effect suppressed.",
                    MessageType.Info);
        }

        EditorGUILayout.HelpBox(Summary(auto, passthrough, withFilter, set, baseline, filterFx, pid), MessageType.None);
        EditorGUILayout.Space(6);

        DrawPropertiesExcluding(serializedObject,
            "m_Script", "m_participantId", "m_sessionMode", "m_environment", "m_targetSet",
            "m_filter", "m_baselineScreening", "m_filterMode");
        serializedObject.ApplyModifiedProperties();
    }

    private static string Summary(bool auto, bool passthrough, bool withFilter,
        SerializedProperty set, SerializedProperty baseline, SerializedProperty filterFx,
        SerializedProperty pid)
    {
        string who = pid.intValue < 0 ? (auto ? "next pid from ledger" : "PILOT")
                   : pid.intValue >= 900 ? $"PILOT{pid.intValue} (excluded from balancing)"
                   : $"P{pid.intValue}";
        if (auto)
            return $"Will run:  {who} · full 4-block session (assignment from device ledger)";
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
