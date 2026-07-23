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

## Probe + window revision (2026-07-21)

- **Ring is BLACK now** (`m_ringColor` (0,0,0,0.4), width 0.05→0.06; presenter default + scene).
  Rationale: black is the one colour the filter's desat+dim leaves unchanged, so an on-top black
  ring is identical to a behind-filter ring — supervisor Point 3 satisfied with no shader change.
  Known trade-off: may floor against dark scene content — check the no-filter baseline stays in
  the ~60–85% band and look at which targets floor. Fallback: yellow ring attenuated by the
  filter's local transform in `ApplyBlob`.
- **Window stays 4×4° — DELIBERATE (user decision, 2026-07-21).** Earlier HANDOFF framing of
  4×4 as "too small / the bug" was wrong. Rationale: the window size is the DOSE of the
  manipulation. With a windscreen window (25/15) + falloff, the filter lives only beyond ~35°
  where vision is weak anyway — the whole usable view is normal and nothing guides attention.
  With 4×4 + the 24° SignPop soft edge (kept), the filter grades outward from 4°: ~50% strength
  at 16°, full at 28° — a focus funnel that is actually present in the view. The falloff makes
  the perceived clear region much bigger than nominal (by design). The v1 0%-filter runs are
  attributed to the old near-black periphery (~7% luminance) + probe style, not the window per
  se — with the lessened dim and the black ring, probes should be detectable through the filter.
- **Analysis consequence (open, discuss with John):** `split_pool.py`'s centre/periphery tag
  (boundary 25/15) no longer means unfiltered-vs-filtered — under 4×4+24° every target sits on
  the gradient (a "centre" target at 20° az gets ~80% filter). Treat filter strength at the
  target as a continuous dose: t = smoothstep((max(|az|,|el|)−4°)/24°), computable per target
  from az/el at analysis time. A/B stay comparable (eccentricity was balanced as a covariate).
  Point 2's bifurcation becomes clear-core (≲10°) / graded / full-filter (≥28°).
- **Periphery dim lessened**: shader's dead `_PopGreyDim` uniform re-wired (grey track was
  hard-coded 0.15, now uses the inspector knob = 0.4) and the overall periphery dim 0.44→0.70.
  Non-ROG periphery ≈ 28% of original luminance (was ~7%). Brighter periphery also keeps the
  black ring detectable there and softens the grey boundary (why the narrower soft edge is OK).
- **Window-size research note (2026-07-21, web-verified): `../window-size-research.md`** — anchors
  (PRC road-centre = 8° radius; PDT probe band 11–23°; UFOV ≈30° dia, shrinks under load) + per-
  target dose tables for candidate configs. Recommendation on the table: **8×6° core + 24° edge**
  (clear core = road-centre region, 50% dose at 20°, full at 32°; evenest dose spread; fixes the
  5-clear-target A-set anchor + practice-id-0-at-0.74 issues). **ADOPTED 2026-07-21: scene +
  presenter default now 8×6 + 24° edge.** Expected doses per set: A 9/8/7/3/13, B 14/8/3/1/14
  (clear/light/mid/heavy/full), practice id 0 at 0.50.
- **Boot incident 2026-07-21 ~21:35 (build 21:32, pid 1001, AutoSession):** user booted into a
  dark scene, A cycled free-play modes, no HUD, X unusable (left controller battery dead — X is
  left-hand only). Diagnosis: the on-device ledger has NO PILOT1001 PLAN row, i.e. the
  presenter's Start() died before writing it (a healthy video-armed boot locks free-play input
  via ApplyCondition and shows the armed HUD — none of that happened). Root cause NOT yet
  identified (no live logcat: boot lines rotated; the code path for pid 1001 has no obvious
  throw). Hardening added to `AuthoredTargetPresenter.cs` (compiles clean, needs rebuild):
  (1) Start() wrapped — any boot exception now shows on the HUD + writes
  `presenter_boot_error.txt` to persistentDataPath and parks the presenter;
  (2) `m_startButtonAlt` = right-controller **B** as fallback advance (dead left battery can't
  strand a session); (3) passthrough-armed hold now sets StudyInputLock (A can't cycle modes
  while waiting for X); (4) HUD material renderQueue 4100→4600 (readable above dark filters).
  **ROOT CAUSE FOUND (2026-07-22 00:57, via presenter_boot_error.txt):**
  `UnauthorizedAccessException` appending to `session_ledger.csv`. The ledger was adb-PUSHED
  back to the device at 2026-07-21 20:03 (the P0→PILOT999 reclassify) — **adb-pushed files are
  owned by `shell` and READ-ONLY to the app**, so every boot that appended a PLAN/BLOCK row died.
  Fixes: (1) delete the device ledger and let the app recreate it (history preserved in
  `raw/session_ledger.csv`); (2) `AppendLedger` now self-heals — on UnauthorizedAccess it
  deletes and rewrites the file app-owned from memory (app owns the directory, so delete is
  permitted). **RULE: never adb-push `session_ledger.csv` (or any file the APP must write);
  push is fine for read-only inputs like `pool_split.csv`.**
