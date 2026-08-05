# Chapter 6 — User Study Design

> **Status note.** The study described in this chapter is fully designed and pre-registered in
> substance, but the **formal, frozen-protocol confirmatory collection has not yet run**. All
> protocol elements are therefore written in the future tense. What has happened instead is
> extensive iterative piloting — informal ("naive-pilot") sessions across two rounds in late July
> 2026, well beyond the ~20 numbered pilot participants the design's pass/fail gates alone would
> require — used to tune stimulus parameters (task form, window size, periphery-dim strength,
> distractor pool) and surface data-quality issues (session aborts, mislabelled participant IDs,
> false-alarm-rate outliers) ahead of a single supervisor-agreed protocol freeze. None of this
> iteration data enters, or is intended to enter, the confirmatory analysis: it is gate/design-
> motivation evidence only, explicitly never pooled with the formal sample (see the exclusion
> handling of Section 6.9.4). The design is fixed before that formal collection begins: the
> pass/fail pilot of Section 6.8.2 may tune stimulus parameters up to the freeze point, but once the
> protocol is frozen and formal collection starts, hypotheses, margins, and analysis choices may not
> change. The operational source document for this chapter is `testing-strategy-v2.md`; where this
> chapter compresses, that document remains normative.

## 6.1 Rationale and Methodological Positioning

### 6.1.1 What the study must be able to conclude

The purpose of this study is to determine whether the diminished-reality (DR) attention-guidance
system described in Chapters 3 and 4 *works* — and, just as importantly, to be able to determine that
it *does not* work, if that is what the data show. This dual requirement shapes every design decision
in the chapter. A study that can only confirm is not an experiment; it is a demonstration. An earlier
version of this methodology suffered from exactly that defect: it paired an underpowered
between-subjects design with hypotheses phrased so that no observable outcome could refute them
("peripheral detection holds steady"; "accuracy is maintained or improves"). A non-significant
difference in a small sample is evidence of nothing, and a design whose safety hypotheses are bare
nulls has no way to fail informatively.

The present design removes the possibility of an inconclusive outcome by reframing the research
question around an effect that is already established in the literature. Peripheral visual
distractors reliably impose a measurable performance cost on a focal sustained-attention task in a
head-mounted display: in a virtual-classroom Go/No-go study with 66 participants, commission errors
more than doubled in the presence of peripheral distractor events (1.33 → 3.15, *p* < .001), with
omission errors showing the same pattern (Ai et al., 2025). The primary question is therefore not the unconstrained "does the
vignette make people better?" but the falsifiable:

> *Peripheral distractors impose a measurable cost on a focal task. Does the DR vignette reduce that
> cost?*

Under this framing, every outcome is informative. If the vignette works, the distractor cost shrinks
when the vignette is active. If the vignette does nothing, the cost remains at its vignette-off size
— a bounded, conclusive negative. The only failure mode that could render the comparison
uninterpretable — distractors that fail to distract — is checked twice: once as a hard gate in the
pilot study before the main study is permitted to launch (Section 6.8.2), and once as a manipulation
check inside the main analysis (Section 6.9). There is no configuration of results that yields
"inconclusive".

### 6.1.2 Simulation-based evaluation and the oracle-perception assumption

The study occupies a deliberate position between concept evaluation and artefact evaluation, mirroring
the dissertation's two-part structure (Chapter 1). Two named methodological commitments govern this
position.

First, the dynamic (driving) block is a **simulation-based evaluation**. Participants view
pre-recorded equirectangular driving footage rendered on the system's sphere rather than a live street
scene, in the tradition established by Cheng et al. (2022), who evaluated DR concepts entirely inside
VR simulations because live DR was not feasible. Simulation gives every participant identical,
repeatable, event-dense stimuli — something no real environment provides — at a known cost to
ecological validity that is declared rather than obscured: no claim in this dissertation concerns
driving, drivers, or road safety. The driving footage is a dynamic, high-event-rate monitoring
stimulus, nothing more (see the claim-scoping rules in Chapter 1).

Second, the perception problem is **decoupled from the attention question — twice over**. The
evaluated modes are detector-free by design: ColorPop keys on luminance and colour, and the dark
modes on window geometry alone, so no object detection — live or pre-baked — runs anywhere in this
study; every stimulus is deterministic and scripted. Where detection enters the dissertation at all,
it enters as an oracle: the offline full-resolution detection bake validates the driving stimulus's
event density and anchors the oracle-vs-live benchmark of Chapter 5, standing in for the perfect
detector that future detector-dependent variants would require. The human-factors findings of this
study will therefore remain valid regardless of future improvements in object detection — first
because the evaluated modes require no detector, and second because detector-dependent extensions
are characterised against a measured oracle bound that live systems approach from below.

Critically, the two confirmatory blocks sit at opposite ends of this spectrum. Block A (workstation)
runs on **real passthrough with real physical distractors** — it evaluates the actual artefact as
built. Block B (driving) runs on the simulated video scene — it evaluates the concept under the
ideal-context assumption. Convergence between them strengthens both; divergence is itself a
reportable finding about what simulation-based XR evaluation misses.

### 6.1.3 Why within-subjects with repeated trials

The design is fully within-subjects, with no between-subjects factors, targeting **N = 20 analysed
participants** and a high trial count per condition. Three considerations justify this choice over the
superficially more ambitious alternative of a larger between-groups design.

First, precedent: the two closest published DR evaluations are within-subject. Cheng et al. (2022)
ran their two studies at N = 16 and N = 12; McLaughlin et al. (2025) ran two within-subject
experiments analysed with linear mixed-effects models. Second, field norms: Caine (2016) found the
modal sample size across all CHI user studies to be 12, with in-person studies drawing their
statistical power from repeated measures rather than headcount. Third, arithmetic: each participant
will contribute approximately 140 scored task events per Block A condition (under the virtual task
option) and 24 probe events per Block B condition. Twenty participants therefore yield on the order
of 11,000 Block A trials — a dataset that trial-level mixed models can exploit fully, while
per-participant aggregates feed classical paired tests as robustness checks. A between-subjects
design at feasible recruitment numbers would discard the within-person variance control that makes
small-N studies conclusive; its predecessor's own power analysis condemned it.

## 6.2 Research Questions and Pre-Registered Hypotheses

