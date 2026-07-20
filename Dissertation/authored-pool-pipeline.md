# Authored-Pool Target Pipeline (informal driving harness)

> Compiled 2026-07-19. End-to-end pipeline for the driving-task click-probe study: hand-author a pool
> of real click-targets, screen them for clickability, split into two counterbalanced sets, and run
> the filter-vs-no-filter experiment with simultaneous ring probes. Branch: `Test/PointAuthoring`.
> This is the *informal* harness (standalone from StudyLogger); it produces its own CSVs. The probe
> visual design + methods rationale live in `probe-target-design.md`; this doc is the operational
> pipeline and current data status.

## Why this exists
Auto-selecting targets from the YOLO bake produced uneven/uninteresting probes. Instead we
hand-curate real detections (traffic lights, people) as the target pool, screen which are actually
clickable, and split them into two matched sets so filter/no-filter can be counterbalanced across
participants without any participant seeing a target twice (avoids the memorisation confound).

## Stages

### 1. Authoring — `PointAuthoringTool.cs` (+ `Assets/Editor/PointAuthoringMenu.cs`)
Point EITHER controller at the playing 360° video and pull the trigger; it matches your aim + video
time to the nearest baked YOLO detection LIFETIME and records that detection as a target. Suppresses
the manager's filter + right-trigger window-painting (raw video; triggers free to mark). Clip plays
once; at the end **X = replay & keep appending**, **B = finish**. Either-hand reticles (cyan/orange).
Add via **Meta/Study/Point Authoring → Add To Open Scene** in `VideoTestScene`.
- Output: `authored_points_<stamp>.csv` — `point_id,cls,kind,t_start,t_end,duration_s,az_mid_deg,
  el_mid_deg,eccentricity_deg,box_height_deg`. Flushed per mark.
- A "point" is a detection *lifetime* (span from the bake), not the instant clicked. Clicks matching
  no detection are discarded.

### 2. Merge + dedup + duration filter — `Tools/analysis/merge_pool.py`
Across multiple authoring passes, merge all `authored_points_*.csv` and **dedup by detection identity**
`(cls, t_start, t_end)` (re-marking the same object never double-counts). Then drop the too-brief
non-clickable blips with a **uniform ≥1 s duration filter** (people kept regardless was considered,
but a single uniform rule is cleaner; the ≥1 s cut only removes near-instant targets — real
floor/ceiling screening is stage 4). Scripted as `merge_pool.py` (2026-07-19): `--existing` preserves
prior point_ids across rebuilds so old results stay linkable; new detections append at the next ids.
- Bake lifetimes are **quantized to 0.5 s steps** — a softer 0.6 s cut is a no-op; sub-1 s means
  exactly 0.5 s blips (25 of them, excluded: confirmed floors were 1–2 s, so 0.5 s ≈ certain floor).
- **Result v1 (2026-07-19): 71 unique → 58 after ≥1 s** (3 files merged).
  **Result v2 (2026-07-19 evening): all 9 files → 107 unique → 82 after ≥1 s** (31 traffic lights +
  51 people); ids 0–57 preserved, newcomers 58–81.
- Region tagged from the locked window: centre if `|az_mid|≤25° & |el_mid|≤15°`, else periphery →
  v2: centre 52, periphery 30 (periphery is people-dominated; only 3 peripheral lights).

### 3. Split into counterbalanced sets — `Tools/analysis/split_pool.py`
Partitions the 58 into two sets **matched on the properties that drive difficulty**:
- **Stratify** on `class × region` (hard) → each set balanced on those by construction.
- **Optimise** (seeded random-restart) to equalise `duration`, `onset (t_start)`, `eccentricity`.
- **Simultaneity balancing** (`W_OVERLAP`): penalise same-set temporally-overlapping pairs so
  overlapping clusters distribute across sets → lower per-set peak concurrency.
