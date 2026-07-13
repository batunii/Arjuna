// Study orchestrator (testing-strategy-v2 §13). One sequencer per scene:
//   Plan = PassthroughScene_BlockA  → CameraSphereVignette scene (Hard Dark workstation 2×2)
//   Plan = VideoScene_BlocksBC      → VideoTestScene (ColorPop probes + mode sampler)
//
// Counterbalancing is derived from the participant ID — no experimenter arithmetic:
//   Block A: 4×4 Williams balanced Latin square, row = pid % 4  (A1..A4 = off/black,
//            off/reel, on/black, on/reel; "off" = vignette suppressed, tablets per payload)
//   Block B: pid % 2 → B1-first (even) or B2-first (odd)
//   Block C: 3×3 Latin square, row = pid % 3 (ColorPop / Soft Dark / Hard Dark orders)
//
// Experimenter drives the session over the existing adb connection (scrcpy session):
//   adb shell input keyevent <code>
//   62 SPACE  advance (next phase / start condition)     48 T  TLX_START / TLX_END toggle
//   44 P      run practice block                         51 W  (re)place CPT panel
//   39 K      skip Block C (fatigue rule §4.3)           37 I  log INCIDENT marker
//   33 E      end session                                7–16  digits 0–9 (participant ID)
//   66 ENTER  confirm typed participant ID and open the session log
// Controller fallback for SPACE: click both thumbsticks and hold ~1 s.
//
// Configuration guards run when the session opens: missing references refuse to run
// (GUARD_FAIL); fixable flags (m_enableMotion for Block B) are forced + logged when
// m_autoFixConfig is on. The full configuration is snapshotted as CONFIG rows.

using System.Text;
using UnityEngine;

namespace PassthroughCameraSamples.ShaderSample.Study
{
    public enum StudyPlan
    {
        PassthroughScene_BlockA,
        VideoScene_BlocksBC,
    }

    public class ConditionSequencer : MonoBehaviour
    {
        // ---- inspector ----

        [SerializeField] private StudyPlan m_plan = StudyPlan.VideoScene_BlocksBC;
        [Tooltip("Fallback participant ID if none is typed at runtime (digits + ENTER via adb).")]
        [SerializeField] private int m_participantId = 0;

        [Tooltip("Any MonoBehaviour implementing IStudyVignetteControl (the scene's vignette manager).")]
        [SerializeField] private MonoBehaviour m_vignetteManagerBehaviour;
        [SerializeField] private StudyLogger m_logger;
        [SerializeField] private CPTPanel m_cptPanel;             // Block A, Option V only
        [SerializeField] private ProbeScheduler m_probeScheduler; // Block B only

        [Tooltip("Option V (virtual CPT panel) vs Option P (paper sheets — panel unused; pilot gate G2 decides).")]
        [SerializeField] private bool m_useCptPanel = true;
        [Tooltip("Force fixable config flags (Block B motion suppression) instead of refusing.")]
        [SerializeField] private bool m_autoFixConfig = true;

        [Header("Blocks B/C fixed focus window")]
        [Tooltip("Vertical centre (deg) of the fixed Blocks B/C window; negative = below straight-ahead " +
                 "(approximates a windscreen sightline). Size comes from the active manager's " +
                 "DefaultWindowHalfWidthDeg (shared with the free-play brush seed), not from here.")]
        [SerializeField] private float m_windscreenElCenterDeg = -2.5f;

        // Nominal durations — MUST match Tools/validate_session.py (±5 % tolerance there).
        private const float k_durA = 210f;
        private const float k_durB = 240f;
        private const float k_durC = 75f;

        private const int k_cptTrials = 140;
        private const int k_practiceTrials = 40;

        // Williams 4×4 balanced Latin square (rows = condition orders, zero-based A1..A4).
        private static readonly int[][] k_blockAOrders =
        {
            new[] { 0, 1, 3, 2 }, new[] { 1, 2, 0, 3 }, new[] { 2, 3, 1, 0 }, new[] { 3, 0, 2, 1 },
        };
        private static readonly VignetteMode[][] k_blockCOrders =
        {
            new[] { VignetteMode.ColorPop, VignetteMode.SoftDark, VignetteMode.HardDark },
            new[] { VignetteMode.SoftDark, VignetteMode.HardDark, VignetteMode.ColorPop },
            new[] { VignetteMode.HardDark, VignetteMode.ColorPop, VignetteMode.SoftDark },
        };

