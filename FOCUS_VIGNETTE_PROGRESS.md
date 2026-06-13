# Focus Vignette + Peripheral Salience — Progress Log

> Working log of the custom effect built on top of the Passthrough Camera API samples.
> Last updated: 2026-06-04

---

## 1. Goal

Build a **focus + peripheral-salience** mixed-reality effect on Quest 3:

- A **world-locked focus cone** stays sharp and in full color.
- The **periphery is progressively blurred + desaturated** (attention guidance — reduce peripheral distraction).
- A **YOLO object detector** runs continuously; when an *important* object (stop sign, person, vehicle…) appears in the dimmed periphery, that region **sheds the blur/desaturation and gets a brightness/contrast boost** so it "pops" to grab attention.

Essentially a safety-attention aid: distraction-free periphery, except things you must not miss.

---

## 2. Base project / environment

- **Project:** Unity-PassthroughCameraApiSamples (Meta sample set).
- **Unity:** 6000.0.61f1 · **Device:** Quest 3 (`eureka`, serial `2G0YC5ZG5F051R`).
- **Core API:** `PassthroughCameraAccess` (Meta MRUK package) — single **mono** RGB camera, intrinsics/extrinsics, GPU texture.
- **ML:** Unity Inference Engine (Sentis) + bundled **YOLOv9**, COCO-80 classes (`SentisYoloClasses.txt`).
- **Tooling:** Coplay MCP (drives the Unity Editor), hzdb MCP (drives the Quest device).
- **Constraint:** PCA only works on a physical Quest 3/3S or Meta Horizon Link v2.1+ — **not** the XR Simulator.

---

## 3. What we built

### New files (under `Assets/PassthroughCameraApiSamples/ShaderSample/`)
| File | Purpose |
|---|---|
| `Shaders/FocusVignette.shader` (`Meta/PCA/FocusVignette`) | The effect: world-locked focus, peripheral blur+desaturation, salience override |
| `Scripts/FocusVignetteManager.cs` | Feeds camera texture, recenters sphere on head, freezes focus dir, feeds head basis+FOV, requests camera permission |
| `Scripts/FocusSalienceDetector.cs` | Slim YOLO runner → feeds detected "important" object boxes to the shader |
| `Materials/FocusVignetteMat.mat` | Material using the shader |

### Scene
- **`Assets/FocusVignette.unity`** — created from `ShaderSample.unity` (reuses camera rig, passthrough, `PassthroughCameraAccessPrefab`).
- Removed the water plane/pool (`ShaderSampleWaterArea`) and the old `ShaderSampleManager` (kept its `DebugText`).
- Added **`FocusVignetteSphere`**: inverted sphere, scale 20 (≈10 m radius), `Cull Front`, collider removed, `FocusVignetteMat`.
- Components on the sphere: `FocusVignetteManager` + `FocusSalienceDetector`, wired to:
  - `m_cameraAccess` → `PassthroughCameraAccessPrefab`
  - `m_renderer` → the sphere's MeshRenderer
  - `m_headAnchor` → `CenterEyeAnchor`
  - `m_debugText` → reused `DebugText`
  - `m_sentisModel` → `yolov9sentis.sentis`, `m_labelsAsset` → `SentisYoloClasses.txt`

