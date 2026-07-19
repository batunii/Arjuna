# HANDOFF — authored-pool driving study (state as of 2026-07-19)

Entry point for a new session. Full pipeline design: `../authored-pool-pipeline.md`. Probe visual
design + methods: `../probe-target-design.md`. Branch: **`Test/PointAuthoring`** (pushed). Raw data:
`Dissertation/authored/raw/` (pulled from the Quest); working pool: `Dissertation/authored/`.

## TL;DR — where we are + the immediate blocker
- The whole pipeline is built and works: author targets → merge/filter → split into counterbalanced
  sets A/B → baseline clickability screen → experiment runtime with simultaneous ring probes.
- **No-filter (baseline) data is good:** set A ~71% hit, set B ~84% hit; 6 targets are consistent
  floors (unclickable in both passes).
- **BLOCKER: every Filter run is 0% hit (0/34 in set A).** Cause: the locked window is far too small
  (~2°), and probes are composited *after* the filter (supervisor Point 3), so with a 2° window
  almost every target sits in the dimmed periphery → the ring is dimmed to invisibility → unclickable.
  This is a window-size artifact, not a real attention effect. **Fix before any more filter runs:
  set a realistic windscreen window** (e.g. `m_windowHalfWidthDeg≈20–25`, `m_windowHalfHeightDeg≈12–15`)
  so targets inside it stay visible; ~20 of 29 set-A targets are "centre" and would then be
  measurable, ~9 peripheral remain filtered (which is the intended central-vs-peripheral contrast).
- The filter itself renders correctly — the diag from the working run shows `effStrength=1.00,
  suppressed=False, mode=SignPop`. Earlier "no filter" was `PointAuthoringTool` left in the scene
  forcing `StudyEffectSuppressed=true` each frame (+ motion suppression). **Only ONE of
  {PointAuthoringTool, AuthoredTargetPresenter} may be in the scene at once.**

## Results so far

### No-filter clickability (BASELINE, per set, n=2 each)
| set | passes | aggregate hit-rate | consistent floors (missed both passes) |
|---|---|---|---|
| A | 150540 (23/29), 152344 (19/29) | 42/59 = **0.71** | ids **3, 22, 24, 27** |
| B | 154815 (26/29), 160101 (23/29) | 49/58 = **0.84** | ids **16, 40** |
| (all-58, over-crowded ref) | 032247 | 90/116 = 0.78 | — |

**6 floor candidates total** (A: 3,22,24,27; B: 16,40), all short (1–2 s). NOT dropped (held).
Validation: the crowded all-58 run falsely floored ids 23 & 49 — both hit once screened per-set.

### Filter (SignPop, ~2° window) — set A
| run | targets | hit | miss | hit-rate |
|---|---|---|---|---|
| 162938 | 4 | 0 | 4 | 0.00 |
| 204656 | 4 | 0 | 4 | 0.00 |
| 205514 | 25 | 0 | 25 | 0.00 |

**0/34 — all missed** (targets logged as active/miss, 0 false alarms → they were present but the
dimmed probes couldn't be seen/clicked). Uninformative until the window is enlarged. No Filter/set-B
runs yet.

## The pool
- 71 unique authored (dedup by cls+t_start+t_end) → **58 after ≥1 s filter** (24 traffic lights + 34
  people; 0 stop signs) → `pool_uniform_1s.csv`.
- Split (stratified class×region + duration/onset/ecc + simultaneity balancing) → **A=29, B=29**,
  max simultaneous rings A=2/B=3 → `pool_split.csv` (the runtime loads this; also pushed to device).
- Note: an extra authoring file `authored_points_20260719_032344.csv` postdates the split and was NOT
  merged into the 58 — reconcile if those points were meant to be included.

## How to run each stage (all in `VideoTestScene`)
- **Author:** Meta/Study/Point Authoring → Add To Open Scene. Either trigger marks; X play/pause;
  clip end → X replay / B finish. → `authored_points_*.csv`. (Remove it before running the presenter.)
- **Split:** `python Tools/analysis/split_pool.py Dissertation/authored/pool_uniform_1s.csv --out pool_split.csv`
- **Baseline screen:** Meta/Study/Authored Study → Add Presenter. `m_baselineMode` on, `m_baselineSet=A`
  then B, ~2 passes each, click everything. → `authored_results_*.csv` (condition=BASELINE).
- **Experiment:** presenter, `m_baselineMode` off, `m_condition=Filter`/`NoFilter`, `m_forceSet=A`/`B`
  (or Auto for participant counterbalance), **set a proper window size first**. → `authored_results_*.csv`.
- **Push pool to device (needed for device runs):**
  `MSYS_NO_PATHCONV=1 adb push Dissertation/authored/pool_split.csv /storage/emulated/0/Android/data/com.samples.passthroughcamera/files/pool_split.csv`

## Gotchas (learned the hard way)
- **One presenter/authoring component at a time** — both take over the scene and fight; PointAuthoringTool
  forces the filter off. (Consider adding a mutual-exclusion guard — not done yet.)
- **Filter needs motion suppression off** — presenter sets `MotionEnabled=false`.
- **Window size is critical** — too small floors the Filter condition at 0 (current bug).
- **Device `Debug.Log` is unreliable in logcat** here — write diagnostics to a file in persistentDataPath.
- **adb paths from Git Bash** need `MSYS_NO_PATHCONV=1` (remote path) and a Windows-form local dest.
- persistentDataPath = `/storage/emulated/0/Android/data/com.samples.passthroughcamera/files/`.

## Files
Scripts: `.../Study/PointAuthoringTool.cs`, `.../Study/AuthoredTargetPresenter.cs`,
`Assets/Editor/PointAuthoringMenu.cs`, `.../Shaders/CameraSphereVignette.shader` (multi-probe),
`.../Scripts/VideoTestSceneManager.cs` (`SetBlobProbes`, controller aim, video controls).
Analysis: `Tools/analysis/split_pool.py`, `Tools/analysis/blob_probe.py` (older schema; adapt for
`authored_results`). Recent commits on `Test/PointAuthoring`: 7069f84 (runtime loader), fb86878
(force-set), 44359a2 (filter fix), 7a27c40 (window slider), 95f303f (pipeline doc).

## Next steps (in order)
1. **Enlarge the window** (~20–25 / 12–15) and re-run **A-filter + B-filter** (2 passes each). Consider
   whether probes should be *fully* filtered (Point 3, currently → 0% floor) or kept partly visible in
   defocus so the Filter condition is measurable — a real design decision to make/discuss.
2. Aggregate **filter vs no-filter per set** (hit-rate / RT / false alarms) — the actual effect.
3. Finalise screening: drop the 6 floors → re-run `split_pool.py` on 52 survivors.
4. Consider a mutual-exclusion guard between PointAuthoringTool and AuthoredTargetPresenter.
5. Reconcile the un-merged `authored_points_20260719_032344.csv` if those points were intended.

## CSV schemas
- `authored_points_*`: point_id,cls,kind,t_start,t_end,duration_s,az_mid_deg,el_mid_deg,eccentricity_deg,box_height_deg
- `pool_split`: above + region,set
- `authored_results_*`: t_ms,pid,condition,set,target_id,kind,t_start,t_end,duration_s,outcome,rt_s,angle_deg
