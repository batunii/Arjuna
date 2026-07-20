// Runtime loader/presenter for the authored, split target pool (Test/PointAuthoring branch).
//
// Loads pool_split.csv (the counterbalance sets A/B produced by Tools/analysis/split_pool.py),
// resolves each point back to its baked YOLO detection lifetime (for per-frame position), and
// presents ONE set as SIMULTANEOUS ring probes over the driving video while the participant clicks
// them with either controller. Hit/miss/RT per target is logged to authored_results_*.csv.
//
// RUN FLOW (one build = one mode; switch m_mode in the inspector and rebuild):
//   boot  -> video paused at 0, HUD shows the mode + "Press X to start".
//   X     -> video restarts from 0. The practice targets (pool set P — the two temporally-first
//            points) ramp in, then the video PAUSES and the HUD asks the participant to pull
//            EITHER trigger on each highlighted ring. It resumes only when both are clicked.
//   play  -> the set's targets present as rings; clicks score hit/miss/false_alarm as before.
//   end   -> when the video ends (no looping) the pass is over: unresolved targets log as miss,
//            the CSV is closed, HUD shows the tally. X starts a fresh pass (new CSV, same mode).
//
// Modes (single dropdown): BaselineA/B = screening (no filter, that set); FilterA/B, NoFilterA/B =
// experiment with the set forced (piloting); AutoFilter/AutoNoFilter = experiment with the set
// from participant-id parity (even pid: Filter->A, NoFilter->B; odd: swapped).
//
// BlockA_HardDark / BlockA_NoFilter = the passthrough test block's two arms, launched from the
// same dropdown so ALL test controls live in one place. X loads the CameraSphereVignette scene;
// the presenter survives the load just long enough to configure the vignette manager there —
// HardDark mode with the world-anchor depth-raycast backend (EnvironmentRaycastManager, wired in
// that scene since 2026-07-16: painted windows lock onto real geometry), or the same setup with
// the effect suppressed (No Hard Dark baseline — identical procedure, invisible filter). The
// participant paints the window with the right trigger as in free play. Nothing of the authored
// harness runs in these modes. Hold Y (SceneSwitcher) to come back to the video scene.
//
// Participant id -1 = experimenter pilot; results file is named authored_results_PILOT_<mode>_
// <stamp>.csv. Real participants (pid >= 0) get authored_results_P<pid>_<mode>_<stamp>.csv.
// Every pass gets its own timestamped file either way.
//
// Practice: pool rows with set=P are shown in EVERY run as warm-up. They behave like normal
// probes but log set=P, so analysis drops them (the participant's first two clicks never score).
//
// Condition drives the filter: NoFilter/Baseline suppress the vignette (raw video); Filter forms
// the vignette (m_filterMode) over the locked window. Probes are composited AFTER the filter, so a
// target in a defocused area is filtered too (supervisor Point 3). Window painting is locked off.
//
// pool_split.csv is loaded from persistentDataPath (adb push it there) with a StreamingAssets
// fallback (editor/standalone only). Disables TestModeSequencer so the two don't fight.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PassthroughCameraSamples.ShaderSample.Study
{
    public class AuthoredTargetPresenter : MonoBehaviour
    {
        public enum StudyMode
        {
            BaselineA, BaselineB,
            FilterA, FilterB,
            NoFilterA, NoFilterB,
            AutoFilter, AutoNoFilter,
            // The passthrough test block's two arms — X loads the passthrough scene and the
            // launcher configures its vignette manager: HardDark (world-anchored via the scene's
            // EnvironmentRaycastManager) vs the suppressed-effect baseline. Appended last so
            // existing serialized m_mode values keep their meaning.
            BlockA_HardDark,
            BlockA_NoFilter,
        }

        private enum Phase { Armed, Running, Done }

        [SerializeField] private VideoTestSceneManager m_video;
        [SerializeField] private string m_poolFileName = "pool_split.csv";

        [Header("Run identity")]
        [Tooltip("-1 = experimenter pilot run (results named PILOT). >= 0 = real participant.")]
        [SerializeField] private int m_participantId = -1;
        [Tooltip("The ONE thing to change between builds. Baseline = screening (no filter). Filter/NoFilter with a set = forced (piloting). Auto* = set from participant-id parity. BlockA_* = the passthrough test block (X switches scene): HardDark (world-anchored window via depth raycast) or NoFilter (same procedure, effect suppressed).")]
        [SerializeField] private StudyMode m_mode = StudyMode.BaselineA;
        [SerializeField] private VignetteMode m_filterMode = VignetteMode.SignPop;

        [Header("Block A launch (m_mode = BlockA only)")]
        [Tooltip("Scene loaded when launching Block A. Its StudyRig (ConditionSequencer + CPTPanel + StudyLogger) runs the formal protocol, keyboard-driven over adb. Must match SceneSwitcher's name and be in Build Settings.")]
        [SerializeField] private string m_blockASceneName = "CameraSphereVignette";

        [Header("Locked focus window (constant geometry, both conditions)")]
        [Tooltip("Half-width/height of the fixed clear window in degrees (window = 2x these). Painting is locked off, so this IS the window. Raise toward 25/15 for a windscreen.")]
        [SerializeField, Range(0.1f, 80f)] private float m_windowHalfWidthDeg = 25f;
        [SerializeField, Range(0.1f, 60f)] private float m_windowHalfHeightDeg = 15f;

        [Header("Probe (ring) — matches the piloted style")]
        [SerializeField, Range(0.2f, 8f)] private float m_probeSizeDeg = 1.6f;
        [SerializeField] private Color m_ringColor = new(1f, 0.85f, 0.1f, 0.4f);
        [SerializeField, Range(0.02f, 0.2f)] private float m_ringWidth = 0.05f;
        [SerializeField, Range(0f, 2f)] private float m_onsetRampSeconds = 0.5f;
        [SerializeField] private Color m_hitFlashColor = new(0.3f, 1f, 0.4f, 0.9f);
        [SerializeField, Range(0.05f, 1f)] private float m_hitFlashSeconds = 0.25f;

        [Header("Input")]
        [SerializeField] private OVRInput.RawButton m_startButton = OVRInput.RawButton.X;
        [SerializeField] private OVRInput.RawButton m_markLeft = OVRInput.RawButton.LIndexTrigger;
        [SerializeField] private OVRInput.RawButton m_markRight = OVRInput.RawButton.RIndexTrigger;

        [Header("Hit detection")]
        [SerializeField, Range(1f, 30f)] private float m_hitAngleToleranceDeg = 10f;
        [SerializeField, Range(0f, 3f)] private float m_hitGraceSec = 0.75f;
        [SerializeField, Range(0f, 2f)] private float m_holdSeconds = 0.5f;

        private static readonly HashSet<int> k_classes = new() { 0, 9, 11 };

        private class Target
        {
            public int id, cls;
            public string set, kind;
            public float tStart, tEnd;
            public float azFallback, elFallback;                 // radians (from az/el mid deg)
            public List<(float t, Vector4 box)> samples;         // resolved lifetime, or null
            public bool resolved;
            public float flash;                                  // hit-flash, decays to 0
        }

        private readonly List<Target> m_targets = new();
        private string m_setForRun;
        private bool m_initialised;
        private Phase m_phase = Phase.Armed;
        private float m_lastVt = -1f;

        // practice gate
        private float m_practiceGateT = -1f;    // video time at which to pause for practice clicks
        private bool m_practicePaused;
        private bool m_practiceDone;

        // per-pass tally for the end-of-run HUD
        private int m_passHits, m_passMisses, m_passFalseAlarms, m_passNumber;

        private readonly Vector4[] m_data = new Vector4[12];
        private readonly Vector4[] m_flash = new Vector4[12];

        private StreamWriter m_log;
        private string m_logPath;
        private const string k_header =
            "t_ms,pid,condition,set,target_id,kind,t_start,t_end,duration_s,outcome,rt_s,angle_deg";

        // HUD (same proven pattern as PointAuthoringTool: world-space canvas, head-following)
        private GameObject m_uiRoot;
        private Text m_uiText;

        private static string Kind(int cls) => cls switch
        {
            9 => "traffic_light", 11 => "stop_sign", 0 => "person", _ => "unknown",
        };

        private bool IsBlockALaunch =>
            m_mode == StudyMode.BlockA_HardDark || m_mode == StudyMode.BlockA_NoFilter;

        private bool IsBaseline => m_mode == StudyMode.BaselineA || m_mode == StudyMode.BaselineB;

        private bool IsFilterCondition =>
            m_mode == StudyMode.FilterA || m_mode == StudyMode.FilterB || m_mode == StudyMode.AutoFilter;

        private string ConditionLabel => IsBaseline ? "BASELINE" : (IsFilterCondition ? "Filter" : "NoFilter");

        private string ModeTag => $"{(IsBaseline ? "BASELINE" : IsFilterCondition ? "FILTER" : "NOFILTER")}-{m_setForRun}";

        private string WhoTag => m_participantId < 0 ? "PILOT" : $"P{m_participantId}";

        private void Start()
        {
            if (IsBlockALaunch)
            {
                BuildHUD();
                SetHUD(BlockAHudText());
                Debug.Log($"[AuthoredPresenter] {m_mode} launcher armed. Press {m_startButton} to load "
                        + $"'{m_blockASceneName}' — its StudyRig runs the formal protocol.");
                return;
            }
            if (m_video == null) m_video = FindObjectOfType<VideoTestSceneManager>();
            m_setForRun = ResolveSet();
            LoadPool();
            Debug.Log($"[AuthoredPresenter] {WhoTag} {ModeTag}: {m_targets.Count} targets "
                    + $"(incl. practice). Press {m_startButton} to start.");
            BuildHUD();
        }

        private string ResolveSet()
        {
            switch (m_mode)
            {
                case StudyMode.BaselineA:
                case StudyMode.FilterA:
                case StudyMode.NoFilterA: return "A";
                case StudyMode.BaselineB:
                case StudyMode.FilterB:
                case StudyMode.NoFilterB: return "B";
                default: // Auto*: even pid: Filter->A, NoFilter->B ; odd pid: swapped.
                    bool even = (m_participantId % 2) == 0;
                    bool wantA = IsFilterCondition == even;
                    return wantA ? "A" : "B";
            }
        }

        private void Update()
        {
            if (TestModeSequencer.Instance != null) { Destroy(TestModeSequencer.Instance.gameObject); return; }
            if (IsBlockALaunch) { TickBlockALauncher(); return; }
            if (m_video == null) return;

            if (!m_initialised)
            {
                m_initialised = true;
                ApplyCondition();
                ResolveLifetimes();
                ComputePracticeGate();
                m_video.SetBlobProbeStatics(3 /*ring*/, 0.18f, 0.7f, 0.08f, 0.6f, 0.45f,
                                            m_ringColor, m_ringWidth, m_hitFlashColor);
                m_video.VideoLooping = false;
                m_video.SetVideoPlaying(false);
                m_video.SetBlobProbes(0, m_data, m_flash);
                SetHUD($"{WhoTag}  |  {ModeTag}\n\nPress X to start");
            }

            switch (m_phase)
            {
                case Phase.Armed:
                case Phase.Done:
                    m_video.VideoLooping = false;   // the manager boots with looping on; keep it off
                    if (OVRInput.GetDown(m_startButton)) StartPass();
                    break;
                case Phase.Running:
                    TickRun();
                    break;
            }
        }

        private void StartPass()
        {
            foreach (var t in m_targets) { t.resolved = false; t.flash = 0f; }
            m_passHits = m_passMisses = m_passFalseAlarms = 0;
            m_passNumber++;
            m_practicePaused = false;
            m_practiceDone = m_practiceGateT < 0f;   // no practice targets -> skip the gate
            m_lastVt = -1f;
            m_video.VideoLooping = false;
            m_video.RestartVideo();
            m_phase = Phase.Running;
            SetHUD("");
            Debug.Log($"[AuthoredPresenter] pass {m_passNumber} started ({ModeTag}).");
        }

        // ---- Block A launcher (m_mode = BlockA_HardDark / BlockA_NoFilter) ----
        // X performs a full scene load into the passthrough scene. The launcher survives the load
        // (DontDestroyOnLoad) just long enough to configure the CameraSphereVignetteManager there:
        // both arms get mode HardDark and free painting (right trigger — with the scene's
        // EnvironmentRaycastManager wired, the painted window world-anchors onto real geometry),
        // and the NoFilter arm suppresses the effect so the procedure is identical but invisible
        // (the standard baseline pattern). Then it shows brief instructions and destroys itself;
        // hold Y (SceneSwitcher) to come back.

        private bool m_launchingBlockA;
        private bool m_blockAInit;

        private string BlockAHudText() =>
            (m_mode == StudyMode.BlockA_HardDark
                ? "BLOCK A — HARD DARK (world-anchored)\n"
                : "BLOCK A — NO FILTER (baseline)\n")
          + $"Press {m_startButton} to launch the passthrough scene";

        private void TickBlockALauncher()
        {
            if (!m_blockAInit)
            {
                // Same first-Update timing the normal path uses: hold the video scene quiet
                // behind the launcher HUD (the manager boots with the clip looping).
                m_blockAInit = true;
                if (m_video == null) m_video = FindObjectOfType<VideoTestSceneManager>();
                if (m_video != null) { m_video.VideoLooping = false; m_video.SetVideoPlaying(false); }
            }
            if (m_launchingBlockA || !OVRInput.GetDown(m_startButton)) return;
            if (!Application.CanStreamedLevelBeLoaded(m_blockASceneName))
            {
                Debug.LogError($"[AuthoredPresenter] scene '{m_blockASceneName}' is not in Build Settings — cannot launch Block A.");
                SetHUD($"Scene '{m_blockASceneName}' missing from build!");
                return;
            }
            m_launchingBlockA = true;
            SetHUD("Loading Block A…");
            Debug.Log($"[AuthoredPresenter] Block A launch ({m_mode}) -> {m_blockASceneName}");
            transform.SetParent(null);            // DontDestroyOnLoad needs a root object
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnBlockASceneLoaded;
            SceneManager.LoadScene(m_blockASceneName, LoadSceneMode.Single);
        }

        private void OnBlockASceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            SceneManager.sceneLoaded -= OnBlockASceneLoaded;
            StartCoroutine(ConfigureBlockA());
        }

        private System.Collections.IEnumerator ConfigureBlockA()
        {
            yield return null;   // let the scene's own Awake/Start settle first
            var mgr = FindObjectOfType<CameraSphereVignetteManager>();
            if (mgr == null)
            {
                Debug.LogError("[AuthoredPresenter] no CameraSphereVignetteManager in the loaded scene — Block A not configured.");
                Destroy(gameObject);
                yield break;
            }

            bool hardDark = m_mode == StudyMode.BlockA_HardDark;
            IStudyVignetteControl ctrl = mgr;
            ctrl.StudySetMode(VignetteMode.HardDark);   // both arms: identical mode/procedure
            ctrl.StudyEffectSuppressed = !hardDark;     // baseline arm = same run, invisible effect
            ctrl.StudyInputLock = false;                // participant paints the window

            // World-anchor backend sanity check: without the scene's EnvironmentRaycastManager the
            // painted Hard Dark window silently falls back to head-relative bearing.
            if (hardDark && FindObjectOfType<Meta.XR.EnvironmentRaycastManager>() == null)
                Debug.LogWarning("[AuthoredPresenter] EnvironmentRaycastManager missing — Hard Dark "
                               + "window will NOT world-anchor (head-relative bearing fallback).");

            Debug.Log($"[AuthoredPresenter] Block A configured: {(hardDark ? "HardDark (world-anchored)" : "NoFilter baseline")}.");
            SetHUD((hardDark ? "BLOCK A — HARD DARK\n" : "BLOCK A — NO FILTER\n")
                 + "Hold RIGHT trigger to paint the window, release to lock\n"
                 + "B clears — hold Y to return to the video scene");
            yield return new WaitForSeconds(6f);
            Destroy(gameObject);
        }

        private void TickRun()
        {
            float vt = m_video.VideoTime;
            if (vt < 0f) return;
            float dt = Time.deltaTime;
            m_lastVt = vt;

            // Practice gate: once the practice rings have fully ramped in, hold the video and
            // require a click on each before the clip proceeds (the participant's warm-up).
            if (!m_practiceDone)
            {
                if (!m_practicePaused && vt >= m_practiceGateT)
                {
                    m_practicePaused = true;
                    m_video.SetVideoPlaying(false);
                    SetHUD("Practice: pull EITHER trigger\nto select each highlighted ring");
                }
                if (m_practicePaused && AllPracticeResolved())
                {
                    m_practicePaused = false;
                    m_practiceDone = true;
                    m_video.SetVideoPlaying(true);
                    SetHUD("");
                }
            }

            // End of clip = end of pass (video does not loop).
            double len = m_video.VideoLength;
            if (m_practiceDone && len > 1.0 && vt >= (float)len - 0.15f) { EndPass(); return; }

            // Resolve misses (window + grace passed, never hit).
            foreach (var t in m_targets)
                if (!t.resolved && vt > t.tEnd + m_hitGraceSec && vt < t.tEnd + m_hitGraceSec + 0.5f)
                    { t.resolved = true; m_passMisses++; Log(t, "miss", -1f, -1f); }

            // Build the simultaneous probe set (active, unresolved, or flashing).
            int n = 0;
            for (int i = 0; i < m_targets.Count && n < m_data.Length; i++)
            {
                var t = m_targets[i];
                bool live = vt >= t.tStart && vt <= t.tEnd && !t.resolved;
                if (t.flash > 0f) t.flash = Mathf.Max(0f, t.flash - dt / Mathf.Max(m_hitFlashSeconds, 1e-4f));
                if (!live && t.flash <= 0f) continue;
                Vector2 azel = PosAt(t, vt);
                float ramp = m_onsetRampSeconds > 0f ? Mathf.Clamp01((vt - t.tStart) / m_onsetRampSeconds) : 1f;
                m_data[n] = new Vector4(azel.x, azel.y, 0.5f * m_probeSizeDeg * Mathf.Deg2Rad,
                                        live ? ramp : 1f);
                m_flash[n] = new Vector4(t.flash, 0f, 0f, 0f);
                n++;
            }
            m_video.SetBlobProbes(n, m_data, m_flash);

            // Clicks (either controller) → nearest live target within tolerance.
            if (OVRInput.GetDown(m_markLeft)) { m_video.GetLeftControllerAzEl(out float a, out float e); TryHit(a, e, vt); }
            if (OVRInput.GetDown(m_markRight)) { m_video.GetRightControllerAzEl(out float a, out float e); TryHit(a, e, vt); }
        }

        private void EndPass()
        {
            foreach (var t in m_targets)
                if (!t.resolved) { t.resolved = true; m_passMisses++; Log(t, "miss", -1f, -1f); }
            m_video.SetBlobProbes(0, m_data, m_flash);
            m_video.SetVideoPlaying(false);
            CloseLog();
            m_phase = Phase.Done;
            int scored = m_passHits + m_passMisses;   // includes practice; close enough for the HUD
            SetHUD($"Pass {m_passNumber} complete — {m_passHits}/{scored} hit, "
                 + $"{m_passFalseAlarms} false alarms\n\nPress X for another pass");
            Debug.Log($"[AuthoredPresenter] pass {m_passNumber} done: {m_passHits}/{scored} hit, "
                    + $"{m_passFalseAlarms} FA -> {m_logPath}");
        }

        private bool AllPracticeResolved()
        {
            foreach (var t in m_targets)
                if (t.set == "P" && !t.resolved) return false;
            return true;
        }

        private void ComputePracticeGate()
        {
            m_practiceGateT = -1f;
            foreach (var t in m_targets)
                if (t.set == "P")
                    m_practiceGateT = Mathf.Max(m_practiceGateT, t.tStart + m_onsetRampSeconds);
        }

        private void TryHit(float az, float el, float vt)
        {
            Target best = null; float bestAng = m_hitAngleToleranceDeg;
            foreach (var t in m_targets)
            {
                if (t.resolved || vt < t.tStart || vt > t.tEnd + m_hitGraceSec) continue;
                Vector2 p = PosAt(t, vt);
                float ang = AngDeg(az, el, p.x, p.y);
                if (ang < bestAng) { bestAng = ang; best = t; }
            }
            if (best == null) { m_passFalseAlarms++; Log(null, "false_alarm", -1f, -1f); return; }
            best.resolved = true; best.flash = 1f;
            m_passHits++;
            Log(best, "hit", vt - best.tStart, bestAng);
        }

        // ---- condition / window ----

        private void ApplyCondition()
        {
            m_video.StudyInputLock = true;   // participant can't repaint the window
            m_video.MotionEnabled = false;   // the filter IS the manipulation — don't let head motion fade it
            float halfW = m_windowHalfWidthDeg * Mathf.Deg2Rad, halfH = m_windowHalfHeightDeg * Mathf.Deg2Rad;
            m_video.StudySetWindow(new Vector4(-halfW, halfW, -halfH, halfH));
            if (!IsBaseline && IsFilterCondition)
            {
                m_video.StudyEffectSuppressed = false;
                m_video.StudySetMode(m_filterMode);
                m_video.StudySetActive(true);               // form the vignette
            }
            else
            {
                m_video.StudyEffectSuppressed = true;       // raw video (baseline screening + no-filter condition)
            }
        }

        // ---- pool loading + lifetime resolution ----

        private void LoadPool()
        {
            string path = Path.Combine(Application.persistentDataPath, m_poolFileName);
            if (!File.Exists(path))
            {
                string alt = Path.Combine(Application.streamingAssetsPath, m_poolFileName);
                if (File.Exists(alt)) path = alt;
                else { Debug.LogError($"[AuthoredPresenter] pool file not found: {path} (nor StreamingAssets)."); return; }
            }
            string[] lines = File.ReadAllLines(path);
            if (lines.Length < 2) { Debug.LogError("[AuthoredPresenter] pool file empty."); return; }
            var cols = new Dictionary<string, int>();
            var hdr = lines[0].Split(',');
            for (int i = 0; i < hdr.Length; i++) cols[hdr[i].Trim()] = i;
            for (int li = 1; li < lines.Length; li++)
            {
                if (string.IsNullOrWhiteSpace(lines[li])) continue;
                var f = lines[li].Split(',');
                string set = f[cols["set"]].Trim();
                // Set "P" = practice targets: shown in EVERY run (both sets, both conditions) as
                // warm-up; logged with set=P so analysis excludes them.
                if (set != m_setForRun && set != "P") continue;
                int cls = int.Parse(f[cols["cls"]], CultureInfo.InvariantCulture);
                m_targets.Add(new Target
                {
                    id = int.Parse(f[cols["point_id"]], CultureInfo.InvariantCulture),
                    cls = cls, kind = Kind(cls), set = set,
                    tStart = P(f, cols, "t_start"), tEnd = P(f, cols, "t_end"),
                    azFallback = P(f, cols, "az_mid_deg") * Mathf.Deg2Rad,
                    elFallback = P(f, cols, "el_mid_deg") * Mathf.Deg2Rad,
                });
            }
        }

        private void ResolveLifetimes()
        {
            var lifetimes = m_video.BuildDetectionLifetimes(k_classes, m_holdSeconds);
            if (lifetimes == null) return;
            foreach (var t in m_targets)
            {
                foreach (var l in lifetimes)
                    if (l.cls == t.cls && Mathf.Abs(l.tStart - t.tStart) < 0.06f && Mathf.Abs(l.tEnd - t.tEnd) < 0.06f)
                        { t.samples = l.samples; break; }
                if (t.samples == null)
                    Debug.LogWarning($"[AuthoredPresenter] target {t.id} ({t.kind} {t.tStart:F1}-{t.tEnd:F1}s) "
                                   + "did not match a lifetime — using its static position.");
            }
        }

        private Vector2 PosAt(Target t, float vt)
        {
            if (t.samples == null || t.samples.Count == 0)
                return new Vector2(t.azFallback, t.elFallback);
            var s = t.samples;
            var s0 = s[0]; var s1 = s[s.Count - 1];
            for (int i = 0; i < s.Count - 1; i++)
                if (vt >= s[i].t && vt <= s[i + 1].t) { s0 = s[i]; s1 = s[i + 1]; break; }
            float frac = s1.t > s0.t ? Mathf.Clamp01((vt - s0.t) / (s1.t - s0.t)) : 0f;
            Vector4 r = m_video.DetectionBoxToAzElRect(Vector4.Lerp(s0.box, s1.box, frac));
            return new Vector2((r.x + r.y) * 0.5f, (r.z + r.w) * 0.5f);
        }

        private static float AngDeg(float az1, float el1, float az2, float el2) =>
            Vector3.Angle(Dir(az1, el1), Dir(az2, el2));

        private static Vector3 Dir(float az, float el)
        {
            float cosEl = Mathf.Cos(el);
            return new Vector3(Mathf.Sin(az) * cosEl, Mathf.Sin(el), Mathf.Cos(az) * cosEl);
        }

        // ---- HUD ----

        private void SetHUD(string text)
        {
            if (m_uiText != null) m_uiText.text = text;
            if (m_uiRoot != null) m_uiRoot.SetActive(!string.IsNullOrEmpty(text));
        }

        private void BuildHUD()
        {
            if (m_uiRoot != null) return;
            m_uiRoot = new GameObject("PresenterHUD");
            m_uiRoot.transform.SetParent(transform, false);
            var canvas = m_uiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            m_uiRoot.GetComponent<RectTransform>().sizeDelta = new Vector2(700f, 200f);
            m_uiRoot.transform.localScale = Vector3.one * 0.0015f;
            var mat = new Material(Canvas.GetDefaultCanvasMaterial()) { renderQueue = 4100 };

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(m_uiRoot.transform, false);
            m_uiText = textGO.AddComponent<Text>();
            m_uiText.material = mat;
            m_uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            m_uiText.fontSize = 30;
            m_uiText.alignment = TextAnchor.MiddleCenter;
            m_uiText.color = Color.white;
            var rt = textGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private void LateUpdate()
        {
            if (m_uiRoot == null || !m_uiRoot.activeSelf || Camera.main == null) return;
            Transform h = Camera.main.transform;
            Vector3 fwd = Vector3.ProjectOnPlane(h.forward, Vector3.up);
            fwd = fwd.sqrMagnitude > 0.001f ? fwd.normalized : h.forward;
            m_uiRoot.transform.position = h.position + fwd * 1.5f + Vector3.down * 0.35f;
            m_uiRoot.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }

        // ---- logging ----

        private void Log(Target t, string outcome, float rt, float ang)
        {
            if (m_log == null)
            {
                string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                m_logPath = Path.Combine(Application.persistentDataPath,
                                         $"authored_results_{WhoTag}_{ModeTag}_{stamp}.csv");
                m_log = new StreamWriter(m_logPath, false, new UTF8Encoding(false));
                m_log.WriteLine(k_header);
                Debug.Log($"[AuthoredPresenter] logging to {m_logPath}");
            }
            long ms = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // Target rows carry the target's OWN set: practice targets log set=P (excluded in
            // analysis) even though they play inside an A/B run.
            string row = t != null
                ? string.Join(",", ms, m_participantId, ConditionLabel, t.set, t.id, t.kind,
                              F(t.tStart), F(t.tEnd), F(t.tEnd - t.tStart), outcome, F(rt), F(ang))
                : string.Join(",", ms, m_participantId, ConditionLabel, m_setForRun, -1, "",
                              "", "", "", outcome, F(rt), F(ang));
            m_log.WriteLine(row); m_log.Flush();
        }

        private void CloseLog()
        {
            m_log?.Flush(); m_log?.Dispose(); m_log = null;
        }

        private static float P(string[] f, Dictionary<string, int> cols, string key) =>
            float.Parse(f[cols[key]], CultureInfo.InvariantCulture);

        private static string F(float v) => v < 0f ? "" : v.ToString("F3", CultureInfo.InvariantCulture);

        private void OnDestroy() { CloseLog(); }
    }
}
