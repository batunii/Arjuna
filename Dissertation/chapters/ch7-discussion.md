# Chapter 7 — Discussion

## 7.1 What follows from the system itself

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
task-relevant vision never pays the Tier-3 quality tax. Chapter 5, Section 5.5 quantifies the tax at
roughly one to two logMAR lines in the window's favour, plus a narrower field of view on the
camera-fed path (an unplanned finding of the same benchmark); the architectural lesson stands
independently: on any platform where an accessible feed is worse than the native view, put the
manipulation where the eyes are not.

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
(attenuate: Soft/Hard Dark; re-grade: SignPop), and the tier taxonomy describes which profiles the
platform physically permits. The analogy also imports a useful discipline from its acoustic
original: noise-cancelling headphones are evaluated on measured attenuation *and* on what they cost
the wearer in awareness — which is exactly the H1/H2b pairing. The analogy is offered as
positioning, not as a claim of equivalence: acoustic cancellation is subtractive at the signal
level, whereas everything in Chapter 3 exists precisely because consumer VST compositing forbids
signal-level subtraction and forces synthesis by overlay.

### 7.1.4 What the study establishes, and what it does not

The study ran on 18 participants and its primary hypothesis is supported. The literature gave
reason to take both outcomes seriously — peripheral distractors reliably impose sustained-attention
costs in headsets (Ai et al., 2025) and DR-style attenuation reduced workload in VR simulation
(McLaughlin et al., 2025), but area darkening's guidance effects come mostly from immersive video
contexts, desaturation alone has shown weak effects (Wang, Gan & Li, 2020), and awareness costs are
documented wherever attenuation succeeds (Murph et al., 2021). The design space contained both a
working system and a placebo. Section 7.2 reports which this is.

What the study does not establish is equally definite. It cannot attribute SignPop's effect to any
one of its six bundled components. It cannot speak to sessions longer than the ~45 minutes of the
protocol, to modes other than the two carried to confirmatory evaluation, or to optical see-through
hardware. And its dynamic-scene block ran on simulated footage with oracle-quality detection, so
its findings travel toward a deployed system only as far as Chapter 5's oracle bound allows.

## 7.2 The result, against the pre-registered frame

The decision table (Chapter 6, Section 6.9.3) committed the dissertation in advance to a conclusion
under every outcome. **The outcome that occurred is row 1: H1 supported, and H2b's refuting
observation did not occur.** The meaning that row carries was fixed before the data existed, and
this section reports it together with the two qualifications the data attach to it.

### 7.2.1 What row 1 means here

Peripheral diminishment improved focal-task accuracy under sustained real distraction, on real
passthrough, on consumer hardware, by a little over three percentage points with a confidence
interval clear of zero (Section 6.11.2). The prior-art search reported in Chapter 2 did not find an
existing performance-based demonstration of DR distraction suppression on consumer passthrough
hardware rather than inside a VR simulation, which is the sense in which this result adds something
the field did not already have.

The claim is mode-in-context and stays that way. What was tested is Hard Dark on a workstation
1-back task with on-screen video distractors in the near periphery, and SignPop on a driving-video
monitoring stimulus.
The design cannot support "dark vignettes work" in general, and the dissertation does not say it.

Two properties of the result deserve emphasis because they are what make a three-point effect
worth reporting rather than noise. First, the two statistical tests agree, and the rank test — which
assumes nothing about the shape of the distribution and is the more conservative choice at *n* = 17
— returns the lower *p*-value. The finding does not rest on a normality assumption. Second, the
effect survived a mid-collection change in task parameters that split the sample five to twelve, at
the same magnitude in both halves (+3.30 and +2.99 points), differing only in dispersion for a
reason the ceiling explains (Section 6.11.2). An effect that reproduces at the same size across a
stimulus change is behaving like a property of the manipulation rather than of one stimulus
configuration.

The size itself should not be oversold. Three percentage points of accuracy on a 1-back task is a
real but modest improvement, and its practical significance depends entirely on the cost of an
error in whatever the task stands in for. What the study licenses is the directional and mechanistic
claim, not a claim about magnitude in any deployed setting.

### 7.2.2 The awareness question, and why two tests are reported

H2b asked whether salience re-grading degrades peripheral awareness beyond a ten-point margin. It
does not: non-inferiority against that margin passes at *p* = .0002, and the point estimate is a
rise rather than a drop (Section 6.11.3). The safety question the hypothesis exists to answer is
answered.

