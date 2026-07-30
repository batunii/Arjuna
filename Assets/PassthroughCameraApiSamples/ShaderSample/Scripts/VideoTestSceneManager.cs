// Copyright (c) Meta Platforms, Inc. and affiliates.
//
// Standalone manager for VideoTestScene.unity.
// Replaces passthrough with a looping equirectangular video sphere so the
// DR vignette modes can be evaluated without a physical passthrough stream.
//
// Setup (done at runtime, no pre-wired references required):
//   1. Disables any OVRPassthroughLayer in the scene.
//   2. Creates a large equirectangular video sphere (VideoSphereEQ shader).
//   3. Streams Assets/StreamingAssets/DebugVideo.mp4 into a RenderTexture.
//   4. Feeds the RT as _MainTexL and enables _EqCamSampling on the vignette sphere.
//
// Controls (same as main scene):
//   Right trigger (hold): paint focus window
//   A button:             cycle mode
//   B button:             clear focus window

using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace PassthroughCameraSamples.ShaderSample
{
    [MetaCodeSample("PassthroughCameraApiSamples-ShaderSample")]
    public class VideoTestSceneManager : MonoBehaviour, IStudyVignetteControl
    {
        // ---- inspector ----

        [SerializeField] private MeshRenderer m_vignetteSphereRenderer;

        [Tooltip("Optional right controller anchor for aim direction.")]
        [SerializeField] private Transform m_rightControllerAnchor;
        [SerializeField] private Transform m_cameraRig;

        [Header("Video")]
        [Tooltip("Flip the video texture vertically (platform-dependent — try toggling if video is upside-down).")]
        [SerializeField] private bool m_flipVideoY = false;
        [Tooltip("Filename to use from StreamingAssets (e.g. \"DebugVideo.mp4\") instead of the built-in " +
                 "default. Leave empty for the normal resolution order (persistentDataPath override, " +
                 "else the flat-clip/360 default). A persistentDataPath file of the SAME name still wins, " +
                 "same as the default behaviour — this only changes the STREAMING ASSETS fallback name, " +
                 "so TestModeSequencer's video-based modes can point at a specific clip from the Inspector.")]
        [SerializeField] private string m_videoFileNameOverride = "";

        [Header("UV Offset — Video Mode")]
        [Tooltip("Shift the 360° video horizontally without rotating the sphere.")]
        [SerializeField, Range(-0.5f, 0.5f)] private float m_videoUOffset = 0f;
        [Tooltip("Shift the 360° video vertically (e.g. −0.25 raises horizon by 45°).")]
        [SerializeField, Range(-0.5f, 0.5f)] private float m_videoVOffset = 0f;

        [Header("Flat Clip Mode")]
        [Tooltip("Play a FLAT (perspective/dashcam) clip reprojected onto the forward sector of the sphere instead of a 360 equirect video. Clip: persistentDataPath/flat_clip.mp4, else StreamingAssets/FlatClip.mp4. The detection track is looked up next to the clip (same name, .detections.json) — build both with Tools/lisa_to_detections.py.")]
        [SerializeField] private bool m_flatClipMode = false;
        [Tooltip("Horizontal FOV of the clip's source camera (deg). MUST match the --hfov used by Tools/lisa_to_detections.py or the detection boxes won't line up.")]
        [SerializeField, Range(30f, 120f)] private float m_flatHFovDeg = 60f;
        [Tooltip("Elevation of the clip centre on the sphere (deg, positive = up). Must match --el-center-deg in the converter.")]
        [SerializeField, Range(-30f, 30f)] private float m_flatElCenterDeg = 0f;
        [Tooltip("Colour of the sphere outside the flat clip (dim surround).")]
        [SerializeField] private Color m_flatSurroundColor = new Color(0.05f, 0.05f, 0.06f, 1f);
        [Tooltip("Flip the flat clip vertically if it appears upside-down.")]
        [SerializeField] private bool m_flatFlipY = false;
        [Tooltip("Fill the sphere around the flat clip with the standard 360 video (study_video.mp4 override, else DebugVideo.mp4) so the periphery has real motion/clutter for the filters to suppress. Off = plain surround colour.")]
        [SerializeField] private bool m_flatSurround360 = true;

        [Header("Filter")]
        [SerializeField, Range(1f, 45f)]   private float m_softEdgeDeg   = 20f;
        [Tooltip("Hard Dark uses its own, much narrower soft edge — once a selection is locked in, everything outside it should read as black almost immediately, not fade through a wide gradient buffer.")]
        [SerializeField, Range(0.5f, 20f)] private float m_hardDarkSoftEdgeDeg = 2f;
        [Tooltip("Default half-width (deg), applied symmetrically to az/el, of the focus window. " +
                 "Seeds the free-play brush size AND the fixed Blocks B/C windscreen window (single " +
                 "shared source). 0 = start from nothing and build the window entirely by painting " +
                 "(no forced minimum size). Originally set to 15° per Ball & Owsley UFOV central-field " +
                 "reasoning, but on-device testing found that felt too large in practice — tune to taste.")]
        [SerializeField, Range(0f, 30f)] private float m_defaultWindowHalfWidthDeg = 0f;

        [Header("Mode")]
        [SerializeField] private VignetteMode m_vignetteMode = VignetteMode.ColorPop;
        [SerializeField, Range(0.5f, 10f)]   private float m_vignetteFormTime = 3f;
        [SerializeField, Range(0.1f, 0.95f)] private float m_mode2MaxAlpha    = 0.75f;

        [Header("Motion Disable — Per Mode")]
	[SerializeField] private bool m_enableMotion = false;
        [SerializeField] private MotionSettings m_motionSoftDark      = new MotionSettings { speedThreshDeg = 50f,  holdSeconds = 1.0f };
        [SerializeField] private MotionSettings m_motionHardDark      = new MotionSettings { speedThreshDeg = 70f,  holdSeconds = 1.5f };
        [SerializeField] private MotionSettings m_motionColorPop      = new MotionSettings { speedThreshDeg = 35f,  holdSeconds = 0.7f };

        [SerializeField, Range(0.05f, 2f)] private float m_motionFadeOutSec = 0.20f;
        [SerializeField, Range(0.05f, 3f)] private float m_motionFadeInSec  = 0.70f;

        [Header("Selection Dots")]
        [SerializeField] private Material m_dotMaterialTemplate;
        [SerializeField] private float    m_dotSize     = 0.055f;
        [SerializeField, Range(1f, 10f)]  private float m_dotHideDelay = 3f;
        [SerializeField] private Color    m_dotColorHeld   = Color.white;
        [SerializeField] private Color    m_dotColorLocked = new Color(1f, 1f, 1f, 0.55f);
        [SerializeField] private Color    m_dotColorCursor = new Color(1f, 0.9f, 0.3f, 1f);

        [Header("Noise Texture")]
        [Tooltip("Noise source for the Gaussian sampling helper (ColorPop glare-core guard).")]
        [SerializeField] private Texture2D m_frostTex;

        [Header("Color Pop")]
        [Tooltip("Brightness of the muted grey periphery (lower = stronger pop contrast).")]
        [SerializeField, Range(0.1f, 1f)] private float m_popGreyDim   = 0.4f;
        [Tooltip("Saturation boost applied to kept ROG colors INSIDE the focus window.")]
        [SerializeField, Range(1f, 3f)]   private float m_popSatBoost  = 1.7f;
        [Tooltip("How much non-ROG colors are desaturated INSIDE the window (0 = untouched).")]
        [SerializeField, Range(0f, 1f)]   private float m_popInsideDesat = 0.25f;
        [Tooltip("Brightness multiplier on ROG colors inside the window (>1 lifts them above the scene).")]
        [SerializeField, Range(1f, 1.5f)] private float m_popBrightInside = 1.15f;
        [Tooltip("Brightness multiplier on ROG colors outside the window (<1 keeps them below the true scene but above the grey periphery).")]
        [SerializeField, Range(0.3f, 1f)] private float m_popBrightOutside = 0.65f;
        [Tooltip("Saturation floor for the red/orange band — keeps warm-white headlights from passing as orange.")]
        [SerializeField, Range(0f, 0.8f)] private float m_popWarmSatMin = 0.35f;
        [Tooltip("Sigmoidal midtone contrast on ROG colors inside the window (Sutton 2022, alpha=10 beta=0.5). 0 = linear boost only.")]
        [SerializeField, Range(0f, 1f)]   private float m_popSigmoid = 0.6f;
        [Tooltip("Overall luminance dim on the whole unselected region, ROG included (1 = off).")]
        [SerializeField, Range(0.4f, 1f)] private float m_popPeriphDim = 0.85f;
        [Tooltip("Luma above which unsaturated non-ROG pixels count as glare (headlights).")]
        [SerializeField, Range(0.3f, 1f)] private float m_popGlareKnee = 0.6f;
        [Tooltip("Glare compression strength inside the focus window (0 = off).")]
        [SerializeField, Range(0f, 1f)]   private float m_popGlareInside = 0.35f;
        [Tooltip("Glare compression strength in the periphery.")]
        [SerializeField, Range(0f, 1f)]   private float m_popGlareOutside = 0.85f;
        [Tooltip("Local-surround radius (UV) for the blown-core guard: white lamp cores with a saturated fringe are exempt from glare dimming. 0 disables the guard.")]
        [SerializeField, Range(0f, 0.03f)] private float m_popGuardRadius = 0.008f;
        [Tooltip("ColorPop uses its own (wider) focus-window soft edge — a colour/grey boundary reads harsher than a blur boundary.")]
        [SerializeField, Range(1f, 60f)]  private float m_popSoftEdgeDeg = 32f;

        [Header("Sign Pop")]
        [Tooltip("Graceful miss: how much of the kept-ROG treatment UNDETECTED ROG pixels retain (0 = full grey like other colours, 1 = identical to ColorPop). A bake miss dims a real signal instead of hiding it.")]
        [SerializeField, Range(0f, 1f)] private float m_signRogFallback = 0.35f;
        [Tooltip("Hold a baked detection for this many video-seconds after it vanishes from the track, so pops don't blink between bake samples.")]
        [SerializeField, Range(0f, 2f)] private float m_signDetHoldSec = 0.5f;

        [Header("Person Detection")]
        [Tooltip("Minimum apparent box height (deg) for a detected person to count as 'close' — bigger box = closer. " +
                 "This is a box-size proxy for distance: the bake is monocular/offline with no depth sensor, and " +
                 "YOLO11 has no built-in distance estimation, so apparent size is the practical signal. Rough " +
                 "mapping for a ~1.7 m person: 6° ≈ within ~16 m, 3° ≈ within ~32 m, 2° ≈ within ~49 m. Tune live; " +
                 "no re-bake needed.")]
        [SerializeField, Range(0f, 30f)] private float m_personMinBoxHeightDeg = 3f;
        [Tooltip("Maximum angular distance (deg) OUTSIDE the focus window for a close-enough person to still " +
                 "count as near the region (about to enter / leaving / standing near). Only matters when the " +
                 "person is outside the window — a close-enough person already INSIDE the window is always " +
                 "shown, regardless of this value. No region active = no persons shown (undefined 'edge'). " +
                 "Also the end of the graded strength taper: full effect near the edge, zero at this distance. " +
                 "Measured on the study bake, 25° covers ~52% of close peripheral persons (8° covered only ~23%).")]
        [SerializeField, Range(0f, 60f)] private float m_personMaxEdgeDistDeg = 25f;
        [Tooltip("Distance outside the window edge (deg) up to which a close person keeps FULL effect strength; " +
                 "beyond it the effect tapers linearly, reaching zero at Person Max Edge Dist. Clamped below " +
                 "that value at runtime. Raise toward the max for a hard-edged reach, lower for a gentler falloff.")]
        [SerializeField, Range(0f, 60f)] private float m_personFullStrengthDeg = 12.5f;
        [Tooltip("Graded person-effect floor at the window CENTRE: strength ramps linearly from this floor at " +
                 "dead-centre up to full (1) at the window edge, staying full outside until the reach taper — " +
                 "so the emphasis relaxes only when the person is actually in front of the user, and grows " +
                 "again as they drift toward the edge or leave.")]
        [SerializeField, Range(0f, 1f)] private float m_personInsideScale = 0.25f;
        [Tooltip("Widen the person cone with closeness: extra acceptance degrees per degree of box height ABOVE the minimum (deg/deg). The same pedestrian slides OUTWARD in the view as you approach — eccentricity grows exactly as they get closest — so a fixed cone drops people at their nearest point. This widens both the accept limit and the full-strength band with apparent size, which is geometrically ~a constant lateral corridor in metres (x ≈ 1.7 m × angle_rad / boxHeight_rad). 0 = old fixed-cone behaviour. At 3 (default; 5 was visually noisy in city footage): 3° box (~32 m) → cone unchanged (25°); 6° (~16 m) → 34°; 10° (~10 m) → 46°.")]
        [SerializeField, Range(0f, 15f)] private float m_personCloseWideningDegPerDeg = 2.5f;

        [Header("YOLO Detection")]
        [Tooltip("Drag the YoloRunner component here; it will read from the video RenderTexture instead of the passthrough camera.")]
        [SerializeField] private YoloRunner m_yoloRunner;
        [Tooltip("Live-YOLO fallback only: how long a detection slot stays lit after inference last reported it. The BAKED track path (what this scene normally runs) refreshes slot timestamps every frame and expires tracks through the tracker instead, so this knob has NO visible effect there — tune m_signDetHoldSec, the detection fade in/out, and m_detectionMinAgeSec instead.")]
        [SerializeField] private float      m_detectionLifetime = 0.6f;
        [Tooltip("Flip detection box Y. Toggle if detected zones appear at mirror-image elevations.")]
        [SerializeField] private bool       m_yoloFlipY = true;
        [Tooltip("Soft edge on detection clear zones (degrees).")]
        [SerializeField, Range(0f, 10f)] private float m_detectionSoftEdgeDeg = 3f;
        [Tooltip("Saturation/brightness boost on detected objects (0=clear only, 1=vivid).")]
        [SerializeField, Range(0f, 1f)]  private float m_detectionEnhance = 0.8f;
        [Tooltip("How much the annulus around a detected object is darkened (center-surround contrast).")]
        [SerializeField, Range(0f, 0.5f)] private float m_detectionSurround = 0.15f;
        [Tooltip("Amplitude of the gentle ~1 Hz breathing on the object boost (0 = static).")]
        [SerializeField, Range(0f, 1f)] private float m_detectionPulseAmp = 0.25f;
        [Header("Detection Timing")]
        [Tooltip("How long (seconds) a detection's window/highlight takes to fade in when it first appears. Quick, so the effect still feels responsive.")]
        [SerializeField, Range(0.02f, 2f)] private float m_detectionFadeInSeconds = 0.2f;
        [Tooltip("How long (seconds) a detection's window/highlight takes to fade out after it vanishes/holds out — gentler than fade-in so it doesn't snap away.")]
        [SerializeField, Range(0.02f, 2f)] private float m_detectionFadeOutSeconds = 0.5f;
        [Tooltip("Delay window opening this many seconds AFTER the object first appears on screen (0 = open instantly). Visibility-envelope only: box positions always track the current frame, so this can never draw a window ahead of the object.")]
        [SerializeField, Range(0f, 1.5f)] private float m_detectionOnsetDelaySec = 0f;
        [Tooltip("Keep the window open this many seconds AFTER the object's known lifetime ends (box holds its last position, then the fade-out follows). 0 = start fading the moment the object is gone.")]
        [SerializeField, Range(0f, 1.5f)] private float m_detectionHoldPastEndSec = 0f;
        [Tooltip("Minimum FULL baked lifetime (seconds) a detection's real-world object must have for its window to ever show. Evaluated against the lifetime spans precomputed at load — the whole future is known offline — so real detections appear the INSTANT they arrive (no onset delay, no position lead) while single-sample YOLO blips never appear at all. 0.6 requires 2+ consecutive bake samples. 0 = off.")]
        [SerializeField, Range(0f, 3f)] private float m_detectionMinAgeSec = 0.6f;
        [Header("Detection Window Grading")]
        [Tooltip("Extra saturation inside detection windows: how far colours are pushed past natural (0 = the old subtle lift, higher = vivid). Scaled by Detection Colour Boost and the breathing pulse.")]
        [SerializeField, Range(0f, 2f)] private float m_detectionSatLift = 0.85f;
        [Tooltip("Brightness lift inside detection windows (fraction, scaled by Detection Colour Boost).")]
        [SerializeField, Range(0f, 1f)] private float m_detectionBrightLift = 0.18f;
        [Tooltip("Contrast expansion around mid-grey inside detection windows (0 = none, scaled by Detection Colour Boost).")]
        [SerializeField, Range(0f, 1f)] private float m_detectionContrast = 0.2f;
        [Tooltip("Minimum apparent size (deg, max of az/el extent) a detection's box must currently have to be visible. The shader inflates every detection to a ~2° minimum halo so distant lights stay noticeable — but below ~1.2° the object itself is still a speck, so the inflated circle reads as a window opening over nothing. Measured on the current bake, 33 of the 246 post-debounce traffic-light lifetimes never exceed 1.2°. Gated per-frame on the current box, so the window still appears naturally once an approaching light grows past the gate. Persons are exempt (PassesPersonGate already requires close/large boxes). 0 = off.")]
        [SerializeField, Range(0f, 5f)] private float m_detectionMinSizeDeg = 1.2f;
        [Tooltip("Scales the saliency-boost highlight (colour/brightness lift + surround dim) down for detections OUTSIDE the active focus window — the window opening around the object is already enough there, so the extra pop stays subtle. Detections INSIDE the window, or when no window is active, always get full strength; this only softens the periphery case.")]
        [SerializeField, Range(0f, 1f)] private float m_detectionOutsideFocusScale = 0.35f;

        [Header("Baked Detections")]
        [Tooltip("If a baked detection track exists (StreamingAssets/DebugVideo.detections.json), use it instead of live YOLO.")]
        [SerializeField] private bool m_useBakedDetections = true;

        [Header("Debug")]
        [Tooltip("Show raw video RenderTexture in the bottom-left corner (screen-space). " +
                 "If this preview rotates, the video file itself is rotating (metadata/encoding). " +
                 "If it's stable, the issue is in the sphere mapping.")]
        [SerializeField] private bool m_debugVideoPreview = false;

        // ---- shader IDs ----
        private static readonly int s_mainTexLId          = Shader.PropertyToID("_MainTexL");
        private static readonly int s_sphereCenterId      = Shader.PropertyToID("_SphereCenter");
        private static readonly int s_camLFwdId           = Shader.PropertyToID("_CamLFwd");
        private static readonly int s_camLRtId            = Shader.PropertyToID("_CamLRt");
        private static readonly int s_camLUpId            = Shader.PropertyToID("_CamLUp");
        private static readonly int s_tanHalfFovLId       = Shader.PropertyToID("_TanHalfFovL");
        private static readonly int s_hasRightCamId       = Shader.PropertyToID("_HasRightCam");
        private static readonly int s_focusRectId         = Shader.PropertyToID("_FocusRect");
        private static readonly int s_softEdgeId          = Shader.PropertyToID("_SoftEdge");
        private static readonly int s_detectionCountId    = Shader.PropertyToID("_DetectionCount");
        private static readonly int s_detectionRectsId    = Shader.PropertyToID("_DetectionRects");
        private static readonly int s_detectionSoftEdgeId = Shader.PropertyToID("_DetectionSoftEdge");
        private static readonly int s_detectionEnhanceId  = Shader.PropertyToID("_DetectionEnhance");
        private static readonly int s_detectionSurroundId   = Shader.PropertyToID("_DetectionSurround");
        private static readonly int s_detectionPulseAmpId   = Shader.PropertyToID("_DetectionPulseAmp");
        private static readonly int s_detectionFadeId          = Shader.PropertyToID("_DetectionFade");
        private static readonly int s_detectionOutsideScaleId  = Shader.PropertyToID("_DetectionOutsideScale");
        private static readonly int s_detectionSatLiftId       = Shader.PropertyToID("_DetectionSatLift");
        private static readonly int s_detectionBrightLiftId    = Shader.PropertyToID("_DetectionBrightLift");
        private static readonly int s_detectionContrastId      = Shader.PropertyToID("_DetectionContrast");
        private static readonly int s_suppressDetectionWindowsId = Shader.PropertyToID("_SuppressDetectionWindows");
        private static readonly int s_simpleModeId        = Shader.PropertyToID("_SimpleMode");
        private static readonly int s_tintModeId          = Shader.PropertyToID("_TintMode");
        private static readonly int s_chromaticCoolId     = Shader.PropertyToID("_ChromaticCool");
        private static readonly int s_squeezeModeId       = Shader.PropertyToID("_SqueezeMode");
        private static readonly int s_grainModeId         = Shader.PropertyToID("_GrainMode");
        private static readonly int s_outlineModeId       = Shader.PropertyToID("_OutlineMode");
        private static readonly int s_spotLiftModeId      = Shader.PropertyToID("_SpotLiftMode");
        private static readonly int s_vignetteStrengthId  = Shader.PropertyToID("_VignetteStrength");
        private static readonly int s_maxVignetteAlphaId  = Shader.PropertyToID("_MaxVignetteAlpha");
        private static readonly int s_frostTexId          = Shader.PropertyToID("_FrostTex");
        private static readonly int s_colorPopModeId      = Shader.PropertyToID("_ColorPopMode");
        private static readonly int s_popGreyDimId        = Shader.PropertyToID("_PopGreyDim");
        private static readonly int s_popSatBoostId       = Shader.PropertyToID("_PopSatBoost");
        private static readonly int s_popInsideDesatId    = Shader.PropertyToID("_PopInsideDesat");
        private static readonly int s_popBrightInId       = Shader.PropertyToID("_PopBrightIn");
        private static readonly int s_popBrightOutId      = Shader.PropertyToID("_PopBrightOut");
        private static readonly int s_popWarmSatMinId     = Shader.PropertyToID("_PopWarmSatMin");
        private static readonly int s_popSigmoidId        = Shader.PropertyToID("_PopSigmoid");
        private static readonly int s_popPeriphDimId      = Shader.PropertyToID("_PopPeriphDim");
        private static readonly int s_popGlareKneeId      = Shader.PropertyToID("_PopGlareKnee");
        private static readonly int s_popGlareInsideId    = Shader.PropertyToID("_PopGlareInside");
        private static readonly int s_popGlareOutsideId   = Shader.PropertyToID("_PopGlareOutside");
        private static readonly int s_popGuardRadiusId    = Shader.PropertyToID("_PopGuardRadius");
        private static readonly int s_popDetGateId        = Shader.PropertyToID("_PopDetGate");
        private static readonly int s_popDetFallbackId    = Shader.PropertyToID("_PopDetFallback");
        private static readonly int s_eqCamSamplingId     = Shader.PropertyToID("_EqCamSampling");
        private static readonly int s_passThroughModeId   = Shader.PropertyToID("_PassthroughMode");
        private static readonly int s_flipYId             = Shader.PropertyToID("_FlipY");
        private static readonly int s_eqUOffsetId         = Shader.PropertyToID("_EqUOffset");
        private static readonly int s_eqVOffsetId         = Shader.PropertyToID("_EqVOffset");

        // Blob probe (Route B) — a low-salience click-target composited as a local modulation
        // of the scene pixels inside the vignette shader (BlobTargetController drives these).
        private const int k_maxBlobs = 12;
        private readonly Vector4[] m_blobData  = new Vector4[k_maxBlobs];
        private readonly Vector4[] m_blobFlash = new Vector4[k_maxBlobs];
        private static readonly int s_blobCountId     = Shader.PropertyToID("_BlobCount");
        private static readonly int s_blobDataId       = Shader.PropertyToID("_BlobData");
        private static readonly int s_blobFlash4Id     = Shader.PropertyToID("_BlobFlash4");
        private static readonly int s_blobSigmaId      = Shader.PropertyToID("_BlobSigma");
        private static readonly int s_blobStyleId      = Shader.PropertyToID("_BlobStyle");
        private static readonly int s_blobDesatId      = Shader.PropertyToID("_BlobDesat");
        private static readonly int s_blobDimId        = Shader.PropertyToID("_BlobDim");
        private static readonly int s_blobRimId        = Shader.PropertyToID("_BlobRim");
        private static readonly int s_blobLensId       = Shader.PropertyToID("_BlobLens");
        private static readonly int s_blobRingColorId  = Shader.PropertyToID("_BlobRingColor");
        private static readonly int s_blobRingWidthId  = Shader.PropertyToID("_BlobRingWidth");
        private static readonly int s_blobRingSegmentsId = Shader.PropertyToID("_BlobRingSegments");
        private static readonly int s_blobRingOutlineId  = Shader.PropertyToID("_BlobRingOutline");
        private static readonly int s_blobRingBehindFilterId = Shader.PropertyToID("_BlobRingBehindFilter");
        private static readonly int s_blobFlashColorId = Shader.PropertyToID("_BlobFlashColor");

        // FlatClipToEquirect composite material (flat clip mode)
        private static readonly int s_flatMainTexId  = Shader.PropertyToID("_MainTex");
        private static readonly int s_flatTanHalfId  = Shader.PropertyToID("_TanHalf");
        private static readonly int s_flatAzElHalfId = Shader.PropertyToID("_AzElHalf");
        private static readonly int s_flatSurroundId = Shader.PropertyToID("_Surround");
        private static readonly int s_flatFlipYId    = Shader.PropertyToID("_FlipY");

        // A-button cycle — every mode in the enum, in enum order, matching the passthrough
        // scene's cycle so the same button walks the same list in both. The study still uses
        // only ColorPop / SoftDark / HardDark; the rest are free-play/demo modes. SignPop
        // (detection-gated ColorPop) is video's own, gated by the baked track.
        private static readonly VignetteMode[] k_modeCycle =
        {
            VignetteMode.Blur, VignetteMode.SoftDark, VignetteMode.HardDark,
            VignetteMode.TintedDark, VignetteMode.ChromaticCool, VignetteMode.ColorPop,
            VignetteMode.ConspicuitySqueeze, VignetteMode.GranulatedPeriphery,
            VignetteMode.OutlinedDark, VignetteMode.SpotLift, VignetteMode.SignPop
        };

        private static readonly Vector4 k_fullSphere =
            new(-Mathf.PI, Mathf.PI, -Mathf.PI * 0.5f, Mathf.PI * 0.5f);

        // ---- runtime state ----
        private Material      m_material;
        private RenderTexture m_videoRT;
        private RenderTexture m_yoloRT;   // small downsampled RT for YOLO — avoids reading 4K on GPU
        private VideoPlayer   m_videoPlayer;
        private GameObject    m_videoPreviewRoot;

        // Flat clip mode
        private RenderTexture m_flatRT;           // native-res decode target
        private Material      m_flatCompositeMat; // FlatClipToEquirect
        private bool          m_flatCleared;
        private string        m_resolvedVideoUrl;
        private VideoPlayer   m_surroundPlayer;   // 360 periphery around the flat clip
        private RenderTexture m_surroundRT;

        private GameObject[]  m_selectionDots;
        private Material[]    m_dotMats;
        private const float   k_dotDistance = 4f;

        private Vector4 m_activeRect = k_fullSphere;
        // The window the person gate/grading actually tests against: the painted rect when
        // one exists, else the pop-mode gaze auto-follow rect (which previously only reached
        // the shader — so with nothing painted, m_activeRect stayed full-sphere and
        // PassesPersonGate failed closed: persons were NEVER highlighted in free-play/
        // TestModeSequencer SignPop. Now the gaze window counts as "the region".)
        private Vector4 m_effectiveRect = k_fullSphere;
        private VideoDetectionTrack m_bakedTrack;

        private float     m_vignetteStrength = 1f;
        private Coroutine m_formCoroutine;

        private Quaternion m_lastHeadRot;
        private float      m_motionDisableTimer;
        private float      m_motionSuppression;

        private Coroutine m_dotHideCoroutine;
        private bool      m_cornerDotsHidden;

        private GameObject  m_modeUIRoot;
        private Material    m_modeUIMat;
        private CanvasGroup m_modeUIGroup;
        private Text        m_modeNameText;
        private Text        m_modeHintText;
        private float       m_modeUITimer;
        private const float k_modeUIShowTime = 2.8f;
        private const float k_modeUIFadeDur  = 0.35f;

        // 16 slots (matches _DetectionRects[16] in the shader): the full-res bake finds a
        // median of 12 lights/signs per sample, so 8 slots dropped ~a third of them.
        private const int k_maxDetections = 16;
        private readonly Vector4[] m_detectionRects      = new Vector4[k_maxDetections];
        private readonly float[]   m_detectionTimestamps = new float[k_maxDetections];
        private readonly float[]   m_detectionFade       = new float[k_maxDetections];
        // COCO: 9 = traffic light, 11 = stop sign, 0 = person. Person detections pass this
        // class filter but are then further gated by PassesPersonGate (below) — only people
        // near the focus-window edge AND close to the camera are ever shown.
        private static readonly HashSet<int> k_targetClasses = new() { 0, 9, 11 };
        private const int k_personClassId = 0;

        // SignPop detection tracker: bake samples are 0.25-0.5 s apart, so raw per-sample
        // slot fills blink as boxes come and go. Identities are matched across the two
        // bracketing samples (class + centre proximity), lerped between them, and held
        // for m_signDetHoldSec after they vanish. Boxes tracked in normalized video space.
        // `presence` (0..1) drives the shader's fade in/out (see m_detectionFadeInSeconds/
        // m_detectionFadeOutSeconds below) — it ramps toward 1 while the track is live/held
        // and toward 0 once the hold window expires, so the "window" around a detection
        // opens/closes smoothly instead of popping. Only removed from the list once it has
        // fully faded out (presence <= 0), not the instant the hold window expires.
        // `lifeStart`/`lifeEnd` are the full baked lifetime of the real-world object this
        // track belongs to, precomputed at load (BuildRuntimeLifetimeSpans) — visibility is
        // decided from them instantly (lifetime >= m_detectionMinAgeSec shows from its first
        // frame; a blip never shows), replacing the old wait-out-the-debounce approach whose
        // lead-time compensation drew boxes at future positions, ahead of the object.
        private struct TrackedDet { public Vector4 box; public int cls; public float lifeStart; public float lifeEnd; public float lastSeenVt; public float presence; }
        private readonly List<TrackedDet> m_trackedDets = new();
        private float m_lastVideoVt = -1f;
        // [sampleIdx][detIdx] = (lifetime start, lifetime end) of the real-world object the
        // detection belongs to — precomputed once at load (BuildRuntimeLifetimeSpans), same
        // identity heuristic as the tracker. Only filled for k_targetClasses entries.
        private Vector2[][] m_entryLifeSpan;

        // ---- Study/BlobTargetController support ----

        /// <summary>One real-world object's full on-screen lifetime, reconstructed by walking the
        /// whole baked track once (offline pass, not tied to the live playhead — contrast with
        /// m_trackedDets, which is the rolling live tracker). Same identity heuristic as
        /// UpsertTracked/FindMatch: same class, nearest centre within a radius scaled to box size.</summary>
        public struct DetectionLifetime
        {
            public int cls;
            public float tStart;
            public float tEnd;
            /// <summary>Raw normalized boxes at each sample this instance was seen, in order —
            /// bracket-lerp between the two samples straddling a given time, same as the live tracker.</summary>
            public List<(float t, Vector4 box)> samples;
        }

        private class OpenLifetime
        {
            public int cls;
            public float cx, cy, sz;
            public float tStart, tLast;
            public List<(float t, Vector4 box)> samples = new();
        }

        /// <summary>Reconstructs every DetectionLifetime for the given classes across the entire
        /// baked track (e.g. {9, 11} for traffic lights + stop signs — SignPop's "lights & signs").
        /// Call once (it's an O(samples) pass, not per-frame) — e.g. when a test-mode blob
        /// controller first needs a pool of real candidates to select from.</summary>
        public List<DetectionLifetime> BuildDetectionLifetimes(HashSet<int> classIds, float holdSeconds)
        {
            var closed = new List<DetectionLifetime>();
            if (m_bakedTrack == null) return closed;

            var open = new List<OpenLifetime>();
            foreach (var s in m_bakedTrack.samples)
            {
                float t = s.t;
                var dets = new List<BakedDetection>();
                foreach (var d in s.d) if (classIds.Contains(d.c)) dets.Add(d);

                var matchedIdx = new HashSet<int>();
                foreach (var tr in open)
                {
                    int bestI = -1;
                    float bestDist = Mathf.Max(tr.sz, 0.01f) * 1.5f;
                    for (int i = 0; i < dets.Count; i++)
                    {
                        if (matchedIdx.Contains(i) || dets[i].c != tr.cls) continue;
                        var d = dets[i];
                        float cx = (d.x1 + d.x2) * 0.5f, cy = (d.y1 + d.y2) * 0.5f;
                        float dist = Mathf.Sqrt((cx - tr.cx) * (cx - tr.cx) + (cy - tr.cy) * (cy - tr.cy));
                        if (dist < bestDist) { bestDist = dist; bestI = i; }
                    }
                    if (bestI >= 0)
                    {
                        matchedIdx.Add(bestI);
                        var d = dets[bestI];
                        tr.cx = (d.x1 + d.x2) * 0.5f; tr.cy = (d.y1 + d.y2) * 0.5f;
                        tr.sz = Mathf.Max(d.x2 - d.x1, d.y2 - d.y1);
                        tr.tLast = t;
                        tr.samples.Add((t, new Vector4(d.x1, d.y1, d.x2, d.y2)));
                    }
                }

                for (int i = open.Count - 1; i >= 0; i--)
                {
                    if (t - open[i].tLast > holdSeconds)
                    {
                        var tr = open[i];
                        closed.Add(new DetectionLifetime { cls = tr.cls, tStart = tr.tStart, tEnd = tr.tLast, samples = tr.samples });
                        open.RemoveAt(i);
                    }
                }

                for (int i = 0; i < dets.Count; i++)
                {
                    if (matchedIdx.Contains(i)) continue;
                    var d = dets[i];
                    var tr = new OpenLifetime
                    {
                        cls = d.c,
                        cx = (d.x1 + d.x2) * 0.5f, cy = (d.y1 + d.y2) * 0.5f,
                        sz = Mathf.Max(d.x2 - d.x1, d.y2 - d.y1),
                        tStart = t, tLast = t,
                    };
                    tr.samples.Add((t, new Vector4(d.x1, d.y1, d.x2, d.y2)));
                    open.Add(tr);
                }
            }
            foreach (var tr in open)
                closed.Add(new DetectionLifetime { cls = tr.cls, tStart = tr.tStart, tEnd = tr.tLast, samples = tr.samples });
            return closed;
        }

        /// <summary>Same normalized-box → az/el-rect (radians: azMin,azMax,elMin,elMax) conversion
        /// UpdateBakedDetections uses to draw boxes — so a blob placed via this lines up exactly
        /// with "where the box would have been".</summary>
        public Vector4 DetectionBoxToAzElRect(Vector4 box) => BoxToAzElRectEQ(box, Vector2Int.one);

        // Precompute every target-class detection's full lifetime (same identity heuristic as
        // the live tracker: same class, nearest centre within a size-scaled radius, closed
        // after m_signDetHoldSec unseen) and stamp (tStart, tEnd) onto each raw entry — the
        // tracker then decides visibility the moment an object first appears, from its whole
        // (offline-known) future instead of waiting out a real-time debounce.
        private void BuildRuntimeLifetimeSpans()
        {
            var samples = m_bakedTrack.samples;
            m_entryLifeSpan = new Vector2[samples.Count][];
            var open = new List<OpenSpanTrack>();
            var closed = new List<OpenSpanTrack>();
            for (int i = 0; i < samples.Count; i++)
            {
                var s = samples[i];
                m_entryLifeSpan[i] = new Vector2[s.d.Count];
                float t = s.t;

                var matched = new HashSet<int>();
                foreach (var tr in open)
                {
                    int bestJ = -1;
                    float bestDist = Mathf.Max(tr.sz, 0.01f) * 1.5f;
                    for (int j = 0; j < s.d.Count; j++)
                    {
                        var d = s.d[j];
                        if (matched.Contains(j) || d.c != tr.cls) continue;
                        float cx = (d.x1 + d.x2) * 0.5f, cy = (d.y1 + d.y2) * 0.5f;
                        float dist = Mathf.Sqrt((cx - tr.cx) * (cx - tr.cx) + (cy - tr.cy) * (cy - tr.cy));
                        if (dist < bestDist) { bestDist = dist; bestJ = j; }
                    }
                    if (bestJ >= 0)
                    {
                        matched.Add(bestJ);
                        var d = s.d[bestJ];
                        tr.cx = (d.x1 + d.x2) * 0.5f; tr.cy = (d.y1 + d.y2) * 0.5f;
                        tr.sz = Mathf.Max(d.x2 - d.x1, d.y2 - d.y1);
                        tr.tLast = t;
                        tr.entries.Add((i, bestJ));
                    }
                }

                for (int k = open.Count - 1; k >= 0; k--)
                {
                    if (t - open[k].tLast > m_signDetHoldSec)
                    {
                        closed.Add(open[k]);
                        open.RemoveAt(k);
                    }
                }

                for (int j = 0; j < s.d.Count; j++)
                {
                    if (matched.Contains(j)) continue;
                    var d = s.d[j];
                    if (k_targetClasses != null && !k_targetClasses.Contains(d.c)) continue;
                    var tr = new OpenSpanTrack
                    {
                        cls = d.c,
                        cx = (d.x1 + d.x2) * 0.5f, cy = (d.y1 + d.y2) * 0.5f,
                        sz = Mathf.Max(d.x2 - d.x1, d.y2 - d.y1),
                        tStart = t, tLast = t,
                    };
                    tr.entries.Add((i, j));
                    open.Add(tr);
                }
            }
            closed.AddRange(open);
            foreach (var tr in closed)
                foreach (var (si, di) in tr.entries)
                    m_entryLifeSpan[si][di] = new Vector2(tr.tStart, tr.tLast);
        }

        private class OpenSpanTrack
        {
            public int cls;
            public float cx, cy, sz;
            public float tStart, tLast;
            public List<(int si, int di)> entries = new();
        }

        // ---- lifecycle ----

        private void Start()
        {
            // Disable passthrough (this scene uses video as background)
            foreach (var cam in Camera.allCameras)
            {
                cam.clearFlags      = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }
            var ptLayer = FindObjectOfType<OVRPassthroughLayer>();
            if (ptLayer != null) ptLayer.enabled = false;

            InitSelectionDots();
            InitModeUI();

            // The cycle now covers the whole enum, so this only catches a genuinely invalid
            // serialized value.
            if (Array.IndexOf(k_modeCycle, m_vignetteMode) < 0)
                m_vignetteMode = VignetteMode.ColorPop;

            m_material = m_vignetteSphereRenderer.material;
            m_material.SetVector(s_focusRectId,    m_activeRect);
            m_material.SetFloat(s_hasRightCamId,    0f);
            m_material.SetFloat(s_eqCamSamplingId,  1f);  // equirectangular UV for 360° video
            m_material.SetFloat(s_passThroughModeId, 0f); // video mode: single opaque sphere
            m_material.SetFloat(s_flipYId,          m_flipVideoY ? 1f : 0f);
            m_material.SetInt(s_detectionCountId,   0);

            // All mode toggles are driven per-frame by UpdateModeUniforms now that the cycle
            // covers the whole enum — no stale-value zeroing needed here.
            m_material.SetInt(s_blobCountId,      0); // no blob probes until a controller pushes some

            if (Camera.main != null) m_lastHeadRot = Camera.main.transform.rotation;

            SetupVideoSphere();
            StartCoroutine(InitDetections());
            if (m_debugVideoPreview) CreateVideoPreview();
            ShowModeToast();
        }

        // Prefer the baked detection track (offline pre-scan of the video — stable coordinates,
        // zero runtime inference); fall back to live YOLO when no track file exists.
        private IEnumerator InitDetections()
        {
            var baker = FindObjectOfType<VideoDetectionBaker>();
            if (baker != null && baker.BakeOnPlay) yield break; // baker owns the engine this run

            if (m_useBakedDetections)
            {
                string path;
                if (m_flatClipMode)
                {
                    // Flat mode: the track lives next to the resolved clip (same name,
                    // .detections.json) — works for sideloaded, bundled, and jar: URLs.
                    path = m_resolvedVideoUrl.Replace(".mp4", ".detections.json");
                }
                else
                {
                    // Prefer sideloaded detections that match the sideloaded study video
                    // (adb push to persistentDataPath/study_video.detections.json); else
                    // the bundled bake for DebugVideo.mp4.
                    string overrideDet = System.IO.Path.Combine(Application.persistentDataPath, "study_video.detections.json");
                    path = System.IO.File.Exists(overrideDet)
                        ? overrideDet
                        : System.IO.Path.Combine(Application.streamingAssetsPath, "DebugVideo.detections.json");
                }
                yield return VideoDetectionTrack.Load(path, tr => m_bakedTrack = tr);
                if (m_bakedTrack != null && m_bakedTrack.samples.Count > 0)
                {
                    BuildRuntimeLifetimeSpans();
                    Debug.Log($"[VideoTestScene] Using baked detection track " +
                              $"({m_bakedTrack.samples.Count} samples @ {m_bakedTrack.interval}s).");
                    yield break;
                }
            }

            if (m_yoloRunner == null) m_yoloRunner = GetComponent<YoloRunner>();
            if (m_yoloRunner != null)
            {
                // Feed a small RT to YOLO instead of the 4K video RT.
                // TextureConverter reading 3840x2160 on the GPU competes with rendering and causes stutter.
                // Blitting down to 640x360 first is cheap; YOLO inference then reads ~36x fewer pixels.
                m_yoloRT = new RenderTexture(640, 360, 0, RenderTextureFormat.ARGB32);
                m_yoloRT.Create();
                m_yoloRunner.SetOverrideRT(m_yoloRT);
                m_yoloRunner.SetBlitSource(m_videoRT); // blit happens inside YoloRunner, not every frame
                m_yoloRunner.OnDetectionsReady += OnDetectionsReady;
            }
        }

        private void CreateVideoPreview()
        {
            m_videoPreviewRoot = new GameObject("VideoDebugPreview");

            var canvas = m_videoPreviewRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            // Bottom-left quarter of the screen
            var imgGO = new GameObject("PreviewImage");
            imgGO.transform.SetParent(m_videoPreviewRoot.transform, false);
            var raw = imgGO.AddComponent<UnityEngine.UI.RawImage>();
            raw.texture = m_videoRT;

            var rt = imgGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(0.35f, 0.22f);
            rt.offsetMin = new Vector2(8f, 8f);
            rt.offsetMax = new Vector2(-4f, -4f);

            // Label
            var labelGO = new GameObject("PreviewLabel");
            labelGO.transform.SetParent(m_videoPreviewRoot.transform, false);
            var txt = labelGO.AddComponent<Text>();
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                         ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize  = 18;
            txt.color     = new Color(1f, 1f, 0.3f, 0.9f);
            txt.text      = "RAW VIDEO RT\n(rotates = video file issue)";
            txt.alignment = TextAnchor.LowerLeft;
            var lrt = labelGO.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = new Vector2(0.35f, 0.22f);
            lrt.offsetMin = new Vector2(8f, 8f);
            lrt.offsetMax = new Vector2(-4f, -4f);
        }

        private void SetupVideoSphere()
        {
            // 2:1 equirectangular target — matches the 360 study clip so the shader's
            // [0,1] UVs map 1:1 onto the sphere (no letterbox/stretch distortion).
            // 6K (5760x2880) preserves the higher-res transcode instead of squeezing to 4K;
            // if this costs framerate on-device, step back down to 3840x1920.
            m_videoRT = new RenderTexture(5760, 2880, 0, RenderTextureFormat.ARGB32);
            m_videoRT.Create();

            // The vignette sphere is the only renderer — no separate background sphere.
            // VideoPlayer decodes into the RT; the vignette shader samples it directly.
            m_videoPlayer               = gameObject.AddComponent<VideoPlayer>();
            m_videoPlayer.playOnAwake   = false;
            m_videoPlayer.renderMode    = VideoRenderMode.RenderTexture;
            m_videoPlayer.isLooping     = true;
            m_videoPlayer.skipOnDrop    = true;
            // Prefer a sideloaded video (adb push to persistentDataPath) so large clips
            // stay OUT of the APK; fall back to the bundled StreamingAssets file.
            string overrideName = m_flatClipMode ? "flat_clip.mp4" : "study_video.mp4";
            string bundledName  = !string.IsNullOrEmpty(m_videoFileNameOverride)
                ? m_videoFileNameOverride
                : (m_flatClipMode ? "FlatClip.mp4" : "DebugVideo.mp4");
            string overridePath = System.IO.Path.Combine(Application.persistentDataPath, overrideName);
            m_videoPlayer.url = System.IO.File.Exists(overridePath)
                ? overridePath
                : System.IO.Path.Combine(Application.streamingAssetsPath, bundledName);
            m_resolvedVideoUrl = m_videoPlayer.url;
            m_videoPlayer.aspectRatio = UnityEngine.Video.VideoAspectRatio.Stretch;
            Debug.Log($"[VideoTestScene] Video source: {m_videoPlayer.url}" +
                      (m_flatClipMode ? " (flat clip mode)" : ""));

            if (m_flatClipMode)
            {
                // Flat clip: decode into a native-res RT (created once dimensions are
                // known), then reproject into the equirect RT every frame — everything
                // downstream (vignette shader, focus window, detection mapping) keeps
                // working in unchanged equirect space.
                var flatShader = Resources.Load<Shader>("FlatClipToEquirect");
                if (flatShader != null) m_flatCompositeMat = new Material(flatShader);
                else Debug.LogError("[VideoTestScene] FlatClipToEquirect shader not found in Resources.");
                m_videoPlayer.prepareCompleted += OnFlatClipPrepared;
                m_videoPlayer.Prepare();
                if (m_flatSurround360) SetupSurroundVideo();
            }
            else
            {
                m_videoPlayer.targetTexture = m_videoRT;
                m_videoPlayer.Play();
            }

            m_material.SetTexture(s_mainTexLId, m_videoRT);
        }

        // Second decoder: the standard 360 clip fills the periphery around the flat
        // clip, so the DR modes have real visual noise to suppress — an empty dark
        // surround gives the vignette nothing to do and kills immersion.
        private void SetupSurroundVideo()
        {
            string overridePath = System.IO.Path.Combine(Application.persistentDataPath, "study_video.mp4");
            string url = System.IO.File.Exists(overridePath)
                ? overridePath
                : System.IO.Path.Combine(Application.streamingAssetsPath, "DebugVideo.mp4");

            // Half-res equirect target — periphery sits at low visual acuity, and this
            // keeps the extra decode+blit cost modest on the XR2.
            m_surroundRT = new RenderTexture(2880, 1440, 0, RenderTextureFormat.ARGB32);
            m_surroundRT.Create();

            m_surroundPlayer                 = gameObject.AddComponent<VideoPlayer>();
            m_surroundPlayer.playOnAwake     = false;
            m_surroundPlayer.renderMode      = VideoRenderMode.RenderTexture;
            m_surroundPlayer.targetTexture   = m_surroundRT;
            m_surroundPlayer.isLooping       = true;
            m_surroundPlayer.skipOnDrop      = true;
            m_surroundPlayer.audioOutputMode = VideoAudioOutputMode.None; // no double audio
            m_surroundPlayer.url             = url;
            m_surroundPlayer.Play();
            Debug.Log($"[VideoTestScene] Peripheral surround video: {url}");
        }

        private void OnFlatClipPrepared(VideoPlayer vp)
        {
            vp.prepareCompleted -= OnFlatClipPrepared;
            m_flatRT = new RenderTexture((int)vp.width, (int)vp.height, 0, RenderTextureFormat.ARGB32);
            m_flatRT.Create();
            vp.targetTexture = m_flatRT;
            vp.Play();
            Debug.Log($"[VideoTestScene] Flat clip {vp.width}x{vp.height}, hfov {m_flatHFovDeg}°.");
        }

        // Draw the flat clip into the forward sector of the equirect RT (pinhole →
        // equirect reprojection, FlatClipToEquirect.shader). Only the sector's az/el
        // bounding box is rasterized each frame; the rest of the RT keeps the surround
        // colour from a one-time clear.
        private void CompositeFlatToEquirect()
        {
            if (m_flatRT == null || m_flatCompositeMat == null || m_videoRT == null) return;

            float azHalf = 0.5f * m_flatHFovDeg * Mathf.Deg2Rad;
            float elHalf = Mathf.Atan(Mathf.Tan(azHalf) * m_flatRT.height / m_flatRT.width);
            float elC    = m_flatElCenterDeg * Mathf.Deg2Rad;

            m_flatCompositeMat.SetTexture(s_flatMainTexId, m_flatRT);
            m_flatCompositeMat.SetVector(s_flatTanHalfId,
                new Vector4(Mathf.Tan(azHalf), Mathf.Tan(elHalf), 0f, 0f));
            m_flatCompositeMat.SetVector(s_flatAzElHalfId, new Vector4(azHalf, elHalf, 0f, 0f));
            m_flatCompositeMat.SetColor(s_flatSurroundId, m_flatSurroundColor);
            m_flatCompositeMat.SetFloat(s_flatFlipYId, m_flatFlipY ? 1f : 0f);

            // Periphery fill: live 360 video when available (real suppressible
            // noise), else a one-time flat-colour clear.
            bool surroundLive = m_flatSurround360 && m_surroundRT != null
                             && m_surroundPlayer != null && m_surroundPlayer.isPrepared;
            if (surroundLive) Graphics.Blit(m_surroundRT, m_videoRT);

            var prev = RenderTexture.active;
            RenderTexture.active = m_videoRT;
            if (!surroundLive && !m_flatCleared)
            {
                GL.Clear(false, true, m_flatSurroundColor);
                m_flatCleared = true;
            }

            // Sector bounding box in RT uv space — mirrors the vignette shader's
            // u = 0.5 + az/2π + uOffset, v = 0.5 + el/π + vOffset mapping, so the clip
            // lands exactly where the shader (and the converter's boxes) expect it.
            float u0 = 0.5f - azHalf / (2f * Mathf.PI) + m_videoUOffset;
            float u1 = 0.5f + azHalf / (2f * Mathf.PI) + m_videoUOffset;
            float v0 = 0.5f + (elC - elHalf) / Mathf.PI + m_videoVOffset;
            float v1 = 0.5f + (elC + elHalf) / Mathf.PI + m_videoVOffset;

            GL.PushMatrix();
            GL.LoadOrtho();
            m_flatCompositeMat.SetPass(0);
            GL.Begin(GL.QUADS);
            GL.TexCoord2(0f, 0f); GL.Vertex3(u0, v0, 0f);
            GL.TexCoord2(1f, 0f); GL.Vertex3(u1, v0, 0f);
            GL.TexCoord2(1f, 1f); GL.Vertex3(u1, v1, 0f);
            GL.TexCoord2(0f, 1f); GL.Vertex3(u0, v1, 0f);
            GL.End();
            GL.PopMatrix();
            RenderTexture.active = prev;
        }

        private void OnDestroy()
        {
            if (m_yoloRunner != null)
                m_yoloRunner.OnDetectionsReady -= OnDetectionsReady;
            if (m_selectionDots != null)
                foreach (var go in m_selectionDots) if (go != null) Destroy(go);
            if (m_dotMats != null)
                foreach (var mat in m_dotMats) if (mat != null) Destroy(mat);
            if (m_modeUIRoot != null) Destroy(m_modeUIRoot);
            if (m_modeUIMat  != null) Destroy(m_modeUIMat);
            if (m_videoPreviewRoot != null) Destroy(m_videoPreviewRoot);
            if (m_flatCompositeMat != null) Destroy(m_flatCompositeMat);
            if (m_flatRT     != null) { m_flatRT.Release();     Destroy(m_flatRT);     }
            if (m_surroundRT != null) { m_surroundRT.Release(); Destroy(m_surroundRT); }
            if (m_yoloRT  != null) { m_yoloRT.Release();  Destroy(m_yoloRT);  }
            if (m_videoRT != null) { m_videoRT.Release(); Destroy(m_videoRT); }
        }

        private void LateUpdate()
        {
            if (m_material == null) return;
            if (m_flatClipMode) CompositeFlatToEquirect();
            UpdateSpherePosition();
            UpdateCameraUniforms();
            UpdateFilterUniforms();
            UpdateBakedDetections();
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
            // Lock world rotation to identity — sphere may be parented to OVRCameraRig which
            // rotates with the head. Without this, the vertex world positions rotate and the
            // world-space az/el UV mapping rotates the video with the head.
            transform.rotation = Quaternion.identity;
            m_material.SetVector(s_sphereCenterId, head.position);
        }

        // In video mode: drive the vignette shader's camera uniforms from the head transform.
        // The equirectangular sampling path uses these for YOLO-style detection only; the actual
        // UV computation ignores them. Set a wide FOV so CamUV fallback is safe if ever toggled.
        private void UpdateCameraUniforms()
        {
            Transform head = Camera.main != null ? Camera.main.transform : transform;
            float tanX = Mathf.Tan(0.5f * 90f * Mathf.Deg2Rad); // 90° horizontal FOV placeholder
            float tanY = tanX * (9f / 16f);

            m_material.SetVector(s_camLFwdId,     head.forward);
            m_material.SetVector(s_camLRtId,      head.right);
            m_material.SetVector(s_camLUpId,      head.up);
            m_material.SetVector(s_tanHalfFovLId, new Vector4(tanX, tanY, 0f, 0f));
        }

        // ---- filter uniforms ----

        private void UpdateFilterUniforms()
        {
            // ColorPop/SignPop get a wider soft edge: a colour/grey boundary reads harsher
            // than a blur or dark boundary, so the transition needs to be more gradual.
            // Hard Dark goes the other way — a narrow edge so the black-out reads as an
            // almost-binary "selection vs everything else" rather than a wide buffer zone.
            float softEdgeDeg = IsPopMode(m_vignetteMode) ? m_popSoftEdgeDeg
                               : m_vignetteMode == VignetteMode.HardDark ? m_hardDarkSoftEdgeDeg
                               : m_softEdgeDeg;
            m_material.SetFloat(s_softEdgeId,       softEdgeDeg     * Mathf.Deg2Rad);
            if (m_frostTex != null) m_material.SetTexture(s_frostTexId, m_frostTex);
            m_material.SetFloat(s_popGreyDimId,      m_popGreyDim);
            m_material.SetFloat(s_popSatBoostId,     m_popSatBoost);
            m_material.SetFloat(s_popInsideDesatId,  m_popInsideDesat);
            m_material.SetFloat(s_popBrightInId,     m_popBrightInside);
            m_material.SetFloat(s_popBrightOutId,    m_popBrightOutside);
            m_material.SetFloat(s_popWarmSatMinId,   m_popWarmSatMin);
            m_material.SetFloat(s_popSigmoidId,      m_popSigmoid);
            m_material.SetFloat(s_popPeriphDimId,    m_popPeriphDim);
            m_material.SetFloat(s_popGlareKneeId,    m_popGlareKnee);
            m_material.SetFloat(s_popGlareInsideId,  m_popGlareInside);
            m_material.SetFloat(s_popGlareOutsideId, m_popGlareOutside);
            m_material.SetFloat(s_popGuardRadiusId,  m_popGuardRadius);
            m_material.SetFloat(s_eqUOffsetId,      m_videoUOffset);
            m_material.SetFloat(s_eqVOffsetId,      m_videoVOffset);
        }

        // ---- YOLO detection zones ----

        private void OnDetectionsReady(
            IReadOnlyList<(int classId, Vector4 box)> detections, Vector2Int inputSize)
        {
            int slot = 0;
            foreach (var (classId, box) in detections)
            {
                if (slot >= k_maxDetections) break;
                if (k_targetClasses != null && !k_targetClasses.Contains(classId)) continue;
                var rect = BoxToAzElRectEQ(box, inputSize);
                if (classId == k_personClassId && !PassesPersonGate(rect)) continue;
                m_detectionRects[slot]      = rect;
                m_detectionTimestamps[slot] = Time.time;
                m_detectionFade[slot]       = 1f; // live YOLO fallback has no persistent tracking to fade against yet
                slot++;
            }
        }

        // A detected person is shown only if close to the camera (apparent box height above
        // threshold — a monocular distance proxy). Given that, they're shown either because
        // they're ALREADY inside the focus window (position within it doesn't matter — dead
        // center counts the same as just inside the wall), OR because they're outside it but
        // near enough to the boundary to read as entering/exiting. Traffic-light/stop-sign
        // detections never call this — they're always shown, inside or outside the window,
        // same as this eventually is for people too. No active region = fail closed, since
        // "the region" is undefined without one.
        private bool PassesPersonGate(Vector4 azElRect)
        {
            if (m_effectiveRect == k_fullSphere) return false;

            float heightDeg = (azElRect.w - azElRect.z) * Mathf.Rad2Deg;
            if (heightDeg < m_personMinBoxHeightDeg) return false;

            float cx = (azElRect.x + azElRect.y) * 0.5f;
            float cy = (azElRect.z + azElRect.w) * 0.5f;
            float dxOutside = Mathf.Max(0f, m_effectiveRect.x - cx, cx - m_effectiveRect.y);
            float dyOutside = Mathf.Max(0f, m_effectiveRect.z - cy, cy - m_effectiveRect.w);
            if (dxOutside <= 0f && dyOutside <= 0f) return true; // inside the window: always shown

            // Outside the window: only near enough to the boundary to read as entering/exiting.
            // The acceptance widens with closeness (box height) — see m_personCloseWideningDegPerDeg.
            float edgeDistRad = Mathf.Sqrt(dxOutside * dxOutside + dyOutside * dyOutside);
            return edgeDistRad * Mathf.Rad2Deg <= m_personMaxEdgeDistDeg + PersonWidenDeg(heightDeg);
        }

        // Extra cone width earned by closeness: proportional to apparent size above the minimum,
        // so the highlight follows a pedestrian outward as the car approaches instead of dropping
        // them at their nearest (widest-angle) moment.
        private float PersonWidenDeg(float heightDeg) =>
            Mathf.Max(0f, heightDeg - m_personMinBoxHeightDeg) * m_personCloseWideningDegPerDeg;

        // Graded person-effect strength by position relative to the focus window: floor at
        // the window CENTRE ramping to full at the edge (relax only when the person is
        // actually in front of the user), full while entering/leaving near the edge, then
        // tapering to zero by m_personMaxEdgeDistDeg outside. Multiplied into the per-slot
        // fade so the whole effect (carve-out + highlight) breathes with position.
        private float PersonRegionScale(Vector4 azElRect) =>
            PersonRegionScaleFor(azElRect, m_effectiveRect);

        /// <summary>Predicted person-effect strength for a box against the CURRENT painted
        /// window (falls back to the frame's effective window). Public so BlobTargetController
        /// can select person targets the runtime will actually visibly highlight — call it
        /// after the window is locked (TestModeSequencer locks before activating blobs).</summary>
        public float PersonPredictedStrength(Vector4 azElRect) =>
            PersonRegionScaleFor(azElRect, m_activeRect != k_fullSphere ? m_activeRect : m_effectiveRect);

        private float PersonRegionScaleFor(Vector4 azElRect, Vector4 window)
        {
            if (window == k_fullSphere) return 1f;
            float cx = (azElRect.x + azElRect.y) * 0.5f;
            float cy = (azElRect.z + azElRect.w) * 0.5f;
            float dx = Mathf.Max(window.x - cx, cx - window.y);
            float dy = Mathf.Max(window.z - cy, cy - window.w);
            if (dx <= 0f && dy <= 0f)
            {
                // Inside: rect-normalized distance from window centre — 0 dead-centre,
                // 1 at the edge — so "in front" means literally central, independent of
                // how big the window is.
                float winCx = (window.x + window.y) * 0.5f;
                float winCy = (window.z + window.w) * 0.5f;
                float halfW = Mathf.Max((window.y - window.x) * 0.5f, 1e-4f);
                float halfH = Mathf.Max((window.w - window.z) * 0.5f, 1e-4f);
                float tEdge = Mathf.Max(Mathf.Abs(cx - winCx) / halfW,
                                        Mathf.Abs(cy - winCy) / halfH);
                return Mathf.Lerp(m_personInsideScale, 1f, Mathf.Clamp01(tEdge));
            }
            // Outside: full strength out to m_personFullStrengthDeg, then linear taper to
            // zero at the gate's own acceptance limit (m_personMaxEdgeDistDeg). Both bands
            // widen with closeness (box height) in lockstep with PassesPersonGate, so a close
            // pedestrian keeps full highlight at angles where a far one has faded out.
            float outDeg = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f)
                                    + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f)) * Mathf.Rad2Deg;
            float widen = PersonWidenDeg((azElRect.w - azElRect.z) * Mathf.Rad2Deg);
            float accept = m_personMaxEdgeDistDeg + widen;
            float taperStart = Mathf.Min(m_personFullStrengthDeg + widen, accept - 0.01f);
            return 1f - Mathf.Clamp01((outDeg - taperStart)
                                      / Mathf.Max(accept - taperStart, 0.01f));
        }

        // Drive detection slots from the baked track, keyed by video time.
        // Boxes are lerped between the two bracketing samples and held briefly after
        // vanishing (see m_trackedDets) so SignPop's pops don't blink at sample edges.
        private void UpdateBakedDetections()
        {
            if (m_bakedTrack == null || m_videoPlayer == null) return;
            float vt = (float)m_videoPlayer.time;
            if (vt < m_lastVideoVt - 0.5f) m_trackedDets.Clear(); // video looped / seeked back
            m_lastVideoVt = vt;

            int i0 = m_bakedTrack.LookupIndex(vt);
            if (i0 >= 0)
            {
                var s0 = m_bakedTrack.samples[i0];
                var s1 = i0 + 1 < m_bakedTrack.samples.Count ? m_bakedTrack.samples[i0 + 1] : null;
                float frac = s1 != null && s1.t > s0.t
                    ? Mathf.Clamp01((vt - s0.t) / (s1.t - s0.t)) : 0f;

                for (int j = 0; j < s0.d.Count; j++)
                {
                    var det = s0.d[j];
                    if (k_targetClasses != null && !k_targetClasses.Contains(det.c)) continue;
                    var box = new Vector4(det.x1, det.y1, det.x2, det.y2);
                    if (s1 != null)
                    {
                        var match = FindMatch(s1, det);
                        if (match != null)
                            box = Vector4.Lerp(box,
                                new Vector4(match.x1, match.y1, match.x2, match.y2), frac);
                    }
                    if (det.c == k_personClassId && !PassesPersonGate(BoxToAzElRectEQ(box, Vector2Int.one)))
                        continue;
                    // The entry's full baked lifetime, precomputed at load — lets the tracker
                    // decide visibility instantly instead of waiting out a real-time debounce.
                    Vector2 span = m_entryLifeSpan != null ? m_entryLifeSpan[i0][j] : new Vector2(0f, float.MaxValue);
                    UpsertTracked(box, det.c, vt, span);
                }
            }

            // Ramp presence toward 1 while still within the hold window (still "on screen" or
            // recently so), toward 0 once past it — a track is only actually removed after it
            // has fully faded out, so the window/highlight closes smoothly instead of popping.
            for (int i = 0; i < m_trackedDets.Count; i++)
            {
                var tr = m_trackedDets[i];
                // Hold bridges mid-lifetime bake gaps; once the KNOWN lifetime end passes,
                // the window stays open only for the explicit user-tunable tail
                // (m_detectionHoldPastEndSec, box frozen at its last position), then fades.
                bool withinLife = vt <= tr.lifeEnd + 0.001f;
                bool inEndTail  = !withinLife && vt <= tr.lifeEnd + m_detectionHoldPastEndSec + 0.001f;
                bool active = (withinLife && vt - tr.lastSeenVt <= m_signDetHoldSec) || inEndTail;
                // Blip filter + onset delay: visibility requires the object's FULL baked
                // lifetime (known offline) to reach m_detectionMinAgeSec — a real detection
                // shows m_detectionOnsetDelaySec after it arrives (0 = instantly), a blip
                // never shows at all. No waiting-out-a-debounce, no position lead.
                bool mature = tr.lifeEnd - tr.lifeStart >= m_detectionMinAgeSec
                              && vt - tr.lifeStart >= m_detectionOnsetDelaySec;
                // Size gate: a speck-sized box would be inflated to the shader's ~2° minimum
                // halo — a visible circle around nothing discernible — so stay silent until
                // the object is actually big enough to see. Boxes are normalized equirect:
                // az extent spans 360°, el extent 180°.
                float sizeDeg = Mathf.Max((tr.box.z - tr.box.x) * 360f, (tr.box.w - tr.box.y) * 180f);
                bool bigEnough = tr.cls == k_personClassId || sizeDeg >= m_detectionMinSizeDeg;
                bool visible = active && mature && bigEnough;
                float rate = visible ? 1f / Mathf.Max(m_detectionFadeInSeconds, 0.001f)
                                      : 1f / Mathf.Max(m_detectionFadeOutSeconds, 0.001f);
                tr.presence = Mathf.MoveTowards(tr.presence, visible ? 1f : 0f, rate * Time.deltaTime);
                m_trackedDets[i] = tr;
            }
            m_trackedDets.RemoveAll(tr => vt - tr.lastSeenVt > m_signDetHoldSec && tr.presence <= 0.001f);

            int slot = 0;
            foreach (var tr in m_trackedDets)
            {
                if (slot >= k_maxDetections) break;
                // Boxes are stored normalized — inputSize (1,1) reuses the same conversion.
                m_detectionRects[slot]      = BoxToAzElRectEQ(tr.box, Vector2Int.one);
                m_detectionTimestamps[slot] = Time.time;
                m_detectionFade[slot]       = tr.cls == k_personClassId
                    ? tr.presence * PersonRegionScale(m_detectionRects[slot])
                    : tr.presence;
                slot++;
            }
            // Expire unused tail slots so the count drops immediately instead of
            // ghosting stale rects for m_detectionLifetime.
            for (int i = slot; i < k_maxDetections; i++)
            {
                m_detectionTimestamps[i] = float.NegativeInfinity;
                m_detectionFade[i]       = 0f;
            }
        }

        // Nearest same-class detection in the next sample, within a radius scaled to the
        // box size — the identity match that makes bracket-lerping possible.
        private static BakedDetection FindMatch(BakedSample sample, BakedDetection det)
        {
            float cx = (det.x1 + det.x2) * 0.5f, cy = (det.y1 + det.y2) * 0.5f;
            float size = Mathf.Max(det.x2 - det.x1, det.y2 - det.y1);
            float best = Mathf.Max(size, 0.01f) * 1.5f;
            BakedDetection found = null;
            foreach (var cand in sample.d)
            {
                if (cand.c != det.c) continue;
                float dx = (cand.x1 + cand.x2) * 0.5f - cx;
                float dy = (cand.y1 + cand.y2) * 0.5f - cy;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist < best) { best = dist; found = cand; }
            }
            return found;
        }

        private void UpsertTracked(Vector4 box, int cls, float vt, Vector2 span)
        {
            float cx = (box.x + box.z) * 0.5f, cy = (box.y + box.w) * 0.5f;
            float size = Mathf.Max(box.z - box.x, box.w - box.y);
            float thresh = Mathf.Max(size, 0.01f) * 1.5f;
            for (int i = 0; i < m_trackedDets.Count; i++)
            {
                var tr = m_trackedDets[i];
                if (tr.cls != cls) continue;
                float dx = (tr.box.x + tr.box.z) * 0.5f - cx;
                float dy = (tr.box.y + tr.box.w) * 0.5f - cy;
                if (Mathf.Sqrt(dx * dx + dy * dy) < thresh)
                {
                    m_trackedDets[i] = new TrackedDet { box = box, cls = cls, lifeStart = span.x, lifeEnd = span.y, lastSeenVt = vt, presence = tr.presence };
                    return;
                }
            }
            m_trackedDets.Add(new TrackedDet { box = box, cls = cls, lifeStart = span.x, lifeEnd = span.y, lastSeenVt = vt, presence = 0f });
        }

        private void UpdateDetectionUniforms()
        {
            int count = 0;
            // StudyEffectSuppressed means "effect forced invisible" — the detection-highlight
            // boost below is part of that effect (it saturates/brightens detected objects
            // independently of _VignetteStrength), so it must be suppressed too, or a
            // baseline/no-filter condition would still visibly highlight lights and signs.
            if (!StudyEffectSuppressed)
            {
                for (int i = 0; i < k_maxDetections; i++)
                {
                    if (Time.time - m_detectionTimestamps[i] < m_detectionLifetime)
                        count = i + 1;
                }
            }
            m_material.SetInt(s_detectionCountId,          count);
            m_material.SetVectorArray(s_detectionRectsId,  m_detectionRects);
            m_material.SetFloatArray(s_detectionFadeId,    m_detectionFade);
            m_material.SetFloat(s_detectionSoftEdgeId,     m_detectionSoftEdgeDeg * Mathf.Deg2Rad);
            m_material.SetFloat(s_detectionEnhanceId,      m_detectionEnhance);
            m_material.SetFloat(s_detectionSurroundId, m_detectionSurround);
            m_material.SetFloat(s_detectionPulseAmpId, m_detectionPulseAmp);
            m_material.SetFloat(s_detectionOutsideScaleId, m_detectionOutsideFocusScale);
            m_material.SetFloat(s_detectionSatLiftId,    m_detectionSatLift);
            m_material.SetFloat(s_detectionBrightLiftId, m_detectionBrightLift);
            m_material.SetFloat(s_detectionContrastId,   m_detectionContrast);
        }

        // EQ box → az/el rect.
        // YOLO box: (x1,y1,x2,y2) in model pixel space.
        // Shader samples: u_eq = 0.5 + az/(2π) + uOffset  →  az = (u_tex - 0.5 - uOffset) * 2π
        //                 v_eq = 0.5 + el/π   + vOffset  →  el = (v_tex - 0.5 - vOffset) * π
        // UV offsets must be subtracted so boxes align with where the shader actually draws the video.
        // m_yoloFlipY: Sentis typically reads RenderTextures with y=0 at bottom (OpenGL), so
        // flip is needed when the YOLO model expects y=0 at top (standard image convention).
        private Vector4 BoxToAzElRectEQ(Vector4 box, Vector2Int inputSize)
        {
            float u1 = box.x / inputSize.x;
            float u2 = box.z / inputSize.x;

            float rawV1 = box.y / inputSize.y;   // top of box in YOLO space
            float rawV2 = box.w / inputSize.y;   // bottom of box in YOLO space
            float v1 = m_yoloFlipY ? 1f - rawV2 : rawV1;
            float v2 = m_yoloFlipY ? 1f - rawV1 : rawV2;

            float az1 = (u1 - 0.5f - m_videoUOffset) * 2f * Mathf.PI;
            float az2 = (u2 - 0.5f - m_videoUOffset) * 2f * Mathf.PI;
            float el1 = (v1 - 0.5f - m_videoVOffset) * Mathf.PI;
            float el2 = (v2 - 0.5f - m_videoVOffset) * Mathf.PI;

            return new Vector4(
                Mathf.Min(az1, az2), Mathf.Max(az1, az2),
                Mathf.Min(el1, el2), Mathf.Max(el1, el2));
        }

        // ---- motion disable ----

        // The video scene keeps only three motion profiles, so the modes added to the cycle
        // map onto the nearest one. TintedDark/ChromaticCool land on profiles whose defaults
        // already match their passthrough counterparts; Blur and Squeeze use ColorPop's 35°/0.7s
        // rather than passthrough's dedicated 30°/0.6s m_motionBlur. Moot by default anyway —
        // m_enableMotion is false here, so UpdateMotionDisable returns before reading this.
        private MotionSettings CurrentMotionSettings => m_vignetteMode switch
        {
            VignetteMode.ColorPop            => m_motionColorPop,
            VignetteMode.SignPop             => m_motionColorPop,
            VignetteMode.ChromaticCool       => m_motionColorPop,
            VignetteMode.Blur                => m_motionColorPop,
            VignetteMode.ConspicuitySqueeze  => m_motionColorPop,
            VignetteMode.SoftDark            => m_motionSoftDark,
            VignetteMode.GranulatedPeriphery => m_motionSoftDark,
            VignetteMode.SpotLift            => m_motionSoftDark,
            _                                => m_motionHardDark
        };

        private const float k_focusArrivalMarginRad = 0.2618f;

        private void UpdateMotionDisable()
        {
	    if (!m_enableMotion)
		return;
            Transform head = Camera.main != null ? Camera.main.transform : transform;
            float angularSpeed = Quaternion.Angle(m_lastHeadRot, head.rotation)
                                 / Mathf.Max(Time.deltaTime, 0.001f);
            m_lastHeadRot = head.rotation;

            bool headInFocus = false;
            if (m_activeRect != k_fullSphere)
            {
                float headAz = Mathf.Atan2(head.forward.x, head.forward.z);
                float headEl = Mathf.Asin(Mathf.Clamp(head.forward.y, -1f, 1f));
                headInFocus  = headAz >= m_activeRect.x - k_focusArrivalMarginRad
                            && headAz <= m_activeRect.y + k_focusArrivalMarginRad
                            && headEl >= m_activeRect.z - k_focusArrivalMarginRad
                            && headEl <= m_activeRect.w + k_focusArrivalMarginRad;
            }

            var s = CurrentMotionSettings;
            if (headInFocus)          m_motionDisableTimer = 0f;
            else if (angularSpeed > s.speedThreshDeg) m_motionDisableTimer = s.holdSeconds;
            else if (m_motionDisableTimer > 0f)        m_motionDisableTimer -= Time.deltaTime;

            float target    = m_motionDisableTimer > 0f ? 1f : 0f;
            float fadeSpeed = target > m_motionSuppression
                ? 1f / Mathf.Max(m_motionFadeOutSec, 0.001f)
                : 1f / Mathf.Max(m_motionFadeInSec,  0.001f);
            m_motionSuppression = Mathf.MoveTowards(m_motionSuppression, target, fadeSpeed * Time.deltaTime);
        }

        // ---- study API (IStudyVignetteControl — driven by Study/ConditionSequencer) ----

        public VignetteMode CurrentMode => m_vignetteMode;
        public Vector4 ActiveRect => m_activeRect;
        public float PersonMinBoxHeightDeg => m_personMinBoxHeightDeg;
        public float DefaultWindowHalfWidthDeg => m_defaultWindowHalfWidthDeg;
        public float CurrentEffectiveStrength { get; private set; }
        public bool StudyInputLock { get; set; }
        public bool StudyEffectSuppressed { get; set; }
        public bool MotionEnabled { get => m_enableMotion; set => m_enableMotion = value; }

        // ---- blob-probe support (BlobTargetController, Route B) ----

        /// <summary>Static blob-probe shape/amount — set once when the blob task activates. The
        /// probe is a scene-pixel modulation composited in the vignette shader (soft desaturation
        /// + gentle dim), so a probe in a defocused area is filtered too (supervisor Point 3);
        /// for the style-3 ring that behaviour is gated by ringBehindFilter.</summary>
        public void SetBlobProbeStatics(int style, float sigmaFrac, float desat, float dim,
                                        float rim, float lens, Color ringColor, float ringWidth,
                                        Color flashColor, int ringSegments = 0, float ringOutline = 0f,
                                        bool ringBehindFilter = true)
        {
            if (m_material == null) return;
            m_material.SetFloat(s_blobStyleId, style);
            m_material.SetFloat(s_blobSigmaId, sigmaFrac);
            m_material.SetFloat(s_blobDesatId, desat);
            m_material.SetFloat(s_blobDimId,   dim);
            m_material.SetFloat(s_blobRimId,   rim);
            m_material.SetFloat(s_blobLensId,  lens);
            m_material.SetColor(s_blobRingColorId, ringColor);
            m_material.SetFloat(s_blobRingWidthId, ringWidth);
            m_material.SetColor(s_blobFlashColorId, flashColor);
            m_material.SetFloat(s_blobRingSegmentsId, ringSegments);
            m_material.SetFloat(s_blobRingOutlineId, ringOutline);
            m_material.SetFloat(s_blobRingBehindFilterId, ringBehindFilter ? 1f : 0f);
        }

        /// <summary>Single blob probe (slot 0), azEl in radians; radiusRad = angular radius;
        /// strength 0..1 (onset ramp); flash 0..1 (hit cue). active=false clears all probes.</summary>
        public void SetBlobProbe(bool active, Vector2 azEl, float radiusRad, float strength, float flash)
        {
            if (m_material == null) return;
            if (!active || strength <= 0f) { m_material.SetInt(s_blobCountId, 0); return; }
            m_blobData[0]  = new Vector4(azEl.x, azEl.y, radiusRad, strength);
            m_blobFlash[0] = new Vector4(flash, 0f, 0f, 0f);
            m_material.SetVectorArray(s_blobDataId, m_blobData);
            m_material.SetVectorArray(s_blobFlash4Id, m_blobFlash);
            m_material.SetInt(s_blobCountId, 1);
        }

        /// <summary>Up to k_maxBlobs SIMULTANEOUS probes. data[i]=(az,el,radiusRad,strength);
        /// flash[i].x = hit-flash mix. Extra slots ignored; count clamps to k_maxBlobs.</summary>
        public void SetBlobProbes(int count, Vector4[] data, Vector4[] flash)
        {
            if (m_material == null) return;
            int n = Mathf.Clamp(count, 0, k_maxBlobs);
            for (int i = 0; i < n; i++) { m_blobData[i] = data[i]; m_blobFlash[i] = flash[i]; }
            if (n > 0)
            {
                m_material.SetVectorArray(s_blobDataId, m_blobData);
                m_material.SetVectorArray(s_blobFlash4Id, m_blobFlash);
            }
            m_material.SetInt(s_blobCountId, n);
        }

        // ---- click-capture support (ClickProbeTest) ----

        /// <summary>Current video playhead time in seconds — the key that aligns clicks with
        /// the per-frame YOLO bake (DebugVideo.detections.json). -1 if no player.</summary>
        public float VideoTime => m_videoPlayer != null ? (float)m_videoPlayer.time : -1f;
        public float VideoUOffset => m_videoUOffset;
        public float VideoVOffset => m_videoVOffset;
        public bool  VideoFlipY   => m_flipVideoY;

        /// <summary>World-direction aim of the LEFT controller as azimuth/elevation (radians).</summary>
        public void GetLeftControllerAzEl(out float az, out float el)
        {
            var rot = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch);
            Vector3 worldDir = m_cameraRig != null
                ? m_cameraRig.TransformDirection(rot * Vector3.forward)
                : rot * Vector3.forward;
            worldDir = worldDir.normalized;
            az = Mathf.Atan2(worldDir.x, worldDir.z);
            el = Mathf.Asin(Mathf.Clamp(worldDir.y, -1f, 1f));
        }

        public void StudySetMode(VignetteMode mode)
        {
            if (Array.IndexOf(k_modeCycle, mode) < 0) mode = VignetteMode.ColorPop;
            m_vignetteMode       = mode;
            m_motionDisableTimer = 0f;
            m_motionSuppression  = 0f;
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

        /// <summary>Play/pause the video (TestModeSequencer's X toggle). No-op if flat-clip
        /// surround player is active — only the primary player is under test-mode control.</summary>
        public void SetVideoPlaying(bool playing)
        {
            if (m_videoPlayer == null) return;
            if (playing) m_videoPlayer.Play();
            else m_videoPlayer.Pause();
        }

        // ---- point-authoring support (PointAuthoringTool) ----

        /// <summary>Clip length in seconds (0 if no player / not prepared).</summary>
        public double VideoLength => m_videoPlayer != null ? m_videoPlayer.length : 0.0;

        /// <summary>Loop the primary video. Authoring turns this off so the clip ends (offering
        /// replay/finish); the normal test harness leaves it on (blob targets reset per loop).</summary>
        public bool VideoLooping
        {
            get => m_videoPlayer != null && m_videoPlayer.isLooping;
            set { if (m_videoPlayer != null) m_videoPlayer.isLooping = value; }
        }

        /// <summary>Seek to the start and play — used by authoring "replay".</summary>
        public void RestartVideo()
        {
            if (m_videoPlayer == null) return;
            m_videoPlayer.Stop(); // resets playback time to 0 — a stopped/ended player ignores a bare time=0
            m_videoPlayer.Play();
        }

        /// <summary>Right-controller aim as az/el (radians) — mirror of GetLeftControllerAzEl.</summary>
        public void GetRightControllerAzEl(out float az, out float el)
        {
            var rot = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);
            Vector3 worldDir = m_cameraRig != null
                ? m_cameraRig.TransformDirection(rot * Vector3.forward)
                : rot * Vector3.forward;
            worldDir = worldDir.normalized;
            az = Mathf.Atan2(worldDir.x, worldDir.z);
            el = Mathf.Asin(Mathf.Clamp(worldDir.y, -1f, 1f));
        }

        public void StudySetWindow(Vector4 azElRadians)
        {
            m_activeRect = azElRadians;
            // Null-guard: TestModeSequencer calls this from OnSceneLoaded, which fires
            // BEFORE this manager's Start() creates m_material — Start() pushes
            // m_activeRect to the material itself, so skipping the SetVector here is safe.
            if (m_material != null) m_material.SetVector(s_focusRectId, m_activeRect);
            m_isPainting = false;
            CancelDotHide();
            HideCornerDotsImmediate();
        }

        public void StudyClearWindow()
        {
            m_activeRect = k_fullSphere;
            if (m_material != null) m_material.SetVector(s_focusRectId, m_activeRect);
            m_isPainting = false;
            CancelDotHide();
            HideCornerDotsImmediate();
        }

        // ---- selection ----

        private bool m_isPainting;
        private float BrushPadRad => m_defaultWindowHalfWidthDeg * Mathf.Deg2Rad;

        // Pop family: ColorPop + its detection-gated variant SignPop.
        private static bool IsPopMode(VignetteMode mode) =>
            mode == VignetteMode.ColorPop || mode == VignetteMode.SignPop;

        // "Camera" modes render the source texture (instant, no formation animation).
        // Same set as CameraSphereVignetteManager.IsCameraMode — the dark-overlay family
        // (SoftDark/HardDark/TintedDark/OutlinedDark/Grain) is what animates in.
        private static bool IsCameraMode(VignetteMode mode) =>
            IsPopMode(mode) || mode == VignetteMode.Blur || mode == VignetteMode.ChromaticCool
            || mode == VignetteMode.ConspicuitySqueeze || mode == VignetteMode.SpotLift;

        private void HandleSelection()
        {
            // Study lock: the trigger belongs to the task (probe presses) and A/B must not
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
            bool aPressed     = OVRInput.GetDown(OVRInput.RawButton.A);
            bool bPressed     = OVRInput.GetDown(OVRInput.RawButton.B);

            if (aPressed)
            {
                m_vignetteMode = k_modeCycle[(Array.IndexOf(k_modeCycle, m_vignetteMode) + 1) % k_modeCycle.Length];
                m_motionDisableTimer = 0f;
                m_motionSuppression  = 0f;
                bool hasRect = m_activeRect != k_fullSphere;
                if (IsCameraMode(m_vignetteMode))
                {
                    StopFormCoroutine();
                    m_vignetteStrength = 1f;
                }
                else
                {
                    StopFormCoroutine();
                    m_vignetteStrength = 0f;
                    if (hasRect) m_formCoroutine = StartCoroutine(FormVignette());
                }
                ShowModeToast();
            }

            if (bPressed)
            {
                m_activeRect = k_fullSphere;
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
                CancelDotHide();
                m_cornerDotsHidden = false;
                RestoreDotsScale();
                if (!IsCameraMode(m_vignetteMode)) { StopFormCoroutine(); m_vignetteStrength = 0f; }
                m_activeRect = new Vector4(az - BrushPadRad, az + BrushPadRad,
                                          el - BrushPadRad, el + BrushPadRad);
                m_material.SetVector(s_focusRectId, m_activeRect);
                m_isPainting = true;
            }
            else if (held && m_isPainting)
            {
                m_activeRect = new Vector4(
                    Mathf.Min(m_activeRect.x, az - BrushPadRad), Mathf.Max(m_activeRect.y, az + BrushPadRad),
                    Mathf.Min(m_activeRect.z, el - BrushPadRad), Mathf.Max(m_activeRect.w, el + BrushPadRad));
                m_material.SetVector(s_focusRectId, m_activeRect);
            }

            if (justReleased && m_isPainting && m_activeRect != k_fullSphere)
            {
                if (IsCameraMode(m_vignetteMode)) m_vignetteStrength = 1f;
                else { StopFormCoroutine(); m_formCoroutine = StartCoroutine(FormVignette()); }
                m_dotHideCoroutine = StartCoroutine(HideDotsCoro());
            }

            if (!held) m_isPainting = false;
            DrawPointerAndBorder(az, el, held);
        }

        // ---- formation ----

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

        // ---- mode uniforms ----

        private void UpdateModeUniforms()
        {
            bool isPop = IsPopMode(m_vignetteMode);
            // Exactly one branch toggle may be set — the shader tests them in this order
            // (Simple > Tint > ColorPop > Squeeze > Grain > Outline > SpotLift > else Blur),
            // so Blur is the all-toggles-off fallback.
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
            // Grain/Outline/SpotLift scale their own peripheral alpha (_GrainDensity,
            // _OutlineDimAlpha, _SpotDimAlpha), so the shared cap must not also dim them.
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
            m_material.SetFloat(s_chromaticCoolId,    isCoolBlur ? 1f : 0f);
            m_material.SetFloat(s_squeezeModeId,      isSqueeze  ? 1f : 0f);
            m_material.SetFloat(s_grainModeId,        isGrain    ? 1f : 0f);
            m_material.SetFloat(s_outlineModeId,      isOutline  ? 1f : 0f);
            m_material.SetFloat(s_spotLiftModeId,     isSpotLift ? 1f : 0f);
            m_material.SetFloat(s_colorPopModeId,     isPop ? 1f : 0f);
            m_material.SetFloat(s_popDetGateId,       m_vignetteMode == VignetteMode.SignPop ? 1f : 0f);
            m_material.SetFloat(s_popDetFallbackId,   m_signRogFallback);
            m_material.SetFloat(s_vignetteStrengthId, effectiveStrength);
            m_material.SetFloat(s_maxVignetteAlphaId, maxAlpha);
            // Hard Dark: once a selection is locked in, that exact painted rect is the only
            // thing that's ever clear — suppress the generic per-detection carve-out/highlight
            // entirely so a detected light/person elsewhere can't poke a hole in the black-out.
            m_material.SetFloat(s_suppressDetectionWindowsId, m_vignetteMode == VignetteMode.HardDark ? 1f : 0f);

            // ColorPop/SignPop: when no selection is painted, auto-follow head gaze so the
            // effect is always visible without needing to hold trigger first. The gaze rect
            // also becomes the effective window for the person gate/grading — previously it
            // only reached the shader, so with nothing painted the person gate failed closed
            // and persons were never highlighted at all in free-play/TestModeSequencer runs.
            m_effectiveRect = m_activeRect;
            if (isPop && m_activeRect == k_fullSphere)
            {
                Transform head = Camera.main != null ? Camera.main.transform : transform;
                float headAz   = Mathf.Atan2(head.forward.x, head.forward.z);
                float headEl   = Mathf.Asin(Mathf.Clamp(head.forward.y, -1f, 1f));
                float halfW    = 40f * Mathf.Deg2Rad;
                float halfH    = 25f * Mathf.Deg2Rad;
                m_effectiveRect = new Vector4(headAz - halfW, headAz + halfW,
                                              headEl - halfH, headEl + halfH);
                m_material.SetVector(s_focusRectId, m_effectiveRect);
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

        // ---- selection dots ----

        private void InitSelectionDots()
        {
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
            float elapsed = 0f, dur = 0.3f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float s  = Mathf.Lerp(m_dotSize, 0f, Mathf.SmoothStep(0f, 1f, elapsed / dur));
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

        private void DrawPointerAndBorder(float az, float el, bool holding)
        {
            if (m_selectionDots == null) return;
            Transform head = Camera.main != null ? Camera.main.transform : transform;
            Vector3 org = head.position;
            m_selectionDots[4].transform.position = org + DirFromAzEl(az, el) * k_dotDistance;
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

        // ---- mode UI ----

        private void InitModeUI()
        {
            m_modeUIRoot = new GameObject("ModeIndicatorUI");
            var canvas = m_modeUIRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            m_modeUIRoot.AddComponent<CanvasScaler>();
            var rt = m_modeUIRoot.GetComponent<RectTransform>();
            rt.sizeDelta  = new Vector2(540, 150);
            rt.localScale = Vector3.one * 0.001f;
            m_modeUIGroup = m_modeUIRoot.AddComponent<CanvasGroup>();
            m_modeUIGroup.alpha = 0f; m_modeUIGroup.blocksRaycasts = false; m_modeUIGroup.interactable = false;

            // The vignette sphere renders on the Transparent queue (3000) with its bounds
            // centered on the head, so it sorts closer than the toast and draws over it —
            // default UI is also queue 3000. Queue 4100 puts the toast above the sphere
            // and the selection dots (4000). GetDefaultCanvasMaterial survives build stripping.
            m_modeUIMat = new Material(Canvas.GetDefaultCanvasMaterial()) { renderQueue = 4100 };

            var bg = CreateChild(m_modeUIRoot, "BG");
            var bgImg = bg.AddComponent<Image>();
            bgImg.color    = new Color(0.05f, 0.05f, 0.05f, 0.82f);
            bgImg.material = m_modeUIMat;
            StretchFill(bg);

            var stripe = CreateChild(m_modeUIRoot, "Stripe");
            var stripeImg = stripe.AddComponent<Image>();
            stripeImg.color    = ModeAccentColor();
            stripeImg.material = m_modeUIMat;
            var srt = stripe.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0.88f); srt.anchorMax = Vector2.one;
            srt.offsetMin = srt.offsetMax = Vector2.zero;

            var nameGO = CreateChild(m_modeUIRoot, "ModeName");
            m_modeNameText = nameGO.AddComponent<Text>();
            m_modeNameText.material = m_modeUIMat;
            m_modeNameText.font = BuiltinFont(); m_modeNameText.fontSize = 46;
            m_modeNameText.fontStyle = FontStyle.Bold; m_modeNameText.alignment = TextAnchor.MiddleCenter;
            m_modeNameText.color = Color.white;
            var nrt = nameGO.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0f, 0.38f); nrt.anchorMax = new Vector2(1f, 0.88f);
            nrt.offsetMin = nrt.offsetMax = Vector2.zero;

            var hintGO = CreateChild(m_modeUIRoot, "Hint");
            m_modeHintText = hintGO.AddComponent<Text>();
            m_modeHintText.material = m_modeUIMat;
            m_modeHintText.font = BuiltinFont(); m_modeHintText.fontSize = 21;
            m_modeHintText.alignment = TextAnchor.MiddleCenter;
            m_modeHintText.color = new Color(1f, 1f, 1f, 0.6f);
            var hrt = hintGO.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 0f); hrt.anchorMax = new Vector2(1f, 0.4f);
            hrt.offsetMin = hrt.offsetMax = Vector2.zero;

            m_modeUIRoot.transform.position = Vector3.zero;
        }

        private void ShowModeToast()
        {
            if (m_modeNameText == null) return;
            int idx = Array.IndexOf(k_modeCycle, m_vignetteMode) + 1;
            string modeName = m_vignetteMode switch
            {
                VignetteMode.Blur                => "BLUR",
                VignetteMode.SoftDark            => "SOFT DARK",
                VignetteMode.TintedDark          => "TINTED DARK",
                VignetteMode.ChromaticCool       => "CHROMA COOL",
                VignetteMode.ColorPop            => "COLOR POP",
                VignetteMode.ConspicuitySqueeze  => "FLATTEN",
                VignetteMode.GranulatedPeriphery => "GRAIN",
                VignetteMode.OutlinedDark        => "OUTLINE DARK",
                VignetteMode.SpotLift            => "SPOTLIGHT",
                VignetteMode.SignPop             => "SIGN POP",
                _                                => "HARD DARK"
            };
            string desc = m_vignetteMode switch
            {
                VignetteMode.Blur                => "Blurred + desaturated periphery",
                VignetteMode.SoftDark            => $"Gradual dark vignette  ({(int)(m_mode2MaxAlpha * 100)}% max)",
                VignetteMode.TintedDark          => "Coloured dark vignette",
                VignetteMode.ChromaticCool       => "Warm focus / cool periphery, colour kept",
                VignetteMode.ColorPop            => "Red/orange/green pop in window, kept outside; glare dimmed",
                VignetteMode.ConspicuitySqueeze  => "Contrast balanced, gently desaturated + dimmed",
                VignetteMode.GranulatedPeriphery => "Static noise grains in periphery",
                VignetteMode.OutlinedDark        => "Blackout with edge outlines kept",
                VignetteMode.SpotLift            => "Brightened focus, soft dim periphery",
                VignetteMode.SignPop             => m_bakedTrack != null
                    ? "Only DETECTED lights & signs pop; other colours muted"
                    : "NO DETECTION TRACK — all ROG at fallback dim",
                _                                => "Full black-out vignette"
            };
            m_modeNameText.text = $"[{idx}/{k_modeCycle.Length}]  {modeName}  [VIDEO]";
            m_modeHintText.text = $"{desc}     [A] cycle  [B] clear";
            var stripe = m_modeUIRoot.transform.Find("Stripe");
            if (stripe != null) { var img = stripe.GetComponent<Image>(); if (img != null) img.color = ModeAccentColor(); }
            m_modeUITimer = k_modeUIShowTime;
        }

        // Same accents as CameraSphereVignetteManager.ModeAccentColor so a mode reads the
        // same in both scenes.
        private Color ModeAccentColor() => m_vignetteMode switch
        {
            VignetteMode.Blur                => new Color(0.25f, 0.55f, 1.00f, 1f), // blue
            VignetteMode.SoftDark            => new Color(1.00f, 0.65f, 0.10f, 1f), // amber
            VignetteMode.TintedDark          => new Color(0.55f, 0.35f, 1.00f, 1f), // violet
            VignetteMode.ChromaticCool       => new Color(0.45f, 0.90f, 0.95f, 1f), // cyan
            VignetteMode.ColorPop            => new Color(1.00f, 0.80f, 0.10f, 1f), // warm yellow
            VignetteMode.ConspicuitySqueeze  => new Color(0.60f, 0.60f, 0.65f, 1f), // neutral grey
            VignetteMode.GranulatedPeriphery => new Color(0.75f, 0.75f, 0.55f, 1f), // sand
            VignetteMode.OutlinedDark        => new Color(0.95f, 0.95f, 0.95f, 1f), // white
            VignetteMode.SpotLift            => new Color(1.00f, 0.95f, 0.55f, 1f), // pale gold
            VignetteMode.SignPop             => new Color(0.20f, 0.85f, 0.35f, 1f), // green
            _                                => new Color(0.90f, 0.15f, 0.15f, 1f)  // red (hard dark)
        };

        private void UpdateModeUI()
        {
            if (m_modeUIRoot == null) return;
            Transform head = Camera.main != null ? Camera.main.transform : transform;
            Vector3 target = head.position + head.forward * 1.5f + Vector3.down * 0.30f;
            m_modeUIRoot.transform.position = Vector3.Lerp(m_modeUIRoot.transform.position, target, Time.deltaTime * 9f);
            Vector3 away = m_modeUIRoot.transform.position - head.position;
            if (away.sqrMagnitude > 0.001f)
                m_modeUIRoot.transform.rotation = Quaternion.LookRotation(away, Vector3.up);
            if (m_modeUITimer > 0f)
            {
                m_modeUITimer -= Time.deltaTime;
                float fadeIn  = Mathf.Clamp01((k_modeUIShowTime - m_modeUITimer) / k_modeUIFadeDur);
                float fadeOut = Mathf.Clamp01(m_modeUITimer / k_modeUIFadeDur);
                m_modeUIGroup.alpha = Mathf.Min(fadeIn, fadeOut);
            }
            else m_modeUIGroup.alpha = 0f;
        }

        // ---- static helpers ----

        private static Vector3 DirFromAzEl(float az, float el)
        {
            float cosEl = Mathf.Cos(el);
            return new Vector3(Mathf.Sin(az) * cosEl, Mathf.Sin(el), Mathf.Cos(az) * cosEl);
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
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static Font BuiltinFont()
        {
            var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return f;
        }
    }
}
