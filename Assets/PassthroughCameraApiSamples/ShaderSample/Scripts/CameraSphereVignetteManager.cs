// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
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

        // Fallback FOV when camera intrinsics are unavailable.
        [SerializeField] private float m_cameraHorizontalFovDeg = 82f;

        // ---- shader property IDs ----
        private static readonly int s_mainTexId      = Shader.PropertyToID("_MainTex");
        private static readonly int s_sphereCenterId = Shader.PropertyToID("_SphereCenter");
        private static readonly int s_headRightId    = Shader.PropertyToID("_HeadRight");
        private static readonly int s_headUpId       = Shader.PropertyToID("_HeadUp");
        private static readonly int s_headForwardId  = Shader.PropertyToID("_HeadForward");
        private static readonly int s_tanHalfFovId   = Shader.PropertyToID("_TanHalfFov");
        private static readonly int s_focusRectId    = Shader.PropertyToID("_FocusRect");
        private static readonly int s_softEdgeId     = Shader.PropertyToID("_SoftEdge");
        private static readonly int s_maxBlurRadId   = Shader.PropertyToID("_MaxBlurRadius");
        private static readonly int s_blurCurveExpId = Shader.PropertyToID("_BlurCurveExp");
        private static readonly int s_desatDelayId   = Shader.PropertyToID("_DesatDelay");
        private static readonly int s_desatCurveExpId= Shader.PropertyToID("_DesatCurveExp");

        // Full-sphere default → fully transparent overlay before first selection.
        private static readonly Vector4 k_fullSphere =
            new(-Mathf.PI, Mathf.PI, -Mathf.PI * 0.5f, Mathf.PI * 0.5f);

        private Material m_material;
        private bool     m_loggedIntrinsics;

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
            SetDebug("Point at corner A and press trigger.");
        }

        private void LateUpdate()
        {
            if (m_material == null) return;

            UpdateHeadUniforms();
            UpdateFilterUniforms();
            HandleSelection();
        }

        // ---- head uniforms (updated every frame) ----

        private void UpdateHeadUniforms()
        {
            Transform head = Camera.main != null ? Camera.main.transform : transform;

            // Sphere follows head position so the user is always at the centre.
            // It does NOT rotate — that is intentional.
            transform.position = head.position;

            m_material.SetVector(s_sphereCenterId, head.position);
            m_material.SetVector(s_headRightId,    head.right);
            m_material.SetVector(s_headUpId,       head.up);
            m_material.SetVector(s_headForwardId,  head.forward);
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
