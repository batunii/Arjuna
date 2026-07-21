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

## The pool (REBUILT 2026-07-19 evening — v2, 82 targets)
- ALL 9 authoring files merged (`Tools/analysis/merge_pool.py`, stage 2 now scripted): 107 unique
  (dedup by cls+t_start+t_end) → **82 after ≥1 s filter** (31 traffic lights + 51 people).
  The previously-unreconciled `032344` pass contributed 17 new targets; the afternoon files
  (authoring tool left in scene during baseline runs) contributed 7.
- **≥0.6 s relaxation was considered and is a NO-OP**: bake lifetimes are quantized to 0.5 s steps,
  so nothing exists between 0.6 and 1.0 s. The 25 dropped points are all 0.5 s blips (kept out —
  the 6 confirmed floors were 1–2 s, so 0.5 s targets are near-certain floors).
- **point_ids 0–57 preserved** from the v1 pool (old results stay linkable); newcomers are ids 58–81.
- **Practice set P (2026-07-19): ids 0 & 58** — the pool's two temporally-first targets (both centre
  traffic lights, t=0.0–2.5 s / 0.5–2.5 s). Shown in EVERY run (both sets, both conditions) as
  unscored warm-up clicks; the presenter logs them with set=P so analysis drops them. First scored
  target starts at 6.5 s (run A) / 10.5 s (run B) — a natural breather after practice.
- **Full re-split** (user decision; supersedes the v1 A/B membership) → **A=40, B=40** (+2 P),
  class×region matched (13/13 centre lights, 1/2 periph lights, 12/12 centre people, 14/13 periph
  people), duration/onset/ecc means near-identical, peak simultaneous rings per run (incl. practice)
  **A=3, B=4** (shader cap 12) → `pool_split.csv`. NOT yet pushed to device.
- ⚠ Consequence: the v1 per-set baseline screening no longer matches set membership — **re-run
  baseline screening (A ×2, B ×2) on the new split before filter runs.** Prior per-target results
  remain valid history via the preserved ids (the 6 old floors are still expected floors).

## How to run each stage (all in `VideoTestScene`)
- **Author:** Meta/Study/Point Authoring → Add To Open Scene. Either trigger marks; X play/pause;
  clip end → X replay / B finish. → `authored_points_*.csv`. (Remove it before running the presenter.)
- **Merge:** `python Tools/analysis/merge_pool.py Dissertation/authored/raw --existing Dissertation/authored/pool_uniform_1s.csv --min-duration 1.0 --out Dissertation/authored/pool_uniform_1s.csv` (dedup + ≥1 s + stable ids)
- **Split:** `python Tools/analysis/split_pool.py Dissertation/authored/pool_uniform_1s.csv --practice 0,58 --out Dissertation/authored/pool_split.csv` (`--practice` = warm-up ids → set P)
- **Run (presenter; config homogenised 2026-07-20):** three orthogonal inspector dropdowns
  replace the old 10-value `m_mode` — **Environment** (`DrivingVideo` = authored-pool test /
  `MetaPassthrough` = Block A: X loads `CameraSphereVignette`, presenter survives the load and
  configures world-anchored HardDark or suppressed baseline, then self-destroys), **Target Set**
  (`Auto` from pid parity / forced `A`/`B`; video only — greyed out in passthrough, no data
  there), **Filter** (`WithFilter`/`NoFilter`) + a `m_baselineScreening` toggle (video+NoFilter
  only) that tags the pass BASELINE. A custom inspector
  (`Assets/Editor/AuthoredTargetPresenterEditor.cs`) greys out inapplicable fields and shows a
  "Will run: …" summary; `OnValidate` enforces the same rules. CSV naming/labels unchanged
  (BASELINE/FILTER/NOFILTER-set tags). The presenter remains the single launch point for all
  test blocks.
- **QoL pass (branch `Test/StudyQoL`, 2026-07-20):** Block A modes run minimal-UI (no mode
  toast, A-cycle/B-clear disabled — controllers ONLY paint the window there / click targets in
  the video tests; debug text self-clears after 4 s). The Y-hold `SceneSwitcher`, the "SWITCHED"
  toast bootstrap, and the formal `StudyRig` (+ old `ClickProbeTest` cyan reticle) were removed
  from BOTH scenes — re-add rigs via `Meta > Study > Wire ...` if the formal protocol is ever
  run. Returning from Block A to the video scene = relaunch the app. `m_participantId=-1` = pilot (file named PILOT),
  `>=0` = real participant. Change mode → rebuild → run. In-headset flow: **X starts** (video held
  at 0 with a HUD banner); the 2 practice rings ramp in and the **video pauses** with "pull EITHER
  trigger" text until both are clicked; video plays once (no loop); at clip end the pass closes its
  CSV and shows the tally; **X starts a fresh pass** (new CSV). Files:
  `authored_results_<PILOT|P#>_<MODE>-<set>_<stamp>.csv`.
- **Pull results to PC:** `.\Tools\study-console.ps1 apull` (→ `Dissertation/authored/raw/`);
  then `.\Tools\study-console.ps1 awipe` deletes device copies ONLY where byte-size matches the
  pulled local file.
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
1. **Push the new `pool_split.csv` to the device** (adb command below) — the runtime loads it.
2. **Re-run baseline screening on the v2 split** (`m_baselineMode` on, A ×2 then B ×2) — required
   because the full re-split changed set membership/crowding. Watch for floors among ids 58–81.
3. **Enlarge the window** (~20–25 / 12–15; scene currently has 4×4 — too small) and run
   **A-filter + B-filter** (2 passes each). Consider whether probes should be *fully* filtered
   (Point 3, previously → 0% floor) or kept partly visible in defocus so the Filter condition is
   measurable — a real design decision to make/discuss.
4. Aggregate **filter vs no-filter per set** (hit-rate / RT / false alarms) — the actual effect.
5. Finalise screening: drop confirmed floors → re-run `split_pool.py` on survivors.
6. Consider a mutual-exclusion guard between PointAuthoringTool and AuthoredTargetPresenter.

## CSV schemas
- `authored_points_*`: point_id,cls,kind,t_start,t_end,duration_s,az_mid_deg,el_mid_deg,eccentricity_deg,box_height_deg
- `pool_split`: above + region,set
- `authored_results_*`: t_ms,pid,condition,set,target_id,kind,t_start,t_end,duration_s,outcome,rt_s,angle_deg
