# System: Focus Vignette (world-locked attention filter)

Last updated: 2026-06-09

A **multi-mode** Diminished-Reality overlay on the real system passthrough (Underlay) — full
passthrough quality, no camera feed in the visuals (→ no warp). One shader + `FocusVignetteManager`,
**switch modes with the left controller Y button** (`_FocusMode`):

- **Mode 1 — DYNAMIC (driving):** a clear **cone follows the gaze** (`_FocusDir` = head forward),
  soft falloff `_InnerAngle.._OuterAngle`; **light** dim in the periphery (`m_dynamicDimColor`,
  `m_dynamicMax` ~0.45). The dim **eases off while you turn your head** (head angular speed >
  `m_motionThresholdDeg` → `_DrIntensity`→0 over `m_revealSeconds`) and **eases back on when you
  settle** (over `m_reapplySeconds` ≈ 2–4s) — situational-awareness easing (Gap 3). No selection.
- **Mode 3 — STATIC (workstation):** user picks **two opposite corners** → axis-aligned rectangle
  window; **tunnel** (black) outside, gradual soft edge (`_EdgeSoftness`).

YOLO salience un-dims detected objects in both modes (esp. relevant for Mode 1 situational awareness).

**Static controller selection (`FocusVignetteManager`):** the user picks **two opposite corners** → an
axis-aligned rectangle window.
- **A / index pinch** — place the next corner at the aim point (`pointer.position + forward *
  m_selectDistance`); a cyan `FV_AimCursor` shows where you're aiming; yellow markers mark placed
  corners.
- **B / middle pinch** — reset and start over.
- Until both corners are placed (`_RegionActive = 0`) the whole view is clear so the user can aim.
  Corners are stored as **world points**; their directions-from-head are recomputed each frame, so the
  window is world-locked. `m_pointer` = RightControllerAnchor; `m_headAnchor` = CenterEyeAnchor.
- The outside fade is **gradual** (`_EdgeSoftness`, default 0.2) via a signed-distance soft edge, not a
  hard boundary.

**Outside look:** `_DimColor`/`_MaxDim` (default black @ 1.0 = tunnel; set dark-grey @ ~0.8 for a gentle
dim instead). Markers use an `Unlit/Color` material (watch for pink on device → add `Unlit/Color` to
Always Included Shaders if stripped).

**Why no camera feed:** the modifiable camera feed warps (mono, offset, no depth correction) and the
crisp system passthrough can't be blurred/read (OS owns the pixels — see
[[pca-mono-camera-stereo-comfort]]). So the effect is **subtractive (black-out/dim), not blur**.

Architecture: Transparent queue, `Blend SrcAlpha OneMinusSrcAlpha`, `Cull Front`, `ZWrite Off`. The
shader gnomonically projects the 4 corner directions + the fragment direction onto the region-center
tangent plane and does a point-in-quad test; `alpha = tunnel * _MaxDim`. YOLO salience reduces `tunnel`
→ detected objects stay visible through the tunnel. Head-basis/intrinsics uniforms are fed only to
locate the salience boxes (no colour sampled).

## Files

- `ShaderSample/Shaders/FocusVignette.shader` — shader `Meta/PCA/FocusVignette`.
- `ShaderSample/Scripts/FocusVignetteManager.cs` — driver MonoBehaviour.
- `ShaderSample/Materials/FocusVignetteMat.mat` — material using the shader.
- `ShaderSample/Scripts/FocusSalienceDetector.cs` — slim YOLO runner (peripheral object salience).

## Peripheral object salience (YOLO)

`FocusSalienceDetector` runs the bundled Sentis YOLOv9 model (COCO-80) on the camera frames and feeds
the bounding boxes of "important" classes (people, stop signs, vehicles, … — configurable
`m_highlightClasses`) to the shader as normalized image-space rects (`_SalienceBoxes` + `_SalienceCount`,
up to 16). In those regions the shader drops the blur/desaturation (`effect *= 1 - salience`) and adds a
brightness/contrast boost, so safety-relevant objects pop out of the dimmed periphery.

