# Literature Review Source — 48 Papers in 8 Thematic Sections

> Source material provided by the author (2026-07-05). Covers 48 research papers organised into 8
> thematic sections, ordered by relevance to the dissertation: *Guiding User Attention in Real-World
> Tasks Using XR Overlays* on Meta Quest (Passthrough AR). PDF filenames in parentheses refer to the
> author's local paper collection. ⭐ marks papers the author flagged as particularly actionable.
>
> NOTE (added during synthesis): section themes reference the older Dynamic/Semi-Dynamic/Static mode
> names and the classroom scenario; the Related Work chapter maps these onto the current system
> (ColorPop / Soft Dark / Hard Dark; classroom folded into the Block C sampler). Research Gap 2's
> wording is refined by the 2026-07-05 prior-art search (`framing.md` §6.1): apps CAN colour-remap
> system passthrough via Meta's Styling API, so the precise novelty is the hybrid
> system-passthrough + processed-camera-feed composite, not "DR on passthrough" in general.

---

## 1. Core Concept & Theory — Diminished Reality (DR) & Visual Noise Cancellation (VNC)

These papers define the theoretical foundation: using AR not to **add** information, but to **remove
or mute** distractions to enhance focus.

- **Visual Noise Cancellation** *(3634699.pdf)* — Introduces VNC, analogous to acoustic noise
  cancellation, using head-worn AR to moderate the visual environment. Closest conceptual match to
  the entire project. The three modes can be positioned as different VNC profiles.
- **Towards Understanding Diminished Reality** *(3491102.3517452.pdf)* — Defines DR as the process of
  concealing real-world objects and visual clutter to support focus. Core theoretical foundation.
- **A Survey of Diminished Reality** *(s41074-017-0028-1.pdf)* — Comprehensive survey of DR
  techniques and applications. Justifies using AR to *remove* rather than add information.
- **Programmable Peripheral Vision** *(3491101.3503821.pdf)* — Hardware and conceptual approaches to
  augmenting peripheral visual perception. Validates the physical desire for what the project
  achieves computationally via shaders.
- **IlluminatedFocus** *(Ueda et al.)* — Spatial defocusing via tunable lenses to reshape human
  visual perception. Closely aligned with the Meta Quest passthrough approach.

> **Key Takeaway:** DR and VNC provide the theoretical backbone. The project can be uniquely framed
> as a *software-defined VNC system* running on video-passthrough MR hardware — a gap no prior work
> has filled.

## 2. Saliency Modulation & Subtle Gaze Guidance

- **Look over there! Investigating Saliency Modulation** *(3526113.3545633.pdf)* — Modulating
  saliency of real-world objects via brightness/contrast adjustments in AR. Directly justifies the
  desaturation shader logic.
- **Directing Attention with Visual Saliency Modulation** *(1978942.1979158.pdf)* — Mathematical
  framing for saliency-based attention guidance. Helps define shader falloff parameters.
- **Flicker Augmentations** *(3613904.3642085.pdf)* — Rapid brightness modulation for gaze guidance.
  Alternative/complementary technique; "subtle vs. overt" framing useful for positioning.
- **Automatic Target Prediction & Subtle Gaze Direction** *(2804408.2804415.pdf)* ⭐ — SGD technique
  that terminates before foveal scrutiny. Temporal termination logic could refine mode transitions.
- **Depth-Based Subtle Gaze Guidance in VR** *(2804408.2814187.pdf)* ⭐ — Extends gaze guidance to 3D
  using depth data. Could improve spatial accuracy of focus regions.
- **Gaze Guidance using Artificial Color Shifts** *(3206505.3206517.pdf)* ⭐ — CMY colour layer
  shifts as a subtler alternative to desaturation. Possible lighter mode variant.
- **Gaze Navigation by Changing Visual Appearance** *(3281505.3281537.pdf)* ⭐ — Projector-based
  pixel-shift blur as the real-world analogue of passthrough shader DR; pixel-shift is more
  performance-friendly than Gaussian blur.
- **Dichoptic Color Cues in OST-AR** *(Erickson et al.)* ⭐ — Hue-based dichoptic cues are effective
  pop-out attention cues; hue vs. saturation vs. value analysis informs shader parameters.

