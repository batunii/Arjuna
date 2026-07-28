# Block A Pilot Tools

Standalone, self-contained web pages for Block A's workstation task — no headset, no Unity
build. Same idea as `TabletApp/distractor-reel.html`: no install, no build step, runs
identically in any browser (desktop, phone, or tablet).

**The Block A study task (decided 2026-07-22) is `block-a-cpt.html`: forced-choice 1-back
with real-footage video clips as the side-margin distractors.** The tool used to also offer
a Go/No-Go CPT mode (the original spec), a respond-with selector (CPT-only), and a
procedural-reel distractor option — all removed the same day so the tool runs exactly one
configuration: the study task. The filename keeps the historical `cpt` name so existing
references (HANDOFF, the Unity Block A HUD, git history) stay valid.

| File | Task design | Status |
|---|---|---|
| `block-a-cpt.html` | Forced-choice 1-back: one shape at a time, answer YES/NO on every shape — same as the previous one? Video-clip distractors in the side margins. | **The Block A study task.** (`testing-strategy-v2.md` §4.1 still describes the old Go/No-Go CPT and needs updating.) |
| `block-a-multitarget.html` | Multi-target visual search: 3-5 shapes on screen at once, click the circle among square decoys, all shapes respawn on any click, runs for a fixed 3.5 min session. | **Exploratory only** — not in the spec, no Unity implementation exists. Built to pilot-test the idea before deciding whether to adopt it. |

## Running `block-a-cpt.html`

Open it in any browser (serve via a local static server if `file://` iframes are blocked —
see the bottom of this doc). No other setup.

1. Enter a participant ID and pick **Practice** (40 trials) or a condition slot **A1-A4**
   (**v4, 2026-07-27: 140 trials at 2.0 s SOA / 1.7 s answer window** = 4:40, six
   look-alike shapes, 50% repeats, memoryless sequence; see the v4 section below. The
   window was re-derived after the lures moved RTs up (author self-pilot median 812 ms
   vs naive 500–690 ms on v3) — anchors and rationale in
   `Dissertation/blocka-timing-research.md`. History: v1 = 84 × 2.5 s, v2 = 105 × 2.0 s — v2 was the ceiling fix
   after all Day-1 naive runs scored 96–100%; v3 (2026-07-23) restored the original
   spec's 140-trial count and trimmed the SOA to 1.8 s — and Day-2 naive runs STILL
   scored 95–100% (RT 500–690 ms, flat), proving the pace lever exhausted; v4 attacks
   the discrimination and the sequence statistics instead, and widens the window
   (1.5→1.7 s) so difficulty comes from the decision, not the deadline — a tight window
   censors the slow-RT tail where distraction effects live (ex-Gaussian τ) and turns
   d′ into a speed-accuracy-criterion measure.
   **Versions must never be aggregated** (supervisor directive 2026-07-23); the
   run-start payload's `trials`/`soa_s`/`task` fields distinguish them — v4 shares v3's
   timing, so `task=nback1_lures` is its distinguisher. A **slot-reuse guard** (v4) warns
   when a pid+slot pair already has a saved run on this device — same seed = the identical
   sequence again (the P9 incident); it lists the free slots and requires an explicit
   confirm to override.)
2. Every shape asks "same as the previous one?" — YES (`Y` / `←` / `1` or the left button),
   NO (`N` / `→` / `2` or the right button). The first shape is memorize-only.
3. After the run: on-screen stats (repeat-hit rate, false alarms — split lure vs
   non-lure in v4 — no-responses, overall accuracy, mean/median hit RT, d-prime, and —
   for Practice — the ≥ 80% criterion pass/fail (v4; was 90%)), plus **time-course rows**
   (2026-07-23): accuracy / median correct RT /
   RT-consistency (CV) split into run thirds, and an RT-drift slope in ms per minute —
   the at-a-glance "were they slowing down / getting erratic late in the run" view.
   A **Save CSV** button re-exports the log. For the cross-run FILTER-vs-NOFILTER
   comparison of the same metrics, run
   `python Tools/analysis/blocka_timecourse.py` (defaults to
   `Dissertation/authored/raw/blocka/`; groups by version, never pools them).

