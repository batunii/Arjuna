# Unity-PassthroughCameraApiSamples — Agent Documentation

> Auto-generated project knowledge base for AI agent comprehension.
> Last updated: 2026-07-20

## Quick Context

Meta's official sample project demonstrating the **Passthrough Camera API (PCA)** on Quest 3 / 3S.
It shows how to read the headset's RGB cameras at runtime via the `PassthroughCameraAccess`
component (shipped in the Meta MR Utility Kit package) and use the frames for display, 2D→3D
world alignment, brightness estimation, ML object detection (Unity Inference Engine / Sentis),
and custom shader effects. A `StartScene` menu launches each sample.

**The repo now also hosts a second, separate build**: an MSc dissertation Diminished-Reality
attention-guidance study (`CameraSphereVignette` + `VideoTestScene` scenes, no `StartScene`) — see
[Scene Flow § Dissertation study build](<scenes/_flow.md>#dissertation-study-build),
[Focus Vignette](<systems/focus-vignette.md>), [Study Tooling](<systems/study-tooling.md>). Current
work branch: `Test/AuthoredStudyRunner` (authored-pool driving harness — see Study Tooling § Layer 3
and `Dissertation/authored/HANDOFF.md`); check `git status`/`git log` before assuming anything is
committed.

## Document Map

- [Project Overview](project-overview.md)
- Scenes
  - [Scene Flow](<scenes/_flow.md>)
  - [StartScene](<scenes/start-scene.md>)
  - [CameraViewer](<scenes/camera-viewer.md>)
  - [CameraToWorld](<scenes/camera-to-world.md>)
  - [BrightnessEstimation](<scenes/brightness-estimation.md>)
  - [MultiObjectDetection](<scenes/multi-object-detection.md>)
  - [ShaderSample](<scenes/shader-sample.md>)
  - [VideoTestScene](<scenes/video-test-scene.md>) *(dissertation build)*
  - [CameraSphereVignette](<scenes/camera-sphere-vignette.md>) *(dissertation build)*
- Systems
  - [Passthrough Camera Access (core)](<systems/passthrough-camera-access.md>)
  - [Input](<systems/input.md>)
  - [Start Menu & Debug UI](<systems/start-menu-ui.md>)
  - [Object Detection](<systems/object-detection.md>)
  - [Sentis Inference](<systems/sentis-inference.md>)
  - [Environment Raycast](<systems/environment-raycast.md>)
  - [Focus Vignette (custom, dissertation)](<systems/focus-vignette.md>)
  - [Video Test Scene (dissertation)](<systems/video-test-scene.md>)
  - [Study Tooling (dissertation)](<systems/study-tooling.md>)
  - [Distractor Content Pipeline (dissertation, exploratory)](<systems/distractor-content-pipeline.md>)
- Prefabs
  - [Sample Manager Prefabs](<prefabs/sample-managers.md>)
  - [StartScene Debug UI Prefabs](<prefabs/debug-ui.md>)
- Assets
  - [Sentis YOLO Model](<assets/sentis-model.md>)
- Scripts
  - [Scripts Reference](<scripts/reference.md>)

## Runtime Flow

**Sample suite build:** App launches into **StartScene** (build index 0). `StartMenu` preloads the
YOLO model and builds a debug-UI menu listing every other scene from Build Settings. Selecting an
entry loads that sample scene. On the first non-StartScene load, `RequestPermissionsOnce` requests
the `Scene` and `PassthroughCameraAccess` permissions. Each sample scene contains a
`PassthroughCameraAccessPrefab` plus its own manager prefab that consumes the camera texture/pose.
`ReturnToStartScene` (Start button) returns to the menu.

**Dissertation study build:** boots straight into `VideoTestScene`; switches to/from
`CameraSphereVignette` via a full scene reload (hold left `Y`, key `V`, or the informal
`TestModeSequencer` preview harness). See [Scene Flow § Dissertation study build](<scenes/_flow.md>#dissertation-study-build).