        private enum Phase
        {
            EnterPid,      // typing participant ID (session log not yet open)
            Idle,          // session open, waiting to start the block
            AwaitWindow,   // Block A: participant paints; SPACE locks
            Practice,      // CPT or probe practice running
            Ready,         // waiting for SPACE to start condition m_condIdx
            Condition,     // condition running (timer)
            Gap,           // between conditions (TLX etc.)
            AwaitC,        // after Block B: SPACE starts Block C, K skips it
            SamplerMode,   // Block C mode running
            SamplerGap,    // verbal ratings between sampler modes
            Done,
            Refused,       // guard failure — nothing runs
        }

        private Phase m_phase = Phase.EnterPid;
        private IStudyVignetteControl m_ctrl;
        private int   m_pid;
        private int   m_condIdx;      // Block A: 0..3 into the Latin row; Block B: 0..1; Block C: 0..2
        private float m_condTimer;
        private bool  m_tlxOpen;
        private Phase m_phaseBeforePractice;
        private readonly StringBuilder m_pidEntry = new();


        // ---- lifecycle ----

        private void Start()
        {
            m_ctrl = m_vignetteManagerBehaviour as IStudyVignetteControl;
            Debug.Log($"[Sequencer] Plan={m_plan}. Type participant ID digits, ENTER to open session " +
                      $"(ENTER alone opens default pid {m_participantId}).");
        }

        private void Update()
        {
            // Stamp shared per-row state onto the logger.
            if (m_logger != null && m_logger.SessionOpen && m_ctrl != null)
            {
                m_logger.Mode        = m_ctrl.CurrentMode.ToString();
                m_logger.DrIntensity = m_ctrl.CurrentEffectiveStrength;
            }

            HandleGlobalKeys();

            switch (m_phase)
            {
                case Phase.EnterPid:   UpdateEnterPid(); break;
                case Phase.Practice:   UpdatePractice(); break;
                case Phase.Condition:  UpdateCondition(); break;
                case Phase.SamplerMode: UpdateSamplerMode(); break;
            }
        }

        // ---- experimenter input ----

        private bool AdvancePressed()
        {
            // Keyboard SPACE only. (A thumbstick-click chord used to live here but a
            // headset-wearing solo operator triggers it by accident while adjusting the
            // controllers — it kept jumping the session into a block. Removed.)
            return Input.GetKeyDown(KeyCode.Space);
        }

        private void HandleGlobalKeys()
        {
            if (m_phase == Phase.EnterPid || m_phase == Phase.Refused) { CheckRefusedAdvance(); return; }

            if (Input.GetKeyDown(KeyCode.I))
                m_logger?.LogIncident("marker (details in paper incident log)");

            if (Input.GetKeyDown(KeyCode.T))
            {
                m_tlxOpen = !m_tlxOpen;
                m_logger?.LogEvent(m_tlxOpen ? "TLX_START" : "TLX_END");
                Debug.Log($"[Sequencer] {(m_tlxOpen ? "TLX_START" : "TLX_END")}");
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                m_logger?.EndSession("experimenter");
                ResetForNextParticipant();
                return;
            }

            if (Input.GetKeyDown(KeyCode.W) && m_plan == StudyPlan.PassthroughScene_BlockA
                && m_useCptPanel && m_cptPanel != null && m_phase != Phase.Condition)
            {
                m_cptPanel.PlaceAtGaze();
                Debug.Log("[Sequencer] CPT panel (re)placed at gaze.");
            }

            if (Input.GetKeyDown(KeyCode.P) && (m_logger != null && m_logger.SessionOpen)
                && m_phase != Phase.Condition && m_phase != Phase.Practice && m_phase != Phase.Done)
                StartPractice();

            if (Input.GetKeyDown(KeyCode.K) && m_plan == StudyPlan.VideoScene_BlocksBC
                && (m_phase == Phase.AwaitC || m_phase == Phase.Gap))
            {
                m_logger?.LogEvent("BLOCKC_SKIPPED", "reason=experimenter_skip_rule");
                Debug.Log("[Sequencer] Block C skipped (fatigue/overrun rule).");
                FinishBlock("C_SKIPPED");
                return;
            }

            if (AdvancePressed()) OnAdvance();
        }

