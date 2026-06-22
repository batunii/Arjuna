# System: CameraSphereVignette (DR attention-guidance overlay)

Last updated: 2026-06-22. Branch: `feature/PolishingModes`.

Multi-mode Diminished Reality attention-guidance prototype for the MSc dissertation. One shader + one
manager on a **head-centered inverted sphere** (~10 m radius, `Cull Front`). The sphere renders as
transparent inside the focus window so OS passthrough (full ~110° FOV, OS quality) shows through;
the DR effect lives only in the periphery.

---

## Files

| File | Purpose |
|---|---|
| `ShaderSample/Shaders/CameraSphereVignette.shader` | Shader `Meta/PCA/CameraSphereVignette` |
| `ShaderSample/Scripts/CameraSphereVignetteManager.cs` | Driver MonoBehaviour |
| `ShaderSample/Materials/SelectionDotMat.mat` | Template material for selection dot clones |
| `Assets/CameraSphereVignette.unity` | Scene |

GameObject in scene: `CameraSphereSphere`. Manager lives on it.

---

## Modes

### Mode 1 — Blur (`VignetteMode.Blur`)

Both passthrough cameras (Left + Right `PassthroughCameraAccess`) are sampled. A **hard-split blend**
picks the camera with the higher in-FOV weight at each fragment (`inFovL >= inFovR ? sampledL :
sampledR`) — no alpha crossfade avoids ghosting. The periphery is **blurred** (9-tap offset kernel,
radius scales with `tEff`) and **desaturated** (greyscale lerp, delayed by `_DesatDelay`).

Vignette strength is always 1 (no formation animation). Focus window is transparent → OS passthrough shows through.

### Mode 2 — Soft Dark (`VignetteMode.SoftDark`)

`_SimpleMode = 1`: shader skips all camera sampling, returns `fixed4(0, 0, 0, t * _VignetteStrength * _MaxVignetteAlpha)`.
`_MaxVignetteAlpha` = `m_mode2MaxAlpha` (default 0.75) — periphery never fully opaque.
`_VignetteStrength` is animated 0→1 by `FormVignette()` coroutine (SmoothStep over `m_vignetteFormTime`),
triggered on right trigger release.

### Mode 3 — Hard Dark (`VignetteMode.HardDark`)

Same as Soft Dark but `_MaxVignetteAlpha = 1.0` → complete black-out in periphery.

---

## Shader: key properties

| Property | Set by | Meaning |
|---|---|---|
| `_FocusRect` | Manager / frame | `(azMin, azMax, elMin, elMax)` in radians — world-locked focus rect |
| `_SoftEdge` | `m_softEdgeDeg * Deg2Rad` | SmoothStep width at rect boundary |
| `_SimpleMode` | `UpdateModeUniforms` | 0 = Blur (camera), 1 = Dark (simple) |
| `_VignetteStrength` | `UpdateModeUniforms` | Formation + motion suppression combined (0–1) |
| `_MaxVignetteAlpha` | `UpdateModeUniforms` | Ceiling alpha (0.75 = Soft, 1.0 = Hard) |
| `_MainTexL/R` | Start coroutine | Left/Right PCA textures |
| `_HasRightCam` | `FeedCameraUniforms` | 1 if right camera is live |
| `_SphereCenter` | `UpdateSpherePosition` | Head world-pos (updated each frame) |
| `_CamL/RFwd/Rt/Up`, `_TanHalfFovL/R` | `FeedOneCameraUniforms` | Camera projection basis |

**`t` value** (per-fragment): `smoothstep(0, _SoftEdge, max(dAz, dEl))` where `dAz/dEl` is signed
distance outside `_FocusRect`. `t = 0` inside focus window, `t = 1` fully outside. Mode 1 uses `t`
for blur and desat weights; Modes 2/3 use it directly as overlay alpha.

---

## Manager: key systems

### Focus rect selection (`HandleSelection`)

Right trigger held → paint rect (starts at cursor ± `k_brushPad`, expands while held).
Right trigger release → lock rect; trigger vignette formation (Modes 2/3) or snap strength=1 (Mode 1);
start dot-hide countdown.

Rect stored in world spherical coords `(azMin, azMax, elMin, elMax)` via `atan2` / `asin` on
controller forward. Recomputed every frame → world-locked even as head moves.

**B** = clear rect, cancel formation, hide dots immediately.
**A** = cycle mode (Blur → SoftDark → HardDark → Blur); if rect exists in new Modes 2/3, starts
formation immediately.

### Vignette formation (`FormVignette` coroutine)

`m_vignetteStrength` SmoothStep 0→1 over `m_vignetteFormTime` seconds. Runs independently of motion
suppression — if head moves and suppresses, the coroutine keeps counting in the background so the
effect resumes at whatever level it reached when the head settles.

### Motion suppression (`UpdateMotionDisable`)

Computes head angular velocity (deg/s) each frame using `Quaternion.Angle(m_lastHeadRot, head.rotation)`.

