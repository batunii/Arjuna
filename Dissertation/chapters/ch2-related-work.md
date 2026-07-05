# Chapter 2 — Background and Related Work

This chapter positions the dissertation within five bodies of literature. Section 2.1 establishes the
perceptual and cognitive foundations: what visual salience is, how it captures attention, and why
removing irrelevant visual information should reduce cognitive load. Section 2.2 reviews diminished
reality (DR) and the emerging framing of visual noise cancellation, the conceptual home of this work.
Section 2.3 reviews the techniques by which attention has been guided in mediated views — from subtle
gaze direction through saliency modulation to overt cues — from which the system's ColorPop mode
directly descends. Section 2.4 reviews what is known about peripheral vision, foveated degradation,
and the comfort limits of peripheral restriction, which constrain how aggressively any peripheral
diminishment may act. Section 2.5 surveys attention guidance in the applied contexts this work draws
its scenarios from. Section 2.6 turns to the platform layer: what consumer passthrough hardware
permits, what systems already exist there, and the documented search establishing that the
architecture contributed by this dissertation has no direct precedent. Section 2.7 reviews how XR
attention systems are evaluated, grounding the methodological choices of Chapter 6. Section 2.8
synthesises the review into the research gaps this dissertation addresses and positions the work
against its two nearest published neighbours.

Two housekeeping notes. First, this project's modes were renamed during development; where cited
work motivated an earlier design (Dynamic / Semi-Dynamic / Static, or the superseded Blur mode), the
text maps it onto the current system (ColorPop, Soft Dark, Hard Dark; see Chapter 4). Second,
citations whose full bibliographic identity could not be confirmed against the source PDF at the
time of writing are marked **[VERIFY]** and collected in the reference list for resolution before
submission.

---

## 2.1 Foundations: Attention, Salience, and Cognitive Load

### 2.1.1 Salience and attentional capture

The computational account of visual attention that underpins this entire dissertation is the
salience-map model of Itti, Koch and Niebur (1998): attention is drawn to locations whose
centre–surround contrast — in intensity, colour opponency, or orientation — differs most from their
neighbourhood. Three consequences of this model recur throughout the design of the present system.
First, *contrast is the master variable*: any manipulation that raises local contrast attracts gaze,
and any manipulation that flattens it repels gaze. Second, salience is channel-specific, and the
channels are not equal — in every head-to-head comparison in the guidance literature, luminance
manipulations outperform chromatic ones (Bailey et al., 2009; Grogorick et al., 2017; Waldin et al.,
2017). Third, salience operates *pre-attentively*: capture by a high-contrast peripheral event
happens before, and regardless of, the observer's intentions. Barreiros et al. argue that AR
visualisations must operate at exactly this pre-attentive level to feel automatic rather than
effortful [VERIFY], and the same holds in reverse for diminishment: a periphery whose salience has
been flattened stops issuing capture events at all, which is the mechanism this dissertation's dark
modes exploit.

The salience account has recently been extended to augmented reality specifically. Duan et al. (2022)
contribute the first dedicated AR saliency dataset (SARD) and demonstrate that the opacity of AR
content directly and predictably modulates where attention lands — empirical confirmation that an
alpha-blended overlay, the core primitive of this system's Tier-2 modes (Chapter 3), is an attention
instrument with a measurable dose–response character rather than a mere visual style.

### 2.1.2 Cognitive load

Where salience explains *how* the periphery captures attention, Cognitive Load Theory explains *why
that capture is costly*. Extraneous load — processing demanded by information irrelevant to the task
— competes for the same limited working-memory resources as the task itself. Buchner et al. review
the evidence that AR can reduce, not only add, cognitive load when it is used to structure or filter
what the user must process [VERIFY]. This supplies the theoretical justification for the entire
project: peripheral diminishment is an *extraneous-load intervention*. It does not make the user
smarter or the task easier; it removes competing processing demands at the perceptual source. The
dissertation's primary hypothesis (Chapter 6) is this theory operationalised: if peripheral
distractors impose a measurable performance cost on a focal task, a manipulation that suppresses
their salience should reduce that cost.

### 2.1.3 The inherent tension

The same literature that motivates diminishment also names its central risk. Attention capture by
peripheral events is not a design flaw of the human visual system; it is a safety mechanism.
Suppressing it trades focus against situational awareness, a tension documented empirically in the
DR literature (Murphy et al., 2021 [VERIFY]; McLaughlin et al., 2025) and formalised in this
dissertation as an equivalence-tested safety hypothesis rather than an afterthought (H2b, Chapter 6).
This tension — every section of this chapter returns to it — is the reason the dissertation's
evaluation design refuses to measure benefit without simultaneously measuring cost.

---

## 2.2 Diminished Reality and Visual Noise Cancellation

### 2.2.1 Definitions and taxonomy

Mori, Ikeda and Saito (2017) provide the canonical survey and taxonomy of diminished reality: the
family of techniques that *conceal, eliminate, or see through* real objects in a mediated view. Their
survey establishes the field's legitimacy — that mediation of reality can subtract as well as add —
and its historical centre of gravity: most classical DR work concerns object removal (inpainting a
region so the object appears gone), which is computationally demanding and semantically targeted.
The present work sits in a different corner of their taxonomy: it does not remove objects but
*attenuates regions*, trading semantic precision for robustness, generality, and real-time
feasibility on mobile hardware.

Cheng, Yin, Yan, Gugenheimer and Lindlbauer (2022) supply the field's most direct study of how users
*want* reality diminished. Across two studies (N = 16 and N = 12, conducted inside VR simulations of
DR because no deployable DR hardware existed), they found participants preferred partial opacity
reduction over complete removal, and wanted *context-preserving* diminishment — outlines or residual
structure indicating that something is there, even when its detail is suppressed. Both findings are
load-bearing for this dissertation: the preference for partial attenuation motivates the Soft Dark
mode's deliberately incomplete dimming (maximum opacity ≈ 0.75, never a blackout), and the
context-preservation finding motivates treating Hard Dark's total blackout as the *extreme* of the
design space rather than its default. Equally load-bearing is Cheng et al.'s method: they could only
simulate DR inside VR. This dissertation runs on the real passthrough hardware their participants
could only imagine — the delta is developed in Section 2.8.

### 2.2.2 Visual noise cancellation

The most recent conceptual frame for this family of systems is *visual noise cancellation* (VNC): by
analogy with acoustic noise cancellation, a head-worn display actively moderates the visual
environment, attenuating "noise" while passing "signal" (Visual Noise Cancellation, ACM, c. 2024
[VERIFY]). The analogy is productive because it imports an architecture: acoustic ANC requires a
microphone (sensing the environment), a processing stage, and a speaker (re-emitting a modified
signal) — precisely the camera → shader → display pipeline of a video-see-through headset. The VNC
paper is the closest conceptual match to this entire project, and this dissertation can be read as
the first *software-defined VNC system on consumer video-passthrough hardware*: the three modes are
three VNC profiles differing in what they classify as noise (everything peripheral, for the dark
modes; everything non-salient by a colour-based relevance rule, for ColorPop) and in how aggressively
they attenuate it. A follow-up line of work has begun evaluating VNC-style workspaces with sustained
attention and working-memory batteries in mixed-reality settings (see Section 2.7).