        private void CheckRefusedAdvance()
        {
            if (m_phase == Phase.Refused && Input.GetKeyDown(KeyCode.E))
            {
                m_logger?.EndSession("guard_refused");
                ResetForNextParticipant();
            }
        }

        // Return to the "type participant ID" state so the next participant can be run
        // without relaunching the app. Called after every session ends.
        private void ResetForNextParticipant()
        {
            m_pidEntry.Clear();
            m_condIdx = 0;
            m_tlxOpen = false;
            if (m_ctrl != null)
            {
                m_ctrl.StudyEffectSuppressed = true;
                m_ctrl.StudyInputLock = false;
            }
            if (m_logger != null) { m_logger.Block = ""; m_logger.Condition = ""; }
            m_phase = Phase.EnterPid;
            Debug.Log("[Sequencer] Session closed. Ready for next participant — type ID digits then ENTER.");
        }

        // ---- participant ID entry ----

        private void UpdateEnterPid()
        {
            for (var k = KeyCode.Alpha0; k <= KeyCode.Alpha9; k++)
                if (Input.GetKeyDown(k)) m_pidEntry.Append((char)('0' + (k - KeyCode.Alpha0)));

            // ENTER only (not SPACE) opens a session — a stray SPACE keycode (any input
            // device, including a controller's HID-mapped buttons) must never silently
            // start a session; ENTER with no digits typed already falls back to the
            // default pid below, so SPACE added no unique capability, only accidental-open risk.
            bool confirm = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
            if (!confirm) return;

            m_pid = m_pidEntry.Length > 0 && int.TryParse(m_pidEntry.ToString(), out int typed)
                ? typed : m_participantId;
            OpenSession();
        }

        private void OpenSession()
        {
            if (!RunGuards(out string failReason))
            {
                // Open a log anyway so the refusal is on record.
                m_logger?.BeginSession(m_pid.ToString());
                m_logger?.LogEvent("GUARD_FAIL", $"reason={failReason}");
                Debug.LogError($"[Sequencer] GUARD_FAIL: {failReason} — refusing to run. Press E to close.");
                m_phase = Phase.Refused;
                return;
            }

            m_logger.BeginSession(m_pid.ToString());
            LogConfigSnapshot();

            if (m_plan == StudyPlan.VideoScene_BlocksBC)
            {
                m_ctrl.StudyInputLock = true;
                m_ctrl.StudySetMode(VignetteMode.ColorPop);
                m_ctrl.StudyEffectSuppressed = true;
                float half = m_ctrl.DefaultWindowHalfWidthDeg;
                var fixedWindowDeg = new Vector4(-half, half,
                    m_windscreenElCenterDeg - half, m_windscreenElCenterDeg + half);
                m_ctrl.StudySetWindow(DegWindowToRad(fixedWindowDeg));
                m_logger.LogEvent("WINDOW_LOCKED", WindowPayload(m_ctrl.ActiveRect) + ";source=fixed");
            }
            else
            {
                m_ctrl.StudySetMode(VignetteMode.HardDark);
                m_ctrl.StudyEffectSuppressed = true;
                m_ctrl.StudyInputLock = true; // unlocked only for window painting
            }

            m_condIdx = 0;
            m_phase = Phase.Idle;
            Debug.Log($"[Sequencer] Session open for P{m_pid}. SPACE starts the block " +
                      $"({(m_plan == StudyPlan.PassthroughScene_BlockA ? "A: window painting first" : "B")}).");
        }

        // ---- guards & config ----

        private bool RunGuards(out string reason)
        {
            reason = "";
            if (m_logger == null) { reason = "logger_missing"; return false; }
            if (m_ctrl == null)   { reason = "vignette_manager_missing_or_wrong_type"; return false; }

            if (m_plan == StudyPlan.VideoScene_BlocksBC)
            {
                if (m_probeScheduler == null) { reason = "probe_scheduler_missing"; return false; }
                if (!m_ctrl.MotionEnabled)
                {
                    if (!m_autoFixConfig) { reason = "motion_suppression_disabled"; return false; }
                    m_ctrl.MotionEnabled = true; // logged in the CONFIG snapshot as forced
                }
            }
            else if (m_useCptPanel && m_cptPanel == null)
            {
                reason = "cpt_panel_missing_optionV";
                return false;
            }
            return true;
        }

