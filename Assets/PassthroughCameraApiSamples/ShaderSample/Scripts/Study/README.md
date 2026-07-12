# Study Tooling — Wiring & Session Runbook

Implements `Dissertation/testing-strategy-v2.md` §13 (Appendix A). Python counterparts
(validator, analysis, probe-schedule generator) live in `Tools/` — see
`Tools/README-study-tools.md`. The CSV contract is defined there; C# and Python must change
together.

## Components

| Script | Role |
|---|---|
| `StudyLogger.cs` | Per-frame CSV heartbeat + labelled events, flushed every frame |
| `ConditionSequencer.cs` | Latin-square orders from participant ID, condition state machine, configuration guards, experimenter controls |
| `ProbeScheduler.cs` | Block B diamond probes from JSON schedules (ISO 17488 windows, head-yaw deferral) |
| `CPTPanel.cs` | Block A Option V world-locked Go/No-go panel (runtime-generated shapes) |
| `IStudyVignetteControl.cs` | Study API implemented by both vignette managers |

## Scene wiring

**VideoTestScene (Blocks B + C):** add one GameObject `StudyRig` with `StudyLogger`,
`ProbeScheduler`, `ConditionSequencer`. Sequencer: Plan = `VideoScene_BlocksBC`; drag the
`VideoTestSceneManager` into *Vignette Manager Behaviour*; drag logger + probe scheduler.
ProbeScheduler: assign `probes_B1.json` / `probes_B2.json` (import copies of
`Assets/StreamingAssets/StudySchedules/*.json` anywhere under Assets so they become
TextAssets, e.g. `Assets/StudySchedules/`) and set *Probe Material Template* to
`SelectionDotMat` (Shader.Find is stripped on Android — a serialized template is mandatory
for device builds).

**CameraSphereVignette (Block A):** add `StudyRig` with `StudyLogger`, `CPTPanel`,
`ConditionSequencer`. Sequencer: Plan = `PassthroughScene_BlockA`; drag the
`CameraSphereVignetteManager`; *Use Cpt Panel* per the pilot's G2 decision (off = Option P
paper sheets; the sequencer then only marks conditions and the sheets are scored by hand).

**Project setting:** Player → Active Input Handling must include the old Input Manager
("Input Manager (Old)" or "Both") — experimenter keys arrive via `UnityEngine.Input`.

## Experimenter controls (over the existing adb/scrcpy connection)

**Easiest path:** `Tools\study-console.ps1` wraps everything below into friendly commands
(`pid 12`, `space`, `tlx`, `practice`, `pull`, `install`, …) and finds adb automatically —
see its header for the full list. Raw equivalents, `adb shell input keyevent <code>`:

| Key | keyevent | Action |
|---|---|---|
| digits `0–9` | 7–16 | type participant ID (before session opens) |
| ENTER | 66 | confirm ID, open session log, run guards, snapshot CONFIG |
| SPACE | 62 | advance (lock window / start next condition / next sampler mode) |
| T | 48 | TLX_START / TLX_END toggle (blocks SPACE while open) |
| P | 44 | practice run (CPT 40 trials / 6 probes) |
| W | 51 | (re)place CPT panel at participant's gaze (Block A) |
| K | 39 | skip Block C (pre-registered fatigue/overrun rule) |
| I | 37 | INCIDENT marker (details go in the paper incident log) |
| E | 33 | end session (writes SESSION_END, closes the file) |
| V | 50 | switch environment: passthrough scene ↔ video scene (`SceneSwitcher`) |

Advancing is keyboard SPACE only (the thumbstick-click chord was removed — a headset-wearing
solo operator triggered it by accident, jumping the session into a block).
After a session ends (E), the app returns to "type participant ID" automatically — no relaunch
needed between participants.
Environment switch from inside the headset: **hold Y (left controller) ~1 s**. Both scenes ship in
one APK (boot scene: VideoTestScene). The switch is **blocked while a session is recording** — end
with E first so the CSV closes cleanly; a full scene load re-initialises the target environment
(passthrough cameras / video player) from scratch.
All state transitions print `[Sequencer]` lines — watch `adb logcat -s Unity` next to scrcpy.

## Session-day sequence (per participant)

1. Pre-session checklist (testing-strategy §10.2); launch app; start `scrcpy` + screen record.
2. Type participant ID digits → ENTER. Check logcat for `Session open` and no `GUARD_FAIL`.
3. Practice: P (repeat once if criterion missed — go-accuracy ≥ 90 % is printed).
4. Block A: SPACE → participant paints window → SPACE locks (panel auto-places; W to redo)
   → SPACE starts each condition. Tablet cue is printed per condition (`tablets: START REEL`
   → run the countdown). TLX between conditions: T … T. SPACE for the next condition.
5. Block B (video scene build): SPACE per condition; TLX via T; after both, the post-Block-B
   comfort check → SPACE starts Block C or K skips it. Sampler modes auto-run 75 s; verbal
   ratings in each gap, SPACE for the next mode.
6. E ends the session. Then, **before the participant leaves**:
   `adb pull /sdcard/Android/data/<package>/files/ ./logs/P<id>/` and
   `python Tools/validate_session.py logs/P<id>/study_P<id>_*.csv` — a FAIL triggers the
   §8.4 re-run rule while the participant is still present. Archive to two locations.

## Notes & known deviations

- **CPT sequence constraint:** the strategy doc's "no more than three identical in a row" is
  unsatisfiable at 80 % go (mean run length 5). Implemented as: exact 20 % no-go count, no
  two consecutive no-gos, go runs ≤ 8 (`CPTPanel.BuildSequence`). Align the methodology text.
- **Baselines** keep mode/window identical and force effect strength to 0 via
  `StudyEffectSuppressed` — `dr_intensity` in the log verifies every frame of every condition.
- **Blocks A and B run in different scenes/builds.** Between blocks the experimenter switches
  apps; each scene writes its own CSV. The validator accepts either a combined or split
  session (split: expected condition counts come from the CONFIG plan row).
- Distractor tablets are manual by design (§10.1): the sequencer prints the required tablet
  state per condition; start reels on a spoken countdown at CONDITION_START.
