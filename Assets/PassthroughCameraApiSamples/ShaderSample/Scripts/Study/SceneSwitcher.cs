// Runtime switch between the two study environments, each with its 3 modes:
//   CameraSphereVignette — live passthrough (Blur*/Soft Dark/Hard Dark; *legacy cycle)
//   VideoTestScene       — pre-recorded video (ColorPop/Soft Dark/Hard Dark)
//
// Controls:
//   Hold Y (left controller) ~1 s  — the only face button not already used
//   Key V (adb: `adb shell input keyevent 50`) — experimenter side
//
// A full scene load (not an in-place toggle) is deliberate: the two managers own different
// pipelines (PCA cameras vs VideoPlayer, passthrough layer on vs off) and each scene's
// Start() already initialises its world correctly; switching in place would have to undo
// all of that by hand.
//
// Deliberately NOT gated on StudyLogger.SessionOpen: this switch must always work
// unconditionally during dev/manual testing. During a real recorded study session,
// switch mid-condition via the experimenter console (key E ends the session first) so
// the CSV closes cleanly — that's a procedural discipline, not a code-enforced block.

using UnityEngine;
using UnityEngine.SceneManagement;

namespace PassthroughCameraSamples.ShaderSample.Study
{
    public class SceneSwitcher : MonoBehaviour
    {
        [SerializeField] private string m_passthroughSceneName = "CameraSphereVignette";
        [SerializeField] private string m_videoSceneName = "VideoTestScene";
        [Tooltip("Hold duration for the Y button, to avoid accidental switches.")]
        [SerializeField, Range(0.3f, 3f)] private float m_holdSeconds = 1.0f;

        private float m_heldFor;
        private bool  m_switching;

        private void Update()
        {
            if (m_switching) return;

            m_heldFor = OVRInput.Get(OVRInput.RawButton.Y) ? m_heldFor + Time.deltaTime : 0f;
            bool triggered = m_heldFor >= m_holdSeconds || Input.GetKeyDown(KeyCode.V);
            if (!triggered) return;
            m_heldFor = 0f;

            string current = SceneManager.GetActiveScene().name;
            string target = current == m_videoSceneName ? m_passthroughSceneName : m_videoSceneName;
            Debug.Log($"[SceneSwitcher] {current} -> {target}");
            m_switching = true;
            SceneManager.LoadScene(target, LoadSceneMode.Single);
        }
    }
}