- Output `pool_split.csv` (= pool + `region` + `set` columns).
- **Practice targets** (`--practice <ids>`): set aside as set **P**, excluded from the A/B
  optimisation; the presenter shows them in every run as unscored warm-up (logged set=P, dropped in
  analysis). Current: ids 0 & 58 — the pool's two temporally-first targets (centre lights, ending
  2.5 s; first scored target 6.5 s/10.5 s into runs A/B).
- **Result v1: A=29, B=29**, max simultaneous rings A=2, B=3 (whole pool 5).
  **Result v2 (full re-split of the 82-target pool, 2026-07-19 evening): A=40, B=40 + 2 P**,
  class/region matched, duration/onset/ecc means near-identical, peak simultaneous rings per run
  **A=3, B=4** (shader cap 12). ⚠ The re-split changed set membership, so the v1 per-set baseline
  screening is superseded — **re-screen A and B before filter runs.** Sets are interchangeable; the
  per-participant rotation happens at runtime.

### 4. Baseline clickability screening — presenter **baseline mode**
Run the pool with **no filter, no counterbalance**, click everything, ~2 passes; a target missed in
*both* passes is a floor (unclickable) candidate. **Run per set (A then B)** so each is screened at
the SAME simultaneity the experiment uses (2–3 rings) — screening the whole 58 at once over-crowds
(peak 5) and falsely floors targets.
- Presenter: `m_baselineMode` on, `m_baselineSet = A`/`B`. Logs `authored_results_*.csv` tagged
  `condition=BASELINE`. Analysis = per-target hit-rate across passes (I run it ad-hoc; `blob_probe.py`
  covers the same idea for the older schema and needs a small adapt for `authored_results`).
- **Result (2026-07-19, A n=2, B n=2): 6 consistent floor candidates**, all short (1–2 s):
  set A ids 3, 22, 24, 27; set B ids 16, 40. Validation of the per-set approach: the crowded all-58
  run had falsely floored ids 23 & 49 — both were **hit** once tested un-crowded.
- **Status: nothing dropped yet** — held until the filter runs are done (dropping/re-splitting now
  would change set membership and break comparability with the no-filter passes already collected).

### 5. Experiment runtime — `AuthoredTargetPresenter.cs`
Loads `pool_split.csv` (from `persistentDataPath`; adb-push it there), resolves each point to its
baked lifetime (so rings follow the moving objects), and presents ONE set as **simultaneous ring
probes**, scoring multi-target hits.
- **Counterbalance:** even pid → Filter=A / NoFilter=B; odd → swapped. `m_forceSet` (Auto/A/B)
  overrides this for piloting specific set×condition combos.
- **Block A launchers (`m_mode = BlockA_HardDark` / `BlockA_NoFilter`, added 2026-07-20):** the
  presenter doubles as the single launch point for ALL test blocks. These are the passthrough
  test block's two arms: X loads `CameraSphereVignette`; the launcher survives the scene load
  (`DontDestroyOnLoad`), configures the `CameraSphereVignetteManager` — both arms get mode
  HardDark + free right-trigger window painting (the painted window **world-anchors onto real
  geometry** via the scene's wired `EnvironmentRaycastManager`, the depth-raycast backend added
  2026-07-16); the NoFilter arm additionally sets `StudyEffectSuppressed` so the procedure is
  identical but the effect invisible (standard baseline pattern) — then shows brief instructions
  and destroys itself. Hold Y (SceneSwitcher) to return to the video scene.
- **Condition drives the filter:** NoFilter = raw video (`StudyEffectSuppressed`); Filter = vignette
  (`m_filterMode`, default SignPop) formed over the **locked window**, with rings composited AFTER
  the filter (a probe in a dimmed area is dimmed too — supervisor Point 3).
- **Locked window** (`m_windowHalfWidthDeg/HeightDeg`) — painting is off; this fixed window replaces
  it. Currently small (down to 0.1° half); set the intended size before real filter runs.
- **Motion suppression is disabled** during runs (`MotionEnabled=false`) — the filter is the
  manipulation and must not fade on head movement.
- Output `authored_results_<stamp>.csv` — `t_ms,pid,condition,set,target_id,kind,t_start,t_end,
  duration_s,outcome,rt_s,angle_deg`. Outcomes: hit / miss / bad_aim (n/a here) / false_alarm.