> **Key Takeaway:** Desaturation is a validated saliency reduction technique. Parameters must be
> calibrated between *"just noticeable"* and *"too obtrusive"*. Combining desaturation + blur has
> never been formally studied in AR — Research Gap 5.

## 3. Technical Implementation — Foveated Rendering & Focus+Context Visualisation

- **Foveated Rendering: State-of-the-Art Survey** *(2211.07969v1.pdf)* — Foveated rendering math and
  GPU performance techniques; peripheral desaturation/blur can reuse these principles.
- **Beyond Blur: Ventral Metamers for Foveated Rendering** *(SIGGRAPH2021.pdf)* — More natural
  peripheral degradation than Gaussian blur, preserving visual structure.
- **Foveated 3D Graphics** *(foveated_final15.pdf)* — Multi-layer compositing with progressive
  peripheral quality reduction; framework for smooth focus-to-periphery transitions.
- **Luminance-Contrast-Aware Foveated Rendering** *(3306346.3322985.pdf)* — Content-adaptive quality
  degradation; justifies adapting desaturation/blur intensity to scene content.
- **Comprehensible Visualisation & Interactive F+C in AR** *(Kalkofen et al.)* — Focus and Context
  design principles for AR; UX vocabulary for natural-feeling focus regions.

> **Key Takeaway:** Use Kawase blur (cheaper than Gaussian on mobile GPUs) and luminance-weighted
> greyscale conversion for desaturation. Content-adaptive intensity preferable to fixed falloffs.

## 4. AR Attention Guiding Techniques & Comparisons

- **Two is Better Than One** *(Hein et al.)* ⭐ — Combining coarse + fine guidance significantly
  outperforms either alone. Supports layering desaturation (coarse) with boundary highlights (fine).
- **Attention Guiding via Peripheral Vision & Eye Tracking** *(Renner & Pfeiffer)* ⭐ —
  Eye-tracking-adaptive guidance for narrow-FOV AR.
- **Comparing Non-Visual and Visual Guidance in AR** *(Marquardt et al.)* ⭐ — Audio-tactile guidance
  matched visual guidance in accuracy and improved situational awareness; suggests subtle audio cues
  for critical events in the dimmed periphery.
- **Attention Guiding in Complex AR Environments** *(Renner & Pfeiffer)* — Multi-modal cues reduce
  search time and errors.
- **Pre-attentive Features in Natural AR Visualisations** *(Barreiros et al.)* ⭐ — DR effects must
  work at the pre-attentive level to feel automatic and unobtrusive.
- **Saliency-Based Label Placement for AR** *(Where_to_Place)* ⭐ — Saliency computation could
  identify the most distracting peripheral regions for saliency-weighted dimming.

> **Key Takeaway:** No single attention-guiding technique is optimal alone. Combined
> desaturation + boundary-highlight is well-supported.

## 5. Dynamic Mode — Driving

- **Visual Saliency Design for AR-HUD Navigation** *(Zhu et al.)* — Dynamic/contour saliency in
  AR-HUDs reduces inattentional blindness and improves reaction times in adverse weather.
- **AR Warnings in Vehicles: Modality & Specificity** *(1-s2.0-S0001457517300465-main.pdf)* — Highly
  specific visual AR warnings improve braking reaction time.
- **Directing Driver Attention with AR Cues** *(1-s2.0-S1369847812000782-main.pdf)* — AR cues improve
  driver response time without harming detection of non-target objects.
- **Diminishing Reality: Potential Benefits & Risks** *(murph-et-al-2021)* — DR lowers cognitive
  workload but risks situational awareness loss. Critical for balancing focus vs. safety.
- **Cognitive Aid Design Using DR** *(mclaughlin-et-al-2025)* — Framework to test whether peripheral
  manipulation causes dangerous situational blindness.

> **Key Takeaway:** AR can safely guide driver attention, but peripheral DR must be carefully tuned
> to avoid inattentional blindness. Multi-modal cues recommended as a safety fallback.

## 6. Static Mode — Workstation

- **FocalSpace: Multimodal Activity Tracking for Video Conferencing** *(2491367.2491377.pdf &
  823939522-MIT.pdf)* — Background blur for video conferencing based on depth/activity tracking; the
  2D webcam analogue of what the project builds in 3D AR.
