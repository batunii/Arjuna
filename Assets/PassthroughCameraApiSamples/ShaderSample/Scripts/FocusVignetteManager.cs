// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.ShaderSample
{
    /// <summary>
    /// Drives the <c>Meta/PCA/FocusVignette</c> shader across the dissertation modes. Place on the
    /// inverted sphere GameObject (whose MeshRenderer uses the FocusVignette material).
    ///
    /// - DYNAMIC (Mode 1, driving): clear cone follows the gaze; light dim in the periphery; the dim
    ///   eases OFF while you turn your head (situational awareness) and eases back on when you settle.
    /// - STATIC (Mode 3, workstation): point a controller at two opposite corners to define a fixed
    ///   rectangle window; tunnel (black-out) outside it.
    ///
    /// Left controller Y toggles modes. In Static: A/index-pinch places a corner, B/middle-pinch resets.
    /// </summary>
    [MetaCodeSample("PassthroughCameraApiSamples-ShaderSample")]
    public class FocusVignetteManager : MonoBehaviour
    {
        private enum FocusMode { Dynamic, Static }

        [SerializeField] private PassthroughCameraAccess m_cameraAccess;
        [SerializeField] private MeshRenderer m_renderer;
        [SerializeField] private Text m_debugText;
        [SerializeField] private Transform m_headAnchor;

        [Tooltip("Controller transform used to aim in Static mode (e.g. RightControllerAnchor).")]
        [SerializeField] private Transform m_pointer;

        [SerializeField] private FocusMode m_mode = FocusMode.Dynamic;

        [Header("Dynamic (Mode 1) — gaze-follow + motion easing")]
        [SerializeField] private Color m_dynamicDimColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        [SerializeField, Range(0, 1)] private float m_dynamicMax = 0.45f;
        [SerializeField] private float m_dynamicInnerAngle = 0.35f;   // radians
        [SerializeField] private float m_dynamicOuterAngle = 0.95f;   // radians
        [Tooltip("Head angular speed (deg/s) above which the dim eases off for situational awareness.")]
        [SerializeField] private float m_motionThresholdDeg = 30f;
        [Tooltip("Seconds to ease the dim OFF when you start turning.")]
        [SerializeField] private float m_revealSeconds = 0.25f;
        [Tooltip("Seconds to ease the dim back ON once your gaze settles (2-4s).")]
        [SerializeField] private float m_reapplySeconds = 3f;

        [Header("Static (Mode 3) — controller rectangle window")]
        [SerializeField] private Color m_staticDimColor = Color.black;
        [SerializeField, Range(0, 1)] private float m_staticMax = 1f;
        [SerializeField, Range(0.01f, 1f)] private float m_staticEdgeSoftness = 0.2f;
        [SerializeField] private float m_selectDistance = 2.5f;
        [SerializeField] private Transform m_aimCursor;
        [SerializeField] private Transform[] m_cornerMarkers = new Transform[4];

        [SerializeField] private float m_cameraHorizontalFovDeg = 82f;

        private Material m_material;
        private bool m_loggedIntrinsics;
        private Vector3 m_prevForward = Vector3.forward;
        private float m_drIntensity = 1f;
        private readonly Vector3[] m_cornerPoints = new Vector3[2];
        private int m_cornerCount;

        private static readonly int s_mainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int s_sphereCenterId = Shader.PropertyToID("_SphereCenter");
        private static readonly int s_headRightId = Shader.PropertyToID("_HeadRight");
        private static readonly int s_headUpId = Shader.PropertyToID("_HeadUp");
        private static readonly int s_headForwardId = Shader.PropertyToID("_HeadForward");
        private static readonly int s_tanHalfFovId = Shader.PropertyToID("_TanHalfFov");
        private static readonly int s_focusModeId = Shader.PropertyToID("_FocusMode");
        private static readonly int s_focusDirId = Shader.PropertyToID("_FocusDir");
        private static readonly int s_innerAngleId = Shader.PropertyToID("_InnerAngle");
        private static readonly int s_outerAngleId = Shader.PropertyToID("_OuterAngle");
        private static readonly int s_edgeSoftnessId = Shader.PropertyToID("_EdgeSoftness");
        private static readonly int s_drIntensityId = Shader.PropertyToID("_DrIntensity");
        private static readonly int s_dimColorId = Shader.PropertyToID("_DimColor");
        private static readonly int s_maxDimId = Shader.PropertyToID("_MaxDim");
        private static readonly int s_regionActiveId = Shader.PropertyToID("_RegionActive");
        private static readonly int[] s_cornerIds =
        {
            Shader.PropertyToID("_Corner0"),
            Shader.PropertyToID("_Corner1"),
        };

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

            // Common: keep the sphere on the head; feed head basis + FOV (for the salience projection).
            transform.position = head.position;
            m_material.SetVector(s_sphereCenterId, head.position);
            m_material.SetVector(s_headRightId, head.right);
            m_material.SetVector(s_headUpId, head.up);
            m_material.SetVector(s_headForwardId, head.forward);
            FeedFovUniform();

            // B button / middle-finger pinch toggles modes (works with controllers AND hands).
            if (InputManager.IsButtonBDownOrMiddleFingerPinchStarted())
            {
                m_mode = m_mode == FocusMode.Dynamic ? FocusMode.Static : FocusMode.Dynamic;
                ApplyMode();
            }

            if (m_mode == FocusMode.Dynamic)
            {
                UpdateDynamic(head);
            }
            else
            {
                UpdateStatic(head);
            }
        }

        private void UpdateDynamic(Transform head)
        {
            // Clear cone follows the gaze.
            m_material.SetVector(s_focusDirId, head.forward);

            // Ease the dim OFF while turning (situational awareness), back ON when settled.
            float angle = Vector3.Angle(head.forward, m_prevForward);
            float speed = Time.deltaTime > 0f ? angle / Time.deltaTime : 0f;
            m_prevForward = head.forward;

            float target = speed > m_motionThresholdDeg ? 0f : 1f;
            float seconds = target < m_drIntensity ? m_revealSeconds : m_reapplySeconds;
            float rate = 1f / Mathf.Max(seconds, 0.01f);
            m_drIntensity = Mathf.MoveTowards(m_drIntensity, target, rate * Time.deltaTime);
            m_material.SetFloat(s_drIntensityId, m_drIntensity);
        }

        private void UpdateStatic(Transform head)
        {
            m_material.SetFloat(s_drIntensityId, 1f);

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

            // A / index pinch places the next corner; once both are placed, A restarts the selection.
            // (B / middle pinch is the global mode switch, handled in LateUpdate.)
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

            m_material.SetFloat(s_focusModeId, m_mode == FocusMode.Static ? 1f : 0f);

            if (m_mode == FocusMode.Dynamic)
            {
                m_material.SetColor(s_dimColorId, m_dynamicDimColor);
                m_material.SetFloat(s_maxDimId, m_dynamicMax);
                m_material.SetFloat(s_innerAngleId, m_dynamicInnerAngle);
                m_material.SetFloat(s_outerAngleId, m_dynamicOuterAngle);
                m_material.SetFloat(s_regionActiveId, 0f);
                m_drIntensity = 1f;
                HideMarkers();
                if (m_aimCursor != null)
                {
                    m_aimCursor.gameObject.SetActive(false);
                }
            }
            else
            {
                m_material.SetColor(s_dimColorId, m_staticDimColor);
                m_material.SetFloat(s_maxDimId, m_staticMax);
                m_material.SetFloat(s_edgeSoftnessId, m_staticEdgeSoftness);
                m_material.SetFloat(s_drIntensityId, 1f);
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
            if (m_mode == FocusMode.Dynamic)
            {
                m_debugText.text = "Mode 1: Dynamic  (B / middle-pinch = switch mode)";
            }
            else
            {
                m_debugText.text = m_cornerCount >= 2
                    ? "Mode 3: Static — window set  (A = redo, B = switch mode)"
                    : $"Mode 3: Static — aim + A, corner {m_cornerCount + 1}/2  (B = switch mode)";
            }
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
