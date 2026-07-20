// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;
namespace PassthroughCameraSamples.ShaderSample
{
    public enum VignetteMode
    {
        Blur               = 0, // Camera: blurred + desaturated periphery (contrast-restored)
        SoftDark           = 1, // Dark overlay, soft (~75% max alpha), gradual formation
        HardDark           = 2, // Dark overlay, full blackout, gradual formation
        TintedDark         = 3, // Configurable-colour dark overlay, gradual formation
        ChromaticCool      = 4, // Camera: warm focus, cool blue periphery shift
        ColorPop           = 5, // Muted grey periphery; saturated warm/green colors boosted vivid
        ConspicuitySqueeze = 6, // Camera: periphery contrast flattened toward local mean (Veas 2011)
        GranulatedPeriphery= 7, // World-locked noise grains, density ramps with eccentricity (Cao 2021)
        OutlinedDark       = 8, // Near-blackout with luminance edges kept (Cheng 2022)
        SpotLift           = 9, // Focus window brightened + soft peripheral dim (video mode)
        SignPop            = 10,// Video only: ColorPop gated by baked detections — only actual
                                // traffic lights/signs pop; ROG look-alikes (neon, ads) muted
    }

    [System.Serializable]
    public struct MotionSettings
    {
        [Tooltip("Head angular speed (deg/s) that triggers suppression.")]
        [Range(5f, 180f)] public float speedThreshDeg;
        [Tooltip("Seconds the suppression holds after speed drops below threshold.")]
        [Range(0f, 5f)] public float holdSeconds;
    }

    [MetaCodeSample("PassthroughCameraApiSamples-ShaderSample")]
    public class CameraSphereVignetteManager : MonoBehaviour, IStudyVignetteControl
    {
        [SerializeField] private PassthroughCameraAccess m_cameraAccess;
        [SerializeField] private PassthroughCameraAccess m_cameraAccessRight;
        // Named m_renderer to preserve scene serialization from the prior version.
        [SerializeField] private MeshRenderer m_renderer;

        [Tooltip("OVRCameraRig's right controller anchor (for aim direction).")]
        [SerializeField] private Transform m_rightControllerAnchor;

        [Tooltip("Optional: OVRCameraRig root.")]
        [SerializeField] private Transform m_cameraRig;

        [SerializeField] private Text m_debugText;

        [Header("Filter")]
        [SerializeField, Range(1f, 45f)]   private float m_softEdgeDeg   = 20f;
        [Tooltip("Hard Dark uses its own, much narrower soft edge — once a selection is locked in, everything outside it should read as black almost immediately, not fade through a wide gradient buffer.")]
        [SerializeField, Range(0.5f, 20f)] private float m_hardDarkSoftEdgeDeg = 2f;
        [SerializeField, Range(0f, 0.15f)] private float m_maxBlurRadius = 0.01f;
        [SerializeField, Range(0.5f, 8f)]  private float m_blurCurveExp  = 1.5f;
        [SerializeField, Range(0f, 0.9f)]  private float m_blurDelay     = 0.5f;
        [SerializeField, Range(0f, 0.9f)]  private float m_desatDelay    = 0.3f;
        [SerializeField, Range(0.5f, 4f)]  private float m_desatCurveExp = 3f;
        [Tooltip("Default half-width (deg), applied symmetrically to az/el, of the focus window. " +
                 "Seeds the free-play brush size AND the fixed Blocks B/C windscreen window (single " +
                 "shared source). 0 = start from nothing and build the window entirely by painting " +
                 "(no forced minimum size). Originally set to 15° per Ball & Owsley UFOV central-field " +
                 "reasoning, but on-device testing found that felt too large in practice — tune to taste.")]
        [SerializeField, Range(0f, 30f)] private float m_defaultWindowHalfWidthDeg = 0f;

        // m_selectionLine kept for scene serialisation compatibility — disabled at runtime
        [SerializeField] private LineRenderer m_selectionLine;

        [Header("Mode")]
        [SerializeField] private VignetteMode m_vignetteMode    = VignetteMode.Blur;
        [SerializeField, Range(0.5f, 10f)]   private float m_vignetteFormTime = 3f;
        [SerializeField, Range(0.1f, 0.95f)] private float m_mode2MaxAlpha    = 0.75f;

        [Header("World Anchor (Hard Dark)")]
        [Tooltip("Optional. When set, releasing the trigger in Hard Dark raycasts the painted " +
                 "window's corners against live depth data (Meta.XR.EnvironmentRaycastManager) " +
                 "and pins the window to that real-world point — it re-projects from the current " +
                 "head position every frame, so walking toward/away/around the target keeps the " +
                 "window on the real object instead of it translating with the headset. Leave " +
                 "unassigned to keep the legacy head-relative-bearing behaviour.")]
        [SerializeField] private EnvironmentRaycastManager m_raycastManager;

        [Header("Motion Disable — Per Mode")]
        [SerializeField] private MotionSettings m_motionBlur          = new MotionSettings { speedThreshDeg = 30f,  holdSeconds = 0.6f };
        [SerializeField] private MotionSettings m_motionSoftDark      = new MotionSettings { speedThreshDeg = 50f,  holdSeconds = 1.0f };
        [SerializeField] private MotionSettings m_motionHardDark      = new MotionSettings { speedThreshDeg = 70f,  holdSeconds = 1.5f };
        [SerializeField] private MotionSettings m_motionTintedDark    = new MotionSettings { speedThreshDeg = 70f,  holdSeconds = 1.5f };
        [SerializeField] private MotionSettings m_motionChromaticCool = new MotionSettings { speedThreshDeg = 35f,  holdSeconds = 0.7f };
        [SerializeField] private MotionSettings m_motionColorPop      = new MotionSettings { speedThreshDeg = 35f,  holdSeconds = 0.7f };

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

        [Header("Blur Noise Texture")]
        [Tooltip("Noise source for the Blur-mode perceptual noise enhancement.")]
        [SerializeField] private Texture2D m_frostTex;

        [Header("Tinted Dark")]
        [SerializeField] private Color m_tintColor = new Color(0.05f, 0.10f, 0.25f, 1f);