        private void LogConfigSnapshot()
        {
            void C(string kv) => m_logger.LogEvent("CONFIG", kv);
            C($"plan={m_plan}");
            C($"pid={m_pid}");
            C($"motion_enabled={m_ctrl.MotionEnabled};autofix={m_autoFixConfig}");
            C($"fixed_window_half_width_deg={m_ctrl.DefaultWindowHalfWidthDeg};el_center_deg={m_windscreenElCenterDeg}");
            C($"nominal_s=A:{k_durA};B:{k_durB};C:{k_durC}");
            if (m_plan == StudyPlan.PassthroughScene_BlockA)
            {
                C($"blockA_order={string.Join(">", System.Array.ConvertAll(k_blockAOrders[m_pid % 4], i => $"A{i + 1}"))}");
                C($"task_option={(m_useCptPanel ? "V_cpt_panel" : "P_paper")};cpt_trials={k_cptTrials}");
            }
            else
            {
                C($"blockB_order={(m_pid % 2 == 0 ? "B1>B2" : "B2>B1")}");
                C($"blockC_order={string.Join(">", System.Array.ConvertAll(k_blockCOrders[m_pid % 3], m => m.ToString()))}");
            }
        }

        // ---- advance state machine ----

        private void OnAdvance()
        {
            switch (m_phase)
            {
                case Phase.Idle:
                    if (m_plan == StudyPlan.PassthroughScene_BlockA)
                    {
                        m_logger.LogEvent("BLOCK_START", "block=A");
                        m_logger.Block = "A";
                        m_ctrl.StudyInputLock = false; // participant paints the window
                        m_phase = Phase.AwaitWindow;
                        Debug.Log("[Sequencer] Participant paints the focus window; SPACE locks it.");
                    }
                    else
                    {
                        m_logger.LogEvent("BLOCK_START", "block=B");
                        m_logger.Block = "B";
                        m_phase = Phase.Ready;
                        Debug.Log($"[Sequencer] SPACE starts {CurrentBConditionName()}.");
                    }
                    break;

                case Phase.AwaitWindow:
                    if (m_ctrl.ActiveRect == new Vector4(-Mathf.PI, Mathf.PI, -Mathf.PI * 0.5f, Mathf.PI * 0.5f))
                    {
                        Debug.LogWarning("[Sequencer] No window painted yet — not locking.");
                        break;
                    }
                    m_ctrl.StudyInputLock = true;
                    m_logger.LogEvent("WINDOW_LOCKED", WindowPayload(m_ctrl.ActiveRect) + ";source=painted");
                    if (m_useCptPanel && m_cptPanel != null && !m_cptPanel.Placed) m_cptPanel.PlaceAtGaze();
                    m_phase = Phase.Ready;
                    Debug.Log($"[Sequencer] Window locked. SPACE starts {CurrentAConditionName()}.");
                    break;

                case Phase.Ready:
                    StartCondition();
                    break;

                case Phase.Gap:
                    if (m_tlxOpen) { Debug.LogWarning("[Sequencer] TLX still open — press T to close first."); break; }
                    AdvanceAfterGap();
                    break;

                case Phase.AwaitC:
                    m_logger.LogEvent("BLOCK_START", "block=C");
                    m_logger.Block = "C";
                    m_condIdx = 0;
                    StartSamplerMode();
                    break;

                case Phase.SamplerGap:
                    m_condIdx++;
                    if (m_condIdx < 3) StartSamplerMode();
                    else FinishBlock("C");
                    break;
            }
        }

        private void AdvanceAfterGap()
        {
            m_condIdx++;
            int total = m_plan == StudyPlan.PassthroughScene_BlockA ? 4 : 2;
            if (m_condIdx < total)
            {
                m_phase = Phase.Ready;
                Debug.Log($"[Sequencer] SPACE starts " +
                    $"{(m_plan == StudyPlan.PassthroughScene_BlockA ? CurrentAConditionName() : CurrentBConditionName())}.");
            }
            else if (m_plan == StudyPlan.VideoScene_BlocksBC)
            {
                m_logger.LogEvent("BLOCK_END", "block=B");
                m_phase = Phase.AwaitC;
                Debug.Log("[Sequencer] Block B done. Comfort check now: SPACE starts Block C, K skips it.");
            }
            else
            {
                FinishBlock("A");
            }
        }

