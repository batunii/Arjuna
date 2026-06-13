# System: Focus Vignette (world-locked attention filter)

Last updated: 2026-06-03

A custom effect built on top of the ShaderSample foundation. Keeps one **world-locked**
direction sharp and progressively **blurs + desaturates** the passthrough camera feed toward the
periphery, to guide the user's attention to a fixed direction in the room.

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

Tuning: `m_cameraHorizontalFovDeg` controls how much the image is zoomed (and aligns the salience
boxes); `_FlipY` if the image is vertically inverted. Needs on-device verification. Trade-off: the
world looks flat-ish (mono → no real depth), but it fuses comfortably.

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
