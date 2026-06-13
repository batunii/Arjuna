# Scene: CameraToWorld

Last updated: 2026-06-03

- **Path:** `Assets/PassthroughCameraApiSamples/CameraToWorld/CameraToWorld.unity`
- **Role:** Demonstrates aligning the RGB camera pose with passthrough and converting 2D camera
  pixels into 3D world-space rays.

## Purpose

Visualizes the camera pose and projects rays from the four viewport corners into the world. Lets the
user freeze a camera snapshot and see how 2D image points map to 3D directions. Core demo of
`GetCameraPose()` + `ViewportPointToRay()`.

## Key GameObjects & scripts

- `CameraToWorldManager` — orchestrates snapshots, pose markers, and corner-ray rendering.
- `CameraToWorldCameraCanvas` — shows live feed or a frozen snapshot on a `RawImage`
  (`MakeCameraSnapshot()` / `ResumeStreamingFromCamera()`).
- `CameraToWorldRayRenderer` — holds the debug ray segment GameObjects.
- Input: `InputManager` (A button / index pinch to snapshot).

## Prefabs used

- `CameraToWorldCameraCanvas`, `CameraToWorldButtonA_Highlight`
- `PassthroughCameraAccessPrefab` (shared)

## Transitions

- **Loaded by:** StartScene menu. **Returns to:** StartScene (Start button).

## Classification

Sample / example scene.

Related: [Passthrough Camera Access](<../systems/passthrough-camera-access.md>), [Input](<../systems/input.md>).
