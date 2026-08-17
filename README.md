# Arjuna — Diminished Reality attention guidance on Quest 3

MSc dissertation prototype (TCD, 25377738). A Diminished Reality (DR) system that guides
visual attention by *suppressing* the periphery rather than adding overlays: the region a
user is looking at stays clear while everything outside it is progressively darkened,
desaturated, blurred or flattened in contrast.

The prototype runs on Quest 3 / 3S passthrough via the Meta Passthrough Camera API (PCA),
and ships with the experiment harness and analysis pipeline used to evaluate it.

## What's here

| Path | Contents |
|---|---|
| `Assets/CameraSphereVignette.unity` | Live passthrough scene. The filter is applied to a head-locked sphere over the real world. |
| `Assets/VideoTestScene.unity` | Video scene. A 360°/flat driving clip replaces passthrough so conditions are identical across participants. |
| `Assets/PassthroughCameraApiSamples/ShaderSample/` | The filter itself — shaders, materials and the two scene managers. |
| `.../ShaderSample/Scripts/Study/` | Experiment harness: session driver, probe presentation, logging. |
| `Assets/Editor/` | Editor menus for scene wiring, builds and pulling data off the headset. |
| `Tools/` | Python + PowerShell pipeline: video encoding, detection baking, session validation, analysis. |
| `PilotTools/` | Browser-based Block A tasks (n-back CPT, distractor reel). |
| `Dissertation/` | Manuscript, raw study data, analysis records. |

## Filter modes

`VignetteMode` (`CameraSphereVignetteManager.cs`) defines eleven peripheral treatments.
The two used in the study are marked:

| Mode | Effect |
|---|---|
| `Blur` | Blurred, desaturated periphery with contrast restored |
| `SoftDark` | Soft dark overlay (~75 % alpha) |
| **`HardDark`** | Full peripheral blackout — **Block A condition** |
| `TintedDark` | Dark overlay in a configurable colour |
| `ChromaticCool` | Periphery shifts cool, focus shifts warm |
| `ColorPop` | Grey periphery, saturated colours boosted |
| `ConspicuitySqueeze` | Peripheral contrast flattened toward the local mean |
| `GranulatedPeriphery` | World-locked noise grains, density rising with eccentricity |
| `OutlinedDark` | Near-blackout with luminance edges preserved |
| `SpotLift` | Focus window brightened, periphery softly dimmed |
| **`SignPop`** | `ColorPop` gated by baked detections, so only real signs and lights pop — **Block B condition** |

The clear region ("focus window") is painted by the user with the right trigger and, in the
passthrough scene, world-anchors onto real geometry through MRUK's
`EnvironmentRaycastManager`. In the study it is instead locked to a fixed size so window
geometry is constant across participants.

`SignPop` needs to know which objects are worth highlighting. Detections come from a YOLO
model run through the Unity Inference Engine — live via `YoloRunner`, or pre-baked per video
frame via `VideoDetectionBaker` so the study runs at a stable frame rate.

## Experiment harness

`AuthoredTargetPresenter` is the single entry point for a participant session. It presents
ring probes over the video, scores hits and misses per target, and writes one timestamped
CSV per pass.

- **Block A** — peripheral detection under live passthrough (`HardDark` vs. no filter),
  with the n-back task from `PilotTools/` on a separate display.
- **Block B** — probe detection over the driving video (`SignPop` vs. no filter), with
  targets drawn from a hand-authored, screened and counterbalanced pool (`pool_split.csv`).

Each participant runs four X-gated blocks: the video pair on opposite target sets and the
Block A pair, condition order mirrored between the two. Assignment is balanced across
participants from an on-device session ledger, so a session can be resumed after a relaunch.

Baseline arms keep the mode, window and procedure identical and force effect strength to
zero, so the only difference between arms is whether the filter is visible.

`TestModeSequencer` is a separate quick-preview harness for stepping through modes outside
a session. `AuthoredTargetPresenter` destroys it on startup, so only one of the two runs at
a time.

## Requirements

- **Unity** 6000.0.61f1
- **Packages** — Meta MR Utility Kit (`com.meta.xr.mrutilitykit` 201.0.0), Unity Inference
  Engine (`com.unity.ai.inference` 2.2.1)
- **Hardware** — Quest 3 or Quest 3S, Horizon OS v74+
- **Permission** — `horizonos.permission.HEADSET_CAMERA`
- **Player settings** — Active Input Handling must include the old Input Manager;
  experimenter keys arrive through `UnityEngine.Input`

Passthrough camera access needs a physical headset or Meta Horizon Link v2.1+. The XR
Simulator does not support PCA.

Large media stays out of the APK: the driving clip and its baked detection track are pushed
to `persistentDataPath` on the device, with a `StreamingAssets` fallback for Editor runs.

## Running a session

1. Build both scenes: **Meta > Study > Build Study APK (both scenes)**.
2. Push the clip, detection track and `pool_split.csv` to the app's `persistentDataPath`.
3. Launch, set the participant ID on `AuthoredTargetPresenter`, and gate each block with X.
4. Pull results with **Meta > Study > Pull Study Data**, or `adb pull` the app's files
   directory.
5. Validate before the participant leaves: `python Tools/validate_session.py <csv>`.
   A failure triggers the pre-registered re-run rule.

Analysis lives in `Tools/analysis/` — `blocka_pooled.py`, `blockb_pooled.py`, `h1_lmm.py`,
`h2_tost.py`, `t1_framecost.py` and friends. The CSV contract is documented in
`Tools/README-study-tools.md`; the C# writers and Python readers must change together.

## Origin and licence

The project began as Meta's Unity Passthrough Camera API samples. Only the parts still in
use remain — `PassthroughCamera/` (camera access, permissions, input) and `ShaderSample/`,
which has been rewritten into the DR filter. The other sample scenes were removed.

The [`Oculus License`](./LICENSE.txt) applies to the SDK and supporting material. The
[`MIT License`](./Assets/PassthroughCameraApiSamples/LICENSE.txt) applies to certain clearly
marked documents. The YOLO model in
[`ShaderSample/Models`](./Assets/PassthroughCameraApiSamples/ShaderSample/Models) is licensed
under [`MIT`](https://github.com/MultimediaTechLab/YOLO/blob/main/LICENSE).

Meta's PCA documentation remains the reference for the camera layer:
[overview](https://developers.meta.com/horizon/documentation/unity/unity-pca-overview) ·
[getting started](https://developers.meta.com/horizon/documentation/unity/unity-pca-documentation) ·
[inference engine](https://developers.meta.com/horizon/documentation/unity/unity-pca-sentis).
