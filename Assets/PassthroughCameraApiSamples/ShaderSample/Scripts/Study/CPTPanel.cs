// Block A Option V: world-locked Go/No-go sustained-attention panel (testing-strategy-v2 §4.1).
// One shape every SOA (1.5 s), visible for 700 ms: circle = go (pull trigger), square = no-go
// (withhold). 80% go / 20% no-go, seeded and deterministic per (participant, condition).
//
// Sequence constraint note: the strategy doc says "no more than three identical in a row",
// which is unsatisfiable for the go class at an 80% go rate (mean run length 5). Implemented
// as the enforceable version of the same intent: no two consecutive no-gos, go runs capped at
// 8. Flagged in the build report so the methodology text can be aligned.
//
// Shapes are generated at runtime (circle texture drawn procedurally) — no sprite assets, no
// Shader.Find, nothing strippable in Android builds. Panel UI uses the default canvas material
// at renderQueue 4050 so the head-centred vignette sphere (queue 3000) cannot sort over it;
// the panel sits inside the focus window, where the overlay is transparent anyway.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.ShaderSample.Study
{
    public class CPTPanel : MonoBehaviour
    {
        [Header("Placement")]
        [Tooltip("Distance from the eyes at placement (m).")]
        [SerializeField, Range(0.3f, 1.5f)] private float m_distance = 0.6f;
        [SerializeField] private Vector2 m_panelSizeM = new(0.24f, 0.24f);

        [Header("Stimuli")]
        [Tooltip("Shape size as visual angle at placement distance (deg). Spec minimum 3°.")]
        [SerializeField, Range(3f, 12f)] private float m_shapeSizeDeg = 5.5f;
        [SerializeField, Range(0.5f, 3f)]  private float m_soaSeconds      = 1.5f;
        [SerializeField, Range(0.2f, 1.4f)] private float m_stimulusSeconds = 0.7f;
        [SerializeField, Range(0.05f, 0.5f)] private float m_noGoProportion = 0.2f;
        [SerializeField] private Color m_panelColor = new(0.09f, 0.09f, 0.11f, 0.92f);
        [SerializeField] private Color m_shapeColor = Color.white;

        public bool Running { get; private set; }
        public bool Placed { get; private set; }
        /// <summary>Go-trial accuracy of the last completed run (practice criterion: ≥ 0.90).</summary>
        public float LastRunGoAccuracy { get; private set; }

        private GameObject m_root;
        private Material   m_uiMat;
        private Image      m_shapeImage;
        private Sprite     m_circleSprite;
        private Coroutine  m_run;

        // ---- placement ----

        /// <summary>World-lock the panel at the current head gaze. Call before each Block A run
        /// (experimenter key), after the focus window is locked, so the panel sits inside it.</summary>
        public void PlaceAtGaze()
        {
            var head = Camera.main != null ? Camera.main.transform : transform;
            EnsureBuilt();
            m_root.transform.position = head.position + head.forward * m_distance;
            var away = m_root.transform.position - head.position;
            m_root.transform.rotation = Quaternion.LookRotation(away, Vector3.up);
            m_root.SetActive(true);
            m_shapeImage.enabled = false;
            Placed = true;
            StudyLogger.Instance?.LogEvent("CPT_PANEL_PLACED",
                $"pos={m_root.transform.position.ToString("F2")};dist_m={m_distance}");
        }

        public void Hide()
        {
            StopRun();
            if (m_root != null) m_root.SetActive(false);
            Placed = false;
        }

        // ---- run control ----

        public bool BeginRun(int trials, int seed)
        {
            if (!Placed)
            {
                Debug.LogError("[CPTPanel] BeginRun before PlaceAtGaze.");
                return false;
            }
            StopRun();
            m_run = StartCoroutine(RunCoro(trials, seed));
            return true;
        }

        public void StopRun()
        {
            if (m_run != null) { StopCoroutine(m_run); m_run = null; }
            Running = false;
            if (m_shapeImage != null) m_shapeImage.enabled = false;
        }

        private IEnumerator RunCoro(int trials, int seed)
        {
            Running = true;
            var seq = BuildSequence(trials, seed);
            var log = StudyLogger.Instance;
            log?.LogEvent("CPT_RUN_START", $"trials={trials};seed={seed};soa_s={m_soaSeconds}");

            int goCount = 0, goHits = 0;

            for (int i = 0; i < seq.Count; i++)
            {
                bool isGo = seq[i];
                ShowShape(isGo);
                log?.LogEvent("CPT_ONSET", $"idx={i};kind={(isGo ? "go" : "nogo")}");

                float onset = Time.time;
                bool  responded = false;
                float rt = 0f;

                // Response window = the full SOA from onset.
                while (Time.time - onset < m_soaSeconds)
                {
                    if (!responded && OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
                    {
                        responded = true;
                        rt = Time.time - onset;
                    }
                    if (m_shapeImage.enabled && Time.time - onset >= m_stimulusSeconds)
                        m_shapeImage.enabled = false;
                    yield return null;
                }

                string outcome;
                if (isGo)
                {
                    goCount++;
                    if (responded) { goHits++; outcome = $"hit;rt_ms={(int)(rt * 1000f)}"; }
                    else             outcome = "miss";
                }
                else
                {
                    outcome = responded ? $"commission;rt_ms={(int)(rt * 1000f)}" : "correct_reject";
                }
                log?.LogEvent("CPT_RESULT", $"idx={i};outcome={outcome}");
            }

            LastRunGoAccuracy = goCount > 0 ? (float)goHits / goCount : 0f;
            log?.LogEvent("CPT_RUN_END",
                $"go_acc={LastRunGoAccuracy.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}");
            Running = false;
            m_run = null;
        }

        // Seeded, deterministic: exact no-go count, no two consecutive no-gos, go runs ≤ 8.
        private List<bool> BuildSequence(int trials, int seed)
        {
            var rng = new System.Random(seed);
            int noGo = Mathf.RoundToInt(trials * m_noGoProportion);

            for (int attempt = 0; attempt < 2000; attempt++)
            {
                var positions = new HashSet<int>();
                while (positions.Count < noGo) positions.Add(rng.Next(trials));

                bool ok = true;
                foreach (int p in positions)
                    if (positions.Contains(p + 1)) { ok = false; break; }
                if (!ok) continue;

                var seq = new List<bool>(trials);
                for (int i = 0; i < trials; i++) seq.Add(!positions.Contains(i));

                int run = 0;
                foreach (bool go in seq)
                {
                    run = go ? run + 1 : 0;
                    if (run > 8) { ok = false; break; }
                }
                if (ok) return seq;
            }

            // Deterministic fallback: evenly spaced no-gos (still valid, slightly regular).
            Debug.LogWarning("[CPTPanel] Sequence constraints not met by sampling; using even spacing.");
            var fallback = new List<bool>(new bool[trials]);
            for (int i = 0; i < trials; i++) fallback[i] = true;
            float step = (float)trials / noGo;
            for (int k = 0; k < noGo; k++) fallback[Mathf.Min(trials - 1, (int)(k * step + step * 0.5f))] = false;
            return fallback;
        }

        // ---- UI construction (runtime-only, no assets) ----

        private void EnsureBuilt()
        {
            if (m_root != null) return;

            m_root = new GameObject("CPTPanel");
            var canvas = m_root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = m_root.GetComponent<RectTransform>();
            rt.sizeDelta  = new Vector2(m_panelSizeM.x * 1000f, m_panelSizeM.y * 1000f);
            rt.localScale = Vector3.one * 0.001f;

            m_uiMat = new Material(Canvas.GetDefaultCanvasMaterial()) { renderQueue = 4050 };

            var bg = new GameObject("BG");
            bg.transform.SetParent(m_root.transform, false);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = m_panelColor;
            bgImg.material = m_uiMat;
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

            m_circleSprite = MakeCircleSprite(128);

            var shape = new GameObject("Shape");
            shape.transform.SetParent(m_root.transform, false);
            m_shapeImage = shape.AddComponent<Image>();
            m_shapeImage.material = m_uiMat;
            m_shapeImage.color = m_shapeColor;
            float sizeMm = 2f * m_distance * Mathf.Tan(0.5f * m_shapeSizeDeg * Mathf.Deg2Rad) * 1000f;
            var sRt = shape.GetComponent<RectTransform>();
            sRt.sizeDelta = new Vector2(sizeMm, sizeMm);
            m_shapeImage.enabled = false;

            m_root.SetActive(false);
        }

        private void ShowShape(bool isGo)
        {
            m_shapeImage.sprite  = isGo ? m_circleSprite : null; // null sprite = solid square
            m_shapeImage.enabled = true;
        }

        private static Sprite MakeCircleSprite(int res)
        {
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            float c = (res - 1) * 0.5f, rOut = res * 0.5f - 1f;
            var pixels = new Color32[res * res];
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    byte a = (byte)(255f * Mathf.Clamp01(rOut - d)); // 1-px AA edge
                    pixels[y * res + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100f);
        }

        private void OnDestroy()
        {
            StopRun();
            if (m_root != null) Destroy(m_root);
            if (m_uiMat != null) Destroy(m_uiMat);
        }
    }
}
