# Project Overview

Last updated: 2026-06-03

## Identity

- **Name:** Unity-PassthroughCameraApiSamples
- **Purpose:** Official Meta sample set for the **Passthrough Camera API (PCA)** — accessing Quest
  headset RGB cameras at runtime (texture, intrinsics/extrinsics, pose, timestamps).
- **Unity version:** `6000.0.61f1` (Unity 6; README floor is 6000.0.38f1).
- **Source:** Mirror of `github.com/oculus-samples/Unity-PassthroughCameraApiSamples`.

## Target Platform

- **Hardware:** Quest 3 / Quest 3S, Horizon OS v74+.
- **XR stack:** OpenXR (`com.unity.xr.openxr`) + Unity XR Management; Meta XR / OVR plugin.
- **Required permission:** `horizonos.permission.HEADSET_CAMERA` (+ Scene permission for depth/raycast).
- **Constraint:** Passthrough camera works only on a physical headset or Meta Horizon Link v2.1+.
  The XR Simulator does **not** support PCA.

## High-Level Architecture

- A single asset root, `Assets/PassthroughCameraApiSamples/`, holds everything.
- One folder per sample (`CameraViewer`, `CameraToWorld`, `BrightnessEstimation`,
  `MultiObjectDetection`, `ShaderSample`) plus shared `PassthroughCamera/` and `StartScene/` folders.
- Each sample folder follows the same layout: `Scripts/`, `Prefabs/`, and the sample `.unity` scene.
- No assembly definitions (`.asmdef`) — all scripts compile into the default `Assembly-CSharp`.
- No networking. Single-user, on-device.
- The core camera abstraction is the `PassthroughCameraAccess` MonoBehaviour from the MRUK package
  (namespace `Meta.XR`); samples reference it via a serialized field and read `GetTexture()`,
  `GetColors()`, `GetCameraPose()`, `ViewportPointToRay()`, `CurrentResolution`, `IsPlaying`.

## Key Third-Party Packages

| Package | Version | Role in this project |
|---|---|---|
| `com.meta.xr.mrutilitykit` (MRUK) | 201.0.0 | Provides `PassthroughCameraAccess`, `EnvironmentRaycastManager`, OVR plumbing. The core dependency. |
| `com.unity.ai.inference` (Sentis / Inference Engine) | 2.2.1 | Runs the YOLOv9 model in the MultiObjectDetection sample. |
| `com.unity.xr.openxr` | 1.15.1 | OpenXR runtime backend. |
| `com.unity.xr.management` | 4.5.3 | XR loader configuration per build target. |
| `com.unity.ugui` | 2.0.0 | uGUI canvases for all sample UI. |
| `com.unity.mobile.android-logcat` | 1.4.6 | On-device log inspection. |
| `com.coplaydev.coplay` | git `#beta` | Editor AI tooling (Coplay MCP). Not part of sample runtime. |

## Notes

- The repo is wired for AI coding agents (`AGENTS.md`, `.mcp.json`, `CLAUDE.md`/RTK, Meta Quest skills).
- `CLAUDE.md` instructs using the `rtk` command prefix for token-efficient tooling output.
- The repo also now hosts a second, unrelated build on top of the sample suite: an MSc
  dissertation Diminished-Reality attention-guidance study (`CameraSphereVignette` +
  `VideoTestScene` scenes, `ShaderSample/Scripts/Study/`). See
  [Scene Flow § Dissertation study build](<scenes/_flow.md>#dissertation-study-build) and
  [Study Tooling](<systems/study-tooling.md>) — do not conflate its scenes/scripts with the
  sample-suite ones above.
