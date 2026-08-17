// Point-authoring tool: hand-curate the pool of clickable target points for the driving study.
//
// Aim either controller at a real object in the playing 360 video and pull its trigger. The tool
// matches the aim plus the current video time to the nearest baked YOLO detection lifetime and
// records that detection's whole on-screen span as a point, with the fields the later
// set-splitting step needs (class, start/end/duration, representative az/el, eccentricity, box
// size). Points go to authored_points_<stamp>.csv in persistentDataPath, flushed on every mark.
//
// A point is a detection lifetime, not an instant: the click only identifies which tracked
// object was meant, and its span and position come from the bake. Clicks matching no detection
// are discarded.
//
// Controls (either controller unless noted):
//   Left or right trigger   mark: record the nearest active detection under the aim (green,
//                           high beep). No detection = rejected (red, low beep). Already
//                           recorded = duplicate (yellow, mid beep).
//   X                       play/pause the video; replay once the clip has ended
//   B                       log the CSV path; finish and close the CSV once the clip has ended
//
// Authoring plays the clip once with looping off. At the end it pauses and offers replay, which
// keeps appending to the same file and dedup set, or finish. Disables TestModeSequencer while
// present. Add it via the Meta/Study/Point Authoring editor menu, which also wires the marker
// material, or drop the component into VideoTestScene.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.ShaderSample.Study
{
    public class PointAuthoringTool : MonoBehaviour
    {
        [SerializeField] private VideoTestSceneManager m_video;
        [Tooltip("Unlit material template for reticle/marker (SelectionDotMat — Shader.Find is stripped on Android).")]
        [SerializeField] private Material m_markerMaterialTemplate;

        [Header("Matching")]
        [Tooltip("Max angular distance (deg) between the aim and a detection's current position for a mark to match it.")]
        [SerializeField, Range(1f, 20f)] private float m_matchToleranceDeg = 8f;
        [Tooltip("Same meaning as the manager's detection hold — how long a lifetime tolerates a missed sample before closing.")]
        [SerializeField, Range(0f, 2f)] private float m_holdSeconds = 0.5f;
        [Tooltip("Treat the clip as ended when the playhead is within this many seconds of the end.")]
        [SerializeField, Range(0.05f, 1f)] private float m_endEpsilonSec = 0.2f;

        [Header("Reticle / marker")]
        [SerializeField] private float m_reticleDistance = 6f;
        [SerializeField] private float m_reticleSize = 0.05f;
        [SerializeField] private float m_markerDistance = 8f;
        [SerializeField] private float m_markerSize = 0.12f;
        [SerializeField] private float m_markerSeconds = 0.4f;

        [Header("Input (either controller marks)")]
        [SerializeField] private OVRInput.RawButton m_markLeft = OVRInput.RawButton.LIndexTrigger;
        [SerializeField] private OVRInput.RawButton m_markRight = OVRInput.RawButton.RIndexTrigger;
        [SerializeField] private OVRInput.RawButton m_playPauseButton = OVRInput.RawButton.X; // ended: replay
        [SerializeField] private OVRInput.RawButton m_exportButton = OVRInput.RawButton.B;    // ended: finish

        // COCO classes of interest: 0 person, 9 traffic light, 11 stop sign.
        private static readonly HashSet<int> k_classes = new() { 0, 9, 11 };

        private enum Phase { Authoring, Ended, Finished }
        private Phase m_phase = Phase.Authoring;

        private List<VideoTestSceneManager.DetectionLifetime> m_lifetimes;
        private readonly HashSet<string> m_recorded = new();
        private int m_count;
        private bool m_playing = true;
        private bool m_started;
        private bool m_sawMidClip; // guards end-detection: only fire after the clip has played past the start

        private GameObject m_reticleL, m_reticleR, m_markerGO;
        private Material m_reticleMatL, m_reticleMatR, m_markerMat;
        private Coroutine m_markerCo;
        private AudioSource m_audio;
        private GameObject m_uiRoot;
        private Text m_uiText;

        private StreamWriter m_log;
        private string m_logPath;
        private const string k_header =
            "point_id,cls,kind,t_start,t_end,duration_s,az_mid_deg,el_mid_deg,eccentricity_deg,box_height_deg";

        private static string Kind(int cls) => cls switch
        {
            9 => "traffic_light", 11 => "stop_sign", 0 => "person", _ => "unknown",
        };

        private void Start()
        {
            if (m_video == null) m_video = FindObjectOfType<VideoTestSceneManager>();
            EnsureVisuals();
            SetHUD($"Points: {m_count}");
            Debug.Log("[PointAuthoring] Either trigger = mark; X = play/pause; B = log CSV path. Clip plays once, then X = replay / B = finish.");
        }

        private void Update()
        {
            // Take over from the harness first: while it exists it paints windows, pauses the
            // video and advances modes. Wait a frame for it to be gone.
            if (TestModeSequencer.Instance != null) { Destroy(TestModeSequencer.Instance.gameObject); return; }
            if (m_video == null) return;

            // Suppress the manager's filter and window painting: authoring shows raw video, and
            // the trigger marks rather than paints.
            m_video.StudyInputLock = true;         // disables VideoTestSceneManager's paint handler
            m_video.StudyEffectSuppressed = true;  // effectiveStrength 0 → no vignette/dim/highlights

            if (!m_started)
            {
                m_started = true;
                m_video.VideoLooping = false;      // author over one pass; replay offered at the end
                m_video.SetVideoPlaying(true);     // (re)start — the harness may have paused it
                m_playing = true;
            }

            UpdateReticles();

            switch (m_phase)
            {
                case Phase.Authoring:
                    double len = m_video.VideoLength;
                    if (len > 0.0)
                    {
                        if (m_video.VideoTime < len - 1.0) m_sawMidClip = true; // genuinely mid-clip since last (re)start
                        if (m_playing && m_sawMidClip && m_video.VideoTime >= len - m_endEpsilonSec)
                        {
                            EnterEnded();
                            break;
                        }
                    }
                    if (OVRInput.GetDown(m_playPauseButton))
                    {
                        m_playing = !m_playing;
                        m_video.SetVideoPlaying(m_playing);
                    }
                    if (OVRInput.GetDown(m_exportButton) && m_logPath != null)
                        Debug.Log($"[PointAuthoring] {m_count} points -> {m_logPath}");
                    if (OVRInput.GetDown(m_markLeft))
                    {
                        m_video.GetLeftControllerAzEl(out float az, out float el);
                        Mark(az, el);
                    }
                    if (OVRInput.GetDown(m_markRight))
                    {
                        m_video.GetRightControllerAzEl(out float az, out float el);
                        Mark(az, el);
                    }
                    break;

                case Phase.Ended:
                    if (OVRInput.GetDown(m_playPauseButton))   // replay & keep appending
                    {
                        m_phase = Phase.Authoring;
                        m_playing = true;
                        m_sawMidClip = false;      // don't let end-detection re-fire before the rewind registers
                        m_video.RestartVideo();
                        SetHUD($"Points: {m_count}  (replay)");
                    }
                    else if (OVRInput.GetDown(m_exportButton)) // finish
                    {
                        Finish();
                    }
                    break;

                case Phase.Finished:
                    break;
            }
        }

        private void EnterEnded()
        {
            m_playing = false;
            m_video.SetVideoPlaying(false);
            m_phase = Phase.Ended;
            SetHUD($"Clip ended — {m_count} points\n[X] replay & keep marking     [B] finish");
        }

        private void Finish()
        {
            m_phase = Phase.Finished;
            m_log?.Flush(); m_log?.Dispose(); m_log = null;
            SetHUD($"Done — {m_count} points\n{m_logPath}");
            Debug.Log($"[PointAuthoring] Finished. {m_count} points -> {m_logPath}");
        }

        private void Mark(float az, float el)
        {
            float vt = m_video.VideoTime;
            if (vt < 0f) return;
            EnsureLifetimes();

            VideoTestSceneManager.DetectionLifetime? best = null;
            float bestAng = m_matchToleranceDeg;
            foreach (var l in m_lifetimes)
            {
                if (vt < l.tStart || vt > l.tEnd + m_holdSeconds) continue; // not on screen now
                Vector2 p = PosAt(l, vt);
                float ang = AngDeg(az, el, p.x, p.y);
                if (ang < bestAng) { bestAng = ang; best = l; }
            }

            if (best == null) { Feedback(false, az, el); return; } // nothing under the aim

            var lf = best.Value;
            string key = $"{lf.cls}:{lf.tStart:F2}:{lf.tEnd:F2}";
            if (!m_recorded.Add(key)) { Feedback(false, az, el, dup: true); return; } // already got this one

            RecordPoint(lf);
            Vector2 mid = PosAt(lf, (lf.tStart + lf.tEnd) * 0.5f);
            Feedback(true, mid.x, mid.y);
        }

        private void RecordPoint(VideoTestSceneManager.DetectionLifetime lf)
        {
            float tMid = (lf.tStart + lf.tEnd) * 0.5f;
            Vector2 mid = PosAt(lf, tMid);
            Vector4 rectMid = RectAt(lf, tMid);
            float ecc = AngDeg(0f, 0f, mid.x, mid.y);
            float boxH = (rectMid.w - rectMid.z) * Mathf.Rad2Deg;

            OpenLogIfNeeded();
            m_log.WriteLine(string.Join(",", m_count, lf.cls, Kind(lf.cls),
                F(lf.tStart), F(lf.tEnd), F(lf.tEnd - lf.tStart),
                F(mid.x * Mathf.Rad2Deg), F(mid.y * Mathf.Rad2Deg), F(ecc), F(boxH)));
            m_log.Flush();
            m_count++;
            SetHUD($"Points: {m_count}\nlast: {Kind(lf.cls)}  t={lf.tStart:F1}-{lf.tEnd:F1}s  ecc={ecc:F0}°");
        }

        // ---- geometry ----

        private Vector4 RectAt(VideoTestSceneManager.DetectionLifetime l, float vt)
        {
            var s = l.samples;
            var s0 = s[0]; var s1 = s[s.Count - 1];
            for (int i = 0; i < s.Count - 1; i++)
                if (vt >= s[i].t && vt <= s[i + 1].t) { s0 = s[i]; s1 = s[i + 1]; break; }
            float frac = s1.t > s0.t ? Mathf.Clamp01((vt - s0.t) / (s1.t - s0.t)) : 0f;
            return m_video.DetectionBoxToAzElRect(Vector4.Lerp(s0.box, s1.box, frac));
        }

        private Vector2 PosAt(VideoTestSceneManager.DetectionLifetime l, float vt)
        {
            Vector4 r = RectAt(l, vt);
            return new Vector2((r.x + r.y) * 0.5f, (r.z + r.w) * 0.5f);
        }

        private static float AngDeg(float az1, float el1, float az2, float el2) =>
            Vector3.Angle(Dir(az1, el1), Dir(az2, el2));

        private static Vector3 Dir(float az, float el)
        {
            float cosEl = Mathf.Cos(el);
            return new Vector3(Mathf.Sin(az) * cosEl, Mathf.Sin(el), Mathf.Cos(az) * cosEl);
        }

        private void EnsureLifetimes()
        {
            // Build once the baked track is available (loads async at scene start); rebuild while
            // still empty so an early first click doesn't cache an empty list.
            if (m_lifetimes != null && m_lifetimes.Count > 0) return;
            m_lifetimes = m_video.BuildDetectionLifetimes(k_classes, m_holdSeconds);
            if (m_lifetimes == null || m_lifetimes.Count == 0)
                Debug.LogWarning("[PointAuthoring] No detection lifetimes yet — is the baked track loaded?");
        }

        // ---- feedback (per-hand reticles, brief marker at matched point, beep) ----

        private void UpdateReticles()
        {
            Vector3 head = Camera.main != null ? Camera.main.transform.position : transform.position;
            if (m_reticleL != null)
            {
                m_video.GetLeftControllerAzEl(out float az, out float el);
                m_reticleL.transform.position = head + Dir(az, el) * m_reticleDistance;
            }
            if (m_reticleR != null)
            {
                m_video.GetRightControllerAzEl(out float az, out float el);
                m_reticleR.transform.position = head + Dir(az, el) * m_reticleDistance;
            }
        }

        private void Feedback(bool matched, float az, float el, bool dup = false)
        {
            Color c = matched ? new Color(0.3f, 1f, 0.4f, 0.9f)
                     : dup    ? new Color(1f, 0.9f, 0.2f, 0.9f)
                              : new Color(1f, 0.3f, 0.3f, 0.9f);
            if (m_markerCo != null) StopCoroutine(m_markerCo);
            m_markerCo = StartCoroutine(MarkerFlash(az, el, c));
            if (m_audio != null) m_audio.PlayOneShot(Beep(matched ? 880f : dup ? 520f : 320f, 0.12f));
        }

        private IEnumerator MarkerFlash(float az, float el, Color c)
        {
            if (m_markerGO == null) yield break;
            Vector3 head = Camera.main != null ? Camera.main.transform.position : transform.position;
            m_markerGO.transform.position = head + Dir(az, el) * m_markerDistance;
            m_markerMat.color = c;
            m_markerGO.SetActive(true);
            yield return new WaitForSeconds(m_markerSeconds);
            m_markerGO.SetActive(false);
            m_markerCo = null;
        }

        // ---- setup ----

        private void EnsureVisuals()
        {
            if (m_reticleL == null)
                m_reticleL = MakeSphere("AuthorReticleL", m_reticleSize, new Color(0.2f, 0.9f, 1f, 0.7f), 4200, out m_reticleMatL);
            if (m_reticleR == null)
                m_reticleR = MakeSphere("AuthorReticleR", m_reticleSize, new Color(1f, 0.7f, 0.2f, 0.7f), 4200, out m_reticleMatR);
            if (m_markerGO == null)
            {
                m_markerGO = MakeSphere("AuthorMarker", m_markerSize, Color.green, 4201, out m_markerMat);
                m_markerGO.SetActive(false);
            }
            if (m_audio == null)
            {
                m_audio = gameObject.AddComponent<AudioSource>();
                m_audio.playOnAwake = false; m_audio.spatialBlend = 0f; m_audio.volume = 0.6f;
            }
            BuildHUD();
        }

        private GameObject MakeSphere(string name, float size, Color color, int queue, out Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(transform, false);
            Destroy(go.GetComponent<Collider>());
            mat = m_markerMaterialTemplate != null ? new Material(m_markerMaterialTemplate)
                                                    : new Material(Shader.Find("Standard"));
            if (m_markerMaterialTemplate == null)
                Debug.LogWarning("[PointAuthoring] No marker material template — Shader.Find fallback breaks in Android builds.");
            mat.color = color;
            mat.renderQueue = queue;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.transform.localScale = Vector3.one * size;
            return go;
        }

        private void SetHUD(string text)
        {
            if (m_uiText != null) m_uiText.text = text;
        }

        private void BuildHUD()
        {
            if (m_uiRoot != null) return;
            m_uiRoot = new GameObject("AuthorHUD");
            m_uiRoot.transform.SetParent(transform, false);
            var canvas = m_uiRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            m_uiRoot.GetComponent<RectTransform>().sizeDelta = new Vector2(700f, 160f);
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
            m_uiText.text = "Points: 0";
        }

        private void LateUpdate()
        {
            if (m_uiRoot == null || Camera.main == null) return;
            Transform h = Camera.main.transform;
            Vector3 fwd = Vector3.ProjectOnPlane(h.forward, Vector3.up);
            fwd = fwd.sqrMagnitude > 0.001f ? fwd.normalized : h.forward;
            m_uiRoot.transform.position = h.position + fwd * 1.5f + Vector3.down * 0.35f;
            m_uiRoot.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }

        // ---- audio / logging ----

        private static AudioClip Beep(float freqHz, float durationSec)
        {
            const int sr = 44100;
            int n = Mathf.Max(1, Mathf.RoundToInt(durationSec * sr));
            var clip = AudioClip.Create("AuthorBeep", n, 1, sr, false);
            var data = new float[n];
            for (int i = 0; i < n; i++)
                data[i] = Mathf.Sin(2f * Mathf.PI * freqHz * i / sr) * (1f - (float)i / n) * 0.5f;
            clip.SetData(data, 0);
            return clip;
        }

        private void OpenLogIfNeeded()
        {
            if (m_log != null) return;
            string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            m_logPath = Path.Combine(Application.persistentDataPath, $"authored_points_{stamp}.csv");
            m_log = new StreamWriter(m_logPath, false, new UTF8Encoding(false));
            m_log.WriteLine(k_header);
            Debug.Log($"[PointAuthoring] Logging to {m_logPath}");
        }

        private static string F(float v) => v.ToString("F3", CultureInfo.InvariantCulture);

        private void OnDestroy()
        {
            m_log?.Flush(); m_log?.Dispose();
            if (m_reticleL != null) Destroy(m_reticleL);
            if (m_reticleR != null) Destroy(m_reticleR);
            if (m_markerGO != null) Destroy(m_markerGO);
            if (m_reticleMatL != null) Destroy(m_reticleMatL);
            if (m_reticleMatR != null) Destroy(m_reticleMatR);
            if (m_markerMat != null) Destroy(m_markerMat);
        }
    }
}
