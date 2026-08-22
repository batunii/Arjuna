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
- **Block A v4 (2026-07-27, ahead of the day-3 participants):** v3's ceiling (95–100%)
  plus participant reports of SEQUENCE exploits (after a repeat the next answer was
  certainly NO — the generator forbade consecutive targets; droughts made repeats feel
  "due"; 30% base rate made default-NO 70% correct) → decision-difficulty +
  anti-predictability pass (140 trials, SOA 1.8→**2.0 s** / window 1.5→**1.7 s** = 4:40 —
  decided against literature anchors after the author self-pilot's median hit 812 ms with
  the slowest answer at the exact 1500 ms edge; field-standard n-back window is 3.0 s,
  SART 1.15 s is a detection-task pace floor only, tight windows censor the ex-Gaussian-τ
  slow tail where distraction lives and deflate d′ via criterion shift — full note:
  `Dissertation/blocka-timing-research.md`): 6 shapes in 3 look-alike pairs
  (square/diamond, pentagon/hexagon, circle/ellipse), non-repeats show the previous
  shape's twin at p=0.6 (logged `;lure=1`; results split FAs lure vs non-lure), 50%
  repeat rate (forced-choice base rate), memoryless per-trial coin-flip sequence (caps:
  ≤3 consecutive repeats, ≤8 drought; count ±7; verified over all pid≤60 seeds — 43% of
  repeats follow a repeat, exploit dead). Payload `task=nback1_lures;gen=v4` is the
  version distinguisher (v4 shares v3 timing!) — `blocka_timecourse.py` now appends
  "+lures" to the version key so v3/v4 never pool. Practice criterion 0.90→0.80
  (comprehension gate, not performance bar). Tool also gained a **slot-reuse guard**
  (pid+slot already saved on this device → confirm dialog listing free slots; the P9
  same-seed incident made impossible-by-default). **Self-pilot v4 before the first
  participant** — target ~75–90% no-distractor accuracy; if still ≥95, raise lure_p /
  consider 2-back with John.
- **Block A distractor pool third pair (2026-07-27):** four Delhi-protest news Shorts
  (lathi charge / detention / clash footage, 30–47 s portrait, used untrimmed) added to
  `RawFootage/` as `video1[0-3]_protest_*`, then five ad/meme Shorts later the same day
  (`video1[4-8]_*`: 3 retail/perfume ads, a football prank, dogs eating; 11–27 s) → reel
  pair `tiktok_5min_5/6` built over the full 18-source pool (`--long-sources bolly
  --seed 5/6`; regenerated in place the same day BEFORE any PROTEST-stamped run existed,
  so the stamp has one meaning; `_1/_2` NOT regenerated — day-2 NEW content preserved
  byte-identical). CPT tool selector gained "Newest videos (protest + ad/meme clips)" as
  the DEFAULT, stamped `distractors=PROTEST`. Same pooling caveat as NEW|ORIGINAL: runs
  with different `distractors` values don't pool without noting it.
- **Day-3 P12→P13 (2026-07-27):** this participant started as P12 — filter-arm CPT first
  (hand-typed pid **1**, Unity not yet up so no HUD pid to copy; the exact hazard the
  cross-system-id note predicted) then video Filter B — and the app crashed after that
  block. Relaunch auto-assigned **13**; the full redo session (video NoFilter A + Filter B,
  CPT A1 FILTER + A2 NOFILTER, questionnaire) is canonical. P12 fragments + the mistyped
  "P1" CPT file are quarantined in `raw/aborted_p12_20260727/` (see its README). Caveats:
  P13 carries prior exposure (one video-Filter-B + one filter-CPT run before their real
  session); P13's final passthrough-NoFilter BLOCK row is missing from the ledger (app
  relaunched for the next participant before X-advance — CPT CSV itself is complete).
  **P11 was never used** (ledger jumps P10→P12). Day-3 pids are NOT contiguous.