        [Header("Chromatic Cool")]
        [SerializeField, Range(0f, 1f)] private float m_coolStrength = 0.6f;

        [Header("Color Pop")]
        [Tooltip("Brightness of the muted grey periphery (lower = stronger pop contrast).")]
        [SerializeField, Range(0.1f, 1f)] private float m_popGreyDim  = 0.4f;
        [Tooltip("Saturation boost applied to kept warm/green colors.")]
        [SerializeField, Range(1f, 3f)]   private float m_popSatBoost = 1.7f;

        [Header("Blur Contrast Restore")]
        [Tooltip("Re-amplify local contrast after blurring (Patney 2016) — removes the tunnel-vision percept.")]
        [SerializeField, Range(0f, 2f)] private float m_blurContrastRestore = 0.8f;

        [Header("Conspicuity Squeeze")]
        [SerializeField, Range(0.005f, 0.08f)] private float m_squeezeRadius = 0.015f;
        [SerializeField, Range(0f, 1f)]        private float m_squeezeLum    = 0.5f;
        [SerializeField, Range(0f, 1f)]        private float m_squeezeChroma = 0.85f;
        [Tooltip("Gentle global desaturation on top of the local flattening (0 = off, keep light).")]
        [SerializeField, Range(0f, 1f)]        private float m_squeezeDesat  = 0.25f;
        [Tooltip("Peripheral luminance multiplier (1 = no dimming; 'a little dimming' ~0.9).")]
        [SerializeField, Range(0.5f, 1f)]      private float m_squeezeDim    = 0.9f;

        [Header("Granulated Periphery")]
        [SerializeField, Range(8f, 256f)] private float m_grainScale      = 90f;
        [SerializeField, Range(0f, 1f)]   private float m_grainDensityMin = 0.25f;
        [SerializeField, Range(0f, 1f)]   private float m_grainDensityMax = 0.75f;
        [SerializeField]                  private Color m_grainColor      = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Header("Outlined Dimming")]
        [SerializeField, Range(1f, 40f)]   private float m_edgeGain        = 12f;
        [SerializeField, Range(0f, 1f)]    private float m_edgeBrightness  = 0.35f;
        [SerializeField, Range(0.5f, 1f)]  private float m_outlineDimAlpha = 0.92f;

        [Header("Spotlight Lift")]
        [SerializeField, Range(0f, 0.3f)] private float m_spotLiftAmp  = 0.08f;
        [SerializeField, Range(0f, 1f)]   private float m_spotDimAlpha = 0.35f;

        [Header("Detection Islands")]
        [Tooltip("Soft edge width on YOLO detection clear zones (degrees).")]
        [SerializeField, Range(0f, 10f)] private float m_detectionSoftEdgeDeg = 3f;
        [Tooltip("How much to boost saturation/brightness on detected objects (0=clear only, 1=vivid).")]
        [SerializeField, Range(0f, 1f)]  private float m_detectionEnhance = 0.4f;
        [Tooltip("How much the annulus around a detected object is darkened (center-surround contrast).")]
        [SerializeField, Range(0f, 0.5f)] private float m_detectionSurround = 0.15f;
        [Tooltip("Amplitude of the gentle ~1 Hz breathing on the object boost (0 = static).")]
        [SerializeField, Range(0f, 1f)] private float m_detectionPulseAmp = 0.25f;
        [Tooltip("How long (seconds) a detection's window/highlight takes to fade in when it first appears.")]
        [SerializeField, Range(0.02f, 2f)] private float m_detectionFadeInSeconds = 0.2f;
        [Tooltip("How long (seconds) a detection's window/highlight takes to fade out after it vanishes/holds out.")]
        [SerializeField, Range(0.02f, 2f)] private float m_detectionFadeOutSeconds = 0.5f;
        [Tooltip("Debounce: a tracked detection only becomes visible once it has persisted this many seconds (i.e. survived 2+ YOLO inference frames). Single-inference-frame false positives otherwise ghost a ~1s window over nothing. Must exceed m_detectionLifetime (0.6) to fully suppress one-frame blips — below that they still flash partially before the track expires. 0 = off.")]
        [SerializeField, Range(0f, 3f)] private float m_detectionMinAgeSec = 0.7f;
        [Tooltip("Minimum apparent size (deg, max of az/el extent) a detection's box must currently have to be visible — a speck-sized box would be inflated to the shader's ~2° minimum halo, reading as a circle around nothing discernible. Same gate as VideoTestSceneManager. 0 = off.")]
        [SerializeField, Range(0f, 5f)] private float m_detectionMinSizeDeg = 1.2f;
        [Tooltip("Scales the saliency-boost highlight down for detections OUTSIDE the active focus window — the window opening around the object is already enough there, so the extra pop stays subtle.")]
        [SerializeField, Range(0f, 1f)] private float m_detectionOutsideFocusScale = 0.35f;

        [Header("Debug")]
        [SerializeField] private bool m_debugCamOverlay;

        [Header("YOLO Detection")]
        [Tooltip("Optional — wire up to enable automatic clear zones for detected objects.")]
        [SerializeField] private YoloRunner m_yoloRunner;
        [SerializeField] private float      m_detectionLifetime = 0.6f;

        [SerializeField] private float m_cameraHorizontalFovDeg = 82f;

