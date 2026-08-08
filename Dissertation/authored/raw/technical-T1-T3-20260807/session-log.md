# T1 / T3 technical validation session — 2026-08-07

Live log. Every row is written the moment the author calls out a switch. Times are wall-clock,
local (Europe/Dublin, +01:00), matched against the OVR Metrics CSV timestamps and against the
author's own screen recording for T3 frame extraction.

Device: Quest 3, serial 2G0YC5ZG5F051R. Build: study.apk (both scenes), TestModeSequencer
auto-bootstrap disabled for this session (temporary edit, reverted after).

## T1 — passthrough scene (CameraSphereVignette)

### Block 1 (03:40–03:57) — INVALID, no data

OVR Metrics Tool app was not actually installed at the time (only the bundled background service
`com.oculus.ovrmonitormetricsservice` existed on device); the `CapturedMetrics` directory did not
exist and `adb pull` confirmed nothing was written. Timestamps below are preserved for reference only
— **do not use for analysis**.

| Wall-clock time | Config | Notes |
|---|---|---|
| 2026-08-07 03:42:01 +0100 | Soft Dark | first logged switch; recording + CSV both running |
| 2026-08-07 03:42:48 +0100 | Blur | only 47s after Soft Dark, no Hard Dark called out — flagged, see chat |
| 2026-08-07 03:43:42 +0100 | Hard Dark | Blur segment was only 54s |
| 2026-08-07 03:46:23 +0100 | (Hard Dark ends) | Hard Dark ran 2m41s — good, this one's usable |
| 2026-08-07 03:47:48 +0100 | Blur (redo) | proper ~2min pass, supersedes the 54s attempt at 03:42:48 |
| 2026-08-07 03:49:21 +0100 | Effects off | Blur redo ran 1m33s — a bit short of 2min, see chat |
| 2026-08-07 03:53:16 +0100 | Soft Dark (redo) | Effects off ran 3m55s — well over 2min, good |
| 2026-08-07 03:55:40 +0100 | (Soft Dark redo ends) | Soft Dark (redo) ran 2m24s — good, this one's usable |

### Block 2 (14:40– ) — REDO, OVR Metrics Tool app now properly installed

Fixed by installing OVR Metrics Tool from the Horizon Store and enabling CSV storage in-app (Basic
preset). Verified working end-to-end with a test ENABLE_CSV/DISABLE_CSV cycle before this run — CSV
file grew by the expected row count. App relaunched fresh into passthrough scene, effects off.

- ENABLE_CSV fired: 2026-08-07 14:40:56 +0100

| Wall-clock time | Config | Notes |
|---|---|---|
| 2026-08-07 14:40:56 +0100 | Effects off | CSV enabled, app just relaunched into this state |
| 2026-08-07 14:42:39 +0100 | Soft Dark | Effects off ran 1m43s |
| 2026-08-07 14:44:08 +0100 | Hard Dark | Soft Dark ran 1m29s — a bit short of 2min, may want a redo pass |
| 2026-08-07 14:46:46 +0100 | Blur | Hard Dark ran 2m38s — good, usable |
| 2026-08-07 14:49:31 +0100 | (Blur ends, DISABLE_CSV fired) | Blur ran 2m45s — good, usable |

- ENABLE_CSV fired: 2026-08-07 14:40:56 +0100
- DISABLE_CSV fired: 2026-08-07 14:49:31 +0100
- Session ended here by author choice; Soft Dark (1m29s) is short but usable, everything else clean.
- CSV recording was actually continuous from app relaunch (in-app "store CSV" setting, Basic preset),
  not gated by the ENABLE/DISABLE_CSV broadcasts — file spans 14:40:05 to 14:49:31 (565 rows, ~1 Hz),
  fully covering all four logged configs with margin either side.
- Pulled to: `Dissertation/authored/raw/technical-T1-T3-20260807/ovr-metrics-block2-passthrough/CapturedMetrics/`
  - `com.samples.passthroughcamera#UnityPlayerGameActivity-20260807_144005.csv` — the real data (565 rows)
  - `com.samples.passthroughcamera#UnityPlayerGameActivity-20260807_142600.csv` — earlier pipeline test, 9 rows, discard

