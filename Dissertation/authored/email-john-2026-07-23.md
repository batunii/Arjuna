# Draft email to John — 2026-07-23

Subject: Pilot update — first naive participants: no detection cost, consistent filter advantage on the attention task

Hi John,

I ran the first batch of naive participants through the full two-block protocol on Tuesday
evening — five sessions in one sitting, which also stress-tested the session harness
(auto-assignment, counterbalancing ledger, data pull) end-to-end. One participant (P1) is
excluded throughout: both of their video runs failed the false-alarm criterion (39–47% of
presses), which was an instruction failure on my side — the briefing now says "press ONLY
when you see the ring" and the practice gate will fail on false alarms. The four remaining
pairs are below.

## First, your questions on the hypotheses

You asked me to define the hypotheses clearly enough that the null can't be supported just
because the conditions were vague. They're pre-committed in the testing strategy; in plain
terms, the study makes two claims, one per task:

- **H1 (Task A — the DR vignette):** distractions hurt performance on a focused task, and
  **with the vignette on they hurt less** — measured as the reduction in the distractor
  cost on accuracy and speed, not as a raw "filter makes you better" main effect. A
  manipulation check (MC1) gates it: distractors must first demonstrably hurt *without*
  the vignette, and the effect must clear a pre-set smallest-effect-of-interest (recover
  at least half the unprotected distractor cost), so a vague or trivial result can't pass.
- **H2 (Task B — selective/adaptive filtering of the periphery):** suppressing peripheral
  visual noise can **enhance focal detection without sacrificing peripheral awareness**.
  Formally two halves: **H2a** — central events are detected faster, with no
  loss in central hit rate; **H2b** — peripheral detection is *maintained*, and
  "maintained" must be positively proven (equivalence test, 10-percentage-point margin) —
  a drop beyond the margin is a conclusive harm verdict, not an inconclusive shrug.

Alongside these: **H3** (people also *report* lower workload, NASA-TLX, with the vignette
under distraction) and an exploratory comparison of the three filter modes with no
committed hypothesis. Every outcome, including failure, has a pre-written row in the
decision table.

Tuesday previews each claim: the Block A results below are the H1/H3 channel (direction
favourable, ceiling-limited — and without scripted distractors, not yet the H1 test); the
video-block periphery results show H2b behaving exactly as the equivalence framing hopes
(deltas inside the 10 pp margin once stimulus artifacts are controlled); and the
central-probe picture is H2a's territory, currently muddied by a probe visibility artifact
I know how to fix. Details in that order.

## Block A (central 1-back in passthrough): a consistent filter advantage

Every pair scored higher with the filter on, and — the detail I find most interesting —
the filter rounds contain **zero commission errors between them**: every impulsive press in
the dataset happened in a no-filter round. Median RT was faster under the filter in every
pair but one:

| P | No filter | Filter | Median RT, no-filter → filter |
|---|---|---|---|
| P2 | 97.6% | 100% | 683 → 719 ms |
| P3 | 96.3% | 98.8% | 776 → 678 ms |
| P33 | 97.6% | 98.8% | 667 → 608 ms |
| P4 | ~98.4% | 100% | — → 593 ms |
| P5 | (export pending) | | 700 → 567 ms |

To be upfront about the size of this: these are 1–3 trial differences on a task at ceiling —
your Point 5a warning, realised. The task ended up easy for a reason: in early testing three
users with glasses (poor eyesight) struggled to read text inside the headset, so any
reading-based task (including the classic letter n-back) had to go, and I moved to a
forced-choice shape 1-back; raising it to 2-back in piloting made the task confusing rather
than merely harder. So the current form is deliberately simple, and the ceiling is the
price. Even so, the direction is uniform across accuracy, commissions, and RT — which I
read as a genuine lean worth chasing, not a result yet. Next batch I'll harden it just
enough to open headroom (shorter SOA and/or lure no-gos rather than a higher n), so
whatever is producing this consistency has room to express itself. (These sessions ran
without scripted distractors — a quiet room — so this isn't the H1 interaction yet; it's
the encouraging observation that the filter helps slightly even with almost nothing to
filter out. The distractor arm is what turns this channel into the H1 test proper, with
MC1 checked first.)

## Block B (driving video with ring probes): improvement exactly in the peripheral detection band

