# Chapter 7 — Discussion

> **Status note.** This chapter is drafted before the user study (Chapter 6) and the benchmark
> programme (Chapter 5) have produced data. It therefore does two different things, kept explicitly
> separate: Section 7.1 discusses what the dissertation can already claim on the basis of completed
> work (the design space, the implementation, the prior-art search); Sections 7.2–7.3 fix the
> interpretive frame within which the empirical results, once collected, will be read. Committing to
> that frame *before* seeing data is deliberate — it is the discussion-chapter counterpart of the
> pre-registered decision table, and it prevents the most common failure of small-N dissertations:
> results interpreted by a frame constructed after the fact to flatter them. Data-dependent passages
> are written in the conditional, and no outcome is predicted.

## 7.1 What can already be discussed

### 7.1.1 The design space is itself a result

The central finding of Part T does not await any benchmark: it is the shape of the design space in
Chapter 3. On a consumer VST headset, diminished reality is not one problem but three, indexed by
compositing access. At Tier 1, the platform grants global colour remapping of the system passthrough
layer (the Styling API's brightness, contrast, saturation, and LUT controls) and nothing spatial: no
per-pixel masks, no world-locked regions, no read access, and — since the deprecation of
surface-projected passthrough at SDK v83 — no per-surface control either. The significant fact is
the *absence*: a developer who wants to dim the periphery and spare a window cannot do it at Tier 1
at all. That absence is a platform-policy finding, not an engineering one, and it shapes what DR
research is possible on consumer hardware more than any algorithm does.

The system's response to that absence — the world-locked focus window realised as a shaped *absence*
of overlay, native passthrough showing through it, with all manipulation confined to the periphery —
is an existence proof: windowed subtractive DR is achievable at Tier 2 without OS cooperation, and
hybrid Tier-2/Tier-3 composites (native passthrough inside the window, an effect-processed camera
feed outside it) are achievable with Passthrough Camera Access. The structured prior-art search of
2026-07-05 (framing document, Section 6.1) found no publication, product, or open-source project
that composites system passthrough with a live shader-processed camera feed in one view, for any
purpose, while every constituent mechanic has documented precedent. The composite, precisely scoped,
is the novelty claim — and the documented search protocol is what entitles the dissertation to make
it. Should the refresh search before submission surface a concurrent system, the claim degrades
gracefully to independent invention with a characterised design space, which is why the claim is
worded around the space rather than the trick.

### 7.1.2 Engineering lessons that generalise

Four lessons from the implementation are candidates for reuse beyond this project, and they are
findings in the sense that each was purchased with a documented failure.

**Sample by world direction, not screen space, when re-rendering a mono camera feed.** The naive
screen-space UV mapping of the camera texture produced double vision, because each eye sampled a
different pixel for the same world point. Sampling by world direction through a head-centred pinhole
is eye-independent, so the binocular system fuses the re-rendered image. Any future system that
re-renders a mono VST feed — on this platform or another — will face the same problem and can use
the same solution.

**Exploit the quality asymmetry: route the task-critical region through the native layer.** The
naive architecture re-renders everything and accepts camera-feed quality everywhere. Inverting it —
manipulating only the periphery and leaving the window as an absence — means the user's
task-relevant vision never pays the Tier-3 quality tax. Chapter 5's T3 benchmark will quantify the
tax; the architectural lesson stands independently: on any platform where an accessible feed is
worse than the native view, put the manipulation where the eyes are not.

**Decouple effects from head motion.** Head-coupled peripheral restriction increases discomfort
(Norouzi et al., 2018); the system therefore fades the effect out during fast head rotation and
restores it on settling, with an immediate restore when the head enters the focus region. This
inverts a common default — effects that follow the head — into one that yields to the head.

**Consumer-platform DR research inherits platform-policy risk.** The failure museum's least
technical entry may matter most: surface-projected passthrough, a capability this project might have
built on, was deprecated mid-project at SDK v83. Tier boundaries on consumer hardware are set by
vendor policy and move without notice. Research designs that, like this one, keep a working path at
every tier are robust to that movement; designs premised on a single capability are not.

### 7.1.3 Positioning: software-defined visual noise cancellation

The literature review's closest conceptual frame for the whole project is visual noise cancellation
(VNC): moderating the visual environment through head-worn hardware the way acoustic noise
cancellation moderates sound. Read through that frame, the present system is a *software-defined*
VNC device: the "cancellation profile" is a shader parameter set, the modes are profiles
(attenuate: Soft/Hard Dark; re-grade: ColorPop), and the tier taxonomy describes which profiles the
platform physically permits. The analogy also imports a useful discipline from its acoustic
original: noise-cancelling headphones are evaluated on measured attenuation *and* on what they cost
the wearer in awareness — which is exactly the H1/H2b pairing. The analogy is offered as
positioning, not as a claim of equivalence: acoustic cancellation is subtractive at the signal
level, whereas everything in Chapter 3 exists precisely because consumer VST compositing forbids
signal-level subtraction and forces synthesis by overlay.

### 7.1.4 What cannot yet be discussed

No statement about whether the system *works* — in the sense of H1, H2a, or H2b — is available, and
none is made here. The literature gives reason to take both outcomes seriously: peripheral
distractors reliably impose sustained-attention costs in headsets (Frontiers in Human Neuroscience,
2025), and DR-style attenuation reduced workload in VR simulation (McLaughlin et al., 2025); but
area darkening's guidance effects come mostly from immersive video contexts, desaturation alone has
shown weak effects, and awareness costs are documented wherever attenuation succeeds (Murphy et al.,
2021 [VERIFY]). The design space contains both a working system and a placebo; only Chapter 6's
data can say which this is.

## 7.2 The pre-registered interpretation frame

The decision table (Chapter 6, Section 8.3) is reproduced here in condensed form, with the
scientific meaning each row would carry. The dissertation will report the row that occurred; this
section exists so that the meaning of each row is on record before the data are.

**Row 1 — H1 supported and H2b passes equivalence.** The mechanism claim would be validated on real
hardware: peripheral diminishment reduces the performance cost of real peripheral distractors, and
the salience-re-grading mode does not measurably degrade peripheral event detection. The claim
remains mode-in-context (Hard Dark, workstation task; ColorPop, monitoring stimulus) — the design
cannot support "dark vignettes work" in general, and the dissertation will not say it. Within that
scope, this row would be the first performance-based demonstration of DR distraction suppression on
consumer passthrough hardware rather than in VR simulation.

**Row 2 — H1 supported, H2b fails.** The focus–awareness trade-off documented in simulated DR
(McLaughlin et al., 2025; Murphy et al., 2021 [VERIFY]) would have been reproduced and *quantified*
on real hardware: the periphery's protection is bought with a measured awareness cost exceeding ten
percentage points of peripheral hit rate. This is not a failure row. It is arguably the most
design-relevant outcome, because it converts "mode choice" from aesthetics into a safety-relevant
context decision: attenuation modes for closed environments where the periphery genuinely does not
matter; salience modes, or nothing, where it does.

**Row 3 — H1 refuted (the CI excludes the SESOI from below).** The bounded negative: peripheral
*visual* attenuation, even at Hard Dark's maximum, recovers less than half of the distraction cost.
Scientifically this would suggest that distractor interference in this paradigm is not carried
solely by continuing visual input — knowing the tablets are there, and the residual attentional
set toward them, may impose costs a visual overlay cannot remove. That would be a genuinely useful
result for the DR field, which currently extrapolates from simulation studies in which "removing"
a distractor is total. The dissertation would report the recovered-cost upper bound as its headline
number.

**Row 4 — Signed but unclaimable benefit.** The estimate favours the vignette but the confidence
interval spans both the SESOI and trivially small effects. The pilot gate and trial counts make
this row unlikely by design; if it occurs, the dissertation reports the estimate and interval as a
bounded conclusion and explicitly declines to claim the pre-set effect size. The temptation this
row exists to forbid is the retrospective discovery that a smaller effect "would also be
meaningful".

**Row 5 — Manipulation-check failure.** If the distractor cost fails to appear with the vignette
off despite the pilot gate, H1 is uninterpretable, and the honest report is a methodological one:
scheduled video distractors at ±35° did not capture attention under these task demands — itself a
datum for anyone designing distraction paradigms in MR, where the literature's distractor effects
come mostly from social and event-based interruptions rather than looping media.

**Row 6 — Pilot gates fail.** The main study does not launch, and the dissertation's empirical
chapter reports the pilot outcomes and redesign rationale. This row is included because a
pre-registered frame that omits its own abort path is incomplete.

## 7.3 Block A versus Block B: a built-in test of simulation-based evaluation

The two confirmatory blocks run the same guidance principle at opposite ends of the feasibility
spectrum: Block A on the real Tier-2 artifact over live passthrough with physical distractors;
Block B on the simulated context — video sphere, scripted probes — that stands in for the
future system. This pairing was designed as a bridge (framing document, Section 4), and it carries
an incidental methodological payload. If the two blocks agree in direction — diminishment helps
focal performance in both — the simulation methodology that the nearest published work relies on
entirely (Cheng et al., 2022; McLaughlin et al., 2025, both VR-simulated) gains a point of external
credibility from this project's real-hardware anchor. If they diverge, the divergence localises
what simulation misses: candidate explanations would include the absence of depth motion in
monoscopic video, the different distractor ontology (baked footage events versus physically present
screens), and hardware-borne effects such as fusion strain that no video condition reproduces.
Either way the comparison is reportable, which is the property that was designed for; the blocks
were not powered as a formal cross-context contrast, and any A-versus-B statement will be framed as
observational.

Operationally, "agreement" will be read at the level of sign and decision-table row, not effect
magnitude: Block A's H1 and Block B's H2a estimate the benefit of diminishment-family manipulations
on focal performance in their respective contexts, and the comparison asks whether both point the
same way. Magnitudes are not comparable across the blocks — different tasks, different dependent
variables, different modes — and no ratio between them will be interpreted.

## 7.4 Transfer: what leaves this dissertation, and under what conditions

**To optical see-through (OST) hardware, the dark overlay does not transfer.** The Tier-2 modes
work because a VST display is an opaque screen under full render control; additive OST optics
cannot darken the world. What transfers is the taxonomy and the question. On OST hardware the
taxonomy collapses toward display-side attenuation — of which segmented dimming hardware (e.g.
Magic Leap 2's dimmer panel [VERIFY capability details]) is the nearest existing analogue of a
Tier-2 windowed dim — and toward the additive salience modulation demonstrated by Sutton et al.
(2022). Whatever Chapter 6 finds about *what peripheral diminishment does to attention* transfers
as a human-factors result to any hardware that can implement an equivalent stimulus; the
implementation findings transfer only within VST.

**From the simulation, transfer is conditional, and the conditions are enumerated.** Headset weight
biases only comfort ratings, and conservatively; attention effects are unaffected in either
direction by lightening hardware. Monoscopic video removes motion-in-depth, so peripheral capture
by looming stimuli is untested. The passenger viewpoint removes task coupling, which is why no
statement in this dissertation concerns driving performance, and none should be quoted as if it
did. Video dynamic range compresses glare, so ColorPop's glare-suppression track is exercised only
weakly. Identical stimuli for all participants trade ecological breadth for internal validity, and
the trade is intended. Block A, running on the real system against physical distractors, is the one
component that transfers without these caveats — which is why it carries the primary hypothesis.

**Oracle conditioning.** Every statement about detector-dependent guidance variants is conditioned
on the oracle assumption, whose measured size is Chapter 5's T4 deliverable. Until the live column
of the oracle-gap table is filled, "the detector sees everything in time" is an assumption with a
plan for its own measurement — not a fact. The study protocol itself is insulated from this
caveat by design: its stimuli are scripted and its shipping modes detector-free.

## 7.5 Limitations, consolidated

The limitations below are collected from Chapters 3–6 so that they appear once, together, rather
than diluted across the text.

1. **Mode × scenario confound.** Hard Dark is tested only in the workstation context, ColorPop only
   in the monitoring context. Every empirical claim is mode-in-context; the design cannot separate
   mode effects from scenario effects, and Block C's common-stimulus ratings soften this only at
   the subjective level.
2. **Soft Dark carries sampler-level data only**, and the sampler is skippable under the fatigue
   rule, so its completion rate bounds even that. The mode's retention is justified on design-space
   grounds (testing-strategy, Section 3.5), not empirical ones.
3. **No eye tracking.** Quest 3 has none. Head pose is a validated proxy at the eccentricities used
   (Higgins et al., 2022; Sitzmann et al., 2018), but covert attention shifts — distraction without
   head movement — are invisible to telemetry. The dependent variables were chosen so that
   *performance outcomes*, not attention allocation, carry the hypotheses; the mechanism remains
   partially inferred.
4. **Sensitivity floor.** N = 20 gives 80 % power at dz ≈ 0.65 for the classical paired test;
   trial-level mixed models improve on this, but effects meaningfully smaller than the SESOI are
   detectable only as estimates with wide intervals (decision-table row 4 exists for this reason).
5. **Single experimenter, single site, single hardware generation.** Scripts and checklists bound
   experimenter variance but cannot eliminate it; every number is specific to Quest 3 and the
   frozen OS/SDK versions, on a platform whose capabilities have already moved once mid-project.
6. **Ecological validity boundaries.** The workstation distractors are two tablets with scheduled
   bursts; the monitoring stimulus is a video. The study measures attention mechanisms under
   controlled distraction — deliberately — and its conclusions end where those controls end.
7. **Residual novelty.** Practice-to-criterion and counterbalancing distribute first-exposure
   effects; they do not abolish the fact that every participant meets peripheral diminishment for
   the first time that hour. Longitudinal adaptation — the deployment-relevant question — is future
   work by construction.
8. **Passthrough acuity boundary.** The task was designed around the platform's acuity ceiling
   (virtual panel or pilot-verified large print), so no claim extends to fine-detail real-world
   tasks such as reading small text through passthrough — the very context that motivated some of
   the use cases. This is a hardware boundary the dissertation documents rather than solves.

## 7.6 Implications in both futures

**If the system works (rows 1–2).** The immediate implication is that subtractive attention
guidance is deliverable *today*, at Tier 2, on unmodified consumer hardware — no OS cooperation, no
eye tracker, no detector. The near-term research programme that opens is concrete: hour-scale field
deployment of Soft Dark (the camera-free mode whose endurance T5 tests), the temporal-transition
and user-controlled-release questions already identified as gaps in the literature review (gaps 3
and 4), an eye-tracked replication to convert performance findings into attention-allocation
findings, and an OST translation via segmented dimming hardware. A row-2 outcome adds the design
rule that mode selection is a safety decision, and would motivate a context-detection layer —
which is where the oracle-gap measurement re-enters as an engineering budget.

**If the system does not work (row 3).** The negative is bounded and useful. For the field, it
would indicate that peripheral visual attenuation on passthrough — the cheapest and most deployable
DR — does not by itself buy back distraction costs, redirecting effort toward the channels the
overlay cannot touch: semantic knowledge of the distractor, auditory leakage, and interruption
timing. For this dissertation, the contributions that survive are exactly the ones constructed to
be result-independent: the design space and its tier constraints (C1), the reference implementation
and its transferable engineering lessons (C2), the benchmark characterisation of a consumer VST
platform (C3), and a pre-registered, falsifiable evaluation method for DR attention claims (C4) —
which a null result would demonstrate rather than undermine. A field that currently argues DR's
promise largely from simulation would gain its first hard, hardware-grounded boundary.

**In either future, the method is part of the contribution.** The evaluation apparatus built for
this dissertation — a single primary endpoint with a smallest effect size of interest, equivalence
margins for safety claims, a gated pilot whose failure aborts rather than degrades the main study,
and a decision table committed before data collection — is not standard practice at
masters-project scale, where underpowered multi-hypothesis designs that can only end inconclusively
remain common. The apparatus costs little (its artefacts are documents and thresholds, not
equipment) and converts every outcome into a claim with stated bounds. Whichever row of the
decision table occurs, the dissertation demonstrates the apparatus by using it in public, which is
a transferable contribution to how small-N XR evaluations can be run.

Both futures are written here with equal seriousness because the study was designed so that either
one is a finding. That property — not any particular row of the decision table — is the standard
this dissertation set out to meet.

## References (this chapter)

- Cheng, Y., Yin, H., Yan, Y., Gugenheimer, J., & Lindlbauer, D. (2022). Towards Understanding
  Diminished Reality. *CHI 2022*. https://dl.acm.org/doi/10.1145/3491102.3517452
- McLaughlin, et al. (2025). Cognitive Aid Design Using Diminished Reality to Support Selective
  Attention by Reducing Distraction. *Human Factors*. doi:10.1177/00187208251325169
- Murphy, et al. (2021). Diminishing Reality: Potential Benefits and Risks. *HFES 2021*. [VERIFY
  full author list and venue details against the collected PDF]
- Norouzi, N., et al. (2018). [Head-coupled field-of-view restriction and simulator sickness.]
  *ACM SAP 2018*. [VERIFY exact title]
- Sutton, J., Langlotz, T., Plopski, A., Zollmann, S., Itoh, Y., & Regenbrecht, H. (2022). Look
  over there! Investigating Saliency Modulation for Visual Guidance with Augmented Reality Glasses.
  *UIST 2022*. https://dl.acm.org/doi/10.1145/3526113.3545633
- Impact of visual distractors in VR on sustained attention. *Frontiers in Human Neuroscience*
  (2025). https://pmc.ncbi.nlm.nih.gov/articles/PMC12698649/
- Higgins, et al. (2022). Head Pose as a Proxy for Gaze in Virtual Reality. *VAM-HRI 2022*.
  https://iral.cs.umbc.edu/Pubs/Higgins2022VAM-HRI.pdf
- Sitzmann, V., et al. (2018). How do people explore virtual environments? arXiv:1612.04335.
- Fernandes, A. S., & Feiner, S. K. (2016). Combating VR sickness through subtle dynamic
  field-of-view modification. *IEEE 3DUI 2016*. [Context for FOV-restriction lineage]
- Magic Leap 2 segmented dimming — Magic Leap developer documentation. [VERIFY]
- Meta Passthrough Styling API; Meta MR Motifs "Passthrough Transitioning"; Passthrough Windows —
  Meta Horizon OS developer documentation. [VERIFY page titles]
- "Native Mixed Reality Compositing on Meta Quest 3." arXiv:2509.18929 (2025).
- Lakens, D. (2017). Equivalence Tests. *Social Psychological and Personality Science*.
  doi:10.1177/1948550617697177
