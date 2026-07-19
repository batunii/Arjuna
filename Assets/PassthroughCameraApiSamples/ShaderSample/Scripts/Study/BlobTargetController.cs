// Low-salience clickable "blob" targets for TestModeSequencer's modes 1 & 2 (Test/TestBlobs
// branch). Randomly selects real traffic-light/stop-sign tracked lifetimes from the current
// baked detection track and marks each, for exactly as long as that real object is actually on
// screen (plus a short grace period), exactly where its box would have been. Aim the LEFT
// controller at it and pull the left trigger — same convention as the existing ClickProbeTest.
//
// Route B rendering (2026-07-18): the target is NOT a world-space object. Each frame this pushes
// the active target's az/el + radius + ramped strength to the vignette shader (via
// VideoTestSceneManager.SetBlobProbe), which composites it as a LOCAL modulation of the real
// scene pixels — a soft desaturation + gentle dim, so it reads as a natural haze/smudge when
// foveated and vanishes pre-attentively, never a foreign overlaid object. Because it modulates
// the already-filtered colour, a target in a defocused/dimmed area is filtered too (supervisor
// Point 3): on the blacked-out Hard-Dark periphery it simply disappears, like a real object there.
// See Dissertation/probe-target-design.md.
//
// The same random selection (seeded) is generated once per app run and reused for every
// mode-1/mode-2 activation and every video loop, so mode 1 (SignPop on) and mode 2 (no
// filter) are a fair paired comparison; only the hit/miss/reaction-time OUTCOME is reset
// per activation, tagged with the mode label, so results are recorded separately per mode.
//
// Deliberately standalone from StudyLogger (same reasoning as TestModeSequencer) — logs to
// its own CSV via a tiny inline writer, not the real study's contracted schema.
//
// Tunables below (min/long lifetime floors, hit-angle tolerance, grace window) are best-guess
// defaults reasoned from the real lifetime distribution in the current bake (median ~0s, but
// 9% of tracks exceed 2s) — pilot this yourself and retune, the same way the real study
// calibrates probe size/timing empirically rather than by theory alone.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace PassthroughCameraSamples.ShaderSample.Study
{
    public class BlobTargetController : MonoBehaviour
    {
        [SerializeField] private VideoTestSceneManager m_video;

        [Header("Selection (generated once per app run, seeded)")]
        [SerializeField] private int m_targetCount = 10;
        [Tooltip("Floor applied to EVERY candidate — excludes near-instant single-sample blips that would be physically unclickable regardless of skill.")]
        [SerializeField, Range(0.1f, 3f)] private float m_minLifetimeSec = 0.75f;
        [Tooltip("A candidate counts as \"long\" if its real on-screen lifetime exceeds this.")]
        [SerializeField, Range(1f, 10f)] private float m_longLifetimeSec = 2.0f;
        [Tooltip("At least this fraction of the selected targets must be \"long\" (ceil-rounded).")]
        [SerializeField, Range(0f, 1f)] private float m_longFraction = 0.5f;
        [Tooltip("Minimum gap enforced between two selected targets' time windows, so at most one is ever active at once.")]
        [SerializeField, Range(0f, 5f)] private float m_minGapSec = 1f;
        [SerializeField] private int m_seed = 12345;
        [Tooltip("Same meaning as VideoTestSceneManager's m_signDetHoldSec — how long a lifetime tolerates a missed sample before closing.")]
        [SerializeField, Range(0f, 2f)] private float m_holdSeconds = 0.5f;
        [Tooltip("Minimum angular eccentricity (deg) from video-forward (az=0, el=0) required to qualify — keeps selection out of the central cone a driver naturally fixates, into the peripheral \"easy to miss\" region. The real study's own peripheral probe band starts at 25°; 20° default leaves a larger candidate pool.")]
        [SerializeField, Range(0f, 60f)] private float m_minEccentricityDeg = 20f;

        [Header("Person-target selection (separate pool, generated once)")]
        [Tooltip("A second, independent target pool built from person (COCO class 0) lifetimes — tests whether the DR filter diverts attention to peripheral pedestrians a driver would otherwise miss, not just traffic lights/signs. Selected the same way as the signal pool (seeded shuffle + long/short quota + overlap avoidance), then merged into one time-sorted target list so only one target of either kind is ever active at once.")]
        [SerializeField] private int m_personTargetCount = 10;
        [Tooltip("Lower than the signal pool's floor — pedestrians crossing frame likely have shorter clean on-screen windows than a static light. Best-guess default, not yet measured against the actual person-lifetime distribution — retune once inspected, same as this file's other tunables.")]
        [SerializeField, Range(0.1f, 3f)] private float m_personMinLifetimeSec = 0.5f;
        [SerializeField, Range(1f, 10f)] private float m_personLongLifetimeSec = 1.5f;
        [SerializeField, Range(0f, 1f)] private float m_personLongFraction = 0.5f;
        [SerializeField, Range(0f, 5f)] private float m_personMinGapSec = 1f;
        [SerializeField] private int m_personSeed = 54321; // distinct from m_seed — independent draw, not correlated
        [Tooltip("Same rationale as the signal pool's eccentricity floor — keep person targets peripheral, out of the cone a driver naturally fixates.")]
        [SerializeField, Range(0f, 60f)] private float m_personMinEccentricityDeg = 20f;
        [Tooltip("Person candidates must have at least this predicted effect strength (VideoTestSceneManager.PersonPredictedStrength at the lifetime midpoint, against the locked window) — otherwise a blob lands on a person the runtime will never visibly highlight and the mode-1-vs-2 comparison degenerates. Requires the window to be locked before Activate (TestModeSequencer does this).")]
        [SerializeField, Range(0f, 1f)] private float m_personMinPredictedStrength = 0.7f;

        public enum ProbeStyle { Desaturate = 0, Halo = 1, Bubble = 2, LocalContrastRing = 3 }

        [Header("Blob probe — scene-pixel modulation (Route B: composited in the vignette shader)")]
        [Tooltip("Probe visual style. Desaturate = colour-loss smudge (blends most; can look like the YOLO window). Halo = soft bright rim. Bubble = glassy droplet (refracts scene); most natural but content-DEPENDENT. LocalContrastRing = a thin, fixed-colour translucent ring that fades in and holds steady — no pulsing/shimmer. Colour content-dependence is handled by counterbalancing + screening, not per-pixel. Recommended for the measured study.")]
        [SerializeField] private ProbeStyle m_probeStyle = ProbeStyle.LocalContrastRing;
        [Tooltip("Rim intensity. For Halo/Bubble = glass-edge brightness. For LocalContrastRing = the luminance CONTRAST STEP the ring holds vs its local surround (the content-independence knob) — 0.4-0.6 is a clear-but-not-blaring ring; 0.9 is very strong.")]
        [SerializeField, Range(0f, 1.5f)] private float m_blobRim = 0.5f;
        [Tooltip("(Ring) Ring colour (RGB) and its persistent alpha (A). Low-but-present alpha = a translucent ring that fades in and holds, without drawing attention. Fixed colour (no scene-coupled flip), so it never pulses/shimmers.")]
        [SerializeField] private Color m_blobRingColor = new(1f, 0.85f, 0.1f, 0.4f); // yellow, low persistent alpha
        [Tooltip("(Ring) Thickness — Gaussian sigma in normalized radius. Smaller = thinner ring.")]
        [SerializeField, Range(0.02f, 0.2f)] private float m_blobRingWidth = 0.05f;
        [Tooltip("(Bubble) Refraction/magnification of the droplet lens, in angular space. 0.45 ~ 1.8x magnification at the core; raise for a stronger lens, lower toward 0.25 if it warps too much.")]
        [SerializeField, Range(0f, 0.7f)] private float m_blobLens = 0.45f;
        [Tooltip("OFF (default) = every probe is the same fixed angular size (m_blobSizeDeg), position still tracks the object. ON = size matches the real detection box (locked per appearance). You asked for fixed size, so this is off.")]
        [SerializeField] private bool m_boxMatchSize = false;
        [Tooltip("The fixed angular size of every probe (used when box-matching is off). This is the uniform-size knob.")]
        [SerializeField, Range(0.2f, 8f)] private float m_blobSizeDeg = 1.6f;
        [SerializeField, Range(0.2f, 4f)] private float m_blobMinSizeDeg = 1.2f;
        [SerializeField, Range(0.5f, 6f)] private float m_blobMaxSizeDeg = 2.5f;
        [Tooltip("Local DESATURATION at the probe core (0..1). The probe reads as a faint colour-loss / haze on the real scene pixels — texture fully preserved, no overlaid object. Primary detection cue; content-dependent (invisible over already-grey areas, hence paired with a small dim).")]
        [SerializeField, Range(0f, 1f)] private float m_blobDesat = 0.7f;
        [Tooltip("Gentle luminance DIP at the probe core (0..1). Kept small so it stays natural (like a smudge) over any content, including grey road. Titrate this + desat against the ceiling to land the ~60-85% no-filter hit rate.")]
        [SerializeField, Range(0f, 0.5f)] private float m_blobDim = 0.08f;
        [Tooltip("Gaussian falloff sigma as a fraction of the blob RADIUS — soft rim, no hard edge (removes the pre-attentive silhouette cue).")]
        [SerializeField, Range(0.2f, 1f)] private float m_blobSigmaFrac = 0.5f;
        [Tooltip("Onset ramp (seconds): the probe fades in from its lifetime start instead of appearing abruptly. A gradual, non-transient onset avoids the exogenous capture an abrupt onset triggers (Yantis & Jonides; Simons; Cole). Static once ramped — no pulsing/looming.")]
        [SerializeField, Range(0f, 2f)] private float m_onsetRampSeconds = 0.5f;

        [Header("DEBUG — confirm-it-works (turn OFF for the real study probe)")]
        [Tooltip("TEMPORARY: renders each probe as a big, solid, bright patch (m_debugColor) at full strength, no onset ramp, ignoring the chosen style. Use it to confirm the probe appears at the right place/time; then turn OFF to get the real probe style.")]
        [SerializeField] private bool m_debugObviousBlob = false;
        [SerializeField, Range(1f, 12f)] private float m_debugBlobSizeDeg = 4f;
        [SerializeField] private Color m_debugColor = new(1f, 0f, 1f, 1f); // magenta — never occurs naturally in the scene

        [Header("Pointer (left controller, matches ClickProbeTest)")]
        [SerializeField] private bool m_showPointerReticle = true;
        [SerializeField] private float m_reticleDistance = 6f;
        [SerializeField] private float m_reticleSize = 0.05f;
        [SerializeField] private Color m_reticleColor = new(0.2f, 0.9f, 1f, 0.6f);

        [Tooltip("Unlit material template for both the blob and the reticle (Shader.Find is stripped on Android) — reuse SelectionDotMat.")]
        [SerializeField] private Material m_markerMaterialTemplate;

        [Header("Hit detection")]
        [SerializeField] private OVRInput.RawButton m_clickButton = OVRInput.RawButton.LIndexTrigger;
        [Tooltip("Angular distance (deg) between the left-controller aim and the target's current direction, within which a trigger pull counts as a hit.")]
        [SerializeField, Range(1f, 30f)] private float m_hitAngleToleranceDeg = 10f;
        [Tooltip("After a target's real on-screen lifetime ends, a correctly-aimed press still counts as a hit for this long (aimed at its last known position) — the object is hidden during this window, not frozen visibly in place.")]
        [SerializeField, Range(0f, 3f)] private float m_hitGraceSec = 0.75f;

        [Header("Hit feedback")]
        [SerializeField] private Color m_hitFlashColor = new(0.3f, 1f, 0.4f, 0.9f);
        [SerializeField, Range(0.05f, 1f)] private float m_hitFlashSeconds = 0.25f;
        [SerializeField, Range(1f, 2f)] private float m_hitFlashScale = 1.6f;
        [SerializeField] private bool m_hitBeep = true;
        [SerializeField, Range(200f, 2000f)] private float m_hitBeepFreqHz = 880f;
        [SerializeField, Range(0.05f, 0.5f)] private float m_hitBeepSeconds = 0.12f;

        private class BlobTarget
        {
            public int id;
            public int cls; // COCO class id (9/11 = signal, 0 = person) — internal/logging only, no visual difference
            public float tStart, tEnd;
            public List<(float t, Vector4 box)> samples;
            public bool resolved;
        }

        private bool m_active;
        private bool m_generated;
        private List<BlobTarget> m_targets;
        private int m_ptr;
        private float m_lastVt = -1f;
        private string m_modeLabel = "";
        private BlobTarget m_sizeLockedFor;   // ring size is locked once per target appearance...
        private float m_lockedSizeDeg = 1f;   // ...so it never grows/shrinks with the moving box

        private GameObject m_reticleGO;
        private Material m_reticleMat;
        private Coroutine m_flashCoroutine;
        private AudioSource m_audioSource;
        private AudioClip m_beepClip;

        private StreamWriter m_log;
        private const string k_logHeader =
            "t_ms,mode,target_id,target_kind,t_start,t_end,duration_s,outcome,rt_s,angle_deg";

        private static string KindLabel(int cls) => cls switch
        {
            9 => "traffic_light",
            11 => "stop_sign",
            0 => "person",
            _ => "unknown",
        };

        // ---- external API (TestModeSequencer) ----

        public void Activate(string modeLabel)
        {
            m_modeLabel = modeLabel;
            if (m_video == null) m_video = FindObjectOfType<VideoTestSceneManager>();
            EnsureGenerated();
            EnsureVisuals();
            m_ptr = 0;
            m_lastVt = -1f;
            m_sizeLockedFor = null; // recompute the locked ring size for the first target of this run
            if (m_targets != null) foreach (var tg in m_targets) tg.resolved = false;
            m_active = true;
        }

        public void Deactivate()
        {
            m_active = false;
            if (m_flashCoroutine != null) { StopCoroutine(m_flashCoroutine); m_flashCoroutine = null; }
            if (m_video != null) m_video.SetBlobProbe(false, Vector2.zero, 0f, 0f, 0f);
            if (m_reticleGO != null) m_reticleGO.SetActive(false);
        }

        // ---- selection (once per app run) ----

        private static readonly HashSet<int> k_signalClasses = new() { 9, 11 }; // traffic light, stop sign
        private static readonly HashSet<int> k_personClasses = new() { 0 };     // person

        private void EnsureGenerated()
        {
            if (m_generated) return;
            m_generated = true;
            if (m_video == null)
            {
                Debug.LogError("[BlobTargetController] No VideoTestSceneManager found — can't generate targets.");
                return;
            }

            // Signal pool first (unchanged selection behaviour/order), then the person pool —
            // passed the signal pool's chosen windows so a person target can never time-overlap
            // a signal target (only one target of either kind is ever active at once).
            var signalChosen = SelectTargets(k_signalClasses, m_holdSeconds, m_targetCount,
                m_minLifetimeSec, m_minEccentricityDeg, m_longLifetimeSec, m_longFraction,
                m_minGapSec, m_seed, null, 0f, 0f);

            var signalWindows = new List<(float tStart, float tEnd)>();
            foreach (var l in signalChosen) signalWindows.Add((l.tStart, l.tEnd));

            // Person candidates must also satisfy the runtime person gate's "close" criterion
            // (apparent box height) — otherwise a blob forms on a distant pedestrian that
            // PassesPersonGate will never highlight, and mode 1 vs mode 2 compares nothing.
            var personChosen = SelectTargets(k_personClasses, m_holdSeconds, m_personTargetCount,
                m_personMinLifetimeSec, m_personMinEccentricityDeg, m_personLongLifetimeSec,
                m_personLongFraction, m_personMinGapSec, m_personSeed, signalWindows,
                m_video.PersonMinBoxHeightDeg, m_personMinPredictedStrength);

            var merged = new List<VideoTestSceneManager.DetectionLifetime>();
            merged.AddRange(signalChosen);
            merged.AddRange(personChosen);
            merged.Sort((a, b) => a.tStart.CompareTo(b.tStart));

            m_targets = new List<BlobTarget>();
            for (int k = 0; k < merged.Count; k++)
            {
                var l = merged[k];
                m_targets.Add(new BlobTarget { id = k, cls = l.cls, tStart = l.tStart, tEnd = l.tEnd, samples = l.samples });
            }

            string summary = "";
            foreach (var tg in m_targets) summary += $"[{tg.id}:{KindLabel(tg.cls)}] t={tg.tStart:F1}-{tg.tEnd:F1}s ";
            Debug.Log($"[BlobTargetController] Generated {m_targets.Count} targets " +
                      $"({signalChosen.Count} signal, {personChosen.Count} person; " +
                      $"seeds {m_seed}/{m_personSeed}): {summary}");
        }

        /// <summary>Shared selection algorithm (filter by lifetime/eccentricity, seeded shuffle,
        /// two-pass long/short quota fill with overlap avoidance) parameterized so it can build
        /// an independent pool for any class set. <paramref name="externalWindows"/> lets a later
        /// pool's picks avoid overlapping an earlier pool's picks too.</summary>
        private List<VideoTestSceneManager.DetectionLifetime> SelectTargets(
            HashSet<int> classIds, float holdSeconds, int targetCount,
            float minLifetimeSec, float minEccentricityDeg,
            float longLifetimeSec, float longFraction, float minGapSec, int seed,
            List<(float tStart, float tEnd)> externalWindows, float minBoxHeightDeg,
            float minPredictedStrength)
        {
            var pool = m_video.BuildDetectionLifetimes(classIds, holdSeconds);
            var filtered = new List<VideoTestSceneManager.DetectionLifetime>();
            foreach (var l in pool)
            {
                if (l.tEnd - l.tStart < minLifetimeSec) continue;
                if (ComputeEccentricityDeg(l) < minEccentricityDeg) continue; // stay out of the central/fixated cone
                if (minBoxHeightDeg > 0f && ComputeBoxHeightDeg(l) < minBoxHeightDeg) continue; // runtime gate's "close" proxy
                // Runtime-visibility alignment: only pick targets the effect will actually
                // show clearly (predicted strength vs the locked window at mid-life).
                if (minPredictedStrength > 0f &&
                    m_video.PersonPredictedStrength(RepresentativeAzElRect(l)) < minPredictedStrength) continue;
                filtered.Add(l);
            }

            var order = new List<int>();
            for (int i = 0; i < filtered.Count; i++) order.Add(i);
            Shuffle(order, new System.Random(seed));

            var chosenIdx = new List<int>();
            int minLong = Mathf.CeilToInt(targetCount * longFraction);

            foreach (int i in order)
            {
                if (chosenIdx.Count >= minLong) break;
                var l = filtered[i];
                if (l.tEnd - l.tStart <= longLifetimeSec) continue;
                if (OverlapsAny(filtered, chosenIdx, l, minGapSec, externalWindows)) continue;
                chosenIdx.Add(i);
            }
            if (chosenIdx.Count < minLong)
                Debug.LogWarning($"[BlobTargetController] Only found {chosenIdx.Count}/{minLong} long-lived " +
                                  $"(>{longLifetimeSec}s) non-overlapping candidates for class set " +
                                  $"{{{string.Join(",", classIds)}}}.");

            foreach (int i in order)
            {
                if (chosenIdx.Count >= targetCount) break;
                if (chosenIdx.Contains(i)) continue;
                var l = filtered[i];
                if (OverlapsAny(filtered, chosenIdx, l, minGapSec, externalWindows)) continue;
                chosenIdx.Add(i);
            }
            if (chosenIdx.Count < targetCount)
                Debug.LogWarning($"[BlobTargetController] Only generated {chosenIdx.Count}/{targetCount} " +
                                  $"non-overlapping targets for class set {{{string.Join(",", classIds)}}}.");

            chosenIdx.Sort((a, b) => filtered[a].tStart.CompareTo(filtered[b].tStart));

            var result = new List<VideoTestSceneManager.DetectionLifetime>();
            foreach (int i in chosenIdx) result.Add(filtered[i]);
            return result;
        }

        private bool OverlapsAny(List<VideoTestSceneManager.DetectionLifetime> filtered, List<int> chosenIdx,
            VideoTestSceneManager.DetectionLifetime candidate, float minGapSec,
            List<(float tStart, float tEnd)> externalWindows)
        {
            foreach (int i in chosenIdx)
            {
                var c = filtered[i];
                if (candidate.tStart < c.tEnd + minGapSec && c.tStart < candidate.tEnd + minGapSec)
                    return true;
            }
            if (externalWindows != null)
            {
                foreach (var w in externalWindows)
                {
                    if (candidate.tStart < w.tEnd + minGapSec && w.tStart < candidate.tEnd + minGapSec)
                        return true;
                }
            }
            return false;
        }

        /// <summary>Angular distance (deg) from video-forward (az=0, el=0) to this candidate's
        /// position at its lifetime's temporal midpoint — a representative "how far off to the
        /// side was this, typically" figure, since the object's angle drifts as the vehicle
        /// approaches it.</summary>
        private float ComputeEccentricityDeg(VideoTestSceneManager.DetectionLifetime l)
        {
            float tMid = (l.tStart + l.tEnd) * 0.5f;
            var best = l.samples[0];
            float bestDist = Mathf.Abs(best.t - tMid);
            foreach (var s in l.samples)
            {
                float d = Mathf.Abs(s.t - tMid);
                if (d < bestDist) { bestDist = d; best = s; }
            }
            Vector4 rect = m_video.DetectionBoxToAzElRect(best.box);
            float az = (rect.x + rect.y) * 0.5f, el = (rect.z + rect.w) * 0.5f;
            return Vector3.Angle(Dir(0f, 0f), Dir(az, el));
        }

        /// <summary>Az/el rect at the lifetime's temporal-midpoint sample — the same
        /// representative-sample convention as ComputeEccentricityDeg.</summary>
        private Vector4 RepresentativeAzElRect(VideoTestSceneManager.DetectionLifetime l)
        {
            float tMid = (l.tStart + l.tEnd) * 0.5f;
            var best = l.samples[0];
            float bestDist = Mathf.Abs(best.t - tMid);
            foreach (var s in l.samples)
            {
                float d = Mathf.Abs(s.t - tMid);
                if (d < bestDist) { bestDist = d; best = s; }
            }
            return m_video.DetectionBoxToAzElRect(best.box);
        }

        /// <summary>Apparent box height (deg) at the representative sample — matches the
        /// box-height "closeness" proxy PassesPersonGate applies per-frame at runtime.</summary>
        private float ComputeBoxHeightDeg(VideoTestSceneManager.DetectionLifetime l)
        {
            Vector4 rect = RepresentativeAzElRect(l);
            return (rect.w - rect.z) * Mathf.Rad2Deg;
        }

        private static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // ---- per-frame ----

        private void Update()
        {
            if (!m_active || m_targets == null || m_video == null) return;

            float vt = m_video.VideoTime;
            if (vt < 0f) return;

            if (m_lastVt >= 0f && vt < m_lastVt - 1f)
            {
                // Video looped — start a fresh pass through the same target set.
                m_ptr = 0;
                foreach (var tg in m_targets) tg.resolved = false;
            }
            m_lastVt = vt;

            UpdatePointer();

            // Push the probe shape/amount every frame (not just on Activate) so it can't be lost to
            // init ordering and so Inspector tweaks apply live. Cheap (a few SetFloat calls).
            // In debug mode the bright m_debugColor is fed through the flash-tint channel.
            m_video.SetBlobProbeStatics((int)m_probeStyle, m_blobSigmaFrac, m_blobDesat, m_blobDim,
                                        m_blobRim, m_blobLens, m_blobRingColor, m_blobRingWidth,
                                        m_debugObviousBlob ? m_debugColor : m_hitFlashColor);

            while (m_ptr < m_targets.Count && vt > m_targets[m_ptr].tEnd + m_hitGraceSec)
            {
                var tg = m_targets[m_ptr];
                if (!tg.resolved) { tg.resolved = true; LogOutcome(tg, "miss", -1f, -1f); }
                m_ptr++;
            }

            BlobTarget current = (m_ptr < m_targets.Count && vt >= m_targets[m_ptr].tStart) ? m_targets[m_ptr] : null;
            Vector2 curAzEl = default;
            bool haveAzEl = false;
            if (current != null)
            {
                curAzEl = InterpolateAzEl(current, vt);
                haveAzEl = true;
            }

            // Lock the ring SIZE once per target appearance — position still tracks the object, but
            // the size is fixed for the target's whole lifetime (sized from the lifetime's midpoint
            // box). Per-frame box-matching made the ring grow/shrink with the approaching object and
            // jitter with YOLO's box; locking removes that "breathing".
            if (current != null && !ReferenceEquals(current, m_sizeLockedFor))
            {
                m_lockedSizeDeg = m_boxMatchSize
                    ? Mathf.Clamp(RectMaxAngularDeg(InterpolateAzElRect(current, (current.tStart + current.tEnd) * 0.5f)),
                                  m_blobMinSizeDeg, m_blobMaxSizeDeg)
                    : m_blobSizeDeg;
                m_sizeLockedFor = current;
            }

            // The just-hit target stays "current" until its window naturally closes — while the
            // hit-flash coroutine owns the shader probe, don't fight it here.
            if (m_flashCoroutine == null)
            {
                bool visible = current != null && !current.resolved && vt <= current.tEnd; // hidden during the grace tail
                if (visible)
                {
                    // Onset ramp: fade in over the first m_onsetRampSeconds of the target's life
                    // (keyed to video time, so mode 1 and mode 2 ramp identically).
                    float ramp = m_onsetRampSeconds > 0f
                        ? Mathf.Clamp01((vt - current.tStart) / m_onsetRampSeconds)
                        : 1f;
                    if (m_debugObviousBlob)
                    {
                        // Big, full-strength, solid bright patch (flash channel = m_debugColor at 1)
                        // so you can unambiguously confirm placement/timing.
                        m_video.SetBlobProbe(true, curAzEl, 0.5f * m_debugBlobSizeDeg * Mathf.Deg2Rad, 1f, 1f);
                    }
                    else
                    {
                        m_video.SetBlobProbe(true, curAzEl, 0.5f * m_lockedSizeDeg * Mathf.Deg2Rad, ramp, 0f);
                    }
                }
                else m_video.SetBlobProbe(false, curAzEl, 0f, 0f, 0f);
            }

            if (OVRInput.GetDown(m_clickButton))
            {
                m_video.GetLeftControllerAzEl(out float az, out float el);
                if (current != null && haveAzEl)
                {
                    float angDist = AngularDistanceDeg(az, el, curAzEl.x, curAzEl.y);
                    if (angDist <= m_hitAngleToleranceDeg)
                    {
                        current.resolved = true;
                        float rt = vt - current.tStart;
                        LogOutcome(current, "hit", rt, angDist);
                        PlayHitFeedback(curAzEl, 0.5f * m_lockedSizeDeg * Mathf.Deg2Rad);
                    }
                    else
                    {
                        LogOutcome(current, "bad_aim_attempt", -1f, angDist);
                    }
                }
                else
                {
                    LogOutcome(null, "false_alarm", -1f, -1f);
                }
            }
        }

        private Vector4 InterpolateAzElRect(BlobTarget tg, float vt)
        {
            var samples = tg.samples;
            (float t, Vector4 box) s0 = samples[0], s1 = samples[samples.Count - 1];
            for (int i = 0; i < samples.Count - 1; i++)
            {
                if (vt >= samples[i].t && vt <= samples[i + 1].t)
                {
                    s0 = samples[i]; s1 = samples[i + 1];
                    break;
                }
            }
            float frac = s1.t > s0.t ? Mathf.Clamp01((vt - s0.t) / (s1.t - s0.t)) : 0f;
            Vector4 box = Vector4.Lerp(s0.box, s1.box, frac);
            return m_video.DetectionBoxToAzElRect(box);
        }

        private Vector2 InterpolateAzEl(BlobTarget tg, float vt)
        {
            Vector4 rect = InterpolateAzElRect(tg, vt);
            return new Vector2((rect.x + rect.y) * 0.5f, (rect.z + rect.w) * 0.5f);
        }

        // Angular size (deg) of an az/el rect — the larger of its width/height, used to
        // box-match the blob to the real detection it stands in for.
        private static float RectMaxAngularDeg(Vector4 rect) =>
            Mathf.Max(rect.y - rect.x, rect.w - rect.z) * Mathf.Rad2Deg;

        private static float AngularDistanceDeg(float az1, float el1, float az2, float el2) =>
            Vector3.Angle(Dir(az1, el1), Dir(az2, el2));

        private static Vector3 Dir(float az, float el)
        {
            float cosEl = Mathf.Cos(el);
            return new Vector3(Mathf.Sin(az) * cosEl, Mathf.Sin(el), Mathf.Cos(az) * cosEl);
        }

        private void UpdatePointer()
        {
            if (!m_showPointerReticle || m_reticleGO == null || m_video == null) return;
            m_video.GetLeftControllerAzEl(out float az, out float el);
            Vector3 head = Camera.main != null ? Camera.main.transform.position : transform.position;
            m_reticleGO.transform.position = head + Dir(az, el) * m_reticleDistance;
        }

        // ---- visuals (built once, parented under this persistent object so they survive scene loads) ----
        // Route B: the blob target itself has NO GameObject — it's a scene-pixel modulation composited
        // in the vignette shader (see VideoTestSceneManager.SetBlobProbe). Only the aim reticle is a
        // world-space object here.

        private void EnsureVisuals()
        {
            if (m_reticleGO == null)
            {
                m_reticleGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                m_reticleGO.name = "BlobPointerReticle";
                m_reticleGO.transform.SetParent(transform, false);
                Destroy(m_reticleGO.GetComponent<Collider>());
                m_reticleMat = NewMarkerMaterial(m_reticleColor);
                m_reticleMat.renderQueue = 4200; // matches ClickProbeTest's reticle
                m_reticleGO.GetComponent<MeshRenderer>().sharedMaterial = m_reticleMat;
                m_reticleGO.transform.localScale = Vector3.one * m_reticleSize;
                m_reticleGO.SetActive(m_showPointerReticle);
            }
            if (m_audioSource == null)
            {
                // On this component's own (always-active) GameObject so PlayOneShot never drops.
                m_audioSource = gameObject.AddComponent<AudioSource>();
                m_audioSource.playOnAwake = false;
                m_audioSource.spatialBlend = 0f; // 2D — a confirmation cue, not a locatable sound
                m_audioSource.volume = 0.6f;
            }
        }

        // ---- hit feedback: brief flash + synthesized beep (no audio asset needed) ----

        private void PlayHitFeedback(Vector2 azEl, float radiusRad)
        {
            if (m_flashCoroutine != null) StopCoroutine(m_flashCoroutine);
            m_flashCoroutine = StartCoroutine(HitFlash(azEl, radiusRad));
            if (m_hitBeep && m_audioSource != null) m_audioSource.PlayOneShot(GetBeepClip());
        }

        // Brief green flash at the hit location, driven through the shader probe (flash 1->0 over
        // m_hitFlashSeconds). The colour comes from m_hitFlashColor (pushed as the flash tint in
        // Activate); here we just fade the flash mix and hold the probe at the last position.
        private IEnumerator HitFlash(Vector2 azEl, float radiusRad)
        {
            float flashRad = radiusRad * m_hitFlashScale;
            float t = 0f;
            while (t < m_hitFlashSeconds)
            {
                t += Time.deltaTime;
                float f = Mathf.Clamp01(1f - t / Mathf.Max(m_hitFlashSeconds, 1e-4f));
                if (m_video != null) m_video.SetBlobProbe(true, azEl, flashRad, 1f, f);
                yield return null;
            }
            if (m_video != null) m_video.SetBlobProbe(false, azEl, radiusRad, 0f, 0f);
            m_flashCoroutine = null;
        }

        private AudioClip GetBeepClip()
        {
            if (m_beepClip == null) m_beepClip = MakeBeepClip(m_hitBeepFreqHz, m_hitBeepSeconds);
            return m_beepClip;
        }

        private static AudioClip MakeBeepClip(float freqHz, float durationSec)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(durationSec * sampleRate));
            var clip = AudioClip.Create("BlobHitBeep", sampleCount, 1, sampleRate, false);
            var data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - (float)i / sampleCount; // linear fade-out avoids an end-click
                data[i] = Mathf.Sin(2f * Mathf.PI * freqHz * t) * envelope * 0.5f;
            }
            clip.SetData(data, 0);
            return clip;
        }

        private Material NewMarkerMaterial(Color color)
        {
            if (m_markerMaterialTemplate != null)
            {
                var mat = new Material(m_markerMaterialTemplate);
                mat.color = color;
                return mat;
            }

            // Editor-only fallback (breaks in Android builds — assign the template for device
            // builds). Standard defaults to Opaque, which would ignore our alpha entirely, so
            // switch it to Fade so a low-alpha blob actually renders as translucent.
            Debug.LogWarning("[BlobTargetController] No marker material template — Shader.Find fallback breaks in Android builds.");
            var fallback = new Material(Shader.Find("Standard"));
            fallback.SetFloat("_Mode", 2f); // Fade
            fallback.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            fallback.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            fallback.SetInt("_ZWrite", 0);
            fallback.DisableKeyword("_ALPHATEST_ON");
            fallback.EnableKeyword("_ALPHABLEND_ON");
            fallback.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            fallback.color = color;
            return fallback;
        }

        // ---- logging (own small CSV, deliberately not StudyLogger — see file header) ----

        private void OpenLogIfNeeded()
        {
            if (m_log != null) return;
            string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string path = Path.Combine(Application.persistentDataPath, $"blobtargets_{stamp}.csv");
            m_log = new StreamWriter(path, false, new UTF8Encoding(false));
            m_log.WriteLine(k_logHeader);
            Debug.Log($"[BlobTargetController] Logging to {path}");
        }

        private void LogOutcome(BlobTarget tg, string outcome, float rt, float angleDeg)
        {
            OpenLogIfNeeded();
            long tMs = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string row = tg != null
                ? string.Join(",", tMs, m_modeLabel, tg.id, KindLabel(tg.cls), F(tg.tStart), F(tg.tEnd), F(tg.tEnd - tg.tStart), outcome, F(rt), F(angleDeg))
                : string.Join(",", tMs, m_modeLabel, -1, "", "", "", "", outcome, F(rt), F(angleDeg));
            m_log.WriteLine(row);
            m_log.Flush();
        }

        private static string F(float v) => v < 0f ? "" : v.ToString("F3", CultureInfo.InvariantCulture);

        private void OnDestroy()
        {
            m_log?.Flush();
            m_log?.Dispose();
            if (m_video != null) m_video.SetBlobProbe(false, Vector2.zero, 0f, 0f, 0f);
            if (m_reticleGO != null) Destroy(m_reticleGO);
            if (m_reticleMat != null) Destroy(m_reticleMat);
        }
    }
}
