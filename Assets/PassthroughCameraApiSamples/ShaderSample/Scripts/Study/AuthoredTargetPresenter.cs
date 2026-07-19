// Runtime loader/presenter for the authored, split target pool (Test/PointAuthoring branch).
//
// Loads pool_split.csv (the counterbalance sets A/B produced by Tools/analysis/split_pool.py),
// resolves each point back to its baked YOLO detection lifetime (for per-frame position), and
// presents ONE set as SIMULTANEOUS ring probes over the driving video while the participant clicks
// them with either controller. Hit/miss/RT per target is logged to authored_results_<stamp>.csv.
//
// Counterbalance: which set is shown depends on (participant id parity, condition), so across
// participants each set appears equally in each condition:
//     even pid: Filter->A, NoFilter->B      odd pid: Filter->B, NoFilter->A
// Run once per condition (set m_participantId + m_condition), rebuild/relaunch for the other.
//
// Condition drives the filter: NoFilter suppresses the vignette (raw video); Filter forms the
// vignette (m_filterMode) over the locked window. Probes are composited AFTER the filter, so a
// target in a defocused area is filtered too (supervisor Point 3). Window painting is locked off.
//
// pool_split.csv is loaded from persistentDataPath (adb push it there) with a StreamingAssets
// fallback (editor/standalone only). Disables TestModeSequencer so the two don't fight.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace PassthroughCameraSamples.ShaderSample.Study
{
    public class AuthoredTargetPresenter : MonoBehaviour
    {
        public enum Condition { Filter, NoFilter }
        public enum BaselineSet { All, A, B }
        public enum SetOverride { Auto, A, B }

        [SerializeField] private VideoTestSceneManager m_video;
        [SerializeField] private string m_poolFileName = "pool_split.csv";

        [Header("Run identity (drives counterbalancing)")]
        [SerializeField] private int m_participantId = 1;
        [SerializeField] private Condition m_condition = Condition.NoFilter;
        [SerializeField] private VignetteMode m_filterMode = VignetteMode.SignPop;
        [Tooltip("Experiment runs (baseline off): force a specific set regardless of participant counterbalance — for piloting specific set x condition combos. Auto = normal counterbalance by participant id.")]
        [SerializeField] private SetOverride m_forceSet = SetOverride.Auto;

        [Header("Baseline screening")]
        [Tooltip("Screening pass: NO filter, NO counterbalance. Click everything you can; let it loop a few times. Then screen out targets you couldn't reliably hit. Overrides condition/participant.")]
        [SerializeField] private bool m_baselineMode = false;
        [Tooltip("Baseline only: which set to show. Run A then B to screen each at the SAME simultaneity the experiment uses (one set at a time, ~2-3 rings). All = the whole pool at once (more crowded than the experiment).")]
        [SerializeField] private BaselineSet m_baselineSet = BaselineSet.A;

        [Header("Locked focus window (constant geometry, both conditions)")]
        [Tooltip("Half-width/height of the fixed clear window in degrees (window = 2x these). Painting is locked off, so this IS the window. Slider goes down to 0.1deg half (0.2deg window) for a very small focus; raise toward 25/15 for a windscreen.")]
        [SerializeField, Range(0.1f, 80f)] private float m_windowHalfWidthDeg = 1f;
        [SerializeField, Range(0.1f, 60f)] private float m_windowHalfHeightDeg = 1f;

        [Header("Probe (ring) — matches the piloted style")]
        [SerializeField, Range(0.2f, 8f)] private float m_probeSizeDeg = 1.6f;
        [SerializeField] private Color m_ringColor = new(1f, 0.85f, 0.1f, 0.4f);
        [SerializeField, Range(0.02f, 0.2f)] private float m_ringWidth = 0.05f;
        [SerializeField, Range(0f, 2f)] private float m_onsetRampSeconds = 0.5f;
        [SerializeField] private Color m_hitFlashColor = new(0.3f, 1f, 0.4f, 0.9f);
        [SerializeField, Range(0.05f, 1f)] private float m_hitFlashSeconds = 0.25f;

        [Header("Hit detection")]
        [SerializeField] private OVRInput.RawButton m_markLeft = OVRInput.RawButton.LIndexTrigger;
        [SerializeField] private OVRInput.RawButton m_markRight = OVRInput.RawButton.RIndexTrigger;
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
        private bool m_started;
        private float m_lastVt = -1f;

        private readonly Vector4[] m_data = new Vector4[12];
        private readonly Vector4[] m_flash = new Vector4[12];

        private StreamWriter m_log;
        private string m_logPath;
        private const string k_header =
            "t_ms,pid,condition,set,target_id,kind,t_start,t_end,duration_s,outcome,rt_s,angle_deg";

        private static string Kind(int cls) => cls switch
        {
            9 => "traffic_light", 11 => "stop_sign", 0 => "person", _ => "unknown",
        };

        private void Start()
        {
            if (m_video == null) m_video = FindObjectOfType<VideoTestSceneManager>();
            m_setForRun = m_baselineMode
                ? (m_baselineSet == BaselineSet.All ? "ALL" : m_baselineSet.ToString())
                : (m_forceSet != SetOverride.Auto ? m_forceSet.ToString()
                                                  : SetForRun(m_participantId, m_condition));
            LoadPool();
            Debug.Log(m_baselineMode
                ? $"[AuthoredPresenter] BASELINE screening (set {m_setForRun}), no filter; {m_targets.Count} targets. Click everything."
                : $"[AuthoredPresenter] pid {m_participantId} / {m_condition} -> set {m_setForRun}; {m_targets.Count} targets.");
        }

        // even pid: Filter->A, NoFilter->B ; odd pid: swapped. Each set appears in each condition
        // equally across an even participant count.
        private static string SetForRun(int pid, Condition cond)
        {
            bool even = (pid % 2) == 0;
            bool wantA = (cond == Condition.Filter) == even;
            return wantA ? "A" : "B";
        }

        private void Update()
        {
            if (TestModeSequencer.Instance != null) { Destroy(TestModeSequencer.Instance.gameObject); return; }
            if (m_video == null) return;

            if (!m_started)
            {
                m_started = true;
                ApplyCondition();
                ResolveLifetimes();
                m_video.SetBlobProbeStatics(3 /*ring*/, 0.18f, 0.7f, 0.08f, 0.6f, 0.45f,
                                            m_ringColor, m_ringWidth, m_hitFlashColor);
                m_video.SetVideoPlaying(true);
            }

            float vt = m_video.VideoTime;
            if (vt < 0f) return;
            float dt = Time.deltaTime;

            if (m_lastVt >= 0f && vt < m_lastVt - 1f)         // video looped
                foreach (var t in m_targets) { t.resolved = false; t.flash = 0f; }
            m_lastVt = vt;

            // Resolve misses (window + grace passed, never hit).
            foreach (var t in m_targets)
                if (!t.resolved && vt > t.tEnd + m_hitGraceSec && vt < t.tEnd + m_hitGraceSec + 0.5f)
                    { t.resolved = true; Log(t, "miss", -1f, -1f); }

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
            if (best == null) { Log(null, "false_alarm", -1f, -1f); return; }
            best.resolved = true; best.flash = 1f;
            Log(best, "hit", vt - best.tStart, bestAng);
        }

        // ---- condition / window ----

        private void ApplyCondition()
        {
            m_video.StudyInputLock = true;   // participant can't repaint the window
            m_video.MotionEnabled = false;   // the filter IS the manipulation — don't let head motion fade it
            float halfW = m_windowHalfWidthDeg * Mathf.Deg2Rad, halfH = m_windowHalfHeightDeg * Mathf.Deg2Rad;
            m_video.StudySetWindow(new Vector4(-halfW, halfW, -halfH, halfH));
            if (m_baselineMode || m_condition == Condition.NoFilter)
            {
                m_video.StudyEffectSuppressed = true;       // raw video (baseline screening + no-filter condition)
            }
            else
            {
                m_video.StudyEffectSuppressed = false;
                m_video.StudySetMode(m_filterMode);
                m_video.StudySetActive(true);               // form the vignette
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
                if (m_setForRun != "ALL" && set != m_setForRun) continue; // "ALL" = whole pool (baseline)
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

        // ---- logging ----

        private void Log(Target t, string outcome, float rt, float ang)
        {
            if (m_log == null)
            {
                string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                m_logPath = Path.Combine(Application.persistentDataPath, $"authored_results_{stamp}.csv");
                m_log = new StreamWriter(m_logPath, false, new UTF8Encoding(false));
                m_log.WriteLine(k_header);
                Debug.Log($"[AuthoredPresenter] logging to {m_logPath}");
            }
            long ms = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string cond = m_baselineMode ? "BASELINE" : m_condition.ToString();
            string row = t != null
                ? string.Join(",", ms, m_participantId, cond, m_setForRun, t.id, t.kind,
                              F(t.tStart), F(t.tEnd), F(t.tEnd - t.tStart), outcome, F(rt), F(ang))
                : string.Join(",", ms, m_participantId, cond, m_setForRun, -1, "",
                              "", "", "", outcome, F(rt), F(ang));
            m_log.WriteLine(row); m_log.Flush();
        }

        private static float P(string[] f, Dictionary<string, int> cols, string key) =>
            float.Parse(f[cols[key]], CultureInfo.InvariantCulture);

        private static string F(float v) => v < 0f ? "" : v.ToString("F3", CultureInfo.InvariantCulture);

        private void OnDestroy() { m_log?.Flush(); m_log?.Dispose(); }
    }
}
