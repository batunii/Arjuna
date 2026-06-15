// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.ShaderSample
{
    /// <summary>
    /// Drives the <c>Meta/PCA/FocusVignette</c> shader. Inverted-sphere technique: a head-centered
    /// sphere (Cull Front) renders a translucent eccentricity-adaptive overlay over the OS passthrough.
    ///
    /// Mode 1 — LIGHT PERCEPTUAL: mild peripheral blur + desaturation. Clear focus zone is wide;
    ///   effect is subtle enough to be near-imperceptible. After scanning, the effect returns quickly.
    ///
    /// Mode 2 — DEEP PERCEPTUAL: strong peripheral blur + desaturation. Effect starts closer to the
    ///   gaze centre; desaturation is heavier. Crucially, after scanning, the normal-vision window
    ///   persists significantly longer before the vignette reapplies — rewarding deliberate head
    ///   movements with extended unobstructed view.
    ///
    /// Switching between Mode 1 and Mode 2 smoothly interpolates all shader parameters over
    /// <see cref="m_transitionDuration"/> seconds (no hard cut).
    ///
    /// Mode 3 — STATIC: controller-defined rectangle window; hard tunnel outside.
    ///
    /// B / middle-finger pinch cycles modes: Light → Deep → Static → Light.
    /// A / index-pinch places a corner (Static only).
    ///
    /// Research grounding: Hansen et al. 2009 (4.5× colour threshold at 50° vs. 5°),
    /// Kergassner SIGGRAPH 2025 (σ≈5.4 arcmin blur at 10°), Krajancich SIGGRAPH 2023
    /// (task focus suppresses peripheral sensitivity — justifies heavier Mode 2 params).
    /// </summary>
    [MetaCodeSample("PassthroughCameraApiSamples-ShaderSample")]
    public class FocusVignetteManager : MonoBehaviour
    {
        private enum FocusMode { LightPerceptual, DeepPerceptual, Static }

        [SerializeField] private PassthroughCameraAccess m_cameraAccess;
        [SerializeField] private OVRPassthroughLayer m_passthroughLayer;
        [SerializeField] private MeshRenderer m_renderer;
        [SerializeField] private Text m_debugText;
        [SerializeField] private Transform m_headAnchor;

        [Tooltip("Controller transform used to aim in Static mode (e.g. RightControllerAnchor).")]
        [SerializeField] private Transform m_pointer;

        [SerializeField] private FocusMode m_mode = FocusMode.LightPerceptual;

        // -----------------------------------------------------------------------------------------
        // Mode 1: Light Perceptual — mild, near-imperceptible eccentricity vignette
        // Blur starts wide (12°); desaturation is gentle and begins well away from centre (25°).
        // Effect reapplies quickly after head movement (2.5 s) — scanning feels natural.
        // -----------------------------------------------------------------------------------------
        [Header("Mode 1 Light — eccentricity angles")]
        [Tooltip("Eccentricity at which blur begins (deg). Research baseline: 10°.")]
        [SerializeField, Range(5f, 25f)]  private float m_lightBlurStartDeg   = 12f;
        [Tooltip("Eccentricity at peak blur (deg).")]
        [SerializeField, Range(15f, 60f)] private float m_lightBlurMaxDeg     = 27f;
        [Tooltip("Eccentricity at which desaturation begins (deg). Research: start later than blur.")]
        [SerializeField, Range(10f, 45f)] private float m_lightDesatStartDeg  = 25f;
        [Tooltip("Eccentricity at peak desaturation (deg). Research: 4.5× threshold at 50°.")]
        [SerializeField, Range(30f, 80f)] private float m_lightDesatMaxDeg    = 62f;

        [Header("Mode 1 Light — intensity")]
        [SerializeField, Range(0f, 0.05f)] private float m_lightMaxBlurRadius  = 0.008f;
        [SerializeField, Range(0.5f, 3f)]  private float m_lightBlurCurveExp   = 1.5f;
        [SerializeField, Range(0.5f, 4f)]  private float m_lightDesatCurveExp  = 2.0f;
        [SerializeField, Range(0f, 0.4f)]  private float m_lightContrastBoost  = 0.12f;
        [SerializeField, Range(0f, 1f)]    private float m_lightMaxDim         = 0.55f;
        [SerializeField, Range(0.01f, 1f)] private float m_lightEdgeSoftness   = 0.20f;
        [SerializeField]                   private Color m_lightDimColor        = Color.black;

        [Header("Mode 1 Light — motion easing")]
        [Tooltip("Seconds to ease the effect back ON after head settles. Short = effect returns quickly.")]
        [SerializeField] private float m_lightReapplySeconds   = 2.5f;
        [Tooltip("Head speed (deg/s) that triggers the effect easing off.")]
        [SerializeField] private float m_lightMotionThreshDeg  = 30f;

        // -----------------------------------------------------------------------------------------
        // Mode 2: Deep Perceptual — strong eccentricity vignette with extended normal-vision window
        // Blur and desaturation start closer to gaze centre and ramp faster.
        // After scanning, normal vision persists much longer (5.5 s) before the effect returns —
        // rewarding deliberate head movements with extended unobstructed passthrough.
        // -----------------------------------------------------------------------------------------
        [Header("Mode 2 Deep — eccentricity angles")]
        [SerializeField, Range(5f, 25f)]  private float m_deepBlurStartDeg    = 8f;
        [SerializeField, Range(15f, 60f)] private float m_deepBlurMaxDeg      = 22f;
        [SerializeField, Range(10f, 45f)] private float m_deepDesatStartDeg   = 15f;
        [SerializeField, Range(30f, 80f)] private float m_deepDesatMaxDeg     = 45f;

        [Header("Mode 2 Deep — intensity")]
        [SerializeField, Range(0f, 0.05f)] private float m_deepMaxBlurRadius  = 0.025f;
        [SerializeField, Range(0.5f, 3f)]  private float m_deepBlurCurveExp   = 1.2f;
        [SerializeField, Range(0.5f, 4f)]  private float m_deepDesatCurveExp  = 1.5f;
        [SerializeField, Range(0f, 0.4f)]  private float m_deepContrastBoost  = 0.18f;
        [SerializeField, Range(0f, 1f)]    private float m_deepMaxDim         = 0.90f;
        [SerializeField, Range(0.01f, 1f)] private float m_deepEdgeSoftness   = 0.30f;
        [SerializeField]                   private Color m_deepDimColor        = Color.black;

        [Header("Mode 2 Deep — motion easing")]
        [Tooltip("Seconds to ease back ON after head settles. Long = normal vision persists after scanning.")]
        [SerializeField] private float m_deepReapplySeconds   = 5.5f;
        [Tooltip("Head speed (deg/s) that triggers the effect easing off. Lower = more sensitive.")]
        [SerializeField] private float m_deepMotionThreshDeg  = 22f;

        // -----------------------------------------------------------------------------------------
        // Shared motion easing + mode transition
        // -----------------------------------------------------------------------------------------
        [Header("Shared motion easing")]
        [Tooltip("Seconds to ease the effect OFF when head motion is detected (both modes).")]
        [SerializeField] private float m_revealSeconds = 0.25f;

        [Header("Mode transition")]
        [Tooltip("Time (seconds) to smoothly interpolate all shader params when switching between Mode 1 and Mode 2.")]
        [SerializeField, Range(0.2f, 3f)] private float m_transitionDuration = 1.5f;

        // -----------------------------------------------------------------------------------------
        // Static (Mode 3) — unchanged
        // -----------------------------------------------------------------------------------------
        [Header("Mode 3 Static — controller rectangle window")]
        [SerializeField] private Color m_staticDimColor = Color.black;
        [SerializeField, Range(0, 1)] private float m_staticMax = 1f;
        [SerializeField, Range(0.01f, 1f)] private float m_staticEdgeSoftness = 0.2f;
        [SerializeField] private float m_selectDistance = 2.5f;
        [SerializeField] private Transform m_aimCursor;
        [SerializeField] private Transform[] m_cornerMarkers = new Transform[4];

        [SerializeField] private float m_cameraHorizontalFovDeg = 82f;

        // -----------------------------------------------------------------------------------------
        // Runtime state
        // -----------------------------------------------------------------------------------------
        private Material m_material;
        private bool m_loggedIntrinsics;

        // Perceptual blend (0 = Mode 1 Light, 1 = Mode 2 Deep).
        private float m_perceptualT = 0f;
        private float m_targetPerceptualT = 0f;

        private Vector3 m_prevForward = Vector3.forward;
        private float m_drIntensity = 0f;

        // Static mode state.
        private readonly Vector3[] m_cornerPoints = new Vector3[2];
        private int m_cornerCount;

        // -----------------------------------------------------------------------------------------
        // Shader property IDs
        // -----------------------------------------------------------------------------------------
        private static readonly int s_mainTexId        = Shader.PropertyToID("_MainTex");
        private static readonly int s_sphereCenterId   = Shader.PropertyToID("_SphereCenter");
        private static readonly int s_headRightId      = Shader.PropertyToID("_HeadRight");
        private static readonly int s_headUpId         = Shader.PropertyToID("_HeadUp");
        private static readonly int s_headForwardId    = Shader.PropertyToID("_HeadForward");
        private static readonly int s_tanHalfFovId     = Shader.PropertyToID("_TanHalfFov");
        private static readonly int s_focusModeId      = Shader.PropertyToID("_FocusMode");
        private static readonly int s_focusDirId       = Shader.PropertyToID("_FocusDir");
        private static readonly int s_edgeSoftnessId   = Shader.PropertyToID("_EdgeSoftness");
        private static readonly int s_drIntensityId    = Shader.PropertyToID("_DrIntensity");
        private static readonly int s_desatStrengthId  = Shader.PropertyToID("_DesatStrength");
        private static readonly int s_dimColorId       = Shader.PropertyToID("_DimColor");
        private static readonly int s_maxDimId         = Shader.PropertyToID("_MaxDim");
        private static readonly int s_regionActiveId   = Shader.PropertyToID("_RegionActive");
        private static readonly int[] s_cornerIds =
        {
            Shader.PropertyToID("_Corner0"),
            Shader.PropertyToID("_Corner1"),
        };
        private static readonly int s_blurStartAngleId  = Shader.PropertyToID("_BlurStartAngle");
        private static readonly int s_blurMaxAngleId    = Shader.PropertyToID("_BlurMaxAngle");
        private static readonly int s_maxBlurRadiusId   = Shader.PropertyToID("_MaxBlurRadius");
        private static readonly int s_blurCurveExpId    = Shader.PropertyToID("_BlurCurveExp");
        private static readonly int s_desatStartAngleId = Shader.PropertyToID("_DesatStartAngle");
        private static readonly int s_desatMaxAngleId   = Shader.PropertyToID("_DesatMaxAngle");
        private static readonly int s_desatCurveExpId   = Shader.PropertyToID("_DesatCurveExp");
        private static readonly int s_contrastBoostId   = Shader.PropertyToID("_ContrastBoost");

        // -----------------------------------------------------------------------------------------

        private void OnDisable()
        {
            m_passthroughLayer?.SetBrightnessContrastSaturation(0f, 0f, 0f);
        }

        private Transform Head =>
            m_headAnchor != null ? m_headAnchor : (Camera.main != null ? Camera.main.transform : null);

        private Transform Pointer => m_pointer != null ? m_pointer : Head;

        private IEnumerator Start()
        {
            m_material = m_renderer.material;

            if (m_debugText != null)
            {
                m_debugText.text = "No permission granted.";
            }
            if (!OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.PassthroughCameraAccess))
            {
                OVRPermissionsRequester.Request(new[] { OVRPermissionsRequester.Permission.PassthroughCameraAccess });
            }
            while (!OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.PassthroughCameraAccess))
            {
                yield return null;
            }
            while (!m_cameraAccess.IsPlaying)
            {
                yield return null;
            }

            m_material.SetTexture(s_mainTexId, m_cameraAccess.GetTexture());

            var head = Head;
            if (head != null)
            {
                m_prevForward = head.forward;
            }
            ApplyMode();
        }

        private void LateUpdate()
        {
            var head = Head;
            if (head == null || m_material == null)
            {
                return;
            }

            transform.position = head.position;
            m_material.SetVector(s_sphereCenterId, head.position);
            m_material.SetVector(s_headRightId, head.right);
            m_material.SetVector(s_headUpId, head.up);
            m_material.SetVector(s_headForwardId, head.forward);
            FeedFovUniform();

            // B / middle-finger pinch cycles: Light → Deep → Static → Light.
            if (InputManager.IsButtonBDownOrMiddleFingerPinchStarted())
            {
                m_mode = m_mode switch
                {
                    FocusMode.LightPerceptual => FocusMode.DeepPerceptual,
                    FocusMode.DeepPerceptual  => FocusMode.Static,
                    _                         => FocusMode.LightPerceptual,
                };
                ApplyMode();
            }

            if (m_mode == FocusMode.LightPerceptual || m_mode == FocusMode.DeepPerceptual)
            {
                UpdatePerceptual(head);
            }
            else
            {
                UpdateStatic(head);
            }
        }

        // Advances the mode-transition blend and pushes all lerped shader params each frame.
        private void UpdatePerceptual(Transform head)
        {
            // Advance perceptual blend toward target.
            float transRate = m_transitionDuration > 0f ? Time.deltaTime / m_transitionDuration : 1f;
            m_perceptualT = Mathf.MoveTowards(m_perceptualT, m_targetPerceptualT, transRate);

            float t = m_perceptualT;

            // Lerp all shader params between Light (t=0) and Deep (t=1) presets.
            float blurStart  = Mathf.Lerp(m_lightBlurStartDeg,  m_deepBlurStartDeg,  t) * Mathf.Deg2Rad;
            float blurMax    = Mathf.Lerp(m_lightBlurMaxDeg,    m_deepBlurMaxDeg,    t) * Mathf.Deg2Rad;
            float blurRadius = Mathf.Lerp(m_lightMaxBlurRadius, m_deepMaxBlurRadius, t);
            float blurExp    = Mathf.Lerp(m_lightBlurCurveExp,  m_deepBlurCurveExp,  t);
            float desatStart = Mathf.Lerp(m_lightDesatStartDeg, m_deepDesatStartDeg, t) * Mathf.Deg2Rad;
            float desatMax   = Mathf.Lerp(m_lightDesatMaxDeg,   m_deepDesatMaxDeg,   t) * Mathf.Deg2Rad;
            float desatExp   = Mathf.Lerp(m_lightDesatCurveExp, m_deepDesatCurveExp, t);
            float contrast   = Mathf.Lerp(m_lightContrastBoost, m_deepContrastBoost, t);
            float maxDim     = Mathf.Lerp(m_lightMaxDim,        m_deepMaxDim,        t);
            float edgeSoft   = Mathf.Lerp(m_lightEdgeSoftness,  m_deepEdgeSoftness,  t);
            Color dimColor   = Color.Lerp(m_lightDimColor,      m_deepDimColor,      t);

            m_material.SetFloat(s_blurStartAngleId,  blurStart);
            m_material.SetFloat(s_blurMaxAngleId,    blurMax);
            m_material.SetFloat(s_maxBlurRadiusId,   blurRadius);
            m_material.SetFloat(s_blurCurveExpId,    blurExp);
            m_material.SetFloat(s_desatStartAngleId, desatStart);
            m_material.SetFloat(s_desatMaxAngleId,   desatMax);
            m_material.SetFloat(s_desatCurveExpId,   desatExp);
            m_material.SetFloat(s_contrastBoostId,   contrast);
            m_material.SetFloat(s_maxDimId,          maxDim);
            m_material.SetFloat(s_edgeSoftnessId,    edgeSoft);
            m_material.SetColor(s_dimColorId,        dimColor);

            // Feed gaze direction (head forward as proxy; swap for OVREyeGaze when available).
            m_material.SetVector(s_focusDirId, head.forward);

            // Motion easing: ease OFF while scanning, back ON when settled.
            // Reapply speed is slower in Deep mode — normal vision persists longer after each scan.
            float angle = Vector3.Angle(head.forward, m_prevForward);
            float speed = Time.deltaTime > 0f ? angle / Time.deltaTime : 0f;
            m_prevForward = head.forward;

            float motionThresh  = Mathf.Lerp(m_lightMotionThreshDeg, m_deepMotionThreshDeg, t);
            float reapplyTime   = Mathf.Lerp(m_lightReapplySeconds,  m_deepReapplySeconds,  t);

            float intensityTarget = speed > motionThresh ? 0f : 1f;
            float intensityRate   = intensityTarget < m_drIntensity
                ? 1f / Mathf.Max(m_revealSeconds, 0.01f)   // easing OFF: fast (same both modes)
                : 1f / Mathf.Max(reapplyTime,     0.01f);  // easing ON:  slow in Deep mode
            m_drIntensity = Mathf.MoveTowards(m_drIntensity, intensityTarget, intensityRate * Time.deltaTime);
            m_material.SetFloat(s_drIntensityId, m_drIntensity);
        }

        private void UpdateStatic(Transform head)
        {
            float drTarget = m_cornerCount >= 2 ? 1f : 0f;
            float drRate = 1f / Mathf.Max(m_lightReapplySeconds, 0.01f);
            m_drIntensity = Mathf.MoveTowards(m_drIntensity, drTarget, drRate * Time.deltaTime);
            m_material.SetFloat(s_drIntensityId, m_drIntensity);

            var pointer = Pointer;
            Vector3 aimPoint = pointer.position + pointer.forward * m_selectDistance;
            if (m_aimCursor != null)
            {
                bool aiming = m_cornerCount < 2;
                if (m_aimCursor.gameObject.activeSelf != aiming)
                {
                    m_aimCursor.gameObject.SetActive(aiming);
                }
                if (aiming)
                {
                    m_aimCursor.position = aimPoint;
                }
            }

            if (InputManager.IsButtonADownOrPinchStarted())
            {
                if (m_cornerCount >= 2)
                {
                    ResetRegion();
                }
                PlaceCorner(aimPoint);
            }

            if (m_cornerCount >= 2)
            {
                for (int k = 0; k < 2; k++)
                {
                    m_material.SetVector(s_cornerIds[k], (m_cornerPoints[k] - head.position).normalized);
                }
                m_material.SetFloat(s_regionActiveId, 1f);
            }
            else
            {
                m_material.SetFloat(s_regionActiveId, 0f);
            }
        }

        private void ApplyMode()
        {
            if (m_material == null)
            {
                return;
            }

            m_drIntensity = 0f;
            m_material.SetFloat(s_drIntensityId, 0f);
            HideMarkers();
            if (m_aimCursor != null)
            {
                m_aimCursor.gameObject.SetActive(false);
            }

            if (m_mode == FocusMode.LightPerceptual || m_mode == FocusMode.DeepPerceptual)
            {
                // Both perceptual modes share shader path 2. Target blend drives lerp in UpdatePerceptual.
                m_targetPerceptualT = m_mode == FocusMode.DeepPerceptual ? 1f : 0f;
                m_material.SetFloat(s_focusModeId,    2f);
                m_material.SetFloat(s_desatStrengthId, 0f);   // desat handled by shader internally
                m_passthroughLayer?.SetBrightnessContrastSaturation(0f, 0f, 0f);
            }
            else
            {
                // Static: shader path 3, controller rectangle.
                m_material.SetFloat(s_focusModeId,    3f);
                m_material.SetColor(s_dimColorId,     m_staticDimColor);
                m_material.SetFloat(s_maxDimId,       m_staticMax);
                m_material.SetFloat(s_edgeSoftnessId, m_staticEdgeSoftness);
                m_material.SetFloat(s_desatStrengthId, 0f);
                m_passthroughLayer?.SetBrightnessContrastSaturation(0f, 0f, 0f);
                ResetRegion();
            }
            UpdateStatusText();
        }

        private void PlaceCorner(Vector3 worldPoint)
        {
            m_cornerPoints[m_cornerCount] = worldPoint;
            if (m_cornerMarkers != null && m_cornerCount < m_cornerMarkers.Length && m_cornerMarkers[m_cornerCount] != null)
            {
                m_cornerMarkers[m_cornerCount].position = worldPoint;
                m_cornerMarkers[m_cornerCount].gameObject.SetActive(true);
            }
            m_cornerCount++;
            UpdateStatusText();
        }

        private void ResetRegion()
        {
            m_cornerCount = 0;
            HideMarkers();
            m_material.SetFloat(s_regionActiveId, 0f);
            UpdateStatusText();
        }

        private void HideMarkers()
        {
            if (m_cornerMarkers == null) return;
            foreach (var marker in m_cornerMarkers)
            {
                if (marker != null) marker.gameObject.SetActive(false);
            }
        }

        private void UpdateStatusText()
        {
            if (m_debugText == null) return;
            m_debugText.text = m_mode switch
            {
                FocusMode.LightPerceptual => "Mode 1: Light Perceptual  (B = next mode)",
                FocusMode.DeepPerceptual  => "Mode 2: Deep Perceptual  (B = next mode)",
                _ => m_cornerCount >= 2
                    ? "Mode 3: Static — window set  (A = redo, B = next mode)"
                    : $"Mode 3: Static — aim + A, corner {m_cornerCount + 1}/2  (B = next mode)",
            };
        }

        private void FeedFovUniform()
        {
            float tanX, tanY;
            var intr = (m_cameraAccess != null && m_cameraAccess.IsPlaying) ? m_cameraAccess.Intrinsics : default;
            if (intr.FocalLength.x > 0f && intr.FocalLength.y > 0f && intr.SensorResolution.x > 0)
            {
                tanX = intr.SensorResolution.x / (2f * intr.FocalLength.x);
                var cur = m_cameraAccess.CurrentResolution;
                tanY = (cur.x > 0 && cur.y > 0)
                    ? tanX * ((float)cur.y / cur.x)
                    : intr.SensorResolution.y / (2f * intr.FocalLength.y);

                if (!m_loggedIntrinsics)
                {
                    m_loggedIntrinsics = true;
                    Debug.Log($"[FocusVignette] focal={intr.FocalLength} sensorRes={intr.SensorResolution} currentRes={cur}");
                }
            }
            else
            {
                float aspect = 4f / 3f;
                if (m_cameraAccess != null && m_cameraAccess.IsPlaying)
                {
                    var res = m_cameraAccess.CurrentResolution;
                    if (res.x > 0 && res.y > 0) aspect = (float)res.x / res.y;
                }
                tanX = Mathf.Tan(0.5f * m_cameraHorizontalFovDeg * Mathf.Deg2Rad);
                tanY = tanX / aspect;
            }
            m_material.SetVector(s_tanHalfFovId, new Vector4(tanX, tanY, 0f, 0f));
        }
    }
}
