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

        // ---- hold-progress ring (visible only while Y is held, in the scene being left) ----

        private const int   k_ringSegments = 48;
        private const float k_ringRadius   = 0.045f;
        private const float k_ringDistance = 0.5f;

        private static readonly Vector3[] s_circlePoints = BuildCirclePoints();

        private GameObject  m_ringRoot;
        private LineRenderer m_ringFg;

        private static Vector3[] BuildCirclePoints()
        {
            // Closed loop starting at the top (12 o'clock), sweeping clockwise, so a partial
            // slice of this same array reads as a clock-style fill.
            var pts = new Vector3[k_ringSegments + 1];
            for (int i = 0; i <= k_ringSegments; i++)
            {
                float t   = (float)i / k_ringSegments;
                float rad = Mathf.PI / 2f - t * Mathf.PI * 2f;
                pts[i] = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * k_ringRadius;
            }
            return pts;
        }

        private void Update()
        {
            if (m_switching) return;

            m_heldFor = OVRInput.Get(OVRInput.RawButton.Y) ? m_heldFor + Time.deltaTime : 0f;

            if (m_heldFor > 0f)
            {
                EnsureRing();
                UpdateRing();
            }
            else
            {
                DestroyRing();
            }

            bool triggered = m_heldFor >= m_holdSeconds || Input.GetKeyDown(KeyCode.V);
            if (!triggered) return;
            m_heldFor = 0f;
            DestroyRing();

            string current = SceneManager.GetActiveScene().name;
            string target = current == m_videoSceneName ? m_passthroughSceneName : m_videoSceneName;
            Debug.Log($"[SceneSwitcher] {current} -> {target}");
            m_switching = true;
            SceneManager.LoadScene(target, LoadSceneMode.Single);
        }

        private void OnDisable() => DestroyRing();

        private void EnsureRing()
        {
            if (m_ringRoot != null) return;
            m_ringRoot = new GameObject("SwitchHoldRing");
            var bg = CreateRingRenderer(m_ringRoot, "Background", new Color(1f, 1f, 1f, 0.25f));
            bg.positionCount = s_circlePoints.Length;
            bg.SetPositions(s_circlePoints);
            m_ringFg = CreateRingRenderer(m_ringRoot, "Fill", new Color(0.30f, 0.85f, 1f, 0.95f));
        }

        private static LineRenderer CreateRingRenderer(GameObject parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace    = false;
            lr.loop             = false;
            lr.widthMultiplier  = 0.004f;
            lr.numCapVertices   = 2;
            // Queue 4100 matches the existing mode-toast trick (VideoTestSceneManager.ShowModeToast)
            // so the ring draws above the vignette sphere (queue 3000) instead of being occluded.
            lr.material = new Material(Shader.Find("Sprites/Default")) { renderQueue = 4100 };
            lr.startColor = lr.endColor = color;
            return lr;
        }

        private void UpdateRing()
        {
            Transform head = Camera.main != null ? Camera.main.transform : null;
            if (head == null) return;
            m_ringRoot.transform.position = head.position + head.forward * k_ringDistance;
            m_ringRoot.transform.rotation = Quaternion.LookRotation(-head.forward, head.up);

            float progress = Mathf.Clamp01(m_heldFor / m_holdSeconds);
            int   count    = Mathf.Max(2, Mathf.RoundToInt(k_ringSegments * progress) + 1);
            m_ringFg.positionCount = count;
            for (int i = 0; i < count; i++) m_ringFg.SetPosition(i, s_circlePoints[i]);
        }

        private void DestroyRing()
        {
            if (m_ringRoot == null) return;
            Destroy(m_ringRoot);
            m_ringRoot = null;
            m_ringFg   = null;
        }
    }
}
