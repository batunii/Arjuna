# Chapter 3 — A Design Space for Diminished Reality under Compositing Constraints

## 3.1 Introduction

Chapter 2 established that diminished reality (DR) — the selective removal or attenuation of
real-world visual information — has a substantial conceptual literature but a thin implementation
record on consumer hardware. This chapter addresses the technical half of the dissertation's spine
question: *what degree of diminishment control is actually achievable on a consumer video-see-through
(VST) headset, without operating-system-level access to the passthrough layer?*

The answer is not a single number but a structured design space. The central claim of this chapter is
that on a platform such as the Meta Quest 3, the feasibility of any DR effect is determined almost
entirely by **which layer of the compositing stack an application is permitted to touch**, and that
these permissions stratify into three discrete tiers with sharply different capabilities and costs.
The three vignette modes evaluated in this dissertation — ColorPop, Soft Dark, and Hard Dark — were
not designed as arbitrary aesthetic variants; each occupies a distinct position in this space, and
together they span it. The taxonomy presented here is Contribution C1; the modes that instantiate it are described in implementation detail in Chapter 4.

The chapter proceeds as follows. Section 3.2 states the compositing constraint precisely.
Section 3.3 develops the three-tier access taxonomy, the chapter's centrepiece. Section 3.4 makes
explicit what is *impossible*, and why the absence itself is a finding. Section 3.5 derives three
design axes from the taxonomy and locates the three modes on them. Section 3.6 states the novelty
position against prior art, and Section 3.7 states — before an examiner needs to ask — what does and
does not transfer to optical see-through (OST) hardware.

## 3.2 The Compositing Constraint

On Quest 3, the reconstructed view of the real world that the user experiences ("system passthrough")
is produced and owned by the operating system. An application **cannot read, modify, or subtract from
the passthrough layer; it can only composite content on top of it**. The passthrough image is never
exposed to application shaders as a texture: it is composited *behind* application content by the OS
compositor, at a stage of the pipeline that applications do not reach. Consequently, the naive
formulation of subtractive DR — "sample the passthrough pixel, darken it, write it back" — is not
merely difficult on this platform; it is architecturally impossible.

This is not an incidental limitation but a deliberate privacy and safety boundary, and it is shared
in stronger form by the other major consumer platform: on Apple Vision Pro, camera pixels are
unavailable to consumer applications entirely (raw camera access requires an enterprise-only
entitlement incompatible with App Store distribution), and the only world-appearance control offered
is a global, non-spatial surroundings dimming effect (Apple, 2024). On true optical see-through
glasses the constraint is harsher still and physical rather than administrative: an additive display
can only *add* light to the optical path. It can superimpose brightness but cannot remove photons
arriving from the world; darkening is physically unavailable without auxiliary hardware such as
per-pixel dimming layers (Itoh et al., 2021 [VERIFY]).

It is worth being clear about *why* the boundary exists, because its motivation predicts its
persistence. The passthrough image is a continuous camera view of the user's home, workplace, and bystanders; granting arbitrary applications read access to it is a privacy exposure, and granting
write access is a safety exposure (an application could imperceptibly alter the user's view of the
physical world they are walking through). Both concerns strengthen rather than weaken over time, so research that assumes future read–modify–write access to system passthrough is betting against the platform vendors' incentives. This dissertation makes the opposite bet: it treats the constraint as permanent and asks what can be built *inside* it — which is what gives the resulting design space its claim to durability.

Subtractive attention guidance — dimming, muting, or blacking out the visual periphery — must
therefore be *synthesized* on consumer VST hardware through whatever compositing rights the platform
does grant. Characterising those rights, and the DR capability each one supports, is the purpose of
the taxonomy that follows.

[Figure 3.1 — Diagram of the Quest 3 compositing stack: OS passthrough layer at the back
(inaccessible), application render layer(s) in front, compositor combining them. Annotate the three
access tiers with arrows showing what each tier can touch.]