        private void FinishBlock(string label)
        {
            m_logger.LogEvent("BLOCK_END", $"block={label}");
            m_logger.Block = "";
            m_logger.Condition = "";
            m_ctrl.StudyEffectSuppressed = true;
            m_phase = Phase.Done;
            Debug.Log($"[Sequencer] {label} finished. E ends the session (VRSQ/SUS/debrief off-headset).");
        }

        // ---- conditions ----

        private string CurrentAConditionName() => $"A{k_blockAOrders[m_pid % 4][m_condIdx] + 1}";
        private string CurrentBConditionName() => (m_pid % 2 == 0) == (m_condIdx == 0) ? "B1" : "B2";

        private void StartCondition()
        {
            m_condTimer = 0f;

            if (m_plan == StudyPlan.PassthroughScene_BlockA)
            {
                int slot = k_blockAOrders[m_pid % 4][m_condIdx]; // 0..3 = A1..A4
                bool vignetteOn = slot >= 2;
                bool tabletsOn  = slot % 2 == 1;
                string name = $"A{slot + 1}";

                m_ctrl.StudyEffectSuppressed = !vignetteOn;
                m_logger.Condition = name;
                m_logger.LogEvent("CONDITION_START",
                    $"name={name};vignette={(vignetteOn ? "on" : "off")};tablets={(tabletsOn ? "reel" : "black")};nominal_s={k_durA}");
                Debug.Log($"[Sequencer] {name} RUNNING — tablets: {(tabletsOn ? "START REEL (countdown)" : "black")}.");

                if (m_useCptPanel)
                    m_cptPanel.BeginRun(k_cptTrials, seed: m_pid * 10 + slot);
            }
            else
            {
                string name = CurrentBConditionName();
                m_ctrl.StudyEffectSuppressed = name == "B1";
                m_logger.Condition = name;
                m_logger.LogEvent("CONDITION_START",
                    $"name={name};effect={(name == "B1" ? "off" : "colorpop")};nominal_s={k_durB}");
                if (!m_probeScheduler.Begin(name))
                {
                    m_logger.LogEvent("GUARD_FAIL", $"reason=probe_schedule_load_{name}");
                    Debug.LogError($"[Sequencer] Probe schedule for {name} failed to load — condition aborted.");
                    m_phase = Phase.Gap;
                    return;
                }
                Debug.Log($"[Sequencer] {name} RUNNING.");
            }

            m_phase = Phase.Condition;
        }

        private void UpdateCondition()
        {
            m_condTimer += Time.deltaTime;
            float dur = m_plan == StudyPlan.PassthroughScene_BlockA ? k_durA : k_durB;
            if (m_condTimer < dur) return;

            if (m_plan == StudyPlan.PassthroughScene_BlockA)
            {
                if (m_useCptPanel) m_cptPanel.StopRun();
                m_logger.LogEvent("CONDITION_END",
                    $"name={m_logger.Condition};elapsed_s={(int)m_condTimer}");
            }
            else
            {
                m_logger.LogEvent("CONDITION_END",
                    $"name={m_logger.Condition};elapsed_s={(int)m_condTimer};probes_fired={m_probeScheduler.ProbesFired}");
                m_probeScheduler.StopRun();
            }

            m_ctrl.StudyEffectSuppressed = true;
            m_logger.Condition = "";
            m_phase = Phase.Gap;
            Debug.Log("[Sequencer] Condition done. TLX now (T ... T), then SPACE.");
        }

        // ---- Block C sampler ----

        private void StartSamplerMode()
        {
            var mode = k_blockCOrders[m_pid % 3][m_condIdx];
            string name = $"C_{mode.ToString().ToUpperInvariant()}";
            m_ctrl.StudySetMode(mode);
            m_ctrl.StudyEffectSuppressed = false;
            m_condTimer = 0f;
            m_logger.Condition = name;
            m_logger.LogEvent("CONDITION_START", $"name={name};nominal_s={k_durC}");
            m_logger.LogEvent("SAMPLER_MODE_START", $"mode={mode}");
            m_phase = Phase.SamplerMode;
            Debug.Log($"[Sequencer] Sampler: {mode} for {k_durC:0} s.");
        }

