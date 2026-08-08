# Chapter 8 — Conclusion and Future Work

## 8.1 The question, revisited

This dissertation asked one question with two halves: *can subtractive attention guidance be
delivered on consumer XR hardware — and when it is delivered, does it actually help human attention?*

The first half is answered in the affirmative, with precision about what "delivered" means.
Chapter 3 showed that on the Meta Quest 3 the question reduces to a compositing-rights problem, and
mapped the three tiers of access an application actually has: a colour-only styling interface to the
OS-owned passthrough layer; overlay compositing on top of it; and full pixel control over a
lower-quality camera feed. Chapter 4 demonstrated that this narrow space is sufficient for a working
multi-mode attention-guidance system, built around an architectural inversion — the focus region
rendered as an *absence* in an overlay, so that native passthrough at full quality serves the region
that matters, while ten peripheral treatments spanning the design space occupy the periphery around
it — two of them, SignPop and Hard Dark, carried to confirmatory evaluation. The hybrid composite at
the heart of the most capable mode — native passthrough inside the window, a shader-processed camera
feed outside it — survived a structured prior-art search (Chapter 2) with no published or shipped
precedent found. Chapter 5 measured what the architecture costs and what it buys: the study
configurations render inside the 72 Hz frame budget, and the native-passthrough window resolves
roughly one to two logMAR lines finer than the Tier-3 camera path, which is the quantitative case
for the inversion.

**The second half is answered too, and the answer is yes, within a stated scope.** Peripheral
diminishment improved focal-task accuracy under sustained real distraction, at a sample size that
gave the test adequate power, and the improvement was significant on both a parametric and a
rank-based test. Salience re-grading did not degrade peripheral awareness against the pre-registered
margin, and improved detection in the 20–30° band, which is where the filter reaches full strength
while targets remain resolvable. Chapter 6, Section 6.11 gives the figures; the pre-registered
decision table, committed before collection, resolves to its positive row.

The scope of that answer is as important as its direction. The claim is mode-in-context: Hard Dark
on a workstation task, SignPop on a monitoring stimulus. It is bounded to sessions of about
45 minutes, to expected rather than unexpected peripheral events, and — for the dynamic block — to a
simulated context with oracle-quality detection. Chapter 7 states these boundaries in full, and none
of them is repaired by more participants.

## 8.2 Contributions, honestly stated

Four contributions were claimed in Chapter 1; their standing is as follows.

**C1 — the design space** (Chapter 3) is complete as an analytical contribution and is grounded in
implementation experience rather than speculation. Two of its quantitative columns, frame cost and
legibility per tier, are measured (Chapter 5, Sections 5.3 and 5.5); latency awaits the remainder of
the benchmark campaign.

**C2 — the reference implementation** (Chapter 4) is complete and running on target hardware: the
world-locked focus-window architecture, world-direction fusion sampling for the monocular feed,
motion-coupled suppression, and ten peripheral treatments spanning the design space, of which two —
SignPop and Hard Dark — are carried to confirmatory evaluation, together with an engineering record
that reports dead ends — screen-space sampling double vision, a deprecated surface-projection API,
shader stripping on Android — as reusable knowledge.

**C3 — the technical evaluation** measures what the architecture costs and what it buys
(Chapter 5). The study's Tier-2 configurations pass their frame-budget and stale-frame criteria; the
video-scene configurations fail the stale-frame criterion regardless of vignette treatment, a
negative result that bounds the dynamic block and is reported rather than smoothed; and the window
path resolves roughly one to two logMAR lines finer than the Tier-3 camera path, which is the
quantitative case for routing the focus region through the native layer. Three further
measurements — world-locking stability, thermal endurance, and the gap between live and oracle
detection — were scoped out, and Chapter 5, Section 5.5 states what each would have bounded. The
last of these is the one that matters most for how far the driving-block result travels.

**C4 — the pre-registered study and its results** is complete. Eighteen participants were tested
between 22 July and 7 August 2026, yielding 17 paired participants on the primary block, 15 on the
secondary block, and 16 questionnaires. Every pre-registered test is reported, including the Block B
overall detection difference that the sample cannot resolve and that is therefore reported as a
bounded estimate rather than claimed. One protocol deviation occurred — a mid-collection change to
the Block A trial count — and is reported with the analysis showing the effect reproduces at the
same magnitude on both sides of it.

An examiner should read C3 and C4 as what they are: an empirical programme in which the analytical
decisions were made and committed to in advance of the data, and in which the places where the data
fall short of those commitments are marked rather than reinterpreted.

## 8.3 Future work

Six directions follow directly from the material, and the results sharpen several of them.

