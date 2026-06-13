# System: Input

Last updated: 2026-06-03

Unifies controller buttons and hand-tracking pinches into simple static queries.

## Key script

`PassthroughCamera/Scripts/InputManager.cs`

- Self-bootstrapping singleton: a `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` creates a
  `DontDestroyOnLoad` GameObject and attaches itself. No prefab/scene wiring needed.
- Polls `OVRPlugin.GetHandState` each frame for both hands to detect pinch starts.

## Public API

- `InputManager.IsButtonADownOrPinchStarted()` — controller A **or** index-finger pinch start.
- `InputManager.IsButtonBDownOrMiddleFingerPinchStarted()` — controller B **or** middle-finger pinch start.

## Consumers

- `CameraToWorldManager` (snapshot trigger), `DetectionUiMenuManager` (menu interaction).

## Related input pieces

These live in StartScene and handle pointer/menu input rather than the static button queries above:

- `HandedInputSelector` — sets the active hand for `OVRInputModule`.
- `LaserPointer` — pointer ray + cursor visual (extends `OVRCursor`).
- `ReturnToStartScene` — Start-button-to-menu, swapping controller vs. hand tooltips.

Related: [Start Menu & Debug UI](start-menu-ui.md).
