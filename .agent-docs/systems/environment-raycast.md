# System: Environment Raycast

Last updated: 2026-06-03

Thin wrapper over MRUK's depth-based `EnvironmentRaycastManager`, used to turn 2D detection boxes into
3D world positions. Lives in `MultiObjectDetection/EnvironmentRaycast/`.

## Key script

`EnvironmentRayCastSampleManager`

- `HasScenePermission()` — checks the Android Scene permission required for depth.
- `Raycast(Ray)` — casts against the environment mesh, returns the hit (used to place markers/boxes).
- Serialized `m_raycastManager` (`EnvironmentRaycastManager` from MRUK).

## Used by

- `SentisInferenceUiManager` (place bounding boxes in 3D).
- `DetectionManager` (anchor markers on surfaces).
- `CameraSphereVignetteManager` (dissertation build, added 2026-07-16) — raycasts the 4 corners
  of a just-painted Hard Dark selection against live depth to world-lock the focus window to a
  real object instead of a head-relative bearing. Uses `Meta.XR.EnvironmentRaycastManager`
  directly (no dependency on `EnvironmentRayCastSampleManager`). See
  [Focus Vignette § World anchor](<focus-vignette.md#world-anchor-hard-dark-real-object-lock>).

## Prefab

`EnvironmentRaycastPrefab`.

## Requirements

Scene permission (granted alongside camera permission in `RequestPermissionsOnce`).

Related: [Object Detection](object-detection.md), [Sentis Inference](sentis-inference.md).
