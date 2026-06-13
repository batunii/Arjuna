# Scripts Reference

Last updated: 2026-06-03

Usage-context index of all C# scripts, by feature area. No `.asmdef` exists — everything compiles into
`Assembly-CSharp`. Paths are relative to `Assets/PassthroughCameraApiSamples/`.

## Shared / Core — `PassthroughCamera/Scripts/`

| Script | Purpose |
|---|---|
| `InputManager.cs` | Self-bootstrapping singleton; static `IsButtonADownOrPinchStarted` / `IsButtonBDownOrMiddleFingerPinchStarted`. See [Input](<../systems/input.md>). |
| `RequestPermissionsOnce.cs` | Requests Scene + PassthroughCameraAccess permissions once on first non-StartScene load. |
| `PassthroughCameraDebugger.cs` | Static conditional logger gated by `DebugLevel`. |

> The core `PassthroughCameraAccess` type comes from the MRUK package, not this repo.

## CameraViewer — `CameraViewer/Scripts/`

| Script | Belongs to | Purpose |
|---|---|---|
| `CameraViewerManager.cs` | `CameraViewerManagerPrefab` | Assigns camera texture to a `RawImage`; shows permission status. |

## CameraToWorld — `CameraToWorld/Scripts/`

| Script | Belongs to | Purpose |
|---|---|---|
| `CameraToWorldManager.cs` | scene manager | Snapshots, pose markers, corner-ray rendering. |
| `CameraToWorldCameraCanvas.cs` | `CameraToWorldCameraCanvas` | Live/snapshot feed; `MakeCameraSnapshot` / `ResumeStreamingFromCamera`. |
| `CameraToWorldRayRenderer.cs` | scene manager | Holds debug ray segment GameObjects. |

## BrightnessEstimation — `BrightnessEstimation/Scripts/`

| Script | Belongs to | Purpose |
|---|---|---|
| `BrightnessEstimationManager.cs` | `BrightnessEstimationManagerPrefab` | Samples pixels, averages luminance, fires `m_onBrightnessChange`. |
| `BrightnessEstimationDebugger.cs` | `BrightnessEstimationDebuggerPrefab` | Threshold reactions (`TooDark`/`TooLight`) + debug UI. |

## MultiObjectDetection

### `DetectionManager/Scripts/`

| Script | Purpose |
|---|---|
| `DetectionManager.cs` | Spawns/cleans 3D markers, spatial anchors, object lifecycle; `OnObjectsIdentified`. |
| `DetectionSpawnMarkerAnim.cs` | Marker animation + label; `SetYoloClassName`/`GetYoloClassName`. |
| `DetectionUiMenuManager.cs` | UI state machine, pause, counters; `IsPaused`, `OnPause`. |
| `DetectionUiTextWritter.cs` | Typewriter reveal; `OnStartWritting`/`OnFinishWritting`. |
| `DetectionUiBlinkText.cs` | Blinks a UI `Text`. |

### `SentisInference/Scripts/` + `Editor/`

| Script | Purpose |
|---|---|
| `SentisInferenceRunManager.cs` | Runs YOLO inference + NMS; static `PreloadModel`. |
| `SentisInferenceUiManager.cs` | Draws/places 2D→3D bounding boxes; `SetLabels`, `DrawUIBoxes`. |
| `Editor/SentisModelEditorConverter.cs` | Inspector button: ONNX → `.sentis` conversion. |

### `EnvironmentRaycast/Scripts/`

| Script | Purpose |
|---|---|
| `EnvironmentRayCastSampleManager.cs` | Wraps `EnvironmentRaycastManager`; `HasScenePermission`, `Raycast`. |

## ShaderSample — `ShaderSample/Scripts/`

| Script | Belongs to | Purpose |
|---|---|---|
| `ShaderSampleManager.cs` | `ShaderSampleManagerPrefab` | Feeds camera texture into a material's `_MainTex`. |
| `FocusVignetteManager.cs` | sphere using `FocusVignetteMat` | World-locked focus effect: feeds camera texture, recenters sphere on head, freezes `_FocusDir`. See [Focus Vignette](<../systems/focus-vignette.md>). |

## StartScene — `StartScene/Scripts/`

| Script | Purpose |
|---|---|
| `StartMenu.cs` | Preloads YOLO model; builds menu from Build Settings; `LoadScene(int)`. |
| `DebugUIBuilder.cs` | Singleton UI factory (`AddButton`/`AddSlider`/…, `Show`/`Hide`). |
| `HandedInputSelector.cs` | Selects active hand for `OVRInputModule`. |
| `LaserPointer.cs` | Pointer ray + cursor (extends `OVRCursor`). |
| `ReturnToStartScene.cs` | Start-button → menu; swaps controller/hand tooltips. |
