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