## What matches the Unity study config

- Seed formula: practice seed 999, main seed = `pid*10 + slot` (slot 0-3 for A1-A4) — the
  same formula `ConditionSequencer.cs` uses. (Main trial count is 84 here vs
  `ConditionSequencer.cs`'s `k_cptTrials` = 140 — that constant belongs to the old CPT's
  1.5 s SOA and is not used by this task.)
- Outcome categories and event names (`CPT_ONSET`, `CPT_RESULT` with `hit`/`miss`/
  `commission`/`correct_reject`, `CPT_RUN_START`/`END`, `PRACTICE_START`/`END`) match
  `StudyLogger`'s payload conventions.
- Exported CSV uses the exact same header as `StudyLogger.cs`
  (`t_ms,pid,block,condition,yaw_deg,pitch_deg,roll_deg,head_speed_dps,dr_intensity,mode,event,payload`).
  Head-pose columns are blank (no VR headset here); `mode` is stamped `NBACK1_PILOT` so this
  can never be mistaken for a Unity-logged session if it ever ends up near that data.

## Historical: CPTPanel.cs likely always falls back for main runs

(The Go/No-Go CPT mode was removed from this tool 2026-07-22, but `Study/CPTPanel.cs`
still exists in Unity, so this finding stays recorded.) `CPTPanel.BuildSequence` retries
up to 2000 times to find a sequence satisfying BOTH "no two consecutive no-gos" AND
"go-runs ≤ 8" before giving up and using a fixed, seed-independent evenly-spaced fallback.
Measured empirically: for 140 trials / 28 no-go (the main-run size), satisfying both
constraints together takes on the order of **~75,000 random attempts on average** (highly
variable — sampled range was ~700 to ~275,000 across 20 seeds). 2000 attempts finds a real
match only **roughly 2-3% of the time** — so as shipped it most likely falls back to its
fixed pattern for nearly every 140-trial run, silently defeating the intended per-session
randomization. If `CPTPanel.cs` is ever used, raise the cap (`attempt < 2000` → e.g.
`attempt < 1_000_000`; ~100ms, runs once per `BeginRun`).

## The 1-back task (added 2026-07-20; adopted as the study task 2026-07-22)

Answer **YES ("same as the previous shape") or NO ("different") on every shape**, via
on-screen buttons or the Y/N keys. Built after supervisor feedback (2026-07-17, point 6)
that the Go/No-Go CPT is bottom-up/reactive; the 1-back adds a working-memory/
goal-maintenance component while keeping the trial engine (trial counts, seed formula,
CSV schema, stats). Details:

- **Forced choice, not go/no-go** (changed same day after first hands-on feel: watch-and-mostly-
  do-nothing felt wrong): an answer is expected on every scored trial. Outcomes: `hit` (YES on
  repeat), `miss;answered_no` / `miss;timeout` (repeat missed), `commission` (YES on non-repeat),
  `correct_reject` (NO on non-repeat), `no_response` (non-repeat timed out — counted separately
  as an engagement/erraticness signal, which also serves feedback point 5b's outlier-detection
  ask). The first trial has no predecessor, so it is shown to memorize and logged
  `first_unscored`.
- **No per-trial right/wrong feedback** (removed 2026-07-20 at the user's call — also the
  methodologically sound choice: trial-by-trial correctness feedback shifts speed/accuracy
  strategy mid-run). The answer still visibly *registers* — chosen button outlined neutral grey,
  both buttons locked, status line says "Answer recorded" — and "⏱ Too slow" still shows
  (pacing compliance, not correctness). Correctness lives only in the CSV and the end-of-run
  stats. If this graduates and practice needs learnable feedback, re-enabling it for practice
  runs only is a small conditional.