## 3.3 The Three-Tier Access Taxonomy

The Quest 3 grants applications three distinct kinds of access relevant to DR, summarised in
Table 3.1 and elaborated below.

**Table 3.1 — Diminished-reality capability by compositing-access tier on Quest 3.**

| Tier | Access mechanism | Achievable DR | Ceiling / cost | Modes located here |
|---|---|---|---|---|
| **1 — OS passthrough styling** | `OVRPassthroughLayer` Styling API: brightness / contrast / saturation scalars, 3D colour look-up tables, edge tint | **Global colour remapping only.** Whole-layer desaturation, tinting, posterisation | No blur; no per-pixel or spatial masks; no world-locked regions; layer can never be read. Surface-projected passthrough (per-surface styling) deprecated at SDK v83 | — (bounding case) |
| **2 — Overlay compositing** | Application draws alpha-blended geometry in front of the passthrough layer | Dimming, blackout, occlusion; a shaped *absence* of overlay passes OS-native passthrough through untouched | Can only attenuate or occlude reality — cannot recolour, blur, or filter it | Soft Dark, Hard Dark; the focus window itself |
| **3 — Camera re-render (PCA)** | Passthrough Camera Access: the raw forward camera feed as a GPU texture, with intrinsics and pose | Arbitrary per-pixel manipulation: blur, desaturation, salience re-grading, glare compression | Mono 1280×960 at ~85–90° horizontal FOV vs the ~110° OS view; added latency; binocular-fusion burden (Chapter 4); sustained on-device processing is thermally bounded (Section 3.3.3) | ColorPop, Blur (superseded) |

### 3.3.1 Tier 1 — the styling ceiling

Tier 1 deserves precision because it prevents an overclaim this dissertation must not make. It is
*not* true that "nothing can be done" to the system passthrough layer: Meta's Passthrough Styling API
allows an application to apply brightness, contrast and saturation adjustments, full 3D colour
look-up tables, and edge tinting to the passthrough layer as a whole (Meta, 2024a). A global
desaturation of the entire visual field — one conceivable DR primitive — is therefore achievable at
Tier 1 without touching a camera pixel.

What Tier 1 categorically lacks is **spatial control**. Every styling operation applies to the whole
layer uniformly; there is no mechanism for per-pixel masks, gradients, world-locked regions, or any
effect that treats one part of the visual field differently from another. The one historical
exception — surface-projected passthrough, which allowed passthrough to be projected onto
application-defined geometry with per-surface styling — was deprecated at SDK v83 and is unavailable
going forward. Since attention guidance is *definitionally* spatial — its entire purpose is to treat
the focus region differently from the periphery — Tier 1 alone cannot implement any mode in this
dissertation. **The absence of spatial control, not the absence of control, is the finding.**

### 3.3.2 Tier 2 — diminishment by occlusion

Tier 2 is ordinary alpha compositing: the application draws translucent or opaque geometry in front
of the passthrough layer. Because the VST display is an opaque screen whose every pixel the
compositor ultimately controls, an application overlay can darken the apparent world without limit —
up to and including full blackout — something physically impossible on additive OST optics. The
constraint runs the other way: a Tier-2 overlay knows nothing about the pixels behind it. It can
attenuate or occlude reality but cannot recolour, sharpen, blur, or respond to its content.

Two of this dissertation's modes live entirely at Tier 2. **Soft Dark** composites a uniform black
overlay at up to 75 % opacity across the periphery; **Hard Dark** takes the same overlay to full
opacity. Both leave a shaped hole — the world-locked focus window — through which the untouched OS
passthrough remains visible.

