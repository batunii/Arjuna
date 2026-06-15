// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.ShaderSample
{
    /// <summary>
    /// Camera-Sphere approach: the camera feed is rendered as a texture on the inside of a
    /// head-centred inverted sphere. Shader Mode 4 treats the sphere surface as the visual output:
    ///
    ///   Focus rectangle (controller-marked, A button): transparent (alpha=0) — OS passthrough
    ///   underlay shows through at full quality.
    ///
    ///   Outside rectangle: camera feed rendered opaque, increasingly blurred and desaturated as
    ///   eccentricity grows. Blur masks the camera-feed warp in the periphery.
    ///
    /// Mode 1 LIGHT — mild blur + desat, generous clear zone, effect returns quickly (2.5 s).
    /// Mode 2 DEEP  — heavy blur + desat, tighter clear zone, effect returns slowly (5.5 s):
    ///   scanning rewards the user with a long window of normal-quality vision before
    ///   the peripheral degradation reapplies.
    ///
    /// B / middle-finger pinch cycles: Light → Deep → Light.
    /// A / index-finger pinch places a focus-rectangle corner (need 2). Third press resets.
    /// </summary>
    [MetaCodeSample("PassthroughCameraApiSamples-ShaderSample")]
    public class FocusVignetteManager : MonoBehaviour
    {
        private enum FocusMode { Light, Deep }

        [SerializeField] private PassthroughCameraAccess m_cameraAccess;
        [SerializeField] private OVRPassthroughLayer m_passthroughLayer;
        [SerializeField] private MeshRenderer m_renderer;
        [SerializeField] private Text m_debugText;
        [SerializeField] private Transform m_headAnchor;
        [SerializeField] private Transform m_pointer;

        [SerializeField] private FocusMode m_mode = FocusMode.Light;

        // -----------------------------------------------------------------------------------------
        // Mode 1 Light — mild peripheral blur + desaturation
        // -----------------------------------------------------------------------------------------
        [Header("Mode 1 Light — blur")]
        [Tooltip("Max Gaussian kernel UV radius at peak periphery. ~0.012 ≈ 8 px at 640 px cam width.")]
        [SerializeField, Range(0f, 0.05f)] private float m_lightMaxBlurRadius = 0.012f;
        [Tooltip("Power-law exponent for blur ramp (1=linear from focus edge, >1=slow onset then fast).")]
        [SerializeField, Range(0.5f, 3f)]  private float m_lightBlurCurveExp  = 1.4f;

        [Header("Mode 1 Light — desaturation")]
        [Tooltip("Fraction of periphery depth at which desaturation begins (0=starts at edge, 1=never). Research: start later than blur.")]
        [SerializeField, Range(0f, 0.8f)]  private float m_lightDesatDelay    = 0.30f;
        [Tooltip("Power-law exponent for desaturation ramp.")]
        [SerializeField, Range(0.5f, 4f)]  private float m_lightDesatCurveExp = 1.8f;
        [SerializeField, Range(0f, 0.4f)]  private float m_lightContrastBoost = 0.10f;

        [Header("Mode 1 Light — focus zone")]
        [Tooltip("Soft-edge width at the focus rectangle boundary.")]
        [SerializeField, Range(0.01f, 1f)] private float m_lightEdgeSoftness  = 0.12f;
        [SerializeField]                   private Color m_lightDimColor       = Color.black;

        [Header("Mode 1 Light — motion easing")]
        [Tooltip("Seconds for peripheral degradation to return after head settles.")]
        [SerializeField] private float m_lightReapplySeconds  = 2.5f;
        [Tooltip("Head angular speed (deg/s) that triggers the reveal.")]
        [SerializeField] private float m_lightMotionThreshDeg = 30f;

        // -----------------------------------------------------------------------------------------
        // Mode 2 Deep — heavy peripheral blur + desaturation, slow return after scanning
        // -----------------------------------------------------------------------------------------
        [Header("Mode 2 Deep — blur")]
        [SerializeField, Range(0f, 0.05f)] private float m_deepMaxBlurRadius  = 0.030f;
        [SerializeField, Range(0.5f, 3f)]  private float m_deepBlurCurveExp   = 1.1f;

        [Header("Mode 2 Deep — desaturation")]
        [SerializeField, Range(0f, 0.8f)]  private float m_deepDesatDelay     = 0.10f;
        [SerializeField, Range(0.5f, 4f)]  private float m_deepDesatCurveExp  = 1.3f;
        [SerializeField, Range(0f, 0.4f)]  private float m_deepContrastBoost  = 0.20f;

        [Header("Mode 2 Deep — focus zone")]
        [SerializeField, Range(0.01f, 1f)] private float m_deepEdgeSoftness   = 0.20f;
        [SerializeField]                   private Color m_deepDimColor        = Color.black;

        [Header("Mode 2 Deep — motion easing")]
        [Tooltip("Seconds for degradation to return after head settles. Long = normal vision is preserved after each scan.")]
        [SerializeField] private float m_deepReapplySeconds   = 5.5f;
        [Tooltip("Lower threshold = eases off more readily while scanning.")]
        [SerializeField] private float m_deepMotionThreshDeg  = 22f;

        // -----------------------------------------------------------------------------------------
        // Shared
        // -----------------------------------------------------------------------------------------
        [Header("Shared — motion easing")]
        [Tooltip("Seconds to ease the effect OFF when motion is detected (same for both modes).")]
        [SerializeField] private float m_revealSeconds = 0.20f;

        [Header("Shared — mode transition")]
        [Tooltip("Seconds to smoothly interpolate shader params when switching Light ↔ Deep.")]
        [SerializeField, Range(0.1f, 3f)] private float m_transitionDuration = 1.2f;

        [Header("Shared — focus rectangle")]
        [SerializeField] private float m_selectDistance = 2.5f;
        [SerializeField] private Transform m_aimCursor;
        [SerializeField] private Transform[] m_cornerMarkers = new Transform[2];

        [SerializeField] private float m_cameraHorizontalFovDeg = 82f;

        // -----------------------------------------------------------------------------------------
        // Runtime state
        // -----------------------------------------------------------------------------------------
        private Material m_material;
        private bool m_loggedIntrinsics;

        // 0 = Light, 1 = Deep; lerps toward m_targetT on mode switch.
        private float m_blendT       = 0f;
        private float m_targetBlendT = 0f;

        private Vector3 m_prevForward = Vector3.forward;
        private float m_drIntensity   = 0f;

        private readonly Vector3[] m_cornerPoints = new Vector3[2];
        private int m_cornerCount = 0;

        // -----------------------------------------------------------------------------------------
        // Shader property IDs
        // -----------------------------------------------------------------------------------------
        private static readonly int s_mainTexId         = Shader.PropertyToID("_MainTex");
        private static readonly int s_sphereCenterId    = Shader.PropertyToID("_SphereCenter");
        private static readonly int s_headRightId       = Shader.PropertyToID("_HeadRight");
        private static readonly int s_headUpId          = Shader.PropertyToID("_HeadUp");
        private static readonly int s_headForwardId     = Shader.PropertyToID("_HeadForward");
        private static readonly int s_tanHalfFovId      = Shader.PropertyToID("_TanHalfFov");
        private static readonly int s_focusModeId       = Shader.PropertyToID("_FocusMode");
        private static readonly int s_edgeSoftnessId    = Shader.PropertyToID("_EdgeSoftness");
        private static readonly int s_drIntensityId     = Shader.PropertyToID("_DrIntensity");
        private static readonly int s_desatStrengthId   = Shader.PropertyToID("_DesatStrength");
        private static readonly int s_dimColorId        = Shader.PropertyToID("_DimColor");
        private static readonly int s_maxDimId          = Shader.PropertyToID("_MaxDim");
        private static readonly int s_regionActiveId    = Shader.PropertyToID("_RegionActive");
        private static readonly int[] s_cornerIds =
        {
            Shader.PropertyToID("_Corner0"),
            Shader.PropertyToID("_Corner1"),
        };
        private static readonly int s_maxBlurRadiusId   = Shader.PropertyToID("_MaxBlurRadius");
        private static readonly int s_blurCurveExpId    = Shader.PropertyToID("_BlurCurveExp");
        private static readonly int s_desatCurveExpId   = Shader.PropertyToID("_DesatCurveExp");
        private static readonly int s_contrastBoostId   = Shader.PropertyToID("_ContrastBoost");
        private static readonly int s_sphereDesatDelayId = Shader.PropertyToID("_SphereDesatDelay");

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

            if (m_debugText != null) m_debugText.text = "No permission granted.";

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
            if (head != null) m_prevForward = head.forward;

            // Always use shader Mode 4 on this branch.
            m_material.SetFloat(s_focusModeId,    4f);
            m_material.SetFloat(s_desatStrengthId, 0f);
            m_material.SetFloat(s_maxDimId,        1f);
            m_passthroughLayer?.SetBrightnessContrastSaturation(0f, 0f, 0f);

            ApplyMode();
        }

        private void LateUpdate()
        {
            var head = Head;
            if (head == null || m_material == null) return;

            transform.position = head.position;
            m_material.SetVector(s_sphereCenterId, head.position);
            m_material.SetVector(s_headRightId,    head.right);
            m_material.SetVector(s_headUpId,       head.up);
            m_material.SetVector(s_headForwardId,  head.forward);
            FeedFovUniform();

            // B / middle-finger pinch toggles Light ↔ Deep.
            if (InputManager.IsButtonBDownOrMiddleFingerPinchStarted())
            {
                m_mode = m_mode == FocusMode.Light ? FocusMode.Deep : FocusMode.Light;
                ApplyMode();
            }

            // A / index-finger pinch places / resets focus rectangle corners.
            if (InputManager.IsButtonADownOrPinchStarted())
            {
                if (m_cornerCount >= 2)
                {
                    ResetCorners();
                }
                else
                {
                    PlaceCorner();
                }
            }

            UpdatePerceptual(head);
        }

        private void UpdatePerceptual(Transform head)
        {
            // Advance blend toward target (Light=0, Deep=1).
            float rate = m_transitionDuration > 0f ? Time.deltaTime / m_transitionDuration : 1f;
            m_blendT = Mathf.MoveTowards(m_blendT, m_targetBlendT, rate);

            float t = m_blendT;

            // Lerp all shader params between Light and Deep presets.
            m_material.SetFloat(s_maxBlurRadiusId,    Mathf.Lerp(m_lightMaxBlurRadius, m_deepMaxBlurRadius, t));
            m_material.SetFloat(s_blurCurveExpId,     Mathf.Lerp(m_lightBlurCurveExp,  m_deepBlurCurveExp,  t));
            m_material.SetFloat(s_sphereDesatDelayId, Mathf.Lerp(m_lightDesatDelay,    m_deepDesatDelay,    t));
            m_material.SetFloat(s_desatCurveExpId,    Mathf.Lerp(m_lightDesatCurveExp, m_deepDesatCurveExp, t));
            m_material.SetFloat(s_contrastBoostId,    Mathf.Lerp(m_lightContrastBoost, m_deepContrastBoost, t));
            m_material.SetFloat(s_edgeSoftnessId,     Mathf.Lerp(m_lightEdgeSoftness,  m_deepEdgeSoftness,  t));
            m_material.SetColor(s_dimColorId,         Color.Lerp(m_lightDimColor,      m_deepDimColor,      t));

            // Push focus rectangle corners to shader.
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

            // Aim cursor while placing corners.
            var pointer = Pointer;
            if (m_aimCursor != null)
            {
                bool placing = m_cornerCount < 2;
                if (m_aimCursor.gameObject.activeSelf != placing)
                    m_aimCursor.gameObject.SetActive(placing);
                if (placing)
                    m_aimCursor.position = pointer.position + pointer.forward * m_selectDistance;
            }

            // Motion easing: ease OFF while scanning, ON when settled.
            // Mode 2 Deep reapply is slow — normal vision persists longer after each scan.
            float angle = Vector3.Angle(head.forward, m_prevForward);
            float speed = Time.deltaTime > 0f ? angle / Time.deltaTime : 0f;
            m_prevForward = head.forward;

            float motionThresh  = Mathf.Lerp(m_lightMotionThreshDeg, m_deepMotionThreshDeg, t);
            float reapplyTime   = Mathf.Lerp(m_lightReapplySeconds,  m_deepReapplySeconds,  t);

            float intensityTarget = speed > motionThresh ? 0f : 1f;
            float intensityRate   = intensityTarget < m_drIntensity
                ? 1f / Mathf.Max(m_revealSeconds, 0.01f)
                : 1f / Mathf.Max(reapplyTime,     0.01f);
            m_drIntensity = Mathf.MoveTowards(m_drIntensity, intensityTarget, intensityRate * Time.deltaTime);
            m_material.SetFloat(s_drIntensityId, m_drIntensity);
        }

        private void ApplyMode()
        {
            m_targetBlendT = m_mode == FocusMode.Deep ? 1f : 0f;
            UpdateStatusText();
        }

        private void PlaceCorner()
        {
            var pointer = Pointer;
            Vector3 point = pointer.position + pointer.forward * m_selectDistance;
            m_cornerPoints[m_cornerCount] = point;

            if (m_cornerMarkers != null && m_cornerCount < m_cornerMarkers.Length && m_cornerMarkers[m_cornerCount] != null)
            {
                m_cornerMarkers[m_cornerCount].position = point;
                m_cornerMarkers[m_cornerCount].gameObject.SetActive(true);
            }
            m_cornerCount++;
            UpdateStatusText();
        }

        private void ResetCorners()
        {
            m_cornerCount = 0;
            if (m_cornerMarkers != null)
            {
                foreach (var m in m_cornerMarkers)
                {
                    if (m != null) m.gameObject.SetActive(false);
                }
            }
            m_material.SetFloat(s_regionActiveId, 0f);
            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            if (m_debugText == null) return;
            string modeLabel = m_mode == FocusMode.Light
                ? "Mode 1: Light  |  blur=mild  reapply=2.5s"
                : "Mode 2: Deep   |  blur=heavy  reapply=5.5s";
            string cornerState = m_cornerCount >= 2
                ? "window set (A=redo)"
                : $"aim + A for corner {m_cornerCount + 1}/2";
            m_debugText.text = $"{modeLabel}  |  {cornerState}  |  B=toggle";
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
