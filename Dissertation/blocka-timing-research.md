# Block A timing research — SOA / answer-window anchors (2026-07-27, web-verified)

Companion to `window-size-research.md` (which covers the Block B visual window). This note
answers: **what does the literature say the trial timing of a forced-choice 1-back should
be**, so the v4 answer window is chosen on evidence rather than projection. Decision
context: v4 made the *decision* hard (look-alike lures, 50% repeats, memoryless sequence);
the open question is whether v3's 1.8 s SOA / 1.5 s answer window still fits.

## TL;DR

The field default for n-back is a **3.0 s response window** (500 ms stimulus + 2500 ms
ISI) — twice our current 1.5 s. Our fast anchors (SART 1.15 s, Conners ~1 s) are simple
*detection* tasks, not discriminations, so they justify a pace floor but not a
discrimination window. Two literatures argue against a tight window for our design: RT
lapse/distraction effects live in the slow tail (ex-Gaussian τ), which a tight window
censors; and deadline-driven errors reflect a speed-accuracy criterion shift, not
sensitivity, which muddies d′. Conclusion: window should comfortably exceed ~2× the naive
median RT; with v4 author-median already at 812 ms, 1.5 s fails that, 1.7–1.9 s passes.

## Anchors

| Task | Stimulus | Response window (SOA) | Task type | Note |
|---|---|---|---|---|
| Standard n-back (Jaeggi-style; PsyToolkit; clinical-trial versions) | 500 ms | **3.0 s** (500 + 2500 ISI) | WM match/non-match | The convention across age groups and stimulus types; match:non-match 1:2 |
| SART (Robertson et al. 1997) | 250 ms + 900 ms mask | **1.15 s** | Go/no-go *detection* (respond 89% of trials) | Response is a prepotent keypress, no discrimination — pace-floor anchor only |
| Conners' CPT | 250 ms | ISI 1–4 s (blocked) | Go/no-go detection | Same caveat as SART |
| Block A v1 | full window | 2.5 s | 1-back, 4 distinct shapes | naive accuracy 96–100% |
| Block A v3 | full window | 1.8 s (1.5 s window + 0.3 s gap) | 1-back, 4 distinct shapes | naive accuracy 95–100%, medians 567–776 ms → window = 1.9–2.6× median |
| Block A v4 (current) | full window | 1.8 s (1.5 s window) | 1-back, 6 look-alike shapes, 50% repeats | author median 812 ms → window = 1.8× the *author's* median; naive projected ~950–1000 ms → ~1.5× — fails the ≥2× rule |

Notes on the anchors:
- **The ≥2×-median rule is a house rule** (introduced at the v2 ceiling fix), but it is
  *conservative relative to the field*: the standard n-back window (3.0 s) is 3–6× typical
  letter-matching medians (~500–900 ms). Every timing we have used is already faster than
  the literature default.
- **SART's 1.15 s is not a discrimination window.** SART responses are simple detections
  with RTs ~350–450 ms; its SOA is 2.5–3× its median RT. Citing it as a floor for *pace*
  is valid; citing it to justify a tight window on a fine same/different discrimination
  is not.

## Why a tight window hurts this specific study (two literatures)

1. **Lapses and distraction live in the slow RT tail.** Ex-Gaussian decompositions of RT
   consistently localize attentional lapses in τ (the exponential slow tail), and τ-based
   measures outperform trimmed means for exactly this reason (ADHD/lapse meta-analyses).
   Block A's H1 mechanism is peripheral distraction capturing attention → occasional slow
   responses. A 1.5 s ceiling right-censors the tail where that signal lives — the
   distractor cost in RT (and in the CV/drift time-course metrics) is truncated at the
   exact moment it appears.
2. **Deadline errors are criterion, not sensitivity.** The speed-accuracy-tradeoff
   literature (deadline/response-signal paradigms; drift-diffusion accounts) shows that
   under a deadline participants lower decision thresholds to guarantee an answer in
   time. Accuracy lost this way reflects the threshold shift, not reduced discrimination
   ability — so a d′ deflated by the deadline is not measuring what H1 needs. The clean
   design puts difficulty in the decision (v4's lures) and keeps the clock generous
   enough to record the decision that was actually made.

## Options (140-trial run lengths; 0.3 s closed gap kept throughout)

| Option | SOA / window | Run length | Window vs 812 ms author-median | vs ~950–1000 ms naive projection |
|---|---|---|---|---|
| Keep v3 timing | 1.8 / 1.5 s | 4:12 | 1.8× | ~1.5× — fails the rule; censors the tail |
| Moderate bump | 2.0 / 1.7 s | 4:40 | 2.1× | ~1.7–1.8× — borderline |
| Full-rule | 2.2 / 1.9 s | 5:08 (or **126 trials = 4:37**, still 63 repeats vs v3's 42) | 2.3× | ~1.9–2.0× — passes |
| Literature default | 3.0 / 2.7 s | 7:00 (or 100 trials = 5:00) | 3.3× | ~2.7× — no pace pressure at all |

The 50% repeat rate gives slack on trial count: any count ≥ ~100 still beats v3's 42
signal trials, so a longer SOA can trade against trials to hold the ~4–5 min block length.

**DECIDED 2026-07-27 (user): 2.0 s SOA / 1.7 s window, 140 trials (4:40).** Comfortably
above the SART pace floor, deliberately faster than the 3.0 s field default (pace pressure
retained), 2.1× the author-median; borderline (~1.7–1.8×) against the *projected* naive
median — to be checked against participant 1's practice-run median RT and timeout count
before their main runs; if naive medians come in ≥ ~950 ms with multiple timeouts, the
fallback is 2.2/1.9 at 126 trials (same run length).

## Sources

- [PsyToolkit N-back / 2-back task](https://us.psytoolkit.org/experiment-library/nback2.html) — 500 ms letter + 2500 ms blank, 3 s to respond.
- [Psychometric characteristics of the n-back task (Current Psychology, 2025)](https://link.springer.com/article/10.1007/s12144-025-07318-9) — construct validity across stimulus types; standard parameterization.
- [Sustained Attention to Response Task — PsyToolkit](https://www.psytoolkit.org/experiment-library/sart.html) and [Stothart's SART implementation of Robertson et al. (1997)](https://github.com/cstothart/sustained-attention-to-response-task) — 250 ms digit + 900 ms mask, 1.15 s ISI, 225 trials, respond on 89%.
- [Ex-Gaussian RT parameters in ADHD: meta-analysis (PMC10920450)](https://pmc.ncbi.nlm.nih.gov/articles/PMC10920450/) and [Capturing the dynamics of response variability (PMC4299975)](https://pmc.ncbi.nlm.nih.gov/articles/PMC4299975/) — τ (slow tail) indexes attentional lapses; tail-sensitive measures beat trimming.
- [Speed-accuracy trade-off under response deadlines (PMC4133757)](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC4133757/) and [The speed-accuracy tradeoff: history, physiology, methodology (PMC4052662)](https://pmc.ncbi.nlm.nih.gov/articles/PMC4052662/) — deadlines induce threshold lowering; deadline errors reflect criterion, not sensitivity.
- [Investigating the speed–accuracy trade-off: deadlines vs response signals (BRM 2013)](https://link.springer.com/article/10.3758/s13428-012-0303-0) — methodology of deadline paradigms.