        // ---- shader property IDs ----
        private static readonly int s_mainTexLId           = Shader.PropertyToID("_MainTexL");
        private static readonly int s_mainTexRId           = Shader.PropertyToID("_MainTexR");
        private static readonly int s_sphereCenterId       = Shader.PropertyToID("_SphereCenter");
        private static readonly int s_camLFwdId            = Shader.PropertyToID("_CamLFwd");
        private static readonly int s_camLRtId             = Shader.PropertyToID("_CamLRt");
        private static readonly int s_camLUpId             = Shader.PropertyToID("_CamLUp");
        private static readonly int s_tanHalfFovLId        = Shader.PropertyToID("_TanHalfFovL");
        private static readonly int s_camRFwdId            = Shader.PropertyToID("_CamRFwd");
        private static readonly int s_camRRtId             = Shader.PropertyToID("_CamRRt");
        private static readonly int s_camRUpId             = Shader.PropertyToID("_CamRUp");
        private static readonly int s_tanHalfFovRId        = Shader.PropertyToID("_TanHalfFovR");
        private static readonly int s_hasRightCamId        = Shader.PropertyToID("_HasRightCam");
        private static readonly int s_focusRectId          = Shader.PropertyToID("_FocusRect");
        private static readonly int s_softEdgeId           = Shader.PropertyToID("_SoftEdge");
        private static readonly int s_maxBlurRadId         = Shader.PropertyToID("_MaxBlurRadius");
        private static readonly int s_blurCurveExpId       = Shader.PropertyToID("_BlurCurveExp");
        private static readonly int s_blurDelayId          = Shader.PropertyToID("_BlurDelay");
        private static readonly int s_desatDelayId         = Shader.PropertyToID("_DesatDelay");
        private static readonly int s_desatCurveExpId      = Shader.PropertyToID("_DesatCurveExp");
        private static readonly int s_detectionCountId     = Shader.PropertyToID("_DetectionCount");
        private static readonly int s_detectionRectsId     = Shader.PropertyToID("_DetectionRects");
        private static readonly int s_detectionSoftEdgeId  = Shader.PropertyToID("_DetectionSoftEdge");
        private static readonly int s_detectionEnhanceId   = Shader.PropertyToID("_DetectionEnhance");
        private static readonly int s_detectionSurroundId  = Shader.PropertyToID("_DetectionSurround");
        private static readonly int s_detectionPulseAmpId  = Shader.PropertyToID("_DetectionPulseAmp");
        private static readonly int s_detectionFadeId          = Shader.PropertyToID("_DetectionFade");
        private static readonly int s_detectionOutsideScaleId  = Shader.PropertyToID("_DetectionOutsideScale");
        private static readonly int s_suppressDetectionWindowsId = Shader.PropertyToID("_SuppressDetectionWindows");
        private static readonly int s_blurContrastRestoreId = Shader.PropertyToID("_BlurContrastRestore");
        private static readonly int s_squeezeModeId    = Shader.PropertyToID("_SqueezeMode");
        private static readonly int s_squeezeRadiusId  = Shader.PropertyToID("_SqueezeRadius");
        private static readonly int s_squeezeLumId     = Shader.PropertyToID("_SqueezeLum");
        private static readonly int s_squeezeChromaId  = Shader.PropertyToID("_SqueezeChroma");
        private static readonly int s_squeezeDesatId   = Shader.PropertyToID("_SqueezeDesat");
        private static readonly int s_squeezeDimId     = Shader.PropertyToID("_SqueezeDim");
        private static readonly int s_grainModeId       = Shader.PropertyToID("_GrainMode");
        private static readonly int s_grainScaleId      = Shader.PropertyToID("_GrainScale");
        private static readonly int s_grainDensityMinId = Shader.PropertyToID("_GrainDensityMin");
        private static readonly int s_grainDensityMaxId = Shader.PropertyToID("_GrainDensityMax");
        private static readonly int s_grainColorId      = Shader.PropertyToID("_GrainColor");
        private static readonly int s_outlineModeId     = Shader.PropertyToID("_OutlineMode");
        private static readonly int s_edgeGainId        = Shader.PropertyToID("_EdgeGain");
        private static readonly int s_edgeBrightnessId  = Shader.PropertyToID("_EdgeBrightness");
        private static readonly int s_outlineDimAlphaId = Shader.PropertyToID("_OutlineDimAlpha");
        private static readonly int s_spotLiftModeId    = Shader.PropertyToID("_SpotLiftMode");
        private static readonly int s_spotLiftAmpId     = Shader.PropertyToID("_SpotLiftAmp");
        private static readonly int s_spotDimAlphaId    = Shader.PropertyToID("_SpotDimAlpha");
        private static readonly int s_debugCamOverlayId    = Shader.PropertyToID("_DebugCamOverlay");
        private static readonly int s_simpleModeId         = Shader.PropertyToID("_SimpleMode");
        private static readonly int s_vignetteStrengthId   = Shader.PropertyToID("_VignetteStrength");
        private static readonly int s_maxVignetteAlphaId   = Shader.PropertyToID("_MaxVignetteAlpha");
        private static readonly int s_frostTexId           = Shader.PropertyToID("_FrostTex");
        private static readonly int s_popGreyDimId         = Shader.PropertyToID("_PopGreyDim");
        private static readonly int s_popSatBoostId        = Shader.PropertyToID("_PopSatBoost");
        private static readonly int s_tintColorId          = Shader.PropertyToID("_TintColor");
        private static readonly int s_tintModeId           = Shader.PropertyToID("_TintMode");
        private static readonly int s_chromaticCoolId      = Shader.PropertyToID("_ChromaticCool");
        private static readonly int s_coolStrengthId       = Shader.PropertyToID("_CoolStrength");
        private static readonly int s_colorPopModeId       = Shader.PropertyToID("_ColorPopMode");

        private static readonly Vector4 k_fullSphere =
            new(-Mathf.PI, Mathf.PI, -Mathf.PI * 0.5f, Mathf.PI * 0.5f);

        // COCO: 9 = traffic light, 11 = stop sign. Signs only — vehicles/people excluded.
        private static readonly HashSet<int> k_targetClasses = new() { 9, 11 };

        private const int k_maxDetections = 8;
        private readonly Vector4[] m_detectionRects = new Vector4[k_maxDetections];
        private readonly float[]   m_detectionFade  = new float[k_maxDetections];

        // Live per-frame YOLO detections have no persistent identity of their own, so the
        // same nearest-centre matching heuristic used by VideoTestSceneManager is applied
        // here too — that's what lets `presence` ramp smoothly per real object instead of
        // per array slot (slot indices aren't stable frame to frame otherwise).
        // `firstSeenTime` gates visibility on track age (m_detectionMinAgeSec) — same debounce
        // as VideoTestSceneManager: a detection that doesn't persist past the min age never
        // ramps presence above 0, so single-inference-frame YOLO false positives never flash
        // a window/highlight at all.
        private struct TrackedDet { public Vector4 rect; public float firstSeenTime; public float lastSeenTime; public float presence; }
        private readonly List<TrackedDet> m_trackedDets = new();

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