That hole is the load-bearing architectural insight of the whole system, and it is worth stating in
its general form: **at Tier 2, the highest-fidelity rendering of reality is an absence of
rendering.** Inside the focus window nothing is drawn at all (overlay alpha = 0), so the user
receives OS passthrough at its full native quality, full ~110° field of view, and zero added
latency — precisely where the user is being asked to direct their attention. The manipulation is
confined to the periphery, where visual acuity is lowest and quality loss matters least. This
inverts the naive design (re-render the whole view from the camera feed and accept camera-grade
quality everywhere), and it is why the system escapes the passthrough-acuity ceiling exactly where
task performance depends on seeing well. Alpha punch-through as a mechanism is documented platform
practice ("Passthrough Windows"; Meta, 2024b) and shipped commercially in Immersed's Passthrough
Portals (Immersed, 2022) — but in all found prior uses the hole reveals passthrough *surrounded by
virtual content*. Using the hole as the *focus region of a diminished periphery* is, per the prior-art
search of Section 3.6, unprecedented.

### 3.3.3 Tier 3 — full pixel control at a price

Tier 3 is the Passthrough Camera Access (PCA) API (Meta, 2025), which since early 2025 exposes the
headset's forward RGB camera feed to applications as a GPU texture together with camera intrinsics
and pose. At Tier 3 the application is no longer manipulating a proxy for reality; it holds the
camera pixels themselves and can apply arbitrary per-pixel computation: Gaussian blur, luminance-
weighted desaturation, hue-selective salience re-grading, glare compression — everything the ColorPop mode does (Chapter 4).

The price is paid in four currencies:

1. **Resolution and field of view.** The accessible feed is a single mono stream at 1280×960
   covering roughly 85–90° horizontally, against the OS passthrough's ~110° stereo view. A Tier-3
   re-render is *categorically* lower-fidelity than what the user would otherwise see.
2. **Latency.** The application-side pipeline (camera capture → texture upload → shader → display)
   adds delay that the OS passthrough path, with its dedicated reprojection, does not exhibit.
3. **Binocular fusion.** The feed is monocular; presenting it convincingly to two eyes is
   non-trivial, and the naive approach measurably fails (the double-vision failure and its
   world-direction sampling solution are detailed in Chapter 4).
4. **Thermal budget.** Sustained on-device processing of the camera stream is bounded: the only
   published system that renders and processes the PCA feed as a live view reports 720p30 operation
   with thermal throttling after 5–10 minutes (Bajireanu et al., 2025 [VERIFY author list]). Tier-3
   modes are, on today's hardware, modes for *sessions*, not workdays.

### 3.3.4 The hybrid: composing Tier 2 and Tier 3

