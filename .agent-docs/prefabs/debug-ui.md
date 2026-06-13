# Prefabs: StartScene Debug UI

Last updated: 2026-06-03

Reusable world-space UI element prefabs instantiated by `DebugUIBuilder` to build the StartScene menu
(and usable for any debug panel). All live in `StartScene/Prefabs/`.

## Container / helpers

- `CanvasWithDebug` — main canvas hosting the debug panel.
- `UIHelpers` — shared UI helper rig (pointer/raycast support).
- `DebugUITestCanvas` — test/demo canvas exercising every element type.
- `ReturnToStartScene` — Start-button tooltip + return-to-menu behaviour.

## Element prefabs (one per `DebugUIBuilder.Add*`)

| Prefab | Built by |
|---|---|
| `DebugButton` | `AddButton` |
| `DebugLabel` | `AddLabel` |
| `DebugSlider` | `AddSlider` |
| `DebugToggle` | `AddToggle` |
| `DebugRadio` | `AddRadio` |
| `DebugTextField` | `AddTextField` |
| `DebugDivider` | `AddDivider` |

## Usage

`DebugUIBuilder` is a singleton (`DebugUIBuilder.Instance`); call the `Add*` methods to append elements,
then `Show()`. Layout is controlled by the builder's `IsHorizontal` / spacing / margin fields.

Related: [Start Menu & Debug UI system](<../systems/start-menu-ui.md>).