        private void UpdateSamplerMode()
        {
            m_condTimer += Time.deltaTime;
            if (m_condTimer < k_durC) return;
            m_logger.LogEvent("SAMPLER_MODE_END", $"mode={k_blockCOrders[m_pid % 3][m_condIdx]}");
            m_logger.LogEvent("CONDITION_END", $"name={m_logger.Condition};elapsed_s={(int)m_condTimer}");
            m_ctrl.StudyEffectSuppressed = true;
            m_logger.Condition = "";
            m_phase = Phase.SamplerGap;
            Debug.Log("[Sequencer] Mode done — verbal Likert ratings now, then SPACE.");
        }

        // ---- practice ----

        private void StartPractice()
        {
            m_phaseBeforePractice = m_phase;
            m_logger.Block = "PRACTICE";

            if (m_plan == StudyPlan.PassthroughScene_BlockA && m_useCptPanel)
            {
                if (!m_cptPanel.Placed) m_cptPanel.PlaceAtGaze();
                m_logger.LogEvent("PRACTICE_START", $"kind=cpt;trials={k_practiceTrials}");
                m_cptPanel.BeginRun(k_practiceTrials, seed: 999);
                m_phase = Phase.Practice;
            }
            else if (m_plan == StudyPlan.VideoScene_BlocksBC)
            {
                m_logger.LogEvent("PRACTICE_START", "kind=probes;count=6");
                m_probeScheduler.Begin(PracticeSchedule());
                m_phase = Phase.Practice;
            }
        }

        private void UpdatePractice()
        {
            if (m_plan == StudyPlan.PassthroughScene_BlockA)
            {
                if (m_cptPanel.Running) return;
                m_logger.LogEvent("PRACTICE_END",
                    $"kind=cpt;go_acc={m_cptPanel.LastRunGoAccuracy:F3};criterion=0.90");
                Debug.Log($"[Sequencer] Practice go-accuracy {m_cptPanel.LastRunGoAccuracy:P0} " +
                          $"(criterion ≥ 90 %; P repeats once if missed).");
            }
            else
            {
                if (!m_probeScheduler.ScheduleExhausted) return;
                m_probeScheduler.StopRun();
                m_logger.LogEvent("PRACTICE_END", "kind=probes;criterion=4_of_6_hits_see_log");
            }
            m_logger.Block = m_plan == StudyPlan.PassthroughScene_BlockA ? "A" : "B";
            m_phase = m_phaseBeforePractice;
        }

        // Six fixed practice probes: generous spacing, both bands, both sides.
        private static ProbeScheduleData PracticeSchedule() => new()
        {
            version = 1, condition = "PRACTICE", seed = 0,
            probes = new[]
            {
                new ProbeDef { id = 901, t = 3f,  az_deg =   0f, el_deg =  2f, band = "central",    duration_ms = 300 },
                new ProbeDef { id = 902, t = 10f, az_deg =  32f, el_deg =  5f, band = "peripheral", duration_ms = 300 },
                new ProbeDef { id = 903, t = 17f, az_deg =  -6f, el_deg = -4f, band = "central",    duration_ms = 300 },
                new ProbeDef { id = 904, t = 24f, az_deg = -30f, el_deg =  8f, band = "peripheral", duration_ms = 300 },
                new ProbeDef { id = 905, t = 31f, az_deg =   5f, el_deg =  6f, band = "central",    duration_ms = 300 },
                new ProbeDef { id = 906, t = 38f, az_deg =  36f, el_deg = -6f, band = "peripheral", duration_ms = 300 },
            },
        };

        // ---- helpers ----

        private static Vector4 DegWindowToRad(Vector4 deg) =>
            new(deg.x * Mathf.Deg2Rad, deg.y * Mathf.Deg2Rad, deg.z * Mathf.Deg2Rad, deg.w * Mathf.Deg2Rad);

        private static string WindowPayload(Vector4 r) =>
            $"azMin={r.x:F4};azMax={r.y:F4};elMin={r.z:F4};elMax={r.w:F4}";
    }
}