Two-sided equivalence is nonetheless not established, and Chapter 6 reports both tests rather than
the one that passes. The two-sided procedure fails only on its upper bound, which means the data
cannot exclude a benefit larger than ten points. That is a mismatch between a two-directional test
and a one-directional hypothesis, not a safety signal — but reporting only the non-inferiority
result would be selecting a test after seeing which one passed, and the pre-registered frame exists
precisely to forbid that. The distinction is left visible so a reader can weigh it independently.

### 7.2.3 The near-periphery result, and why its location matters

Block B's overall detection difference is +6.00 points with a confidence interval that includes
zero (Section 6.11.3). Under the pre-registered frame this is row 4 applied to a secondary
measure: real-signed, moderate, and unclaimable at this sample size. It is reported as a bounded
estimate with its interval and its achieved power, and the temptation row 4 exists to forbid — the
retrospective discovery that a smaller effect would also have been meaningful — is declined.

The eccentricity analysis is a different matter. Detection in the 20–30° band improved by 17.55
points, significant on both tests, *d*<sub>z</sub> = 0.722, at 84 % achieved power (Section 6.11.4).
This is the only Block B measure that is both significant and adequately powered, and it is roughly
three times the size of any other band.

Its location is the reason it is convincing rather than merely favourable. The filter is a graded
dose, not a hard boundary: clear to 8°, half strength at 17°, full at 26° (Section 4.2). The band
where the effect appears is the band where that dose arrives at maximum while targets are still
large enough to resolve. The bands on either side behave as the same account predicts — inside 10°
the filter is not acting, and beyond 30° it is at full strength but the targets are hardest. An
effect that tracks the dose profile of the manipulation is harder to explain as chance than one
that simply appeared somewhere.

This also sharpens what SignPop is for. The mode does not make the whole visual field easier to
monitor. It buys detection in a specific annulus, which is a narrower and more useful design claim
than "salience re-grading helps", and one a designer can act on when choosing window size.

One caveat bounds the finding independently of its statistics, and it is a measurement caveat rather
than an inferential one. Band membership uses each target's mid-lifetime position, and targets move:
the median lifetime swing is 17.2°, and roughly 31 % of catches land in a band other than the one
that position assigns. The effect is therefore located to roughly the right annulus rather than
precisely to 20–30°, and a press-time reconstruction (Section 6.11.4) would be required to tighten
it.

### 7.2.4 Where the subjective and behavioural results disagree

The questionnaire's focus-benefit construct runs at 89 % favourable across 16 participants
(Section 6.11.5), which agrees with the Block A behavioural result and adds little beyond it. The
interesting disagreement is in the awareness construct. Only 7 of 16 participants reported noticing
side events at their usual speed, and only 7 of 16 were comfortable not seeing everything around
them, while the behavioural data show peripheral detection non-inferior overall and improved in the
near periphery.

**The perceived awareness cost exceeds the measured one.** Two readings are available and the data
collected here cannot separate them.

The *cost* reading takes the reports at face value: the manipulation delays peripheral awareness in
ways the probe task did not catch, because the probes were task-relevant events participants were
instructed to look for, and attention to an expected target is not the same as noticing something
unexpected.

The *mechanism* reading treats the reports as the subjective signature of the manipulation working
as designed. Peripheral onsets no longer capture attention involuntarily; events that persist are
attended eventually. Under this reading a participant reporting "I noticed things later" is
describing exactly what peripheral suppression should feel like from the inside, and the answer that
would indicate failure is "I noticed everything instantly, same as always".

The behavioural evidence favours the mechanism reading for task-relevant events, since detection was
not degraded anywhere and improved in the near periphery. What remains unmeasured is
task-*irrelevant* events, which is precisely where the two readings diverge. Chapter 8 proposes the
instrument that separates them: decoy targets in regions the task does not care about, where slower
responses under the filter would indicate desired filtering, while delayed responses to
meaningful-but-unexpected events would indicate the hazard.

Either way, one design observation stands on its own. Users perceive a larger awareness cost than
the instruments record, and for a system whose value proposition is subtraction, perceived cost
governs adoption as firmly as measured cost. A deployed version would need to address the feeling
of being cut off, not only the fact of it.

## 7.3 Block A versus Block B: a built-in test of simulation-based evaluation

The two confirmatory blocks run the same guidance principle at opposite ends of the feasibility
spectrum: Block A on the real Tier-2 artifact over live passthrough with real moving distractors;
Block B on the simulated context — video sphere, scripted probes — that stands in for the future
system. This pairing was designed as a bridge (framing document, Section 4), and it carries an
incidental methodological payload.