### 2.2.3 DR for distraction suppression: the empirical record

The empirical case that diminishment helps focus is recent but consistent. McLaughlin et al. (2025)
provide the closest published analogue to this dissertation's primary hypothesis: in two
within-subject experiments (STEM graduates and NASA Johnson Space Center employees), participants
performed a demanding assembly task under visual and auditory distraction, with DR-style attenuation
of the distractors applied either universally or context-sensitively. Universal attenuation — the
condition structurally closest to this project's peripheral vignette — produced fewer errors
(β = −0.37, p = .021) and lower subjective workload than context-aware removal. Their analysis
(linear mixed-effects models over trial-level data) is adopted directly by this dissertation's
analysis plan. Critically, McLaughlin et al. also measured awareness of off-task objects and events,
finding the focus/awareness trade-off real but manageable — the empirical ancestor of this work's
H2b.

Lee and Kim's DiminishAR (2025) approaches the same goal from the object side: AR-camouflaging a
single distracting object (a smartphone) restored working-memory performance to a level statistically
comparable with physically removing it (N = 60). Murphy et al. document the same benefit–risk
structure in a medical assembly context — DR lowered cognitive workload but risked situational
awareness loss (Murphy et al., 2021 [VERIFY]) — and in later work reframe DR as a *training scaffold*,
in which distractions are gradually reintroduced as the trainee's tolerance grows (Murphy et al.,
2022 [VERIFY]); that gradual-reintroduction logic maps directly onto this system's temporal fade
transitions (formation ramps and motion-triggered suppression, Chapter 4). Richardson et al. address
the complementary problem this dissertation defers to future work: how to alert users to genuinely
critical events inside a diminished region (Richardson et al., 2021 [VERIFY]).

Two 2026 threads confirm both the demand for and the immaturity of DR-for-attention. A co-design
study with fifteen students with ADHD (DIS 2026) developed the concept of *attentional diminishment*
through diary studies and workshops — blur, desaturation, and removal as user-configurable filters —
but built no working prototype (Exploring Diminished Reality for Attention Support, DIS 2026). A CHI
2026 study on Apple Vision Pro obscured specific individuals to alleviate social discomfort,
demonstrating overlay-based person-targeted diminishment on consumer hardware, though as targeted
occlusion rather than peripheral attention guidance (Obscuring Undesirable Individuals…, CHI 2026).
Both are formative or adjacent; neither implements peripheral diminishment for attention on real
passthrough. Earlier applied work points the same direction: Yantaç et al. explored interactive DR
walls filtering irrelevant visual information for individuals on the autism spectrum [VERIFY],
validating the psychological premise of environment filtering for populations for whom distraction
is most costly; and FocalSpace (Yao, DeVincenzi, Pereira and Ishii, 2013) demonstrated the 2D
ancestor of this project's workstation scenario — synthetic background blur in video conferencing,
driven by depth and activity tracking, improving memory for meeting content and user preference.

### 2.2.4 Hardware routes to diminishment

Finally, two projects demonstrate the *hardware* appetite for what this dissertation does in
software. IlluminatedFocus spatially defocuses regions of the real world using electrically tunable
lenses, reshaping perception optically (Ueda et al. [VERIFY]); Programmable Peripheral Vision
explores hardware and conceptual approaches to augmenting the peripheral visual field (CHI EA 2022
[VERIFY]). Both accept substantial optical complexity to achieve region-selective manipulation of
perceived reality. That the same capability now falls out of a shader on a €550 consumer headset is
the platform shift this dissertation documents in Section 2.6 and Chapter 3.

---

## 2.3 Saliency Modulation and Gaze Guidance in Mediated Views

The techniques for steering attention in a mediated view form a spectrum from imperceptible to overt.
This section traverses it: subtle gaze direction (2.3.1), saliency modulation of video see-through
imagery — the direct ancestor of ColorPop (2.3.2), and overt cues with the comparative studies that
justify combining techniques (2.3.3).

### 2.3.1 Subtle gaze direction

The subtle gaze direction (SGD) family originates with Bailey, McNamara, Sudarsanam and Grimm (2009):
a brief luminance modulation (±9.5 %, ~0.76° region, 10 Hz) presented in the viewer's periphery
attracts a saccade, and the cue is terminated the moment gaze moves toward it — so the viewer never
foveates the modulation and remains unaware of being steered. Luminance modulation outperformed
warm–cool chromatic modulation, an early instance of the luminance-dominance law of Section 2.1.
McNamara, Bailey and Grimm (2008) demonstrated task-level value: in visual search, subtle modulation
raised accuracy from 40.97 % to 56.25 % — statistically indistinguishable from an *obvious* cue
(56.94 %) — with zero of eighteen participants noticing the manipulation. Sridharan, Bailey, McNamara
and Grimm (2012) extended the paradigm to mammography training (accuracy 54.2 % → 64.5–68.8 %), and a
later SAP paper added automatic target prediction so the cue could terminate before foveal scrutiny
without an explicit eye-tracker trigger (Automatic Target Prediction and Subtle Gaze Direction, SAP
2015 [VERIFY]) — temporal termination logic that informed this system's transition behaviour.
Depth-based variants extended SGD into stereoscopic VR, validating angular eccentricity from a
reference direction as the operative variable in 3D (Depth-Based Subtle Gaze Guidance in VR, SAP 2015
[VERIFY]).

Grogorick and colleagues systematised the paradigm for immersive displays. In head-mounted 360°
environments, Grogorick, Stengel, Eisemann and Magnor (2017) measured peripheral detection thresholds
that scale linearly with eccentricity (s_static(e) = 0.0098e + 0.1009°; s_dynamic(e) = 0.0076e
+ 0.289°) and showed subtle stimuli cutting hidden-target search from 36.07 s to 8.20 s. Their
five-way comparison in an immersive dome (N = 102; Grogorick, Albuquerque and Magnor, 2018; companion
article Grogorick et al., ACM TAP, 2018) is the most complete map of the subtlety–effectiveness
terrain: local magnification (*ZoomRect*) achieved the best guidance-per-noticeability ratio (~30 %
of target saccades within one second; only 3 of 102 participants recognised it), while *SpatialBlur*
— whole-periphery blur — was the least subtle and actively irritated 59 of 102 participants. Their
sober conclusion, "no method remains completely unnoticed", together with the blur-irritation
finding, directly shaped this project: the original whole-periphery Blur mode was demoted and
ultimately superseded (Chapter 4), and the design accepted noticeability rather than chasing
imperceptibility. Related stimuli complete the palette: stereo inverse brightness modulation exploits
binocular rivalry (Grogorick et al., 2020) — noteworthy here because it can only ever be implemented
on content the application renders itself, never on the OS-owned passthrough layer (Chapter 3); and
flicker exploits the fovea–periphery difference in critical flicker fusion, visible peripherally yet
invisible foveally at 60–72 Hz (Waldin, Waldner and Viola, 2017), though marginal on passthrough
hardware whose camera pipeline runs well below those rates. Recent CHI work revisits flicker as a
practical AR guidance augmentation (Flicker Augmentations, CHI 2024 [VERIFY]), and its explicit
subtle-versus-overt framing is adopted in this chapter's organisation. Gaze guidance by *artificial
colour shifts* — sub-noticeable CMY layer displacements — offers a chromatic-channel alternative
gentler than desaturation (AVI 2018 [VERIFY]), and dichoptic colour cues (different hue per eye)
produce reliable pop-out in optical see-through AR, with Erickson et al.'s parametric analysis of hue
versus saturation versus value informing this project's choice to manipulate saturation and luminance
rather than hue [VERIFY]. Outside the headset entirely, projector-based systems have guided gaze by
changing the apparent visual appearance of real surfaces, using a pixel-shift blur cheaper than
Gaussian filtering (Gaze Navigation by Changing Visual Appearance, VRST 2018 [VERIFY]) — the
real-world analogue of passthrough shader diminishment, and the source of a performance idea (offset
sampling over kernel filtering) echoed in this system's mobile-GPU budget decisions.

