# Scene: ShaderSample

Last updated: 2026-06-03

- **Path:** `Assets/PassthroughCameraApiSamples/ShaderSample/ShaderSample.unity`
- **Role:** Applies a custom GPU shader effect to the live camera texture.

## Purpose

Minimal demo of feeding the passthrough camera texture into a material's `_MainTex` so a custom
shader can post-process the camera image on a mesh.

## Key GameObjects & scripts

- `ShaderSampleManager` — sets `GetTexture()` onto a `MeshRenderer` material's main texture.
- `PassthroughCameraAccessPrefab` — the core camera provider.

## Prefabs used

- `ShaderSampleManagerPrefab`
- `PassthroughCameraAccessPrefab` (shared)

## Transitions

- **Loaded by:** StartScene menu. **Returns to:** StartScene (Start button).

## Classification

Sample / example scene.

Related: [Passthrough Camera Access](<../systems/passthrough-camera-access.md>).