### Build / project changes
- **Build target switched** Standalone Windows → **Android**.
- **`FocusVignette` set as boot scene** (Build index 0); `StartScene` kept at index 1 (so ☰/Start still returns to the menu).
- `FocusVignetteManager` **requests the camera permission itself** (the shared `RequestPermissionsOnce` doesn't fire for the boot scene).

### Docs / memory
- `.agent-docs/systems/focus-vignette.md` — living doc for the system.
- Memory: `pca-mono-camera-stereo-comfort` — the mono-camera comfort constraint.

---

## 4. How it works (architecture)

1. **Geometry:** a large inverted sphere centered on the head (translation only — never rotated, so the focus stays world-locked).
2. **Camera UV:** each fragment's **world direction** (`worldPos − headCenter`) is projected through a head-centered pinhole (`_HeadRight/_HeadUp/_HeadForward` + `_TanHalfFov`, fed per-frame). Because world direction is **eye-independent**, both eyes sample the same camera pixel for the same point → the mono image **fuses**.
3. **Focus mask:** `effect = smoothstep(_InnerAngle, _OuterAngle, angleFromFocusDir)` → 0 in the focus cone, 1 in the periphery. Drives blur (9-tap box) + desaturation.
4. **Salience:** `FocusSalienceDetector` runs YOLO on camera frames, keeps only the configured classes, and feeds up to 16 normalized boxes (`_SalienceBoxes`/`_SalienceCount`). In those regions the shader does `effect *= (1 - salience)` (drops blur/desat) and applies a brightness/contrast boost.

---

## 5. Problems hit & fixes

| Problem | Cause | Fix |
|---|---|---|
| **Build failed** — `IOException: RuntimeActionBindings.json already exists` | (a) Build target was **StandaloneWindows64**, not Android; (b) stale file from a prior build left in the output folder, crashing the Meta XR post-process copy | Switched target to **Android**; deleted the stale file. Recommend building to a **non-OneDrive** folder (OneDrive sync locks files mid-build). |
| **Couldn't use controllers** in the menu | Controllers `CONNECTED_INACTIVE` (asleep / hand-tracking active); no laser | Wake controllers; **also made FocusVignette boot directly** so no menu navigation is needed. |
| **Black screenshots** via metacam | The app holds the passthrough camera, so Meta's camera-based screenshot service returns black | Use the **framebuffer path** (`screencap` / `screenrecord`) instead of metacam. |
| **Double vision** (image won't fuse) | Camera sampled by **screen position** (`uv = screenPos`) → same world point lands at different screen spots per eye → each eye samples a different camera pixel | **Sample by world direction** (eye-independent) via head-centered pinhole. *(Implemented; pending on-device verification.)* |
| MCP servers down | hzdb cold-`npx` download exceeded 30s timeout; Coplay needs Unity open | Warm the npx cache; reconnect via `/mcp` (servers attach at session start). |

---

## 6. Approaches tried / considered (and why)

- **Flat panel (like the stock ShaderSample water plane)** — rejected: only covers a small surface, not a full-view focus effect.
- **Color-pop vs. object-highlight** (asked the user) — superseded by the actual concept: focus vignette + peripheral salience.
- **Keep real OS passthrough as base + only overlay highlights** — comfortable, but you **can't dim the OS passthrough layer**, and the vignette needs to dim the periphery → so a full-view camera layer is required.
- **Surface-projected passthrough** (project real passthrough onto a sphere) — rejected: **deprecated at the SDK level as of v83**.
- **World-projected camera via full intrinsics (`WorldToViewportPoint`)** — viable for exact alignment; deferred in favor of a simpler tunable-FOV pinhole for now.
- **Screen-space camera sampling** — implemented first, but it **caused the double vision**; replaced (see §5).
- **Example effect shaders (edge-detect / thermal)** — illustrative only; not used.

---

## 7. Current status

- ✅ Code compiles clean; scene wired; Android build boots **directly into FocusVignette**.
- ✅ Effect confirmed **running on device** earlier (screencap showed sharp focus + blurred/desaturated periphery, "Permission granted").
- ✅ YOLO salience implemented (detection-only; no spatial anchors / env raycast).
- ⏳ **Double-vision fix** (world-direction UV) implemented but **not yet verified on device** (headset was unplugged at time of writing).
- ⏳ **Salience** not yet verified on device (alignment is approximate).

---

## 8. Open items / next steps

1. ✅ **Fusion fix verified on device** (world-direction sampling — user confirmed it fuses).
2. ✅ **Zoom + sharpness:** FOV now derived from **camera intrinsics** (`SensorResolution/(2·FocalLength)`) instead of the 82° guess → natural 1:1 scale.
3. ✅ **Slowness:** detector throttled via `m_detectionInterval` (default 0.15s ≈ 6–7 Hz).
4. ⏳ **Re-test on device** after the above (zoom natural? framerate smooth? salience aligned?). Tune `_FlipY` / `_SalienceFlipY` if image or boxes are vertically off.
5. ⏳ **Salience tuning:** `m_scoreThreshold`, `_SalienceFeather` / `_HighlightBrightness` / `_HighlightContrast`, `m_highlightClasses`.
6. **Detector backend:** CPU (default) vs `GPUCompute` if detection still feels laggy.
7. **Build hygiene:** build to a local non-OneDrive output folder.
8. **Quality ceiling:** capped by the mono 1280×960 camera; can try a wider supported resolution, but it won't match real passthrough.

---

## 9. Tuning reference

**`FocusVignetteMat` (shader properties)**
- `_InnerAngle` / `_OuterAngle` (radians) — focus cone size & falloff width.
- `_BlurRadius`, `_Desat` — peripheral blur strength & desaturation.
- `_FlipY` — flip camera image vertically.
- `_SalienceFeather`, `_HighlightBrightness`, `_HighlightContrast`, `_SalienceFlipY` — object-pop tuning.

**`FocusVignetteManager`**
- `m_headAnchor`, `m_allowRecenter` (A / index pinch re-aims focus), `m_cameraHorizontalFovDeg`.

**`FocusSalienceDetector`**
- `m_backend`, `m_iouThreshold`, `m_scoreThreshold`, `m_highlightClasses`
  (default: person, bicycle, car, motorbike, bus, truck, traffic light, stop sign, dog, cat).

---

## 10. Repro / run notes

- Open `FocusVignette.unity` in Unity (Coplay connected) → **Build And Run** (Android) to the Quest 3.
- First launch prompts for camera permission (one unavoidable tap; remembered after).
- For capture/logs use the framebuffer path (`screencap`/`screenrecord`) — metacam is black while the app holds the camera.
- The headset must be physically connected & **USB-debugging authorized** for hzdb device tools.