Slow ramps constitute the second escape hatch from the noticeability trade-off: Hata, Koike and Sato
(2016) showed that blur introduced gradually enough stays below awareness threshold while still
biasing gaze, because the guidance threshold sits below the awareness threshold. This finding is the
perceptual justification for this system's formation animation — effects ramp in over seconds rather
than switching on (Chapter 4).

### 2.3.2 Saliency modulation of see-through video

Where SGD adds a transient stimulus, saliency *modulation* re-grades the image itself — raising
conspicuity where attention should go and lowering it elsewhere. This is the direct technical
lineage of ColorPop.

The foundational demonstration is Veas, Mendez, Feiner and Schmalstieg (2011): using an Itti-style
conspicuity model, they raised salience in a focus region and suppressed it in context regions of
video imagery (channel order: luminance first, then red–green, then blue–yellow; algorithm in Mendez,
Feiner and Schmalstieg, 2010). Modulated video was statistically indistinguishable from unmodulated
video for naive viewers, yet produced faster first fixations on target regions (t(35) = 2.916,
p < .01) and improved recall of modulated content (0.19 → 0.31, p = .008). This remains the strongest
evidence that video-mediated reality can be re-graded imperceptibly with measurable attentional
consequences — and video mediation is exactly what a passthrough headset is. Kalkofen, Mendez and
Schmalstieg (2007) contribute the focus-and-context vocabulary this dissertation uses throughout:
the design discipline of keeping a focus region maximally legible while context is compressed but
not destroyed.

The modern anchor is Sutton, Langlotz, Plopski, Zollmann, Itoh and Regenbrecht (2022): saliency
modulation of the *real world* viewed through AR glasses, implemented as saturation adjustment plus
sigmoidal contrast remapping (α = 10, β = 0.5), deliberately avoiding hue shifts, which participants
found confusing. Modulation drew significantly more fixations to target regions (χ² = 25.57,
p < .001) but — an honest and important negative — was *not* imperceptible; against an overt circle
cue, modulation's advantage was that it preserved natural scene exploration where explicit markers
"glue" gaze to themselves. This dissertation's ColorPop mode implements Sutton et al.'s exact
sigmoidal contrast recipe on its salient-colour channel (Chapter 4), inheriting the philosophy:
accept visibility, preserve exploration, re-grade rather than annotate. The critical difference is
platform: Sutton et al. built a bench-mounted optical see-through rig whose additive optics can only
add light; the present system runs on consumer video see-through hardware with full subtractive
control of its rendered layers (Section 2.6).

An eye-tracked comparison in cinematic VR closes the loop on which *diminishment* channel works:
comparing area darkening, context-based darkening, and desaturation for steering attention in 360°
video, area darkening guided attention most effectively while desaturation alone had almost no
guidance effect (IEEE, 2021 [VERIFY]). This single result compactly justifies this project's mode
hierarchy: the dark modes are the guidance workhorses, and desaturation appears only as one
ingredient inside ColorPop's compound re-grading, never as a mode of its own.

### 2.3.3 Overt cues, comparisons, and combinations

At the overt end of the spectrum, the Attention Funnel (Biocca, Tang, Owen et al., 2006) — a 3D
tunnel of AR rings leading the head to an out-of-view target — improved search speed by 22 % and
consistency by 65 % while reducing mental workload by 18 %, establishing that aggressive guidance
pays in speed what it costs in scene engagement. Biocca and colleagues' companion analyses of
attention in spatial information systems name the corresponding failure mode, *attention tunnelling*
(Attention Issues in Spatial Information Systems [VERIFY]) — the risk most relevant to this
dissertation's Hard Dark mode, and one of the reasons its evaluation measures peripheral awareness
rather than assuming it. Later cinematic-VR work found that overt guidance can also simply fail:
Nielsen et al. (2016) compared a diegetic firefly cue against forced body rotation in 360° film,
establishing head/gaze direction as the outcome measure for guidance in exactly the medium this
project's video test scene uses. Comparative studies fill in the middle ground: Renner and Pfeiffer
(2017) showed gaze-adaptive peripheral cues reducing search time in narrow-FOV AR, and their
follow-up work in complex AR environments found multi-modal (visual + audio) combinations reducing
search time and errors further [VERIFY]. Marquardt et al. (2020) demonstrated that audio-tactile
guidance can match visual guidance in accuracy while *improving* situational awareness — a finding
this dissertation flags for future work as the natural safety companion to peripheral dimming
(critical events in a dimmed periphery could be announced on the untouched auditory channel). Hein
et al. report that combining a coarse guidance channel with a fine one significantly outperforms
either alone (Two is Better Than One [VERIFY]), which supports this system's compound design —
coarse peripheral attenuation plus fine salience lift inside the focus window. Salience computation
has also been used in the service of *placement* rather than guidance — choosing where AR labels can
sit without occluding what matters (Saliency-Based Label Placement for AR [VERIFY]) — a technique
this dissertation notes as repurposable for saliency-weighted dimming (attenuate hardest where the
periphery is most distracting), as suggested too by Lu et al.'s finding that scene clutter should
modulate cue intensity [VERIFY]. Kruijff et al.'s analysis of label legibility in wide-FOV displays
identifies which peripheral features most disrupt viewing [VERIFY], effectively a priority list for
what peripheral diminishment should suppress first. Finally, an early ISMAR-era study of directing
attention with AR annotations completes the overt-cue baseline this project defines itself against
(Directing Attention Using AR, 2011 [VERIFY]), and deep-learning attention prediction in VR (Li,
2021 [VERIFY]) points to the eventual automation of focus-region selection — this system currently
delegates that choice to the user (trigger-painted windows) or to pre-baked detections, by design
(Chapter 4).

---

## 2.4 Peripheral Vision, Foveated Degradation, and Comfort

### 2.4.1 What the periphery can lose

