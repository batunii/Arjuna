# Chapter 8 — Conclusion and Future Work

## 8.1 The question, revisited

This dissertation asked one question with two halves: *can subtractive attention guidance be
delivered on consumer XR hardware — and when it is delivered, does it actually help human attention?*

The first half has been answered in the affirmative, with precision about what "delivered" means.
Chapter 3 showed that on the Meta Quest 3 the question reduces to a compositing-rights problem, and
mapped the three tiers of access an application actually has: a colour-only styling interface to the
OS-owned passthrough layer; overlay compositing on top of it; and full pixel control over a
lower-quality camera feed. Chapter 4 demonstrated that this narrow space is sufficient for a working
multi-mode attention-guidance system, built around an architectural inversion — the focus region
rendered as an *absence* in an overlay, so that native passthrough at full quality serves the region
that matters, while three peripheral treatments (ColorPop, Soft Dark, Hard Dark) occupy the space
around it. The hybrid composite at the heart of the most capable mode — native passthrough inside the
window, a shader-processed camera feed outside it — survived a structured prior-art search (Chapter
2) with no published or shipped precedent found. Chapter 5 specified how the system's costs are to be
measured, including the distance between live on-device detection and the offline oracle that
anchors the dissertation's detector-dependent analysis (the study itself runs detector-free), and
reported preliminary results from the offline detection pipeline.

The second half of the question is, deliberately, not yet answered — but it is now *answerable*, and
that is the contribution of Chapter 6. The pre-registered study design carries a single, properly
powered primary hypothesis (peripheral dimming reduces the measurable cost that distractors impose on
a focal task), an equivalence-bounded safety hypothesis (salience re-grading must not degrade
peripheral awareness beyond a stated margin), pilot gates that prevent an uninterpretable main study
from launching, and a decision table that commits the analysis to a conclusion under every outcome.
Whatever the data say when the study runs, the dissertation's question will receive a direct answer:
the system works, the system works at a quantified cost, or the system does not deliver a meaningful
benefit. "Inconclusive" is not among the reachable outcomes.

## 8.2 Contributions, honestly stated

Four contributions were claimed in Chapter 1; their standing at the time of writing is as follows.

**C1 — the design space** (Chapter 3) is complete as an analytical contribution and is grounded in
implementation experience rather than speculation; its quantitative columns (frame cost, latency,
legibility per tier) await the Chapter 5 benchmark campaign.

**C2 — the reference implementation** (Chapter 4) is complete and running on target hardware: the
world-locked focus-window architecture, world-direction fusion sampling for the monocular feed,
motion-coupled suppression, and the three modes, together with an engineering record that reports
dead ends — screen-space sampling double vision, a deprecated surface-projection API, shader
stripping on Android — as reusable knowledge.

**C3 — the technical evaluation** exists as a specified plan with preliminary results (the offline
full-resolution detection bake: 9,170 detections across 720 sampled frames, 97% of samples with at
least one detection). The benchmark campaign itself — frame timing, latency, legibility, thermal
endurance, and the oracle-versus-live gap — remains to be run. [Update this paragraph, and Chapter 5,
once the campaign is complete.]

**C4 — the pre-registered study design** is complete, ethics-mapped against the approved protocol,
and instrumented on paper down to verbatim participant scripts and exclusion rules. Data collection
had not begun at the time of writing. [Update this paragraph, and Chapters 6–7, once data exist.]

An examiner should read C3 and C4 as what they are: the empirical programme of this dissertation, in
a state where every analytical decision has been made and committed to in advance of the data —
which is precisely the state in which such decisions carry evidential weight.

## 8.3 Future work

Six directions follow directly from the material.

**Closing the oracle gap.** The evaluated modes are detector-free; detector-dependent guidance
variants exist in this dissertation only as designs characterised against the offline oracle, and
Chapter 5's benchmark quantifies how far live on-device detection currently falls short of it. The
engineering programme this implies — quantised detectors, region-of-interest second passes,
temporally amortised inference — is well charted, and the study's design guarantees that its
human-factors findings remain valid as that gap closes. The natural next system iteration wires a
live detector into ColorPop's salience hierarchy and evaluates the detector-dependent variant this
dissertation deliberately deferred.

**Transfer to optical see-through hardware.** The Tier-2 dark overlay does not transfer to OST
glasses, whose additive optics cannot darken the world; this was stated in Chapter 7 and bears
repeating as a boundary, not a defect. What does transfer is the taxonomy itself — on OST hardware
the design space collapses towards display-side dimming layers and segment attenuation — and,
critically, the human-factors findings, which concern peripheral diminishment as a perceptual
manipulation rather than any one rendering path. A replication of the Chapter 6 paradigm on a
dimming-capable OST device would be the cleanest possible test of that transfer.

**Adaptive introduction of diminishment.** An idea raised and deliberately deferred during study
design: rather than switching the effect on, a deployed system might introduce it gradually as focus
wanes. As a validation instrument a ramp confounds time with intensity and was excluded from the
study; as an *adaptive-system UX question* — when and how fast should a focus aid fade in, and should
the user or the system control it — it is a well-formed research problem, seeded by the system's
existing formation animation and motion-coupled fade mechanics, and by the pre-registered
time-on-task analysis in the study, which will show whether the vignette's benefit grows as boredom
sets in.

**Dose–response.** The study tests the extreme of the attenuation axis (Hard Dark) confirmatorily and
the midpoint (Soft Dark) subjectively. A discrete-levels design — off, partial, full attenuation,
counterbalanced — would establish the shape of the benefit curve and answer the deployment question
the sampler can only gesture at: how much diminishment buys how much focus, at what comfort cost.

**Hour-long deployment.** The published thermal ceiling on continuous on-device camera processing
(throttling within five to ten minutes) makes Soft Dark — the one mode with no camera pipeline — the
only plausible basis for session-length deployment today. A longitudinal study of real work sessions
under Soft Dark, with the study's workload and comfort instruments administered across days rather
than minutes, is the necessary bridge between this dissertation's controlled findings and any claim
about practice.

**Saliency-weighted diminishment.** The literature review identified the combination of channels
(desaturation with blur; dimming weighted by computed scene saliency) as unstudied in AR attention
guidance. The system's shader architecture already computes per-fragment treatment; driving its
intensity from a saliency model of the periphery — dimming most where the periphery pulls hardest —
is a natural composition of the Tier-3 rendering path with the saliency-modulation literature, and a
candidate for the strongest version of the guidance concept.

## 8.4 Closing

The premise of this work was that a consumer headset can be an instrument for *removing* the world as
well as adding to it — and that both halves of that proposition deserve rigour: the systems half,
where an OS-owned display layer had to be subtracted from by purely additive means; and the human
half, where "it helps you focus" had to be converted from a hope into a hypothesis that could fail.
The system exists and is documented to be rebuilt; the hypothesis is stated, powered, and awaiting
data. Whichever way the data fall, the question will have been asked properly — and on the hardware
people actually own.

## References (this chapter)

- Laghari, M. K., Shaikh, A. A., Khan, F., & Siddiqui, A. G. (2025). Native mixed reality compositing
  on Meta Quest 3: a quantitative feasibility study of ARM-based SoCs and thermal headroom. *arXiv
  2509.18929*. https://arxiv.org/abs/2509.18929
- Cheng, Y., et al. (2022). Towards understanding diminished reality. *Proc. CHI 2022*.
- McLaughlin, A. C., et al. (2025). Cognitive aid design using diminished reality to support
  selective attention by reducing distraction. *Human Factors*, 67(9), 937–961.
  doi:10.1177/00187208251325169
