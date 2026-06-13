# System: Passthrough Camera Access (core)

Last updated: 2026-06-03

The central abstraction shared by every sample. Provides runtime access to the Quest RGB cameras.

## What it is

`PassthroughCameraAccess` is a MonoBehaviour from the **Meta MR Utility Kit** package
(`com.meta.xr.mrutilitykit`, namespace `Meta.XR`) — it is **not** defined in this repo. In each sample
it is instantiated from the shared prefab `PassthroughCamera/Prefabs/PassthroughCameraAccessPrefab.prefab`
and wired into the sample's manager via a serialized field.

## How samples use it

Common usage pattern:

1. Hold a `[SerializeField] PassthroughCameraAccess m_cameraAccess;`.
2. Coroutine-wait until `m_cameraAccess.IsPlaying`.
3. Read frames/metadata.

Members observed across samples:

| Member | Used by | Purpose |
|---|---|---|
| `IsPlaying` | all | True once the camera stream is live |
| `GetTexture()` | CameraViewer, CameraToWorld, ShaderSample, Sentis | Current frame as a `Texture` |
| `GetColors()` | BrightnessEstimation, CameraToWorld | Raw pixel color array for CPU processing |
| `GetCameraPose()` | CameraToWorld, Sentis | World pose of the camera at capture time |
| `ViewportPointToRay()` | CameraToWorld, Sentis UI | 2D viewport point → 3D world ray |
| `CurrentResolution` | most | Active capture resolution |
| `GetSupportedResolutions(CameraPositionType)` | CameraViewer | Enumerate available resolutions (Left/Right) |

## Helper scripts in this folder (`PassthroughCamera/Scripts/`)

- `RequestPermissionsOnce` — requests `Scene` + `PassthroughCameraAccess` permissions once, on first
  non-StartScene load.
- `PassthroughCameraDebugger` — static conditional logger gated by a `DebugLevel` enum
  (`DebugMessage(LogType, string)`).
- `InputManager` — see [Input](input.md) (lives here but is an input utility).

## Permissions

Requires `horizonos.permission.HEADSET_CAMERA` (declared in `Assets/Plugins/Android/AndroidManifest.xml`)
and the runtime grant flow in `RequestPermissionsOnce`.

Related scenes: all samples.