Peripheral vision is not blurry central vision; it is a differently tuned system with its own
sensitivities (to motion and flicker above all) and its own tolerances. The review literature on
peripheral–foveal interactions (Stewart et al., 2020 [VERIFY]) establishes the parameters that bound
this project: how much degradation the periphery absorbs before scene perception destabilises, and
which manipulations the periphery notices most (contrast loss and motion change) versus least
(chromatic detail loss). Foveated rendering — degrading image quality with eccentricity to save
computation — has mapped this terrain thoroughly. Guenter, Finch, Drucker, Tan and Snyder (2012)
demonstrated multi-layer compositing with progressive peripheral quality reduction; the
state-of-the-art survey (arXiv 2211.07969 [VERIFY]) catalogues the acceptable-degradation
mathematics this project reuses for its focus-to-periphery falloffs; and Tursun et al. (2019) showed
that tolerable degradation is *content-dependent* — a luminance-contrast-aware model permits far more
degradation in low-contrast regions — motivating this dissertation's recommendation (future work)
that diminishment intensity adapt to scene content rather than follow a fixed falloff.

Two perceptual findings sharpen the picture. Patney et al. (2016) established that peripheral blur is
detected primarily through the *contrast reduction* it causes — restoring local contrast after
blurring doubles the tolerable blur radius. Walton et al. (2021) push further with ventral metamers:
peripheral images that are structurally different but perceptually indistinguishable, degrading far
more gracefully than Gaussian blur. Both explain, mechanistically, the irritation finding of
Grogorick et al. (2018): naive whole-periphery blur announces itself through contrast collapse.
This chain of evidence is why the present system's superseded Blur mode was retired and why its dark
modes attenuate luminance (which the periphery tolerates) rather than blurring structure (which it
resents). Padmanaban et al. (2017) supply the complementary optics baseline — how blur and focal
depth are perceived in head-mounted displays at all.

### 2.4.2 Comfort limits of peripheral restriction

A second literature approaches the periphery from the comfort side: field-of-view restriction as a
cybersickness countermeasure. Fernandes and Feiner (2016) showed a soft, dynamically applied FOV
vignette reduces discomfort in VR locomotion without measurable presence loss — the canonical
demonstration that peripheral occlusion is tolerable when it is soft and situationally applied. Lin
et al. (2020) found static peripheral blur likewise reduced sickness at no presence cost. Sustained
use appears benign: Teixeira and Palmisano (2021) report sickness reductions holding over repeated
10-minute exposures with presence unaffected. But the manner of restriction matters greatly. Norouzi,
Bruder and Welch (2018) found that vignetting *coupled to the user's own head motion* — strengthening
as the head turns — actually **increased** sickness relative to no vignette: a directly actionable
negative result that this system's motion policy inverts (effects fade *out* during head motion and
restore during stability; Chapter 4), and the citation that justifies that inversion. Wu and Suma
Rosenberg (2022) showed that masking only the high-optic-flow periphery beats symmetric restriction
on presence — suppress the distracting *signal*, not the region wholesale — and Cao, Grandi and
Kopper (2021) found granulated rest frames (sparse static noise grains, 1–7°, 25–75 % density)
outperform opaque black restrictors on search performance while preserving peripheral awareness,
with 15 of 20 participants preferring grains to blackout. Barhorst-Cates, Rand and Creem-Regehr
(2016) contribute the sobering datum on the far end: spatial learning survives restriction down to
10° FOV, but anxiety is elevated in *all* restricted conditions and rises monotonically as the field
narrows. Waldner et al. (2014) map the comfort envelope for the flicker channel (≈ ¼ luminance range
below 2 Hz achieves ≥ 0.9 detection with low annoyance), parameters this project adopts for its
detection-highlighting pulse rather than anything in the 15–25 Hz photosensitivity band.

Collectively this literature draws the corridor the present system must fly through: peripheral
attenuation is tolerable and even beneficial (Fernandes & Feiner; Lin), *if* it is soft-edged,
decoupled from head motion (Norouzi), ideally selective about what it suppresses (Wu; Cao), and
mindful that total blackout carries a measurable anxiety cost (Barhorst-Cates) and an attention-
tunnelling risk (Biocca). Those constraints are visible in the final system as Soft Dark's
partial-opacity ceiling, every mode's motion-triggered suppression, and the evaluation's insistence
on measuring peripheral awareness (H2b) and comfort (VRSQ, ratings) alongside benefit. One honest
gap remains, inherited rather than resolved: no study to date measures the comfort of peripheral
*desaturation* specifically over sessions longer than ten minutes, and no head-to-head of blur
versus black vignette exists within a single design — this dissertation's Block C sampler collects
first subjective data on adjacent questions but does not close either gap.

---

## 2.5 Attention Guidance in Applied Contexts

### 2.5.1 Driving-adjacent contexts

The densest applied literature on guiding attention under peripheral load is automotive. AR head-up
display research shows that salience-engineered cues — dynamic contour highlighting of hazards —
reduce inattentional blindness and improve reaction times, particularly in adverse weather (Zhu et
al. [VERIFY]). Controlled simulator studies corroborate: highly specific visual AR warnings improve
braking reaction time over generic alerts (AR Warnings in Vehicles, Accident Analysis & Prevention,
2017 [VERIFY]; N = 88, within-subject), and AR cueing of roadside hazards improves response time
*without* degrading detection of non-target objects (Directing Driver Attention with AR Cues,
Transportation Research Part F, 2013 [VERIFY]; N = 27) — the latter being an early empirical
demonstration that guidance benefit and situational-awareness cost are separable, the exact
separation this dissertation's H2a/H2b pair formalises. The DR-specific studies of Murphy et al.
(2021 [VERIFY]) and McLaughlin et al. (2025), reviewed in Section 2.2, translate the same
benefit–risk structure into diminishment terms.

A scoping statement is required here, because this dissertation uses driving *footage* as one of its
stimulus contexts (Chapter 6). Per the claim-scoping rules fixed in the project's framing (Chapter
1), **no claims about driving, drivers, or road safety are made anywhere in this dissertation**. The
automotive literature above motivates the *scenario aesthetics* — a dynamic, high-event-rate
monitoring stimulus with colour-coded semantic anchors (traffic signals) — and supplies the
salience-engineering precedent for ColorPop's red/orange/green hierarchy. A seated participant
passively watching passenger-perspective footage licenses conclusions about attention mechanics
under dynamic stimulation, nothing more.

### 2.5.2 Workstation and desk contexts

The workstation scenario — this dissertation's primary evaluation context — has its clearest
ancestor in FocalSpace (Yao et al., 2013; Section 2.2), which established that suppressing background
distraction in a mediated view improves retention of foreground content. The recent VNC-adjacent
line of mixed-reality workspace studies (Section 2.2.2) brings the same question into headsets with
sustained-attention and working-memory batteries. What was missing until very recently was the
distractor-cost baseline in immersive settings; that has now been supplied — peripheral visual
distractors in a virtual classroom significantly increase both commission errors (1.33 → 3.15,
p < .001) and omission errors (0.14 → 1.18, p < .001) on a Go/No-go continuous performance task
(N = 66, within-subject), while leaving mean reaction time unchanged (Impact of Visual Distractors
in VR on Sustained Attention, Frontiers in Human Neuroscience, 2025 [VERIFY]). That single result
does double duty for this dissertation: it establishes that *errors, not latency*, are the sensitive
currency of distraction cost (fixing the primary dependent variable of Chapter 6), and it quantifies
the effect the vignette must reduce for the primary hypothesis to be supported.

### 2.5.3 Educational contexts

