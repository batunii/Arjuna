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
        /// Default focus-window half-width (deg), applied symmetrically to az/el. Seeds both
        /// the free-play painted-brush size and the fixed Blocks B/C windscreen window, so
        /// there is one shared, Inspector-tunable source for "how big is the window by default"
        /// (see CameraSphereVignetteManager/VideoTestSceneManager, Filter header).
        /// </summary>
        float DefaultWindowHalfWidthDeg { get; }

        /// <summary>
        /// Effect strength actually sent to the shader this frame (0..1), after motion
        /// suppression and study overrides. Logged by StudyLogger as dr_intensity.
        /// </summary>
        float CurrentEffectiveStrength { get; }

        /// <summary>
        /// True: participant A/B/paint input is ignored. MUST be true while any condition
        /// runs — the trigger belongs to the task (CPT / probe presses) and would otherwise
        /// repaint the focus window.
        /// </summary>
        bool StudyInputLock { get; set; }

        /// <summary>
        /// True: effect forced invisible (baseline conditions) without changing mode,
        /// window, or anything else the participant experiences.
        /// </summary>
        bool StudyEffectSuppressed { get; set; }

        /// <summary>
        /// Motion-based suppression availability. The video manager's serialized default is
        /// FALSE — the sequencer's configuration guard asserts/fixes this for Block B.
        /// The passthrough manager has motion suppression always on (returns true).
        /// </summary>
        bool MotionEnabled { get; set; }

        /// <summary>Set a mode at full formed strength, without the mode toast.</summary>
        void StudySetMode(VignetteMode mode);

        /// <summary>Lock a focus window programmatically (az/el radians).</summary>
        void StudySetWindow(Vector4 azElRadians);

        /// <summary>Clear the focus window (back to full sphere / no effect region).</summary>
        void StudyClearWindow();
    }
}