## T1 — video scene (VideoTestScene)

New APK built with VideoTestScene as the boot scene and a temp `DisablePresenterForT1TEMP.cs`
script (deletes-on-cleanup) disabling `AuthoredTargetPresenter` so its `StudyInputLock` doesn't
block free-play A-button mode cycling. App relaunched fresh at 15:00.

| Wall-clock time | Config | Notes |
|---|---|---|
| 2026-08-07 15:02:20 +0100 | Sign Pop | CSV enabled; author skipped straight to Sign Pop, no "effects off" baseline segment for this run |
| 2026-08-07 15:06:35 +0100 | No filter (effects off) | Sign Pop ran 4m15s — good, well over 2min |
| 2026-08-07 15:08:41 +0100 | (No filter ends, DISABLE_CSV fired) | No filter ran 2m6s — good, usable |

- ENABLE_CSV fired: 2026-08-07 15:02:20 +0100
- DISABLE_CSV fired: 2026-08-07 15:08:41 +0100
- Pulled to: `Dissertation/authored/raw/technical-T1-T3-20260807/ovr-metrics-block3-video/CapturedMetrics/`
  - `com.samples.passthroughcamera#UnityPlayerGameActivity-20260807_150156.csv` — the real data (app relaunched 15:01:56, covers full session)
  - other two files are earlier blocks/tests, ignore for this leg

## Cleanup still owed

- Revert `TestModeSequencer.cs` (`git checkout --` — auto-bootstrap attribute re-enabled)
- Delete `Assets/PassthroughCameraApiSamples/ShaderSample/Scripts/Study/DisablePresenterForT1TEMP.cs`
- Rebuild once more with the normal scene order (CameraSphereVignette first) if the study build is
  needed again — the two test builds this session are not the study build.

## T3 — legibility (native vs camera path)

Recording: `Dissertation/Casting_Video_1786107960332.mp4`, 2560x1370, ~24fps, 80.9s, MQDH cast.
Segments identified from the in-frame `T3: NATIVE PATH` / camera-path debug label (label is hidden
during the camera-path segments — occluded by the full-cover re-render — so absence of the label is
itself the camera-path signal) and cross-checked by framing (camera path has a visibly narrower FOV
than native passthrough, same chart appears larger/more cropped in frame).

| Wall-clock time | Segment | Notes |
|---|---|---|
| 2026-08-07 14:06:05 +0100 | (recording started) | MQDH recording started; chart at 600mm, headset resting (not worn); app relaunched into passthrough scene, effects off |
| ~0:00–0:28 | Native (OS passthrough) | labelled, full FOV, title+key visible |
| ~0:28–0:44 | Camera path | label occluded, visibly narrower FOV (chart fills more of frame) |
| ~0:44–~1:00–1:05 | Native (OS passthrough) | labelled again |
| ~1:00–1:05 to 1:20.9 (end) | Camera path | label occluded, runs to end of recording |

**Observation (not black, confirms criterion is testable):** in both configurations the chart is
legible and readable — the "confirm camera pixels, not black" pre-check is satisfied throughout.

**Preliminary legibility read (needs author confirmation from the recording itself, this is a first
pass off compressed extracted frames):** native path holds clear ring-gap discrimination down to
roughly the 0.6–0.7 logMAR row; camera path's gap discrimination degrades starting around 0.8–0.9.
That is a ~1-2 row native advantage, consistent with the pre-stated criterion (native resolves at
least one logMAR step finer). Needs a second, careful look at the source recording (not the
compressed frame grabs) to confirm the exact crossover rows before this goes in the write-up.

## CSV recording window

- Block 1 (passthrough scene, off/Soft Dark/Hard Dark/Blur):
  - ENABLE_CSV fired: 2026-08-07 03:40:25 +0100
  - DISABLE_CSV fired: 2026-08-07 03:57:03 +0100 (pausing here to do T3; video-scene block still owed)
- Block 2 (video scene, off/SignPop): not yet run
- Pulled to:
