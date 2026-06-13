// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.ShaderSample
{
    /// <summary>
    /// Drives the <c>Meta/PCA/FocusVignette</c> shader for the dissertation's STATIC mode (Mode 3).
    /// Place this on the inverted sphere GameObject (whose MeshRenderer uses the FocusVignette material).
    ///
    /// The user defines a fixed clear "window" by pointing a controller at FOUR corners:
    /// - A button / index pinch: place the next corner at the aim point.
    /// - B button / middle pinch: reset and start over.
    /// Outside the quad the periphery is tunnelled (blacked out). Until 4 corners are placed the whole
    /// view is clear so the user can aim. The window is world-locked (corners stored as world points;
    /// directions recomputed from the head each frame).
    /// </summary>
    [MetaCodeSample("PassthroughCameraApiSamples-ShaderSample")]
    public class FocusVignetteManager : MonoBehaviour
    {
        [SerializeField] private PassthroughCameraAccess m_cameraAccess;
        [SerializeField] private MeshRenderer m_renderer;
        [SerializeField] private Text m_debugText;

        [Tooltip("Head transform (e.g. CenterEyeAnchor). Falls back to Camera.main.")]
        [SerializeField] private Transform m_headAnchor;

        [Tooltip("Controller transform used to aim (e.g. RightControllerAnchor). Falls back to the head.")]
        [SerializeField] private Transform m_pointer;

        [Tooltip("Distance (m) along the controller ray where a corner is placed / the aim cursor sits.")]
        [SerializeField] private float m_selectDistance = 2.5f;

        [Header("Selection visuals (assign small world-space markers)")]
        [SerializeField] private Transform m_aimCursor;
        [SerializeField] private Transform[] m_cornerMarkers = new Transform[4];

        [Header("Outside-window look (default = tunnel)")]
        [SerializeField] private Color m_outsideColor = Color.black;
        [SerializeField, Range(0, 1)] private float m_outsideMax = 1f;

        [Tooltip("Approximate horizontal FOV (deg) fallback for the salience projection until intrinsics arrive.")]
        [SerializeField] private float m_cameraHorizontalFovDeg = 82f;

        private Material m_material;
        private bool m_loggedIntrinsics;
        private readonly Vector3[] m_cornerPoints = new Vector3[4];
        private int m_cornerCount;

        private static readonly int s_mainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int s_sphereCenterId = Shader.PropertyToID("_SphereCenter");
        private static readonly int s_headRightId = Shader.PropertyToID("_HeadRight");
        private static readonly int s_headUpId = Shader.PropertyToID("_HeadUp");
        private static readonly int s_headForwardId = Shader.PropertyToID("_HeadForward");
        private static readonly int s_tanHalfFovId = Shader.PropertyToID("_TanHalfFov");
        private static readonly int s_dimColorId = Shader.PropertyToID("_DimColor");
        private static readonly int s_maxDimId = Shader.PropertyToID("_MaxDim");
        private static readonly int s_regionActiveId = Shader.PropertyToID("_RegionActive");
        private static readonly int[] s_cornerIds =
        {
            Shader.PropertyToID("_Corner0"),
            Shader.PropertyToID("_Corner1"),
            Shader.PropertyToID("_Corner2"),
            Shader.PropertyToID("_Corner3"),
        };

        private Transform Head =>
            m_headAnchor != null ? m_headAnchor : (Camera.main != null ? Camera.main.transform : null);

        private Transform Pointer => m_pointer != null ? m_pointer : Head;

        private IEnumerator Start()
        {
            m_material = m_renderer.material;
            m_material.SetColor(s_dimColorId, m_outsideColor);
            m_material.SetFloat(s_maxDimId, m_outsideMax);
            m_material.SetFloat(s_regionActiveId, 0f);
            HideMarkers();

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

            // Camera texture is read by FocusSalienceDetector (not for colour here); harmless to assign.
            m_material.SetTexture(s_mainTexId, m_cameraAccess.GetTexture());
            UpdateStatusText();
        }

        private void LateUpdate()
        {
            var head = Head;
            if (head == null || m_material == null)
            {
                return;
            }

            // Keep the sphere centered on the head; tell the shader where the center is.
            transform.position = head.position;
            m_material.SetVector(s_sphereCenterId, head.position);
            m_material.SetVector(s_headRightId, head.right);
            m_material.SetVector(s_headUpId, head.up);
            m_material.SetVector(s_headForwardId, head.forward);
            FeedFovUniform();

            // Aim point along the controller ray; show the cursor while still placing corners.
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

            // A / index pinch: place the next corner. B / middle pinch: reset.
            if (InputManager.IsButtonADownOrPinchStarted() && m_cornerCount < 2)
            {
                PlaceCorner(aimPoint);
            }
            if (InputManager.IsButtonBDownOrMiddleFingerPinchStarted())
            {
                ResetRegion();
            }

            // Feed the world-locked corner directions (from the current head) once all 4 are placed.
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
            if (m_cornerMarkers == null)
            {
                return;
            }
            foreach (var marker in m_cornerMarkers)
            {
                if (marker != null)
                {
                    marker.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateStatusText()
        {
            if (m_debugText == null)
            {
                return;
            }
            m_debugText.text = m_cornerCount >= 2
                ? "Window set (B to redo)"
                : $"Aim + A to place corner {m_cornerCount + 1}/2 (opposite corners)";
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
                    Debug.Log($"[FocusVignette] focal={intr.FocalLength} sensorRes={intr.SensorResolution} " +
                              $"currentRes={cur} tanHalfFov=({tanX:F3},{tanY:F3})");
                }
            }
            else
            {
                float aspect = 4f / 3f;
                if (m_cameraAccess != null && m_cameraAccess.IsPlaying)
                {
                    var res = m_cameraAccess.CurrentResolution;
                    if (res.x > 0 && res.y > 0)
                    {
                        aspect = (float)res.x / res.y;
                    }
                }
                tanX = Mathf.Tan(0.5f * m_cameraHorizontalFovDeg * Mathf.Deg2Rad);
                tanY = tanX / aspect;
            }
            m_material.SetVector(s_tanHalfFovId, new Vector4(tanX, tanY, 0f, 0f));
        }
    }
}
