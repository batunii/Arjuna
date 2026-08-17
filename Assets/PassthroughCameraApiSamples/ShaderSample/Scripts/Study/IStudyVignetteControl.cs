// Study API implemented by both vignette managers (CameraSphereVignetteManager for the
// passthrough scene / Block A, VideoTestSceneManager for the video scene / Blocks B & C).
// The ConditionSequencer drives conditions exclusively through this interface so the
// study never depends on which scene it is running in.
//
// Contract: Dissertation/testing-strategy-v2.md §13 (Appendix A).

using UnityEngine;

namespace PassthroughCameraSamples.ShaderSample
{
    public interface IStudyVignetteControl
    {
        /// <summary>Current vignette mode (read-only; set via StudySetMode).</summary>
        VignetteMode CurrentMode { get; }

        /// <summary>Locked focus window in az/el radians (azMin, azMax, elMin, elMax).</summary>
        Vector4 ActiveRect { get; }

        /// <summary>
        /// Default focus-window half-width (deg), applied symmetrically to az/el. Seeds both the
        /// free-play brush size and the fixed study window.
        /// </summary>
        float DefaultWindowHalfWidthDeg { get; }

        /// <summary>
        /// Effect strength actually sent to the shader this frame (0..1), after motion
        /// suppression and study overrides. Logged by StudyLogger as dr_intensity.
        /// </summary>
        float CurrentEffectiveStrength { get; }

        /// <summary>
        /// True: participant A/B/paint input is ignored. Must be true while a condition runs,
        /// since the trigger belongs to the task and would otherwise repaint the window.
        /// </summary>
        bool StudyInputLock { get; set; }

        /// <summary>
        /// True: effect forced invisible (baseline conditions) without changing mode,
        /// window, or anything else the participant experiences.
        /// </summary>
        bool StudyEffectSuppressed { get; set; }

        /// <summary>
        /// Motion-based suppression availability. The video manager's serialized default is
        /// false; the passthrough manager always returns true.
        /// </summary>
        bool MotionEnabled { get; set; }

        /// <summary>Set a mode at full formed strength, without the mode toast.</summary>
        void StudySetMode(VignetteMode mode);

        /// <summary>
        /// Explicit on/off toggle, independent of StudySetMode. True ramps the effect in with the
        /// same gradual formation free-play uses, instantly for camera modes, which have no
        /// formation animation. False resets to 0 immediately.
        /// </summary>
        void StudySetActive(bool active);

        /// <summary>Lock a focus window programmatically (az/el radians).</summary>
        void StudySetWindow(Vector4 azElRadians);

        /// <summary>Clear the focus window (back to full sphere / no effect region).</summary>
        void StudyClearWindow();
    }
}
