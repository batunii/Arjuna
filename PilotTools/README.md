# Block A Pilot Tools

Standalone, self-contained web pages for piloting Block A's workstation task without the
headset or a Unity build. Same idea as `TabletApp/distractor-reel.html`: no install, no
build step, runs identically in any browser (desktop, phone, or tablet). Two tools here,
for two different task designs:

| File | Task design | Status |
|---|---|---|
| `block-a-cpt.html` | Sequential Go/No-Go CPT: one shape at a time, respond to circles, withhold on squares. | **Matches the current dissertation spec** (`testing-strategy-v2.md` §4.1) and the real `Study/CPTPanel.cs`. |
| `block-a-multitarget.html` | Multi-target visual search: 3-5 shapes on screen at once, click the circle among square decoys, all shapes respawn on any click, runs for a fixed 3.5 min session. | **Exploratory only** — not in the current spec, no Unity implementation exists. Built to pilot-test the idea before deciding whether to adopt it. |

If you're not sure which one reflects what's actually planned for the study right now:
it's `block-a-cpt.html` — that's the one the dissertation doc and the real Unity code
both describe. `block-a-multitarget.html` is a "what if we did it this other way instead"
exploration; nothing else in the codebase depends on it or expects it.

## Running `block-a-cpt.html`

Open it in any browser. No server needed.

1. Enter a participant ID and pick **Practice** (40 trials) or a condition slot **A1-A4**
   (140 trials each, matching `k_practiceTrials`/`k_cptTrials` in `ConditionSequencer.cs`).
2. Circle = go (respond), square = no-go (withhold). Respond with Space, click, or either
   (your choice in the setup screen).
3. After the run: on-screen stats (go-accuracy, false-alarm rate, mean/median hit RT,
   d-prime, and — for Practice — the ≥ 90% criterion pass/fail), plus a **Save CSV** button.

## What matches CPTPanel.cs exactly

- SOA 1.5 s, stimulus visible 700 ms, response window = full SOA from onset.
- 80% go / 20% no-go.
- Trial sequence constraints: no two consecutive no-gos, go-runs capped at 8 (with the
  same retry-then-even-spacing-fallback structure as `CPTPanel.BuildSequence`).
- Trial/run counts: 40 practice (seed 999), 140 main (seed = `pid*10 + slot`, slot 0-3 for
  A1-A4) — the same seed formula `ConditionSequencer.cs` uses.
- Outcome categories and event names (`CPT_ONSET`, `CPT_RESULT` with `hit`/`miss`/
  `commission`/`correct_reject`, `CPT_RUN_START`/`END`, `PRACTICE_START`/`END`) match
  `StudyLogger`'s payload conventions.
- Exported CSV uses the exact same header as `StudyLogger.cs`
  (`t_ms,pid,block,condition,yaw_deg,pitch_deg,roll_deg,head_speed_dps,dr_intensity,mode,event,payload`).
  Head-pose columns are blank (no VR headset here); `mode` is stamped `CPT_PILOT` so this
  can never be mistaken for a real study session if it ever ends up near real data.

## Found while building this: CPTPanel.cs likely always falls back for main runs

`CPTPanel.BuildSequence` retries up to 2000 times to find a sequence satisfying BOTH
"no two consecutive no-gos" AND "go-runs ≤ 8" before giving up and using a fixed,
seed-independent evenly-spaced fallback. I measured this empirically (same constraints,
independent of PRNG family — see below): for 140 trials / 28 no-go (the main-run size),
satisfying both constraints together takes on the order of **~75,000 random attempts on
average** (highly variable — sampled range was ~700 to ~275,000 across 20 seeds). 2000
attempts finds a real match only **roughly 2-3% of the time**.

That means the real Unity task, as shipped, most likely falls back to its fixed pattern
for **nearly every real 140-trial main run** — every participant/condition would get the
*identical* trial order regardless of seed, silently defeating the intended per-session
randomization. The 40-trial practice run is fine (its equivalent constraint succeeds
~18% of the time per attempt, trivially satisfied within 2000 tries).

This pilot tool uses a 1,000,000-attempt cap instead (still only ~100ms, 0 fallbacks in
30 test seeds) so it actually generates varied sequences. **Recommend raising the same
cap in `CPTPanel.cs`** (`attempt < 2000` → e.g. `attempt < 1_000_000`) — it's a one-line,
zero-risk change; this runs once per `BeginRun` call, nowhere near a per-frame budget.

## 1-back mode in `block-a-cpt.html` (added 2026-07-20, exploratory)

The setup screen has a **Task** selector: `Go/No-Go CPT` (default — the study spec, everything
above applies) or `1-back` — answer **YES ("same as the previous shape") or NO ("different")
on every shape**, via on-screen buttons or the Y/N keys. Built after supervisor feedback
(2026-07-17, point 6) that the CPT is bottom-up/reactive; the 1-back adds a
working-memory/goal-maintenance component while keeping the trial engine unchanged (same
SOA/stimulus timing, trial counts, seed formula, CSV schema, stats). Details:

- **Forced choice, not go/no-go** (changed same day after first hands-on feel: watch-and-mostly-
  do-nothing felt wrong): an answer is expected on every scored trial, feedback flashes
  immediately (blue correct, red wrong) and the chosen button stays outlined green/red with both
  buttons locked until the next shape. Outcomes: `hit` (YES on repeat), `miss;answered_no` /
  `miss;timeout` (repeat missed), `commission` (YES on non-repeat), `correct_reject`
  (NO on non-repeat), `no_response` (non-repeat timed out — counted separately as an
  engagement/erraticness signal, which also serves feedback point 5b's outlier-detection ask).
  The first trial has no predecessor, so it is shown to memorize and logged `first_unscored`.
- **1-back timing differs from the CPT** (fixed 2026-07-20 after hands-on feel — with the CPT's
  1.5s SOA and full-SOA response window, a late answer landed on the *next* shape and was scored
  against a stimulus the participant hadn't processed, i.e. seemingly random wrong-answer
  flashes): SOA 2.0s (140 trials ≈ 4:40 per run, vs the CPT's 3:30), stimulus 700ms unchanged,
  answers accepted 0.25–1.7s after onset. Clicks before 0.25s are ignored as spillover from the
  previous trial; after 1.7s the trial is closed and the remaining 300ms shows feedback only
  (previously a timeout's red flash was cleared by the next onset in the same tick, so it was
  never visible).
- 4 shapes (circle, square, triangle, diamond), ~30% repeat rate (12 targets / 40 practice,
  42 / 140 main). Trial 1 is never a target; no two consecutive targets (no triple-repeats);
  max 8 trials between targets. Non-adjacency is guaranteed by gap-sampling construction, so
  unlike the CPT builder this doesn't depend on a huge rejection-sampling attempt cap.
- CSV `mode` column is stamped `NBACK1_PILOT` (vs `CPT_PILOT`), the run-start payload carries
  `task=nback1` (vs `task=cpt`), onset payloads add `;shape=...`, and the exported filename says
  `nback1` — exports from the two tasks can't be confused.
- Results add `No response (timed out)` and `Overall accuracy` rows — overall accuracy is the
  number to watch against the ~75–90% no-distractor band.
- The practice criterion is still the CPT's ≥ 90% — provisional for 1-back; if this mode
  graduates, the pilot should set its own band (target ~75–90% no-distractor accuracy).
- **No Unity implementation exists.** Same status as `block-a-multitarget.html`: adopting this
  for the real study means updating `testing-strategy-v2.md`, `CPTPanel.cs`, and re-checking the
  stats plan (the DV stays signal-detection, so `h1_lmm.py` needs less rework than multitarget
  would).

## What does NOT match (read before drawing conclusions from pilot data)

- **PRNG family differs.** The trial sequence uses a JS `mulberry32` seeded PRNG, not
  .NET's `System.Random`. Same seed will NOT reproduce the exact bit-for-bit trial order
  a real Unity session would generate for that seed — the generation *algorithm* and
  *constraints* are replicated exactly, but not the literal sequence. Use this for piloting
  task feel, timing, and the stats/export pipeline — not for regenerating a specific real
  participant's exact stimulus order outside Unity.
- **No visual-angle sizing.** `CPTPanel.cs` sizes the shape by visual angle at a fixed
  placement distance in the headset; this tool uses a fixed on-screen pixel size. Shape
  size/eccentricity on a flat monitor is not equivalent to the VR condition.
- **Response device differs.** The real task responds to a right-controller trigger pull;
  this tool uses spacebar/mouse click. Motor response time distributions will differ from
  the VR task — don't compare absolute RTs between this tool and real sessions, only
  relative patterns (e.g. did accuracy/RT trend sensibly across your own pilot runs).
- **This is not the full Block A protocol.** It's only the workstation CPT task (Option V's
  cognitive component) — no vignette conditions, no tablet distractors (use
  `TabletApp/distractor-reel.html` for those, loaded separately on a phone/tablet), no
  Latin-square sequencing across the 4 A-conditions, no TLX prompts.

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

Same CSV schema as `block-a-cpt.html`, stamped `mode=MULTITARGET_PILOT` (vs. `CPT_PILOT`)
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
space either side. `block-a-cpt.html` embeds the **actual** `TabletApp/distractor-reel.html`
(not a fake simplified stand-in — this project has a "no improvised stimuli" principle,
§10.1, and it's easy to just reuse the real, tested, schedule-driven asset instead of
inventing something new) in `<iframe>`s filling those margins. Each side is independently
controlled — tap **Distractors-present** + **START** inside it — mirroring how the real
protocol's tablets are manually started by the experimenter anyway, just both sides live
in one browser window instead of two physical tablets. (`block-a-multitarget.html` does
**not** currently have this margin wiring, despite an earlier version of this doc implying
both tools did — only `block-a-cpt.html` embeds it as of 2026-07-16.)

### Video clips as an alternative to the procedural reel (added 2026-07-16)

`block-a-cpt.html`'s setup screen has a **Distractor content** selector: `Video clips`
(default) or `Procedural reel`. Switching it swaps both side-margin iframes' `src` between:

- `../TabletApp/distractor-reel.html?embedded=1` (the real, deterministic study asset), or
- `video-reel.html?src=../Assets/_Scratch/tiktok_5min_{1,2}.mp4&embedded=1` — a real
  pre-rendered fast-cut clip with animated captions, generated by
  `Tools/tiktok_captions.py` from footage in `RawFootage/`. See
  [Distractor Content Pipeline](<../.agent-docs/systems/distractor-content-pipeline.md>) for
  how those clips are built.

`video-reel.html` is a minimal sibling to `distractor-reel.html` — same wake-lock/fullscreen/
`?embedded=1` conventions, just a muted looping `<video>` instead of the procedural canvas.
This is purely exploratory (trying how real high-motion video *feels* as peripheral
distraction, side by side with the procedural option) — it doesn't change what the real
study tablets show.

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