- It is **detection-only**: no spatial anchors, no environment raycast, no DetectionManager graph
  (unlike `MultiObjectDetection`'s `SentisInferenceRunManager`). Depends only on
  `PassthroughCameraAccess` + the model + labels.
- Shader tunables: `_SalienceFeather` (soft edge), `_HighlightBrightness`, `_HighlightContrast`,
  `_SalienceFlipY` (reconcile box vs camera-UV vertical orientation if misaligned).

## Dissertation alignment (Static mode)

This effect is the **Static-mode prototype** for the dissertation (see [[dissertation-attention-guidance]])
— the 3-channel DR pipeline: **desaturation** (`_Desat`) + **blur** (`_BlurRadius`) + **boundary
highlight** (`_EdgeGlow` / `_EdgeColor` / `_EdgeWidth`, a soft rim at the focus-cone edge).

- **User-controlled reveal (Gaps 3+4):** B button / middle-finger pinch toggles DR off (reveal the
  full scene) and back on, **eased** via `_DrIntensity` (driven by `m_transitionSpeed` in
  `FocusVignetteManager`). Demonstrates temporal transitions + user-controlled release.
- Still TODO toward the full dissertation: mode system + UI, Dynamic (head-follow + auto easing on
  head turn) and Semi-Dynamic (multi-anchor) modes, Kawase blur (replace the box blur), per-mode
  presets. The YOLO salience is parked here but belongs to the Dynamic (driving) mode.
- Box alignment is approximate (camera FOV ≠ eye FOV, and the camera is sampled screen-space), which is
  fine for a subtle salience boost; calibrate `_SalienceFlipY` / thresholds on device.

## Stereo comfort (double-vision fix)

The camera UV is computed from each fragment's **world direction** (head → fragment), projected
through a head-centered pinhole (`_HeadRight/_HeadUp/_HeadForward` + `_TanHalfFov`, fed per-frame by
`FocusVignetteManager` from the head transform and `m_cameraHorizontalFovDeg`). Because the world
direction is **eye-independent**, the same real-world point maps to the same camera pixel in both eyes,
so the mono image **fuses** instead of doubling.

This replaced the original screen-space sampling (`uv = screenPos`), which tied the UV to per-eye
screen position → each eye sampled a different camera pixel for the same point → double vision. See
[[pca-mono-camera-stereo-comfort]].

The display FOV (`_TanHalfFov`) is derived from the **camera intrinsics**: horizontal half-FOV =
`SensorResolution.x / (2 * FocalLength.x)`; **vertical is derived from the displayed texture aspect**
(`CurrentResolution`) to avoid a Y-stretch when the sensor aspect differs from the streamed/cropped
aspect. `m_cameraHorizontalFovDeg` is only a fallback until intrinsics are available. `_FlipY` if the
image is vertically inverted.

Out-of-FOV handling: the passthrough camera's FOV is narrower than the headset display, so the lower
periphery (looking down) falls outside the camera image. Rather than clamping/stretching the edge
texels (vertical streaks), the shader forces those areas to **full blur + desaturation** (a soft
dimmed periphery — `_FovFeather` controls the edge softness; `fovInside` drives `effect → 1` beyond the
FOV). It deliberately does NOT darken to black (that read as an ugly dark "box"). If the periphery
still reads as a frame, the cleaner fix is showing real system passthrough there (alpha-blend over the
underlay) — a noted follow-up.

Trade-off: the world looks flat-ish (mono → no real depth) and the periphery beyond the camera FOV is
dark, but it fuses comfortably. A one-time `[FocusVignette]` Debug.Log reports the real
focal/sensorRes/currentRes/FOV at startup (read via on-device logcat).

Performance: `FocusSalienceDetector` throttles inference via `m_detectionInterval` (default 0.15s ≈
6–7 Hz) — running YOLO every frame on CPU starved the render thread. Switch `m_backend` to
`GPUCompute` for snappier detection if needed.

## How it works

- Renders on the **inside of a large inverted sphere** (`Cull Front`, `ZWrite Off`) centered on
  the head. Each fragment derives a world-space direction from `worldPos - _SphereCenter`, so the
  effect is locked to the room, not the head rotation.
- `effect = smoothstep(_InnerAngle, _OuterAngle, angleFromFocus)` → `0` inside the focus cone,
  ramping to `1` in the periphery. Used to lerp sharp→blurred and to drive desaturation.
- Camera feed is sampled in **screen space** (`ComputeScreenPos`) — the camera image fills the eye
  viewport; the focus mask is what's world-locked.
- Blur is a cheap 9-tap box blur with radius scaled by `effect`.

## Shader properties

| Property | Meaning |
|---|---|
| `_MainTex` | Passthrough camera texture (set from script). |
| `_FocusDir` | World-space focus direction (set/frozen from script). |
| `_InnerAngle` / `_OuterAngle` | Radians; start/end of the sharp→blurred falloff. |
| `_BlurRadius` | Max blur radius in texels at full effect. |
| `_Desat` | Max desaturation (0–1) at full effect. |
| `_FlipY` | Toggle if the camera image is vertically inverted. |
| `_SphereCenter` | Head/sphere center world pos (uniform, set per-frame by script). |

## Manager (`FocusVignetteManager`)

Place on the sphere GameObject. Serialized fields:

- `m_cameraAccess` — the scene `PassthroughCameraAccess`.
- `m_renderer` — the sphere's `MeshRenderer` (uses `FocusVignetteMat`).
- `m_debugText` — optional permission-status `Text`.
- `m_headAnchor` — head transform (e.g. `CenterEyeAnchor`); falls back to `Camera.main`.
- `m_allowRecenter` — if true, A button / index pinch re-aims the focus to current gaze
  (uses [InputManager](input.md)).

Behavior: waits for camera permission + `IsPlaying`, assigns `GetTexture()` to `_MainTex`, then each
`LateUpdate` recenters the sphere on the head (position only) and writes `_SphereCenter`; freezes
`_FocusDir` to the head's forward at start (and on re-center).

## Scene

`Assets/FocusVignette.unity` is the **boot scene (Build index 0)**; StartScene is index 1 (kept so
the ☰/Start-button `ReturnToStartScene` still works). It was created from `ShaderSample.unity`, so it
reuses the camera rig, passthrough, and `PassthroughCameraAccessPrefab`. Differences from ShaderSample:

> Boot-scene note: the shared `RequestPermissionsOnce` only fires for scenes loaded *after*
> StartScene, so it never triggers when FocusVignette boots directly. `FocusVignetteManager` therefore
> **requests `PassthroughCameraAccess` itself** in `Start` before waiting on it.


- Water plane / pool geometry (`ShaderSampleWaterArea`) removed; `ShaderSampleManager` component
  removed from the `ShaderSampleManagerPrefab` (its `DebugText` child is kept and reused).
- A `FocusVignetteSphere` (Sphere primitive, scale 20 → 10 m radius, collider removed) with
  `FocusVignetteMat` and the `FocusVignetteManager`, wired to: `m_cameraAccess` →
  `PassthroughCameraAccessPrefab`, `m_renderer` → its own MeshRenderer, `m_headAnchor` →
  `CenterEyeAnchor`, `m_debugText` → the kept `DebugText`.

To recreate from scratch: duplicate `ShaderSample.unity`, remove the water area + old manager, add a
large sphere with `FocusVignetteMat` + `FocusVignetteManager`, wire the four references, add to
Build Settings.

## Constraints / notes

- The passthrough camera is a **single mono, forward-facing** sensor — only the FOV cone you're
  looking at has data. Fine here: the periphery is blurred/desaturated anyway, which hides
  mono/offset artifacts.
- Heavy blur is the main perf cost; the 9-tap box blur is intentionally cheap. For stronger blur,
  pre-blur the camera texture into a downsampled RenderTexture and lerp.
- Keep the inner cone generous and the falloff gradual for comfort; tight rings feel claustrophobic
  and hard edges shimmer with head motion.

Related: [Passthrough Camera Access](passthrough-camera-access.md), [Input](input.md),
[ShaderSample scene](<../scenes/shader-sample.md>).