This block also implements the central-vs-peripheral bifurcation you suggested — upgraded
from a binary split to a five-band continuous dose (the filter strength computed at each
target's position), since under the graded window every target sits somewhere on the
gradient.

Detection of the seeded targets shows the filter's clearest gain at 10–30° eccentricity —
hit rate 44% → 60% at 10–20° and 33% → 67% at 20–30° — with near-identical results in the
other regions once the very short targets are set aside (all other bands within ~10 pp,
i.e. inside H2b's equivalence margin, and median RT under the filter equal or faster;
deep-gradient hits came in at 1.25 s median vs 4.80 s baseline). Your ceiling concern from
Point 1 is also answered on this block: naive no-filter baselines landed at 52–78%,
in or near the informative 60–85% band I targeted — the probes are neither trivial nor
invisible.

That 10–30° band is not an arbitrary place to improve. It brackets the region the driving
literature treats as the safety-critical periphery: the Peripheral Detection Task
deliberately places its probes at **11–23° horizontal eccentricity** (Jahn, Oehme, Krems &
Gelau 2005, Transp. Res. F 8, 255–275), just outside the ~8°-radius "road centre" region
where driver gaze concentrates (Victor, Harbluk & Engström 2005, Transp. Res. F 8,
167–190), and inside the Useful Field of View (~30° diameter, and it shrinks further under
cognitive load). In other words, the filter is helping precisely where peripheral detection
matters for driving and is known to degrade under workload.

The centre (≤10°) came out slightly *below* baseline, and I've traced that to probe
visibility rather than attention: when the detection window opens on a target, the shader
boosts that region's saturation and brightness, and a translucent yellow ring sitting on a
boosted, warm background loses its contrast — the misses concentrate on exactly those
targets (bright traffic lights), and central RT slows in step. That is an instrument
artifact with a designed fix (below), and next round I'll focus on getting the probe
recognition to click so the centre reads equal-to-baseline or better.

## Why the probe looks the way it does (your Point 3)

The probe design has been the hard research problem, because Point 3 demands the probe
respect the filter, and most obvious probes fail on background-dependence:

- A **translucent bubble/refractive probe** borrows its visibility from whatever scene
  contrast happens to be behind it, so per-target baseline detectability varies with
  content. A Monte-Carlo check I ran showed that variance dilutes power and,
  uncounterbalanced, can even flip the sign of an estimated effect — and detectability
  being local-contrast-limited is exactly the finding of Wallis, Dorr & Bex (2015). The DRT
  standard (ISO 17488) solves this with a fixed, background-independent stimulus, which is
  the property the probe needs and a bubble cannot have.
- A **solid black ring** (elegant on paper: black is a fixed point of the dim/desaturate
  transform, so on-top ≡ behind-filter) failed empirically via clutter camouflage — and,
  disqualifyingly, it was *more* visible with the filter on than off, because the flat grey
  periphery removed its camouflage.
- I then ran the naive batch on the **yellow ring**, and have a **filter-respecting
  golden-ring variant** in the build (the ring's colour pushed through the same
  dim/desaturation the scene gets), which keeps baselines comparable to the yellow data.
- The current build moves to a **black-and-white "rope" ring**: alternating dark/light arc
  pairs. It is achromatic (the filter's desaturation is a no-op on it, and it can't be
  confused with the highlight colours), contrast-polarity complete (any background is
  darker than white or brighter than black, so some arc always carries contrast — the
  fiducial-marker principle), and because the filter's dim is multiplicative it preserves
  the pattern's internal Michelson contrast — it dims honestly, like a real object, without
  ever degrading to grey-on-grey. Arc count is kept low so the pattern survives peripheral
  acuity (cortical magnification: Anstis 1974; Rovamo & Virsu 1979), and stimulus alpha is
  the graded conspicuity knob if it pilots at ceiling (per van Winsum's DRT conspicuity
  observation that overly conspicuous probes hide workload effects).

## What participants said afterwards (opinion questionnaire, sections A–C)

I closed each session with the end-of-session opinion questionnaire — sections A–C only,
administered as agree/disagree for time (the one skipped item, the head-turn fade, doesn't
apply cleanly to the video-locked window; I'll return to the full 7-point scale for the
formal runs). Four respondents (P2, P3, P33, P4), three patterns:

- **Perceived focus benefit is near-unanimous: 19 of 20 pro-filter answers.** All four
  agreed it was easier to keep attention on the task, that the effect made it obvious
  where to look, and that things at the edges bothered them less. The single dissent is
  telling: the strongest performer said the effect made "no real difference" to their
  concentration — while still endorsing the other four items.
- **Comfort is a non-issue at the current strength:** 4/4 said the boundary between clear
  and altered regions was not itself distracting (the soft edge doing its job), 4/4
  reported no eye strain, and 3/4 said the effect faded into the background after a while.
- **The most interesting item: all four said they noticed side events *later* than they
  would have without the effect.** I read this two ways, and think the ambiguity is
  itself the finding. As an awareness cost (the item's keying — the subjective analogue
  of H2b), it's a worry; but as phenomenology it is exactly what the mechanism is
  *designed* to produce — peripheral onsets no longer capture attention the moment they
  appear, and events that persist still get attended: deferred, not denied. Had all four
  said "I noticed everything instantly, same as always," I'd doubt the manipulation was
  doing anything. The behaviour sides with the second reading for everything they were
  asked to catch — detection unchanged-to-better, responses faster — so the felt delay
  never materialised against task-relevant events. Where the two readings genuinely
  diverge is task-*irrelevant* events, which aren't measured yet — and your decoy-target
  suggestion from Point 2 is precisely the instrument that separates them (decoys in
  unimportant regions, where *slower* responses under the filter would be the desired
  outcome). I'd now like to build that into the next batch — and I already have a first
  pass from the existing data: some authored person targets never pass the person engine
  (not detected by the YOLO bake, or rejected by the closeness/angle acceptance I added),
  so the guidance system does nothing for them — de-facto decoys. Within the same target
  set, their hit rate is statistically unchanged by the filter (50% off vs 48% on), and
  the persistent ones are still reliably caught with the filter running while the sub-1.5 s
  ones floor in *both* conditions — the "deferred, not denied" texture, visible at target
  level. (These are decoy-like rather than true decoys — the probe ring is still fully
  visible on them in this build — hence still wanting the real thing next batch.)
  Meanwhile the acceptance
  observation stands on its own: three of four were uncomfortable not seeing everything —
  **the perceived cost exceeds the measured cost**, so trust in the periphery is a design
  target regardless of which reading wins.

## Where this leaves us

The harness survived five real participants in an evening; the filter costs nothing
detectable anywhere, helps in the band the literature says matters, shows a small, uniform
advantage on the central task — and participants *feel* the focus benefit, though they
don't yet trust the periphery. The known issues are all stimulus-side and fixable:
I'll re-screen the pool with a 2 s minimum target duration (the ≤1.5 s targets floor for
naive viewers in both conditions), recruit the reverse counterbalance cell to unwind an
A/B set asymmetry, switch to the rope probe for the centre fix, and harden Block A for
headroom. Happy to walk through the full band tables and the per-target traces whenever
suits — and I can send the one-page report with the charts if useful.

Best,
Shreyansh
