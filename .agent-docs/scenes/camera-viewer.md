# Scene: CameraViewer

Last updated: 2026-06-03

- **Path:** `Assets/PassthroughCameraApiSamples/CameraViewer/CameraViewer.unity`
- **Role:** Simplest sample — displays the live passthrough camera feed on a 2D canvas.

## Purpose

Shows the minimal path to get a camera texture on screen: wait for `PassthroughCameraAccess.IsPlaying`,
then assign `GetTexture()` to a `RawImage`. Also logs supported resolutions and shows permission status.

## Key GameObjects & scripts

- `CameraViewerManager` — assigns the camera texture to a `RawImage`; updates a permission-status `Text`.
- `PassthroughCameraAccessPrefab` — the core camera provider.

## Prefabs used

- `CameraViewerManagerPrefab`
- `PassthroughCameraAccessPrefab` (shared)

## Transitions

- **Loaded by:** StartScene menu. **Returns to:** StartScene (Start button).

## Classification

Sample / example scene.

Related: [Passthrough Camera Access](<../systems/passthrough-camera-access.md>).