        // World anchor (Hard Dark only): 4 depth-raycast hit points for the painted rect's
        // corners, re-projected to az/el from the current head position every frame.
        private bool    m_hasWorldAnchor;
        private Vector3 m_anchorBL, m_anchorBR, m_anchorTL, m_anchorTR;

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
        private Material    m_modeUIMat;
        private CanvasGroup m_modeUIGroup;
        private Text        m_modeNameText;
        private Text        m_modeHintText;
        private float       m_modeUITimer;
        private const float k_modeUIShowTime = 2.8f;
        private const float k_modeUIFadeDur  = 0.35f;

        private static bool IsCameraMode(VignetteMode mode) =>
            mode == VignetteMode.Blur || mode == VignetteMode.ChromaticCool || mode == VignetteMode.ColorPop
            || mode == VignetteMode.ConspicuitySqueeze || mode == VignetteMode.SpotLift;

        // A-button cycle — the kept modes. Legacy modes stay in the enum and shader but are
        // no longer reachable from the controller. ConspicuitySqueeze is surfaced as "Flatten":
        // contrast balanced toward the local mean + gentle desaturation + slight dim.
        private static readonly VignetteMode[] k_modeCycle =
        {
            VignetteMode.Blur, VignetteMode.SoftDark, VignetteMode.HardDark,
            VignetteMode.ColorPop, VignetteMode.ConspicuitySqueeze,
        };

        // ---- Unity lifecycle ----

        private IEnumerator Start()
        {
            InitSelectionDots();
            InitModeUI();

            m_material = m_renderer.material;
            m_material.SetVector(s_focusRectId, m_activeRect);
            m_material.SetFloat(s_hasRightCamId, 0f);

            if (Camera.main != null) m_lastHeadRot = Camera.main.transform.rotation;

            // Normal passthrough path
            foreach (var cam in Camera.allCameras)
            {
                cam.clearFlags      = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.clear;
            }

            var ptLayerNormal = FindObjectOfType<OVRPassthroughLayer>();
            if (ptLayerNormal != null) ptLayerNormal.enabled = true;

            if (m_selectionLine != null)
            {
                m_selectionLine.loop          = true;
                m_selectionLine.useWorldSpace = true;
                m_selectionLine.startWidth    = 0.005f;
                m_selectionLine.endWidth      = 0.005f;
                m_selectionLine.enabled       = false;
            }

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
            if (m_modeUIMat != null)
                Destroy(m_modeUIMat);
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
            UpdateWorldAnchorRect();
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
            // Hard Dark uses a narrow edge so the black-out reads as an almost-binary
            // "selection vs everything else" rather than a wide buffer zone.
            float softEdgeDeg = m_vignetteMode == VignetteMode.HardDark ? m_hardDarkSoftEdgeDeg : m_softEdgeDeg;
            m_material.SetFloat(s_softEdgeId,        softEdgeDeg   * Mathf.Deg2Rad);
            m_material.SetFloat(s_maxBlurRadId,      m_maxBlurRadius);
            m_material.SetFloat(s_blurCurveExpId,    m_blurCurveExp);
            m_material.SetFloat(s_blurDelayId,       m_blurDelay);
            m_material.SetFloat(s_desatDelayId,      m_desatDelay);
            m_material.SetFloat(s_desatCurveExpId,   m_desatCurveExp);
            m_material.SetFloat(s_debugCamOverlayId, m_debugCamOverlay ? 1f : 0f);

            if (m_frostTex != null) m_material.SetTexture(s_frostTexId, m_frostTex);
            m_material.SetFloat(s_popGreyDimId,  m_popGreyDim);
            m_material.SetFloat(s_popSatBoostId, m_popSatBoost);
            m_material.SetFloat(s_blurContrastRestoreId, m_blurContrastRestore);
            m_material.SetFloat(s_squeezeRadiusId,   m_squeezeRadius);
            m_material.SetFloat(s_squeezeLumId,      m_squeezeLum);
            m_material.SetFloat(s_squeezeChromaId,   m_squeezeChroma);
            m_material.SetFloat(s_squeezeDesatId,    m_squeezeDesat);
            m_material.SetFloat(s_squeezeDimId,      m_squeezeDim);
            m_material.SetFloat(s_grainScaleId,      m_grainScale);
            m_material.SetFloat(s_grainDensityMinId, m_grainDensityMin);
            m_material.SetFloat(s_grainDensityMaxId, m_grainDensityMax);
            m_material.SetColor(s_grainColorId,      m_grainColor);
            m_material.SetFloat(s_edgeGainId,        m_edgeGain);
            m_material.SetFloat(s_edgeBrightnessId,  m_edgeBrightness);
            m_material.SetFloat(s_outlineDimAlphaId, m_outlineDimAlpha);
            m_material.SetFloat(s_spotLiftAmpId,     m_spotLiftAmp);
            m_material.SetFloat(s_spotDimAlphaId,    m_spotDimAlpha);

            m_material.SetColor(s_tintColorId,        m_tintColor);
            m_material.SetFloat(s_coolStrengthId,     m_coolStrength);
            m_material.SetFloat(s_detectionSoftEdgeId, m_detectionSoftEdgeDeg * Mathf.Deg2Rad);
            m_material.SetFloat(s_detectionEnhanceId,  m_detectionEnhance);
            m_material.SetFloat(s_detectionSurroundId, m_detectionSurround);
            m_material.SetFloat(s_detectionPulseAmpId, m_detectionPulseAmp);
            m_material.SetFloat(s_detectionOutsideScaleId, m_detectionOutsideFocusScale);
        }

        // ---- motion-based disable ----