**RQ1 (primary).** Does peripheral DR dimming (Hard Dark, world-locked window) reduce the performance
cost that peripheral visual distractors impose on a focal workstation task?

**RQ2 (secondary).** Does ColorPop salience re-grading speed detection of focal-region events in a
dynamic driving scene, and does it do so *without* degrading peripheral event detection beyond an
acceptable margin?

**RQ3 (exploratory).** How do the three modes (ColorPop, Soft Dark, Hard Dark) compare subjectively on
comfort, perceived focus benefit, and willingness to use, when sampled on a common stimulus?

Every hypothesis below is stated with the observation that would refute it. A hypothesis without a
refuting observation is not admitted to the design.

| ID | Hypothesis (directional, falsifiable) | Refuting observation | Status |
|---|---|---|---|
| **H1** | The distractor cost on the Block A task (error-rate increase, Distractors-present vs -absent) is smaller with the vignette On than Off — a Vignette × Distractor interaction, with the vignette recovering at least the smallest effect size of interest (SESOI; default ≥ 50 % of the vignette-off distractor cost, finalised from pilot data). | The interaction estimate's 90 % CI excludes the SESOI (benefit too small to matter), or the interaction is absent or reversed. | Primary confirmatory |
| **H2a** | With ColorPop On, median reaction time to central probes (< 10° from the focus centre) is lower, and central hit rate is not lower, than with the vignette Off. | Central RT not reduced (95 % CI on the paired difference includes 0 or favours Off). | Secondary confirmatory |
| **H2b** | With ColorPop On, peripheral probe hit rate (25–40° eccentricity) is *equivalent* to Off within a margin of Δ = 10 percentage points (two one-sided tests, TOST). | Equivalence not established and the point estimate shows a drop > 10 pp — a conclusive finding that the mode harms situational awareness. | Secondary confirmatory (safety) |
| **H3** | NASA-TLX global workload in Block A is lower in Vignette-On + Distractors-present than in Vignette-Off + Distractors-present. | No TLX difference, or higher workload with the vignette. | Secondary |
| **MC1** (manipulation check) | With the vignette Off, Distractors-present produces a higher error rate than Distractors-absent, replicating the established distractor-cost effect. | Failure in the main study despite a pilot pass renders H1 uninterpretable for affected participants; handled by pre-registered rule (Section 6.9.4). | Gate |
| **EQ1–EQ3** (exploratory) | Sampler ratings (comfort, perceived focus benefit, willingness to use) differ across ColorPop / Soft Dark / Hard Dark. | — (exploratory; no confirmatory claim attaches) | Exploratory |

**SESOI rationale.** H1's smallest effect of interest is expressed as a *proportion of the distractor
cost recovered* rather than as a raw error count, because the raw cost depends on stimulus tuning
that the pilot legitimately adjusts. The 50 % default embodies a defensible practical judgement: a
vignette that removes less than half of the distraction cost does not justify wearing a headset to
obtain it. The pilot converts this proportion into an absolute error-rate margin for the power check
before the main study commits, and this conversion is recorded in the post-pilot lock-in memo
(Section 6.8.2). H2b's 10-percentage-point margin reflects the safety framing inherited from the DR
focus-versus-awareness trade-off documented by McLaughlin et al. (2025) and Murph et al. (2021):
in a monitoring context, losing more than one peripheral event in ten to the
overlay is an unacceptable awareness cost. Equivalence testing follows Lakens (2017); the earlier
methodology's bare-null phrasing of this hypothesis is thereby replaced with a test that can fail.

## 6.3 Participants, Ethics, and Apparatus

### 6.3.1 Participants

Twenty-four adults will be recruited (target: 20 analysed, with four spares against dropouts and
pre-registered exclusions). Inclusion criteria follow the approved ethics protocol: aged 18 or over,
normal or corrected-to-normal vision, no self-reported vestibular disorders. Prior VR experience is
recorded on a four-level scale (never / a few times / monthly / weekly or more) and used as a
robustness covariate rather than a selection criterion, since the deployed system must serve VR-naïve
users.

### 6.3.2 Ethics and safety

The study runs under the ethics approval obtained for the earlier protocol version, with each
difference between that protocol and the present design classified (unchanged / minor /
requires-wording-check) in a delta table reviewed with the supervisor before the pilot week. The
substantive task classes — seated video viewing, button presses, digit cancellation, tablets as
distractors, NASA-TLX — are unchanged. Items requiring a wording check before use include the virtual
task panel (if the pilot selects it over the approved paper task), audio recording of the Block C
interview (experimenter notes are the fallback), and the stated session duration on the information
sheet.

Sickness risk is managed by design and by rule. By design: all tasks are seated, there is no
artificial locomotion, total headset time is under 45 minutes, and a headset-off break separates the
two confirmatory blocks. By rule, a written **stop rule** is applied without discussion: the VR
portion terminates immediately if the participant reports nausea or dizziness at a moderate or worse
level at any comfort check or spontaneously, or if the experimenter observes distress. Comfort checks
are scheduled at the headset-off break after Block A and — mandatorily — after Block B, where the
check doubles as the Block C skip gate (Section 6.7); additional checks occur between any conditions
on request. After
termination the participant rests seated, is offered water, and remains until symptoms subside; data
collected up to termination are retained per the consent wording, and the participant is replaced
from the spare pool. The Virtual Reality Sickness Questionnaire (VRSQ; Kim et al., 2018) is
administered **pre and post** session — pre-administration is essential because post-only sickness
scores are uninterpretable without a baseline (Brown, Spronck & Powell, 2022).

### 6.3.3 Apparatus

All sessions use the same Meta Quest 3 headset with controllers (hand tracking disabled), in the same
room, with bright constant lighting at a marked setting — Quest 3 passthrough acuity degrades
markedly in low light (Wang et al., 2026), so lighting is a controlled variable, not a convenience. The participant sits on a fixed chair at a desk. The
experimenter monitors the participant's view via an `scrcpy` mirror with screen recording running as
a redundant data channel. Every stimulus in every block is deterministic and scripted from versioned
schedule files; no live object detection runs anywhere in the study. Instrumentation (the
`StudyLogger` telemetry component, condition sequencer, probe scheduler, and post-session validator)
is specified in the tooling appendix of the operational protocol and summarised in Section 6.10.

