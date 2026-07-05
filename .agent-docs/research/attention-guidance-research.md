# Attention-Guidance Research Notes (compiled 2026-07-03)

Verified literature survey supporting mode design, detection highlighting, and YOLO pipeline
decisions. [FT] = verified against full paper text; [AB] = verified via abstract/metadata.
Citation corrections found during verification: Bailey et al. 2009 is **ACM TOG** (not TAP);
Hata et al. is **AVI 2016**; Sutton et al. is **UIST 2022** (not SIGGRAPH).

---

## Part 1 — Cross-cutting laws (use to justify mode design)

1. **Contrast is the master variable.** Computational saliency = center–surround contrast in
   intensity / color-opponency / orientation (Itti, Koch, Niebur, IEEE TPAMI 20(11), 1998).
2. **Luminance > color** for guidance strength in every head-to-head (Bailey 2009, Grogorick
   2017, Waldin 2017). The ChromaticCool mode is on the weaker channel — comfortable but weak.
3. **Effectiveness ∝ noticeability**, with two escape hatches: gaze-contingent termination
   (needs eye tracker) or slow ramps below awareness thresholds (Hata 2016). Sutton 2022: the
   imperceptible-yet-effective band is thin.
4. **Whole-periphery blur is the least comfortable subtle option** (Grogorick ICIP 2018:
   59/102 participants irritated by SpatialBlur). Contrast-preserving or noise-based
   alternatives are better tolerated.
5. **Suppress motion, keep awareness.** Selective suppression (motion-only, or grain
   preserving peripheral events) beats uniform blackout on presence/search (Wu VRST 2022;
   Cao Frontiers VR 2021).
6. **Don't couple modulation strength to the user's own head motion in the provocative
   direction** (Norouzi SAP 2018: head-coupled vignetting increased sickness). Our design
   (fade out during motion, restore during stability) is the right call and citable as such.

## Part 2 — Key papers

### Subtle gaze direction family
- Bailey, McNamara, Sudarsanam, Grimm. "Subtle Gaze Direction." ACM TOG 28(4), Art. 100, 2009.
  DOI 10.1145/1559755.1559757. Peripheral 10 Hz luminance modulation (±9.5%, ~0.76° region),
  terminated on saccade. Luminance beat warm–cool chroma. [AB]
- McNamara, Bailey, Grimm. "Improving Search Task Performance Using Subtle Gaze Direction."
  APGV 2008, 51–56. [FT] N=18; accuracy 40.97%→56.25% (subtle) vs 56.94% (obvious);
  zero participants noticed the subtle cue.
- Sridharan, Bailey, McNamara, Grimm. "Subtle Gaze Manipulation for Improved Mammography
  Training." ETRA 2012, 75–82. DOI 10.1145/2168556.2168568. [FT] Accuracy 54.2%→64.5–68.8%.
- Grogorick, Stengel, Eisemann, Magnor. "Subtle Gaze Guidance for Immersive Environments."
  ACM SAP 2017. DOI 10.1145/3119881.3119890. [FT] Peripheral detection thresholds scale
  linearly with eccentricity: s_static(e)=0.0098e+0.1009°; s_dynamic(e)=0.0076e+0.289°.
  Hidden-target search 36.07 s → 8.20 s.
- Grogorick, Albuquerque, Magnor. "Comparing Unobtrusive Gaze Guiding Stimuli in HMDs."
  IEEE ICIP 2018, 2805–2809. DOI 10.1109/ICIP.2018.8451784 (companion: ACM TAP 15(4), 2018,
  DOI 10.1145/3238303). [FT] N=102, five stimuli. **ZoomRect (local magnification) best**
  (~30% target saccades within 1 s; only 3/102 recognized it). SpatialBlur least subtle
  (59/102 irritated). "No method remains completely unnoticed."
- Grogorick et al. "Stereo Inverse Brightness Modulation for Guidance in Dynamic Panorama
  Videos in VR." CGF 39(2), 2020. Binocular-rivalry cue — only works on our own overlay,
  never on OS passthrough.
- Waldin, Waldner, Viola. "Flicker Observer Effect." CGF 36(2), 2017, 467–476.
  DOI 10.1111/cgf.13141. [FT] 60–72 Hz flicker: invisible foveally, pops out peripherally.
  Marginal on Quest (needs refresh near CFF; passthrough frames are 30–60 fps).