        private MotionSettings CurrentMotionSettings => m_vignetteMode switch
        {
            VignetteMode.Blur          => m_motionBlur,
            VignetteMode.SoftDark      => m_motionSoftDark,
            VignetteMode.TintedDark          => m_motionTintedDark,
            VignetteMode.ChromaticCool       => m_motionChromaticCool,
            VignetteMode.ColorPop            => m_motionColorPop,
            VignetteMode.ConspicuitySqueeze  => m_motionBlur,
            VignetteMode.GranulatedPeriphery => m_motionSoftDark,
            VignetteMode.SpotLift            => m_motionSoftDark,
            _                                => m_motionHardDark
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

        // ---- study API (IStudyVignetteControl — driven by Study/ConditionSequencer) ----

        public VignetteMode CurrentMode => m_vignetteMode;
        public Vector4 ActiveRect => m_activeRect;
        public float DefaultWindowHalfWidthDeg => m_defaultWindowHalfWidthDeg;
        public float CurrentEffectiveStrength { get; private set; }
        public bool StudyInputLock { get; set; }
        public bool StudyEffectSuppressed { get; set; }

        // Minimal-UI test mode (set by the Block A launcher): painting stays live, but the
        // A mode-cycle and B clear are disabled (controllers only select the window), the mode
        // toast never shows, and debug messages auto-hide a few seconds after they appear.
        private bool m_studyMinimalUi;
        private Coroutine m_debugHideCoroutine;

        public bool StudyMinimalUi
        {
            get => m_studyMinimalUi;
            set
            {
                m_studyMinimalUi = value;
                // Kill a toast already mid-show (Start() fires one before a launcher can set this).
                if (value && m_modeUIGroup != null)
                {
                    m_modeUITimer = 0f;
                    m_modeUIGroup.alpha = 0f;
                }
            }
        }

        // Motion suppression has no enable flag in the passthrough scene — it is always on.
        public bool MotionEnabled { get => true; set { } }

        public void StudySetMode(VignetteMode mode)
        {
            m_vignetteMode       = mode;
            m_motionDisableTimer = 0f;
            m_motionSuppression  = 0f;
            m_hasWorldAnchor     = false;
            StopFormCoroutine();
            m_vignetteStrength = 1f; // conditions start fully formed; baselines use StudyEffectSuppressed
        }

        public void StudySetActive(bool active)
        {
            StopFormCoroutine();
            if (!active) { m_vignetteStrength = 0f; return; }
            m_vignetteStrength = IsCameraMode(m_vignetteMode) ? 1f : 0f;
            if (!IsCameraMode(m_vignetteMode)) m_formCoroutine = StartCoroutine(FormVignette());
        }

        public void StudySetWindow(Vector4 azElRadians)
        {
            m_activeRect     = azElRadians;
            m_hasWorldAnchor = false;
            m_material.SetVector(s_focusRectId, m_activeRect);
            m_isPainting = false;
            CancelDotHide();
            HideCornerDotsImmediate();
        }

        public void StudyClearWindow()
        {
            m_activeRect     = k_fullSphere;
            m_hasWorldAnchor = false;
            m_material.SetVector(s_focusRectId, m_activeRect);
            m_isPainting = false;
            CancelDotHide();
            HideCornerDotsImmediate();
        }

        // ---- selection (paint-while-holding) ----

        private bool m_isPainting;
        private float BrushPadRad => m_defaultWindowHalfWidthDeg * Mathf.Deg2Rad;

        private void HandleSelection()
        {
            // Study lock: the trigger belongs to the task (CPT presses) and A/B must not
            // change mode/window mid-condition. Hide the aim cursor while locked.
            if (StudyInputLock)
            {
                if (m_selectionDots != null && m_selectionDots[4] != null && m_selectionDots[4].activeSelf)
                    m_selectionDots[4].SetActive(false);
                m_isPainting = false;
                return;
            }
            if (m_selectionDots != null && m_selectionDots[4] != null && !m_selectionDots[4].activeSelf)
                m_selectionDots[4].SetActive(true);

            bool held         = OVRInput.Get(OVRInput.RawButton.RIndexTrigger);
            bool justPressed  = OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger);
            bool justReleased = OVRInput.GetUp(OVRInput.RawButton.RIndexTrigger);
            // Minimal-UI test mode: the controller only paints the window — no mode
            // cycling, no clearing (a repaint replaces the window anyway).
            bool aPressed     = !m_studyMinimalUi && OVRInput.GetDown(OVRInput.RawButton.A);
            bool bPressed     = !m_studyMinimalUi && OVRInput.GetDown(OVRInput.RawButton.B);

            // A button: cycle through all 6 modes
            if (aPressed)
            {
                int cyclePos = System.Array.IndexOf(k_modeCycle, m_vignetteMode);
                m_vignetteMode = k_modeCycle[(cyclePos + 1) % k_modeCycle.Length];
                // Reset motion state so the new mode's thresholds apply from a clean slate
                m_motionDisableTimer = 0f;
                m_motionSuppression  = 0f;
                m_hasWorldAnchor     = false;
                bool hasRect         = m_activeRect != k_fullSphere;

                if (IsCameraMode(m_vignetteMode))
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
                m_activeRect     = k_fullSphere;
                m_hasWorldAnchor = false;
                m_material.SetVector(s_focusRectId, m_activeRect);
                StopFormCoroutine();
                m_vignetteStrength = IsCameraMode(m_vignetteMode) ? 1f : 0f;
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
                m_hasWorldAnchor = false;

                if (!IsCameraMode(m_vignetteMode))
                {
                    StopFormCoroutine();
                    m_vignetteStrength = 0f;
                }

                m_activeRect = new Vector4(
                    az - BrushPadRad, az + BrushPadRad,
                    el - BrushPadRad, el + BrushPadRad);
                m_material.SetVector(s_focusRectId, m_activeRect);
                m_isPainting = true;
            }
            else if (held && m_isPainting)
            {
                m_activeRect = new Vector4(
                    Mathf.Min(m_activeRect.x, az - BrushPadRad),
                    Mathf.Max(m_activeRect.y, az + BrushPadRad),
                    Mathf.Min(m_activeRect.z, el - BrushPadRad),
                    Mathf.Max(m_activeRect.w, el + BrushPadRad));
                m_material.SetVector(s_focusRectId, m_activeRect);
            }

            // On release: lock the selection, start vignette + dot-hide countdown
            if (justReleased && m_isPainting && m_activeRect != k_fullSphere)
            {
                if (IsCameraMode(m_vignetteMode))
                    m_vignetteStrength = 1f;
                else
                {
                    StopFormCoroutine();
                    m_formCoroutine = StartCoroutine(FormVignette());
                }
                m_dotHideCoroutine = StartCoroutine(HideDotsCoro());
                TryWorldAnchorSelection();
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
            // Snap any stale serialized/legacy mode into the kept cycle.
            if (System.Array.IndexOf(k_modeCycle, m_vignetteMode) < 0)
                m_vignetteMode = VignetteMode.Blur;

            bool isColorPop = m_vignetteMode == VignetteMode.ColorPop;
            bool isSimple   = m_vignetteMode == VignetteMode.SoftDark || m_vignetteMode == VignetteMode.HardDark;
            bool isTinted   = m_vignetteMode == VignetteMode.TintedDark;
            bool isCoolBlur = m_vignetteMode == VignetteMode.ChromaticCool;
            bool isSqueeze  = m_vignetteMode == VignetteMode.ConspicuitySqueeze;
            bool isGrain    = m_vignetteMode == VignetteMode.GranulatedPeriphery;
            bool isOutline  = m_vignetteMode == VignetteMode.OutlinedDark;
            bool isSpotLift = m_vignetteMode == VignetteMode.SpotLift;

            float effectiveStrength = StudyEffectSuppressed
                ? 0f
                : m_vignetteStrength * (1f - m_motionSuppression);
            CurrentEffectiveStrength = effectiveStrength;
            float maxAlpha = m_vignetteMode switch
            {
                VignetteMode.HardDark            => 1f,
                VignetteMode.TintedDark          => 1f,
                VignetteMode.OutlinedDark        => 1f,
                VignetteMode.GranulatedPeriphery => 1f,
                _                                => m_mode2MaxAlpha
            };

            m_material.SetFloat(s_simpleModeId,       isSimple   ? 1f : 0f);
            m_material.SetFloat(s_tintModeId,         isTinted   ? 1f : 0f);
            m_material.SetFloat(s_chromaticCoolId,    isCoolBlur  ? 1f : 0f);
            m_material.SetFloat(s_colorPopModeId,     isColorPop  ? 1f : 0f);
            m_material.SetFloat(s_squeezeModeId,      isSqueeze   ? 1f : 0f);
            m_material.SetFloat(s_grainModeId,        isGrain     ? 1f : 0f);
            m_material.SetFloat(s_outlineModeId,      isOutline   ? 1f : 0f);
            m_material.SetFloat(s_spotLiftModeId,     isSpotLift  ? 1f : 0f);
            m_material.SetFloat(s_vignetteStrengthId, effectiveStrength);
            m_material.SetFloat(s_maxVignetteAlphaId, maxAlpha);
            // Hard Dark: once a selection is locked in, that exact painted rect is the only
            // thing that's ever clear — suppress the generic per-detection carve-out/highlight
            // entirely so a detected light/person elsewhere can't poke a hole in the black-out.
            m_material.SetFloat(s_suppressDetectionWindowsId, m_vignetteMode == VignetteMode.HardDark ? 1f : 0f);

            // ColorPop: when no selection is painted, auto-follow head gaze so the effect
            // is always visible without needing to hold trigger first.
            if (isColorPop && m_activeRect == k_fullSphere)
            {
                Transform head = Camera.main != null ? Camera.main.transform : transform;
                float headAz   = Mathf.Atan2(head.forward.x, head.forward.z);
                float headEl   = Mathf.Asin(Mathf.Clamp(head.forward.y, -1f, 1f));
                float halfW    = 40f * Mathf.Deg2Rad;
                float halfH    = 25f * Mathf.Deg2Rad;
                m_material.SetVector(s_focusRectId,
                    new Vector4(headAz - halfW, headAz + halfW, headEl - halfH, headEl + halfH));
            }
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

        // ---- world anchor (Hard Dark) ----

        // Raycasts the 4 corners of the just-locked selection against live depth data and, if
        // every corner hits real geometry, stores those world points so the window can be
        // re-projected from the current head position every frame instead of staying a fixed
        // bearing from wherever the head happened to be while painting.
        //
        // The native raycaster is created asynchronously when the scene loads and reports
        // NotReady until then — a paint in the first seconds after entering the scene used to
        // fail permanently for that reason alone. NotReady now retries until the raycaster comes
        // up (or the deadline passes); any other failure logs the per-corner statuses
        // (NoHit / RayOccluded / HitPointOutsideOfCameraFrustum / …) so the cause is visible.
        private const float k_anchorRetrySeconds = 10f;

        private Coroutine m_anchorRetry;

        private void TryWorldAnchorSelection()
        {
            if (m_anchorRetry != null) { StopCoroutine(m_anchorRetry); m_anchorRetry = null; }
            if (AttemptWorldAnchor(out bool notReady) || !notReady) return;
            m_anchorRetry = StartCoroutine(RetryAnchorWhenReady(m_activeRect));
        }

        private IEnumerator RetryAnchorWhenReady(Vector4 rect)
        {
            float deadline = Time.time + k_anchorRetrySeconds;
            while (Time.time < deadline)
            {
                yield return new WaitForSeconds(0.25f);
                // Abort silently if the selection or mode changed while waiting.
                if (m_vignetteMode != VignetteMode.HardDark || m_activeRect != rect)
                    { m_anchorRetry = null; yield break; }
                if (AttemptWorldAnchor(out bool notReady) || !notReady)
                    { m_anchorRetry = null; yield break; }
            }
            m_anchorRetry = null;
            SetDebug($"World-anchor failed: depth raycaster not ready after {k_anchorRetrySeconds:0} s — window will follow head.");
        }

        // Returns true if anchored. notReady = the raycaster is still initialising (retryable);
        // every other miss is a real one and gets its statuses logged.
        private bool AttemptWorldAnchor(out bool notReady)
        {
            notReady = false;
            m_hasWorldAnchor = false;
            if (m_raycastManager == null || m_vignetteMode != VignetteMode.HardDark) return false;
            if (!EnvironmentRaycastManager.IsSupported)
            {
                SetDebug("World-anchor unavailable: depth raycast not supported on this device/session.");
                return false;
            }
            if (m_activeRect == k_fullSphere) return false;

            Transform head = Camera.main != null ? Camera.main.transform : transform;
            Vector3   org  = head.position;

            bool ok = true;
            ok &= RaycastCorner(org, m_activeRect.x, m_activeRect.z, out m_anchorBL, out var sBL);
            ok &= RaycastCorner(org, m_activeRect.y, m_activeRect.z, out m_anchorBR, out var sBR);
            ok &= RaycastCorner(org, m_activeRect.y, m_activeRect.w, out m_anchorTR, out var sTR);
            ok &= RaycastCorner(org, m_activeRect.x, m_activeRect.w, out m_anchorTL, out var sTL);

            m_hasWorldAnchor = ok;
            notReady = sBL == EnvironmentRaycastHitStatus.NotReady || sBR == EnvironmentRaycastHitStatus.NotReady
                    || sTR == EnvironmentRaycastHitStatus.NotReady || sTL == EnvironmentRaycastHitStatus.NotReady;
            SetDebug(ok
                ? "World-anchored to real object — window stays put as you move."
                : notReady
                    ? "World-anchor: depth raycaster still initialising — retrying…"
                    : $"World-anchor failed — corners BL:{sBL} BR:{sBR} TR:{sTR} TL:{sTL} — window will follow head.");
            return ok;
        }

        private bool RaycastCorner(Vector3 origin, float az, float el, out Vector3 worldPoint,
                                   out EnvironmentRaycastHitStatus status)
        {
            var ray = new Ray(origin, DirFromAzEl(az, el));
            bool hitOk = m_raycastManager.Raycast(ray, out var hit);
            status = hit.status;
            worldPoint = hitOk ? hit.point : Vector3.zero;
            return hitOk;
        }

        // Re-derives az/el bounds from the anchored world corners relative to the CURRENT head
        // position every frame — this is what makes the window perspective-correct (it grows
        // angularly as you approach the real object and shrinks as you back away, just like
        // looking at a fixed object/window would), rather than a fixed angular size.
        private void UpdateWorldAnchorRect()
        {
            if (!m_hasWorldAnchor || m_vignetteMode != VignetteMode.HardDark) return;

            Transform head = Camera.main != null ? Camera.main.transform : transform;
            Vector3   org  = head.position;

            Vector2 bl = WorldPointToAzEl(org, m_anchorBL);
            Vector2 br = WorldPointToAzEl(org, m_anchorBR);
            Vector2 tl = WorldPointToAzEl(org, m_anchorTL);
            Vector2 tr = WorldPointToAzEl(org, m_anchorTR);

            m_activeRect = new Vector4(
                Mathf.Min(bl.x, br.x, tl.x, tr.x), Mathf.Max(bl.x, br.x, tl.x, tr.x),
                Mathf.Min(bl.y, br.y, tl.y, tr.y), Mathf.Max(bl.y, br.y, tl.y, tr.y));
            m_material.SetVector(s_focusRectId, m_activeRect);
        }

        private static Vector2 WorldPointToAzEl(Vector3 origin, Vector3 worldPoint)
        {
            Vector3 dir = (worldPoint - origin).normalized;
            return new Vector2(
                Mathf.Atan2(dir.x, dir.z),
                Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)));
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

            // The vignette sphere renders on the Transparent queue (3000) with its bounds
            // centered on the head, so it sorts closer than the toast and draws over it —
            // default UI is also queue 3000. Queue 4100 puts the toast above the sphere
            // and the selection dots (4000). GetDefaultCanvasMaterial survives build stripping.
            m_modeUIMat = new Material(Canvas.GetDefaultCanvasMaterial()) { renderQueue = 4100 };

            // Dark background panel
            var bg   = CreateChild(m_modeUIRoot, "BG");
            var bgImg = bg.AddComponent<Image>();
            bgImg.color    = new Color(0.05f, 0.05f, 0.05f, 0.82f);
            bgImg.material = m_modeUIMat;
            StretchFill(bg);

            // Accent stripe at the top (coloured by mode)
            var stripe    = CreateChild(m_modeUIRoot, "Stripe");
            var stripeImg = stripe.AddComponent<Image>();
            stripeImg.color    = ModeAccentColor();
            stripeImg.material = m_modeUIMat;
            var stripeRt = stripe.GetComponent<RectTransform>();
            stripeRt.anchorMin = new Vector2(0f, 0.88f);
            stripeRt.anchorMax = Vector2.one;
            stripeRt.offsetMin = stripeRt.offsetMax = Vector2.zero;

            // Mode name (large, upper half)
            var nameGO   = CreateChild(m_modeUIRoot, "ModeName");
            m_modeNameText = nameGO.AddComponent<Text>();
            m_modeNameText.material = m_modeUIMat;
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
            m_modeHintText.material = m_modeUIMat;
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
            if (m_modeNameText == null || m_studyMinimalUi) return;

            int cyclePos = System.Array.IndexOf(k_modeCycle, m_vignetteMode);
            int idx   = cyclePos >= 0 ? cyclePos + 1 : 1;
            int total = k_modeCycle.Length;
            string modeName = m_vignetteMode switch
            {
                VignetteMode.Blur                => "BLUR",
                VignetteMode.SoftDark            => "SOFT DARK",
                VignetteMode.HardDark            => "HARD DARK",
                VignetteMode.TintedDark          => "TINTED DARK",
                VignetteMode.ChromaticCool       => "CHROMA COOL",
                VignetteMode.ColorPop            => "COLOR POP",
                VignetteMode.ConspicuitySqueeze  => "FLATTEN",
                VignetteMode.GranulatedPeriphery => "GRAIN",
                VignetteMode.OutlinedDark        => "OUTLINE DARK",
                _                                => "SPOTLIGHT"
            };
            string desc = m_vignetteMode switch
            {
                VignetteMode.Blur                => "Blurred + desaturated periphery",
                VignetteMode.SoftDark            => $"Gradual dark vignette  ({(int)(m_mode2MaxAlpha * 100)}% max)",
                VignetteMode.HardDark            => "Full black-out vignette",
                VignetteMode.TintedDark          => "Coloured dark vignette",
                VignetteMode.ChromaticCool       => "Warm focus / cool periphery",
                VignetteMode.ColorPop            => "Muted grey periphery; vivid colors pop",
                VignetteMode.ConspicuitySqueeze  => "Contrast balanced, gently desaturated + dimmed",
                VignetteMode.GranulatedPeriphery => "Static noise grains in periphery",
                VignetteMode.OutlinedDark        => "Blackout with edge outlines kept",
                _                                => "Brightened focus, soft dim periphery"
            };

            m_modeNameText.text = $"[{idx}/{total}]  {modeName}";
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
                VignetteMode.Blur                => new Color(0.25f, 0.55f, 1.00f, 1f), // blue
                VignetteMode.SoftDark            => new Color(1.00f, 0.65f, 0.10f, 1f), // amber
                VignetteMode.HardDark            => new Color(0.90f, 0.15f, 0.15f, 1f), // red
                VignetteMode.TintedDark          => new Color(0.55f, 0.35f, 1.00f, 1f), // violet
                VignetteMode.ChromaticCool       => new Color(0.45f, 0.90f, 0.95f, 1f), // cyan
                VignetteMode.ColorPop            => new Color(1.00f, 0.80f, 0.10f, 1f), // warm yellow
                VignetteMode.ConspicuitySqueeze  => new Color(0.60f, 0.60f, 0.65f, 1f), // neutral grey
                VignetteMode.GranulatedPeriphery => new Color(0.75f, 0.75f, 0.55f, 1f), // sand
                VignetteMode.OutlinedDark        => new Color(0.95f, 0.95f, 0.95f, 1f), // white
                _                                => new Color(1.00f, 0.95f, 0.55f, 1f)  // pale gold (spotlight)
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
            foreach (var (classId, box) in detections)
            {
                if (!k_targetClasses.Contains(classId)) continue;
                UpsertTracked(BoxToAzElRect(box, inputSize));
            }
        }