The education literature contributed this project's original third scenario, subsequently folded
into the exploratory mode sampler (Chapter 6) when its measures proved the weakest of the original
design. Its motivating findings stand: student attention drifts in instructional settings, and
gaze-informed interventions can steer it back — demonstrated by gaze-data visualisation systems for
educational VR (Rahman et al., 2019 [VERIFY]) and, methodologically, by gaze-contingent blur applied
to dynamic sports stimuli in VR, whose evaluation apparatus (fixation counts, area-of-interest dwell
under central versus peripheral blur) validated that blur manipulations shift gaze distribution
measurably (Rodrigues et al., 2022 [VERIFY]). The present work retains these as design motivation
and evaluation precedent respectively, while declining to make confirmatory claims in an educational
context it no longer tests.

---

## 2.6 Platform Constraints and Prior Systems on Consumer Passthrough Hardware

### 2.6.1 The constraint landscape

Everything in Sections 2.2–2.3 presumes the ability to manipulate what the user sees of reality. On
consumer hardware in 2026, that ability is narrowly rationed, and the rationing defines this
dissertation's technical contribution (Chapter 3 develops it as a three-tier design space).

On Meta Quest 3, the operating system owns the passthrough layer: applications cannot read its
pixels, and until recently could not modify them at all. Meta's Passthrough Styling API is the
exception that proves the rule — `OVRPassthroughLayer` exposes brightness, contrast and saturation
controls, 3D colour look-up tables, and edge tinting, permitting *global colour remapping* of the
entire passthrough layer, but no blur, no per-pixel spatial masks, and no world-locked regions
(Meta Passthrough Styling documentation). A global desaturation is expressible at this tier; a
peripheral or windowed effect is not. Surface-projected passthrough, which briefly allowed
per-surface styling, was deprecated at SDK v83. The complementary capability arrived in early 2025:
Passthrough Camera Access (PCA) grants applications the raw forward camera feed — full pixel
control, but of a stream markedly inferior to the system passthrough (lower resolution, ~85–90°
FOV against the system layer's ~110°, added latency, and no built-in stereo reprojection). One
published system so far processes this feed live on-device — a native mixed-reality compositing
engine combining PCA with real-time segmentation — and its feasibility data double as a warning:
720p at 30 fps, with thermal throttling within five to ten minutes (arXiv 2509.18929). On Apple
Vision Pro the rationing is stricter still: camera pixels require an enterprise-only entitlement
incompatible with consumer distribution, and the sole diminishment primitive available to consumer
apps is a global, non-spatial surroundings dimming (visionOS documentation). Varjo's XR-3/XR-4
research headsets grant full camera control, yet no published study was found that shader-processes
their passthrough region-selectively for attention purposes — researchers with the capability have
not asked this question, while consumer platforms that provoke the question lack the capability.

### 2.6.2 Prior systems and near-precedents

Every constituent mechanic of this dissertation's architecture has a documented precedent, and none
of the precedents combine them. Camera-feed-on-geometry rendering with GPU effects exists in
community samples — QuestCameraKit demonstrates PCA-fed materials with blur and distortion shaders
applied to virtual panels and objects (xrdevrob/QuestCameraKit) — but always as content *within* the
scene, never as a full-surround treated periphery. A head-centred sphere whose shader-controlled
transparency reveals system passthrough exists as Meta's own "Passthrough Transitioning" motif — but
the sphere carries virtual content for scene fades, not a processed camera feed (Meta MR Motifs
documentation). Alpha punch-through to the passthrough underlay is a documented standard technique
("Passthrough Windows"), shipped commercially by Immersed's Passthrough Portals in 2022 so desk
workers could see their keyboards — but the hole is always surrounded by *virtual* workspace, not by
re-rendered reality. A peripheral dimming vignette shipped in Quest OS v67 as "Theatre View" — but
explicitly does not operate over passthrough, only in immersive contexts. At the claim level, two
patents describe adjacent ideas — attenuating external stimuli while preserving spatial awareness
(US 12548271) and blurred external video feeds composited into VR (US 12524072) — with no evidence
either was implemented.

A structured prior-art search conducted for this dissertation (5 July 2026; 26 web queries and 4
GitHub API queries across ISMAR, CHI, UIST, IEEE VR, DIS and arXiv 2022–2026, Meta developer
documentation, all 136 public forks of the PCA samples repository, XR trade press, and platform API
documentation; the full query protocol is archived in the project record) found **no publication,
product, or open-source project that composites system passthrough with a live shader-processed
camera feed in a single view, for any purpose** — and none that implements peripheral passthrough
manipulation for attention guidance on a standalone consumer headset. The nearest systems are the
five named above, each contributing one ingredient. The hybrid composite itself — native-quality
passthrough revealed through a world-locked window in an effect-processed camera-feed sphere,
exploiting the quality and field-of-view asymmetry between the two layers — appears to be novel, and
Chapter 3 presents it as such, scoped precisely to that composite rather than to "DR on passthrough"
in general.

### 2.6.3 The perceptual ceiling of passthrough

One further platform datum constrains not the system but its *evaluation*: psychophysical
measurement shows that no current video see-through headset reaches normal human acuity at any
light level, with Quest 3 significantly worse in low light (The Perceptual Gap between Video
See-Through Displays and Natural Human Vision, arXiv 2601.02805 [VERIFY]). Any evaluation task that
requires reading fine real-world detail through passthrough therefore confounds the manipulation
under test with the medium's acuity ceiling. This finding drives two decisions in Chapter 6: task
stimuli are either virtual or pilot-verified legible at large print, and room lighting is bright and
constant across sessions.

---

## 2.7 Evaluating Attention Guidance in XR

### 2.7.1 Design and sample-size norms

The evaluation norms of this field are within-subject designs with modest samples and many repeated
measures. Caine's census of CHI 2014 (2016) found the modal sample size across all user studies to
be twelve, with in-person studies smaller than remote ones; the nearest-neighbour DR studies ran
N = 16 and N = 12 (Cheng et al., 2022) and two within-subject experiments analysed with
mixed-effects models (McLaughlin et al., 2025); Sutton et al. (2022) ran N = 20 within-subject with
semi-randomised condition order. The methodological lesson codified in Chapter 6 is that small-N XR
studies derive their power from paired comparisons and trial-level repetition, not headcount — and
that trial-level linear mixed-effects models (McLaughlin's choice) extract more sensitivity from the
same sessions than aggregate t-tests. For non-normal aggregates the HCI norm has been the aligned
rank transform (Wobbrock et al., 2011), though recent critique of ART's error properties (The
Transformation Trap [VERIFY]) motivates this dissertation's preference for mixed models with
Wilcoxon fall-backs, and Friedman tests with Bonferroni-corrected pairwise Wilcoxon comparisons for
Likert batteries (the Cheng pattern).

### 2.7.2 Objective measures without eye tracking

Quest 3 carries no eye tracker, so this dissertation's measures must come from behaviour and head
telemetry. The literature validates both channels. Reaction-time probe detection is standardised as
the Detection Response Task (ISO 17488:2016), with academic roots in Jahn, Oehme, Krems and Gelau
(2005): button-press latency to scheduled probe stimuli indexes spare attentional capacity, with
hits scored in a 100–2500 ms window — adopted wholesale for the driving-scene block of Chapter 6,
using virtual probes at known eccentricities so that onset time, position, hit, miss, and false
alarm are all machine-timed ground truth. Error rates under distraction are the sensitive measure
for sustained-attention tasks (the Frontiers 2025 result of Section 2.5.2: errors moved, latency did
not). Head orientation is a validated proxy for gaze at the eccentricities that matter here: Higgins
et al. (2022) formally validated head pose as a gaze surrogate for object-level attribution in VR,
and Sitzmann et al. (2018) showed with concurrent head and eye tracking in 360° environments that
fixations concentrate at low head velocity — head yaw at rest is a reliable pointer to attention
beyond roughly 15° eccentricity, with eye tracking adding value only inside the central window.
Memory probes for attended versus unattended content (recognition of items placed at target versus
periphery) complete the eye-tracker-free toolkit, with eye-tracked studies of attention distribution
(User Attention in VR Art Encounter, 2022 [VERIFY]) indicating what such proxies trade away.

### 2.7.3 Subjective instruments and safety

The standard subjective battery is stable across this literature. Workload: raw NASA-TLX — the
unweighted variant, which Hart's twenty-year retrospective (2006) endorses and Byers, Bittner and
Hill (1989) showed sacrifices no sensitivity against the pairwise-weighted original (Hart &
Staveland, 1988). Simulator sickness: the SSQ (Kennedy et al., 1993) or, better suited to short
mixed-reality sessions, the nine-item VRSQ (Kim et al., 2018); either must be administered before
*and* after exposure, because post-only scores are uninterpretable without a baseline (SSQ
zero-baseline critique, Frontiers in Virtual Reality, 2022 [VERIFY]). Equivalence claims — this
dissertation's "no meaningful loss of peripheral awareness" hypothesis — require dedicated
machinery: the two-one-sided-tests procedure with a pre-registered margin (Lakens, 2017), which
converts a would-be null result into a falsifiable claim. Rothe, Buschek and Hußmann (2019) provide
the guidance-technique taxonomy used to situate all of the above; no canonical systematic review of
XR attention guidance covering 2020–2025 yet exists, a gap this chapter partially services but does
not claim to close.

---

## 2.8 Synthesis: Research Gaps and Position

### 2.8.1 Gaps

Read together, the eight bodies of literature above leave five identifiable gaps, four of which this
dissertation addresses in whole or part:

| # | Gap | Status in this dissertation |
|---|---|---|
| 1 | **No unified multi-mode DR system.** Guidance techniques and diminishment effects have been studied singly; no prior work combines multiple DR guidance modes in one configurable system for comparison within one design. | Addressed: three modes (ColorPop, Soft Dark, Hard Dark) in one system, spanning the design-space tiers of Chapter 3, compared within one study (Chapter 6). |
| 2 | **No hybrid of system passthrough and app-processed camera feed in one view.** Colour-only global restyling of system passthrough exists (Meta Styling API); full pixel control of an inferior camera feed exists (PCA); no prior work composites the two, and no prior work implements peripheral passthrough manipulation for attention on a standalone consumer HMD (documented search, Section 2.6.2). | Addressed: the window-as-absence hybrid architecture (Chapters 3–4) — the dissertation's strongest and most precisely scoped novelty claim. |
| 3 | **No temporal transition mechanics.** Gradual formation, easing, and motion-contingent suppression of DR effects have not been formally parameterised or studied, despite adjacent evidence that ramps evade noticeability (Hata et al., 2016) and that head-coupled strengthening harms comfort (Norouzi et al., 2018). | Partially addressed: formation ramps and inverted motion-coupling are implemented and parameterised (Chapter 4) and grounded in the cited evidence; their isolated evaluation remains future work. |
| 4 | **No user-controlled DR intensity.** User-initiated, graded release and re-application of diminishment is unstudied. | Not addressed; groundwork only (the trigger-painted window gives users spatial but not intensity control). Future work. |
| 5 | **No dual-channel DR technique.** Desaturation and blur have been studied separately, never combined and parameterised as a compound diminishment channel in AR attention guidance. | Not addressed as a confirmatory question: the compound channel exists inside ColorPop, but the study design deliberately tests the *unconfounded* dark modes confirmatorily and leaves channel decomposition to future work. |

### 2.8.2 Position against the nearest neighbours

Two published studies stand closest to this dissertation, and the delta from each defines its
contribution. **Cheng et al. (2022)** established what users want from diminishment — but inside VR
simulations, because deployable DR did not exist; their study is a perception-and-preference study
of imagined systems. **McLaughlin et al. (2025)** established that distractor attenuation improves
task performance — but likewise inside a virtual task environment, with the diminishment simulated
by the VR scene itself. Both are simulation studies *of necessity*. This dissertation contributes
the piece both were missing: a working diminishment system on real consumer passthrough hardware
(Chapters 3–5), evaluated with a design that deliberately pairs a real-hardware block (the Hard Dark
workstation task, with physical distractors in the participant's actual room) against a
simulation block (the ColorPop driving scene under oracle perception) within the same participants
(Chapter 6). If the two blocks agree in direction, the field learns that the simulation methodology
of Cheng and McLaughlin predicts real-hardware outcomes; if they diverge, the divergence itself is a
finding about what simulation misses. Either way, the evaluation inherits the field's strongest
methodological norms — within-subject design, trial-level mixed models, pre-registered directional
and equivalence hypotheses, and a decision table that admits failure — assembled here, per the
review above, for the first time around a deployable diminished-reality attention system.

---

## References (this chapter)

Entries marked [VERIFY] are cited from the author's PDF collection or secondary summaries and must
be checked against the source before submission.

- Bailey, R., McNamara, A., Sudarsanam, N., & Grimm, C. (2009). Subtle Gaze Direction. *ACM
  Transactions on Graphics*, 28(4), Art. 100. doi:10.1145/1559755.1559757
- Barhorst-Cates, E. M., Rand, K. M., & Creem-Regehr, S. H. (2016). The Effects of Restricted
  Peripheral Field-of-View on Spatial Learning while Navigating. *PLoS ONE*, 11(10), e0163785.
- Barreiros, J., et al. Pre-attentive Features in Natural Augmented Reality Visualizations. [VERIFY]
- Biocca, F., Tang, A., Owen, C., & Xiao, F. (2006). Attention Funnel: Omnidirectional 3D Cursor for
  Mobile Augmented Reality Platforms. *CHI 2006*. https://ieeexplore.ieee.org/document/1579336/
- Biocca, F., et al. Attention Issues in Spatial Information Systems. (1394281.1394289.pdf) [VERIFY]
- Buchner, J., et al. The Impact of Augmented Reality on Cognitive Load and Performance. [VERIFY]
- Byers, J. C., Bittner, A. C., & Hill, S. G. (1989). Traditional and Raw Task Load Index (TLX)
  Correlations: Are Paired Comparisons Necessary? *Advances in Industrial Ergonomics and Safety I*.
- Caine, K. (2016). Local Standards for Sample Size at CHI. *CHI 2016*.
  https://dl.acm.org/doi/10.1145/2858036.2858498
- Cao, R., Grandi, J. G., & Kopper, R. (2021). Granulated Rest Frames as a Technique to Mitigate
  Cybersickness. *Frontiers in Virtual Reality*, 2:604889.
- Cheng, Y., Yin, Y., Yan, Y., Gugenheimer, J., & Lindlbauer, D. (2022). Towards Understanding
  Diminished Reality. *CHI 2022*. doi:10.1145/3491102.3517452
- Depth-Based Subtle Gaze Guidance in VR. *ACM SAP 2015* (2804408.2814187.pdf). [VERIFY]
- Directing Attention Using Augmented Reality. (2037826.2037836.pdf, c. 2011). [VERIFY]
- Duan, H., et al. (2022). Saliency in Augmented Reality (SARD dataset). *ACM Multimedia 2022*.
- Erickson, A., et al. Dichoptic Color Cues in Optical See-Through AR. [VERIFY]
- Exploring Diminished Reality for Attention Support: A Co-Design Study with Students with ADHD.
  *DIS 2026*. https://dl.acm.org/doi/10.1145/3800645.3813095
- Fernandes, A. S., & Feiner, S. K. (2016). Combating VR Sickness through Subtle Dynamic
  Field-of-View Modification. *IEEE 3DUI 2016*, 201–210. doi:10.1109/3DUI.2016.7460053
- Flicker Augmentations: Rapid Brightness Modulation for Real-World Visual Guidance. *CHI 2024*.
  https://dl.acm.org/doi/10.1145/3613904.3642085 [VERIFY authors]
- Foveated Rendering: A State-of-the-Art Survey. arXiv:2211.07969. [VERIFY authors]
- Gaze Guidance using Artificial Colour Shifts. *AVI 2018* (3206505.3206517.pdf). [VERIFY]
- Gaze Navigation by Changing Visual Appearance. *VRST 2018* (3281505.3281537.pdf). [VERIFY]
- Grogorick, S., Stengel, M., Eisemann, E., & Magnor, M. (2017). Subtle Gaze Guidance for Immersive
  Environments. *ACM SAP 2017*. doi:10.1145/3119881.3119890
- Grogorick, S., Albuquerque, G., & Magnor, M. (2018). Comparing Unobtrusive Gaze Guiding Stimuli in
  Head-Mounted Displays. *IEEE ICIP 2018*, 2805–2809. doi:10.1109/ICIP.2018.8451784. Companion:
  *ACM TAP*, 15(4), 2018. doi:10.1145/3238303
- Grogorick, S., et al. (2020). Stereo Inverse Brightness Modulation for Guidance in Dynamic
  Panorama Videos in VR. *Computer Graphics Forum*, 39(2).
- Guenter, B., Finch, M., Drucker, S., Tan, D., & Snyder, J. (2012). Foveated 3D Graphics. *ACM
  Transactions on Graphics*, 31(6).
- Hart, S. G. (2006). NASA-Task Load Index (NASA-TLX); 20 Years Later. *HFES Annual Meeting*.
  https://human-factors.arc.nasa.gov/groups/TLX/downloads/HFES_2006_Paper.pdf
- Hart, S. G., & Staveland, L. E. (1988). Development of NASA-TLX. In *Human Mental Workload*.
- Hata, H., Koike, H., & Sato, Y. (2016). Visual Guidance with Unnoticed Blur Effect. *AVI 2016*,
  28–35. doi:10.1145/2909132.2909254
- Hein, et al. Two is Better Than One: Combining Coarse and Fine Guidance. [VERIFY]
- Higgins, P., et al. (2022). Head Pose as a Proxy for Gaze in Virtual Reality. *VAM-HRI 2022*.
  https://iral.cs.umbc.edu/Pubs/Higgins2022VAM-HRI.pdf
- Impact of Visual Distractors in Virtual Reality on Sustained Attention. *Frontiers in Human
  Neuroscience*, 2025. https://pmc.ncbi.nlm.nih.gov/articles/PMC12698649/ [VERIFY authors]
- ISO 17488:2016. Road Vehicles — Detection-Response Task (DRT) for Assessing Attentional Effects of
  Cognitive Load in Driving.
- Itti, L., Koch, C., & Niebur, E. (1998). A Model of Saliency-Based Visual Attention for Rapid
  Scene Analysis. *IEEE TPAMI*, 20(11).
- Jahn, G., Oehme, A., Krems, J. F., & Gelau, C. (2005). Peripheral Detection as a Workload Measure
  in Driving. *Transportation Research Part F*, 8(3).
- Kalkofen, D., Mendez, E., & Schmalstieg, D. (2007). Interactive Focus and Context Visualization
  for Augmented Reality. *ISMAR 2007*, 191–200. doi:10.1109/ISMAR.2007.4538846
- Kennedy, R. S., Lane, N. E., Berbaum, K. S., & Lilienthal, M. G. (1993). Simulator Sickness
  Questionnaire. *International Journal of Aviation Psychology*, 3(3).
- Kim, H. K., Park, J., Choi, Y., & Choe, M. (2018). Virtual Reality Sickness Questionnaire (VRSQ).
  *Applied Ergonomics*, 69, 66–73.
  https://www.sciencedirect.com/science/article/abs/pii/S000368701730282X
- Kruijff, E., et al. AR Label Design in Wide Field-of-View Displays. [VERIFY]
- Lakens, D. (2017). Equivalence Tests: A Practical Primer. *Social Psychological and Personality
  Science*, 8(4). doi:10.1177/1948550617697177
- Lee, J., & Kim, S. (2025). DiminishAR. *CHI 2025*. doi:10.1145/3706598.3713415 (preprint:
  arXiv:2403.03875)
- Li, et al. (2021). Predicting User Visual Attention in VR with Deep Learning. [VERIFY]
- Lin, Y.-X., et al. (2020). How the Presence and Size of Static Peripheral Blur Affects
  Cybersickness. *ACM TAP*, 17(4). doi:10.1145/3419984
- Lu, W., et al. Subtle Cues for Visual Search in Augmented Reality. [VERIFY]
- Luminance-Contrast-Aware Foveated Rendering. Tursun, O. T., et al. (2019). *ACM Transactions on
  Graphics*, 38(4). doi:10.1145/3306346.3322985
- Marquardt, A., et al. (2020). Comparing Non-Visual and Visual Guidance Methods. *IEEE TVCG*.
  [VERIFY volume/pages]
- McLaughlin, A. C., et al. (2025). Cognitive Aid Design Using Diminished Reality to Support
  Selective Attention by Reducing Distraction. *Human Factors*, 67(9), 937–961.
  doi:10.1177/00187208251325169
- McNamara, A., Bailey, R., & Grimm, C. (2008). Improving Search Task Performance Using Subtle Gaze
  Direction. *APGV 2008*, 51–56.
- Mendez, E., Feiner, S., & Schmalstieg, D. (2010). Focus and Context in Mixed Reality by Modulating
  First Order Salient Features. *Smart Graphics 2010*, LNCS 6133, 232–243.
- Meta Platforms. Passthrough Styling / Passthrough Windows / MR Motifs: Passthrough Transitioning /
  Passthrough Camera Access. Meta Horizon developer documentation.
  https://developers.meta.com/horizon/documentation/unity/
- Mori, S., Ikeda, S., & Saito, H. (2017). A Survey of Diminished Reality. *IPSJ Transactions on
  Computer Vision and Applications*, 9:17. doi:10.1186/s41074-017-0028-1
- Murphy, et al. (2021). Diminishing Reality: Potential Benefits and Risks. *HFES 2021*. [VERIFY]
- Murphy, et al. (2022). Diminished Reality as a Training Scaffold. [VERIFY]
- Native Mixed Reality Compositing on Meta Quest 3. arXiv:2509.18929 (2025).
- Nielsen, L. T., et al. (2016). Missing the Point: An Exploration of How to Guide Users' Attention
  During Cinematic Virtual Reality. *VRST 2016*. doi:10.1145/2993369.2993405
- Norouzi, N., Bruder, G., & Welch, G. (2018). Assessing Vignetting as a Means to Reduce VR Sickness
  During Amplified Head Rotations. *ACM SAP 2018*. doi:10.1145/3225153.3225162
- Obscuring Undesirable Individuals to Alleviate Social Discomfort Using Diminished Reality. *CHI
  2026*. https://dl.acm.org/doi/10.1145/3772318.3790918 [full text to be verified via library]
- Padmanaban, N., Konrad, R., Stramer, T., Cooper, E. A., & Wetzstein, G. (2017). Optimizing VR for
  All Users through Gaze-Contingent and Adaptive Focus Displays. *PNAS*, 114(9).
- Patents US 12548271 (attention control in multi-user environments); US 12524072 (blurred external
  video feed in VR). Claim-level adjacency only. [VERIFY numbers]
- Patney, A., et al. (2016). Towards Foveated Rendering for Gaze-Tracked Virtual Reality. *ACM
  Transactions on Graphics*, 35(6). doi:10.1145/2980179.2980246
- The Perceptual Gap between Video See-Through Displays and Natural Human Vision. arXiv:2601.02805
  (2026). [VERIFY authors]
- Programmable Peripheral Vision. *CHI EA 2022* (3491101.3503821.pdf). [VERIFY]
- Rahman, Y., et al. (2019). Gaze Data Visualizations for Educational VR Applications. *ACM SUI
  2019*. (3357251.3358752.pdf) [VERIFY]
- Renner, P., & Pfeiffer, T. (2017). Attention Guiding Techniques using Peripheral Vision and Eye
  Tracking. *IEEE 3DUI 2017*.
- Renner, P., & Pfeiffer, T. Attention Guiding in Complex AR Environments. [VERIFY]
- Richardson, et al. (2021). Effects of Diminished Reality on Notifications. [VERIFY]
- Rodrigues, et al. (2022). Gaze-Contingent Blur in VR Boxing. *Frontiers in Psychology*, 13:902043.
  [VERIFY]
- Rothe, S., Buschek, D., & Hußmann, H. (2019). Guidance in Cinematic Virtual Reality — Taxonomy,
  Research Status and Challenges. *Multimodal Technologies and Interaction*, 3(1), 19.
- Saliency-Based Label Placement for Augmented Reality. ("Where_to_Place") [VERIFY]
- Sitzmann, V., et al. (2018). Saliency in VR: How Do People Explore Virtual Environments? *IEEE
  TVCG*, 24(4). (arXiv:1612.04335)
- Sridharan, S., Bailey, R., McNamara, A., & Grimm, C. (2012). Subtle Gaze Manipulation for Improved
  Mammography Training. *ETRA 2012*, 75–82. doi:10.1145/2168556.2168568
- SSQ zero-baseline critique. *Frontiers in Virtual Reality*, 2022.
  https://www.frontiersin.org/journals/virtual-reality/articles/10.3389/frvir.2022.945800/full
  [VERIFY authors]
- Stewart, E. E. M., Valsecchi, M., & Schütz, A. C. (2020). A Review of Interactions between
  Peripheral and Foveal Vision. *Journal of Vision*, 20(12):2. [VERIFY]
- Automatic Target Prediction and Subtle Gaze Direction. *ACM SAP 2015* (2804408.2804415.pdf).
  [VERIFY]
- Sutton, J., Langlotz, T., Plopski, A., Zollmann, S., Itoh, Y., & Regenbrecht, H. (2022). Look over
  there! Investigating Saliency Modulation for Visual Guidance with AR Glasses. *UIST 2022*,
  Art. 81. doi:10.1145/3526113.3545633
- Teixeira, J., & Palmisano, S. (2021). Effects of Dynamic Field-of-View Restriction on Cybersickness
  and Presence in HMD-Based Virtual Reality. *Virtual Reality*, 25, 433–445.
- The Transformation Trap (critique of the Aligned Rank Transform). https://statransform.github.io/jovi/
  [VERIFY authors/venue]
- Ueda, et al. IlluminatedFocus: Spatial Defocusing via Tunable Lenses. [VERIFY]
- User Attention in VR Art Encounter. *Multimedia Tools and Applications* (2022).
  (s11042-022-13365-2.pdf) [VERIFY]
- Veas, E., Mendez, E., Feiner, S., & Schmalstieg, D. (2011). Directing Attention and Influencing
  Memory with Visual Saliency Modulation. *CHI 2011*, 1471–1480. doi:10.1145/1978942.1979158
- Visual Noise Cancellation: Exploring Visual Diminishment in AR. ACM (c. 2024). (3634699.pdf)
  [VERIFY]
- Waldin, N., Waldner, M., & Viola, I. (2017). Flicker Observer Effect: Guiding Attention through
  High Frequency Flicker. *Computer Graphics Forum*, 36(2), 467–476. doi:10.1111/cgf.13141
- Waldner, M., Le Muzic, M., Bernhard, M., Purgathofer, W., & Viola, I. (2014). Attractive Flicker —
  Guiding Attention in Dynamic Narrative Visualizations. *IEEE TVCG*, 20(12), 2456–2465.
- Walton, D. R., et al. (2021). Beyond Blur: Real-Time Ventral Metamers for Foveated Rendering. *ACM
  Transactions on Graphics (SIGGRAPH 2021)*. [VERIFY page numbers]
- AR Warnings in Vehicles: Modality and Specificity. *Accident Analysis & Prevention* (2017).
  (1-s2.0-S0001457517300465-main.pdf) [VERIFY authors]
- Directing Driver Attention with Augmented Reality Cues. *Transportation Research Part F* (2013).
  (1-s2.0-S1369847812000782-main.pdf) [VERIFY authors]
- Wobbrock, J. O., Findlater, L., Gergle, D., & Higgins, J. J. (2011). The Aligned Rank Transform.
  *CHI 2011*.
- Wu, F., & Suma Rosenberg, E. (2022). Asymmetric FOV Restriction Using Optic Flow. *VRST 2022*.
  doi:10.1145/3562939.3565611
- xrdevrob. QuestCameraKit. https://github.com/xrdevrob/QuestCameraKit
- Yantaç, A. E., et al. Exploring Diminished Reality Spaces for Attention in ASD. [VERIFY]
- Yao, L., DeVincenzi, A., Pereira, A., & Ishii, H. (2013). FocalSpace: Multimodal Activity Tracking,
  Synthetic Blur and Adaptive Presentation for Video Conferencing. *ACM SUI 2013*.
- Zhu, et al. Visual Saliency Design for AR-HUD Navigation. [VERIFY]