- **1-back timing differs from the CPT** (fixed 2026-07-20 after hands-on feel — with the CPT's
  1.5s SOA and full-SOA response window, a late answer landed on the *next* shape and was scored
  against a stimulus the participant hadn't processed): SOA 2.0s, answer window 1.7s (v4,
  2026-07-27 — timing history: 2.0 → 2.5 ("still a little hard", 2026-07-20) → back to 2.0
  as the v2 ceiling fix once Day-1 naive medians came in at 567–776ms → 1.8 in v3 (the 1.5s
  window spanned 1.9–2.6× those medians) → 2.0 in v4, because the lures moved RTs up
  (author self-pilot median 812ms, slowest answer at the exact 1500ms edge) and the same
  ≥~2×-median-RT headroom rule then demands a wider window. Full literature evaluation in
  `Dissertation/blocka-timing-research.md`: the field-standard n-back window is 3.0s
  (500ms stimulus + 2500ms ISI), so v4 stays on the fast side of convention; SART's 1.15s
  (Robertson et al. 1997) is a *detection*-task pace floor, not a discrimination-window
  anchor; and a tight window censors the slow-RT tail (ex-Gaussian τ) where
  lapse/distraction effects live while deflating d′ via a speed-accuracy criterion shift).
  Main runs are 140 trials = 4:40. Answers accepted 0.25–1.7s after onset. The shape stays
  visible for the whole 1.7s window (hiding it at the CPT's 700ms while the timer kept
  running read as a glitch); the last 300ms of each trial is a closed-window gap so late
  answers can't spill onto the next shape. Clicks before 0.25s are ignored as spillover
  from the previous trial.
- **Shapes & sequence (v4, 2026-07-27 — the decision-difficulty + anti-predictability
  pass).** v3 kept 4 maximally distinct shapes at a 30% repeat rate with a
  no-consecutive-targets gap-sampled sequence, and participants both stayed at ceiling
  AND reported exploiting the structure: after a repeat the next answer was certainly NO,
  long droughts made a repeat feel "due", and default-NO was 70% correct. v4 changes all
  three:
  - **6 shapes in 3 look-alike pairs** — square/diamond (rotation twins),
    pentagon/hexagon (side count), circle/ellipse (aspect). On a non-repeat trial the
    previous shape's twin appears with p = 0.6 (a **lure**, logged `;lure=1` on the
    onset row); other non-repeats draw uniformly from the 4 non-twin shapes. "Different"
    now takes a fine discrimination, not a glance; the pair similarity IS the difficulty.
  - **50% repeat rate** — the correct base rate for a forced-choice same/different task
    (30% is go/no-go n-back lore); guessing any fixed answer earns chance. ~70 repeats /
    140 also tightens d′ further (sampling variance scales with inverse signal/noise
    counts, Macmillan & Creelman 2005).
  - **Memoryless sequence** — per-trial coin flips (verified: 43% of repeats directly
    follow another repeat), forcing only at the caps: max 3 consecutive repeats (≤ 4
    identical shapes in a row), max 8-trial drought (rarely binds at 50%), repeat count
    accepted within ±7 of nominal (converges in ≤ 4 draws across all realistic seeds).
  Trial 1 is never a repeat (nothing to repeat).
- CSV `mode` column is stamped `NBACK1_PILOT` (kept from when the tool had two tasks, so
  existing analysis keyed on it keeps working), the run-start payload carries
  `task=nback1_lures;gen=v4;target_p=0.5;lure_p=0.6` (was `task=nback1` through v3),
  onset payloads add `;shape=...` (+ `;lure=0|1` on non-repeats), and the exported
  filename says `nback1`.