- **Black ring REVERTED (2026-07-22, after PILOT1001 data).** The black ring floored performance
  where the filter wasn't even running: NoFilter-A 19/40 (48%, median RT 3.55 s) vs ~60–70% /
  ~1.8 s for yellow on set A; Filter-B 13/40 (32%). Five targets that were yellow-ring hits
  flipped to miss; most other misses were far-peripheral (38–87° az) — without chroma the ring's
  onset never captures attention out there, targets expire unseen. Replacement (implemented in
  `CameraSphereVignette.shader` style 3): **yellow ring, attenuated in-shader by the filter's
  local transform** (ring colour pushed through the SignPop desat+dim × window falloff ×
  effect strength) — full yellow with filter off / inside the window, fades like a real object
  in the filtered periphery. Point 3 still holds; NoFilter baselines stay comparable to the old
  yellow data (alpha restored to the tested 0.4; the 0.78 was black-ring compensation).
  Window is now **6×5°** (user's choice, between 4×4 and the researched 8×6; doses per set at
  6×5+24: A 8/6/8/4/14, B 13/6/5/2/14, med 0.62/0.42). Needs rebuild; then re-run both video
  blocks and compare.
- **PILOT1002 (2026-07-22 ~02:36–03:09, black ring @0.78 alpha, 6×5 window, order P-F):**
  Block A ×2 X'd through quickly, then video NoFilter-B **20/40 (50%)** RT med 2.46 s, video
  Filter-A **27/40 (68%)** RT med 2.20 s. Two findings: (1) even at 0.78 alpha the black ring
  fails the no-filter baseline band — its enemy is CLUTTER masking, and the filter's flat grey
  periphery actually *helps* it (camouflage removal → Filter > NoFilter, backwards vs set
  difficulty). Confirms the yellow-attenuated-ring decision. (2) **Duplicate-presenter bug
  found & fixed**: the presenter is DontDestroyOnLoad for the Block A round-trip; returning to
  the video scene spawns the scene's own copy alongside it → both ran blocks 2–3 in lockstep
  (duplicate CSVs 1 ms apart, doubled ledger BLOCK rows for 1002). Fix: static singleton guard
  in Awake (scene copy self-destructs; travelling instance keeps the session). Duplicate CSVs
  moved to `raw/duplicates_20260722/` (kept the well-formed file of each pair). Ledger's doubled
  BLOCK rows are harmless (resume just re-marks the same block done).
  The ledger self-heal WORKED (file is app-owned again; 1001/1002 PLAN rows written normally).
- **"Rope" ring added (2026-07-22, user proposal):** `m_ringSegments` on the presenter (default 4)
  renders the ring as alternating black/white arc pairs instead of the solid colour. Achromatic
  (desat-proof), contrast-polarity complete (visible on any background — fiducial principle), and
  the multiplicative dim preserves its internal Michelson contrast (honest Point 3 without the
  grey-on-grey floor). Full rationale + citations: `probe-target-design.md` §4e. `0` = solid
  yellow-attenuated ring (the empirically anchored fallback). Arc pairs must stay low (3–5) or
  peripheral acuity blurs the pattern to grey.
