# Quick update email to John — 2026-07-23 (evening, day-2 pilots)

Subject: Day-2 pilots — the 10–30° filter advantage is holding up (6 of 6), and Block A hardening results

Hi John,

Quick update after tonight's three sessions (P6, P9, P10) — the headline is that the
peripheral-band result from Tuesday is holding, and it's cleaner now.

## The 10–30° finding

Pooling the seven usable participants across both days (exclusions below), hit rate on
targets at 10–30° eccentricity — the PDT band, where the driving literature says
peripheral detection matters and degrades under load:

- **With filter: 59% (80/136). Without: 43% (55/128).**
- Direction is positive in **6 of 6** included participants (range +6 to +24 points).
- It is not a practice artifact: the advantage holds separately in the participants who
  ran filter-first and filter-second.
- It is not carried by any one participant: dropping the noisiest one (P3, below) leaves
  59% vs 44%.

Overall hit rate also leans filter (62% vs 57%), but the effect concentrates in the
10–30° band — which is where the mechanism is supposed to work, so this is the first
Block B result I'd actually show someone.

Exclusions, applied consistently: P1 stays out (the false-alarm criterion from Tuesday's
report); P9 (new today) is out of Block B only — they misunderstood what to click during
their first video run and their pair isn't interpretable. Their Block A data stays in.

**One rule I need to settle with you: a pre-stated false-alarm criterion.** P3's filter
run has an absurd false-alarm count (539 — near-continuous trigger pulling), which under
Tuesday's P1 logic should exclude it; as noted, the pooled result barely moves either
way. I'd like to fix a threshold (e.g. false alarms > k × target count → run excluded)
before the formal runs rather than decide per-participant. Suggestions welcome — I'd
default to k = 1.

## Block A: hardening happened, ceiling survived

Per Tuesday's plan I hardened the task: main runs are now 140 trials at 1.8 s SOA
(4:12/run; answer window 1.5 s still spans ~2× the naive median RTs from day 1, and the
pace stays above SART's validated 1.15 s). The result is clear: **the pace lever is
exhausted.** Today's two participants on the new timing scored 95–100% with 500–690 ms
median RTs, flat across the run. Speed pressure is not going to open headroom on this
task; the discrimination is too easy. I'd like to discuss making the *decision* harder
instead — confusable shape lures or a 2-back — knowing 2-back previously confused
pilots, and knowing this is the one iteration we have before the freeze.

## New instrumentation (both blocks)

- Every Block A run now reports time-course metrics (accuracy, median RT, and RT
  consistency split into run thirds, plus an RT-drift slope), and I have analysis
  scripts that compare drift/consistency between filter states across runs — for Block B
  as a difference-in-differences (same set = identical targets in both arms, so
  late-clip target difficulty cancels). Early read: no fatigue signal against the
  filter; if anything filter runs hold up better late (small n).
- The workstation distractor rig for the H1 arm is upgraded: richer video-clip pool with
  the salient clips held longer on screen, two selectable distractor sets, screens stay
  dark until the run starts, and the content choice is stamped into every run's log.

Data hygiene from today, for the record: one participant's video runs were saved under a
mistyped id (fixed, originals preserved); one aborted headset-off run identified and kept
out of scoring; and one participant ran both Block A slots with the same seed (so their
second, perfect run is suspect) — the tool will get a guard for that.

Happy to send the per-participant band tables. Next session continues recruitment into
the reverse counterbalance cells.

Best,
Shreyansh