**Separating filtering from hazard.** The clearest gap the results open is the divergence between
perceived and measured awareness cost (Chapter 7, Section 7.2.4). Participants report noticing
peripheral events later while the behavioural data show detection non-inferior and, in the near
periphery, improved. Two readings survive the current evidence, and separating them requires decoy
events in task-irrelevant regions: slower responses under the filter would confirm the manipulation
is filtering what it should, while delayed responses to meaningful-but-unexpected events would
identify a real hazard. This is the single most informative follow-up and it needs no new hardware.

**Closing the oracle gap.** SignPop is detector-dependent today, on offline oracle-quality detection
rather than a live pipeline, and the measurement described in Chapter 5, Section 5.5 would quantify
how far live on-device detection falls short. The engineering programme this implies is well charted:
quantised detectors, region-of-interest second passes, temporally amortised inference. The natural
next system iteration replaces SignPop's offline oracle gate with a live detector and re-runs the
dynamic block to test whether the 20–30° benefit survives the live-detection accuracy and coverage
gap this dissertation deliberately left unclosed.

**Isolating the bundle.** SignPop switches on six effects at once, so the near-periphery result
cannot be attributed to any one of them. A three-arm follow-up — no filter, filter with detection
gating off, filter with detection gating on — separates the contribution of the detection gate from
that of the surrounding attenuation, and is the minimum design required before the mode's mechanism
can be stated rather than inferred.

**Transfer to optical see-through hardware.** The Tier-2 dark overlay does not transfer to OST
glasses, whose additive optics cannot darken the world; this is a boundary, not a defect. What does
transfer is the taxonomy itself, and — critically — the human-factors findings, which concern
peripheral diminishment as a perceptual manipulation rather than any one rendering path. The 20–30°
result gives such a replication a specific engineering target: a segmented dimming device would need
spatial resolution in that annulus rather than uniform attenuation.

**Dose–response and adaptive introduction.** The study tests the extreme of the attenuation axis
(Hard Dark) confirmatorily and the midpoint (Soft Dark) subjectively. A discrete-levels design —
off, partial, full, counterbalanced — would establish the shape of the benefit curve and answer the
deployment question the sampler can only gesture at. Related and deliberately deferred: rather than
switching the effect on, a deployed system might introduce it gradually as focus wanes. As a
validation instrument a ramp confounds time with intensity and was excluded; as an adaptive-system
UX question it is well formed, and it is now a question about a system known to work.

**Hour-long deployment.** The published thermal ceiling on continuous on-device camera processing
(throttling within five to ten minutes) makes Soft Dark — the one mode with no camera pipeline — the
only plausible basis for session-length deployment today. That the primary result was obtained with
an overlay-only mode makes this more attractive than it would otherwise be: the focal-attention
benefit does not require the expensive rendering path. A longitudinal study of real work sessions
under Soft Dark, with the study's workload and comfort instruments administered across days rather
than minutes, is the necessary bridge between this dissertation's controlled findings and any claim
about practice.

## 8.4 Closing

The premise of this work was that a consumer headset can be an instrument for *removing* the world
as well as adding to it — and that both halves of that proposition deserve rigour: the systems half,
where an OS-owned display layer had to be subtracted from by purely additive means; and the human
half, where "it helps you focus" had to be converted from a hope into a hypothesis that could fail.

It could have failed. The decision table specified, before any data existed, what a bounded negative
would mean and how it would be reported, and the same apparatus that would have enforced that
outcome is what constrains the positive one now: the Block B overall estimate is reported as
unclaimable despite being positive, and both the equivalence and the non-inferiority test are
reported for the safety hypothesis rather than only the one that passed. A frame that never costs
anything is not doing its job, and this one cost something in two places.

What the dissertation delivers is a system that exists and is documented to be rebuilt, a design
space that explains what such systems can and cannot do on hardware people already own, and a
measured answer to whether subtraction helps attention: it does, by a modest amount, in the region
the architecture was built to act on, at a perceived cost larger than the one the instruments can
find. That last gap — between what the periphery loses and what users believe it loses — is the most
interesting thing the study found, and it is where the next experiment should go.

## References (this chapter)

- Laghari, M. K., Shaikh, A. A., Khan, F., & Siddiqui, A. G. (2025). Native mixed reality compositing
  on Meta Quest 3: a quantitative feasibility study of ARM-based SoCs and thermal headroom. *arXiv
  2509.18929*. https://arxiv.org/abs/2509.18929
- Cheng, Y., et al. (2022). Towards understanding diminished reality. *Proc. CHI 2022*.
- McLaughlin, A. C., et al. (2025). Cognitive aid design using diminished reality to support
  selective attention by reducing distraction. *Human Factors*, 67(9), 937–961.
  doi:10.1177/00187208251325169
