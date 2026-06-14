# Claude Session Summary — Focus Vignette / Attention-Guidance Dissertation

> Handoff doc to resume the work. Last session: 2026-06-14.
> Companion docs: `.agent-docs/systems/focus-vignette.md` (system detail), `FOCUS_VIGNETTE_PROGRESS.md`
> (earlier journey), `Dissertation/dissertation_progress_report.md` (the dissertation plan).

## What this is

Implementation for the MSc dissertation **"Guiding User Attention in Real-World Tasks Using XR
Overlays"** — a multi-mode **Diminished Reality (DR)** system on **Quest 3 passthrough**. Built on the
Meta `Unity-PassthroughCameraApiSamples` project (Unity 6, MRUK, Sentis YOLO).

The effect is one shader + one manager on a head-centered inverted sphere
(`Assets/FocusVignette.unity`, boots directly — Build index 0):
- `ShaderSample/Shaders/FocusVignette.shader`
- `ShaderSample/Scripts/FocusVignetteManager.cs`
- `ShaderSample/Scripts/FocusSalienceDetector.cs` (slim YOLO runner → un-dims important objects)

## Current state (what works)

**Multi-mode, switchable, FULL passthrough quality** (a translucent overlay on the real system
passthrough — NO camera feed in the visuals, so no warp/mono issues):

- **Mode 1 — DYNAMIC (driving):** clear cone follows the gaze; **light** dim in the periphery; the dim
  **eases off while you turn your head** (situational awareness) and eases back when you settle.
- **Mode 3 — STATIC (workstation):** point a controller at **two opposite corners** → axis-aligned
  rectangle window; **gradual** (soft-edge) **tunnel/black-out** outside it. *(This is the one the user
  liked best.)*

**Controls** (work with controllers OR hand pinch via the project's `InputManager`):
- **B / middle-finger pinch** = switch Mode 1 ↔ Mode 3.
- **A / index-finger pinch** = (Static) place a corner; press again after 2 to redo.
- Boots into **Mode 3 (Static)**. Debug text shows the current mode.

## The journey / key lessons (so we don't re-litigate)

1. **Mono camera over full FOV = double vision.** Painting the single PCA camera across both eyes
   doesn't fuse. Fixed by sampling via **world direction** (eye-independent) + intrinsics FOV — but the
   raw camera still warps (offset from eyes, no depth correction), worst on close objects.
2. **You cannot read/blur/modify the system passthrough** (OS owns those pixels) — only the camera
   feed is modifiable, and it's lower quality. This is the core trilemma:
   *blur + quality + spatial focus — pick two.*
3. **Final design choice:** keep the crisp **real system passthrough** everywhere and **dim/black-out
   the periphery with a translucent overlay** (subtractive DR = the dissertation's Gap 2). No camera
   feed in the visuals → full quality, no warp. Trade-off: can dim/desaturate but **not blur**.
4. **Input gotcha (resolved):** "controllers don't work" was the **headset being in hand-tracking
   mode** (controllers `CONNECTED_INACTIVE`) — a **headset restart fixed it**. Not a code bug. The
   input code uses `InputManager` (controller buttons OR pinch).
5. ShaderLab `[Header(...)]` text must be plain (no parentheses/dashes) or the build fails to parse.

## Next steps / TODO

- On-device tuning of **Mode 1**: `m_dynamicMax` (dim strength, ~0.45), `m_dynamicOuterAngle` (cone
  width), `m_motionThresholdDeg` / `m_revealSeconds` / `m_reapplySeconds` (the motion easing).
- On-device tuning of **Mode 3**: `_EdgeSoftness` (fade width), `m_staticDimColor`/`m_staticMax`.
- Decide whether **YOLO salience** stays on per mode (currently un-dims detected objects in both).
- Add **Mode 2 (Semi-Dynamic / classroom)** — multi-anchor focus (dissertation's third mode).
- Optional: a proper mode-selection UI; per-mode presets surfaced cleanly (dissertation Gap 1).
- Consider re-introducing hand-tracking input cleanly if hand use is wanted (OpenXR `HandTracking`
  feature — was toggled during debugging; left disabled to match the working config).

## How to run

1. Open `Assets/FocusVignette.unity` in Unity (Coplay connected for agent edits).
2. Connect Quest 3 (USB-debug authorized); ensure controllers are awake / not stuck in hand mode
   (restart headset if input is dead).
3. **Build And Run** (Android). Boots into Mode 3 — aim + A to set a window; B to switch to Mode 1.
4. Capture/verify via `hzdb` screenshots using the **screencap** method (metacam is black while the
   app holds the camera).

## Notes

- Build to a **non-OneDrive** folder (OneDrive sync locks files mid-build → spurious failures).
- `Dissertation/` (research PDFs, ~220 MB) is intentionally **not committed**; consider gitignoring it.
- Branch: `feat/focus-vignette-static-mode` (now also contains Mode 1).