Per-mode thresholds: `m_motionBlur / m_motionSoftDark / m_motionHardDark` (`MotionSettings { speedThreshDeg, holdSeconds }`).
Global fade speeds: `m_motionFadeOutSec` (default 0.2 s), `m_motionFadeInSec` (default 0.7 s).

`m_motionSuppression` (float 0→1) is animated with `MoveTowards` at those speeds. Shader receives
`effectiveStrength = m_vignetteStrength * (1 - m_motionSuppression)`.

**Focus-region cancellation:** each frame, if the head direction is inside `_FocusRect + 15°` margin
(`k_focusArrivalMarginRad = 0.2618 rad`), `m_motionDisableTimer` is zeroed immediately → effect
restores without waiting for the hold timer, even if the user was moving fast.

### Mode indicator toast (`InitModeUI / ShowModeToast / UpdateModeUI`)

World-space Canvas (0.54 m × 0.15 m), lazy-follow at 1.5 m forward + 0.3 m below eye level. Billboards
toward head (`transform.rotation = LookRotation(canvasPos - headPos)`).

Content: pip dots (`● ○ ○` etc.), mode name, description, controls hint. Coloured accent stripe (blue/amber/red).
Fade-in 0.35 s, total show time 2.8 s, then fade out. Triggered on `ShowModeToast()` (called on A press and at startup).

Font: `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")` with `"Arial.ttf"` fallback.

### Selection dots (`InitSelectionDots / DrawPointerAndBorder`)

5 runtime sphere GameObjects: [0–3] corner dots, [4] yellow cursor dot. Each gets `new Material(m_dotMaterialTemplate)`
clone at `renderQueue = 4000`.

`m_dotMaterialTemplate` MUST be assigned in the Inspector (set to `SelectionDotMat.mat`). This
guarantees the material shader is included in the Android build — `Shader.Find("Unlit/Color")` returns
null on Quest (stripped shaders), which would silently crash the Start coroutine.

Auto-hide: `HideDotsCoro()` waits `m_dotHideDelay` seconds, then shrinks corner dots to scale 0 over
0.3 s. `m_cornerDotsHidden` flag prevents `DrawPointerAndBorder` from re-enabling them during shrink.
Cancelled and restored (`RestoreDotsScale`) on next trigger press.

---

## Architecture notes

- **Sphere renders from inside** (`Cull Front`): each fragment's world direction is `normalize(worldPos - _SphereCenter)`.
- **Transparent queue** (`Queue = Transparent`), `Blend SrcAlpha OneMinusSrcAlpha`, `ZWrite Off`. Alpha = 0 in focus window → OS passthrough shows through. This is how the full-FOV effect works — the sphere is invisible where you should look.
- **World-locked rect** via az/el (not screen-space, not head-space) — the window stays fixed in the room even as the head rotates.
- **No blur in Modes 2/3** — the early-return in the shader skips all camera math. Performance cost in those modes is minimal.
- **Dual-camera blend** (Mode 1): hard-split (not alpha blend) avoids double-image ghosting. Left + right cameras face the same direction (horizontally offset like eyes); the stitch is invisible in normal use.

---

## Known constraints / not yet done

- On-device parameter tuning (motion thresholds, formation time, blur curve, soft edge) — not done yet
- YOLO salience disabled (`m_sentisModel: {fileID: 0}` — unassigned intentionally to avoid build failure)
- No session persistence for selected region
- Hand-tracking input disabled; right-hand controller only
- `m_blurCurveExp` is 3.0 in scene YAML (was intended to be 4.0 — CoPlay reset it during a save)

---

## Inspector parameters (CameraSphereVignetteManager)

| Header | Field | Default | Notes |
|---|---|---|---|
| Filter | `m_softEdgeDeg` | 20° | Vignette edge softness |
| Filter | `m_maxBlurRadius` | 0.01 | Mode 1 blur UV radius |
| Filter | `m_blurCurveExp` | 3.0 | Blur ramp power |
| Filter | `m_desatDelay` | 0.3 | Desat starts at this fraction of tEff |
| Filter | `m_desatCurveExp` | 3.0 | Desat ramp power |
| Mode | `m_vignetteMode` | Blur | Starting mode |
| Mode | `m_vignetteFormTime` | 3 s | SmoothStep formation duration (Modes 2/3) |
| Mode | `m_mode2MaxAlpha` | 0.75 | Soft Dark max overlay opacity |
| Motion | `m_motionBlur.speedThreshDeg` | 30°/s | |
| Motion | `m_motionSoftDark.speedThreshDeg` | 50°/s | |
| Motion | `m_motionHardDark.speedThreshDeg` | 70°/s | |
| Motion | `m_motionFadeOutSec` | 0.20 s | Effect fade-out speed on movement |
| Motion | `m_motionFadeInSec` | 0.70 s | Effect fade-in speed on settle |
| Dots | `m_dotSize` | 0.055 | World-space sphere radius |
| Dots | `m_dotHideDelay` | 3 s | Seconds before corner dots shrink away |

Related: [[dissertation-attention-guidance]], [[pca-mono-camera-stereo-comfort]]