- **The screen narrates itself** (2026-07-20, after "even knowing the rules I can't tell what
  to do"): a large status line above the panel always states the current ask or state —
  "MEMORIZE this first shape", "Same as the previous shape?", "Answer recorded", "⏱ Too slow" —
  a time bar under the panel drains over the answer window, and a neutral running tally
  (answered / too slow) sits under the trial counter.
- **Controls:** YES = `Y` / `←` / `1` or the left button; NO = `N` / `→` / `2` or the right
  button. `Esc` ends the run at any time (the on-screen button says "End run (Esc)"); an
  ended run still shows its stats and offers the CSV, flagged `CPT_RUN_ABORTED` in the log.
- Results add `No response (timed out)` and `Overall accuracy` rows — overall accuracy is the
  number to watch against the ~75–90% no-distractor band.
- The practice criterion is ≥ 80% repeat-hit rate (v4; lowered from the old CPT's 90%
  because with look-alike lures a participant who fully understands the task can sit
  below 90% — the criterion is a comprehension gate, not a performance bar). Target
  main-run band remains ~75–90% no-distractor overall accuracy; self-pilot v4 before
  real participants to confirm it lands there.
- **No Unity implementation exists — and none is needed:** Block A's task runs in the browser
  at the workstation (the Unity Block A HUD surfaces the pid AND the arm's filter state to
  type into this tool). Adopting the 1-back as the study task still means updating
  `testing-strategy-v2.md` (§4.1 describes the old Go/No-Go CPT) and re-checking the stats
  plan (the DV stays signal-detection, so `h1_lmm.py` needs little rework).
- **Mode tie-in (2026-07-23):** setup requires a **headset filter state** selection for main
  runs (copy it from the Unity HUD, which shows it at block start). It is stamped into the
  CSV's `dr_intensity` column (`1` = filter on, `0` = off, empty = not recorded), the
  run-start payload (`;filter=FILTER|NOFILTER`), the results table, and the filename
  (`blockA_nback1_P4_A3_FILTER_<stamp>.csv`) — each run is self-describing like the Block B
  CSVs; the ledger time-window cross-check remains the independent verification.
- **Autosave + recovery (2026-07-23):** every trial rewrites the full CSV into
  `localStorage` (`blockA_savedRuns_v1`, last 24 runs), the CSV **auto-downloads at run
  end** (the Save button is a re-export), and the setup screen lists all saved runs with
  Download/Delete — a crash or missed export costs at most the in-flight trial. Re-exports
  keep the run's original filename (stamp fixed at run start), so duplicate files are
  byte-identical, never mistaken for extra runs.
- **Window-setup stage (2026-07-23):** Start no longer launches trials. The task screen
  first shows the 540×540 panel as a dashed-outline dummy ("set your focus window around
  this frame") so the participant paints the headset window against the real task geometry;
  trials begin on the Begin button / Enter. `CPT_PANEL_PLACED` is logged at Begin, so
  window-painting time never contaminates trial timing.

## Caveats (read before drawing conclusions from the data)

- **PRNG note.** The trial sequence uses a JS `mulberry32` seeded PRNG, not .NET's
  `System.Random` — irrelevant now that the task has no Unity counterpart, but recorded in
  case one is ever written: the same seed would not reproduce this tool's literal sequence.
- **Fixed pixel sizing.** The shape is a fixed on-screen pixel size; achieved visual angle
  depends on monitor size and viewing distance.
- **This is only the task, not the full Block A protocol.** The vignette conditions come from
  the headset (configured by the Unity Block A launcher); this page contributes the central
  task plus the embedded side-margin video-clip distractors. The embedded margins are NOT a
  controlled ±35° placement — see the eccentricity caveat below.

## `block-a-multitarget.html` — the exploratory multi-target design

Built on request to pilot a different idea: instead of one shape appearing at a fixed
1.5 s cadence, several shapes (one circle = target, the rest squares = decoys) are visible
on screen at once. Click the circle: correct (hit), logged with RT from when that round's
shapes appeared. Click a square: an error. Either way, all shapes immediately respawn at
new random positions with a fresh random target — continuous, self-paced (not
experimenter/timer-paced like the CPT), for a fixed 3.5-minute session (matching Block A's
condition length). Clicking empty space does nothing (not logged, doesn't respawn).

A **Difficulty** selector (Easy/Medium/Hard) on the setup screen controls shape count and
size — Easy (3-5 shapes, 70px) was too easy to feel like real visual search in initial
testing, so **Hard** (6-9 shapes, 42px) is the default. Tune `DIFFICULTY_PRESETS` in the
script directly if you want a different combination.

This measures a different construct than the sequential CPT: continuous visual-search
throughput and click accuracy under distraction, rather than sustained-attention/vigilance
lapses via discrete-trial signal detection (hit/miss/false-alarm/correct-reject → d-prime).
Both could plausibly speak to "distractor cost," but they're not interchangeable, and
switching the real study to this design would mean also updating `testing-strategy-v2.md`,
writing a Unity equivalent of `CPTPanel.cs`, and rethinking the stats plan (`h1_lmm.py`
currently expects a signal-detection DV) and `Tools/validate_session.py`'s trial-count
check. None of that has happened — this tool exists purely to let the task be *tried* before
committing to any of it.

Same CSV schema as `block-a-cpt.html`, stamped `mode=MULTITARGET_PILOT` (vs. `NBACK1_PILOT`)
so the two are never confused if their exports end up side by side. Same PRNG/visual-angle/
response-device caveats above apply here too — plus there's no Unity implementation at all
to compare against, so "matches the real task" doesn't apply; there is no real task yet.

## Fullscreen

Both tools request fullscreen (best-effort, same pattern as `distractor-reel.html`) when
you click Start — hides the browser chrome so the task fills the screen. Press Esc to exit
(standard browser behavior, not something the page needs to handle itself).

## Distractors: embedded in the fullscreen side margins

In fullscreen, the task itself occupies a fixed-ish central column (`#centerCol`,
same `.screen` max-width as before) — on any screen bigger than that, there's real unused
space either side. `block-a-cpt.html` fills those margins with `<iframe>`s playing the
study's video-clip distractors (`block-a-multitarget.html` does **not** have this margin
wiring).

### Video clips ARE the distractor content (selector removed 2026-07-22)

Each side-margin iframe loads
`video-reel.html?src=../Assets/_Scratch/tiktok_5min_{1,2}.mp4&embedded=1` — a real
pre-rendered fast-cut clip with animated captions, generated by
`Tools/tiktok_captions.py` from footage in `RawFootage/`. See
[Distractor Content Pipeline](<../.agent-docs/systems/distractor-content-pipeline.md>) for
how those clips are built.

**Reel pairs + a set selector (2026-07-23, third pair 2026-07-27):** five Bollywood
music-video clips were added alongside the original four sources (with their cuts held
~2× longer via the generator's new `--long-sources` flag — those reels run ~8 min before
looping), and the setup screen gained a **Distractor videos** selector: *Newest videos*
(`tiktok_5min_5/6.mp4`, the full 18-source pool incl. four Delhi-protest news Shorts
and five ad/meme Shorts
added 2026-07-27 — the default), *New videos* (`tiktok_5min_1/2.mp4`, the 9-source pool)
and *Original videos* (`tiktok_5min_3/4.mp4`, rebuilt from the original 4 sources only).
The side screens stay **dark until the run starts** and go dark again
when it ends (the empty phone frames remain, so no layout jump); the choice is stamped
into the run-start payload (`distractors=PROTEST|NEW|ORIGINAL`), so every run records
what it played.
This is NOT the removed 2026-07-22 content-type selector coming back — both options here
are the study's video-clip distractors, it only picks which reel pair. The side frames
were also bumped a size class (500px/73vh caps, was 440px/66vh) the same day. `video-reel.html` is a minimal sibling to
`TabletApp/distractor-reel.html` — same wake-lock/fullscreen/`?embedded=1` conventions,
just a muted looping `<video>` instead of the procedural canvas.

The setup screen used to offer a **Distractor content** selector (video clips vs the
procedural `TabletApp/distractor-reel.html`, added 2026-07-16 as an exploratory
comparison); with the 2026-07-22 decision the clips are the study's distractor content and
the selector is gone. The procedural reel still exists untouched in `TabletApp/` for
standalone tablet use.

### Layout fix: phones no longer vanish on laptop-width windows (2026-07-20)

The original layout gave the task column `min(96vw, 1100px)` outright, so on any window
narrower than ~1500px the side columns collapsed to their padding and the phones silently
disappeared. The three columns now negotiate via flexbox: the task column shrinks from 1100px
down to a 600px floor before the phones (flex-basis 220px each, width capped at 440px) give up
meaningful width. On a 1366px window each phone gets ~180px; the ±35° eccentricity caveat below
still applies as before. Same fix window: `video-reel.html` now retries `play()` after load
(and on first click as a fallback) — some embedded contexts left the muted autoplay video
paused, showing a black rectangle instead of footage. Also fixed: run timers now carry a run
token, so an aborted run's still-pending 1.5s trial timer (or a double-clicked Start) can no
longer fork a second trial chain that double-paces the next run and scrambles its outcomes.

### Layout: side clips sized/placed like tablets, main task framed (2026-07-16)

Tuned after initial passes felt off in both directions:

- Side-margin videos are capped to a fixed portrait box (`min(34vw, 440px)` wide ×
  `min(66vh, 740px)` tall, matching the clips' own ~0.59 aspect ratio) with a rounded
  border + drop shadow, so each reads as a tablet/phone propped up rather than a
  full-height wall of video or a barely-visible sliver.
- Each side is pushed toward its outer screen edge (`justify-content: flex-start` /
  `flex-end` + 18px edge padding) instead of centred in the margin, putting real distance
  between the props and the central task.
- The central task column (`.screen`) now has its own frame — background fill, border,
  drop shadow — matching the side props' visual language, and its max width grew to
  `min(96vw, 1100px)`; the CPT stimulus panel itself grew from 280×280 to 500×500px
  (shape 110px → 196px, same proportions) so it reads as the clear focal element.

`distractor-reel.html` gained a small, backward-compatible `?embedded=1` flag (only used by
these iframes) that skips its own fullscreen request — a page can only own one fullscreen
element at a time, and the parent page (this pilot tool) already has it. Nothing else
changes; standalone tablet usage is unaffected.

**Why this, and not a second physical device:** running two devices solo (phone propped
next to your laptop) works, but it's real friction for something you're doing repeatedly
while iterating. This gets you the *same-session* central-task + peripheral-distraction
interaction on one screen with one click.

**Why this, and not just scaling the task to fill the screen:** stretching the task itself
doesn't achieve anything — the shapes don't need to be bigger or further apart, and it
would throw away the one thing that space is good for (a place to put the actual
distraction stimulus).

**The honest caveat, unchanged from before:** `testing-strategy-v2.md` §4.1 places the real
tablets at **±35° azimuth**, chosen deliberately as "the near-periphery zone of maximal
involuntary capture" (UFOV literature). At a typical 50-70 cm viewing distance, 35° needs
a 35-49 cm offset from center — more than half of even a 32" monitor's total width. So the
eccentricity you actually get from screen-edge iframes depends entirely on your monitor
size and how close you sit, and will usually be smaller than 35° (something like 15-25° is
more realistic on a normal desktop monitor). The literature on distractor eccentricity
([Understanding peripheral interference: the effects of distractor relevance and
eccentricity on capture](https://jov.arvojournals.org/article.aspx?articleid=2142193);
[Retinal eccentricity modulates saliency-driven but not relevance-driven visual
selection](https://link.springer.com/article/10.3758/s13414-024-02848-z)) confirms
eccentricity isn't a minor detail — capture dynamics genuinely change with it. This is
good enough for piloting *feel and timing*; it is not a controlled replication of the real
study's angle, and nothing about a pilot session run this way should be treated as
informative about the actual 35° manipulation. (I also checked whether a **virtual
chinrest** technique — a validated browser method for estimating viewing distance to
calibrate visual angle, [Li, Joo et al. 2020, *Scientific
Reports*](https://www.nature.com/articles/s41598-019-57204-1), shipped as a [jsPsych
plugin](https://www.jspsych.org/v7/plugins/virtual-chinrest/) — could close this gap. It
can't: it calibrates accurate angles for stimuli that fit on your screen, it can't
manufacture screen width that isn't there. If you want to test something closer to the
real 35°, use a second physical device positioned to the side instead.)

If your browser blocks local-file iframes (uncommon, but some browsers restrict `file://`
framing more than others), the margins will just render empty — serve the repo root with
a trivial local static server instead of opening via `file://` and they'll load normally:
```
python -m http.server 8000
```
then open `http://localhost:8000/PilotTools/block-a-cpt.html`.
