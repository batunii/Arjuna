// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.ShaderSample
{
    /// <summary>
    /// Drives the <c>Meta/PCA/FocusVignette</c> shader. Place this on the inverted
    /// sphere GameObject (the one whose MeshRenderer uses the FocusVignette material).
    ///
    /// Responsibilities:
    /// - Wait for camera permission + stream, then feed the camera texture to the material.
    /// - Keep the sphere centered on the head each frame and tell the shader where the
    ///   center is (so the user always sits at the sphere's center).
    /// - Freeze the focus direction in WORLD space so the sharp region stays locked to the
    ///   room rather than following the head. Optionally re-aim it with A / index pinch.
    /// </summary>
    [MetaCodeSample("PassthroughCameraApiSamples-ShaderSample")]
    public class FocusVignetteManager : MonoBehaviour
    {
        [SerializeField] private PassthroughCameraAccess m_cameraAccess;
        [SerializeField] private MeshRenderer m_renderer;
        [SerializeField] private Text m_debugText;

        [Tooltip("Head transform (e.g. OVRCameraRig/TrackingSpace/CenterEyeAnchor). Falls back to Camera.main.")]
        [SerializeField] private Transform m_headAnchor;

        [Tooltip("Allow re-aiming the focus direction to the current gaze with A / index pinch.")]
        [SerializeField] private bool m_allowRecenter = true;

        [Tooltip("Approximate horizontal FOV (deg) used to map the camera image. Tune so the image fills the view naturally.")]
        [SerializeField] private float m_cameraHorizontalFovDeg = 82f;

        private Material m_material;

        private static readonly int s_mainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int s_focusDirId = Shader.PropertyToID("_FocusDir");
        private static readonly int s_sphereCenterId = Shader.PropertyToID("_SphereCenter");
        private static readonly int s_headRightId = Shader.PropertyToID("_HeadRight");
        private static readonly int s_headUpId = Shader.PropertyToID("_HeadUp");
        private static readonly int s_headForwardId = Shader.PropertyToID("_HeadForward");
        private static readonly int s_tanHalfFovId = Shader.PropertyToID("_TanHalfFov");

        private Transform Head =>
            m_headAnchor != null ? m_headAnchor : (Camera.main != null ? Camera.main.transform : null);

        private IEnumerator Start()
        {
            // Use the instanced material so we don't mutate the shared asset.
            m_material = m_renderer.material;

            if (m_debugText != null)
            {
                m_debugText.text = "No permission granted.";
            }

            // Request the camera permission ourselves so this scene works even when it is the
            // first/boot scene (the shared RequestPermissionsOnce only fires for scenes loaded
            // AFTER StartScene, so it would never trigger if we launch directly into this scene).
            if (!OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.PassthroughCameraAccess))
            {
                OVRPermissionsRequester.Request(new[] { OVRPermissionsRequester.Permission.PassthroughCameraAccess });
            }

            while (!OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.PassthroughCameraAccess))
            {
                yield return null;
            }

            if (m_debugText != null)
            {
                m_debugText.text = "Permission granted.";
            }

            while (!m_cameraAccess.IsPlaying)
            {
                yield return null;
            }

            // The PassthroughCameraAccess GPU texture updates itself each frame; assign once.
            m_material.SetTexture(s_mainTexId, m_cameraAccess.GetTexture());

            FreezeFocusToHead();
        }

        private void LateUpdate()
        {
            var head = Head;
            if (head == null || m_material == null)
            {
                return;
            }

            // Keep the sphere centered on the head (translation only — never copy head
            // rotation, or the focus region would stop being world-locked).
            transform.position = head.position;
            m_material.SetVector(s_sphereCenterId, head.position);

            // Feed the head basis + FOV so the shader can project each fragment's world
            // direction into camera UV identically for both eyes (fixes the double vision).
            m_material.SetVector(s_headRightId, head.right);
            m_material.SetVector(s_headUpId, head.up);
            m_material.SetVector(s_headForwardId, head.forward);

            float aspect = 4f / 3f;
            if (m_cameraAccess != null && m_cameraAccess.IsPlaying)
            {
                var res = m_cameraAccess.CurrentResolution;
                if (res.x > 0 && res.y > 0)
                {
                    aspect = (float)res.x / res.y;
                }
            }
            float tanX = Mathf.Tan(0.5f * m_cameraHorizontalFovDeg * Mathf.Deg2Rad);
            float tanY = tanX / aspect;
            m_material.SetVector(s_tanHalfFovId, new Vector4(tanX, tanY, 0f, 0f));

            if (m_allowRecenter && InputManager.IsButtonADownOrPinchStarted())
            {
                FreezeFocusToHead();
            }
        }

        private void FreezeFocusToHead()
        {
            var head = Head;
            if (head == null || m_material == null)
            {
                return;
            }

            // Lock the sharp region to wherever the user is currently looking, in world space.
            m_material.SetVector(s_focusDirId, head.forward);
        }
    }
}