### Saliency modulation of see-through video (core precedent)
- Veas, Mendez, Feiner, Schmalstieg. "Directing Attention and Influencing Memory with Visual
  Saliency Modulation." CHI 2011, 1471–1480. DOI 10.1145/1978942.1979158. [FT] Itti-style
  conspicuity raised in focus / lowered in context, channel order L → R–G → B–Y. Modulated
  video indistinguishable from unmodulated; faster first fixation (t(35)=2.916, p<.01);
  recall 0.19→0.31 (p=.008). **Strongest evidence for imperceptible video modulation.**
  Algorithm: Mendez, Feiner, Schmalstieg, Smart Graphics 2010, LNCS 6133, 232–243.
- Sutton, Langlotz, Plopski, Zollmann, Itoh, Regenbrecht. "Look over there! Investigating
  Saliency Modulation for Visual Guidance with AR Glasses." UIST 2022, Art. 81.
  DOI 10.1145/3526113.3545633. [FT] Saturation + sigmoidal contrast (α=10, β=0.5) ± blur;
  no hue shifts ("confusing"). More target fixations (χ²=25.57, p<.001) but NOT imperceptible;
  vs overt circle: modulation preserved natural exploration (circles "glue" gaze).
- Kalkofen, Mendez, Schmalstieg. "Interactive Focus and Context Visualization for AR."
  ISMAR 2007, 191–200. DOI 10.1109/ISMAR.2007.4538846. [AB]
- Hata, Koike, Sato. "Visual Guidance with Unnoticed Blur Effect." AVI 2016, 28–35.
  DOI 10.1145/2909132.2909254. [AB] Blur-awareness threshold > gaze-guidance threshold;
  slow ramping keeps blur unnoticed.

### Diminished reality for distraction suppression
- McLaughlin et al. "Cognitive Aid Design Using Diminished Reality…" Human Factors 67(9),
  937–961, 2025. DOI 10.1177/00187208251325169. [AB] Universal attenuation (≈ our vignette)
  → fewer errors (β=−0.37, p=.021), lower workload than context-aware removal.
- Lee, Kim. "DiminishAR." CHI 2025. DOI 10.1145/3706598.3713415. [AB] AR-camouflaging a
  distractor restored working memory ≈ physical removal (N=60).
- Cheng, Yin, Yan, Gugenheimer, Lindlbauer. "Towards Understanding Diminished Reality."
  CHI 2022. DOI 10.1145/3491102.3517452. [AB] Users prefer opacity reduction + want
  context-preserving outlines on diminished content.
- Mori, Ikeda, Saito. "A Survey of Diminished Reality." IPSJ TCVA 9:17, 2017 (taxonomy).

### Peripheral restriction: comfort limits
- Fernandes, Feiner. IEEE 3DUI 2016, 201–210. DOI 10.1109/3DUI.2016.7460053. Soft dynamic
  FOV vignette: less discomfort, no presence loss. [AB]
- Norouzi, Bruder, Welch. ACM SAP 2018. DOI 10.1145/3225153.3225162. [AB] Head-motion-coupled
  vignetting INCREASED sickness.
- Lin et al. ACM TAP 17(4), 2020. DOI 10.1145/3419984. [AB] Static peripheral blur reduced
  sickness, no presence cost.
- Cao, Grandi, Kopper. Frontiers in Virtual Reality 2:604889, 2021. [FT] Granulated rest
  frames (static noise grains 1–7°, 25–75% density) beat black FOV restrictors on search;
  15/20 preferred grains.
- Barhorst-Cates, Rand, Creem-Regehr. PLoS ONE 11(10):e0163785, 2016. [FT] Spatial learning
  intact down to 10° FOV; anxiety elevated in ALL restricted conditions, monotonic.
- Teixeira, Palmisano. Virtual Reality 25:433–445, 2021. Sustained use (3×10 min): sickness
  down, presence unaffected. [AB]
- Wu, Suma Rosenberg. VRST 2022. DOI 10.1145/3562939.3565611. [AB] Masking only
  high-optic-flow periphery beat symmetric restriction on presence.
- Patney et al. ACM TOG 35(6), 2016. DOI 10.1145/2980179.2980246. [AB] Peripheral blur is
  detected via its contrast reduction; restoring contrast post-blur doubles tolerable radius.
- Waldner, Le Muzic, Bernhard, Purgathofer, Viola. "Attractive Flicker." IEEE TVCG
  20(12):2456–2465, 2014. [FT] Sweet spot ≈ ¼ luminance range at <2 Hz for ≥0.9 detection
  with low annoyance.
