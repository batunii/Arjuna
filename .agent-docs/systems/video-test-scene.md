# System: Video Test Scene (VideoTestSceneManager)

Last updated: 2026-07-14

The video-playback counterpart to [CameraSphereVignetteManager](<focus-vignette.md>) — same
`VignetteMode` enum, same shader (`CameraSphereVignette.shader`), same
[`IStudyVignetteControl`](<focus-vignette.md>#study-api-istudyvignettecontrol) API, but the
periphery is a looping 360° **video** instead of live passthrough cameras. Used for Blocks B/C of
the dissertation study and for `Test/FinalCountDown`'s driving-sim preview modes.

File: `Assets/PassthroughCameraApiSamples/ShaderSample/Scripts/VideoTestSceneManager.cs`.
Scene: `Assets/VideoTestScene.unity`, GameObject `CameraSphereSphere`.

## Video source resolution

Prefers a sideloaded file over the bundled one (keeps large clips out of the APK):

- Normal mode: `persistentDataPath/study_video.mp4`, else `StreamingAssets/DebugVideo.mp4`
- Flat-clip mode (`m_flatClipMode`): `persistentDataPath/flat_clip.mp4`, else `StreamingAssets/FlatClip.mp4`
- `m_videoFileNameOverride` (string, empty by default): if set, replaces the **StreamingAssets**
  fallback filename only — the persistentDataPath override still takes priority under the same
  name. Lets `TestModeSequencer`-style tools point at a specific bundled clip from the Inspector
  without touching the hardcoded defaults.

Baked detection track: `persistentDataPath/study_video.detections.json`, else
`StreamingAssets/DebugVideo.detections.json`, else falls back to live `YoloRunner` inference
(`m_useBakedDetections`, default true). Produced by `Tools/bake_detections.py` — see that
script's docstring for the tiling/priority-class/person-gate details. **`Builds/` is gitignored**
— a corrected/rebaked detections JSON has no git history; if you clean false positives out of it,
back up the removed-entries list and a current snapshot somewhere NOT gitignored (see
`Tools/detections_backups/` for the convention) before it's lost, and re-push to the device with
`adb push` — the file on-device does not auto-update just because the local one changed.

## SignPop (detection-gated ColorPop)

`VignetteMode.SignPop` (enum value 10) — video-only. Same periphery treatment as `ColorPop`, but
only pixels inside an **actual detected** traffic light/stop sign box get the full "pop" color
boost; other red/orange/green-ish pixels (neon signs, ads, brake lights) fall back to
`m_signRogFallback` (0–1, default 0.35) dimming instead of full grey — a "graceful miss" so a bake
gap dims a real signal instead of hiding it. `m_signDetHoldSec` (default 0.5s) holds a detection
after it vanishes from the bake so pops don't blink between samples.

**Lifetime-aware blip filter (`m_detectionMinAgeSec`, default 0.6s — reworked 2026-07-18):** at
load, `BuildRuntimeLifetimeSpans()` walks the baked track once (same identity heuristic as the
live tracker) and stamps every entry with its object's full `(tStart, tEnd)` lifetime
(`m_entryLifeSpan`). A track is visible only if that full lifetime ≥ `m_detectionMinAgeSec` —
decided the INSTANT the object first appears (the future is known offline), so real detections
have zero onset delay while single-sample YOLO blips (62% of the pre-scrub bake, 408/654
traffic-light lifetimes) never appear. This replaced two earlier attempts: a real-time wait
debounce (every window formed 0.6s late) and a lead-time lookup shift (`m_detectionLeadSec`,
now REMOVED — it drew boxes at future positions, visibly ahead of moving objects). The hold is
also skipped once a track's known lifetime end passes, so windows close on just the fade-out.
`CameraSphereVignetteManager` keeps a real-time wait debounce for its live-YOLO tracker (default
0.7 — the future genuinely isn't known there; must exceed its `m_detectionLifetime` 0.6).
Timing is fully Inspector-tunable under the **"Detection Timing"** header:
`m_detectionOnsetDelaySec` (open the window this long after the object appears, default 0 =
instant) and `m_detectionHoldPastEndSec` (keep it open this long after the lifetime ends, box
frozen, default 0) — both act on the visibility envelope only, never on box positions, so they
cannot reintroduce the position-lead artefact.

**Detection-window grading (added 2026-07-18):** `_DetectionSatLift` / `_DetectionBrightLift` /
`_DetectionContrast` replace the shader's hardcoded 0.5/0.12 saturation/brightness lift and add
mid-grey contrast expansion, all scaled by `_DetectionEnhance` + the breathing pulse. Shader
property defaults keep the legacy look; `VideoTestSceneManager` exposes them in the Inspector
with raised defaults (0.85 / 0.18 / 0.2) for a more vivid detected region.

**Persons are exempt from the size gate only** — the lifetime filter applies to them too, and
since visibility is decided from the precomputed lifetime, person windows open right when the
person appears. **The person gate now tests the *effective* window**
(`m_effectiveRect`): the painted rect when one exists, else the pop-mode gaze auto-follow rect —
previously the gaze rect only reached the shader, so with nothing painted `PassesPersonGate`
failed closed and persons were **never** highlighted in free-play/`TestModeSequencer` SignPop
(the root cause of "blobs form on humans but the humans aren't highlighted"; the blob pool also
now applies the gate's ≥ 6° box-height "close" criterion at selection —
`BlobTargetController.ComputeBoxHeightDeg`). Note `m_detectionLifetime` is live-YOLO-only and
inert in the baked path (slot timestamps refresh every frame); persistence is tuned via hold /
fades / min-age / lead. Person windows also get a **graded strength** (`PersonRegionScale`,
multiplied into the per-slot `_DetectionFade`): floor `m_personInsideScale` (0.25) at the window
**centre**, ramping linearly (rect-normalized centre→edge distance) to full at the window edge,
full outside until `m_personFullStrengthDeg` (12.5°, exposed 2026-07-18 — was hardcoded at half
the reach), then tapering to zero by `m_personMaxEdgeDistDeg` outside (code default 25°; the
scene currently serializes **20°** after in-editor tuning)
(**25°**, raised 2026-07-18 from the original 8° — at 8° the gate zeroed 77% of close peripheral
persons in the study bake, which read as "people never highlighted"; 25° covers ~52%). The person
concept is deliberately **near-region** ("about to enter / leaving / standing near the selected
region"), not all-periphery. Blob person selection is aligned to this: candidates must have
predicted strength ≥ `m_personMinPredictedStrength` (0.7) via the public
`VideoTestSceneManager.PersonPredictedStrength` at the lifetime midpoint (window must be locked
before `Activate` — `TestModeSequencer` does). `StudySetWindow`/`StudyClearWindow` are
null-guarded (scene-switch entries call them before `Start()` creates the material; `Start()`
re-pushes `m_activeRect`). Note: the shader's `_DetectionOutsideScale` still damps the
*highlight* component for out-of-window detections; the carve-out follows the graded profile at
full strength.

**Data-scrub history (2026-07-18):** beyond the relevance triage, all 43 two-sample "flasher"
traffic-light lifetimes (86 raw entries) were removed from `study_video.detections.json` after a
full visual contact-sheet review found zero persistent false positives among the ≥3-sample
lifetimes — every surviving rendered lifetime is a human-verified real traffic light. Person
entries untouched (10,987 before and after). Snapshot/sidecar/log in `Tools/detections_backups/`
and `Builds/StudyVideo/study_video.detections.flashers_removed_2026-07-18.json`.

**Hunting the surviving persistent false positives:** `Tools/triage_traffic_lights.py preview`
renders one annotated MP4 of the whole video (boxes lerped exactly like the runtime tracker,
lifetime IDs burned in, debounce-suppressed blips drawn dim grey) plus an `apply`-compatible
cache where every lifetime defaults to *keep* — scrub it on desktop, note the IDs circling
nothing, name only those in a decisions file, `apply` removes them. Replaces recording the
headset and eyeballing frame dumps.

Only 4 modes are live in the free-play A-button cycle (`k_modeCycle`): ColorPop, SignPop,
SoftDark, HardDark — the study proper only uses ColorPop/SoftDark/HardDark; SignPop is a
free-play/demo mode (and now also used by `TestModeSequencer`'s mode 1).

## Detection lifetime reconstruction

Two public methods added for `Study/BlobTargetController.cs` (see
[study-tooling.md](<study-tooling.md>#blobtargetcontrollercs)) — reusable by anything else that
needs the full picture of the baked track rather than the rolling live view:

- `BuildDetectionLifetimes(HashSet<int> classIds, float holdSeconds)` — walks the **entire**
  baked track once (not tied to the live playhead — contrast with `m_trackedDets`, the
  frame-by-frame rolling tracker `UpdateBakedDetections`/`UpsertTracked`/`FindMatch` use for
  drawing boxes during playback) and returns every `DetectionLifetime { cls, tStart, tEnd,
  samples }` — one per real-world object, using the same identity heuristic (same class, nearest
  centre within a radius scaled to box size).
- `DetectionBoxToAzElRect(Vector4 box)` — public wrapper around the existing
  `BoxToAzElRectEQ(box, Vector2Int.one)` conversion, so anything placing a marker at a baked box's
  position lines up exactly with where the real drawn box would be.

## Other Study API additions

- `SetVideoPlaying(bool)` — play/pause the primary `VideoPlayer` (no-op on the flat-clip surround
  player). Added for `TestModeSequencer`'s X-toggle.
- `StudySetActive(bool)` (from `IStudyVignetteControl`) — see
  [focus-vignette.md](<focus-vignette.md>#study-api-istudyvignettecontrol).

## Known constraint

`UpdateDetectionUniforms()` now gates `_DetectionCount` on `StudyEffectSuppressed` — see the "bugs
fixed" note in [study-tooling.md](<study-tooling.md>#bugs-fixed-while-building-this-apply-beyond-the-test-branch).

Related: [Focus Vignette](<focus-vignette.md>), [Study Tooling](<study-tooling.md>),
[scene](<../scenes/video-test-scene.md>).
