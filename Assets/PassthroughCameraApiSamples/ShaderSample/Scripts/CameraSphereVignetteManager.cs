// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.ShaderSample
{
    public enum VignetteMode { Blur = 0, SoftDark = 1, HardDark = 2 }

    [System.Serializable]
    public struct MotionSettings
    {
        [Tooltip("Head angular speed (deg/s) that triggers suppression.")]
        [Range(5f, 180f)]  public float speedThreshDeg;
        [Tooltip("Seconds the suppression holds after speed drops below threshold.")]
        [Range(0f, 5f)]    public float holdSeconds;
    }

    [MetaCodeSample("PassthroughCameraApiSamples-ShaderSample")]
    public class CameraSphereVignetteManager : MonoBehaviour
    {
        [SerializeField] private PassthroughCameraAccess m_cameraAccess;
        [SerializeField] private PassthroughCameraAccess m_cameraAccessRight;
        // Named m_renderer to preserve scene serialization from the prior version.
        [SerializeField] private MeshRenderer            m_renderer;

        [Tooltip("OVRCameraRig's right controller anchor (for aim direction).")]
        [SerializeField] private Transform m_rightControllerAnchor;

        [Tooltip("Optional: OVRCameraRig root.")]
        [SerializeField] private Transform m_cameraRig;

        [SerializeField] private Text m_debugText;

        [Header("Filter")]
        [SerializeField, Range(1f, 45f)]   private float m_softEdgeDeg   = 20f;
        [SerializeField, Range(0f, 0.15f)] private float m_maxBlurRadius = 0.01f;
        [SerializeField, Range(0.5f, 8f)]  private float m_blurCurveExp  = 4f;
        [SerializeField, Range(0f, 0.9f)]  private float m_desatDelay    = 0.3f;
        [SerializeField, Range(0.5f, 4f)]  private float m_desatCurveExp = 3f;

        // m_selectionLine kept for scene serialisation compatibility — disabled at runtime
        [SerializeField] private LineRenderer m_selectionLine;

        [Header("Mode")]
        [SerializeField] private VignetteMode m_vignetteMode    = VignetteMode.Blur;
        [SerializeField, Range(0.5f, 10f)]   private float m_vignetteFormTime = 3f;
        [SerializeField, Range(0.1f, 0.95f)] private float m_mode2MaxAlpha    = 0.75f;

        [Header("Motion Disable — Per Mode")]
        [SerializeField] private MotionSettings m_motionBlur     = new MotionSettings { speedThreshDeg = 30f, holdSeconds = 0.6f };
        [SerializeField] private MotionSettings m_motionSoftDark = new MotionSettings { speedThreshDeg = 50f, holdSeconds = 1.0f };
        [SerializeField] private MotionSettings m_motionHardDark = new MotionSettings { speedThreshDeg = 70f, holdSeconds = 1.5f };

        [Tooltip("Seconds for the effect to fade OUT when suppression starts.")]
        [SerializeField, Range(0.05f, 2f)] private float m_motionFadeOutSec = 0.20f;
        [Tooltip("Seconds for the effect to fade back IN after suppression ends.")]
        [SerializeField, Range(0.05f, 3f)] private float m_motionFadeInSec  = 0.70f;

        [Header("Selection Dots")]
        [SerializeField] private Material m_dotMaterialTemplate;
        [SerializeField] private float    m_dotSize       = 0.055f;
        [Tooltip("Seconds after trigger-release before corner dots shrink away.")]
        [SerializeField, Range(1f, 10f)]  private float m_dotHideDelay   = 3f;
        [SerializeField] private Color    m_dotColorHeld   = Color.white;
        [SerializeField] private Color    m_dotColorLocked = new Color(1f, 1f, 1f, 0.55f);
        [SerializeField] private Color    m_dotColorCursor = new Color(1f, 0.9f, 0.3f, 1f);

        [Header("Debug")]
        [SerializeField] private bool m_debugCamOverlay;

        [Header("YOLO Detection")]
        [Tooltip("Optional — wire up to enable automatic clear zones for detected objects.")]
        [SerializeField] private YoloRunner m_yoloRunner;
        [SerializeField] private float      m_detectionLifetime = 0.6f;

        [SerializeField] private float m_cameraHorizontalFovDeg = 82f;

        // ---- shader property IDs ----
        private static readonly int s_mainTexLId         = Shader.PropertyToID("_MainTexL");
        private static readonly int s_mainTexRId         = Shader.PropertyToID("_MainTexR");
        private static readonly int s_sphereCenterId     = Shader.PropertyToID("_SphereCenter");
        private static readonly int s_camLFwdId          = Shader.PropertyToID("_CamLFwd");
        private static readonly int s_camLRtId           = Shader.PropertyToID("_CamLRt");
        private static readonly int s_camLUpId           = Shader.PropertyToID("_CamLUp");
        private static readonly int s_tanHalfFovLId      = Shader.PropertyToID("_TanHalfFovL");
        private static readonly int s_camRFwdId          = Shader.PropertyToID("_CamRFwd");
        private static readonly int s_camRRtId           = Shader.PropertyToID("_CamRRt");
        private static readonly int s_camRUpId           = Shader.PropertyToID("_CamRUp");
        private static readonly int s_tanHalfFovRId      = Shader.PropertyToID("_TanHalfFovR");
        private static readonly int s_hasRightCamId      = Shader.PropertyToID("_HasRightCam");
        private static readonly int s_focusRectId        = Shader.PropertyToID("_FocusRect");
        private static readonly int s_softEdgeId         = Shader.PropertyToID("_SoftEdge");
        private static readonly int s_maxBlurRadId       = Shader.PropertyToID("_MaxBlurRadius");
        private static readonly int s_blurCurveExpId     = Shader.PropertyToID("_BlurCurveExp");
        private static readonly int s_desatDelayId       = Shader.PropertyToID("_DesatDelay");
        private static readonly int s_desatCurveExpId    = Shader.PropertyToID("_DesatCurveExp");
        private static readonly int s_detectionCountId   = Shader.PropertyToID("_DetectionCount");
        private static readonly int s_detectionRectsId   = Shader.PropertyToID("_DetectionRects");
        private static readonly int s_debugCamOverlayId  = Shader.PropertyToID("_DebugCamOverlay");
        private static readonly int s_simpleModeId       = Shader.PropertyToID("_SimpleMode");
        private static readonly int s_vignetteStrengthId = Shader.PropertyToID("_VignetteStrength");
        private static readonly int s_maxVignetteAlphaId = Shader.PropertyToID("_MaxVignetteAlpha");

        private static readonly Vector4 k_fullSphere =
            new(-Mathf.PI, Mathf.PI, -Mathf.PI * 0.5f, Mathf.PI * 0.5f);

        private static readonly HashSet<int> k_targetClasses = new() { 0, 1, 2, 3, 5, 6, 9, 11 };

        private const int k_maxDetections = 8;
        private readonly Vector4[] m_detectionRects     = new Vector4[k_maxDetections];
        private readonly float[]   m_detectionTimestamp = new float[k_maxDetections];

        private Material m_material;
        private bool     m_rightCamTexSet;
        private bool     m_loggedIntrinsics;

        // Selection dot GameObjects: [0-3] corners, [4] cursor
        private GameObject[] m_selectionDots;
        private Material[]   m_dotMats;
        private const float  k_dotDistance = 4f;

        // Cached for YOLO coordinate conversion
        private Vector3 m_headRight, m_headUp, m_headForward;
        private Vector2 m_tanHalfFov;

        private Vector4 m_activeRect = k_fullSphere;

        // Mode / formation
        private float     m_vignetteStrength = 1f;
        private Coroutine m_formCoroutine;

        // Motion disable
        private Quaternion m_lastHeadRot;
        private float      m_motionDisableTimer;
        // 0 = effect fully on, 1 = fully suppressed — animated smoothly
        private float      m_motionSuppression;

        // Dot auto-hide
        private Coroutine m_dotHideCoroutine;
        private bool      m_cornerDotsHidden;

        // Mode indicator UI (created at runtime)
        private GameObject  m_modeUIRoot;
        private CanvasGroup m_modeUIGroup;
        private Text        m_modeNameText;
        private Text        m_modeHintText;
        private float       m_modeUITimer;
        private const float k_modeUIShowTime = 2.8f;
        private const float k_modeUIFadeDur  = 0.35f;

        // ---- Unity lifecycle ----

        private IEnumerator Start()
        {
            InitSelectionDots();
            InitModeUI();

            m_material = m_renderer.material;
            m_material.SetVector(s_focusRectId, m_activeRect);
            m_material.SetFloat(s_hasRightCamId, 0f);

            foreach (var cam in Camera.allCameras)
            {
                cam.clearFlags      = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.clear;
            }

            var ptLayer = FindObjectOfType<OVRPassthroughLayer>();
            if (ptLayer != null) ptLayer.enabled = true;

            if (m_selectionLine != null)
            {
                m_selectionLine.loop          = true;
                m_selectionLine.useWorldSpace = true;
                m_selectionLine.startWidth    = 0.005f;
                m_selectionLine.endWidth      = 0.005f;
                m_selectionLine.enabled       = false;
            }

            if (Camera.main != null) m_lastHeadRot = Camera.main.transform.rotation;

            SetDebug("Waiting for camera...");

            if (!OVRPermissionsRequester.IsPermissionGranted(
                    OVRPermissionsRequester.Permission.PassthroughCameraAccess))
                OVRPermissionsRequester.Request(
                    new[] { OVRPermissionsRequester.Permission.PassthroughCameraAccess });

            while (!OVRPermissionsRequester.IsPermissionGranted(
                       OVRPermissionsRequester.Permission.PassthroughCameraAccess))
                yield return null;

            while (!m_cameraAccess.IsPlaying)
                yield return null;

            m_material.SetTexture(s_mainTexLId, m_cameraAccess.GetTexture());
            SetDebug("Trigger: paint  [A]: mode  [B]: clear");

            if (m_yoloRunner != null)
                m_yoloRunner.OnDetectionsReady += OnDetectionsReady;

            ShowModeToast();
        }

        private void OnDestroy()
        {
            if (m_yoloRunner != null)
                m_yoloRunner.OnDetectionsReady -= OnDetectionsReady;
            if (m_selectionDots != null)
                foreach (var go in m_selectionDots) if (go != null) Destroy(go);
            if (m_dotMats != null)
                foreach (var mat in m_dotMats) if (mat != null) Destroy(mat);
            if (m_modeUIRoot != null)
                Destroy(m_modeUIRoot);
        }

        private void LateUpdate()
        {
            if (m_material == null) return;

            UpdateSpherePosition();
            FeedCameraUniforms();
            UpdateFilterUniforms();
            UpdateDetectionUniforms();
            UpdateMotionDisable();
            HandleSelection();
            UpdateModeUniforms();
            UpdateModeUI();
        }

        // ---- sphere + head ----

        private void UpdateSpherePosition()
        {
            Transform head = Camera.main != null ? Camera.main.transform : transform;
            transform.position = head.position;
            m_material.SetVector(s_sphereCenterId, head.position);
            m_headForward = head.forward;
            m_headRight   = head.right;
            m_headUp      = head.up;
        }

        // ---- dual-camera uniforms ----

        private void FeedCameraUniforms()
        {
            FeedOneCameraUniforms(m_cameraAccess, isLeft: true);

            bool hasRight = m_cameraAccessRight != null && m_cameraAccessRight.IsPlaying;
            m_material.SetFloat(s_hasRightCamId, hasRight ? 1f : 0f);

            if (m_debugCamOverlay && m_debugText != null)
                m_debugText.text = $"L:{m_cameraAccess.IsPlaying}  R:{hasRight}";

            if (hasRight)
            {
                FeedOneCameraUniforms(m_cameraAccessRight, isLeft: false);

                if (!m_rightCamTexSet)
                {
                    m_material.SetTexture(s_mainTexRId, m_cameraAccessRight.GetTexture());
                    m_rightCamTexSet = true;
                    var pose = m_cameraAccessRight.GetCameraPose();
                    Debug.Log($"[CameraVignette] Right cam LIVE — fwd={pose.rotation * Vector3.forward:F3}  intr={m_cameraAccessRight.Intrinsics.FocalLength}");
                }
            }
            else if (m_cameraAccessRight != null && !m_rightCamTexSet)
            {
                Debug.Log($"[CameraVignette] Right cam NOT playing yet (IsPlaying={m_cameraAccessRight?.IsPlaying})");
            }
        }

        private void FeedOneCameraUniforms(PassthroughCameraAccess cam, bool isLeft)
        {
            Vector3 fwd, rt, up;
            float   tanX, tanY;

            if (cam.IsPlaying)
            {
                var pose = cam.GetCameraPose();
                fwd = pose.rotation * Vector3.forward;
                rt  = pose.rotation * Vector3.right;
                up  = pose.rotation * Vector3.up;

                var intr = cam.Intrinsics;
                if (intr.FocalLength.x > 0f && intr.SensorResolution.x > 0)
                {
                    tanX = intr.SensorResolution.x / (2f * intr.FocalLength.x);
                    var cur = cam.CurrentResolution;
                    tanY = (cur.x > 0 && cur.y > 0)
                        ? tanX * ((float)cur.y / cur.x)
                        : intr.SensorResolution.y / (2f * intr.FocalLength.y);

                    if (!m_loggedIntrinsics && isLeft)
                    {
                        m_loggedIntrinsics = true;
                        Debug.Log($"[CameraVignette] L focal={intr.FocalLength} sensor={intr.SensorResolution}");
                    }
                }
                else
                {
                    float aspect = cam.CurrentResolution.x > 0
                        ? (float)cam.CurrentResolution.x / cam.CurrentResolution.y
                        : 4f / 3f;
                    tanX = Mathf.Tan(0.5f * m_cameraHorizontalFovDeg * Mathf.Deg2Rad);
                    tanY = tanX / aspect;
                }
            }
            else
            {
                Transform head = Camera.main != null ? Camera.main.transform : transform;
                fwd  = head.forward;
                rt   = head.right;
                up   = head.up;
                tanX = Mathf.Tan(0.5f * m_cameraHorizontalFovDeg * Mathf.Deg2Rad);
                tanY = tanX / (4f / 3f);
            }

            if (isLeft)
            {
                m_material.SetVector(s_camLFwdId,     fwd);
                m_material.SetVector(s_camLRtId,      rt);
                m_material.SetVector(s_camLUpId,      up);
                m_material.SetVector(s_tanHalfFovLId, new Vector4(tanX, tanY, 0f, 0f));
                m_tanHalfFov = new Vector2(tanX, tanY);
            }
            else
            {
                m_material.SetVector(s_camRFwdId,     fwd);
                m_material.SetVector(s_camRRtId,      rt);
                m_material.SetVector(s_camRUpId,      up);
                m_material.SetVector(s_tanHalfFovRId, new Vector4(tanX, tanY, 0f, 0f));
            }
        }

        private void UpdateFilterUniforms()
        {
            m_material.SetFloat(s_softEdgeId,        m_softEdgeDeg   * Mathf.Deg2Rad);
            m_material.SetFloat(s_maxBlurRadId,      m_maxBlurRadius);
            m_material.SetFloat(s_blurCurveExpId,    m_blurCurveExp);
            m_material.SetFloat(s_desatDelayId,      m_desatDelay);
            m_material.SetFloat(s_desatCurveExpId,   m_desatCurveExp);
            m_material.SetFloat(s_debugCamOverlayId, m_debugCamOverlay ? 1f : 0f);
        }

        // ---- motion-based disable ----

        private MotionSettings CurrentMotionSettings => m_vignetteMode switch
        {
            VignetteMode.Blur     => m_motionBlur,
            VignetteMode.SoftDark => m_motionSoftDark,
            _                     => m_motionHardDark
        };

        // 15 degrees extra margin so the effect restores just before they fully centre on the rect
        private const float k_focusArrivalMarginRad = 0.2618f;

        private void UpdateMotionDisable()
        {
            Transform head = Camera.main != null ? Camera.main.transform : transform;
            float angularSpeed = Quaternion.Angle(m_lastHeadRot, head.rotation)
                                 / Mathf.Max(Time.deltaTime, 0.001f);
            m_lastHeadRot = head.rotation;

            // Check whether the head direction is inside the focus rect + arrival margin.
            // When true: cancel hold immediately so effect resumes without waiting.
            bool headInFocus = false;
            if (m_activeRect != k_fullSphere)
            {
                float headAz = Mathf.Atan2(m_headForward.x, m_headForward.z);
                float headEl = Mathf.Asin(Mathf.Clamp(m_headForward.y, -1f, 1f));
                headInFocus  = headAz >= m_activeRect.x - k_focusArrivalMarginRad
                            && headAz <= m_activeRect.y + k_focusArrivalMarginRad
                            && headEl >= m_activeRect.z - k_focusArrivalMarginRad
                            && headEl <= m_activeRect.w + k_focusArrivalMarginRad;
            }

            var s = CurrentMotionSettings;
            if (headInFocus)
            {
                // User is looking at the region: cancel hold immediately, start restoring
                m_motionDisableTimer = 0f;
            }
            else if (angularSpeed > s.speedThreshDeg)
            {
                // Moving away (or any fast motion outside the rect): re-arm hold
                m_motionDisableTimer = s.holdSeconds;
            }
            else if (m_motionDisableTimer > 0f)
            {
                m_motionDisableTimer -= Time.deltaTime;
            }

            // Animate suppression gradually (0 = full effect, 1 = fully suppressed)
            float target    = m_motionDisableTimer > 0f ? 1f : 0f;
            float fadeSpeed = target > m_motionSuppression
                ? 1f / Mathf.Max(m_motionFadeOutSec, 0.001f)
                : 1f / Mathf.Max(m_motionFadeInSec,  0.001f);
            m_motionSuppression = Mathf.MoveTowards(m_motionSuppression, target, fadeSpeed * Time.deltaTime);
        }

        // ---- selection (paint-while-holding) ----

        private bool        m_isPainting;
        private const float k_brushPad = 0.12f;

        private void HandleSelection()
        {
            bool held         = OVRInput.Get(OVRInput.RawButton.RIndexTrigger);
            bool justPressed  = OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger);
            bool justReleased = OVRInput.GetUp(OVRInput.RawButton.RIndexTrigger);
            bool aPressed     = OVRInput.GetDown(OVRInput.RawButton.A);
            bool bPressed     = OVRInput.GetDown(OVRInput.RawButton.B);

            // A button: cycle mode
            if (aPressed)
            {
                m_vignetteMode = (VignetteMode)(((int)m_vignetteMode + 1) % 3);
                // Reset motion state so the new mode's thresholds apply from a clean slate
                m_motionDisableTimer = 0f;
                m_motionSuppression  = 0f;
                bool hasRect         = m_activeRect != k_fullSphere;

                if (m_vignetteMode == VignetteMode.Blur)
                {
                    StopFormCoroutine();
                    m_vignetteStrength = 1f;
                }
                else
                {
                    StopFormCoroutine();
                    m_vignetteStrength = 0f;
                    // If a region is already selected, begin formation automatically
                    if (hasRect)
                        m_formCoroutine = StartCoroutine(FormVignette());
                }
                ShowModeToast();
            }

            // B button: clear selection + reset
            if (bPressed)
            {
                m_activeRect = k_fullSphere;
                m_material.SetVector(s_focusRectId, m_activeRect);
                StopFormCoroutine();
                m_vignetteStrength = m_vignetteMode == VignetteMode.Blur ? 1f : 0f;
                m_isPainting       = false;
                CancelDotHide();
                HideCornerDotsImmediate();
            }

            GetControllerAzEl(out float az, out float el);

            if (justPressed)
            {
                // New selection: show dots again, cancel any pending hide, reset formation
                CancelDotHide();
                m_cornerDotsHidden = false;
                RestoreDotsScale();

                if (m_vignetteMode != VignetteMode.Blur)
                {
                    StopFormCoroutine();
                    m_vignetteStrength = 0f;
                }

                m_activeRect = new Vector4(
                    az - k_brushPad, az + k_brushPad,
                    el - k_brushPad, el + k_brushPad);
                m_material.SetVector(s_focusRectId, m_activeRect);
                m_isPainting = true;
            }
            else if (held && m_isPainting)
            {
                m_activeRect = new Vector4(
                    Mathf.Min(m_activeRect.x, az - k_brushPad),
                    Mathf.Max(m_activeRect.y, az + k_brushPad),
                    Mathf.Min(m_activeRect.z, el - k_brushPad),
                    Mathf.Max(m_activeRect.w, el + k_brushPad));
                m_material.SetVector(s_focusRectId, m_activeRect);
            }

            // On release: lock the selection, start vignette + dot-hide countdown
            if (justReleased && m_isPainting && m_activeRect != k_fullSphere)
            {
                if (m_vignetteMode == VignetteMode.Blur)
                    m_vignetteStrength = 1f;
                else
                {
                    StopFormCoroutine();
                    m_formCoroutine = StartCoroutine(FormVignette());
                }
                m_dotHideCoroutine = StartCoroutine(HideDotsCoro());
            }

            if (!held) m_isPainting = false;

            DrawPointerAndBorder(az, el, held);
        }

        // ---- dot auto-hide ----

        private void CancelDotHide()
        {
            if (m_dotHideCoroutine == null) return;
            StopCoroutine(m_dotHideCoroutine);
            m_dotHideCoroutine = null;
        }

        private void HideCornerDotsImmediate()
        {
            m_cornerDotsHidden = true;
            if (m_selectionDots == null) return;
            for (int i = 0; i < 4; i++)
                if (m_selectionDots[i] != null) m_selectionDots[i].SetActive(false);
        }

        private void RestoreDotsScale()
        {
            if (m_selectionDots == null) return;
            for (int i = 0; i < 4; i++)
                if (m_selectionDots[i] != null)
                    m_selectionDots[i].transform.localScale = Vector3.one * m_dotSize;
        }

        private IEnumerator HideDotsCoro()
        {
            yield return new WaitForSeconds(m_dotHideDelay);

            // Quick scale-shrink so the dots pop away cleanly
            float elapsed = 0f, dur = 0.3f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float s = Mathf.Lerp(m_dotSize, 0f, Mathf.SmoothStep(0f, 1f, elapsed / dur));
                if (m_selectionDots != null)
                    for (int i = 0; i < 4; i++)
                        if (m_selectionDots[i] != null)
                            m_selectionDots[i].transform.localScale = Vector3.one * s;
                yield return null;
            }

            m_cornerDotsHidden = true;
            if (m_selectionDots != null)
                for (int i = 0; i < 4; i++)
                    if (m_selectionDots[i] != null) m_selectionDots[i].SetActive(false);
            m_dotHideCoroutine = null;
        }

        // ---- vignette formation ----

        private void StopFormCoroutine()
        {
            if (m_formCoroutine == null) return;
            StopCoroutine(m_formCoroutine);
            m_formCoroutine = null;
        }

        private IEnumerator FormVignette()
        {
            float elapsed = 0f;
            while (elapsed < m_vignetteFormTime)
            {
                elapsed           += Time.deltaTime;
                m_vignetteStrength = Mathf.SmoothStep(0f, 1f, elapsed / m_vignetteFormTime);
                yield return null;
            }
            m_vignetteStrength = 1f;
            m_formCoroutine    = null;
        }

        private void UpdateModeUniforms()
        {
            bool  isSimple          = m_vignetteMode != VignetteMode.Blur;
            // Motion suppression fades effect out/in smoothly; formation animates 0→1
            float effectiveStrength = m_vignetteStrength * (1f - m_motionSuppression);
            float maxAlpha          = m_vignetteMode == VignetteMode.HardDark ? 1f : m_mode2MaxAlpha;

            m_material.SetFloat(s_simpleModeId,       isSimple ? 1f : 0f);
            m_material.SetFloat(s_vignetteStrengthId, effectiveStrength);
            m_material.SetFloat(s_maxVignetteAlphaId, maxAlpha);
        }

        // ---- controller aim ----

        private void GetControllerAzEl(out float az, out float el)
        {
            Vector3 worldDir;

            if (m_rightControllerAnchor != null)
            {
                worldDir = m_rightControllerAnchor.forward;
            }
            else
            {
                var rot = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);
                worldDir = m_cameraRig != null
                    ? m_cameraRig.TransformDirection(rot * Vector3.forward)
                    : rot * Vector3.forward;
            }

            worldDir = worldDir.normalized;
            az = Mathf.Atan2(worldDir.x, worldDir.z);
            el = Mathf.Asin(Mathf.Clamp(worldDir.y, -1f, 1f));
        }

        // ---- selection dots ----

        private void InitSelectionDots()
        {
            if (m_selectionLine != null) m_selectionLine.enabled = false;

            m_selectionDots = new GameObject[5];
            m_dotMats       = new Material[5];
            for (int i = 0; i < 5; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = i < 4 ? $"SelCorner{i}" : "SelCursor";
                Destroy(go.GetComponent<SphereCollider>());

                var mat = m_dotMaterialTemplate != null
                    ? new Material(m_dotMaterialTemplate)
                    : new Material(Shader.Find("Standard"));
                mat.renderQueue = 4000;
                mat.color       = i == 4 ? m_dotColorCursor : m_dotColorHeld;

                go.GetComponent<MeshRenderer>().sharedMaterial = mat;
                go.transform.localScale = Vector3.one * m_dotSize;
                go.SetActive(i == 4);

                m_selectionDots[i] = go;
                m_dotMats[i]       = mat;
            }
        }

        private void DrawPointerAndBorder(float az, float el, bool holding)
        {
            if (m_selectionDots == null) return;

            Transform head = Camera.main != null ? Camera.main.transform : transform;
            Vector3 org = head.position;

            m_selectionDots[4].transform.position = org + DirFromAzEl(az, el) * k_dotDistance;

            // Corner dots managed separately when auto-hide is running
            if (!m_cornerDotsHidden)
            {
                bool hasBorder = m_activeRect != k_fullSphere;
                for (int i = 0; i < 4; i++) m_selectionDots[i].SetActive(hasBorder);

                if (hasBorder)
                {
                    float azMin = m_activeRect.x, azMax = m_activeRect.y;
                    float elMin = m_activeRect.z, elMax = m_activeRect.w;
                    m_selectionDots[0].transform.position = org + DirFromAzEl(azMin, elMin) * k_dotDistance;
                    m_selectionDots[1].transform.position = org + DirFromAzEl(azMax, elMin) * k_dotDistance;
                    m_selectionDots[2].transform.position = org + DirFromAzEl(azMax, elMax) * k_dotDistance;
                    m_selectionDots[3].transform.position = org + DirFromAzEl(azMin, elMax) * k_dotDistance;

                    Color c = holding ? m_dotColorHeld : m_dotColorLocked;
                    for (int i = 0; i < 4; i++) m_dotMats[i].color = c;
                }
            }
        }

        // ---- mode indicator UI ----

        private void InitModeUI()
        {
            m_modeUIRoot = new GameObject("ModeIndicatorUI");

            var canvas = m_modeUIRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            // Prevent scale-warning from CanvasScaler; we size manually via localScale
            m_modeUIRoot.AddComponent<CanvasScaler>();

            var rt = m_modeUIRoot.GetComponent<RectTransform>();
            rt.sizeDelta  = new Vector2(540, 150);
            rt.localScale = Vector3.one * 0.001f; // → ~0.54 m × 0.15 m at 1 m

            m_modeUIGroup = m_modeUIRoot.AddComponent<CanvasGroup>();
            m_modeUIGroup.alpha          = 0f;
            m_modeUIGroup.blocksRaycasts = false;
            m_modeUIGroup.interactable   = false;

            // Dark background panel
            var bg   = CreateChild(m_modeUIRoot, "BG");
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.05f, 0.05f, 0.05f, 0.82f);
            StretchFill(bg);

            // Accent stripe at the top (coloured by mode)
            var stripe    = CreateChild(m_modeUIRoot, "Stripe");
            var stripeImg = stripe.AddComponent<Image>();
            stripeImg.color = ModeAccentColor();
            var stripeRt = stripe.GetComponent<RectTransform>();
            stripeRt.anchorMin = new Vector2(0f, 0.88f);
            stripeRt.anchorMax = Vector2.one;
            stripeRt.offsetMin = stripeRt.offsetMax = Vector2.zero;

            // Mode name (large, upper half)
            var nameGO   = CreateChild(m_modeUIRoot, "ModeName");
            m_modeNameText = nameGO.AddComponent<Text>();
            m_modeNameText.font      = BuiltinFont();
            m_modeNameText.fontSize  = 46;
            m_modeNameText.fontStyle = FontStyle.Bold;
            m_modeNameText.alignment = TextAnchor.MiddleCenter;
            m_modeNameText.color     = Color.white;
            var nameRt = nameGO.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.38f);
            nameRt.anchorMax = new Vector2(1f, 0.88f);
            nameRt.offsetMin = nameRt.offsetMax = Vector2.zero;

            // Hint / description (small, lower third)
            var hintGO  = CreateChild(m_modeUIRoot, "Hint");
            m_modeHintText = hintGO.AddComponent<Text>();
            m_modeHintText.font      = BuiltinFont();
            m_modeHintText.fontSize  = 21;
            m_modeHintText.alignment = TextAnchor.MiddleCenter;
            m_modeHintText.color     = new Color(1f, 1f, 1f, 0.6f);
            var hintRt = hintGO.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0f, 0f);
            hintRt.anchorMax = new Vector2(1f, 0.4f);
            hintRt.offsetMin = hintRt.offsetMax = Vector2.zero;

            // Start off-screen
            m_modeUIRoot.transform.position = Vector3.zero;
        }

        private void ShowModeToast()
        {
            if (m_modeNameText == null) return;

            // Mode pip indicator  e.g. "●  ○  ○" / "○  ●  ○" / "○  ○  ●"
            string pip = m_vignetteMode switch
            {
                VignetteMode.Blur     => "●  ○  ○",
                VignetteMode.SoftDark => "○  ●  ○",
                _                     => "○  ○  ●"
            };
            string modeName = m_vignetteMode switch
            {
                VignetteMode.Blur     => "BLUR VIGNETTE",
                VignetteMode.SoftDark => "SOFT DARK",
                _                     => "HARD DARK"
            };
            string desc = m_vignetteMode switch
            {
                VignetteMode.Blur     => "Blurred & desaturated periphery",
                VignetteMode.SoftDark => $"Gradual dark vignette  ({(int)(m_mode2MaxAlpha * 100)}% max)",
                _                     => "Full black-out vignette"
            };

            m_modeNameText.text = $"{pip}     {modeName}";
            m_modeHintText.text = $"{desc}     [A] cycle  [B] clear";

            // Update accent stripe colour to match mode
            var stripe = m_modeUIRoot.transform.Find("Stripe");
            if (stripe != null)
            {
                var img = stripe.GetComponent<Image>();
                if (img != null) img.color = ModeAccentColor();
            }

            m_modeUITimer = k_modeUIShowTime;
        }

        private Color ModeAccentColor()
        {
            return m_vignetteMode switch
            {
                VignetteMode.Blur     => new Color(0.25f, 0.55f, 1.00f, 1f), // blue
                VignetteMode.SoftDark => new Color(1.00f, 0.65f, 0.10f, 1f), // amber
                _                     => new Color(0.90f, 0.15f, 0.15f, 1f)  // red
            };
        }

        private void UpdateModeUI()
        {
            if (m_modeUIRoot == null) return;

            Transform head = Camera.main != null ? Camera.main.transform : transform;

            // Lazy-follow: 1.5 m forward, 0.3 m below eye level
            Vector3 target = head.position + head.forward * 1.5f + Vector3.down * 0.30f;
            m_modeUIRoot.transform.position = Vector3.Lerp(
                m_modeUIRoot.transform.position, target, Time.deltaTime * 9f);

            // Billboard: panel's +Z points away from the head so the front face is visible
            Vector3 away = m_modeUIRoot.transform.position - head.position;
            if (away.sqrMagnitude > 0.001f)
                m_modeUIRoot.transform.rotation = Quaternion.LookRotation(away, Vector3.up);

            // Fade in then fade out
            if (m_modeUITimer > 0f)
            {
                m_modeUITimer -= Time.deltaTime;
                float fadeIn  = Mathf.Clamp01((k_modeUIShowTime - m_modeUITimer) / k_modeUIFadeDur);
                float fadeOut = Mathf.Clamp01(m_modeUITimer / k_modeUIFadeDur);
                m_modeUIGroup.alpha = Mathf.Min(fadeIn, fadeOut);
            }
            else
            {
                m_modeUIGroup.alpha = 0f;
            }
        }

        private static GameObject CreateChild(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void StretchFill(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static Font BuiltinFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }

        // ---- YOLO detection zones ----

        private void OnDetectionsReady(
            IReadOnlyList<(int classId, Vector4 box)> detections, Vector2Int inputSize)
        {
            int slot = 0;
            foreach (var (classId, box) in detections)
            {
                if (slot >= k_maxDetections) break;
                if (!k_targetClasses.Contains(classId)) continue;
                m_detectionRects[slot]     = BoxToAzElRect(box, inputSize);
                m_detectionTimestamp[slot] = Time.time;
                slot++;
            }
        }

        private void UpdateDetectionUniforms()
        {
            int count = 0;
            for (int i = 0; i < k_maxDetections; i++)
            {
                if (Time.time - m_detectionTimestamp[i] < m_detectionLifetime)
                    count = i + 1;
            }
            m_material.SetInt(s_detectionCountId,         count);
            m_material.SetVectorArray(s_detectionRectsId, m_detectionRects);
        }

        private Vector4 BoxToAzElRect(Vector4 box, Vector2Int inputSize)
        {
            var tl = UvToAzEl(new Vector2(box.x / inputSize.x, 1f - box.y / inputSize.y));
            var br = UvToAzEl(new Vector2(box.z / inputSize.x, 1f - box.w / inputSize.y));
            return new Vector4(
                Mathf.Min(tl.x, br.x), Mathf.Max(tl.x, br.x),
                Mathf.Min(tl.y, br.y), Mathf.Max(tl.y, br.y));
        }

        private Vector2 UvToAzEl(Vector2 uv)
        {
            if (m_tanHalfFov.x <= 0f) return Vector2.zero;
            Vector2 ndc = (uv - Vector2.one * 0.5f) * 2f;
            ndc.x *= m_tanHalfFov.x;
            ndc.y *= m_tanHalfFov.y;
            Vector3 dir = (m_headForward + ndc.x * m_headRight + ndc.y * m_headUp).normalized;
            return new Vector2(
                Mathf.Atan2(dir.x, dir.z),
                Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)));
        }

        // ---- helpers ----

        private static Vector3 DirFromAzEl(float az, float el)
        {
            float cosEl = Mathf.Cos(el);
            return new Vector3(Mathf.Sin(az) * cosEl, Mathf.Sin(el), Mathf.Cos(az) * cosEl);
        }

        private void SetDebug(string msg)
        {
            if (m_debugText != null) m_debugText.text = msg;
            Debug.Log($"[CameraVignette] {msg}");
        }
    }
}