## 6.4 Design Overview

### 6.4.1 Structure

| Property | Value |
|---|---|
| Design | Fully within-subjects; no between-subjects factors |
| N | 20 analysed (24 recruited) |
| Session length | ~53 minutes including consent and debrief; Block C is last and skippable |
| Blocks | A — workstation 2×2 (primary); B — driving probes, 2 conditions (secondary); C — compact three-mode sampler (exploratory) |
| Counterbalancing | Block A: 4×4 balanced Latin square over its four conditions (four order groups × five participants). Block B: condition order alternated by participant parity (10 ColorPop-first, 10 Off-first). Block C: three mode orders from a 3×3 Latin square, cycled 7/7/6. |
| Block order | Fixed A→B→C for all participants: A is primary and must come freshest; C is exploratory and absorbs any fatigue. Acknowledged as a limitation (Section 6.10.2). |

Within each block, the stimulus material, seating, lighting, controller, and task are identical
across conditions; only the shader state differs. In vignette-off conditions the focus window is
still painted and locked by the participant, so the motor and procedural experience is identical
across conditions — the vignette is isolated as the sole independent variable.

### 6.4.2 Why these modes, and why these pairings

The three modes are not interchangeable strengths of a single effect; they occupy distinct positions
in the design space of Chapter 3, and each block pairs a mode's *mechanism* with the task paradigm
able to detect it.

**Hard Dark is assigned to Block A** because dark modes *remove* peripheral signal, and the
workstation paradigm measures precisely what removal should buy: a reduced distractor cost. Hard Dark
is preferred over Soft Dark for the confirmatory test because it is the strongest manipulation,
maximising the probability of detecting the mechanism if it exists.

**ColorPop is assigned to Block B** because it does not primarily remove signal; it *re-grades
salience*. The probe-detection paradigm measures what re-grading should buy — faster focal detection
(H2a) — and what it must not cost — peripheral awareness (H2b).

**Soft Dark appears only in Block C**, and its retention deserves explicit defence, since ColorPop
now also dims its periphery and a reader might reasonably ask whether Soft Dark is redundant. It is
retained not as "a weaker Hard Dark" but as the design space's load-bearing middle point, on four
grounds. First, it is the **minimal manipulation**: pure luminance attenuation, the only unconfounded
member of the mode family, whereas ColorPop bundles desaturation, salience re-grading, glare
compression, and dimming into one composite. Second, it is the graded **Tier-2** point in the
compositing hierarchy of Chapter 3: dimmed *real* passthrough at native quality, rather than a
re-rendered camera copy. Third, it has **no camera pipeline**, which makes it the only mode plausibly
deployable for hour-long sessions: the one published feasibility study of on-device processing of the
passthrough camera feed projects thermal throttling within five to ten minutes (Laghari et al., 2025,
arXiv:2509.18929) — a simulation-based estimate, not a measured on-device run.
Fourth, it is the **awareness-preserving** point between full signal and Hard Dark's blackout — the
region of the design space where the focus-versus-awareness trade-off actually lives. A full
confirmatory block for Soft Dark does not fit the session budget; it therefore receives sampler-level
subjective data only, an explicit and stated trade-off.

**The mode × scenario confound is stated plainly.** Hard Dark is tested only at the workstation and
ColorPop only in the driving scene. All confirmatory claims in this dissertation are therefore
*mode-in-context* claims ("Hard Dark reduces distractor cost in a workstation task"), never
mode-generalised claims ("dark vignettes work"). Block C softens the confound slightly by exposing
all three modes on a single common stimulus, but for subjective ratings only.

## 6.5 Block A — Primary: Workstation Focus (Hard Dark, real passthrough)

Block A answers the primary question: does blacking out the periphery around a world-locked focus
window reduce the cost that real peripheral distractors impose on a focal task?

### 6.5.1 Physical layout and distractor stimulus

The participant sits at a desk with the task surface directly ahead: a world-locked virtual panel
(Section 6.5.3) approximately 0.6 m from the eyes. Two tablets on stands are placed at **±35°
azimuth** from the task centre, approximately 1 m from the participant, screens facing them. The 35°
eccentricity places the distractors in the near-periphery zone of maximal involuntary attentional
capture identified by the Useful Field of View literature (Ball & Owsley, 1993). Note that Ball &
Owsley define and validate the UFOV *test*; the ≈30° extent used here is the conventional figure from
that literature rather than a value stated in their paper.

