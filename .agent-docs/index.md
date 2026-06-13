# Unity-PassthroughCameraApiSamples — Agent Documentation

> Auto-generated project knowledge base for AI agent comprehension.
> Last updated: 2026-06-03

## Quick Context

Meta's official sample project demonstrating the **Passthrough Camera API (PCA)** on Quest 3 / 3S.
It shows how to read the headset's RGB cameras at runtime via the `PassthroughCameraAccess`
component (shipped in the Meta MR Utility Kit package) and use the frames for display, 2D→3D
world alignment, brightness estimation, ML object detection (Unity Inference Engine / Sentis),
and custom shader effects. A `StartScene` menu launches each sample.

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
- Systems
  - [Passthrough Camera Access (core)](<systems/passthrough-camera-access.md>)
  - [Input](<systems/input.md>)
  - [Start Menu & Debug UI](<systems/start-menu-ui.md>)
  - [Object Detection](<systems/object-detection.md>)
  - [Sentis Inference](<systems/sentis-inference.md>)
  - [Environment Raycast](<systems/environment-raycast.md>)
  - [Focus Vignette (custom)](<systems/focus-vignette.md>)
- Prefabs
  - [Sample Manager Prefabs](<prefabs/sample-managers.md>)
  - [StartScene Debug UI Prefabs](<prefabs/debug-ui.md>)
- Assets
  - [Sentis YOLO Model](<assets/sentis-model.md>)
- Scripts
  - [Scripts Reference](<scripts/reference.md>)

## Runtime Flow

App launches into **StartScene** (build index 0). `StartMenu` preloads the YOLO model and builds
a debug-UI menu listing every other scene from Build Settings. Selecting an entry loads that sample
scene. On the first non-StartScene load, `RequestPermissionsOnce` requests the `Scene` and
`PassthroughCameraAccess` permissions. Each sample scene contains a `PassthroughCameraAccessPrefab`
plus its own manager prefab that consumes the camera texture/pose. `ReturnToStartScene` (Start button)
returns to the menu.
