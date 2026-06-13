# Scene: MultiObjectDetection

Last updated: 2026-06-03

- **Path:** `Assets/PassthroughCameraApiSamples/MultiObjectDetection/MultiObjectDetection.unity`
- **Role:** Most complex sample — runs YOLOv9 object detection on camera frames (Unity Inference
  Engine / Sentis) and anchors 3D markers on detected real-world objects.

## Purpose

Feeds passthrough frames to a Sentis model, applies non-max suppression, draws 2D bounding boxes,
raycasts each box into the environment depth mesh to find a 3D position, and spawns a labeled,
camera-facing marker (optionally backed by an `OVRSpatialAnchor`).

## Sub-systems (folders)

- `DetectionManager/` — spawns/cleans markers, owns object lifecycle, UI menu state.
- `SentisInference/` — model run + bounding-box UI; also an editor ONNX→Sentis converter.
- `EnvironmentRaycast/` — wraps `EnvironmentRaycastManager` (depth) for 2D→3D placement.

## Key scripts

- `DetectionManager`, `DetectionSpawnMarkerAnim`, `DetectionUiMenuManager`, `DetectionUiTextWritter`,
  `DetectionUiBlinkText`
- `SentisInferenceRunManager`, `SentisInferenceUiManager`
- `EnvironmentRayCastSampleManager`

## Prefabs used

- `DetectionManagerPrefab`, `DetectionSpawnMarker`, `DetectionUiMenuPrefab`
- `SentisInferenceManagerPrefab`, `EnvironmentRaycastPrefab`
- `PassthroughCameraAccessPrefab` (shared)

## Assets

- YOLO model + labels (see [Sentis Model](<../assets/sentis-model.md>)).

## Requirements

Needs both `PassthroughCameraAccess` and **Scene** permission (for depth raycasting). The YOLO model is
preloaded back in StartScene by `StartMenu`.

## Transitions

- **Loaded by:** StartScene menu. **Returns to:** StartScene (Start button).

## Classification

Sample / example scene.

Related: [Object Detection](<../systems/object-detection.md>), [Sentis Inference](<../systems/sentis-inference.md>),
[Environment Raycast](<../systems/environment-raycast.md>).