- **Day-3 P14/P15/P16 = ONE participant (2026-07-27):** P14 = video Filter A
  aborted at 3.6 min (PLAN, no BLOCK row). Relaunch → P15 (passthrough-first): CPT
  A3 + A4 ran back-to-back but the browser filter toggle was never flipped for A4 —
  original file says NOFILTER everywhere, yet the ledger BLOCK row (passthrough
  **Filter**, 17:44), the scores (A4: 60 hit/5 commissions vs A3: 54/24) and user
  recollection all say A4 was the FILTER arm. Corrected copy
  (`blocka/blockA_nback1_P15_A4_FILTER_*.csv`: filename + `filter=` payload +
  `dr_intensity` rewritten) is canonical — note `dr_intensity` is NOT independent
  evidence, it mirrors the same hand-set toggle. P15's video Filter B run flooded
  (206 FAs/246 trials) → excluded; their canonical **Block B pair is P16's** forced-arm
  relaunches (NoFilter-B 18:04 + Filter-A 18:35; no PLAN row, both logged slot 0).
  Rejects + original mislabeled A4 quarantined in `raw/excluded_p15_20260727/`
  (see its README, incl. triple-filter-exposure + response-style-swing caveats).
  **Analysis must join P15 (Block A) + P16 (Block B) as one participant**;
  questionnaire is the shared `post-study-questionnaire-15-16.docx`.
- **Day-3 P17 (2026-07-27): clean session, taken as-is.** Video NoFilter-B +
  Filter-A, CPT practice + A1 FILTER + A2 NOFILTER, questionnaire 17. Two footnotes:
  a 1 KB A2 false start (restarted 23 s later, same seed 171) is quarantined in
  `raw/aborted_p17_a2_20260727/`; and like P13, their final passthrough-NoFilter BLOCK
  row is missing from the ledger (app closed before X-advance — CSV itself complete).
- **Versions POOL now (2026-07-27, John, verbal — supersedes the 2026-07-23
  never-aggregate rule):** the pilot versions came out close enough to report as a
  single section. Pooled run 2026-07-27: Block A = 21 runs P1–P17 (day-1 84×2.5 files
  have NO filter label → they join difficulty/time-course only, not the arm contrast;
  8 byte-identical day-1 re-downloads moved to `raw/duplicates_20260722/`); Block B =
  10 canonical pairs (P9 excluded per day-2 rule, aborts/rejects excluded). Headlines:
  Block A filter +4.5 pp mean paired gain (4/6 pairs positive, all of it from the two
  non-ceiling participants P6 +7.7 / P15 +16); Block B filter 60% vs 51% pooled hits
  (+9 pp mean paired) BUT concentrated in FA-heavy/confused runs (P3 539-FA run, P16,
  P17's 18% first-block NoFilter) — clean pairs ≈ 0; no vigilance decrement in any of
  21 CPT runs. `blocka_timecourse.py` default (per-version) left unchanged.
- **Day-1 Block A filter state RECOVERED from the ledger (2026-07-27):** the 22 July CPT
  files predate both the `filter=` payload and the `dr_intensity` column, so they parsed
  as UNKNOWN and were excluded from every arm contrast. Recovered by matching each run's
  `t_ms` span against that pid's passthrough BLOCK rows (BLOCK is written at block
  COMPLETION, so the run that ends just before a BLOCK row sits inside it). Unambiguous
  matches — block row lands **16/18/19 s** after the CPT run ends, the same signature as
  day-3 sessions where the arm was stamped directly: **P2 A3=FILTER, A4=NOFILTER; P3
  A1=NOFILTER, A2=FILTER; P33 A3=FILTER** (A4 has no BLOCK row → still unknown). **P1 and
  P4 NOT recoverable** — both passthrough BLOCK rows were logged BEFORE their CPT runs
  started (blocks X-advanced without the CPT inside), so no window contains them.
  Block A paired n: 6 → **8**; both new pairs favour Filter (+2.4, +3.6). Recovery is
  analysis-side only — the CSVs were NOT rewritten; the mapping lives here and in the
  report script. If you later bake it in, use corrected copies + quarantine per the
  P66/A4 convention.
