// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.ShaderSample
{
    /// <summary>
    /// Drives the CameraSphereVignette shader.
    ///
    /// World-space sphere centred on the user's head — the sphere follows head
    /// position every frame so the user stays at the centre, but the sphere does
    /// NOT rotate with the head.  A rectangular focus window is fixed in world
    /// space (azimuth / elevation).  Inside the window the overlay is transparent
    /// (full OS passthrough quality).  Outside it the camera feed is blurred and
    /// desaturated.  Turning your head reveals more of the filtered periphery.
    ///
    /// SELECTION (right controller, index trigger):
    ///   Press once → marks corner A.
    ///   Press again → marks corner B, confirms the rectangle.
    ///   Old rectangle stays active while selecting a new one.
    ///
    /// Before the first selection the default rect covers the full sphere, so
    /// the overlay is fully transparent and the user sees unmodified passthrough.
    /// </summary>
    [MetaCodeSample("PassthroughCameraApiSamples-ShaderSample")]
    public class CameraSphereVignetteManager : MonoBehaviour
    {
        [SerializeField] private PassthroughCameraAccess m_cameraAccess;
        // Named m_renderer to preserve scene serialization from the prior version.
        [SerializeField] private MeshRenderer            m_renderer;

        [Tooltip("OVRCameraRig's right controller anchor (for aim direction).")]
        [SerializeField] private Transform m_rightControllerAnchor;

        [Tooltip("Optional: OVRCameraRig root. Used to transform controller " +
                 "rotation into world space when the rig is not at the origin.")]
        [SerializeField] private Transform m_cameraRig;

        [SerializeField] private Text m_debugText;

        [Header("Filter")]
        [SerializeField, Range(1f, 45f)]   private float m_softEdgeDeg   = 20f;
        [SerializeField, Range(0f, 0.15f)] private float m_maxBlurRadius = 0.06f;
        [SerializeField, Range(0.5f, 3f)]  private float m_blurCurveExp  = 1.2f;
        [SerializeField, Range(0f, 0.9f)]  private float m_desatDelay    = 0.1f;
        [SerializeField, Range(0.5f, 4f)]  private float m_desatCurveExp = 2.0f;

        [Header("Selection Preview")]
        [SerializeField] private LineRenderer m_selectionLine;
        [SerializeField] private Color        m_lineColorHeld   = Color.white;
        [SerializeField] private Color        m_lineColorLocked = new Color(1f, 1f, 1f, 0.4f);

        [Header("YOLO Detection")]
        [Tooltip("Optional — wire up to enable automatic clear zones for detected objects.")]
        [SerializeField] private YoloRunner m_yoloRunner;
        [Tooltip("How long a detection zone stays visible after the object was last detected (seconds).")]
        [SerializeField] private float m_detectionLifetime = 0.6f;

        // Fallback FOV when camera intrinsics are unavailable.
        [SerializeField] private float m_cameraHorizontalFovDeg = 82f;

        // ---- shader property IDs ----
        private static readonly int s_mainTexId        = Shader.PropertyToID("_MainTex");
        private static readonly int s_sphereCenterId   = Shader.PropertyToID("_SphereCenter");
        private static readonly int s_headRightId      = Shader.PropertyToID("_HeadRight");
        private static readonly int s_headUpId         = Shader.PropertyToID("_HeadUp");
        private static readonly int s_headForwardId    = Shader.PropertyToID("_HeadForward");
        private static readonly int s_tanHalfFovId     = Shader.PropertyToID("_TanHalfFov");
        private static readonly int s_focusRectId      = Shader.PropertyToID("_FocusRect");
        private static readonly int s_softEdgeId       = Shader.PropertyToID("_SoftEdge");
        private static readonly int s_maxBlurRadId     = Shader.PropertyToID("_MaxBlurRadius");
        private static readonly int s_blurCurveExpId   = Shader.PropertyToID("_BlurCurveExp");
        private static readonly int s_desatDelayId     = Shader.PropertyToID("_DesatDelay");
        private static readonly int s_desatCurveExpId  = Shader.PropertyToID("_DesatCurveExp");
        private static readonly int s_detectionCountId = Shader.PropertyToID("_DetectionCount");
        private static readonly int s_detectionRectsId = Shader.PropertyToID("_DetectionRects");

        // Full-sphere default → no filter before first selection.
        private static readonly Vector4 k_fullSphere =
            new(-Mathf.PI, Mathf.PI, -Mathf.PI * 0.5f, Mathf.PI * 0.5f);

        // YOLO classes to treat as "important" (restore clarity when detected).
        private static readonly HashSet<int> k_targetClasses = new() { 0, 1, 2, 3, 5, 6, 9, 11 };
        // person(0), bicycle(1), car(2), motorbike(3), bus(5), truck(6), traffic light(9), stop sign(11)

        private const int k_maxDetections = 8;
        private readonly Vector4[] m_detectionRects     = new Vector4[k_maxDetections];
        private readonly float[]   m_detectionTimestamp = new float[k_maxDetections];
        private int                m_activeDetectionCount;

        private Material m_material;
        private bool     m_loggedIntrinsics;

        // Cached head pose for coordinate conversion.
        private Vector3 m_headRight, m_headUp, m_headForward;
        private Vector2 m_tanHalfFov;

        // Active (confirmed) focus rectangle.
        private Vector4 m_activeRect = k_fullSphere;

        // ---- Unity lifecycle ----

        private IEnumerator Start()
        {
            m_material = m_renderer.material;
            m_material.SetVector(s_focusRectId, m_activeRect);

            // Quest passthrough only shows through when the camera clears to alpha=0.
            foreach (var cam in Camera.allCameras)
            {
                cam.clearFlags       = CameraClearFlags.SolidColor;
                cam.backgroundColor  = Color.clear;
            }

            // Explicitly activate the OVRPassthroughLayer so PT shows in logcat.
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

            m_material.SetTexture(s_mainTexId, m_cameraAccess.GetTexture());
            SetDebug("Hold trigger and sweep to paint a focus zone.");

            if (m_yoloRunner != null)
                m_yoloRunner.OnDetectionsReady += OnDetectionsReady;
        }

        private void OnDestroy()
        {
            if (m_yoloRunner != null)
                m_yoloRunner.OnDetectionsReady -= OnDetectionsReady;
        }

        private void LateUpdate()
        {
            if (m_material == null) return;

            UpdateHeadUniforms();
            UpdateFilterUniforms();
            UpdateDetectionUniforms();
            HandleSelection();
        }

        // ---- head uniforms (updated every frame) ----

        private void UpdateHeadUniforms()
        {
            Transform head = Camera.main != null ? Camera.main.transform : transform;
            transform.position = head.position;

            m_headRight   = head.right;
            m_headUp      = head.up;
            m_headForward = head.forward;

            m_material.SetVector(s_sphereCenterId, head.position);
            m_material.SetVector(s_headRightId,    m_headRight);
            m_material.SetVector(s_headUpId,       m_headUp);
            m_material.SetVector(s_headForwardId,  m_headForward);
            FeedFovUniform();
        }

        private void FeedFovUniform()
        {
            float tanX, tanY;
            var intr = (m_cameraAccess != null && m_cameraAccess.IsPlaying)
                ? m_cameraAccess.Intrinsics : default;

            if (intr.FocalLength.x > 0f && intr.SensorResolution.x > 0)
            {
                tanX = intr.SensorResolution.x / (2f * intr.FocalLength.x);
                var cur = m_cameraAccess.CurrentResolution;
                tanY = (cur.x > 0 && cur.y > 0)
                    ? tanX * ((float)cur.y / cur.x)
                    : intr.SensorResolution.y / (2f * intr.FocalLength.y);

                if (!m_loggedIntrinsics)
                {
                    m_loggedIntrinsics = true;
                    Debug.Log($"[CameraVignette] focal={intr.FocalLength} sensor={intr.SensorResolution}");
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

            m_tanHalfFov = new Vector2(tanX, tanY);
            m_material.SetVector(s_tanHalfFovId, new Vector4(tanX, tanY, 0f, 0f));
        }

        private void UpdateFilterUniforms()
        {
            m_material.SetFloat(s_softEdgeId,     m_softEdgeDeg   * Mathf.Deg2Rad);
            m_material.SetFloat(s_maxBlurRadId,   m_maxBlurRadius);
            m_material.SetFloat(s_blurCurveExpId, m_blurCurveExp);
            m_material.SetFloat(s_desatDelayId,   m_desatDelay);
            m_material.SetFloat(s_desatCurveExpId,m_desatCurveExp);
        }

        // ---- selection (paint-while-holding) ----
        // Each frame while trigger is held the brush rectangle grows to include the
        // current controller aim.  Release locks the painted region.
        // Pressing again starts a fresh region.

        private bool  m_isPainting;
        private const float k_brushPad = 0.12f; // ~7° padding around each touched point

        private void HandleSelection()
        {
            bool held        = OVRInput.Get(OVRInput.RawButton.RIndexTrigger);
            bool justPressed = OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger);

            GetControllerAzEl(out float az, out float el);

            if (justPressed)
            {
                // Start a fresh paint stroke.
                m_activeRect = new Vector4(
                    az - k_brushPad, az + k_brushPad,
                    el - k_brushPad, el + k_brushPad);
                m_material.SetVector(s_focusRectId, m_activeRect);
                m_isPainting = true;
            }
            else if (held && m_isPainting)
            {
                // Grow the rect to cover the new aim position.
                m_activeRect = new Vector4(
                    Mathf.Min(m_activeRect.x, az - k_brushPad),
                    Mathf.Max(m_activeRect.y, az + k_brushPad),
                    Mathf.Min(m_activeRect.z, el - k_brushPad),
                    Mathf.Max(m_activeRect.w, el + k_brushPad));
                m_material.SetVector(s_focusRectId, m_activeRect);
            }

            if (!held) m_isPainting = false;

            DrawPointerAndBorder(az, el, held);
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

        // ---- pointer + border drawing ----
        // Uses the single LineRenderer for both:
        //   • A crosshair (+) showing current aim direction at all times.
        //   • A rectangle border around the locked/active clear region.

        private void DrawPointerAndBorder(float az, float el, bool holding)
        {
            if (m_selectionLine == null) return;
            m_selectionLine.enabled = true;
            m_selectionLine.loop    = false;

            Transform head = Camera.main != null ? Camera.main.transform : transform;
            Vector3   org  = head.position;
            float     r    = 9.5f;
            float     pr   = 0.055f; // crosshair arm length (~3°)

            bool hasBorder = m_activeRect != k_fullSphere;

            if (hasBorder)
            {
                // 5 border points (close the rect) + 5 crosshair points = 10 total.
                Color borderColor = holding ? m_lineColorHeld : m_lineColorLocked;
                m_selectionLine.positionCount = 10;
                m_selectionLine.startColor    = borderColor;
                m_selectionLine.endColor      = Color.white;

                float azMin = m_activeRect.x, azMax = m_activeRect.y;
                float elMin = m_activeRect.z, elMax = m_activeRect.w;
                m_selectionLine.SetPosition(0, org + DirFromAzEl(azMin, elMin) * r);
                m_selectionLine.SetPosition(1, org + DirFromAzEl(azMax, elMin) * r);
                m_selectionLine.SetPosition(2, org + DirFromAzEl(azMax, elMax) * r);
                m_selectionLine.SetPosition(3, org + DirFromAzEl(azMin, elMax) * r);
                m_selectionLine.SetPosition(4, org + DirFromAzEl(azMin, elMin) * r); // close
                // crosshair (line fades to white by end)
                m_selectionLine.SetPosition(5, org + DirFromAzEl(az - pr, el) * r);
                m_selectionLine.SetPosition(6, org + DirFromAzEl(az + pr, el) * r);
                m_selectionLine.SetPosition(7, org + DirFromAzEl(az,       el) * r);
                m_selectionLine.SetPosition(8, org + DirFromAzEl(az, el - pr)  * r);
                m_selectionLine.SetPosition(9, org + DirFromAzEl(az, el + pr)  * r);
            }
            else
            {
                // No region yet — just show the crosshair.
                m_selectionLine.positionCount = 5;
                m_selectionLine.startColor    = Color.white;
                m_selectionLine.endColor      = Color.white;
                m_selectionLine.SetPosition(0, org + DirFromAzEl(az - pr, el) * r);
                m_selectionLine.SetPosition(1, org + DirFromAzEl(az + pr, el) * r);
                m_selectionLine.SetPosition(2, org + DirFromAzEl(az,       el) * r);
                m_selectionLine.SetPosition(3, org + DirFromAzEl(az, el - pr)  * r);
                m_selectionLine.SetPosition(4, org + DirFromAzEl(az, el + pr)  * r);
            }
        }

        // ---- YOLO detection zones ----

        private void OnDetectionsReady(IReadOnlyList<(int classId, Vector4 box)> detections, Vector2Int inputSize)
        {
            // Reset slot counter each update cycle; we'll fill from index 0.
            int slot = 0;
            foreach (var (classId, box) in detections)
            {
                if (slot >= k_maxDetections) break;
                if (!k_targetClasses.Contains(classId)) continue;

                m_detectionRects[slot]     = BoxToAzElRect(box, inputSize);
                m_detectionTimestamp[slot] = Time.time;
                slot++;
            }
            // Leave older slots as-is — UpdateDetectionUniforms expires them by timestamp.
        }

        private void UpdateDetectionUniforms()
        {
            // Count slots that haven't expired yet.
            int count = 0;
            for (int i = 0; i < k_maxDetections; i++)
            {
                if (Time.time - m_detectionTimestamp[i] < m_detectionLifetime)
                    count = i + 1; // keep the array dense by always including up to last live slot
            }
            m_material.SetInt(s_detectionCountId,        count);
            m_material.SetVectorArray(s_detectionRectsId, m_detectionRects);
        }

        // Converts a YOLO bounding box (pixel space, y-down) to an az/el rect.
        private Vector4 BoxToAzElRect(Vector4 box, Vector2Int inputSize)
        {
            // box = (x1, y1, x2, y2) in model pixel space; y=0 is top of image.
            // UV: x maps left→right, y is flipped (image top → UV bottom).
            var tl = UvToAzEl(new Vector2(box.x / inputSize.x, 1f - box.y / inputSize.y));
            var br = UvToAzEl(new Vector2(box.z / inputSize.x, 1f - box.w / inputSize.y));
            return new Vector4(
                Mathf.Min(tl.x, br.x), Mathf.Max(tl.x, br.x),
                Mathf.Min(tl.y, br.y), Mathf.Max(tl.y, br.y));
        }

        // Reverses the shader's UV projection: UV [0,1] → azimuth/elevation (radians).
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
