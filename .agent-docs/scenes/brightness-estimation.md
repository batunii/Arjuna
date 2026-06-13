# Scene: BrightnessEstimation

Last updated: 2026-06-03

- **Path:** `Assets/PassthroughCameraApiSamples/BrightnessEstimation/BrightnessEstimation.unity`
- **Role:** Estimates ambient room brightness from the camera feed and reacts to it.

## Purpose

Samples camera pixels (`GetColors()`), averages luminance over a rolling buffer, and raises a
`UnityEvent<float>` when brightness changes. A debugger reacts when brightness crosses configurable
min/max thresholds ("too dark" / "too light") and updates debug UI.

## Key GameObjects & scripts

- `BrightnessEstimationManager` — samples pixels on a refresh timer; fires `m_onBrightnessChange`.
  Inspector knobs: `m_refreshTime`, `m_bufferSize`.
- `BrightnessEstimationDebugger` — listens via `OnChangeBrightness(float)`; thresholds
  `m_minBrightnessLevel` / `m_maxBrightnessLevel`; fires `m_onTooDark` / `m_onTooLight`.

## Prefabs used

- `BrightnessEstimationManagerPrefab`, `BrightnessEstimationDebuggerPrefab`
- `PassthroughCameraAccessPrefab` (shared)

## Transitions

- **Loaded by:** StartScene menu. **Returns to:** StartScene (Start button).

## Classification

Sample / example scene.

Related: [Passthrough Camera Access](<../systems/passthrough-camera-access.md>).