- **Rope v1 (translucent) failed in-headset (2026-07-22) — concept NOT yet fairly tested.** At
  0.4 alpha / thin width the alternation fragments (one polarity always invisible → ~4 faint
  flecks, no closure). Fair test config (untested): segments 4 + alpha ~0.9–1.0 + width ~0.10 —
  opaque chunky arcs, per the fiducial principle. See probe-target-design.md §4e. **Also in the
  build: solid yellow ring + dark flanking border (`m_ringOutline` 0.6)** — warning-sign pairing:
  yellow carries chroma/luminance + closure, the multiplicative dark border guarantees contrast
  on bright backgrounds and is filter-invariant by construction. **Probe choice (opaque rope vs
  bordered yellow) is the user's call — both are inspector-selectable in the same build.**
  Scene state saved from the editor: ring yellow 0.4 / width 0.06 / segments 0 / outline 0.6,
  window 6×4°, `m_popGreyDim` 0.3 (periphery ≈21%), pid 1002.
- **Probe DECIDED by user after Editor eyeball (2026-07-22): near-opaque black/white rope.**
  Scene config (saved): `m_ringSegments 4`, ring alpha **0.88** (the load-bearing value — RGB is
  unused while segments>0), `m_ringWidth 0.05`, `m_ringOutline 0`, probe 1.6°, window 6×4°,
  soft edge 24°, pid **1003**. Presenter now re-pushes ring
  statics at every pass start (X), so inspector tweaks apply live in Editor play mode.
  `m_popGreyDim` now **0.21** (2026-07-22; periphery ≈ 0.21×0.70 ≈ **15%** of original
  luminance — anchored to ITU-R BT.500's reference surround ratio of 0.15 × peak luminance for
  critical viewing; also above the classic ~10:1 task-to-remote-surround comfort limit, which is
  IES/ergonomics lore — verify ISO 9241-6 before citing). Dim history: 7% (v1, floored the
  probes) → 28% → 21% → 15%.
- **Block A task DECIDED (2026-07-22): forced-choice 1-back with video-clip distractors.**
  `PilotTools/block-a-cpt.html` now runs ONLY that configuration — the Go/No-Go CPT mode,
  the respond-with selector, and the procedural-reel distractor option were removed (the
  filename keeps its historical `cpt` name so references stay valid). Setup screen is just
  pid + run slot (Practice/A1–A4). Main runs cut 140→**84 trials** (84 × 2.5 s SOA = 3:30,
  the intended block length; 140 was the old CPT count at 1.5 s SOA and ran 5:50) — ~25
  repeat trials per run. CSV `mode` stays `NBACK1_PILOT`. `testing-strategy-v2.md`
  §4.1 still describes the old CPT and needs updating. See `PilotTools/README.md`.
- **Cross-system pid matching (Block A CPT ↔ Unity), 2026-07-22:** within Unity the AutoSession
  pid is stamped everywhere (video CSV filename+rows, ledger PLAN/BLOCK) — guaranteed. The Block
  A SCORES come from `PilotTools/block-a-cpt.html`, whose participant field is HAND-TYPED →
  matching is convention. Safeguards: (1) the Block A instruction HUD now shows "CPT tool
  participant id: N" at block start (copy, don't remember); (2) TODO analysis-side check: each
  pid's CPT rows (`t_ms` epoch ms) must fall inside that pid's Block A window in the ledger
  (BLOCK-row timestamps) — a typo shows up as time/pid inconsistency. Known hazard: pid −1
  builds cannot RESUME a crashed session (relaunch auto-assigns the NEXT id, orphaning the
  interrupted participant) — fix if needed: pid_override.txt read at boot (not built).
  **Tightened 2026-07-23:** the CPT tool now REQUIRES a filter-state selection for main runs
  (stamped in `dr_intensity` 1/0, run-start payload, filename `..._A3_FILTER_...`); the Unity
  Block A HUD shows the arm's filter state next to the pid; every trial autosaves the full CSV
  to localStorage (recovery list on the setup screen, auto-download at run end — a missed
  export no longer loses the run); and a window-setup stage shows the 540×540 panel as a
  dashed dummy so the participant paints the focus window against the real task geometry
  before trials start (P33-style same-arm double-runs and P4-style lost files both addressed).