The tiers are not mutually exclusive, and the most capable configuration this work demonstrates is a
composite: the PCA camera feed, shader-processed, rendered onto a head-centred sphere as the
periphery (Tier 3), with the world-locked focus window left transparent so that native OS passthrough
shows through the hole (Tier 2's absence trick). The user experiences full-fidelity reality inside
the window, seamlessly surrounded by a *re-graded interpretation* of reality outside it — exploiting
the quality/FOV asymmetry between the two layers rather than suffering it. The prior-art search
(Section 3.6) found no publication, product, or open-source project that composites system
passthrough with a live shader-processed camera feed in one view, for any purpose. The implementation
is presented in Chapter 4; its planned quantitative characterisation (legibility across the two
paths, latency, thermal envelope) is the subject of Chapter 5.

[Figure 3.2 — The hybrid composite, exploded-layer diagram: OS passthrough at the back; the
effect-processed camera-feed sphere in front with the focus window cut out; the fused view the user
sees.]

## 3.4 What Is Impossible, and Why It Matters

A design space is defined as much by its walls as by its rooms. Three impossibilities structure
everything above:

- **No read access, at any tier, to the pixels the user actually sees.** Even Tier 3 supplies a
  *different, lower-grade copy* of reality, not the passthrough image itself. Any effect that must
  respond to what the user sees (content-adaptive dimming, saliency-weighted attenuation as proposed
  by Lu et al. [VERIFY]) can respond only to the camera proxy.
- **No spatial modulation of the native layer.** The full-quality view can be revealed or hidden
  (Tier 2) but never *partially processed*. A "blur the real passthrough periphery" mode — arguably
  the perceptually gentlest DR primitive, per the foveated-rendering literature (Patney et al.,
  2016) — cannot exist on this platform except via the Tier-3 copy.
- **No subtraction on OST.** The entire Tier-2 family is VST-specific (Section 3.7).

These walls explain the system's shape. The dark modes exist because attenuation-by-overlay is the
only manipulation available at native quality; ColorPop exists because re-grading requires pixels and
pixels are only available as the Tier-3 copy; the window-as-absence exists because it is the sole
mechanism that delivers native fidelity *and* spatial selectivity simultaneously. Had the platform
offered read–modify–write access to the passthrough layer, none of this architecture would be
necessary — and that counterfactual is precisely what makes the taxonomy transferable: any future
platform can be located in it by asking which of the three walls it removes.

## 3.5 Design Axes and the Placement of the Modes

Three trade-off axes fall out of the taxonomy, and the three evaluated modes were chosen to span
them.

**Fidelity versus control.** Tier 2 offers native fidelity with a single degree of freedom
(attenuation); Tier 3 offers unlimited control at camera-copy fidelity. Hard Dark and Soft Dark sit
at the fidelity pole (the periphery they show — when they show it — is real passthrough, merely
darkened); ColorPop sits at the control pole (its periphery is a synthetic re-grading of the camera
copy).

**Suppression versus awareness.** Hard Dark removes the peripheral signal entirely: maximal
distractor suppression, zero residual awareness. Soft Dark attenuates to ~75 % opacity: peripheral
events remain detectable through the veil. ColorPop suppresses *selectively*: task-relevant colour
classes (red/orange/green — signal lamps, signage) remain visible everywhere, while everything else
is muted. The user-study safety hypothesis H2b (Chapter 6) lives on exactly this axis.

**Deployability versus capability.** Soft Dark and Hard Dark run without the camera pipeline at all
(their shader path samples no texture), draw negligible GPU power, require no camera permission, and
face no thermal ceiling — they are deployable for hour-long sessions today. ColorPop inherits
Tier 3's full cost structure.

Within this frame, **Soft Dark's position deserves explicit defence**, because superficially it can
appear redundant once ColorPop also dims its periphery (`_PopPeriphDim`; Chapter 4). It is retained
as the design space's load-bearing middle point on four grounds. First, it is the **minimal
manipulation**: pure luminance attenuation and nothing else — the only unconfounded member of the
mode family, where ColorPop bundles desaturation, salience re-grading, glare compression and dimming
into a single composite whose ingredients cannot be causally separated. Second, it is the graded
**Tier-2 point**: what it shows is attenuated *reality* at native quality, not a re-rendered copy —
a categorically different object with categorically different failure modes. Third, it is the only
mode **plausibly deployable for extended sessions** on present hardware, given the Tier-3 thermal
envelope. Fourth, it is the **awareness-preserving midpoint** on the suppression axis between
unmodified vision and Hard Dark's blackout — the point at which the focus-versus-awareness trade-off
documented in the DR literature (Murphy et al., 2021 [VERIFY]; McLaughlin et al., 2025) is actually
negotiable rather than decided.

Together, off → Soft Dark → Hard Dark forms a clean single-variable intensity ladder (overlay alpha
0 → 0.75 → 1.0), while ColorPop extends the space along the orthogonal control axis. This is the
sense in which three modes "span" the space: two axes, both covered, with the intensity axis sampled
at three points.

A fourth, orthogonal dimension deserves naming even though this dissertation treats it as design
rather than as an experimental factor: **time**. Every mode has temporal behaviour — a formation
transition when the window locks, a fade-out when the head moves fast, a fade-in when it settles
(Chapter 4) — and the literature review found these transition mechanics to be essentially unstudied
in DR: prior work evaluates diminishment as a static state, not as something that arrives, yields,
and returns. The system therefore exposes every temporal parameter as tunable, and the user study's
exploratory interview asks specifically about the motion-fade experience; a controlled study of DR
transition dynamics is identified in Chapter 7 as future work seeded by this design space rather than
resolved by it.

[Figure 3.3 — Two-axis plot (fidelity/control × suppression/awareness) with the three modes placed;
deployability annotated as marker size or colour.]

## 3.6 Novelty Position

The claims of this chapter are calibrated against a structured prior-art search conducted on
2026-07-05 across ISMAR, CHI, UIST, IEEE VR, DIS and arXiv (2022–2026), Meta developer documentation,
GitHub (including all 136 forks of the upstream PCA samples repository), XR industry press, and the
visionOS and Varjo API surfaces; the full query protocol is archived and reproduced in Appendix
[REF]. Three verdicts matter here:

1. **The hybrid composite** (system passthrough revealed through a window in a live shader-processed
   camera-feed sphere): *nothing found* — no paper, product, or open-source project, for any purpose.
2. **Peripheral passthrough manipulation for attention guidance on a standalone consumer HMD**:
   *nothing found as an implemented system*. The nearest work is concept-level or differently
   situated: McLaughlin et al. (2025) evaluate DR distraction-attenuation entirely inside VR
   simulation; the DIS 2026 ADHD co-design study is formative, with no prototype; DiminishAR and the
   CHI 2026 Vision Pro study target *specific objects/persons* with occlusion rather than the
   periphery; Sutton et al. (2022) modulate real-world saliency but on a bench-mounted optical
   see-through rig that can only add light.
3. **A dark peripheral vignette over passthrough**: *near precedent, no direct precedent*. Quest OS
   v67's "Theatre View" ships an adjustable peripheral dimming vignette — but explicitly not over
   passthrough (immersive mode only); Vision Pro's surroundings dimming is global, not windowed.

Each constituent mechanic, taken alone, has documented precedent that this dissertation cites rather
than claims: camera-feed geometry with GPU effects (QuestCameraKit; xrdevrob, 2025), a head-centred
fade sphere revealing passthrough (Meta MR Motifs "Passthrough Transitioning"; Meta, 2024c), alpha
punch-through windows (Meta, 2024b; Immersed, 2022), OS peripheral dimming in immersive mode (Meta,
2024d), and saliency modulation of reality on OST hardware (Sutton et al., 2022). The defensible
novelty claim is therefore precisely bounded: **the composite** — native passthrough through a
world-locked focus window in an effect-processed camera-feed periphery, exploiting the quality/FOV
asymmetry between layers — **applied to peripheral attention guidance on a consumer standalone
headset**, together with the world-direction fusion sampling that makes the mono feed binocularly
viewable (Chapter 4). An alpha-blended dark overlay, by itself, is just compositing, and is not
claimed as novel.

Two adjacent patents (US 12548271; US 12524072) describe claim-level ideas in this territory —
attenuating external stimuli while preserving spatial awareness, and blurred external video feeds
composited into VR — with no evidence of implementation in either case; they are noted for
completeness.

## 3.7 What Transfers to Optical See-Through — and What Does Not

This section makes the non-transfer statement before an examiner makes it. The Tier-2 dark overlay
family — Soft Dark, Hard Dark, and the darkness component of every mode — works because a VST display
is an opaque screen over which the compositor has total authority. **It does not transfer to optical
see-through glasses**, whose additive optics cannot remove light from the world; on OST, "draw black"
means "draw nothing". A hypothetical OST implementation of peripheral dimming would require optical
hardware assistance (global or segmented dimming layers, per-pixel occlusion masks), which is exactly
Tier-1-style capability relocated into the optics.

What does transfer is, first, the **taxonomy itself**: OST platforms can be located in it (their
"Tier 2" is empty for subtraction; their achievable DR collapses onto additive saliency modulation of
the Sutton et al. kind, plus whatever dimming hardware exists); and second, the **human-factors
findings** of Part H — what peripheral diminishment does to attention is a property of the human
visual system, not of the display that implements the diminishment. The engineering is
platform-bound; the psychology is not. That division of transferable from non-transferable is itself
a use of the design space, and it is the frame in which Chapter 7 discusses generalisation.

## 3.8 Summary

On consumer VST hardware, diminished reality is not one problem but three, stratified by compositing
access: a colour-only, spatially-blind styling tier; an attenuation-and-occlusion overlay tier whose
most powerful move is a shaped absence; and a full-control camera-copy tier that pays in fidelity,
latency, fusion and heat. The three evaluated modes span the space this stratification creates, the
hybrid composite exploits the asymmetry between its tiers, and the walls of the space — no reads, no
spatial modulation of the native layer, no subtraction on OST — are findings in their own right.
Chapter 4 descends from this map into the machine.

---

## References (this chapter)

- Apple (2024). *Accessing the main camera — visionOS enterprise APIs.* developer.apple.com/documentation/visionOS/accessing-the-main-camera
- Bajireanu, R., et al. (2025). *Native Mixed Reality Compositing on Meta Quest 3.* arXiv:2509.18929. [VERIFY author list]
- Cheng, Y., Yin, Y., Yan, Y., Gugenheimer, J., & Lindlbauer, D. (2022). Towards Understanding Diminished Reality. *Proc. CHI 2022.* doi:10.1145/3491102.3517452
- Immersed (2022). *Passthrough Portals.* uploadvr.com/immersed-adds-passthrough-portals-tracked-keyboards/
- Itoh, Y., et al. (2021). Optical see-through head-mounted displays with occlusion capability. [VERIFY — occlusion-capable OST survey]
- Lu, W., et al. Subtle cues for visual search in AR. [VERIFY — full citation from author's paper collection]
- McLaughlin, A. C., et al. (2025). Cognitive Aid Design Using Diminished Reality to Support Selective Attention by Reducing Distraction. *Human Factors.* doi:10.1177/00187208251325169
- Meta (2024a). *Passthrough Styling (colour mapping / LUTs).* developers.meta.com/horizon/documentation/unity/unity-customize-passthrough-color-mapping/
- Meta (2024b). *Passthrough Windows (punch-through).* developers.meta.com/horizon/documentation/unity/unity-customize-passthrough-passthrough-windows/
- Meta (2024c). *MR Motifs: Passthrough Transitioning.* developers.meta.com/horizon/documentation/unity/unity-mrmotifs-passthrough-transitioning/
- Meta (2024d). Quest software update v67: Theatre View. [VERIFY exact release note]
- Meta (2025). *Passthrough Camera Access (PCA).* developers.meta.com/horizon/ (Unity PCA documentation)
- Mori, S., Ikeda, S., & Saito, H. (2017). A survey of diminished reality. *IPSJ Trans. Computer Vision and Applications*, 9(17). doi:10.1186/s41074-017-0028-1
- Murphy, D., et al. (2021). Diminishing Reality: Potential Benefits and Risks. *Proc. HFES 2021.* [VERIFY]
- Patney, A., et al. (2016). Towards foveated rendering for gaze-tracked virtual reality. *ACM TOG*, 35(6).
- Sutton, J., Langlotz, T., Plopski, A., Zollmann, S., Itoh, Y., & Regenbrecht, H. (2022). Look over there! Investigating saliency modulation for visual guidance with augmented reality glasses. *Proc. UIST 2022.* doi:10.1145/3526113.3545633
- US Patent 12548271: Attention control in multi-user environments. US Patent 12524072: External video feed rendered with blur in VR. [Claim-level adjacency only]
- xrdevrob (2025). *QuestCameraKit.* github.com/xrdevrob/QuestCameraKit