- **Exploring DR Spaces for Attention in ASD** *(Yantaç et al.)* — Interactive DR walls to filter
  irrelevant visual information for individuals with ASD; validates psychological benefits of
  environment filtering.

> **Key Takeaway:** Synthetic background blur and DR-enforced focus zones have validated benefits for
> sustained attention tasks, including for neurodivergent users.

## 7. Semi-Dynamic Mode — Classroom

- **Gaze Data Visualisations for Educational VR** *(3357251.3358752.pdf)* — Visualising student gaze
  to guide drifting attention back to the teacher in educational VR.
- **VR Boxing: Gaze-Contingent Blur** *(fpsyg-13-902043.pdf)* — Gaze-contingent blur applied to
  dynamic VR stimuli; methodology relevant to evaluation design.

> **Key Takeaway:** Attention drifts in educational settings. Gaze-contingent blur methodology
> provides an evaluation framework.

## 8. Perception, Eye Tracking & Human Factors

- **Peripheral & Foveal Vision Interactions** *(jovi-20-12-2.pdf)* — How much periphery can be
  blurred/desaturated without breaking stable perception.
- **Gaze-Contingent Adaptive Focus Displays in VR** *(pnas.201617251.pdf)* — How users perceive blur
  and depth in HMDs.
- **AR Impact on Cognitive Load & Performance** *(Buchner et al.)* — AR can reduce cognitive load;
  Cognitive Load Theory justification: DR reduces extraneous load by removing irrelevant visual
  information.
- **Predicting User Visual Attention in VR with Deep Learning** *(Li2021)* — Attention prediction
  could automate dynamic focus region selection.
- **User Attention in VR Art Encounter** *(s11042-022-13365-2.pdf)* — Eye-tracking analysis
  techniques for evaluating attention distribution.
- **Missing The Point: Guiding Attention in Cinematic VR** *(2993369.2993405.pdf)* — Soft transition
  cue design when DR feels too restrictive.
- **Attention Issues in Spatial Information Systems** *(Biocca et al. & 1394281.1394289.pdf)* —
  Theoretical grounding on attention direction in AR, including "attention tunnelling" risk.
- **Subtle Cues for Visual Search in AR** *(Lu et al.)* — Scene clutter should dynamically modulate
  shader intensity.
- **AR Label Design in Wide-FOV Displays** *(Kruijff et al.)* — Which peripheral features DR must
  suppress most aggressively; boundary highlight design.
- **DR as Training Scaffold** *(murph-et-al-2022)* — Gradual reintroduction of distractions maps to
  temporal fade transitions.
- **Effects of DR on Notifications** *(richardson-et-al-2021)* — How to alert users to critical
  events in the dimmed periphery.
- **Saliency in Augmented Reality** *(Duan et al., MM '22)* — First dedicated AR saliency dataset
  (SARD); AR opacity directly modulates attention.
- **Directing Attention Using AR** *(2037826.2037836.pdf)* ⭐ — Complements the DR approach.

> **Key Takeaway:** Cognitive Load Theory justifies the entire project. Shader intensity should scale
> with scene saliency. Attention tunnelling is the primary risk of the static mode and must be
> addressed in the evaluation design.

---

## Research Gaps Identified (author's original list; Gap 2 refined per prior-art search)

| # | Gap | Significance |
| --- | --- | --- |
| 1 | No unified multi-mode DR system | Nobody has combined multiple DR guidance modes in one configurable app |
| 2 | No DR on video passthrough AR *(refined: no hybrid of system passthrough + app-processed camera feed in one view; colour-only restyling of system passthrough exists via Meta's Styling API)* | Quest gives pixel control of the camera feed unlike OST-HMDs (additive-only) — **strongest novelty claim**, precisely scoped per `framing.md` §6.1 |
| 3 | No temporal transition mechanics | Gradual easing of DR has not been formally studied or parameterised |
| 4 | No user-controlled DR intensity | User-initiated gradual DR release and its effects are unstudied |
| 5 | No dual-channel DR technique | Desaturation + blur studied separately, never combined in AR attention guidance |
