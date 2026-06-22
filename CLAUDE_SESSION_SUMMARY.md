# Claude Session Summary — Diminished Reality Attention-Guidance Dissertation

> Last updated: 2026-06-22. Branch: `feature/PolishingModes`.
> Companion doc: `.agent-docs/systems/focus-vignette.md` (full technical detail).
> Dissertation plan: `Dissertation/dissertation_progress_report.md`.

---

## What this is

MSc dissertation prototype: **"Guiding User Attention in Real-World Tasks Using XR Overlays"** — a
3-mode Diminished Reality attention-guidance system on **Quest 3 passthrough**. Built on Meta's
`Unity-PassthroughCameraApiSamples` (Unity 6).

The effect is one shader + one C# manager on a **head-centered inverted sphere** (~10 m radius). The
sphere is transparent where the user should look (focus window) and applies a DR effect in the
periphery. OS passthrough bleeds through the transparent focus window at full ~110° FOV — this is the
key architectural win.

---

## Active files

| File | Role |
|---|---|
| `Assets/CameraSphereVignette.unity` | Scene (boot it in Unity to work) |
| `ShaderSample/Shaders/CameraSphereVignette.shader` | Shader: `Meta/PCA/CameraSphereVignette` |
| `ShaderSample/Scripts/CameraSphereVignetteManager.cs` | Driver MonoBehaviour |
| `ShaderSample/Materials/SelectionDotMat.mat` | Template for runtime selection dot clones |

The sphere GameObject in the scene is called `CameraSphereSphere`.
`m_dotMaterialTemplate` on the manager must be wired to `SelectionDotMat.mat` (guarantees inclusion in
Quest build — `Shader.Find` returns null for stripped shaders on Android).

---

## Current state — what works

### Three modes (switchable with A button at runtime)

| Mode | Enum | Effect | Formation |
|---|---|---|---|
| **Blur** | `VignetteMode.Blur` | Dual-camera overlay in periphery — blurred + desaturated | Instant (always strength=1) |
| **Soft Dark** | `VignetteMode.SoftDark` | Pure dark overlay, never fully opaque (`m_mode2MaxAlpha` ≈ 0.75) | Gradual: SmoothStep 0→1 over `m_vignetteFormTime` |
| **Hard Dark** | `VignetteMode.HardDark` | Pure dark overlay, full black-out | Gradual: same coroutine, max alpha = 1.0 |

In all modes: the focus window is **transparent** → OS passthrough (full ~110° FOV, full quality)
shows through. The DR effect only lives in the periphery.

### Controls

| Input | Action |
|---|---|
| Right trigger (hold + sweep) | Paint focus region (world-locked rect in az/el space) |
| Right trigger release | Lock region; start vignette formation (Modes 2/3) |
| **A** | Cycle mode forward (Blur → Soft Dark → Hard Dark → Blur) |
| **B** | Clear region, reset formation, hide corner dots |

### Selection UI

- 5 runtime sphere GameObjects: **4 corner dots** (white while holding, dimmed when locked) + **1 yellow cursor dot**
- Corner dots auto-hide after `m_dotHideDelay` seconds (default 3 s) via scale-shrink animation
- Pressing trigger again cancels the hide and restores dots at full size

### Mode indicator toast

On A press (mode change), a world-space canvas panel appears at ~1.5 m forward, 0.3 m below eye level:
- Dark background with **colour-coded accent stripe** (blue = Blur, amber = Soft Dark, red = Hard Dark)
- Pip dots (e.g. `● ○ ○`) + mode name + description + controls hint
- Fade-in 0.35 s → visible 2.8 s total → fade out

### Motion-based suppression

When head angular speed exceeds a per-mode threshold, the vignette effect is **gradually suppressed**
(not cut instantly). The effect fades back in when the head slows down.

- **Per-mode thresholds** in Inspector: `m_motionBlur`, `m_motionSoftDark`, `m_motionHardDark`
  (each has `speedThreshDeg` and `holdSeconds`)
- **Global fade speeds**: `m_motionFadeOutSec` (default 0.2 s), `m_motionFadeInSec` (default 0.7 s)
- **Key behaviour**: if head enters the focus rect (+ 15° margin), the hold timer is zeroed
  immediately — effect restores without waiting. Moving toward the region still suppresses until arrival.

---

## Key architectural decisions (do not re-litigate)

1. **OS passthrough in focus window, not camera feed.** The shader returns `alpha = t` where `t = 0`
   inside the focus rect. Alpha=0 → transparent → OS passthrough at full quality shows through. This
   solved the FOV problem (camera gives ~85-90°; OS gives ~110°) and the quality problem in one line.

2. **World-locked focus rect in azimuth/elevation.** The focus rect is stored as
   `(azMin, azMax, elMin, elMax)` in radians. Each fragment computes `az = atan2(dir.x, dir.z)` and
   `el = asin(dir.y)` — fully world-locked, not head-locked.

3. **Mode 2/3 skip camera sampling entirely.** The shader has an early return for `_SimpleMode > 0.5`:
   `return fixed4(0, 0, 0, t * _VignetteStrength * _MaxVignetteAlpha)`. No camera UV math, no blur — just a dark overlay.

4. **Dual camera (Left + Right PCA), hard-split blend.** Mode 1 samples both cameras and picks based
   on FOV weight: `camColor = (inFovL >= inFovR) ? sampledL : sampledR`. No alpha blend (no ghosting).
   The two forward-facing cameras have the same orientation (just horizontally offset like eyes), so the
   stitch is invisible in normal use.

5. **`SelectionDotMat.mat` as serialized template.** Runtime dots use `new Material(m_dotMaterialTemplate)`
   clones. The template being in project assets guarantees it survives Android build stripping.
   `Shader.Find("Unlit/Color")` returns null on Quest — never use it for runtime materials.

---

## Parameters to tune on device

All in the Inspector on `CameraSphereSphere > CameraSphereVignetteManager`:

| Group | Key fields |
|---|---|
| Filter | `m_softEdgeDeg` (vignette softness), `m_maxBlurRadius`, `m_blurCurveExp`, `m_desatDelay`, `m_desatCurveExp` |
| Mode | `m_vignetteFormTime` (formation duration), `m_mode2MaxAlpha` (Soft Dark ceiling) |
| Motion | `m_motionBlur/SoftDark/HardDark.speedThreshDeg`, `.holdSeconds`, `m_motionFadeOutSec`, `m_motionFadeInSec` |
| Dots | `m_dotSize`, `m_dotHideDelay` |

---

## What is NOT done yet

- On-device tuning of all parameters (especially motion thresholds and formation time)
- Saving/persisting the selected region between sessions
- Any formal user study / data collection tooling
- YOLO salience integration (was disabled — `m_sentisModel: {fileID: 0}`)
- Hand-tracking input (disabled; controllers only for now)

---

## How to run

1. Open `Assets/CameraSphereVignette.unity` in Unity (ensure CoPlay is connected for agent edits).
2. Connect Quest 3 via USB, controllers awake (restart headset if input is dead — it gets stuck in
   hand-tracking mode).
3. **Build And Run** (Android / Quest target).
4. On device: hold right trigger + sweep to paint a region, release to lock. A to cycle modes. B to clear.

> Build to a **non-OneDrive** folder — OneDrive sync locks files mid-build.