- **Pooled inferential results (2026-07-27, for John's point 1).** Block A (n=8):
  Filter 96.1% (SD 4.4) vs NoFilter 92.0% (SD 8.4); mean paired diff **+4.05 pp**
  (SD 5.83, median +3.0, IQR +1.3..+5.7), 95% CI **−0.82..+8.92**, t(7)=1.97 **p=.090**,
  Wilcoxon W=5.0 **p=.078**, **dz=0.70**, 6/8 positive, per-day +3.0/+5.0/+3.8.
  Power at n=8 = 40%; **n=19 needed for 80%** (study plans N=20). Block B (n=10):
  +9.00 pp, SD 15.73, 95% CI −2.25..+20.25, t(9)=1.81 p=.104, Wilcoxon p=.184, dz=0.57,
  5/10 positive; **sensitivity excluding the 3 high-variance runs (P3 539-FA, P16, P17
  first-block): mean exactly 0.0 pp, CI ±5.97** — report the sensitivity check alongside
  the headline, it is the first thing a reader will ask for. Filter also HALVES
  between-participant SD in both blocks (A 8.4→4.4, B 17.4→5.9).
- **UPDATED pooled results at n=10 (2026-07-27, user chose the wider day-1 set).** Block A now
  uses 4 day-1 pairs: P2/P3 ledger-verified + **P33/P4 from `analysis-2026-07-22.md` §6
  plan-order assignment** (P4's NoFilter 98.4 is experimenter-reported, NO exported file —
  flag this in the write-up). n=10: Filter 96.8% (SD 4.2) vs NoFilter 93.2% (SD 7.9), mean
  **+3.51 pp** (SD 5.26, median +2.30, IQR +1.30..+4.65), 95% CI −0.25..+7.27, t(9)=2.11
  p=.064, **Wilcoxon W=8.0 p=.0488 — SIGNIFICANT**, dz=0.667, **8/10 positive**, power .47,
  n=20 for 80%. Strict n=8 subset = +4.05, Wilcoxon p=.078 (report as sensitivity).
- **Block B BAND ANALYSIS across all 3 days (2026-07-27) — the mechanism result.** 330 scored
  targets joined to `pool_split.csv` on (set, point_id); bands by `eccentricity_deg`.
  Pooled trial-level: <10° 67.5→80.0 (+12.5); 10–20° 60.9→57.4 (−3.5); **20–30° 19.2→50.0
  (+30.8)**; >30° 75.9→91.1 (+15.1). The 20–30° gap survives EVERY subset — day1 +27.6,
  days2-3 +34.0, trusted-7 +30.4, ≥2 s targets +20.5. **Caveat: only 2–3 targets per pid per
  arm in that ring**, so per-participant rates are 0/50/100% and it cannot be tested — the
  testable version is **10–30°: +10.56 pp (SD 17.06), 95% CI −1.65..+22.76, t(9)=1.96 p=.082,
  Wilcoxon W=6.0 p=.0547, dz=0.62, 7/10 positive**. ACTION for next pool: put more targets in
  20–30°. Duration cliff (all days): ≤1.5 s **10%**, 1.5–3 s 46%, 3–8 s 61%, >8 s 90% — day 1
  carries half the ≤1.5 s trials (pool re-screened to ≥2 s after day 1). Kind: traffic lights
  32→52%, persons 73→81%.
- **ESQ re-extracted for ALL 9 participants (2026-07-27).** Answers live in the docx table's
  THIRD column (participants overwrote the Key cell) — parse `word/document.xml` tables, rows
  where col0 matches ^[ABC]\d. Re-extraction **reproduces `esq-day1-analysis.md` exactly**
  (A 19/20, B 9/20, C 14/16) → method validated. All nine: **A 42/45, B 27/45, C 27/36**;
  **A1/A2/A3 unanimous 9/9**. **B3 ("noticed side events later") flipped: 4/4 day-1 reported a
  delay vs only 1/5 on days 2–3** — tracks the settings freeze; frame as observation, not a
  controlled comparison, but it makes perceived awareness cost a tunable design parameter.
  Section D was never administered (keys still show +/(R)); C3 never asked.
- **CORRECTION (2026-07-28): the band analysis above joined the WRONG POOL FILE.** Two
  `pool_split.csv` exist: `authored/pool_split.csv` (82 = A40/B40+2P) is the one the RUNS LOAD —
  verified by checking every run's target ids against both files. `authored/raw/pool_split.csv`
  (58) is an earlier draft split; only **33 of its 58 (set,point_id) keys** match the 82-pool
  (those 33 agree exactly), so the join analysed **330 of 800** scored targets and credited some
  targets with a *different* point's angles. **Always join `authored/pool_split.csv`.**
  Corrected band table (n=10, both arms, mid-lifetime position): <10° 75.0→76.8 (+1.8, n=64/56);
  10–20° 47.8→49.3 (+1.5, n=138/142); **20–30° 41.0→58.5 (+17.5, n=78/82)**; >30° 60.8→59.2
  (−1.7, n=120/120). Single clean peak; ~8 ring targets per participant per arm (NOT 2–3 — that
  caveat was an artifact of the bad join, so the ring IS testable per participant).
  Per-participant **20–30°: +17.78 pp (SD 25.12), CI −0.2..+35.7, t(9)=2.24 p=.052,
  Wilcoxon W=8.0 p=.0469 SIGNIFICANT, dz=0.71, 8/10, power .52, n=18 for 80%**.
  10–30° dilutes to +7.50 (p=.264); overall Block B is NULL: +3.25 (p=.553, 4/10).
  Duration cliff recomputed: ≤1.5 s **19.2%** (off 11.7 → on 26.7), 1.5–3 s 32.6, 3–8 s 60.0,
  >8 s 86.7. Kind: traffic lights 33.6→41.7, persons 66.9→67.2.
- **Mechanism sharpened: the effect is BRIEF targets IN the ring.** 20–30° split by duration —
  **<2 s: 6.2% (2/32) → 32.1% (9/28), +25.9**; ≥2 s: 65.2→72.2 (+7.0). Brief targets in OTHER
  bands gain only +4..+10, so it is not a general short-target effect. This is why the ≥2 s
  sensitivity cut collapses the ring to +7.1 — it removes the cells where the filter can act.
  ACTION for next pool: keep short (<2 s) targets **in the 20–30° ring specifically**; the ≥2 s
  re-screen was removing the evidence. Day 1 +25.4 vs counterbalanced days 2–3 +12.7 (day-1 set
  confound is real but does not create the pattern).
- **P3 Block B EXCLUDED (2026-07-28), Block A retained.** P3's FILTER-B run logged **539 false
  alarms** (vs 18 in its baseline) and its hits are provably looser than every other filter run:
  median angle_deg 1.82 vs 1.59, p90 **8.62 against the 10° acceptance radius**, Mann-Whitney
  **p=.0301** → some hits were accidental. Excluded from Block B only; its CPT pair shows no
  such pattern (3 commission errors) so Block A stays n=10 and keeps Wilcoxon p=.0488.
  Dropping participants WHOLESALE would destroy Block A (drop P3+P15 → +1.96, W p=.195), and
  there is no evidence against P15 (52 FAs but hits *tighter* on filter: 2.08 vs 2.63) → P15
  KEPT. P3's Block B diff was +32.5 pp, so the exclusion works against the reported result.
  Block B set is now every pair except P3 (n=10, includes P1 — its arms are in the filenames
  even though its CPT arm is unrecoverable).
- **Block B false alarms are a real cost, and they run OPPOSITE to Block A.** Per run: off 5.9
  vs on 11.5, 7/10 pressed more (t p=.199, W p=.121; with P3 in, W p=.023). Block A commission
  errors FALL with the filter. Not a contradiction — CPT responses concern one central stream
  (dimming removes the impulse trigger) while Block B's filter pulls attention outward to things
  worth pressing at. Consequence: **response bias cannot be separated from sensitivity** in
  Block B. ACTION: log the aim direction of `false_alarm` rows — `AuthoredTargetPresenter.Log`
  currently writes `ang=-1` with no az/el, so wrong presses cannot be placed in a band at all.
- **Press-time angle reconstruction (2026-07-28, new method).** Targets MOVE — each is a tracked
  detection whose box is interpolated by `PosAt`; `az_mid_deg/el_mid_deg` is only PosAt(tMid).
  Median lifetime swing **17.2°**; 45/82 cross a band boundary; **31% of catches land in a
  different band than the target's mid position** → the pool column is a weak proxy. Rebuild
  method: bake `Builds/StudyVideo/study_video.detections.json` (0.5 s samples, shipped
  2026-07-18 — NOT `DevVideos/study_video.detections.json`, which is the older 420 s bake),
  map box→az/el with `BoxToAzElRectEQ` (scene values uOffset=0, vOffset=0, yoloFlipY=1 →
  az=(u−0.5)·360, el=(0.5−v_yolo)·180), anchor on the pool's mid position via the two bracketing
  samples, then nearest-same-class walk (1.5× box size). **Validation: reproduces authored
  az/el exactly for 75/82 targets** (median error 0.000°, independent check on box_height_deg
  0.000°); 7 targets differ >2° and are a reported sensitivity subset (+19.6 in the ring).
  Result: catches made while the target sat in 10–30° = **92/219 (42.0%) off vs 114/232 (49.1%)
  on**; median angle at press barely moves (17.5→18.9) → people are not reaching further out,
  they are converting more of what was already out there. Median RT 2.04→1.88 s.
  Scripts live in the session scratchpad (`click_bands.py`, `final_numbers.py`) — NOT yet in
  `Tools/analysis/`; port them before relying on this again.
- **P20 (day 4, 2026-07-28) — BOTH headline results now significant on BOTH tests.** Clean
  session: one PLAN + four BLOCK rows, no aborts, no duplicates. **First session with the arm
  order ALTERNATED — video FILTER ran first** (BLOCK 2 Filter/A 19:22:29, BLOCK 3 NoFilter/B
  19:31:35), so practice worked AGAINST the filter and it still won both blocks. Arms confirmed
  twice: CPT run spans sit inside the matching passthrough BLOCK windows (A3 18:41:51–18:46:34 →
  BLOCK 0 NoFilter; A4 18:57:09–19:01:52 → BLOCK 1 Filter) AND `dr_intensity` 0 vs 1.
  NOTE: CPT filenames are UTC (`...Z`), ledger/`t_ms` are local (UTC+1) — do not compare directly.
  Block A (A3/A4 = the HARD version, well off ceiling): **86.33 → 91.37 (+5.04)**, commissions
  10 → 6, misses 7 → 4; within-run NoFilter faded −7.04 while Filter improved +3.97.
  Block B: **22.5% (9/40) → 32.5% (13/40)**, FA 26 → 23, bands <10 +12.5 / 10–20 +12.3 /
  **20–30 +27.0** / >30 0.0. Absolute rates are the LOWEST in the set — flagged in the report;
  ask what was different about that session if it matters.
  **Group at n=11 — Block A: +3.65 pp, CI +0.28..+7.02 (excludes zero), t(10)=2.42 p=.0364,
  Wilcoxon W=8.0 p=.0244, dz=0.73, 9/11, power .59, n=17 for 80%. Block B 20–30° ring: +18.61 pp,
  CI +2.5..+34.7, t(10)=2.57 p=.028, Wilcoxon W=9.0 p=.0303, dz=0.78, 9/11, power .64, n=16.**
  Block B overall still NULL (+3.86, p=.44, 5/11); 10–30° +8.48 (p=.23). Bands n=11: <10 +4.2,
  10–20 +2.1, **20–30 +18.2**, >30 −1.5. Hard corner holds: 20–30° & <2 s **5.6% (2/36) → 30.0%
  (9/30)**, vs +4.3/+3.3 for brief targets in the other peripheral bands. FA cost weakened to
  7.7 → 12.5 (p=.22) because P20 pressed heavily in BOTH arms.
- **ESQ re-extracted for ALL 10 (2026-07-28) — and the P20 docx CHANGED between reads.** The
  first read happened while Word still held the file (`~$` lock present) and caught it mid-edit;
  7 of 14 items differed from the saved version. **Rule: never parse a .docx while its `~$` lock
  file exists.** Authoritative totals: **A 47/50, B 29/50, C 30/40** (total 106/140). Excluding
  P20 reproduces the published 42/45, 27/45, 27/36 exactly → method still validated.
  **A1/A2/A3 unanimous 10/10.** Section C scores FOUR items (C1, C2, C4, **C5**) — C5 was always
  in the total, which is why 27/36 = 9×4; **C3 is answered by nobody** (all ten left the "+" key
  showing) so it is omitted from the figure. B3 by day: 4/4 day-1 reported the delay vs **1 of
  the 6 tested on days 2–4** — the day-1 flip strengthens rather than dissolves.
- Report republished at the same artifact URL (n=11). Figures with per-row content are now
  generated by scratchpad scripts (`regen_figs.py`) rather than hand-authored — port those to
  `Tools/analysis/` alongside `click_bands.py` if the report is to be maintained.
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