- **Block A distractor pool expanded (2026-07-23):** five Bollywood music-video clips
  (YouTube, middle sections — first 30 s / last 10 s trimmed) added to `RawFootage/` as
  `video[5-9]_bolly_*`, and both `Assets/_Scratch/tiktok_5min_{1,2}.mp4` reels regenerated
  with `tiktok_captions.py`'s new `--long-sources bolly` option (matching sources' cuts hold
  ~2× longer; reels now ~7–8 min before looping). Richer motion/dance content = stronger
  peripheral distraction. Details: `.agent-docs/systems/distractor-content-pipeline.md`.
  Two reel pairs now exist — `tiktok_5min_1/2` ("New videos", default) and `tiktok_5min_3/4`
  ("Original videos", rebuilt from the original 4 sources, no songs) — switchable via the CPT
  tool's new **Distractor videos** selector; the choice is stamped in the run-start payload
  (`distractors=NEW|ORIGINAL`). The side screens stay dark until Start and re-darken at run
  end. Side frames also bumped a size (500px/73vh, was 440/66vh).
  ⚠ Comparability: distractor content is now a recorded variable — runs with different
  `distractors` values (and all pre-2026-07-23 runs, which played the old 5-min originals)
  should not be pooled without noting it, same caveat class as the v1/v2 trial-count change.
