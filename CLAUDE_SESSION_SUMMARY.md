# Claude Session Summary — Diminished Reality Attention-Guidance Dissertation

> Last updated: 2026-07-03. Branch: `fearure/TestVideo1`.
> Companion doc: `.agent-docs/systems/focus-vignette.md` (full technical detail).
> Dissertation plan: `Dissertation/dissertation_progress_report.md`.
>
> ⚠️ The mode tables below (3 modes) are stale — there are now 8 modes, plus a
> **video test scene** (`Assets/VideoTestScene.unity` + `VideoTestSceneManager.cs`) that replaces
> passthrough with an equirect video sphere (`StreamingAssets/DebugVideo.mp4`) so modes can be
> evaluated without live passthrough. Current active work happens there. See the
> **Change log** at the bottom for what's been done since.

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

---

## Change log

### 2026-07-03 — ColorPop rework: four-level ROG hierarchy + headlight glare suppression

Context: after on-headset testing, ColorPop was the standout mode; YOLO pre-baked detections
were missing many traffic lights (root cause: baking runs on a 640×360 downsample — small lights
are below detection size; not pursued further, ColorPop needs no detector). All changes target the
**video pipeline** (`VideoTestScene`); the passthrough scene is deliberately untouched.

Files: `Shaders/CameraSphereVignette.shader`, `Scripts/VideoTestSceneManager.cs`.
Research grounding: `.agent-docs/research/attention-guidance-research.md` — the inside/outside
brightness split puts ROG separation on the luminance channel (strongest per Bailey 2009 /
Grogorick 2017); no hue shifts (Sutton 2022); glare is attenuated, never removed (Cheng 2022,
McLaughlin 2025). Possible refinement: sigmoidal contrast (Sutton's α=10, β=0.5) instead of the
linear sat boost if it looks harsh on device. YOLO recall fix path, if ever revisited: Part 5 of
the research doc (offline full-res baking / YOLO11n / FP16 / ROI second pass).

1. **Four-level ROG (red/orange/green) hierarchy** replacing the old two-level periphery-only pop:
   `outside-other < outside-ROG < inside-other < inside-ROG`.
   - ROG membership extracted into a `PopColorKeep()` shader helper (hue bands: warm 0–0.18 + red
     wrap 0.85+ + green 0.22–0.48, saturation-gated).
   - "Other" track: near-natural inside the window (slight desat, `_PopInsideDesat` 0.25) → dim
     grey outside (`_PopGreyDim` 0.4).
   - ROG track: sat boost ×1.7 + brightness ×1.15 inside (`_PopSatBoost`, `_PopBrightIn`) →
     natural colour dimmed to ×0.8 outside (`_PopBrightOut`) — visible everywhere, privileged inside.
   - In **video mode** ColorPop now grades the **whole sphere** (window included):
     `periAlpha = lerp(1, t, _PassthroughMode) * strength`. In passthrough mode it stays
     periphery-only automatically (window must remain transparent OS passthrough).
2. **Headlight glare suppression**: pixels that are bright (`luma > _PopGlareKnee` 0.6) AND
   unsaturated AND not ROG are compressed toward the knee — a visible dim spot, never black (the
   oncoming car stays legible; only its veiling glare goes). Strength is `_PopGlareInside` 0.35
   inside the window vs `_PopGlareOutside` 0.85 in the periphery.
   - **Blown-core guard**: white clipped centers of bright signal lamps are exempt when the local
     surround (Gaussian mean, `_PopGuardRadius` 0.008 UV) is a kept colour — protects traffic-light
     cores from being dimmed as glare. Set radius to 0 to disable.
3. **Warm-band saturation floor** (`_PopWarmSatMin` 0.35): warm-white headlights (hue ~0.1,
   sat 0.1–0.25) can no longer pass as "orange"; real lamps/signs sit at sat > 0.5.
4. **More gradual selection edge for ColorPop**: mode-specific `m_popSoftEdgeDeg` (32°, vs global
   20°) — a colour/grey boundary reads harsher than a blur boundary. Applied in
   `UpdateFilterUniforms()`; other modes unchanged.

All new parameters are Inspector sliders under "Color Pop" on `VideoTestSceneManager` — tune on
device. Shader property defaults reproduce safe behaviour for the passthrough scene (its manager
doesn't set the new uniforms).

Design decision (confirmed in discussion): outside-window ROG keeps its **natural colour slightly
dimmed** (not desaturated/tinted) — preserves detectability of lights everywhere, which fits the
driving/navigation task.

### 2026-07-04 — ColorPop: sigmoidal contrast + overall periphery dim

Same files (`CameraSphereVignette.shader`, `VideoTestSceneManager.cs`).

1. **Sigmoidal midtone contrast on the ROG track** (Sutton UIST 2022 recipe, α=10 β=0.5,
   normalized to keep 0→0 / 1→1): `SigmoidContrast()` shader helper, applied after the linear
   sat boost and before the brightness lift. Strength `_PopSigmoid` / `m_popSigmoid` (default
   0.6), scaled by `(1 − t)` so it's full inside the window and fades to none outside.
2. **Overall periphery dim** `_PopPeriphDim` / `m_popPeriphDim` (default 0.85): multiplies the
   final graded colour by `lerp(1, dim, t)` — the whole unselected region (ROG included) drops
   a notch in plain luminance, so the window wins on the strongest guidance channel. Stacks
   with `_PopBrightOut` (0.8), so outside-ROG net brightness ≈ 0.68 at defaults — raise either
   slider if outside lights get too dark to serve as cues.

### 2026-07-04 — Video scene trimmed to 3 modes: ColorPop → SoftDark → HardDark

`VideoTestSceneManager.cs` only. The A-button cycle is now `k_modeCycle = { ColorPop, SoftDark,
HardDark }` (ColorPop is mode 1/3, default). Removed from the video manager: Blur, TintedDark,
ChromaticCool, ConspicuitySqueeze, GranulatedPeriphery, OutlinedDark, SpotLift — their Inspector
fields, shader-ID statics, per-frame uniform pushes, motion settings, and toast entries. Verified
compiling in Unity (no errors).

Deliberately kept intact:

- The shared `VignetteMode` enum (in `CameraSphereVignetteManager.cs`) and all shader branches —
  the passthrough scene still uses them; the video scene just never selects them.
- `m_frostTex` — the Gaussian helper used by ColorPop's glare-core guard still binds `_FrostTex`.

Robustness: `Start()` snaps any stale serialized mode (e.g. Blur from an old scene save) to
ColorPop, and zeroes the removed modes' material toggles once (`_TintMode`, `_ChromaticCool`,
`_SqueezeMode`, `_GrainMode`, `_OutlineMode`, `_SpotLiftMode`) since they're no longer driven
per-frame. Scene-serialized values for deleted fields are silently dropped by Unity — no scene
edit needed.

Also fixed: the **magenta patch on the floor** was a leftover `SelectionLine` GameObject in
`VideoTestScene.unity` — a LineRenderer with a null material (error-magenta), default 1 m width,
lying at y=0 at the origin. Unused by `VideoTestSceneManager` (the passthrough manager's selection
line, never wired here). Deleted from the scene.

### 2026-07-04 — Offline full-res detection baker (`Tools/bake_detections.py`)

Replaces the 640×360 in-editor bake (the reason baked traffic lights were missing). Runs tiled
YOLO11 inference on the PC over the full 3840×2160 frame — 3×2 overlapping tiles + one full-frame
pass, merged with per-class NMS — and writes the same `DebugVideo.detections.json` schema, so the
scene loads it with zero Unity changes.

- **Run**: `uv run Tools/bake_detections.py` from the repo root — the script carries PEP 723
  inline metadata, so uv resolves ultralytics/numpy automatically, no install step. Add
  `--max-seconds 10` for a smoke test; `--interval 0.5` / `--model yolo11m.pt` / `--imgsz 1280`
  to trade speed vs recall.
- **Bakes from `Assets/StreamingAssets/DebugVideo.mp4`** (the 180 s clip the headset plays), NOT
  `DevVideos/DebugVideo.mp4` — that's the full ~40 min source; the track is keyed by video time,
  so baking from it would misalign every timestamp. To use new segments: cut a clip from
  DevVideos into StreamingAssets, then bake that.
- Boxes are stored top-down-normalized; playback's `m_yoloFlipY = true` (default) is correct.
  Per-sample ordering puts traffic lights/stop signs first, then largest boxes — the runtime keeps
  only the first 8 that pass its class filter, so ordering decides survival.
- Decode path verified (ffmpeg pipe, AV1 4K OK; frame 0 of the clip is genuinely black —
  fade-in, not a decode bug).
- **Full bake run 2026-07-04** (~30 min on CPU): 720 samples / 180 s, **9,170 traffic-light +
  stop-sign detections; 97% of samples have ≥1** (old 640×360 bake was missing most). Median 12
  lights/signs per sample — well above the runtime's 8-slot shader cap (`k_maxDetections`), so
  the priority-then-size ordering decides what shows; bump the shader array if 8 feels sparse.
- After baking, keep `BakeOnPlay` **disabled** on `VideoDetectionBaker` or the old low-res bake
  will overwrite the JSON on next editor Play.
