# Testing Strategy v2 — User Study Methodology
## Guiding User Attention in Real-World Tasks Using XR Overlays
### MSc Dissertation — Trinity College Dublin

> **System under test:** Multi-mode Diminished Reality (DR) attention-guidance overlay on Meta Quest 3
> passthrough. Modes evaluated: **ColorPop** (salience re-grading, driving scenario), **Soft Dark** and
> **Hard Dark** (peripheral dimming/black-out with a painted, world-locked focus window).
> This document supersedes `dissertation-study-methodology.md` (v1), which is retained for reference.
> The dissertation's overall two-part structure (technical design-space + concept evaluation) and
> claim-scoping rules are fixed in the companion document `framing.md`.

---

## Table of Contents

1. [What Changed Since v1 and Why](#1-what-changed-since-v1-and-why)
2. [Research Questions & Pre-Committed Hypotheses](#2-research-questions--pre-committed-hypotheses)
3. [Design Overview](#3-design-overview)
4. [Block Protocols](#4-block-protocols)
5. [Session Timeline](#5-session-timeline)
6. [Pilot Study with Pass/Fail Gates](#6-pilot-study-with-passfail-gates)
7. [Questionnaires & Safety Protocol](#7-questionnaires--safety-protocol)
8. [Analysis Plan & Decision Table](#8-analysis-plan--decision-table)
9. [Threats to Validity & Mitigations](#9-threats-to-validity--mitigations)
10. [Operational Protocol (Murphy's Law Engineering)](#10-operational-protocol-murphys-law-engineering)
11. [Ethics Delta & Amendment Checklist](#11-ethics-delta--amendment-checklist)
12. [Literature Grounding](#12-literature-grounding)
13. [Appendix A — Tooling Specification](#13-appendix-a--tooling-specification)
14. [Appendix B — Verbatim Scripts & Forms](#14-appendix-b--verbatim-scripts--forms)

---

## 1. What Changed Since v1 and Why

The v1 methodology was criticised on three grounds: it was **happy-path** (hypotheses could only
succeed), **not conclusive** (the design could not distinguish "no effect" from "no power"), and
**unclear about what participants actually do**. Each criticism maps to specific design defects in v1
and a specific fix in v2:

| #   | v1 defect                                                                                                                                | Why it made the study inconclusive or happy-path                                                                                                                      | v2 fix                                                                                                                                                                                                                                                                                          |
| --- | ---------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | 2×2 **between-subjects** groups with n = 8 per cell                                                                                      | v1 §11.7 itself concedes the power analysis requires 24 per group. With 8, a null result is uninterpretable — no power to detect anything.                            | **Fully within-subjects, N = 20.** Every participant experiences every condition; power comes from paired comparisons and many repeated trials per condition, the standard recipe in the closest published DR studies (Cheng 2022; McLaughlin 2025).                                            |
| 2   | H1b ("peripheral detection **holds steady**") and H2b ("accuracy **maintained or improves**") were null-result hypotheses with no bounds | A non-significant difference at n = 8 is evidence of nothing. These hypotheses could not fail informatively — the definition of happy-path.                           | Safety hypotheses are now **equivalence tests (TOST) with a pre-registered margin** (H2b: peripheral hit-rate drop ≤ 10 percentage points). Exceeding the margin is a *conclusive negative finding*, not an ambiguous null.                                                                     |
| 3   | Task instruction: *"press the trigger when you notice anything happening"*, matched to hand-coded video events within ±3 s               | No ground truth: a press cannot be attributed to an event, false alarms are unmeasurable, event coding is subjective. Participants did not know what counted.         | **Scripted, machine-timed stimuli with one unambiguous instruction per task.** Block B probes have exact onset times and known eccentricities; Block A targets are generated (Option V) or printed on matched sheets (Option P). RT, hits, misses, and false alarms are all exactly measurable. |
| 4   | NASA-TLX administered **once per block**, after both conditions — yet H3c compared TLX *between* conditions                              | The workload hypothesis was untestable as instrumented.                                                                                                               | **Raw TLX after every condition** it is compared across (Blocks A and B), ~1 min each on a tablet.                                                                                                                                                                                              |
| 5   | Primary task = **small printed digits read through passthrough**                                                                         | Quest 3 passthrough does not reach normal visual acuity (arXiv 2026 psychophysics study, §12). Both conditions could floor out for reasons unrelated to the vignette. | Task legibility is a **pilot gate**. Two task forms are specified (virtual panel / large-print paper); the pilot selects whichever is demonstrably legible and off-ceiling.                                                                                                                     |
| 6   | No sickness measure, no practice block, no pilot, no manipulation check, no failure criteria                                             | Novelty effects, dropouts, and stimulus failures had no defenses; there was no pre-agreed way to say "it does not work".                                              | **VRSQ pre/post with a stop rule; practice-to-criterion tutorial; a gated pilot study (§6); a manipulation check inside the main analysis; and a pre-committed decision table (§8.3)** covering every outcome, including failure.                                                               |
| 7   | Described the superseded gaze-following system (`FocusVignette.unity`)                                                                   | The study would have tested software that no longer exists.                                                                                                           | Protocols target the **current implementation**: painted world-locked focus window on the camera-sphere overlay, modes ColorPop / Soft Dark / Hard Dark, motion-based suppression (`CameraSphereVignetteManager.cs`, `VideoTestSceneManager.cs`).                                               |
| 8   | Three scenarios as co-equal confirmatory tests                                                                                           | Power and session time split three ways; no single claim was properly tested.                                                                                         | **One primary confirmatory hypothesis** (Block A), one secondary confirmatory pair (Block B), and a compact exploratory mode sampler. The dissertation's headline claim rests on a single, properly powered test.                                                                               |

The core reframing: v1 asked *"does the vignette make people better?"* against an unconstrained
baseline. v2 asks a falsifiable question with a built-in manipulation check:

> **Peripheral distractors impose a measurable cost on a focal task (established effect — Frontiers
> 2025, N = 66). Does the DR vignette reduce that cost?**

If the vignette does nothing, the study *shows* it does nothing (the distractor cost remains at its
vignette-off size). If the distractors fail to distract, the pilot catches it before the main study
runs. There is no configuration of results that yields "inconclusive".

---

## 2. Research Questions & Pre-Committed Hypotheses

**RQ1 (primary).** Does peripheral DR dimming (Hard Dark, world-locked window) reduce the performance
cost that peripheral visual distractors impose on a focal workstation task?

**RQ2 (secondary).** Does ColorPop salience re-grading speed detection of focal-region events in a
dynamic (driving) scene, and does it do so **without** degrading peripheral event detection beyond an
acceptable margin?

**RQ3 (exploratory).** How do the three modes (ColorPop, Soft Dark, Hard Dark) compare subjectively
on comfort, perceived focus benefit, and willingness to use when sampled on a common stimulus?

All hypotheses, tests, and margins below are fixed **before data collection**. The pilot (§6) may tune
stimulus parameters (distractor salience, probe size, task form) but may not alter hypotheses, margins,
or analysis choices.

| ID | Hypothesis (directional, falsifiable) | Refuting observation | Status |
|---|---|---|---|
| **H1** | The distractor cost on the Block A task (error rate increase, Distractors-present vs -absent) is **smaller with the vignette On than Off** — a Vignette × Distractor interaction, with the vignette recovering at least the SESOI (default: ≥ 50 % of the vignette-off distractor cost; finalised from pilot data before the main study, §6). | Interaction estimate's 90 % CI excludes the SESOI (benefit too small to matter), or the interaction is absent/reversed. | Primary confirmatory |
| **H2a** | With ColorPop On, median reaction time to **central** probes (< 10° from focus centre) is lower, and central hit rate is not lower, than with the vignette Off. | Central RT not reduced (95 % CI on the paired difference includes 0 or favours Off). | Secondary confirmatory |
| **H2b** | With ColorPop On, **peripheral** probe hit rate (25–40° eccentricity) is *equivalent* to Off within a margin of **Δ = 10 percentage points** (TOST). | Equivalence not established AND the point estimate shows a drop > 10 pp → conclusive: the mode harms situational awareness. | Secondary confirmatory (safety) |
| **H3** | NASA-TLX global workload in Block A is lower in Vignette-On + Distractors-present than Vignette-Off + Distractors-present. | No TLX difference or higher with vignette. | Secondary |
| **MC1** (manipulation check, not a hypothesis) | With the vignette Off, Distractors-present produces a higher error rate than Distractors-absent (replicating the established distractor-cost effect). | If MC1 fails in the main study despite passing in the pilot, H1 is not interpretable for affected participants; report per pre-registered rule §8.4. | Gate |
| **EQ1–EQ3** (exploratory) | Sampler ratings (comfort, perceived focus benefit, willingness to use) differ across ColorPop / Soft Dark / Hard Dark. | — (exploratory; no confirmatory claim) | Exploratory |

**SESOI rationale.** H1's smallest effect size of interest is expressed as *proportion of distractor
cost recovered* rather than raw errors, because the raw cost depends on stimulus tuning. A vignette
that removes less than half of the distraction cost does not justify wearing a headset to get it; that
judgement is defensible to a supervisor and fixed before the main study. The pilot converts it to an
absolute error-rate margin for the power check. H2b's 10 pp margin reflects the safety framing: in a
driving-like monitoring context, losing more than one peripheral event in ten to the overlay is an
unacceptable awareness cost (consistent with the DR focus-vs-awareness trade-off documented by
McLaughlin 2025 and Murphy 2021).

---

## 3. Design Overview

### 3.1 Structure

| Property | Value |
|---|---|
| Design | Fully **within-subjects**. No between-subjects factors. |
| N | **20 analysed** (recruit 24: 4 spares for dropouts/exclusions) |
| Session length | **~53 min** including consent and debrief (§5) — inside the 40–60 min tolerance window participants typically accept; Block C is last and skippable (§4.3) |
| Blocks per participant | A (workstation, 2×2), B (driving probes, 2 conditions), C (compact 3-mode sampler — exploratory, skippable) |
| Counterbalancing | Block A: 4×4 **balanced Latin square** over its 4 conditions → 4 order groups × 5 participants. Block B: condition order alternated by participant parity (10 ColorPop-first, 10 Off-first). Block C: 3 mode orders from a 3×3 Latin square, cycled (7/7/6). Block order A→B→C is fixed for all participants (A is primary and must come freshest; C is exploratory and absorbs any fatigue — acknowledged as a limitation in §9). |
| Population | Adults 18+, normal or corrected-to-normal vision, no vestibular disorders (per approved ethics). VR experience recorded as a covariate. |

### 3.2 Why within-subjects with repeated trials is the conclusive design

- The two closest published DR evaluations are within-subject: Cheng et al. CHI 2022 (N = 16 and 12)
  and McLaughlin et al. Human Factors 2025 (two within-subject experiments, linear mixed models).
- Caine (CHI 2016): the modal sample size across all CHI user studies is 12; small-N in-person studies
  derive their power from repeated measures, not headcount.
- Each participant contributes **~140 scored task events per Block A condition** (Option V) or a full
  matched sheet (Option P), and **24 probes per Block B condition**. Trial-level mixed models use all
  of it; per-participant means feed the classical paired tests as a robustness check.

### 3.3 Power

For the primary contrast (H1 interaction, computed per participant as a paired difference of
differences): a paired t-test at α = .05 (two-tailed) with N = 20 achieves 80 % power for
**dz ≈ 0.65**. The distractor main effect this interacts with is large in the source literature
(commission errors more than doubled, 1.33 → 3.15, p < .001, N = 66 — Frontiers 2025), so a vignette
recovering half of it is plausibly detectable at this N; the pilot verifies the local cost size before
the main study commits. Trial-level linear mixed models (~11,000 Block A trials total under Option V)
provide additional sensitivity. If the pilot shows the local distractor cost is small, the SESOI
consultation with the supervisor happens **before** data collection, not after.

### 3.4 Conditions are the only thing that changes

Within each block, the stimulus material, seating, lighting, controller, and task are identical across
conditions; only the shader state differs (vignette parameters / `_DrIntensity`). In vignette-off
conditions the focus window is still painted by the participant (Block A) so the motor and procedural
experience is identical — the v1 principle, retained.

### 3.5 Why these modes, and why these pairings

The three modes are not interchangeable strengths of one effect; they occupy distinct positions in
the design space (`framing.md` §3.1), and each pairing matches a mode's mechanism to the task
paradigm that can detect it:

- **Hard Dark → Block A (distractor suppression).** Dark modes *remove* peripheral signal; the
  workstation paradigm measures exactly what removal should buy (reduced distractor cost). Hard Dark
  is chosen over Soft Dark for the confirmatory test because it is the strongest manipulation —
  maximizing the chance of detecting the mechanism if it exists.
- **ColorPop → Block B (salience re-grading).** ColorPop does not primarily remove signal; it
  re-grades salience. The probe-detection paradigm measures what re-grading should buy (faster focal
  detection, H2a) and what it must not cost (peripheral awareness, H2b).
- **Soft Dark → Block C only.** Soft Dark is retained not as "a weaker Hard Dark" but as the design
  space's load-bearing middle point, for four reasons: (1) it is the **minimal manipulation** — pure
  luminance attenuation, the only unconfounded member of the mode family (ColorPop bundles
  desaturation + salience re-grading + glare compression + dimming); (2) it is the graded **Tier-2**
  point — dimmed *real* passthrough at native quality rather than a re-rendered camera copy; (3) it
  has **no camera pipeline**, making it the only mode plausibly deployable for hour-long sessions
  (published on-device PCA processing throttles within 5–10 min — arXiv 2509.18929); (4) it is the
  **awareness-preserving** point between full signal and Hard Dark's blackout. A full confirmatory
  block for Soft Dark does not fit the session budget, so it receives sampler-level subjective data
  only — an explicit, stated trade-off.
- **Mode × scenario confound, stated plainly:** Hard Dark is tested only at the workstation and
  ColorPop only in the driving scene. All claims are therefore *mode-in-context* ("Hard Dark reduces
  distractor cost in a workstation task"), never mode-generalized ("dark vignettes work"). Block C
  softens this slightly by showing all three modes on one stimulus, for subjective ratings only.

---

## 4. Block Protocols

### 4.0 Common setup

- Room: fixed lab/office room, **bright, constant lighting** (passthrough quality degrades in low
  light — §12, VST perceptual-gap study). Same room, same layout, every session. Window blinds set to
  a marked position.
- Participant seated on a fixed (non-swivel-marked) chair; desk in front.
- Quest 3 with controllers; hand tracking disabled. Experimenter monitors via `scrcpy` mirror.
- All stimuli are **deterministic and scripted**. Live YOLO detection is not used anywhere in the
  study (the shipping system does not use it either; ColorPop is detector-free by design).

---

### 4.1 Block A — PRIMARY: Workstation Focus (Hard Dark, real passthrough)

**Question this block answers:** does blacking out the periphery around a world-locked focus window
reduce the cost of real peripheral distractors on a focal task?

#### Physical layout

- A4/A3 task surface flat on the desk directly ahead (Option P) **or** a virtual task panel
  world-locked at the same position, ~0.6 m from the eyes (Option V).
- Two tablets on stands at **±35° azimuth** from the task centre, ~1 m from the participant, screens
  facing them. 35° sits in the near-periphery zone of maximal involuntary capture (UFOV literature, v1
  §11.5 — rationale retained).
- Tablet content: a prepared **distractor reel** (fast-cut, high-motion, bright video) with
  **scheduled salient event bursts** (hard cuts to full-screen colour flashes + sudden motion) at
  fixed timestamps, ~every 20–30 s, identical across participants. In *Distractors-absent* conditions
  the tablets show a static black screen (still physically present, so the visual layout minus motion
  is constant).

#### Conditions (2×2 within, ~3.5 min each, balanced Latin square)

| Condition | Vignette | Tablets |
|---|---|---|
| A1 | Off (`_DrIntensity = 0`, window painted but invisible) | Static black |
| A2 | Off | Distractor reel |
| A3 | Hard Dark On (window live) | Static black |
| A4 | Hard Dark On | Distractor reel |

Window placement: before the first Block A condition, the participant paints the focus window around
the task surface with the right trigger (the system's normal interaction), release to lock;
experimenter verifies both tablets fall outside the window and the whole task surface falls inside.
The **same locked window** persists across all four conditions (re-verified between conditions); only
`_DrIntensity` toggles.

#### Task — Option V (virtual sustained-attention panel) *(pilot decides V vs P — §6 gate 2)*

A world-locked panel inside the focus window shows a **Go/No-go shape stream**: one large shape
(≥ 3° visual angle) every 1.5 s for 700 ms. Target = circle (press trigger); non-target = square
(withhold). 80 % go / 20 % no-go, seeded pseudo-random per (participant, condition) — exact no-go count, no
two consecutive no-gos, go runs capped at 8. ~140 trials per condition. Every stimulus onset and every press is logged to the millisecond.

- **Errors:** commission (press on square) + omission (no press on circle).
- **RT:** correct go-trial presses.

#### Task — Option P (large-print digit cancellation, as ethics-approved)

The v1 digit-cancellation task, with two changes: **large-print digits** (pilot-verified legible
through passthrough at working distance — expected ≥ 24 pt equivalent) and **four matched sheets**
(same digit count, same target frequency, order shuffled) assigned to conditions via the Latin square.
Target digit differs per sheet (7, 4, 2, 9) to prevent target-specific learning; targets equated for
visual confusability. Scored after the session: hits, misses, false alarms, digits reached.

- **Errors:** misses + false alarms, normalised per digit scanned.
- **Throughput:** digits scanned in 3.5 min.

#### Measures

| Measure | Instrument / ground truth |
|---|---|
| Error rate per condition (primary DV input) | StudyLogger event log (V) / scored sheets (P) |
| **Distractor cost** = errors(A2) − errors(A1), and errors(A4) − errors(A3) | Derived per participant |
| **H1 statistic** = cost(Off) − cost(On) | Derived per participant; also modelled trial-level (V) |
| RT (Option V only) | StudyLogger, ms |
| Head-turns toward tablets (> 30° yaw from task centre): count + dwell | Head-pose telemetry, 60 Hz |
| Raw NASA-TLX | Tablet form after **each** of A1–A4 — the distractor-absent administrations give a workload baseline and a workload analogue of the H1 interaction |
| Noticing check | One question after Block A: "Did anything make the side screens easier or harder to ignore?" (open) |

#### Participant script (verbatim)

> *(Option V)* "A panel in front of you will show one shape at a time. **When you see a circle, pull
> the trigger. When you see a square, do nothing.** Respond as fast as you can without guessing.
> Ignore everything else in the room — only the panel matters. Each round lasts about three and a half
> minutes; there are four rounds. Some rounds may look different from others; just do the same thing
> every round."
>
> *(Option P)* "Cross out every **[target digit]** on this sheet, working left to right, row by row,
> as quickly and accurately as you can. Don't skip rows. Only the sheet matters — ignore everything
> else in the room. You have three and a half minutes; I'll tell you when to stop."

Participants are never told which rounds are "ours" or what the vignette is expected to do
(instructions say "compare display modes" — demand-characteristics defense, §9).

---

### 4.2 Block B — SECONDARY: Driving Probe Detection (ColorPop, video test scene)

**Question this block answers:** does ColorPop speed focal-event detection (H2a) without degrading
peripheral awareness beyond the safety margin (H2b)?

#### Stimulus

The existing `VideoTestScene` equirect driving footage on the sphere (the 180 s `DebugVideo.mp4`
extended, or a second matched urban clip appended, to give **two ~4 min segments** — same road type,
similar event density; segment↔condition assignment counterbalanced). Focus window pre-set to the
windscreen region (fixed for all participants, defined in azimuth/elevation — no painting in this
block, to keep the region constant).

#### Conditions (within, order alternated by participant parity)

| Condition | Shader state |
|---|---|
| B1 | Vignette Off (raw video) |
| B2 | ColorPop On (with motion suppression active, per shipping defaults) |

#### Probe task (Detection Response Task, ISO 17488 lineage — v1 grounding retained)

**24 probes per condition** — brief virtual diamond flashes composited over the video, ~300 ms
duration (size and contrast pilot-calibrated to 70–90 % baseline hit rate), onset times scripted with
6–12 s jitter (mean ~9 s → the 24-probe schedule spans ~216 s, leaving ~24 s of slack inside the
4-min segment to absorb head-yaw deferrals), positions scripted at known eccentricities **relative to the focus-window centre**:

- 12 **central** probes: < 10° eccentricity (inside the window).
- 12 **peripheral** probes: 25–40° eccentricity (outside the window, within the headset FOV given
  forward-facing head; probe schedule pauses while head yaw is > 20° off-forward and resumes when
  settled, so every probe is actually displayable in-FOV — logged either way).

**Hit** = trigger press 100–2500 ms after onset (ISO DRT window). Presses outside any window count as
**false alarms**. Every onset and press is machine-logged: RT, hit rate, and false-alarm rate are exact.

#### Measures

| Measure | Instrument |
|---|---|
| Central probe RT (median) & hit rate | ProbeScheduler + StudyLogger |
| Peripheral probe hit rate (H2b input) | ProbeScheduler + StudyLogger |
| False alarms per condition | StudyLogger |
| Head yaw/pitch telemetry, angular speed | StudyLogger, 60 Hz |
| Raw NASA-TLX | After each of B1, B2 |

#### Participant script (verbatim)

> "You'll watch a driving scene. Small **diamond shapes will flash briefly** anywhere in the scene —
> sometimes in front of you, sometimes off to the side. **Pull the trigger as soon as you see a
> diamond. That's the only thing you need to do.** Don't press for anything else. Keep watching the
> road ahead as if you were the driver, and stay seated. Each clip is about four minutes; there are two."

---

### 4.3 Block C — EXPLORATORY: Compact Mode Sampler (skippable)

**Question this block answers:** how do the three modes compare subjectively on a common stimulus?
(No confirmatory claims; the Cheng CHI 2022 pattern — structured ratings + short interview.) This is
the only block in which **Soft Dark** is experienced — its retention rationale is §3.5.

- Participant watches the driving video with each of **ColorPop, Soft Dark, Hard Dark** for ~75 s
  each, order from a 3×3 Latin square. No task — free viewing with the focus window pre-set.
- After each mode, three 7-point Likert items (verbally administered, experimenter records):
  1. "This effect was comfortable to look at." (comfort)
  2. "This effect would help me concentrate on what's in the window." (perceived focus benefit)
  3. "I would use this effect for real work or study." (willingness to use)
- After all three modes, a **3-question interview** (~3 min, headset off; audio-recorded only if
  consent wording permits, otherwise experimenter notes):
  1. Which mode would you choose, and for what activity?
  2. What did the edges of your vision feel like — and did you notice the effect fading when you
     turned your head? How did that feel?
  3. What would stop you using this for an hour of real work?

**Skip rule (pre-registered):** Block C is dropped without discussion if the participant shows or
reports fatigue or symptoms at the post-Block-B comfort check, or if the session is running more than
5 minutes over. Only exploratory data is lost; the confirmatory blocks are never compressed to save
the sampler. Sampler completion rate is reported.

**Caveats, stated honestly:** three brief, task-free, order-affected exposures answer "how did each
mode feel?", not "which mode is better?" Ratings are reported as exploratory only.

#### Participant script (verbatim)

> "No task now — just watch the scene. I'll switch between three different visual effects, about a
> minute each. After each one I'll ask you three quick rating questions. Look around as much as you
> like."

---

## 5. Session Timeline

Total: **~53 minutes** — inside the 40–60 min tolerance window participants typically accept. The
information sheet is sent to participants **in advance**; raw TLX (~1 min each, on-panel or tablet)
follows **every** A and B condition; Block C is last and skippable (§4.3), so a fatigued session
closes at ~44 min without touching confirmatory data.

```
00:00 [ 4 min] Welcome, written consent (info sheet pre-read), demographics +
               VR-experience form, VRSQ (pre)
04:00 [4.5   ] Headset fitting & comfort check; tutorial: paint/lock a practice
               window; practice to criterion (Option V: ≥90% go-accuracy on a
               1-min practice stream; Option P: one practice row; B-probe practice:
               6 practice probes, ≥4 hits). One repeat allowed if criterion missed.
─────────────── BLOCK A — Workstation (Hard Dark), Latin-square order ───────────────
08:30 [ 1 min] Paint & lock focus window; experimenter verifies geometry
09:30 [3.5   ] Condition i        (A1–A4 per Latin square)
13:00 [ 1 min] TLX + reset (tablets re-cued / sheet swapped)
14:00 [3.5   ] Condition ii
17:30 [ 1 min] TLX + reset
18:30 [3.5   ] Condition iii
22:00 [ 1 min] TLX + reset
23:00 [3.5   ] Condition iv
26:30 [1.5   ] TLX + noticing question
28:00 [1.5   ] Headset-off break; verbal comfort check (stop rule §7.2 applies
               throughout); water offered
─────────────── BLOCK B — Driving probes (ColorPop vs Off), parity order ────────────
29:30 [ 4 min] Condition i (B1 or B2)
33:30 [ 1 min] TLX
34:30 [ 4 min] Condition ii
38:30 [ 1 min] TLX + fatigue/comfort check (Block C skip rule applied here)
─────────────── BLOCK C — Compact mode sampler (exploratory, skippable) ─────────────
39:30 [ 6 min] 3 × (75 s mode + 3 Likert items verbally)
45:30 [ 3 min] 3-question interview (headset off)
─────────────── Close ────────────────────────────────────────────────────────────────
48:30 [4.5   ] VRSQ (post), SUS, study-aims debrief, thanks
53:00          Experimenter: run validate_session.py BEFORE participant leaves;
               file incident log entry if anything deviated
```

Arithmetic check: open = 8.5; Block A = 19.5 (1 + 4×3.5 + 3×1 + 1.5); break 1.5; Block B = 10
(2×4 + 2×1); Block C = 9; close 4.5 → 53 min. With Block C skipped the session closes at ~44 min.

---

## 6. Pilot Study with Pass/Fail Gates

**n = 3–5 (not in the main sample), week 1.** The pilot exists to make an inconclusive main study
impossible. The main study **does not launch** until all gates pass. Pilot data never enters the main
analysis. Stimulus parameters may be tuned between pilot participants; hypotheses, margins, and
analyses may not.

| Gate | Test | Pass criterion | On fail |
|---|---|---|---|
| **G1 — distractors distract** | Run A1 vs A2 (vignette Off) on pilot participants | Visible error-rate increase under distractors for all but at most one pilot participant (direction, not significance) | Increase distractor salience: louder motion bursts, brighter flashes, closer/larger tablets, add audio? (audio would need ethics check) — then re-test |
| **G2 — task form** | Both task options tried through passthrough | Option V: panel fully legible, go-accuracy 75–95 % (off ceiling & floor). Option P: every pilot reads the large-print digits without leaning in; miss rate off floor | Choose the option that passes; if both pass, choose **V** (finer measurement); if only P passes, use P; if neither, redesign task before proceeding |
| **G3 — probe calibration** | Block B with candidate probe sizes/contrasts | Baseline (vignette-off) hit rate 70–90 % for both eccentricity bands; RT distribution unimodal | Adjust probe size/duration/contrast per band; re-test |
| **G4 — protocol dry run ×2** | Two full end-to-end sessions with the final configuration | Timeline within ±5 min; zero log gaps (validator clean); battery ≥ 20 % at end; no sickness terminations; TLX/questionnaire flow smooth | Fix the specific failure (checklist §10), re-run one dry run |

**Post-pilot lock-in memo** (one page, to supervisor): the chosen task form, tuned stimulus
parameters, the pilot-observed distractor cost, and the **finalised absolute SESOI** for H1 derived
from it. After this memo, nothing changes until data collection ends.

---

## 7. Questionnaires & Safety Protocol

### 7.1 Instruments

| Instrument | When | Notes |
|---|---|---|
| Demographics + VR experience (frequency of VR use: never / a few times / monthly / weekly+) | Pre | VR experience used as covariate/robustness split |
| **VRSQ** (Kim et al. 2018 — 9 items, oculomotor + disorientation) | Pre **and** post | Chosen over full SSQ for session fit; administered pre as well as post because post-only scores are uninterpretable without a baseline (SSQ zero-baseline critique, §12) |
| **Raw NASA-TLX** (Hart 2006; unweighted, standard in HCI) | After **every** A and B condition (6×) | Six subscales, 0–100 |
| Block C Likert (3 items × 3 modes) | Block C | 7-point, verbal |
| **SUS** | End of session | On the system as a whole |
| Block C interview | Block C end | 3 fixed questions §4.3 |

### 7.2 Stop rule (written, applied without discussion)

- Participant may stop at any time without giving a reason (per approved consent).
- Experimenter **terminates the VR portion immediately** if the participant reports nausea/dizziness
  at moderate or worse level at any check, or spontaneously at any time; or if the experimenter
  observes distress. Verbal comfort checks: after Block A (the headset-off break), a mandatory
  fatigue/comfort check after Block B (which doubles as the Block C skip gate, §4.3), and between
  any conditions on request.
- After termination: seated rest, water, participant stays until symptoms subside; data up to
  termination retained per consent; participant replaced from the spare pool.
- Sessions are seated with no artificial locomotion, ≤ 45 min headset time with a mid-session
  headset-off break — the low-risk end of VR study design.

---

## 8. Analysis Plan & Decision Table

All analysis in Python (pandas, pingouin, statsmodels) or R; scripts written and dry-run on pilot
data **before** main data collection. α = .05. Effect sizes and CIs reported for every test — 95 %
two-sided by default; 90 % where one-sided/equivalence logic applies (the H1 SESOI bound and the
H2b TOST).

### 8.1 Confirmatory analyses

| Hypothesis | Primary test | Robustness check | Effect size |
|---|---|---|---|
| **H1** | Linear mixed model on trial-level errors (Option V; logistic LMM: error ~ vignette × distractor + (1 + distractor | participant)); the interaction term is the test. For Option P: paired t-test on per-participant cost difference [cost(Off) − cost(On)] | Paired t-test on per-participant condition means (V); Wilcoxon signed-rank if Shapiro-Wilk rejects normality | dz on the cost difference; odds ratio from LMM |
| **H2a** | Paired t-test (Wilcoxon fallback) on per-participant median central RT, B1 vs B2; hit rate checked descriptively for non-inferiority | LMM on trial-level RT (correct trials, log-RT) | dz |
| **H2b** | **TOST** on peripheral hit rate (paired), margin Δ = 10 pp | Bayesian paired test as sensitivity check (optional) | Mean difference + 90 % CI vs margin |
| **H3** | Paired t-test/Wilcoxon on TLX global score, A4 vs A2 | Per-subscale exploratory, Holm-corrected | dz / r |
| **MC1** | Paired test, A2 vs A1 errors (vignette-off cost > 0) | — | dz |

Multiplicity: H1 is the **single primary endpoint** — no correction. H2a/H2b/H3 form the secondary
family — Holm correction across the three. Exploratory results (Block C, head-turn telemetry, TLX
subscales) are labelled exploratory and never quoted as confirmatory findings.

### 8.2 Exploratory analyses

- Block C Likert: Friedman across the three modes per item; Bonferroni-corrected pairwise Wilcoxon
  (Cheng 2022 pattern); Kendall's W reported. Exploratory only (§4.3 caveats); sampler completion
  rate reported alongside.
- Head-turn counts/dwell toward tablets (Block A) and head angular speed (Block B): descriptive +
  paired tests, exploratory (head pose is a validated gaze proxy at these eccentricities — Higgins
  2022; Sitzmann 2018).
- TLX workload interaction (the reason TLX follows all four A conditions): 2×2 Vignette × Distractor
  on the global TLX score — the workload analogue of H1. Exploratory; reported alongside H3.
- Block C interview: thematic coding into a small table (mode → recurring likes/concerns); quotes to
  illustrate quantitative findings.
- VR-experience covariate: repeat H1 within experienced/naive subgroups as a robustness description.

### 8.3 Decision table (pre-committed — the dissertation reports the row that occurred)

| # | Outcome pattern | Dissertation conclusion |
|---|---|---|
| 1 | H1 supported (interaction in predicted direction, ≥ SESOI) **and** H2b passes equivalence | **The system works**: peripheral DR dimming reduces distraction cost, and the salience mode does not measurably harm peripheral awareness. |
| 2 | H1 supported, H2b **fails** (drop > 10 pp) | The system works **for focus, at a quantified situational-awareness cost** — a conclusive, qualified result with a design implication (mode choice by context). |
| 3 | H1 **refuted** (90 % CI on the recovered-cost proportion excludes the SESOI from below) | **The system does not deliver a meaningful benefit** in its core use case — a conclusive negative. The dissertation reports the bounded estimate ("the vignette recovers at most X % of the distraction cost"). |
| 4 | H1 estimate positive but CI spans both SESOI and smaller values | The benefit is real-signed but **cannot be claimed at the pre-set size**; report the estimate with its CI as a bounded conclusion. (Made unlikely by the pilot gate + trial counts; if it occurs, it is reported as such — not spun.) |
| 5 | MC1 fails in the main study (no distractor cost despite pilot pass) | H1 is not interpretable; report the manipulation failure honestly and fall back on H2a/H2b + Block C as the evidential core. (§8.4 limits this risk.) |
| 6 | Pilot gates fail after tuning | **Main study does not launch**; the paradigm is redesigned with the supervisor. Inconclusive main-study data is prevented, not explained. |

### 8.4 Pre-registered exclusion & data-quality rules (fixed before collection)

| Rule | Action |
|---|---|
| VR-sickness termination | Session ends (§7.2); all partial data retained but participant excluded from confirmatory analysis; replaced from spares |
| App crash / restart during a condition | That condition void; re-run once at the end of its block if time allows, else participant contributes remaining conditions to LMM (which tolerates missingness) and is excluded from the paired robustness test |
| Option V: go-accuracy < 60 % in the *distractor-absent, vignette-off* condition (A1) | Participant did not perform the task; exclude, replace |
| Block B: false-alarm rate > 30 % of presses in either condition | Response strategy invalid (pressing indiscriminately); exclude Block B for that participant |
| Log integrity failure (validator reports gaps > 1 s or missing condition markers) | Affected condition void; incident log entry; same re-run rule as crashes |
| Participant recognises the driving footage | Note; exclude only if they report strong familiarity affecting behaviour (v1 rule retained) |
| Missing TLX (≥ 2 subscales blank) | That TLX excluded from H3; behavioural data retained (v1 rule retained) |

---

## 9. Threats to Validity & Mitigations

| Threat | Mitigation |
|---|---|
| **Passthrough acuity confound** (Quest 3 VST never reaches normal acuity; worse in low light) | No fine-text reading through passthrough anywhere in the design. Option V renders the task; Option P is admitted only if the pilot proves large-print legibility (G2). Bright constant lighting; lighting position on the pre-session checklist. |
| **Novelty effect** (first-exposure wow/confusion inflates or deflates early conditions) | Practice-to-criterion tutorial before any measured condition; practice includes the vignette so its first appearance is never during a measured trial; Latin square distributes residual novelty across conditions. |
| **Order & learning effects** | Balanced Latin square (A), parity counterbalance (B); matched sheets/segments; equal trial counts. Fixed A→B block order is a limitation: Block B is always second — acceptable because B's comparison is internal to the block and counterbalanced within it. |
| **Coverage limits from the session budget** | Soft Dark receives sampler-level subjective data only, no confirmatory task block (rationale §3.5); sampler ratings come from brief task-free exposures and may be lost to the skip rule (completion rate reported); the mode × scenario confound is inherent and stated (§3.5). |
| **Demand characteristics** | Primary DVs are objective (errors, ms RT, telemetry). Neutral framing ("comparing display modes"); participants never told which condition is the "system". The noticing question comes *after* Block A data is collected. |
| **Distractor floor** (distractors fail to distract → H1 untestable) | Pilot gate G1 + main-study manipulation check MC1 + decision-table row 5. |
| **Simulator sickness / dropout** | Seated, no locomotion, ≤ 45 min headset time, mid-session break, VRSQ pre/post, stop rule, N + 4 recruitment. |
| **Small-N insensitivity** | Within-subjects + high trial counts + LMM; single primary endpoint; SESOI + equivalence margins make nulls informative; effect sizes + CIs always reported. |
| **Experimenter variability** | One experimenter, verbatim scripts (§14), printed checklist per session. |
| **Ecological validity** (driving is a video, workstation distractors are tablets) | Acknowledged in the dissertation's discussion: the claim tested is about attention mechanisms under controlled distraction, with the driving scenario as a stimulus context — not a road-safety claim. This honesty is what keeps the conclusions inside what the data can support. |
| **Stereo comfort of camera-fed effects** (project-specific) | The dark modes are overlay-only (no camera repaint); ColorPop runs in the video scene (no live camera fusion). Head motion moderate by task design (seated). |

---

## 10. Operational Protocol (Murphy's Law Engineering)

### 10.1 Determinism

- **No live ML, no improvised stimuli.** Every distractor burst, probe onset, and shape trial comes
  from versioned schedule files (JSON) checked into the repo. A session is reproducible from its
  participant ID + schedule version.
- Condition order derives from participant ID via the Latin-square table in `ConditionSequencer` —
  no experimenter arithmetic mid-session.

### 10.2 Pre-session checklist (printed, ticked every session)

1. Headset battery ≥ 80 %; controllers ≥ 50 %; tablets ≥ 60 % and charger present.
2. Guardian set to stationary; room lighting at marked setting; blinds at marked position.
3. Tablets on marked floor/desk positions (±35° verified with the printed angle template).
4. Correct APK build hash (shown on the app's start screen) matches the locked pilot build.
5. Free storage on device > 2 GB; previous session's files already pulled and archived.
6. `scrcpy` mirror running and **screen recording started** (redundant capture of everything).
7. Spare: printed sheets ×2 sets, pens ×3, cleaning wipes, sick bags, water.
8. Consent forms, demographics forms, VRSQ/TLX/SUS forms (or tablet forms loaded).

### 10.3 Logging & redundancy

- `StudyLogger` writes one CSV row per frame (~60 Hz) with heartbeat semantics — an unbroken
  timestamp chain is itself evidence of integrity; flush every frame so a crash loses < 1 s.
- Every event (condition start/end, stimulus onset, press, window lock, TLX start) is a labelled row.
- Redundant channel: the `scrcpy`/`adb screenrecord` capture lets any disputed trial be re-checked
  visually.
- **`Tools/validate_session.py` runs before the participant leaves**: verifies expected condition
  count and durations, timestamp continuity, per-condition event counts (e.g., exactly 24 probe
  onsets), and writes a PASS/FAIL summary. A FAIL triggers the re-run rule (§8.4) while the
  participant is still available.
- Files pulled via `adb pull` and copied to two locations (laptop + cloud) before the next session.

### 10.4 Incident log

A running `incidents.md`: date, participant code, what deviated, action taken, which pre-registered
rule applied. Cited verbatim in the dissertation's method section — deviations are reported, not
hidden.

---

## 11. Ethics Delta & Amendment Checklist

Approved protocol (v1) vs this design — classification for the supervisor/ethics conversation:

| Change | Class | Note |
|---|---|---|
| Population, consent process, withdrawal rights, session length (~40 → ~53 min, ~44 if Block C skipped) | Unchanged / minor | Verify the information sheet wording; update the stated duration (usually an administrative amendment). Block C's skip rule keeps the confirmatory session near the approved length. |
| Seated video-watching tasks, button presses, digit cancellation, tablets as distractors, NASA-TLX | Unchanged | Same task classes as approved. |
| Between-subjects groups **dropped**; all-within design | Minor/none | Reducing conditions per person's group assignment to a common protocol is a simplification; typically no amendment needed. |
| Classroom lecture block **dropped** | None | Removal of a task. |
| TLX after each condition instead of per block | None | Same instrument, more administrations. |
| **VRSQ added** (pre/post) | Minor | New instrument, non-invasive; usually covered by generic questionnaire wording — verify. |
| **Option V virtual task panel** (if pilot selects it) | **Check wording** | Same risk profile as approved tasks; if the approval names "digit cancellation" specifically, file a minor amendment or run Option P. This is a named pilot contingency. |
| **Audio recording of the Block C interview** | **Check wording** | If the approved consent does not cover audio, use experimenter notes instead (protocol already permits this). |
| Pilot study (n = 3–5) | Check | Confirm the approval covers piloting; if not, run pilots under the same consent as a protocol-development activity per the ethics office's guidance. |

**Action item (before pilot week):** re-read the approved application against this table; anything in
the "check" rows goes to the supervisor with this document.

---

## 12. Literature Grounding

Retained from v1 (still valid): DRT lineage (ISO 17488:2016; Jahn et al. 2005); NASA-TLX
(Hart & Staveland 1988; raw-TLX justification Byers et al. 1989); UFOV rationale for 35° distractor
placement (Ball & Owsley); digit-cancellation validity (Bourdon 1895; Hatta & Yoshizaki); SGD
eccentricity threshold (Bailey et al. 2009).

Added/updated grounding for the v2 design:

| Design element | Source | Link |
|---|---|---|
| Within-subject DR evaluation at N = 16/12; Friedman + Bonferroni-Wilcoxon for Likert; counterbalanced scenarios; ratings + interview pattern (Block C) | Cheng, Yin, Yan, Gugenheimer, Lindlbauer — "Towards Understanding Diminished Reality", CHI 2022 | https://dl.acm.org/doi/10.1145/3491102.3517452 |
| DR attenuation of distractions evaluated within-subject with linear mixed models; focus-vs-awareness trade-off (basis for H2b framing) | McLaughlin et al. — "Cognitive Aid Design Using Diminished Reality…", Human Factors 2025 | https://journals.sagepub.com/doi/10.1177/00187208251325169 |
| Peripheral distractors reliably raise sustained-attention errors in HMDs (commission 1.33 → 3.15, p < .001, N = 66) — the effect H1 targets; errors (not RT) were the sensitive DV | "Impact of visual distractors in VR on sustained attention", Frontiers in Human Neuroscience 2025 | https://pmc.ncbi.nlm.nih.gov/articles/PMC12698649/ |
| Saliency-modulation evaluation with time-to-first-fixation / target-area measures, N = 20 within-subject (ColorPop's own recipe source) | Sutton, Langlotz, Plopski, Zollmann, Itoh, Regenbrecht — "Look over there!", UIST 2022 | https://dl.acm.org/doi/10.1145/3526113.3545633 |
| Area darkening guides attention in 360° video; desaturation nearly ineffective (motivates testing dark modes as the guidance workhorse) | "Attention-guiding Methods in Cinematic VR Based on Eye Tracking", IEEE 2021 | https://ieeexplore.ieee.org/document/9522492 |
| Guidance-efficiency measurement in immersive environments (time-to-fixate, noticeability) | Grogorick et al., ACM SAP 2017; dome comparison 2018 | https://dl.acm.org/doi/10.1145/3119881.3119890 |
| Head pose validated as a gaze proxy in HMDs (justifies telemetry without eye tracking; Quest 3 has none) | Higgins et al., VAM-HRI 2022; Sitzmann et al. 2018 (arXiv 1612.04335) | https://iral.cs.umbc.edu/Pubs/Higgins2022VAM-HRI.pdf |
| Quest 3 video passthrough never reaches normal acuity, worse in low light (kills fine-text tasks; forces G2 gate + bright lighting) | "The perceptual gap between video see-through displays and natural human vision", arXiv 2026 | https://arxiv.org/pdf/2601.02805 |
| Sample-size norms: CHI modal N = 12; small-N power via repeated measures | Caine — "Local Standards for Sample Size at CHI", CHI 2016 | https://dl.acm.org/doi/10.1145/2858036.2858498 |
| VRSQ instrument (9 items, HMD-specific) | Kim et al., Applied Ergonomics 2018 | https://www.sciencedirect.com/science/article/abs/pii/S000368701730282X |
| Pre/post sickness measurement requirement (post-only uninterpretable) | SSQ zero-baseline critique, Frontiers in Virtual Reality 2022 | https://www.frontiersin.org/journals/virtual-reality/articles/10.3389/frvir.2022.945800/full |
| Raw (unweighted) TLX as current standard | Hart — "NASA-TLX 20 Years Later", HFES 2006 | https://human-factors.arc.nasa.gov/groups/TLX/downloads/HFES_2006_Paper.pdf |
| Equivalence testing (TOST) for the H2b safety margin | Lakens — "Equivalence Tests", SPPS 2017 (doi:10.1177/1948550617697177) | — |
| Positive-control/guidance-comparator precedent; workload −18 %, search +22 % for AR guidance | Biocca et al. — "Attention Funnel", CHI 2006 | https://ieeexplore.ieee.org/document/1579336/ |
| Nonparametric factorial option (ART) and its recent critique (prefer LMMs) | Wobbrock et al., CHI 2011; ART critique | https://statransform.github.io/jovi/ |

---

## 13. Appendix A — Tooling Specification

*(Status 2026-07-05: **BUILT**. C# components live in
`Assets/PassthroughCameraApiSamples/ShaderSample/Scripts/Study/` (wiring & session runbook in that
folder's README.md; both vignette managers now implement `IStudyVignetteControl`); Python tools in
`Tools/` (`README-study-tools.md`). Known deviation: the CPT sequence constraint is implemented as
"no two consecutive no-gos, go runs ≤ 8" because "no more than three identical in a row" is
unsatisfiable at 80 % go rate — §4.1 wording to be aligned. Compile check in the Unity editor and an
on-device dry run are pending (pilot gate G4).)*

| Component | Spec |
|---|---|
| `StudyLogger.cs` | MonoBehaviour; one CSV row/frame: `timestamp_ms, participant_id, block, condition, head_yaw, head_pitch, head_roll, head_ang_speed, dr_intensity, mode, event_label, event_payload`. Flush every frame. Event rows for: `SESSION_START/END, CONDITION_START/END, WINDOW_LOCKED (az/el bounds), STIM_ONSET (id, type, az, el), PRESS, TLX_START/END, INCIDENT`. Output to app external files dir; pulled via `adb pull`. |
| `ConditionSequencer.cs` | Takes participant ID; looks up Block A Latin-square row, Block B parity order, Block C order; drives condition transitions on experimenter keypress (mirrored device or volume-button chord); writes all markers via StudyLogger. |
| Study-configuration guards | ConditionSequencer **asserts required flags at session start and refuses to run on mismatch** — e.g. `m_enableMotion = true` in `VideoTestSceneManager` for Block B (the code default is `false`, so a stock scene would silently drop motion suppression), baked-detection playback off during study conditions, correct scene per block — and logs the full configuration snapshot as the session's first event rows. |
| `ProbeScheduler.cs` | Loads a versioned JSON schedule `{t, az_deg, el_deg, duration_ms, band}`; renders the diamond probe as a world-space quad on the sphere at the scheduled direction; defers onsets while head yaw > 20° off-forward; logs actual onset + matched presses (100–2500 ms window) and false alarms. |
| `CPTPanel.cs` (Option V) | World-locked panel; Go/No-go shape stream from a seeded pseudo-random sequence file (per condition); logs every onset/response. Shape size ≥ 3° visual angle at 0.6 m. |
| Distractor reel + tablet procedure | One pre-rendered video per condition slot with scheduled event bursts (edit list checked in); tablets started by countdown from the experimenter ("3-2-1-play" while condition start is marked); burst timestamps documented so head-turn telemetry can be aligned to bursts in analysis. |
| `Tools/validate_session.py` | Reads the session CSV; asserts: monotonic timestamps with no gap > 1 s, expected condition count/durations ±5 %, exactly 24 probe onsets per B condition, ≥ 130 CPT trials per A condition (V), all required markers present. Prints PASS/FAIL + summary; exit code drives the checklist. |
| `Tools/analysis/` | `parse.py` (CSV → tidy trial table), `h1_lmm.py` (statsmodels MixedLM / logistic GEE + paired robustness tests), `h2_tost.py` (pingouin TOST, Δ=10 pp), `likert.py` (Friedman + pairwise Wilcoxon), `power_check.py` (post-pilot SESOI→absolute-margin conversion). All dry-run on pilot data before main collection. |
| Existing assets reused | `VideoTestScene.unity` + `VideoTestSceneManager.cs` (Block B/C stimulus), `CameraSphereVignette.unity` + `CameraSphereVignetteManager.cs` (Block A), `DebugVideo.mp4` (extend per §4.2). |

---

## 14. Appendix B — Verbatim Scripts & Forms

**Session-opening framing (read aloud):**
> "Today you'll try a headset that shows you the real room through its cameras, with a few different
> display modes. We're comparing how these display modes affect simple visual tasks. There are no
> right or wrong reactions — the modes are being tested, not you. You can stop at any time, for any
> reason, and you don't have to tell me why. If you feel unwell at any point — dizzy, queasy, eye
> strain — say so straight away and we'll stop."

**Block scripts:** §4.1, §4.2, §4.3 (used verbatim; no ad-libbed task descriptions).

**Between-condition line (identical every time):**
> "That round is done. Please answer the workload questions, then we'll start the next round. Same
> task as before."

**Debrief (read aloud):**
> "Thanks — here's what we were testing. The headset can dim or re-colour the edges of your vision to
> help you focus on one area. Some rounds had the effect on, some off, and some rounds had the side
> screens playing videos to try to distract you. We're measuring whether the effect protects your
> focus from that kind of distraction. Your data is stored under a code, not your name. Any questions?"

**Forms bundle (prepared before pilot week):** consent (approved version), demographics + VR
experience, VRSQ ×2, raw TLX ×6, Block C Likert sheet, SUS, incident log template, pre-session
checklist card, printed 35° angle template.

---

*Document v2 — prepared 2026-07-05. Supersedes `dissertation-study-methodology.md` (v1) for design;
v1 §11's paradigm-to-literature mappings that remain valid are incorporated by reference in §12.*