**The two blocks agree in direction.** Diminishment-family manipulations improved focal performance
in both, on the workstation task and on the driving-scene detection task alike (Sections 6.11.2 and
6.11.4). The simulation methodology on which the nearest
published work relies entirely (Cheng et al., 2022; McLaughlin et al., 2025, both VR-simulated)
therefore gains a point of external credibility from this project's real-hardware anchor: an effect
observed in simulation was also observed on the artefact itself, in the same direction.

That agreement should be read at the level of sign and decision-table row, which is what the design
supports, and not at the level of magnitude. The blocks use different tasks, different dependent
variables, and different modes; they were not powered as a formal cross-context contrast; and no
ratio between them is interpreted. The statement the design licenses is that both point the same
way, and it does not extend further.

One asymmetry is worth recording because it runs against the convenient reading. The real-hardware
block produced the *cleaner* result — significant on both tests, adequately powered — while the
simulated block's overall effect remained a bounded estimate and only its band-level analysis
resolved. Simulation is often justified on the grounds that it offers experimental control the real
artefact cannot, and here the controlled context was the noisier measurement of the two. The
explanation is mundane rather than deep: Block B's free-response detection task admits response
strategies that a forced-choice task does not, and its 40 scored targets per condition are far fewer
than Block A's 140 trials. The methodological point stands regardless. Control in the stimulus does
not automatically produce precision in the estimate, and a simulated context can be the weaker
evidence even when it is the tidier experiment.

## 7.4 Transfer: what leaves this dissertation, and under what conditions