- Rothe, Buschek, Hußmann. MTI 3(1):19, 2019 (guidance taxonomy anchor).

## Part 3 — Ranked candidate NEW modes (shader-implementable)

1. **Conspicuity Squeeze (local-contrast flattening).** `lerp(col, localMean, k(t)*s)` in a
   Lab-ish space, compressing chroma opponents first, luminance last. Structure stays, punch
   goes — no tunnel percept. Evidence: Veas CHI 2011 (imperceptible + memory effect).
2. **Contrast-Restored Blur** (upgrade of existing Blur): blur, then re-apply local contrast
   `out = M + (blur − M)(1 + c·k(t))`. Removes the tunnel-vision percept (Patney 2016) and
   the 59/102 irritation finding (Grogorick 2018).
3. **Granulated Periphery**: world-locked (az/el-indexed) static noise grains, 1–4°, density
   25→75% with t. Beat black restrictors on search, preserved awareness (Cao 2021). We already
   have the noise texture.
4. **Outlined Dimming** (upgrade of HardDark): near-blackout + faint monochrome luminance
   edges added back (mip-difference or Sobel). Users want outlines on diminished content
   (Cheng CHI 2022); mitigates the anxiety finding (Barhorst-Cates 2016).
5. **Opponent-Channel Chroma Flattening**: compress Cb/Cr toward the periphery's mean chroma
   (not grey), luminance untouched. Subtler than desaturation.
6. **Focus Luminance Lift ("spotlight")**: gentle brightness/contrast lift inside the window
   (a ≈ 0.05–0.10) paired with soft peripheral dim. Luminance = strongest channel.
7. **Peripheral Motion Damping**: EMA history buffer, `lerp(col, history, m(t))` — periphery
   goes temporally static, focus stays live. Attacks the real distraction channel (motion).
   Needs a history RT; cap m ≤ 0.9 (ghosting is safety-relevant).
8. **Sub-2 Hz Focus Shimmer**: transient luminance oscillation at the window border, ¼ L
   range, <2 Hz, event-gated (re-capture cue, never sustained). Waldner 2014 parameters.
9. **Focus Magnification Pulse (ZoomRect)**: subtle UV magnification (→1.05) pulsed ~1 s.
   Best guidance-per-noticeability ratio measured (Grogorick 2018) but geometric distortion
   conflicts with reach accuracy — brief cue only.
10. **Stylized Abstraction Periphery**: posterize luminance + compress chroma + mip-bias
    flatten. Overt-but-pleasant contrast condition (Cole EGSR 2006; Su APGV 2005).

For sustained tasks prefer 1–5; use 8–9 only as transient re-capture events.

## Part 4 — Detection highlighting (implemented 2026-07-03)

Design: soft ellipse mask + warm halo ring breathing at ~1.2 Hz + steady inner saturation
lift. Rationale: luminance modulation strongest cue (Bailey 2009, Grogorick 2018); outlines
preferred over shaded overlays (OST-AR first-responder study, arXiv:2403.04660); flicker must
be visible to guide (Sci Rep 6:29296, 2016); avoid 15–25 Hz photosensitive band; 1–4 Hz is
the effective+comfortable window; saliency boost validated by Veas 2011 / Sutton 2022.

## Part 5 — Detection pipeline recommendations (ranked by effort)

1. `ScheduleIterable()` + split-over-frames instead of every-N-frames gating.
2. YOLOv9 → **YOLO11n** (~22% fewer FLOPs than v8n), NMS baked into graph, FP16 quantization
   first (UINT8 hurts small-sign recall first — validate).
3. ROI second pass: low-res full-frame detect → re-detect on upscaled crop.
4. Fine-tune nano model on **Mapillary Traffic Sign Dataset** (Ertler et al., ECCV 2020,
   arXiv:1909.04422; 300+ classes; ~+6% AP from MTSD pre-training). Also: GTSDB (Houben et
   al., IJCNN 2013), TT100K (Zhu et al., CVPR 2016).
5. Stay CPU/Burst on Quest (GPU saturated by rendering); skip DETR variants (transformer
   latency unsuitable for XR2).

## Honest gaps (state in dissertation)

- No verified study measures 10+ min comfort of peripheral desaturation specifically.
- No verified head-to-head of blur vs black vignette within one design.
- No canonical 2020–2025 systematic review of XR attention guidance (use Rothe 2019 as anchor).
- "Foveated rendering guides attention" is unsupported — do not claim it.
