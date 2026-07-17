# Scene: VideoTestScene

Last updated: 2026-07-14

- **Path:** `Assets/VideoTestScene.unity`
- **Role:** Dissertation study Blocks B/C — DR vignette modes evaluated against a looping 360°
  video instead of live passthrough (so recall/comparison is possible against a fixed clip). Also
  the driving-sim half of the informal `Test/FinalCountDown` 4-mode preview (modes 1 & 2).
- **Boot scene** for the dissertation study build (index 0) — this is a **separate build
  configuration** from the original sample suite's `StartScene` hub; see
  [Scene Flow](<_flow.md>#dissertation-study-build).

## Key GameObjects & scripts

- `CameraSphereSphere` — `VideoTestSceneManager` (see [system doc](<../systems/video-test-scene.md>)).
  Disables any `OVRPassthroughLayer` in the scene, builds an equirectangular video sphere, streams
  the study clip into a RenderTexture, feeds the vignette shader.
- `StudyRig` — `StudyLogger` + `ConditionSequencer` (plan `VideoScene_BlocksBC`) + `ProbeScheduler`
  + `ClickProbeTest`. Wired via the `Meta > Study > Wire VideoTestScene` Editor menu item, not by
  hand — see [Study Tooling](<../systems/study-tooling.md>).
- `TestModeSequencer` / `BlobTargetController` — **not** present as scene GameObjects. They
  self-bootstrap at runtime (`RuntimeInitializeOnLoadMethod`) — see
  [study-tooling.md](<../systems/study-tooling.md>#layer-2--informal-test-harness-branch-testfinalcountdown--testtestblobs).
- Standard Meta XR building blocks: `[BuildingBlock] Camera Rig`, `[BuildingBlock] Passthrough`.

## Transitions

- **Boots into this scene** (dissertation build). Hold left `Y` (~1s) or key `V` (adb) →
  `CameraSphereVignette` via `SceneSwitcher` (full scene reload — different pipelines).
- `TestModeSequencer` also drives this same transition programmatically when its mode list moves
  between a `VideoScene` mode and a `PassthroughScene` mode.

## Classification

Dissertation study scene (separate build from the sample-suite scenes documented elsewhere in
this doc set).

Related: [Video Test Scene (system)](<../systems/video-test-scene.md>),
[Focus Vignette](<../systems/focus-vignette.md>), [Study Tooling](<../systems/study-tooling.md>),
[CameraSphereVignette (scene)](<camera-sphere-vignette.md>).
