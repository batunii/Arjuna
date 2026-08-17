// Quick-preview harness for stepping through the filter modes outside a study session.
// Standalone from ConditionSequencer and StudyLogger.
//
// Self-bootstraps at app start (RuntimeInitializeOnLoadMethod), so there is no scene wiring and
// no menu step. AuthoredTargetPresenter destroys it, so only one of the two ever runs.
//
// Default sequence (edit m_modes below to change or reorder):
//   1. Baseline   + Video        (probes, filter off, detect-only screening pass)
//   2. SignPop    + Video
//   3. No Filter  + Video
//   4. Hard Dark  + Passthrough
//   5. No Filter  + Passthrough
//
// Per-mode controls:
//   X   toggle. Video modes play/pause the clip; passthrough modes start the vignette forming
//       or reset it. The first press also dismisses the instructions.
//   Y   advance to the next mode, leaving the current one stopped. Moving between a video mode
//       and a passthrough mode does a full scene load, since the two managers own different
//       pipelines. Y rather than A, because A is the scene managers' own mode cycle.
//
// Nothing auto-advances on a timer.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PassthroughCameraSamples.ShaderSample.Study
{
    public enum TestStage
    {
        VideoScene,
        PassthroughScene,
    }

    [System.Serializable]
    public class TestModeEntry
    {
        public string label = "New Mode";
        public TestStage stage = TestStage.VideoScene;
        public VignetteMode vignetteMode = VignetteMode.ColorPop;

        [Tooltip("True = no filter — StudySetActive(true) is skipped, X only affects video play/pause (video modes) or does nothing (passthrough modes).")]
        public bool noFilter;
    }

    public class TestModeSequencer : MonoBehaviour
    {
        public static TestModeSequencer Instance { get; private set; }

        [Header("Mode Order — reorder/edit freely; plays top to bottom")]
        [SerializeField]
        private List<TestModeEntry> m_modes = new()
        {
            // Baseline: probes with the filter off, run first as a detect-only pass to measure
            // each target's baseline detectability. blob_probe.py uses it to screen out
            // unreachable targets. Shares the seeded target set with the later modes.
            new TestModeEntry { label = "Baseline (detect only, no filter)", stage = TestStage.VideoScene,
                vignetteMode = VignetteMode.SignPop, noFilter = true },
            new TestModeEntry { label = "SignPop + Video", stage = TestStage.VideoScene,
                vignetteMode = VignetteMode.SignPop, noFilter = false },
            new TestModeEntry { label = "No Filter + Video", stage = TestStage.VideoScene,
                vignetteMode = VignetteMode.SignPop, noFilter = true },
            new TestModeEntry { label = "Hard Dark + Passthrough", stage = TestStage.PassthroughScene,
                vignetteMode = VignetteMode.HardDark, noFilter = false },
            new TestModeEntry { label = "No Filter + Passthrough", stage = TestStage.PassthroughScene,
                vignetteMode = VignetteMode.HardDark, noFilter = true },
        };

        [Header("Scenes")]
        [SerializeField] private string m_videoSceneName = "VideoTestScene";
        [SerializeField] private string m_passthroughSceneName = "CameraSphereVignette";

        [Header("Input — Quest controller")]
        [SerializeField] private OVRInput.RawButton m_toggleButton = OVRInput.RawButton.X;
        // Y, not A: A is the vignette mode-cycle button in both scene managers.
        [SerializeField] private OVRInput.RawButton m_advanceButton = OVRInput.RawButton.Y;

        [Header("Instruction UI")]
        [SerializeField, Range(0.5f, 3f)] private float m_uiDistance = 1.5f;
        [SerializeField, Range(0f, 30f)]  private float m_uiDropDeg = 12f;

        [Header("Video-mode focus window (locked, identical across modes 1 & 2)")]
        [Tooltip("Lock a fixed 'windscreen' focus window around video-forward on entry to every video mode. Without this nothing is ever painted in the test harness, so SignPop falls back to gaze auto-follow — the person gate/grading then depends on where the participant happens to look, an uncontrolled variable in the mode-1-vs-2 comparison (and the supervisor's point 4: window geometry should be constant during evaluation). Same window in the no-filter mode too (invisible there), so procedure and geometry are identical in both arms.")]
        [SerializeField] private bool m_lockVideoWindow = true;
        [SerializeField, Range(5f, 80f)] private float m_videoWindowHalfWidthDeg = 25f;
        [SerializeField, Range(5f, 60f)] private float m_videoWindowHalfHeightDeg = 15f;

        private int m_index = -1;
        private bool m_active;
        private IStudyVignetteControl m_control;
        private BlobTargetController m_blobs;

        private GameObject m_uiRoot;
        private Material m_uiMat;
        private Text m_uiText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return; // a manually-placed instance already claimed it
            var go = new GameObject(nameof(TestModeSequencer));
            go.AddComponent<TestModeSequencer>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            m_blobs = GetComponent<BlobTargetController>();
            if (m_blobs == null) m_blobs = gameObject.AddComponent<BlobTargetController>();
        }

        private void Start() => AdvanceToNextMode();

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this) Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => EnterCurrentMode();

        private void Update()
        {
            if (OVRInput.GetDown(m_toggleButton)) ToggleActive();
            if (OVRInput.GetDown(m_advanceButton)) AdvanceToNextMode();
        }

        private void LateUpdate()
        {
            if (m_uiRoot != null && m_uiRoot.activeSelf) PositionUI();
        }

        // ---- sequencing ----

        private void ToggleActive()
        {
            HideUI();
            m_active = !m_active;
            ApplyActive(m_active);
        }

        private void ApplyActive(bool active)
        {
            if (m_control == null || m_index < 0 || m_index >= m_modes.Count) return;
            TestModeEntry entry = m_modes[m_index];
            if (entry.stage == TestStage.VideoScene)
            {
                if (m_control is VideoTestSceneManager video) video.SetVideoPlaying(active);
            }
            else if (!entry.noFilter)
            {
                m_control.StudySetActive(active);
            }
        }

        private void AdvanceToNextMode()
        {
            if (m_modes.Count == 0) return;
            if (m_index >= 0) ApplyActive(false); // leave the mode we're stopped/reset
            m_active = false;
            m_index = (m_index + 1) % m_modes.Count; // loops back to mode 0 after the last mode
            EnterCurrentMode();
        }

        private void EnterCurrentMode()
        {
            if (m_index < 0 || m_index >= m_modes.Count) return;

            TestModeEntry entry = m_modes[m_index];
            string targetScene = entry.stage == TestStage.VideoScene ? m_videoSceneName : m_passthroughSceneName;
            if (SceneManager.GetActiveScene().name != targetScene)
            {
                SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
                return; // OnSceneLoaded re-calls EnterCurrentMode once the target scene is up.
            }

            m_control = FindControl();
            if (m_control == null)
            {
                Debug.LogError($"[TestModeSequencer] No IStudyVignetteControl found in scene '{targetScene}'.");
                return;
            }

            m_control.StudySetMode(entry.vignetteMode);
            m_control.StudyEffectSuppressed = entry.noFilter;
            m_active = false;
            ApplyActive(false); // StudySetMode and video auto-play default to on; force off
                                 // until the first X press.

            // Blob targets are video-scene only: same random set every time, with independent
            // hit/miss results per mode. See BlobTargetController.
            if (entry.stage == TestStage.VideoScene)
            {
                // Lock the window before activating blobs, so the person gate and grading run
                // against constant geometry rather than the gaze-follow fallback.
                if (m_lockVideoWindow)
                {
                    float halfW = m_videoWindowHalfWidthDeg * Mathf.Deg2Rad;
                    float halfH = m_videoWindowHalfHeightDeg * Mathf.Deg2Rad;
                    m_control.StudySetWindow(new Vector4(-halfW, halfW, -halfH, halfH));
                }
                m_blobs.Activate(entry.label);
            }
            else m_blobs.Deactivate();

            ShowInstructions(entry);
        }

        private static IStudyVignetteControl FindControl()
        {
            var video = FindObjectOfType<VideoTestSceneManager>();
            if (video != null) return video;
            return FindObjectOfType<CameraSphereVignetteManager>();
        }

        // ---- world-space instruction UI (built once, survives scene loads) ----

        private void BuildUIIfNeeded()
        {
            if (m_uiRoot != null) return;

            m_uiRoot = new GameObject("TestModeUI");
            m_uiRoot.transform.SetParent(transform, false);
            var canvas = m_uiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rootRt = m_uiRoot.GetComponent<RectTransform>();
            rootRt.sizeDelta = new Vector2(900f, 300f);
            m_uiRoot.transform.localScale = Vector3.one * 0.0015f;

            // Queue 4100: the head-centred vignette sphere renders at queue 3000 and would
            // otherwise occlude a default-material world-space canvas.
            m_uiMat = new Material(Canvas.GetDefaultCanvasMaterial()) { renderQueue = 4100 };

            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(m_uiRoot.transform, false);
            var bg = bgGO.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);
            bg.material = m_uiMat;
            var bgRt = bgGO.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(m_uiRoot.transform, false);
            m_uiText = textGO.AddComponent<Text>();
            m_uiText.material = m_uiMat;
            m_uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            m_uiText.fontSize = 34;
            m_uiText.alignment = TextAnchor.MiddleCenter;
            m_uiText.color = Color.white;
            m_uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
            var textRt = textGO.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(30f, 20f);
            textRt.offsetMax = new Vector2(-30f, -20f);

            m_uiRoot.SetActive(false);
        }

        private void ShowInstructions(TestModeEntry entry)
        {
            BuildUIIfNeeded();
            m_uiText.text = $"{m_index + 1}/{m_modes.Count}  {entry.label}\n\nPress [{m_toggleButton}] to start";
            ShowUI();
        }

        private void ShowUI()
        {
            m_uiRoot.SetActive(true);
            PositionUI();
        }

        private void HideUI()
        {
            if (m_uiRoot != null) m_uiRoot.SetActive(false);
        }

        private void PositionUI()
        {
            Transform head = Camera.main != null ? Camera.main.transform : null;
            if (head == null || m_uiRoot == null) return;

            Vector3 flatFwd = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            Vector3 fwd = flatFwd.sqrMagnitude > 0.001f ? flatFwd.normalized : head.forward;

            m_uiRoot.transform.position = head.position + fwd * m_uiDistance;
            m_uiRoot.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(m_uiDropDeg, 0f, 0f);
        }
    }
}