**This is the design target for the formal, in-headset collection; it has not yet been achieved in
practice.** Every naive-pilot session run to date (Section 6's status note) used the standalone
browser prototype on a desktop monitor, which approximates the distractors as `<iframe>`s in the
page's side margins rather than physical tablets — and, as `PilotTools/README.md` documents in
detail, screen-edge iframes on a normal monitor at a normal viewing distance typically achieve only
15–25° of eccentricity, not 35° (reaching 35° would require an offset wider than half a 32" monitor).
This is sufficient for piloting task *feel and timing*, which is all it has been used for, but no
pilot session run this way is informative about the distractor-cost mechanism at the actual 35°
manipulation — that evidence can only come from the physical-tablet apparatus once the formal
collection begins.

[Figure 6.1 — plan-view diagram of the Block A layout: participant, desk, task surface, tablets at
±35°, focus-window extent.]

The tablets play a prepared **distractor reel**: fast-cut, high-motion, bright video with scheduled
salient event bursts — hard cuts to full-screen colour flashes with sudden motion — at fixed
timestamps approximately every 20–30 seconds, identical across participants. In Distractors-absent
conditions the tablets show a static black screen: physically present, so the spatial layout is
constant and only the motion signal varies.

### 6.5.2 Conditions

| Condition | Vignette | Tablets |
|---|---|---|
| A1 | Off (`_DrIntensity = 0`; window painted but invisible) | Static black |
| A2 | Off | Distractor reel |
| A3 | Hard Dark On (window live) | Static black |
| A4 | Hard Dark On | Distractor reel |

Each condition lasts ~3.5 minutes; order follows the balanced Latin square. Before the first
condition the participant paints the focus window around the task surface with the right trigger —
the system's normal interaction — and releases to lock it. The experimenter verifies that both
tablets fall outside the window and the entire task surface falls inside it. The same locked window
persists across all four conditions, re-verified between conditions; only `_DrIntensity` toggles.

### 6.5.3 Task

Two candidate task forms were originally specified for a pilot-time choice (gate G2, Section 6.8.2),
motivated by a genuinely optical rather than attentional risk: Quest 3 passthrough does not reach
normal visual acuity, and a task participants cannot comfortably *see* would floor out in every
condition for reasons unrelated to the vignette. In practice, that choice was resolved ahead of the
formal pilot by extensive informal iteration (Section 6's status note): a virtual, world-locked panel
task (decided 2026-07-22) rather than the large-print digit-cancellation alternative, which was
dropped once the virtual panel's legibility and difficulty were confirmed. Gate G2 in the formal
small-n pilot (Section 6.8.2) now serves to confirm this already-selected task performs in range
rather than to choose between two live options.

**The task — forced-choice 1-back.** A world-locked panel inside the focus window presents one
look-alike shape at a time (drawn from a set of six, ≥ 3° visual angle), and the participant answers
YES/NO on every shape — "same as the previous shape?" — via trigger press. This is a forced-choice
design, not Go/No-go: an iteration in late July 2026 found that a withhold-only non-target response
(the original Go/No-go spec) felt wrong to participants and, more importantly, that Go/No-go's
overwhelmingly-target trial stream let naive pilots ceiling out (96–100 % accuracy) regardless of
distractor condition, which would have floored the block's ability to detect any vignette effect.
The forced-choice 1-back adds a working-memory/goal-maintenance component atop the reactive
component alone, and its harder, memoryless 50 %-repeat sequence keeps performance off ceiling. The
current, frozen-for-study timing (v4, tuned 2026-07-27 against pilot reaction-time data) is 140
trials per condition at a 2.0 s stimulus onset asynchrony and a 1.7 s answer window; the first shape
of each run is a memorise-only trial with no scored response. Outcomes are `hit` (YES on a genuine
repeat), `miss` (a repeat missed, by wrong answer or timeout), `commission` (YES on a non-repeat),
`correct_reject` (NO on a non-repeat), and `no_response` (timeout on a non-repeat, logged separately
as an engagement signal). Every onset and response is logged to the millisecond; error rate is
misses plus commissions, and reaction time is taken from correct trials. No per-trial correctness
feedback is given during scored runs — only pacing feedback ("too slow") — so trial-by-trial
feedback cannot shift participants' speed/accuracy strategy mid-run.

**Implementation status.** The forced-choice 1-back was validated and adopted through a standalone
browser prototype (`PilotTools/block-a-cpt.html`) used for rapid, no-headset piloting; the in-VR
harness's condition sequencer currently still only wires up the earlier Go/No-go panel and the
dropped paper-cancellation option. Porting the browser prototype's trial logic into the VR harness —
replacing, not merely toggling alongside, the superseded Go/No-go wiring — is outstanding work and a
precondition for the formal, in-headset confirmatory collection to begin.

### 6.5.4 Measures

| Measure | Instrument / ground truth |
|---|---|
| Error rate per condition (primary DV input) | StudyLogger event log |
| Distractor cost = errors(A2) − errors(A1) and errors(A4) − errors(A3) | Derived per participant |
| H1 statistic = cost(Off) − cost(On) | Derived per participant; also modelled at trial level |
| Reaction time | StudyLogger, milliseconds |
| Head-turns toward tablets (> 30° yaw from task centre): count and dwell | Head-pose telemetry, 60 Hz |
| Raw NASA-TLX | After each of A1–A4; the distractor-absent administrations provide a workload baseline and a workload analogue of the H1 interaction |
| Noticing check | One open question after Block A: "Did anything make the side screens easier or harder to ignore?" |

### 6.5.5 Participant instructions (verbatim)

> "A panel in front of you will show one shape at a time. **After the first shape, answer whether
> each new shape is the same as the one before it** — one controller trigger for yes, the other for
> no. Respond as fast as you can without guessing. Ignore everything else in the room — only the
> panel matters. Each round lasts about three and a half minutes; there are four rounds. Some rounds
> may look different from others; just do the same thing every round."

Participants are never told which conditions belong to "our system" or what the vignette is expected
to do; the session framing throughout is that they are "comparing display modes" — a defence against
demand characteristics (Section 6.10.2).

## 6.6 Block B — Secondary: Driving Probe Detection (ColorPop, video scene)

Block B answers the secondary pair: does ColorPop speed focal-event detection (H2a) without degrading
peripheral awareness beyond the safety margin (H2b)?

### 6.6.1 Stimulus and conditions

The block uses the system's video test scene: equirectangular urban driving footage rendered on the
display sphere, prepared as two ~4-minute segments of the same road type and similar event density,
with segment-to-condition assignment counterbalanced. The focus window is pre-set to the windscreen
region, fixed in azimuth/elevation for all participants — there is no window painting in this block,
keeping the guided region constant across the sample.

| Condition | Shader state |
|---|---|
| B1 | Vignette Off (raw video) |
| B2 | ColorPop On (motion suppression active, shipping defaults) |

Condition order alternates by participant parity (10 ColorPop-first, 10 Off-first).

### 6.6.2 Probe task

The task is a Detection Response Task in the ISO 17488 lineage (ISO, 2016; Jahn et al., 2005),
retained from the earlier methodology but re-instrumented with machine-timed stimuli. **24 probes per
condition** — brief virtual diamond flashes of ~300 ms, size and contrast pilot-calibrated to a
70–90 % baseline hit rate — are composited over the video at scripted onset times with 6–12 s jitter
(mean ~9 s, so the 24-probe schedule spans ~216 s, leaving ~24 s of slack inside each 4-minute
segment to absorb head-yaw deferrals). Probe positions are scripted at known
eccentricities relative to the focus-window centre:

- 12 **central** probes at < 10° eccentricity (inside the window; this brackets the ~8°-radius "road
  centre" region where driver gaze concentrates, Victor et al., 2005);
- 12 **peripheral** probes at 25–40° eccentricity (outside the window but within the headset's field
  of view for a forward-facing head; the probe schedule pauses while head yaw exceeds 20°
  off-forward and resumes when the head settles, so every probe is actually displayable — and the
  deferral is logged either way).

A **hit** is a trigger press 100–2500 ms after probe onset (the ISO DRT response window); presses
outside any window are **false alarms**. Because every onset and press is machine-logged, reaction
time, hit rate, and false-alarm rate are exact — there is no post-hoc video coding and no ±3-second
matching heuristic anywhere in the design.

### 6.6.3 Measures

| Measure | Instrument |
|---|---|
| Central probe reaction time (median) and hit rate | ProbeScheduler + StudyLogger |
| Peripheral probe hit rate (H2b input) | ProbeScheduler + StudyLogger |
| False alarms per condition | StudyLogger |
| Head yaw/pitch telemetry and angular speed | StudyLogger, 60 Hz |
| Raw NASA-TLX | After each of B1 and B2 |

Head pose serves as the attention proxy in place of eye tracking, which the Quest 3 does not provide;
head orientation is a validated gaze surrogate at the eccentricities used here (Higgins et al., 2022;
Sitzmann et al., 2018).

### 6.6.4 Participant instructions (verbatim)

> "You'll watch a driving scene. Small **diamond shapes will flash briefly** anywhere in the scene —
> sometimes in front of you, sometimes off to the side. **Pull the trigger as soon as you see a
> diamond. That's the only thing you need to do.** Don't press for anything else. Keep watching the
> road ahead as if you were the driver, and stay seated. Each clip is about four minutes; there are
> two."

## 6.7 Block C — Exploratory: Compact Mode Sampler (skippable)

Block C answers RQ3: how do the three modes compare subjectively on a common stimulus? It follows the
ratings-plus-interview pattern of Cheng et al. (2022) and carries no confirmatory claims. It is the
only block in which Soft Dark is experienced (rationale in Section 6.4.2).

The participant watches the driving video under each of ColorPop, Soft Dark, and Hard Dark for ~75 s
each, order drawn from a 3×3 Latin square; there is no task — free viewing with the focus window
pre-set. After each mode, three 7-point Likert items are administered verbally and recorded by the
experimenter:

1. "This effect was comfortable to look at." (comfort)
2. "This effect would help me concentrate on what's in the window." (perceived focus benefit)
3. "I would use this effect for real work or study." (willingness to use)

After all three modes, a three-question semi-structured interview (~3 minutes, headset off;
audio-recorded only if the consent wording permits, otherwise experimenter notes):

1. Which mode would you choose, and for what activity?
2. What did the edges of your vision feel like — and did you notice the effect fading when you turned
   your head? How did that feel?
3. What would stop you using this for an hour of real work?

**Skip rule (pre-registered).** Block C is dropped without discussion if the participant shows or
reports fatigue or symptoms at the post-Block-B comfort check, or if the session is running more than
five minutes over schedule. Only exploratory data is lost; the confirmatory blocks are never
compressed to preserve the sampler. The sampler completion rate is reported.

**Caveats, stated honestly.** Three brief, task-free, order-affected exposures answer "how did each
mode feel?", not "which mode is better?". Ratings are reported as exploratory only.

Participant instruction (verbatim):

> "No task now — just watch the scene. I'll switch between three different visual effects, about a
> minute each. After each one I'll ask you three quick rating questions. Look around as much as you
> like."

## 6.8 Session Procedure and Pilot Study

### 6.8.1 Session timeline

The full session runs ~53 minutes, inside the 40–60-minute tolerance window participants typically
accept; the information sheet is sent in advance, raw TLX (~1 minute each) follows every A and B
condition, and Block C's skip rule means a fatigued session closes at ~44 minutes without touching
confirmatory data.

| Clock | Duration | Activity |
|---|---|---|
| 00:00 | 4 min | Welcome; written consent (information sheet pre-read); demographics + VR-experience form; VRSQ (pre) |
| 04:00 | 4.5 min | Headset fitting and comfort check; tutorial: paint/lock a practice window; practice to criterion (Option V: ≥ 90 % go-accuracy on a 1-minute practice stream; Option P: one practice row; probe practice: 6 probes, ≥ 4 hits). One repeat allowed. |
| 08:30 | 1 min | **Block A** — paint and lock focus window; experimenter verifies geometry |
| 09:30 | 3.5 min | Condition i (A1–A4 per Latin square) |
| 13:00 | 1 min | TLX + reset (tablets re-cued / sheet swapped) |
| 14:00 | 3.5 min | Condition ii |
| 17:30 | 1 min | TLX + reset |
| 18:30 | 3.5 min | Condition iii |
| 22:00 | 1 min | TLX + reset |
| 23:00 | 3.5 min | Condition iv |
| 26:30 | 1.5 min | TLX + noticing question |
| 28:00 | 1.5 min | Headset-off break; verbal comfort check (stop rule applies throughout); water offered |
| 29:30 | 4 min | **Block B** — condition i (B1 or B2) |
| 33:30 | 1 min | TLX |
| 34:30 | 4 min | Condition ii |
| 38:30 | 1 min | TLX + fatigue/comfort check (Block C skip rule applied here) |
| 39:30 | 6 min | **Block C** — 3 × (75 s mode + 3 Likert items verbally) |
| 45:30 | 3 min | Three-question interview (headset off) |
| 48:30 | 4.5 min | VRSQ (post); SUS; study-aims debrief; thanks |
| 53:00 | — | Experimenter runs the session validator **before the participant leaves**; incident log entry if anything deviated |

Arithmetic: opening 8.5; Block A 19.5 (1 + 4×3.5 + 3×1 + 1.5); break 1.5; Block B 10 (2×4 + 2×1);
Block C 9; close 4.5 — total 53 minutes.

The practice-to-criterion tutorial serves a specific validity function beyond familiarisation: the
practice includes the vignette itself, so its first appearance is never during a measured trial,
neutralising first-exposure novelty effects at the point where they would otherwise contaminate data.

### 6.8.2 Pilot study with pass/fail gates

A pilot with n = 3–5 participants (excluded from the main sample) runs in the first week. Its purpose
is structural: to make an inconclusive main study impossible. The main study does not launch until
all four gates pass; pilot data never enters the main analysis; stimulus parameters may be tuned
between pilot participants, but hypotheses, margins, and analysis choices may not.

| Gate | Test | Pass criterion | On fail |
|---|---|---|---|
| **G1 — distractors distract** | Run A1 vs A2 (vignette Off) on pilot participants | Visible error-rate increase under distractors for all but at most one pilot participant (direction, not significance) | Increase distractor salience (brighter/faster bursts, closer or larger tablets; audio would require an ethics check), then re-test |
| **G2 — task form** | The already-selected forced-choice 1-back tried through passthrough (task-form choice itself was resolved ahead of the formal pilot by informal iteration, Section 6's status note) | Panel fully legible; accuracy off both ceiling and floor across pilot participants | Retune timing/difficulty (SOA, answer window, lure ratio) against pilot data, then re-test; redesign the task only if retuning cannot bring it off ceiling or floor |
| **G3 — probe calibration** | Block B with candidate probe sizes/contrasts | Baseline hit rate 70–90 % in both eccentricity bands; unimodal RT distribution | Adjust probe size/duration/contrast per band; re-test |
| **G4 — protocol dry run ×2** | Two full end-to-end sessions with the final configuration | Timeline within ±5 minutes; zero log gaps (validator clean); battery ≥ 20 % at end; no sickness terminations; questionnaire flow smooth | Fix the specific failure; re-run one dry run |

The pilot concludes with a one-page **lock-in memo** to the supervisor recording the chosen task
form, the tuned stimulus parameters, the pilot-observed distractor cost, and the finalised absolute
SESOI for H1 derived from it. After this memo, nothing changes until data collection ends.

## 6.9 Analysis Plan and Decision Table

All analysis will be performed in Python (pandas, pingouin, statsmodels) or R, with scripts written
and dry-run on pilot data before main collection begins. α = .05 throughout; effect sizes and
confidence intervals are reported for every test — 95 % two-sided by default, 90 % where one-sided or
equivalence logic applies (the H1 SESOI bound and the H2b TOST).

### 6.9.1 Confirmatory analyses

| Hypothesis | Primary test | Robustness check | Effect size |
|---|---|---|---|
| **H1** | Linear mixed model on trial-level errors (Option V; logistic LMM: error ~ vignette × distractor + (1 + distractor \| participant)); the interaction term is the test. For Option P: paired t-test on the per-participant cost difference, cost(Off) − cost(On) | Paired t-test on per-participant condition means (V); Wilcoxon signed-rank if Shapiro–Wilk rejects normality | dz on the cost difference; odds ratio from the LMM |
| **H2a** | Paired t-test (Wilcoxon fallback) on per-participant median central RT, B1 vs B2; hit rate checked descriptively for non-inferiority | LMM on trial-level log-RT (correct trials) | dz |
| **H2b** | TOST on peripheral hit rate (paired), margin Δ = 10 pp (Lakens, 2017) | Bayesian paired test as an optional sensitivity check | Mean difference with 90 % CI against the margin |
| **H3** | Paired t-test/Wilcoxon on TLX global score, A4 vs A2 | Per-subscale exploratory, Holm-corrected | dz / r |
| **MC1** | Paired test, A2 vs A1 errors (vignette-off cost > 0) | — | dz |

Mixed-effects modelling at trial level follows the analysis approach of McLaughlin et al. (2025); the
aligned-rank-transform alternative for nonparametric factorial data (Wobbrock et al., 2011) is noted
but not preferred, in light of recent critiques of ART's error control (Tsandilas & Casiez, 2024).

**Multiplicity policy.** H1 is the single primary endpoint and receives no correction. H2a, H2b, and
H3 form the secondary family, Holm-corrected across the three. Everything else — Block C ratings,
head-turn telemetry, TLX subscales — is labelled exploratory and is never quoted as a confirmatory
finding.

**Power.** For the primary contrast, computed per participant as a paired difference of differences,
a paired t-test at α = .05 (two-tailed) with N = 20 achieves 80 % power for dz ≈ 0.65. The distractor
main effect underlying this interaction is large in the source literature (commission errors more
than doubling at N = 66), so a vignette recovering half of it is plausibly detectable at this N; the
pilot verifies the locally achievable cost size before the main study commits, and if the local cost
proves small, the SESOI conversation with the supervisor happens *before* data collection rather than
after. Trial-level mixed models over ~11,000 Block A trials provide additional sensitivity beyond the
aggregate-level power figure.

### 6.9.2 Exploratory analyses

A 2×2 Vignette × Distractor interaction on the global NASA-TLX score — the workload analogue of H1,
and the reason TLX follows all four Block A conditions rather than only the pair H3 compares — is
computed and reported alongside H3, labelled exploratory. Block C Likert items are analysed per item
with Friedman tests across the three modes and Bonferroni-corrected pairwise Wilcoxon comparisons
(the Cheng et al. 2022 pattern), with Kendall's W reported and the sampler completion rate reported
alongside. Head-turn counts and dwell toward the
tablets (Block A) and head angular speed (Block B) are analysed descriptively with paired tests,
flagged exploratory. Interview responses are thematically coded into a compact table (mode →
recurring likes and concerns), with quotes used to illustrate the quantitative findings. H1 is
re-estimated within VR-experienced and VR-naïve subgroups as a robustness description, not a test.

### 6.9.3 Decision table

The following table is pre-committed; the dissertation will report whichever row occurred.

| # | Outcome pattern | Dissertation conclusion |
|---|---|---|
| 1 | H1 supported (interaction in predicted direction, ≥ SESOI) **and** H2b passes equivalence | **The system works**: peripheral DR dimming reduces distraction cost, and the salience mode does not measurably harm peripheral awareness. |
| 2 | H1 supported, H2b **fails** (drop > 10 pp) | The system works **for focus, at a quantified situational-awareness cost** — a conclusive, qualified result with a design implication (mode choice by context). |
| 3 | H1 **refuted** (90 % CI on the recovered-cost proportion excludes the SESOI from below) | **The system does not deliver a meaningful benefit** in its core use case — a conclusive negative. The dissertation reports the bounded estimate ("the vignette recovers at most X % of the distraction cost"). |
| 4 | H1 estimate positive but CI spans both SESOI and smaller values | The benefit is real-signed but **cannot be claimed at the pre-set size**; report the estimate with its CI as a bounded conclusion. (Made unlikely by the pilot gate and trial counts; if it occurs, it is reported as such — not spun.) |
| 5 | MC1 fails in the main study (no distractor cost despite pilot pass) | H1 is not interpretable; report the manipulation failure honestly and fall back on H2a/H2b and Block C as the evidential core. |
| 6 | Pilot gates fail after tuning | **The main study does not launch**; the paradigm is redesigned with the supervisor. Inconclusive main-study data is prevented, not explained. |

Interpretation of whichever row occurs — including the transfer limits of a positive result and the
design implications of a negative one — is developed in Chapter 7. The oracle-vs-live perception gap
that bounds the generality of Block B's simulated context is quantified in Chapter 5.

### 6.9.4 Pre-registered exclusion and data-quality rules

| Rule | Action |
|---|---|
| VR-sickness termination | Session ends under the stop rule; partial data retained but participant excluded from confirmatory analysis; replaced from spares |
| App crash / restart during a condition | That condition void; re-run once at the end of its block if time allows; otherwise the participant contributes remaining conditions to the LMM (which tolerates missingness) and is excluded from the paired robustness test |
| App crash / restart that spans a **new session launch** (the auto-session ledger assigns a fresh participant ID on relaunch rather than resuming the old one) | The pre-crash and post-relaunch IDs are reconciled into one canonical participant record before analysis: the block(s) completed cleanly pre-crash are retained under whichever ID the experimenter designates canonical in the incident log, completed blocks from the other ID are joined in, and any block attempted on both IDs keeps only the later, complete attempt. Reconciliation is logged in the incident log with both IDs, the ledger timestamps used to justify the join, and is fixed before any analysis is run — not revisited afterwards |
| Task accuracy < 60 % in A1 (distractor-absent, vignette-off) | Task not performed; exclude and replace |
| Block B: false-alarm rate > 30 % of presses in either condition | Response strategy invalid (indiscriminate pressing); exclude Block B for that participant |
| Log integrity failure (gaps > 1 s or missing condition markers) | Affected condition void; incident log entry; same re-run rule as crashes |
| Participant recognises the driving footage | Noted; excluded only on reported strong familiarity affecting behaviour |
| Missing TLX (≥ 2 subscales blank) | That TLX excluded from H3; behavioural data retained |

## 6.10 Operational Protocol and Threats to Validity

### 6.10.1 Operational protocol

The study's operational engineering assumes that anything that can go wrong will. Four principles
implement that assumption.

**Determinism.** Every distractor burst, probe onset, and task trial derives from versioned schedule
files checked into the repository; a session is reproducible from its participant ID and schedule
version. Condition orders derive from the participant ID via the Latin-square table in the condition
sequencer — no experimenter arithmetic occurs mid-session. No live machine learning runs anywhere.
The sequencer additionally **asserts the required scene configuration at session start and refuses
to run on mismatch** — for example, that motion suppression is enabled for Block B (the component's
code default is off, so an unguarded stock scene would silently drop it) and that baked-detection
playback is disabled — and it logs the full configuration snapshot as the session's first event rows.

**Logging with redundancy.** The telemetry component writes one CSV row per frame (~60 Hz) with
heartbeat semantics — an unbroken timestamp chain is itself evidence of integrity — and flushes every
frame so that a crash loses under one second of data. Every event (condition boundaries, stimulus
onsets, presses, window lock, questionnaire start) is a labelled row. The `scrcpy` screen recording
provides a redundant channel against which any disputed trial can be visually re-checked.

**Validate before the participant leaves.** A post-session validator checks timestamp continuity,
expected condition counts and durations, and per-condition event counts (for example, exactly 24
probe onsets per Block B condition), returning PASS/FAIL while the participant is still available —
so a voided condition can be re-run under the pre-registered rule rather than discovered a week
later. Data files are pulled and copied to two locations before the next session.

**Incident log.** A running incident file records every deviation — date, participant code, what
happened, action taken, and which pre-registered rule applied. It is quoted verbatim in the write-up:
deviations are reported, not hidden. A printed pre-session checklist (battery thresholds, lighting
marks, tablet positions verified against a printed 35° angle template, build hash, storage, spare
materials, forms) is ticked for every session.

### 6.10.2 Threats to validity

| Threat | Mitigation |
|---|---|
| Passthrough acuity confound (Quest 3 VST below normal acuity, worse in low light) | No fine-text reading through passthrough anywhere; Option V renders the task; Option P admitted only on pilot-proven legibility (G2); bright constant lighting on the checklist |
| Novelty effect | Practice-to-criterion tutorial including the vignette before any measured trial; Latin square distributes residual novelty |
| Order and learning effects | Balanced Latin square (A), parity counterbalance (B); matched sheets/segments; equal trial counts. Fixed A→B block order is a limitation: B is always second — acceptable because B's comparison is internal to the block and counterbalanced within it |
| Coverage limits from the session budget | Soft Dark receives sampler-level subjective data only (Section 6.4.2); sampler ratings come from brief task-free exposures and may be lost to the skip rule (completion rate reported); the mode × scenario confound is inherent and stated |
| Demand characteristics | Objective primary DVs (errors, millisecond RT, telemetry); neutral "comparing display modes" framing; the noticing question follows Block A data collection |
| Distractor floor (distractors fail to distract) | Pilot gate G1, main-study manipulation check MC1, decision-table row 5 |
| Simulator sickness and dropout | Seated, no locomotion, < 45 min headset time, mid-session break, VRSQ pre/post, stop rule, N + 4 recruitment |
| Small-N insensitivity | Within-subjects design, high trial counts, mixed models; single primary endpoint; SESOI and equivalence margins make null results informative; effect sizes and CIs always reported |
| Experimenter variability | One experimenter; verbatim scripts; printed per-session checklist |
| Ecological validity (driving is a video; workstation distractors are tablets) | The claim tested concerns attention mechanisms under controlled distraction, with driving footage as stimulus context — explicitly not a road-safety claim (Chapter 1 scoping rules); developed further in Chapter 7 |
| Stereo comfort of camera-fed effects | The dark modes are overlay-only (no camera repaint); ColorPop runs in the video scene (no live camera fusion); head motion is moderate by task design |

---

## References (this chapter)

- Bailey, R., McNamara, A., Sudarsanam, N., & Grimm, C. (2009). Subtle gaze direction. *ACM
  Transactions on Graphics, 28*(4). https://doi.org/10.1145/1559755.1559757
- Ball, K., & Owsley, C. (1993). The useful field of view test: a new technique for evaluating
  age-related declines in visual function. *Journal of the American Optometric Association, 64*(1),
  71–79. PMID: 8454831.
- Bourdon, B. (1895). Observations comparatives sur la reconnaissance, la discrimination et
  l'association. *Revue Philosophique, 40*, 153–185.
- Byers, J. C., Bittner, A. C., & Hill, S. G. (1989). Traditional and raw task load index (TLX)
  correlations: are paired comparisons necessary? In *Advances in Industrial Ergonomics and Safety I*
  (pp. 481–485). Taylor & Francis.
- Caine, K. (2016). Local standards for sample size at CHI. In *Proceedings of CHI 2016* (pp.
  981–992). ACM. https://dl.acm.org/doi/10.1145/2858036.2858498
- Cheng, Y., Yin, H., Yan, Y., Gugenheimer, J., & Lindlbauer, D. (2022). Towards understanding
  diminished reality. In *Proceedings of CHI 2022*. ACM.
  https://dl.acm.org/doi/10.1145/3491102.3517452
- Ai, X., Wang, Y., Wang, P., & Wang, S. (2025). Impact of visual distractors in virtual reality
  environments on sustained attention behavioral performance and EEG characteristics. *Frontiers in
  Human Neuroscience.* https://pmc.ncbi.nlm.nih.gov/articles/PMC12698649/
- Brown, P., Spronck, P., & Powell, W. (2022). The simulator sickness questionnaire, and the
  erroneous zero baseline assumption. *Frontiers in Virtual Reality, 3*:945800.
  https://doi.org/10.3389/frvir.2022.945800
- Hart, S. G. (2006). NASA-Task Load Index (NASA-TLX); 20 years later. In *Proceedings of the Human
  Factors and Ergonomics Society Annual Meeting, 50*(9), 904–908.
  https://human-factors.arc.nasa.gov/groups/TLX/downloads/HFES_2006_Paper.pdf
- Hart, S. G., & Staveland, L. E. (1988). Development of NASA-TLX (Task Load Index): results of
  empirical and theoretical research. In *Human Mental Workload* (pp. 139–183). North-Holland.
- Hatta, T., Yoshizaki, K., Ito, Y., Mase, M., & Kabasawa, H. (2012). Reliability and validity of the
  digit cancellation test, a brief screen of attention. *Psychologia, 55*(4), 246–256.
  doi:10.2117/psysoc.2012.246
- Higgins, P., Barron, R., & Matuszek, C. (2022). Head pose as a proxy for gaze in virtual reality.
  *VAM-HRI 2022*. https://iral.cs.umbc.edu/Pubs/Higgins2022VAM-HRI.pdf
- ISO (2016). *ISO 17488:2016 — Road vehicles — Transport information and control systems —
  Detection-response task (DRT) for assessing attentional effects of cognitive load in driving.*
- Jahn, G., Oehme, A., Krems, J. F., & Gelau, C. (2005). Peripheral detection as a workload measure in
  driving: effects of traffic complexity and route guidance system use in a driving study.
  *Transportation Research Part F, 8*(3), 255–275. https://doi.org/10.1016/j.trf.2005.04.009
- Kim, H. K., Park, J., Choi, Y., & Choe, M. (2018). Virtual reality sickness questionnaire (VRSQ):
  motion sickness measurement index in a virtual reality environment. *Applied Ergonomics, 69*,
  66–73. https://www.sciencedirect.com/science/article/abs/pii/S000368701730282X
- Lakens, D. (2017). Equivalence tests: a practical primer for t tests, correlations, and
  meta-analyses. *Social Psychological and Personality Science, 8*(4), 355–362.
  https://doi.org/10.1177/1948550617697177
- McLaughlin, A. C., et al. (2025). Cognitive aid design using diminished reality to support selective
  attention by reducing distraction. *Human Factors, 67*(9), 937–961.
  https://journals.sagepub.com/doi/10.1177/00187208251325169
- Murph, I., McDonald, M., Richardson, K., Wilkinson, M., Robertson, S., Karunakaran, A., Gandy
  Coleman, M., Byrne, V., & McLaughlin, A. C. (2021). Diminishing reality: potential benefits and
  risks. *Proceedings of the Human Factors and Ergonomics Society Annual Meeting, 65*(1), 164–168.
  doi:10.1177/1071181321651103
- Wang, J., Ping, S., Xu, K., Li, Y., & Liang, H.-N. (2026). The perceptual gap between video
  see-through displays and natural human vision. *arXiv:2601.02805.* https://arxiv.org/pdf/2601.02805
- Laghari, M. K., Shaikh, A. A., Khan, F., & Siddiqui, A. G. (2025). Native mixed reality compositing
  on Meta Quest 3: a quantitative feasibility study of ARM-based SoCs and thermal headroom. *arXiv
  2509.18929.* https://arxiv.org/abs/2509.18929
- Sitzmann, V., Serrano, A., Pavel, A., Agrawala, M., Gutierrez, D., Masia, B., & Wetzstein, G.
  (2018). Saliency in VR: how do people explore virtual environments? *IEEE TVCG, 24*(4), 1633–1642.
  (arXiv:1612.04335)
- Sutton, J., Langlotz, T., Plopski, A., Zollmann, S., Itoh, Y., & Regenbrecht, H. (2022). Look over
  there! Investigating saliency modulation for visual guidance with augmented reality glasses. In
  *Proceedings of UIST 2022*. ACM. https://dl.acm.org/doi/10.1145/3526113.3545633
- Victor, T. W., Harbluk, J. L., & Engström, J. A. (2005). Sensitivity of eye-movement measures to
  in-vehicle task difficulty. *Transportation Research Part F, 8*(2), 167–190.
  https://doi.org/10.1016/j.trf.2005.04.014
- Tsandilas, T., & Casiez, G. (2024). The illusory promise of the Aligned Rank Transform. *Journal of
  Visualization and Interaction* (under review). https://statransform.github.io/jovi/
- Wobbrock, J. O., Findlater, L., Gergle, D., & Higgins, J. J. (2011). The aligned rank transform for
  nonparametric factorial analyses using only ANOVA procedures. In *Proceedings of CHI 2011* (pp.
  143–146). ACM. doi:10.1145/1978942.1978963
