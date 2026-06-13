# System: Object Detection

Last updated: 2026-06-03

Lifecycle and presentation layer of the MultiObjectDetection sample. Turns inference results into
anchored 3D markers and drives the sample's UI state. Lives in
`MultiObjectDetection/DetectionManager/`.

## Key scripts

- `DetectionManager` — orchestrator. Spawns 3D markers for currently detected objects
  (`SpawnCurrentDetectedObjects`), cleans them up (`CleanMarkers`), and manages `OVRSpatialAnchor`s.
  Raises `OnObjectsIdentified` (`UnityEvent<int>`). Depends on `PassthroughCameraAccess`,
  `DetectionSpawnMarkerAnim`, `SentisInferenceUiManager`.
- `DetectionSpawnMarkerAnim` — the marker behaviour: rotates the model, faces the camera
  (`OVRCameraRig`), shows the YOLO class via a `TextMesh`. `SetYoloClassName` / `GetYoloClassName`.
- `DetectionUiMenuManager` — UI state machine: no-permission → menu → gameplay; pause handling
  (`IsInputActive`, `IsPaused`, `OnPause`); detection/identification counters
  (`OnObjectsDetected`, `OnObjectsIndentified`). Uses `OVRPermissionsRequester`, `InputManager`.
- `DetectionUiTextWritter` — typewriter text reveal; `OnStartWritting` / `OnFinishWritting` events.
- `DetectionUiBlinkText` — blinks a UI `Text` by toggling alpha.

## Data flow

`SentisInferenceRunManager` runs the model → `SentisInferenceUiManager` draws boxes + raycasts to 3D →
`DetectionManager` spawns/anchors markers and updates `DetectionUiMenuManager` counters.

## Prefabs

`DetectionManagerPrefab`, `DetectionSpawnMarker`, `DetectionUiMenuPrefab`.

Related: [Sentis Inference](sentis-inference.md), [Environment Raycast](environment-raycast.md),
[scene](<../scenes/multi-object-detection.md>).