- **Block A v3 + time-course metrics (2026-07-23, user request):** main runs are now
  **140 trials × 1.8 s SOA = 4:12** (window 1.5 s, same 0.3 s closed gap; practice
  unchanged). Rationale recorded in the tool's constants comment: 140 restores the
  original spec count → 42 repeats (d′ sampling variance scales with inverse signal/noise
  counts, Macmillan & Creelman 2005) and ~46-trial thirds; 1.8 s keeps the ≥~2×-median-RT
  window rule (1.5 s vs Day-1 medians 567–776 ms) and sits above SART's 1.15 s
  (Robertson et al. 1997). **v1 (84×2.5) / v2 (105×2.0) / v3 never pool** — payload
  `trials`/`soa_s` distinguish. New consistency metrics: end-of-run stats now show
  accuracy / median correct RT / RT-CV by run thirds + an RT-drift slope (ms/min);
  `Tools/analysis/blocka_timecourse.py` computes the same per run from the CSVs and
  contrasts FILTER vs NOFILTER per version (answers "more/less consistent with time under
  filter?"). Pre-2026-07-23 files lack the filter stamp → UNKNOWN; match via ledger.
- **Block B time-course too (2026-07-23):** `Tools/analysis/blockb_timecourse.py` does the
  same for the video runs (`authored_results_*.csv`) — per run: hit rate / median hit RT /
  RT-CV in the early vs late half of the clip (split at median target onset), RT-drift
  slope, false alarms per half; then a **difference-in-differences fatigue contrast**
  per set: (late−early) under Filter minus under NoFilter. Within a set both arms see
  IDENTICAL targets, so half-difficulty cancels — read the contrast, never the raw
  late-half drop (late targets are different targets). Dedupes the 2026-07-22 twin-CSV
  runs; ⚠ probe/window/dim changed across days, so only contrast same-era runs (date is
  in the filename).
- **Pid-66 typo fixed & merged into P6 (2026-07-23):** participant 6's two video blocks ran
  under mistyped pid 66. Corrected `_P6_` copies (pid column rewritten) are canonical in
  `raw/`; untouched originals are in `raw/mislabeled_p66_20260723/` with a README. A third
  partial NoFilter-A file (15:04) is an ABORT, not exposure: the participant felt uneasy
  and removed the headset before starting; the video played on unattended — do not score,
  and no practice-effect concern for her real 17:06 NoFilter-A run. No PLAN row for 66
  existed → counterbalancing unaffected. Device files left untouched. P6's full dataset:
  Block A browser CSVs (P6) + passthrough BLOCK (pid 6) + the two relabeled video runs.
- **Day-2 results + exclusions (2026-07-23, participants P6/P9/P10):**
  - **P9 is EXCLUDED from Block B** (experimenter decision, same day): they were confused
    about what to click during their first video run (Filter-B, their first-ever block —
    V-F order). Their Block A data stays. Log this as a task-comprehension exclusion rule:
    first-block confusion reported by the participant/experimenter → drop that
    participant's Block B pair.
  - **OPEN ITEM — define a false-alarm exclusion criterion.** P3's Filter-B run has 539
    false alarms (near-constant trigger pulling; hits partly luck). Pooled results barely
    move with/without P3, but the dissertation needs a pre-stated rule (e.g. FAs > k×
    target count → exclude run). Decide k with John before the freeze.
  - **First real Block B signal (n=7, P1–P4/P33/P6/P10, P9 excluded):** in the 10–30°
    band the filter arm beats no-filter in 6 of 7 participants; pooled 56% vs 45%
    (+11 pp; overall 62% vs 57%). Holds in both filter-first and filter-second subgroups
    (so not a warm-up artifact) and survives dropping P3. Caveat: pilot instrument
    changed between days; directional evidence, not a testable claim yet.
  - **P9 Block A hygiene note:** both their main runs were entered as slot A3 → same seed
    → identical shape sequence twice; treat the second (FILTER, 100%) with suspicion.
    Use distinct slots per participant (tool could enforce — not built).
  - Block A v3 (140×1.8 s) still at ceiling for P9/P10 (95–100%, RT ~500–690 ms, no
    drift): pace is not the lever; discuss harder discrimination (confusable shapes /
    2-back) with John.
- Next: rebuild → PILOT1003 NoFilter + Filter with the rope, check the 60–85% band and RTs vs
  the yellow anchors (57–84%, ~1.8 s).
- **Person engine: closeness-widened cone (2026-07-22, user design).** The fixed 12.5°/25°
  person cone dropped pedestrians at their NEAREST moment (same person slides outward in the
  view as the car approaches — ecc grows exactly as they get closest). New
  `m_personCloseWideningDegPerDeg` (now **2.5** after two user tightenings — 5 and 3 were
  visually noisy in city footage): both the accept limit and the full-strength band widen per
  degree of box height above the 3° minimum — 3° box (~32 m) unchanged 25°, 6° (~16 m) → 32.5°,
  10° (~10 m) → 42.5°. Geometrically ≈ a constant lateral corridor in metres
  (x ≈ 1.7 m × angle/boxHeight). 0 = old fixed-cone behaviour. Affects SignPop highlighting
  only — the probe pool is hand-authored, so A/B sets are untouched.

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
- **AutoSession (added 2026-07-20, scene default):** `m_sessionMode = AutoSession` runs one full
  participant session — 4 X-gated blocks with rest in every gap: video filter + video no-filter
  (opposite sets) and Block A no-filter + filter, condition order ABBA-mirrored. Assignment
  (which set gets the filter × which pair/condition starts, cells `AF/BF × V-F/V-N/P-F/P-N`) is
  **greedy-balanced from the on-device `session_ledger.csv`** (a name `awipe` never deletes),
  pid rotation breaking ties. Ledger rows: `PLAN` (one per participant at session start:
  pairing+order) and `BLOCK` (one per completed block — video pass end writes it automatically;
  **Block A blocks complete on X-press**, they produce no CSV of their own). App relaunch
  resumes mid-session from the ledger; **pid −1 auto-assigns the next id**, so one build serves
  every participant (relaunch per participant, no rebuild). **Pids ≥ 900 = experimenter pilots**
  (the ClickProbeTest 999 convention): full session flow, files named `PILOT<pid>`, but excluded
  from the balancing and from auto-assignment — rehearse the auto flow with pid 999 without
  occupying a real counterbalance cell. Manual mode = the override (redo a
  block for a given pid: its BLOCK row marks it complete for the auto resume). Pull the ledger
  along with results when archiving.
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
- **Pull results to PC:** ONE CLICK in Unity — **Meta > Study > Pull Study Data From Headset**
  (`Assets/Editor/StudyDataPuller.cs`): pulls all `authored_results_*`/`authored_points_*` +
  `session_ledger.csv` into `Dissertation/authored/raw/` (size-match skip), then runs
  `Tools/analysis/authored_report.py` and prints the aggregate to the Console. Never deletes
  from the device. CLI equivalent: `.\Tools\study-console.ps1 apull`; `awipe` (deliberate,
  separate) deletes device copies ONLY where byte-size matches the pulled local file — note
  awipe does not touch the ledger.
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