- Add via **Meta/Study/Authored Study → Add Presenter To Open Scene**.

### Multi-probe rendering (enabling change)
`CameraSphereVignette.shader` now composites up to **12 simultaneous blob probes** (`_BlobData[]` /
`_BlobFlash4[]` arrays, `ApplyOneBlob` per probe, `ApplyBlobs` loop). `VideoTestSceneManager` exposes
`SetBlobProbes(count,data,flash)`; the single-probe `SetBlobProbe` is kept (slot 0) so the older
`BlobTargetController` is unchanged. Probe style is the fixed-colour **ring** (see probe-target-design).

## Key design decisions
- **Simultaneous probes, not one-at-a-time** — the authored pool has up to 5 targets on screen at
  once; one-at-a-time would force dropping most. Simultaneous keeps them all (needs multi-probe render).
- **Per-set baseline screening** at the real crowding (not the whole pool at once).
- **Hold all dropping** until filter runs are in, to preserve no-filter↔filter comparability on the
  same sets.
- **Window size is a study parameter** — currently tiny for visibility; pick the intended windscreen
  size before collecting filter data.

## File map
- `Assets/PassthroughCameraApiSamples/ShaderSample/Scripts/Study/PointAuthoringTool.cs` — authoring
- `Assets/PassthroughCameraApiSamples/ShaderSample/Scripts/Study/AuthoredTargetPresenter.cs` — runtime + baseline
- `Assets/Editor/PointAuthoringMenu.cs` — Meta/Study menus (authoring tool + presenter)
- `Assets/PassthroughCameraApiSamples/ShaderSample/Shaders/CameraSphereVignette.shader` — multi-probe
- `Assets/PassthroughCameraApiSamples/ShaderSample/Scripts/VideoTestSceneManager.cs` — SetBlobProbes, controller aim, video controls (VideoLength/Looping/RestartVideo)
- `Tools/analysis/split_pool.py` — set splitter; `Tools/analysis/blob_probe.py` — screening/ISO/d′ (older schema)

## Current data status — see `authored/HANDOFF.md` for the full current picture
- Authored pool: **58** (post ≥1 s filter), split A/B (29 each). Data now persisted in
  `Dissertation/authored/` (working) + `Dissertation/authored/raw/` (raw device pulls).
- Baseline screening: A ×2, B ×2 done → **6 floor candidates** (A 3,22,24,27; B 16,40; not dropped).
- **Filter runs: done but 0% hit (0/34, set A)** — the filter renders fine (diag `effStrength=1`) but
  the ~2° window + probe-filtered-after-vignette (Point 3) dims every probe to invisibility → all
  missed. **Blocked on window size**; enlarge before re-running. Details in HANDOFF.

## Bug-fix log (this arc)
- Authoring replay: `RestartVideo` uses `Stop()+Play()` (a stopped player ignored `time=0`).
- Authoring showed dark/painted: presenter sets `StudyEffectSuppressed`+`StudyInputLock`.
- Filter not visible: **`PointAuthoringTool` left in the scene** forced `StudyEffectSuppressed=true`
  each frame (+ motion suppression). Fix: remove authoring tool before presenter runs; presenter sets
  `MotionEnabled=false`. (Rule: only one of the two components in the scene at once — a guard is a TODO.)
  Vignette confirmed rendering at full strength once alone.
- Window slider extended down to 0.1° half — but note the tiny window is what now floors the Filter
  condition at 0%; a windscreen-sized window is needed for the real comparison.

## Durability — DONE
The pool, split, and all raw device CSVs are now committed under `Dissertation/authored/`.

## Next steps
1. Set the intended window size; run **A-filter** and **B-filter** (matching the no-filter passes).
2. Aggregate **filter vs no-filter per set** (hit-rate / RT / false alarms) — the actual effect.
3. Finalise screening: drop the 6 floors → re-run `split_pool.py` on the 52 survivors → use for
   subsequent runs.
4. Persist the pool/results into the repo.
