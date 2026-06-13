# System: Start Menu & Debug UI

Last updated: 2026-06-03

The menu and the reusable world-space debug-UI toolkit, both living in `StartScene/`.

## Key scripts

- `StartMenu` — on `Awake()` preloads the YOLO `ModelAsset` (via `SentisInferenceRunManager.PreloadModel`)
  and builds a button per Build Settings scene (skipping StartScene), grouped by category. `LoadScene(int)`
  loads the chosen scene by build index.
- `DebugUIBuilder` — singleton UI factory. Public builders: `AddButton`, `AddLabel`, `AddSlider`,
  `AddToggle`, `AddRadio`, `AddDivider`, `AddTextField`; plus `Show()`, `Hide()`,
  `ToggleLaserPointer(bool)`. Layout knobs: `IsHorizontal`, `ElementSpacing`, `MarginH`, `MarginV`.
- `HandedInputSelector`, `LaserPointer`, `ReturnToStartScene` — see [Input](input.md).

## Prefabs

`DebugUIBuilder` instantiates the `Debug*` element prefabs. See
[Debug UI prefabs](<../prefabs/debug-ui.md>).

## Dependencies

- `OVRCameraRig`, `OVRRaycaster`, `OVRInputModule`, `OVROverlay` (Meta XR / OVR).
- `SentisInferenceRunManager` (for model preload only).

Related scene: [StartScene](<../scenes/start-scene.md>).