**To optical see-through (OST) hardware, the dark overlay does not transfer.** The Tier-2 modes
work because a VST display is an opaque screen under full render control; additive OST optics
cannot darken the world. What transfers is the taxonomy and the question. On OST hardware the
taxonomy collapses toward display-side attenuation — of which segmented dimming hardware (e.g.
Magic Leap 2's segmented dimmer, a dedicated low-resolution panel that locally attenuates light behind masked virtual content) is the nearest existing analogue of a
Tier-2 windowed dim — and toward the additive salience modulation demonstrated by Sutton et al.
(2022). Whatever Chapter 6 finds about *what peripheral diminishment does to attention* transfers
as a human-factors result to any hardware that can implement an equivalent stimulus; the
implementation findings transfer only within VST.

**From the simulation, transfer is conditional, and the conditions are enumerated.** Headset weight
biases only comfort ratings, and conservatively; attention effects are unaffected in either
direction by lightening hardware. Monoscopic video removes motion-in-depth, so peripheral capture
by looming stimuli is untested. The passenger viewpoint removes task coupling, which is why no
statement in this dissertation concerns driving performance, and none should be quoted as if it
did. Video dynamic range compresses glare, so SignPop's glare-suppression track is exercised only
weakly. Identical stimuli for all participants trade ecological breadth for internal validity, and
the trade is intended. Block A, running on the real system against real moving distractors, is the one
component that transfers without these caveats — which is why it carries the primary hypothesis.

**Oracle conditioning.** Every statement about detector-dependent guidance variants is conditioned
on the oracle assumption. Its size has not been measured (Chapter 5, Section 5.5), so "the detector
sees everything in time" remains an assumption rather than a fact. The study protocol is not insulated from this caveat:
SignPop, Block B's evaluated mode, is itself detector-dependent — on offline, oracle-quality
detection rather than a live pipeline — so RQ2/H2a/H2b inherit the same conditioning as any future
detector-dependent extension, not a lesser version of it.

## 7.5 Limitations, consolidated

The limitations below are collected from Chapters 3–6 so that they appear once, together, rather
than diluted across the text.

1. **Mode × scenario confound.** Hard Dark is tested only in the workstation context, SignPop only
   in the monitoring context. Every empirical claim is mode-in-context; the design cannot separate
   mode effects from scenario effects, and Block C's common-stimulus ratings soften this only at
   the subjective level.
2. **SignPop's manipulation is a bundle.** A confirmed detection switches on six effects at once —
   colour pop, window carve-out, saturation lift, brightness lift, contrast expansion, and darkened
   surround (Chapter 4, Section 4.5.6) — none isolable in the current design, so RQ2/H2a/H2b cannot
   attribute a measured benefit to any one of them.
3. **Soft Dark carries sampler-level data only**, and the sampler is skippable under the fatigue
   rule, so its completion rate bounds even that. The mode's retention is justified on design-space
   grounds (testing-strategy, Section 3.5), not empirical ones.
4. **No eye tracking.** Quest 3 has none. Head pose is a validated proxy at the eccentricities used
   (Higgins et al., 2022; Sitzmann et al., 2018), but covert attention shifts — distraction without
   head movement — are invisible to telemetry. The dependent variables were chosen so that
   *performance outcomes*, not attention allocation, carry the hypotheses; the mechanism remains
   partially inferred.
5. **Sensitivity floor.** The realised sample gives 82 % power on Block A, which is adequate for
   the effect observed, but only 50 % on Block B's overall detection measure — which is why that
   estimate is reported as bounded rather than claimed (Section 7.2.3). Block B's band analysis
   reaches 84 %. Effects smaller than those observed would be detectable only as estimates with
   wide intervals.
10. **Block B eccentricity bands use a mid-lifetime proxy.** Targets move, the median lifetime
   swing is 17.2°, and roughly 31 % of catches land in a band other than the one assigned. The
   20–30° finding is therefore located to approximately the right annulus rather than precisely,
   and tightening it requires the press-time reconstruction specified but not implemented here.
11. **The awareness claim is scoped to expected events.** H2b's probes were task-relevant targets
   participants were instructed to watch for. Detection of task-*irrelevant* or unexpected events
   was not measured, and that is exactly where the subjective reports and the behavioural data
   diverge (Section 7.2.4).
6. **Single experimenter, single site, single hardware generation.** Scripts and checklists bound
   experimenter variance but cannot eliminate it; every number is specific to Quest 3 and the
   frozen OS/SDK versions, on a platform whose capabilities have already moved once mid-project.
7. **Ecological validity boundaries.** The workstation distractors are two on-screen video panels
   with scheduled bursts; the monitoring stimulus is a video. The study measures attention mechanisms under
   controlled distraction — deliberately — and its conclusions end where those controls end.
8. **Residual novelty.** Practice-to-criterion and counterbalancing distribute first-exposure
   effects; they do not abolish the fact that every participant meets peripheral diminishment for
   the first time that hour. Longitudinal adaptation — the deployment-relevant question — is future
   work by construction.
9. **Passthrough acuity boundary.** The task was designed around the platform's acuity ceiling
   (virtual panel or pilot-verified large print), so no claim extends to fine-detail real-world
   tasks such as reading small text through passthrough — the very context that motivated some of
   the use cases. This is a hardware boundary the dissertation documents rather than solves.

## 7.6 Implications

**Subtractive attention guidance is deliverable today.** The result establishes that it works at
Tier 2 on unmodified consumer hardware, with no OS cooperation, no eye tracker, and no live
detector. Hard Dark — the mode carrying the primary result — is overlay-only, which means the
finding rests on the cheapest and most deployable point in the entire design space rather than on
the most capable one. That matters for anyone deciding whether to build in this area: the
expensive Tier-3 camera path is what SignPop needs, but the focal-attention benefit does not
require it.

The near-term research programme this opens is concrete. Hour-scale field deployment of Soft Dark,
the camera-free mode, is the natural bridge from a 45-minute laboratory session to a working
session. The temporal-transition and
user-controlled-release questions identified as gaps 3 and 4 in the literature review are now
questions about a system known to work rather than about a hypothetical one. An eye-tracked
replication would convert a performance finding into an attention-allocation finding, which is the
mechanism claim the current dependent variables can only infer. And the 20–30° band result gives an
OST translation a specific target: a segmented dimming device would need spatial resolution in that
annulus, not uniform attenuation.

**The design rule that follows from the awareness data.** Peripheral detection was not degraded, so
mode selection is not the safety decision this dissertation anticipated it might be. What the data
support instead is a subtler rule. Users perceive an awareness cost the instruments do not record
(Section 7.2.4), so the deployment risk is acceptance rather than performance. A system that
suppresses the periphery successfully will feel more costly than it is, and design effort belongs on
that gap — on making suppression legible and reversible, so the user knows what is being hidden and
can retrieve it — rather than on defending a measured awareness deficit that did not appear.

**The unresolved measurement is the one worth funding next.** Both readings of the subjective data
are live, and separating them requires an instrument this study did not carry: decoy events in
task-irrelevant regions, where slower responses under the filter would confirm desired filtering
while delayed responses to meaningful-but-unexpected events would confirm the hazard. Until that
runs, "does not degrade awareness" means "does not degrade detection of events the participant was
told to look for", and that is the honest scope of the H2b claim.

**The method is part of the contribution.** The evaluation apparatus built here — a single primary
endpoint, equivalence margins for the safety claim, a gated pilot whose failure aborts rather than
degrades the main study, and a decision table committed before collection — is not standard practice
at masters-project scale, where underpowered multi-hypothesis designs that can only end
inconclusively remain common. It cost little, since its artefacts are documents and thresholds
rather than equipment, and it did the work it was built for. The positive row it resolved to is
credible *because* the negative rows were specified with equal seriousness and would have been
reported with equal prominence. Two places in this chapter show the apparatus constraining the
conclusion rather than decorating it: the Block B overall estimate is reported as unclaimable under
row 4 despite being positive and convenient, and both the equivalence and non-inferiority tests are
reported for H2b rather than only the one that passed. A frame that never costs anything is not
doing its job.

## References (this chapter)

- Cheng, Y., Yin, H., Yan, Y., Gugenheimer, J., & Lindlbauer, D. (2022). Towards Understanding
  Diminished Reality. *CHI 2022*. https://dl.acm.org/doi/10.1145/3491102.3517452
- McLaughlin, A. C., et al. (2025). Cognitive Aid Design Using Diminished Reality to Support
  Selective Attention by Reducing Distraction. *Human Factors*, 67(9), 937–961.
  doi:10.1177/00187208251325169
- Murph, I., McDonald, M., Richardson, K., Wilkinson, M., Robertson, S., Karunakaran, A., Gandy
  Coleman, M., Byrne, V., & McLaughlin, A. C. (2021). Diminishing Reality: Potential Benefits and
  Risks. *Proceedings of the Human Factors and Ergonomics Society Annual Meeting*, 65(1), 164–168.
  doi:10.1177/1071181321651103
- Norouzi, N., Bruder, G., & Welch, G. (2018). Assessing Vignetting as a Means to Reduce VR Sickness
  During Amplified Head Rotations. *15th ACM Symposium on Applied Perception (SAP '18)*.
  doi:10.1145/3225153.3225162
- Sutton, J., Langlotz, T., Plopski, A., Zollmann, S., Itoh, Y., & Regenbrecht, H. (2022). Look
  over there! Investigating Saliency Modulation for Visual Guidance with Augmented Reality Glasses.
  *UIST 2022*. https://dl.acm.org/doi/10.1145/3526113.3545633
- Ai, X., Wang, Y., Wang, P., & Wang, S. (2025). Impact of Visual Distractors in Virtual Reality
  Environments on Sustained Attention Behavioral Performance and EEG Characteristics. *Frontiers in
  Human Neuroscience*. https://pmc.ncbi.nlm.nih.gov/articles/PMC12698649/
- Higgins, P., Barron, R., & Matuszek, C. (2022). Head Pose as a Proxy for Gaze in Virtual Reality.
  *VAM-HRI 2022*. https://iral.cs.umbc.edu/Pubs/Higgins2022VAM-HRI.pdf
- Sitzmann, V., Serrano, A., Pavel, A., Agrawala, M., Gutierrez, D., Masia, B., & Wetzstein, G.
  (2018). Saliency in VR: How Do People Explore Virtual Environments? *IEEE TVCG*, 24(4), 1633–1642.
  (arXiv:1612.04335)
- Fernandes, A. S., & Feiner, S. K. (2016). Combating VR sickness through subtle dynamic
  field-of-view modification. *IEEE 3DUI 2016*. [Context for FOV-restriction lineage]
- Magic Leap (2024). *Global/Segmented Dimmer.* Magic Leap Developer Documentation.
  developer-docs.magicleap.cloud/docs/guides/features/dimmer-feature/
- Meta (2024). *Customize Passthrough Color Mapping*; *Passthrough Transitioning Motif*;
  *Passthrough Windows.* Meta Horizon OS developer documentation.
- Laghari, M. K., Shaikh, A. A., Khan, F., & Siddiqui, A. G. (2025). "Native Mixed Reality Compositing
  on Meta Quest 3: A Quantitative Feasibility Study of ARM-Based SoCs and Thermal Headroom."
  arXiv:2509.18929.
- Lakens, D. (2017). Equivalence Tests. *Social Psychological and Personality Science*.
  doi:10.1177/1948550617697177
- Wang, G., Gan, Q., & Li, Y. (2020). Research on Attention-guiding Methods in Cinematic Virtual
  Reality Based on Eye Tracking Analysis. *2020 International Conference on Innovation Design and
  Digital Technology (ICIDDT)*. doi:10.1109/ICIDDT52279.2020.00020