        // Nearest existing track within a radius scaled to rect size — same identity heuristic
        // VideoTestSceneManager uses (class + nearest centre), minus the class check since both
        // target classes get identical treatment downstream here.
        private void UpsertTracked(Vector4 rect)
        {
            float cx = (rect.x + rect.y) * 0.5f, cy = (rect.z + rect.w) * 0.5f;
            float size = Mathf.Max(rect.y - rect.x, rect.w - rect.z);
            float thresh = Mathf.Max(size, 0.01f) * 1.5f;
            float now = Time.time;
            for (int i = 0; i < m_trackedDets.Count; i++)
            {
                var tr = m_trackedDets[i];
                float dx = (tr.rect.x + tr.rect.y) * 0.5f - cx;
                float dy = (tr.rect.z + tr.rect.w) * 0.5f - cy;
                if (Mathf.Sqrt(dx * dx + dy * dy) < thresh)
                {
                    m_trackedDets[i] = new TrackedDet { rect = rect, firstSeenTime = tr.firstSeenTime, lastSeenTime = now, presence = tr.presence };
                    return;
                }
            }
            m_trackedDets.Add(new TrackedDet { rect = rect, firstSeenTime = now, lastSeenTime = now, presence = 0f });
        }

        private void UpdateDetectionUniforms()
        {
            // StudyEffectSuppressed means "effect forced invisible" — the detection-highlight
            // boost is part of that effect (it saturates/brightens detected objects
            // independently of _VignetteStrength), so it must be suppressed too, or a
            // baseline/no-filter condition would still visibly highlight detected objects.
            float now = Time.time;
            for (int i = 0; i < m_trackedDets.Count; i++)
            {
                var tr = m_trackedDets[i];
                bool active = !StudyEffectSuppressed && now - tr.lastSeenTime <= m_detectionLifetime;
                // Debounce: an active track still shows nothing until it has survived
                // m_detectionMinAgeSec — a false positive whose lifetime expires before it
                // matures fades from 0 to 0, i.e. never appears.
                // Size gate: rect is az/el in radians — stay silent while the object is
                // still too small to actually see (the shader would inflate it to a ~2°
                // minimum halo around nothing).
                float sizeDeg = Mathf.Max(tr.rect.y - tr.rect.x, tr.rect.w - tr.rect.z) * Mathf.Rad2Deg;
                bool visible = active
                               && now - tr.firstSeenTime >= m_detectionMinAgeSec
                               && sizeDeg >= m_detectionMinSizeDeg;
                float rate = visible ? 1f / Mathf.Max(m_detectionFadeInSeconds, 0.001f)
                                      : 1f / Mathf.Max(m_detectionFadeOutSeconds, 0.001f);
                tr.presence = Mathf.MoveTowards(tr.presence, visible ? 1f : 0f, rate * Time.deltaTime);
                m_trackedDets[i] = tr;
            }
            m_trackedDets.RemoveAll(tr => now - tr.lastSeenTime > m_detectionLifetime && tr.presence <= 0.001f);

            int slot = 0;
            foreach (var tr in m_trackedDets)
            {
                if (slot >= k_maxDetections) break;
                m_detectionRects[slot] = tr.rect;
                m_detectionFade[slot]  = tr.presence;
                slot++;
            }
            for (int i = slot; i < k_maxDetections; i++)
                m_detectionFade[i] = 0f;

            m_material.SetInt(s_detectionCountId,         slot);
            m_material.SetVectorArray(s_detectionRectsId, m_detectionRects);
            m_material.SetFloatArray(s_detectionFadeId,   m_detectionFade);
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
            if (m_debugText != null)
            {
                m_debugText.text = msg;
                // Minimal-UI test mode: messages still show (anchor feedback matters while
                // painting) but clear themselves instead of lingering as a UI artifact.
                if (m_studyMinimalUi)
                {
                    if (m_debugHideCoroutine != null) StopCoroutine(m_debugHideCoroutine);
                    m_debugHideCoroutine = StartCoroutine(HideDebugTextSoon());
                }
            }
            Debug.Log($"[CameraVignette] {msg}");
        }

        private IEnumerator HideDebugTextSoon()
        {
            yield return new WaitForSeconds(4f);
            if (m_debugText != null) m_debugText.text = "";
            m_debugHideCoroutine = null;
        }
    }
}
