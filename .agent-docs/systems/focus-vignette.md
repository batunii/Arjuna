# System: CameraSphereVignette (DR attention-guidance overlay)

Last updated: 2026-07-16.

Multi-mode Diminished Reality attention-guidance prototype for the MSc dissertation. One shader +
**two** managers on a **head-centered inverted sphere** (~10 m radius, `Cull Front`):
`CameraSphereVignetteManager` (live passthrough cameras, this doc) and
[`VideoTestSceneManager`](<video-test-scene.md>) (looping 360° video instead of live cameras, same
shader/enum/API — see that doc for video-specific behaviour: SignPop detection gating, baked
detection lifetimes, flat-clip mode). The sphere renders as transparent inside the focus window so
OS passthrough (full ~110° FOV, OS quality) shows through; the DR effect lives only in the periphery.

Both managers implement [`IStudyVignetteControl`](#study-api-istudyvignettecontrol), the shared
API [Study Tooling](<study-tooling.md>) (formal `ConditionSequencer` and the informal
`TestModeSequencer`/`BlobTargetController`) drives them through — study code never needs to know
which scene/manager it's talking to.

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

`VignetteMode` enum (`CameraSphereVignetteManager.cs`) — 11 values total. The study proper only
uses Blur / SoftDark / HardDark / ColorPop (one per block, see
[study-tooling.md](<study-tooling.md>)); the rest are free-play/exploratory or video-only.

| # | Mode | Summary |
|---|---|---|
| 0 | **Blur** | Both PCA cameras sampled; hard-split blend picks whichever has higher in-FOV weight per fragment (no alpha crossfade → no ghosting). Periphery blurred (9-tap kernel, radius scales with `tEff`) + desaturated (delayed by `_DesatDelay`). Strength always 1 (no formation animation) — focus window transparent, OS passthrough shows through. |
| 1 | **SoftDark** | `_SimpleMode=1`: shader skips camera sampling, returns `fixed4(0,0,0, t*_VignetteStrength*_MaxVignetteAlpha)`. `_MaxVignetteAlpha = m_mode2MaxAlpha` (0.75) — never fully opaque. Strength animates 0→1 via `FormVignette()` (SmoothStep over `m_vignetteFormTime`). |
| 2 | **HardDark** | Same as SoftDark but `_MaxVignetteAlpha=1.0` — complete black-out in periphery. |
| 3 | TintedDark | Configurable-colour dark overlay, gradual formation (same mechanism as SoftDark/HardDark). |
| 4 | ChromaticCool | Camera mode: warm focus, cool blue periphery shift. |
| 5 | **ColorPop** | Camera mode: muted grey periphery; saturated warm/green (ROG-band) colours boosted vivid inside the window. Heavily tunable (`m_pop*` fields — sat boost, glare compression, sigmoid contrast, etc). |
| 6 | ConspicuitySqueeze | Camera mode: periphery contrast flattened toward local mean (Veas 2011). |
| 7 | GranulatedPeriphery | World-locked noise grains, density ramps with eccentricity (Cao 2021). |
| 8 | OutlinedDark | Near-blackout with luminance edges kept (Cheng 2022). |
| 9 | SpotLift | Focus window brightened + soft peripheral dim (video mode). |
| 10 | SignPop | **Video only** — ColorPop gated by baked detections; only actual traffic lights/signs pop, ROG look-alikes stay muted. See [video-test-scene.md](<video-test-scene.md>#signpop-detection-gated-colorpop). |

"Camera" modes (Blur, ChromaticCool, ColorPop, ConspicuitySqueeze, SignPop) render the source
texture instantly with no formation animation — `IsCameraMode(mode)` gates this. All other modes
ramp in via `FormVignette()`.

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

### World anchor (Hard Dark real-object lock) — added 2026-07-16

Base behaviour (above) keeps `_FocusRect` fixed in *bearing* (az/el), recomputed every frame from
the head's **current** position — so it doesn't rotate with head turns, but it does translate with
the user, i.e. it's a fixed direction-cone from wherever the eyes currently are, not a lock onto a
real 3D point. `TryWorldAnchorSelection()` / `UpdateWorldAnchorRect()` add an opt-in second mode,
**Hard Dark only**, that locks the window onto an actual physical object so it stays put as the
user walks toward/away/around it — like a real hole cut in a wall, growing/shrinking angularly with
distance exactly as looking at a fixed object would.

- **Trigger:** on trigger-release (`HandleSelection`'s existing paint gesture), if
  `m_vignetteMode == HardDark` and `m_raycastManager` is assigned, `TryWorldAnchorSelection()`
  raycasts the 4 corners of the just-locked `m_activeRect` (via `Meta.XR.EnvironmentRaycastManager.Raycast`,
  MRUK v81+ depth API, no separate `EnvironmentDepthManager` needed at the installed MRUK v201) and
  stores the 4 world-space hit points (`m_anchorBL/BR/TL/TR`) if all 4 hit real geometry.
- **Per-frame:** `UpdateWorldAnchorRect()` (called from `LateUpdate` right after `HandleSelection`)
  re-derives az/el for each of the 4 stored world points from the **current** head position every
  frame, and rebuilds `_FocusRect` as their min/max — this is what makes it perspective-correct
  without any shader changes (the shader already just consumes `_FocusRect` in az/el space).
- **Fails gracefully:** if any corner ray misses (out of depth range, outside the depth camera
  frustum, reflective/transparent surface), `m_hasWorldAnchor` stays false and that selection just
  behaves like the legacy head-relative bearing — logged via `SetDebug()` **with the per-corner
  `EnvironmentRaycastHitStatus` values** (added 2026-07-20 after an on-device all-corners miss
  whose cause was undiagnosable — the old message conflated 5 distinct failure statuses).
- **NotReady auto-retry (added 2026-07-20):** the native raycaster is created asynchronously at
  scene load and reports `NotReady` until then, so painting in the first seconds after entering
  the scene used to fail permanently. A `NotReady` result now retries every 0.25 s for up to 10 s
  (`k_anchorRetrySeconds`), aborting silently if the selection or mode changes meanwhile.
- **Cleared** on: new paint (`justPressed`), B-button clear, mode cycle (A button), `StudySetMode`,
  `StudySetWindow`/`StudyClearWindow` — so a stale anchor never silently reactivates after a mode
  switch back to Hard Dark.
- **Scene wiring:** `CameraSphereVignette.unity` gained a plain `EnvironmentRaycastManager`
  GameObject, wired to the new `m_raycastManager` serialized field on `CameraSphereSphere`. See
  [Environment Raycast](<environment-raycast.md>) for the underlying component.
- **Not yet done:** on-device verification (Depth API raycasts don't function in Editor/XR
  Simulator — Quest 3/3S only); no cross-session persistence of the anchor (would need
  `OVRSpatialAnchor`, per Environment Raycast's own docs on world-locking without MRUK).

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

## Study API (`IStudyVignetteControl`)

`Assets/PassthroughCameraApiSamples/ShaderSample/Scripts/Study/IStudyVignetteControl.cs`.
Implemented identically by `CameraSphereVignetteManager` and `VideoTestSceneManager` so
[study code](<study-tooling.md>) never depends on which scene is active.

| Member | Purpose |
|---|---|
| `CurrentMode`, `ActiveRect`, `DefaultWindowHalfWidthDeg`, `CurrentEffectiveStrength` | Read-only state. |
| `StudyInputLock` | True: participant A/B/paint free-play input is ignored (`HandleSelection`'s top-of-function guard). MUST be true while a formal condition runs — the trigger belongs to the task. `TestModeSequencer` deliberately never sets this (it has no competing task to protect). |
| `StudyEffectSuppressed` | True: effect forced invisible (baseline conditions) *without* changing mode/window. Also now correctly suppresses the detection-highlight boost (`UpdateDetectionUniforms` gates `_DetectionCount` on this — was a real bug, see [study-tooling.md](<study-tooling.md>#bugs-fixed-while-building-this-apply-beyond-the-test-branch)). |
| `MotionEnabled` | Passthrough manager: always true. Video manager: serialized field, default false — the sequencer's config guard asserts/fixes this for Block B. |
| `StudySetMode(mode)` | Set a mode at **full formed strength instantly**, no toast. |
| `StudySetActive(bool)` | *(Added 2026-07)* Explicit on/off toggle, independent of `StudySetMode`: `true` ramps in via the same `FormVignette()` coroutine free-play uses (instant for camera modes, which have no formation animation); `false` resets to 0 instantly. Lets a caller show "off, then form in on demand" instead of `StudySetMode`'s implicit snap-to-1. Added for `TestModeSequencer`'s X-toggle in passthrough modes. |
| `StudySetWindow(azElRadians)` / `StudyClearWindow()` | Lock/clear the focus window programmatically. |

---

## Known constraints / not yet done

- On-device parameter tuning (motion thresholds, formation time, blur curve, soft edge) — not done yet
- YOLO salience disabled (`m_sentisModel: {fileID: 0}` — unassigned intentionally to avoid build failure)
- No session persistence for selected region (world-anchored Hard Dark selections included — the
  anchor lives only in memory for the current app session, no `OVRSpatialAnchor`)
- Hand-tracking input disabled; right-hand controller only
- `m_blurCurveExp` is 3.0 in scene YAML (was intended to be 4.0 — CoPlay reset it during a save)
- World-anchor (Hard Dark) has not been verified on-device yet — Depth API raycasts return nothing
  in Editor/XR Simulator, so `TryWorldAnchorSelection`'s success path is untested outside a real
  Quest 3/3S

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
| World Anchor | `m_raycastManager` | None | Optional `Meta.XR.EnvironmentRaycastManager` ref — leave unassigned to keep legacy head-relative-bearing behaviour for Hard Dark; assign to enable world-locking (see "World anchor" above) |
| Motion | `m_motionBlur.speedThreshDeg` | 30°/s | |
| Motion | `m_motionSoftDark.speedThreshDeg` | 50°/s | |
| Motion | `m_motionHardDark.speedThreshDeg` | 70°/s | |
| Motion | `m_motionFadeOutSec` | 0.20 s | Effect fade-out speed on movement |
| Motion | `m_motionFadeInSec` | 0.70 s | Effect fade-in speed on settle |
| Dots | `m_dotSize` | 0.055 | World-space sphere radius |
| Dots | `m_dotHideDelay` | 3 s | Seconds before corner dots shrink away |

Related: [Video Test Scene](<video-test-scene.md>), [Study Tooling](<study-tooling.md>),
[[dissertation-attention-guidance]], [[pca-mono-camera-stereo-comfort]]
