// Low-salience clickable "blob" targets for TestModeSequencer's modes 1 & 2 (Test/TestBlobs
// branch). Randomly selects real traffic-light/stop-sign tracked lifetimes from the current
// baked detection track and places a camouflaged, colour-muted marker exactly where each
// object's box would have been, for exactly as long as that real object is actually on
// screen (plus a short grace period). Aim the LEFT controller at it and pull the left
// trigger — same convention as the existing ClickProbeTest.
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

        [Header("Blob visuals — deliberately low-salience")]
        [SerializeField] private float m_blobDistance = 8f;
        [SerializeField, Range(0.2f, 8f)] private float m_blobSizeDeg = 1.0f;
        [Tooltip("Muted, desaturated, partially transparent tone — meant to blend with typical background rather than pop. Alpha needs the marker material to actually support blending (SelectionDotMat does, same as the existing locked-dot alpha 0.55).")]
        [SerializeField] private Color m_blobColor = new(0.40f, 0.38f, 0.30f, 0.5f);

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

        private GameObject m_blobGO;
        private Material m_blobMat;
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
            if (m_targets != null) foreach (var tg in m_targets) tg.resolved = false;
            m_active = true;
        }

        public void Deactivate()
        {
            m_active = false;
            if (m_blobGO != null) m_blobGO.SetActive(false);
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
                m_minGapSec, m_seed, null);

            var signalWindows = new List<(float tStart, float tEnd)>();
            foreach (var l in signalChosen) signalWindows.Add((l.tStart, l.tEnd));

            var personChosen = SelectTargets(k_personClasses, m_holdSeconds, m_personTargetCount,
                m_personMinLifetimeSec, m_personMinEccentricityDeg, m_personLongLifetimeSec,
                m_personLongFraction, m_personMinGapSec, m_personSeed, signalWindows);

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
            List<(float tStart, float tEnd)> externalWindows)
        {
            var pool = m_video.BuildDetectionLifetimes(classIds, holdSeconds);
            var filtered = new List<VideoTestSceneManager.DetectionLifetime>();
            foreach (var l in pool)
            {
                if (l.tEnd - l.tStart < minLifetimeSec) continue;
                if (ComputeEccentricityDeg(l) < minEccentricityDeg) continue; // stay out of the central/fixated cone
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

            // The just-hit target stays "current" until its window naturally closes — while the
            // hit flash coroutine owns m_blobGO's transform/visibility, don't fight it here.
            if (m_flashCoroutine == null)
            {
                bool visible = current != null && !current.resolved && vt <= current.tEnd; // hidden during the grace tail
                if (visible) ShowBlob(curAzEl); else HideBlob();
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
                        PlayHitFeedback(curAzEl);
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

        private Vector2 InterpolateAzEl(BlobTarget tg, float vt)
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
            Vector4 rect = m_video.DetectionBoxToAzElRect(box);
            return new Vector2((rect.x + rect.y) * 0.5f, (rect.z + rect.w) * 0.5f);
        }

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

        private void ShowBlob(Vector2 azEl)
        {
            if (m_blobGO == null) return;
            Vector3 head = Camera.main != null ? Camera.main.transform.position : transform.position;
            m_blobGO.transform.position = head + Dir(azEl.x, azEl.y) * m_blobDistance;
            float sizeM = 2f * m_blobDistance * Mathf.Tan(0.5f * m_blobSizeDeg * Mathf.Deg2Rad);
            m_blobGO.transform.localScale = Vector3.one * sizeM;
            m_blobGO.SetActive(true);
        }

        private void HideBlob()
        {
            if (m_blobGO != null) m_blobGO.SetActive(false);
        }

        // ---- visuals (built once, parented under this persistent object so they survive scene loads) ----

        private void EnsureVisuals()
        {
            if (m_blobGO == null)
            {
                m_blobGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                m_blobGO.name = "BlobTarget";
                m_blobGO.transform.SetParent(transform, false);
                Destroy(m_blobGO.GetComponent<Collider>());
                m_blobMat = NewMarkerMaterial(m_blobColor);
                m_blobMat.renderQueue = 4150; // above vignette sphere(3000)/dots(4000)/toast(4100)
                m_blobGO.GetComponent<MeshRenderer>().sharedMaterial = m_blobMat;
                m_blobGO.SetActive(false);
            }
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
                // On this component's own (always-active) GameObject, not m_blobGO — m_blobGO
                // toggles active/inactive constantly, which would silently drop PlayOneShot calls.
                m_audioSource = gameObject.AddComponent<AudioSource>();
                m_audioSource.playOnAwake = false;
                m_audioSource.spatialBlend = 0f; // 2D — a confirmation cue, not a locatable sound
                m_audioSource.volume = 0.6f;
            }
        }

        // ---- hit feedback: brief flash + synthesized beep (no audio asset needed) ----

        private void PlayHitFeedback(Vector2 azEl)
        {
            if (m_flashCoroutine != null) StopCoroutine(m_flashCoroutine);
            m_flashCoroutine = StartCoroutine(HitFlash(azEl));
            if (m_hitBeep && m_audioSource != null) m_audioSource.PlayOneShot(GetBeepClip());
        }

        private IEnumerator HitFlash(Vector2 azEl)
        {
            if (m_blobGO == null) yield break;
            Vector3 head = Camera.main != null ? Camera.main.transform.position : transform.position;
            m_blobGO.transform.position = head + Dir(azEl.x, azEl.y) * m_blobDistance;
            float baseSizeM = 2f * m_blobDistance * Mathf.Tan(0.5f * m_blobSizeDeg * Mathf.Deg2Rad);
            m_blobGO.transform.localScale = Vector3.one * baseSizeM * m_hitFlashScale;
            m_blobMat.color = m_hitFlashColor;
            m_blobGO.SetActive(true);

            yield return new WaitForSeconds(m_hitFlashSeconds);

            m_blobGO.SetActive(false);
            m_blobMat.color = m_blobColor; // restore normal appearance for the next target
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
            if (m_blobGO != null) Destroy(m_blobGO);
            if (m_blobMat != null) Destroy(m_blobMat);
            if (m_reticleGO != null) Destroy(m_reticleGO);
            if (m_reticleMat != null) Destroy(m_reticleMat);
        }
    }
}
