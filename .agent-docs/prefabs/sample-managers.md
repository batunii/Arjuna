# Prefabs: Sample Managers

Last updated: 2026-06-03

Each sample drops one or more manager prefabs into its scene, plus the shared camera-access prefab.
Naming is self-explanatory; this groups them by sample.

## Shared

- `PassthroughCamera/Prefabs/PassthroughCameraAccessPrefab.prefab` — hosts the `PassthroughCameraAccess`
  component. Dropped into **every** sample scene; manager prefabs reference it via serialized field.

## CameraViewer

- `CameraViewerManagerPrefab` — `CameraViewerManager` + `RawImage` + debug `Text`.

## CameraToWorld

- `CameraToWorldCameraCanvas` — canvas showing live feed / snapshot (`CameraToWorldCameraCanvas`).
- `CameraToWorldButtonA_Highlight` — visual hint for the snapshot (A) button.

## BrightnessEstimation

- `BrightnessEstimationManagerPrefab` — sampling/averaging manager.
- `BrightnessEstimationDebuggerPrefab` — threshold reactions + debug UI.

## MultiObjectDetection

- `DetectionManager/Prefabs/DetectionManagerPrefab` — marker spawning + lifecycle.
- `DetectionManager/Prefabs/DetectionSpawnMarker` — the per-object 3D marker (rotating, camera-facing,
  labeled `TextMesh`).
- `DetectionManager/Prefabs/DetectionUiMenuPrefab` — sample UI / menu state.
- `SentisInference/Prefabs/SentisInferenceManagerPrefab` — model run + bounding-box UI.
- `EnvironmentRaycast/Prefabs/EnvironmentRaycastPrefab` — depth raycast manager wrapper.

## ShaderSample

- `ShaderSampleManagerPrefab` — applies camera texture to a material.

## Configuration note

Manager prefabs expose a serialized `PassthroughCameraAccess` field that must point at the scene's
`PassthroughCameraAccessPrefab` instance. Detection prefabs additionally cross-reference the Sentis and
EnvironmentRaycast managers.

Related systems: see each sample's scene doc under [scenes](<../scenes/_flow.md>).
