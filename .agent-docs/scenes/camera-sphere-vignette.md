# Scene: CameraSphereVignette

Last updated: 2026-07-14

- **Path:** `Assets/CameraSphereVignette.unity`
- **Role:** Dissertation study Block A — DR vignette modes evaluated against **live** passthrough
  cameras. Also the passthrough half of the informal `Test/FinalCountDown` 4-mode preview
  (modes 3 & 4: Hard Dark, then no filter).
- Part of the dissertation study build (separate from the sample-suite `StartScene` hub) — see
  [Scene Flow](<_flow.md>#dissertation-study-build).

## Key GameObjects & scripts

- `CameraSphereSphere` — `CameraSphereVignetteManager` (see
  [Focus Vignette](<../systems/focus-vignette.md>)). Samples both PCA cameras (Left + Right).
- `StudyRig` — `StudyLogger` + `ConditionSequencer` (plan `PassthroughScene_BlockA`) + `CPTPanel`.
  Wired via `Meta > Study > Wire CameraSphereVignette`, not by hand — see
  [Study Tooling](<../systems/study-tooling.md>).
- `SelectionLine` — the hold-progress ring `SceneSwitcher` shows while holding left `Y`.
- `TestModeSequencer` / `BlobTargetController` — **not** present as scene GameObjects here either
  (self-bootstrap once, persist across the scene load from `VideoTestScene` via
  `DontDestroyOnLoad`; `BlobTargetController` is inert in this scene — blobs are video-mode-only).
- Standard Meta XR building blocks: `[BuildingBlock] Camera Rig`, `[BuildingBlock] Passthrough`.

## Transitions

- **Reached from `VideoTestScene`** via hold left `Y` / key `V` (`SceneSwitcher`, full reload) or
  programmatically by `TestModeSequencer` moving into a `PassthroughScene` mode.
- Same mechanism switches back to `VideoTestScene`.

## Classification

Dissertation study scene.

Related: [Focus Vignette](<../systems/focus-vignette.md>), [Study Tooling](<../systems/study-tooling.md>),
[VideoTestScene (scene)](<video-test-scene.md>).
