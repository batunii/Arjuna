// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.ShaderSample
{
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

        [Header("Selection Dots")]
        [SerializeField] private Material m_dotMaterialTemplate; // assign SelectionDotMat
        [SerializeField] private float    m_dotSize        = 0.055f;
        [SerializeField] private Color  m_dotColorHeld   = Color.white;
        [SerializeField] private Color  m_dotColorLocked = new Color(1f, 1f, 1f, 0.55f);
        [SerializeField] private Color  m_dotColorCursor = new Color(1f, 0.9f, 0.3f, 1f);

        [Header("Debug")]
        [SerializeField] private bool m_debugCamOverlay;

        [Header("YOLO Detection")]
        [Tooltip("Optional — wire up to enable automatic clear zones for detected objects.")]
        [SerializeField] private YoloRunner m_yoloRunner;
        [SerializeField] private float      m_detectionLifetime = 0.6f;

        [SerializeField] private float m_cameraHorizontalFovDeg = 82f;

        // ---- shader property IDs ----
        private static readonly int s_mainTexLId       = Shader.PropertyToID("_MainTexL");
        private static readonly int s_mainTexRId       = Shader.PropertyToID("_MainTexR");
        private static readonly int s_sphereCenterId   = Shader.PropertyToID("_SphereCenter");
        private static readonly int s_camLFwdId        = Shader.PropertyToID("_CamLFwd");
        private static readonly int s_camLRtId         = Shader.PropertyToID("_CamLRt");
        private static readonly int s_camLUpId         = Shader.PropertyToID("_CamLUp");
        private static readonly int s_tanHalfFovLId    = Shader.PropertyToID("_TanHalfFovL");
        private static readonly int s_camRFwdId        = Shader.PropertyToID("_CamRFwd");
        private static readonly int s_camRRtId         = Shader.PropertyToID("_CamRRt");
        private static readonly int s_camRUpId         = Shader.PropertyToID("_CamRUp");
        private static readonly int s_tanHalfFovRId    = Shader.PropertyToID("_TanHalfFovR");
        private static readonly int s_hasRightCamId    = Shader.PropertyToID("_HasRightCam");
        private static readonly int s_focusRectId      = Shader.PropertyToID("_FocusRect");
        private static readonly int s_softEdgeId       = Shader.PropertyToID("_SoftEdge");
        private static readonly int s_maxBlurRadId     = Shader.PropertyToID("_MaxBlurRadius");
        private static readonly int s_blurCurveExpId   = Shader.PropertyToID("_BlurCurveExp");
        private static readonly int s_desatDelayId     = Shader.PropertyToID("_DesatDelay");
        private static readonly int s_desatCurveExpId  = Shader.PropertyToID("_DesatCurveExp");
        private static readonly int s_detectionCountId    = Shader.PropertyToID("_DetectionCount");
        private static readonly int s_detectionRectsId    = Shader.PropertyToID("_DetectionRects");
        private static readonly int s_debugCamOverlayId   = Shader.PropertyToID("_DebugCamOverlay");

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
        private const float  k_dotDistance = 4f; // metres from head

        // Cached for YOLO coordinate conversion
        private Vector3 m_headRight, m_headUp, m_headForward;
        private Vector2 m_tanHalfFov;

        private Vector4 m_activeRect = k_fullSphere;

        // ---- Unity lifecycle ----

        private IEnumerator Start()
        {
            InitSelectionDots();
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
            SetDebug("Hold trigger and sweep to paint a focus zone.");

            if (m_yoloRunner != null)
                m_yoloRunner.OnDetectionsReady += OnDetectionsReady;
        }

        private void OnDestroy()
        {
            if (m_yoloRunner != null)
                m_yoloRunner.OnDetectionsReady -= OnDetectionsReady;
            if (m_selectionDots != null)
                foreach (var go in m_selectionDots) if (go != null) Destroy(go);
            if (m_dotMats != null)
                foreach (var mat in m_dotMats) if (mat != null) Destroy(mat);
        }

        private void LateUpdate()
        {
            if (m_material == null) return;

            UpdateSpherePosition();
            FeedCameraUniforms();
            UpdateFilterUniforms();
            UpdateDetectionUniforms();
            HandleSelection();
        }

        // ---- sphere + head ----

        private void UpdateSpherePosition()
        {
            Transform head = Camera.main != null ? Camera.main.transform : transform;
            transform.position = head.position;
            m_material.SetVector(s_sphereCenterId, head.position);
            // cache head direction for YOLO az/el conversion
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
                m_tanHalfFov = new Vector2(tanX, tanY); // for YOLO
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
            m_material.SetFloat(s_softEdgeId,     m_softEdgeDeg   * Mathf.Deg2Rad);
            m_material.SetFloat(s_maxBlurRadId,   m_maxBlurRadius);
            m_material.SetFloat(s_blurCurveExpId, m_blurCurveExp);
            m_material.SetFloat(s_desatDelayId,      m_desatDelay);
            m_material.SetFloat(s_desatCurveExpId,   m_desatCurveExp);
            m_material.SetFloat(s_debugCamOverlayId, m_debugCamOverlay ? 1f : 0f);
        }

        // ---- selection (paint-while-holding) ----

        private bool        m_isPainting;
        private const float k_brushPad = 0.12f;

        private void HandleSelection()
        {
            bool held        = OVRInput.Get(OVRInput.RawButton.RIndexTrigger);
            bool justPressed = OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger);

            GetControllerAzEl(out float az, out float el);

            if (justPressed)
            {
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

                // Clone the template material so each dot has independent colour control.
                // Template must be assigned in Inspector to guarantee inclusion in the build.
                var mat = m_dotMaterialTemplate != null
                    ? new Material(m_dotMaterialTemplate)
                    : new Material(Shader.Find("Standard"));
                mat.renderQueue = 4000; // Overlay — always visible on top
                mat.color       = i == 4 ? m_dotColorCursor : m_dotColorHeld;

                go.GetComponent<MeshRenderer>().sharedMaterial = mat;
                go.transform.localScale = Vector3.one * m_dotSize;
                go.SetActive(i == 4);   // only cursor dot starts visible

                m_selectionDots[i] = go;
                m_dotMats[i]       = mat;
            }
        }

        private void DrawPointerAndBorder(float az, float el, bool holding)
        {
            if (m_selectionDots == null) return;

            Transform head = Camera.main != null ? Camera.main.transform : transform;
            Vector3 org = head.position;

            // Cursor dot — always shown
            m_selectionDots[4].transform.position = org + DirFromAzEl(az, el) * k_dotDistance;

            // Corner dots — only when a custom region is active
            bool hasBorder = m_activeRect != k_fullSphere;
            for (int i = 0; i < 4; i++) m_selectionDots[i].SetActive(hasBorder);

            if (!hasBorder) return;

            float azMin = m_activeRect.x, azMax = m_activeRect.y;
            float elMin = m_activeRect.z, elMax = m_activeRect.w;
            m_selectionDots[0].transform.position = org + DirFromAzEl(azMin, elMin) * k_dotDistance;
            m_selectionDots[1].transform.position = org + DirFromAzEl(azMax, elMin) * k_dotDistance;
            m_selectionDots[2].transform.position = org + DirFromAzEl(azMax, elMax) * k_dotDistance;
            m_selectionDots[3].transform.position = org + DirFromAzEl(azMin, elMax) * k_dotDistance;

            Color c = holding ? m_dotColorHeld : m_dotColorLocked;
            for (int i = 0; i < 4; i++) m_dotMats[i].color = c;
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
