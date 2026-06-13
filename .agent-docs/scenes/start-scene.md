# Scene: StartScene

Last updated: 2026-06-03

- **Path:** `Assets/PassthroughCameraApiSamples/StartScene/StartScene.unity`
- **Role:** Main runtime entry point (Build index 0). Menu hub for launching all samples.

## Purpose

Presents a world-space debug-UI menu listing every sample scene. Preloads the YOLO model so the
MultiObjectDetection sample starts faster. Provides controller/hand laser-pointer interaction.

## Key GameObjects & scripts

- `StartMenu` — preloads the object-detection model and builds menu buttons from Build Settings.
- `DebugUIBuilder` (singleton) — constructs the menu panel from `Debug*` prefabs.
- `HandedInputSelector` — points the `OVRInputModule` at the active hand/controller.
- `LaserPointer` — renders the pointer ray + cursor (extends `OVRCursor`).
- OVR rig (`OVRCameraRig`, `OVROverlay`) for tracking and passthrough background.

## Prefabs used

- `CanvasWithDebug`, `UIHelpers`, `DebugUITestCanvas`
- `Debug*` element prefabs: `DebugButton`, `DebugLabel`, `DebugSlider`, `DebugToggle`,
  `DebugRadio`, `DebugTextField`, `DebugDivider`

## Transitions

- **Loads:** any sample scene (by build index) when its menu button is pressed.
- **Loaded by:** app launch (first scene).

## Classification

Main runtime flow — required hub scene.

Related: [Start Menu & Debug UI system](<../systems/start-menu-ui.md>), [Input](<../systems/input.md>).
