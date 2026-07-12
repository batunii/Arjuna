// Click-capture harness for the "click on traffic lights / signs" paradigm.
// The LEFT controller aims a cyan reticle at the video sphere; the left trigger logs a
// CLICK event with the VIDEO PLAYHEAD TIME and click direction (az/el). Matching clicks to
// the YOLO bake (DebugVideo.detections.json) is done OFFLINE in Tools/analysis (a click's
// video_t + az/el is compared against the pre-coded detections at that time).
//
// This is a standalone capture test: with m_autoOpenSession on, it opens a StudyLogger
// session (pid 999, block CLICKTEST) shortly after launch, so you just launch, click on
// lights, and pull the CSV — no session flow needed. StudyLogger flushes every frame, so
// pulling mid-session gives a complete file. Turn m_autoOpenSession OFF when integrating
// this into the real study flow (then clicks log into whatever session is already open).

using System.Globalization;
using UnityEngine;

namespace PassthroughCameraSamples.ShaderSample.Study
{
    public class ClickProbeTest : MonoBehaviour
    {
        [SerializeField] private VideoTestSceneManager m_video;
        [SerializeField] private StudyLogger m_logger;
        [Tooltip("Unlit material template for the reticle (SelectionDotMat — Shader.Find is stripped on Android).")]
        [SerializeField] private Material m_reticleTemplate;

        [SerializeField] private bool  m_autoOpenSession = true;
        [SerializeField] private int   m_pid = 999;
        [SerializeField] private float m_reticleDistance = 6f;
        [SerializeField] private float m_reticleSize = 0.06f;
        [SerializeField] private Color m_reticleColor = new(0.2f, 0.9f, 1f, 1f);

        private GameObject m_reticle;
        private Material   m_reticleMat;
        private bool       m_sessionStarted;
        private int        m_clickCount;

        private void Start()
        {
            if (m_video == null)  m_video  = FindFirstObjectByType<VideoTestSceneManager>();
            if (m_logger == null) m_logger = FindFirstObjectByType<StudyLogger>();
            EnsureReticle();
        }

        private void Update()
        {
            if (m_video == null) return;

            if (m_autoOpenSession && !m_sessionStarted && m_logger != null && !m_logger.SessionOpen)
            {
                m_logger.BeginSession(m_pid.ToString());
                m_logger.Block = "CLICKTEST";
                m_logger.LogEvent("CLICK_CONFIG",
                    $"uOffset={F(m_video.VideoUOffset)};vOffset={F(m_video.VideoVOffset)};" +
                    $"flipY={(m_video.VideoFlipY ? 1 : 0)};eqCam=1;task=click_lights_signs");
                m_sessionStarted = true;
                Debug.Log("[ClickProbeTest] Click-capture session open (P999). Left trigger = click. Reticle is cyan.");
            }

            m_video.GetLeftControllerAzEl(out float az, out float el);
            Vector3 head = Camera.main != null ? Camera.main.transform.position : transform.position;
            if (m_reticle != null) m_reticle.transform.position = head + Dir(az, el) * m_reticleDistance;

            if (OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger) && (m_logger?.SessionOpen ?? false))
            {
                m_clickCount++;
                m_logger.LogEvent("CLICK",
                    $"n={m_clickCount};video_t={F(m_video.VideoTime)};" +
                    $"az_deg={F(az * Mathf.Rad2Deg)};el_deg={F(el * Mathf.Rad2Deg)}");
            }
        }

        private void EnsureReticle()
        {
            m_reticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_reticle.name = "ClickReticle";
            Destroy(m_reticle.GetComponent<Collider>());
            m_reticleMat = m_reticleTemplate != null
                ? new Material(m_reticleTemplate)
                : new Material(Shader.Find("Standard"));
            if (m_reticleTemplate == null)
                Debug.LogWarning("[ClickProbeTest] No reticle material template — Shader.Find fallback breaks in Android builds.");
            m_reticleMat.color = m_reticleColor;
            m_reticleMat.renderQueue = 4200;
            m_reticle.GetComponent<MeshRenderer>().sharedMaterial = m_reticleMat;
            m_reticle.transform.localScale = Vector3.one * m_reticleSize;
        }

        private static string F(float v) => v.ToString("F3", CultureInfo.InvariantCulture);

        private static Vector3 Dir(float az, float el)
        {
            float cosEl = Mathf.Cos(el);
            return new Vector3(Mathf.Sin(az) * cosEl, Mathf.Sin(el), Mathf.Cos(az) * cosEl);
        }

        private void OnDestroy()
        {
            if (m_reticle != null) Destroy(m_reticle);
            if (m_reticleMat != null) Destroy(m_reticleMat);
        }
    }
}
