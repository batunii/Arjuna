# Citation Ledger — what we actually cite, and what we take from each

Compiled 2026-07-29 by auditing the eight chapter reference lists in `Dissertation/chapters/`.

## The number

| | |
|---|---|
| **Distinct sources cited in the chapters** | **104** |
| — research papers / articles | ~90 |
| — non-paper sources | ~14 |
| Reference-list entries before dedupe | 171 |
| Sources carrying a `[VERIFY]` marker | 23 |
| Cited in 2+ chapters | 37 |
| Cited in exactly 1 chapter | 67 |

**The 104 is solid. The 90/14 split has about ±3 of wobble** at the boundary, because
classifying a developer-documentation page or a GitHub reference implementation is a judgement
call, not a fact. Anything in the non-paper table below could reasonably be argued either way.

**Why this is not "~110".** That figure came from `lit-review-48papers.md`, which is a *reading
index* — it catalogues work by PDF filename or DOI as well as by author, and it includes sources
that were read but never cited in a chapter. It is a corpus map, not a bibliography. The number
that belongs in the dissertation is the one above: what the chapters actually cite.

### Per chapter

| Chapter | Entries listed |
|---|---|
| ch1 introduction | 9 |
| ch2 related work | 89 |
| ch3 design space | 18 |
| ch4 system | 11 |
| ch5 technical evaluation | 5 |
| ch6 user study | 23 |
| ch7 discussion | 13 |
| ch8 conclusion | 3 |

Chapter 2 carries 86 % of the breadth; every other chapter re-cites a small working set. That is
the normal shape for a related-work-heavy dissertation, but it does mean the argument in Chapters
4–8 rests on roughly 25 sources, not 104.

### The seven workhorses

Cited across four or more chapters — these carry the dissertation's spine:

| Source | Chapters | Load it bears |
|---|---|---|
| Cheng et al. 2022 | 1,2,3,4,6,7,8 | What users want diminished; the anxiety/outline finding; a closest-prior-work anchor |
| Bajireanu et al. 2025 (arXiv 2509.18929) | 1,2,3,5,6,7,8 | The thermal ceiling: 720p30 with throttling at 5–10 min |
| McLaughlin et al. 2025 | 1,2,3,6,7,8 | That dimming distractors reduces errors; precedent for simulated evaluation; mixed-effects analysis |
| Sutton et al. 2022 | 2,3,4,6,7 | ColorPop's whole grading recipe; the no-hue-shift rule; N=20 design norm |
| Frontiers in Human Neuroscience 2025 | 1,2,6,7 | The distractor cost is real: commission errors doubled, N=66 |
| Murphy et al. 2021 | 2,3,6,7 | The focus-versus-awareness tension, formalised here as H2b |
| arXiv 2601.02805 (2026) | 1,2,5,6 | The perceptual gap between video see-through and natural vision |

---

## A. Sources behind the filters

Cross-referenced with `mode-provenance.md`, which traces each filter to a file:line.

| Source | Filter | What we take |
|---|---|---|
| **Veas, Mendez, Feiner & Schmalstieg 2011** (CHI) | ConspicuitySqueeze / FLATTEN | The whole technique: flatten centre–surround contrast toward the local mean. Also their channel ordering — luminance first, then red–green, then blue–yellow |
| **Mendez, Feiner & Schmalstieg 2010** | ConspicuitySqueeze | The algorithm behind Veas's saliency modulation |
| **Cao, Grandi & Kopper 2021** `[VERIFY]` | GranulatedPeriphery / GRAIN | Grain parameters taken directly: 1–7°, 25–75 % density. Also the finding that grains beat blackout on search, 15/20 preferring them |
| **Cheng, Yin, Yan, Gugenheimer & Lindlbauer 2022** (CHI) | OutlinedDark | That context-preserving outlines reduce anxiety — the mode exists to answer this |
| **Bailey, McNamara, Sudarsanam & Grimm 2009** (TOG 28(4)) | ChromaticCool; ColorPop | The warm–cool chromatic modulation is theirs. Also the SGD stimulus envelope (~0.76–1°, ±10 % luminance) and, decisively, that luminance beat warm–cool chroma |
| **Sutton et al. 2022** (UIST) | ColorPop / SignPop | The sigmoidal midtone-contrast recipe used verbatim (α=10, β=0.5, normalised); saturation-plus-contrast approach; the deliberate avoidance of hue shifts |
| **Patney et al. 2016** (TOG 35(6)) | Blur | Contrast restoration after blurring, and the mechanism: peripheral blur is detected *through* its contrast loss. Restoring contrast doubles tolerable radius |
| **Tariq et al. 2022** `[VERIFY]` | Blur | The perceptual-noise term — world-anchored luminance noise scaled by local variance |
| **Walton et al. 2021** `[VERIFY]` | Blur (against) | Ventral metamers degrade more gracefully than Gaussian blur — part of why Blur was retired |
| **Grogorick, Albuquerque & Magnor 2018** (ICIP, N=102) | Blur (against) | Naive whole-periphery blur is irritating — the empirical half of Blur's retirement |
| **Itti, Koch & Niebur 1998** (TPAMI) | all | The salience-map model the entire design rests on: contrast is the master variable; flattening it repels gaze |
| **Kalkofen, Mendez & Schmalstieg 2007** | all | The focus-and-context vocabulary used throughout: keep focus legible, compress context without destroying it |
| **Waldner et al. 2014** (TVCG) | detection pulse | The comfort envelope for flicker: ≈¼ luminance range below 2 Hz gives ≥0.9 detection at low annoyance. Used for the breathing highlight, deliberately avoiding the 15–25 Hz photosensitivity band |
| **Norouzi, Bruder & Welch 2018** (SAP) | motion policy | The actionable negative result: head-coupled vignetting *increased* sickness. The system inverts it — effects fade out during motion |
| **Fernandes & Feiner 2016** (3DUI) | SoftDark | Soft, dynamically applied FOV vignette reduces discomfort with no presence loss |
| **Lin et al. 2020** | SoftDark | Static peripheral blur likewise reduced sickness at no presence cost |
| **Teixeira & Palmisano 2021** (Virtual Reality 25) | SoftDark | Sickness reductions hold over repeated 10-minute exposures |
| **Barhorst-Cates, Rand & Creem-Regehr 2016** (PLoS ONE) | HardDark (against) | Spatial learning survives to 10° FOV, but anxiety is elevated in *all* restricted conditions and rises as the field narrows |
| **Biocca et al. 2006** (CHI) + companion `[VERIFY]` | HardDark (against) | Attention Funnel as the overt-cue baseline (+22 % speed, −18 % workload); and *attention tunnelling* as the named failure mode |
| **Wu & Suma Rosenberg 2022** | selectivity | Masking only the high-optic-flow periphery beats symmetric restriction — suppress the signal, not the region |
| **Stewart, Valsecchi & Schütz 2020** `[VERIFY]` (JoV) | ChromaticCool (against) | The periphery notices contrast loss and motion most, **chromatic detail loss least** |
| **Grogorick, Stengel, Eisemann & Magnor 2017** (SAP) | falloffs | Peripheral detection thresholds scaling linearly with eccentricity; eccentricity-dependent stimulus elongation |
| **Waldin, Waldner & Viola 2017** (CGF) | channel choice | Third leg of the luminance-over-chroma finding |
| **Guenter et al. 2012** (TOG) | falloffs | Multi-layer compositing with progressive peripheral quality reduction |
| **Foveated Rendering survey** (arXiv 2211.07969) `[VERIFY]` | falloffs | The acceptable-degradation mathematics reused for focus-to-periphery falloff |
| **Luminance-contrast-aware foveated rendering 2019** | future work | Tolerable degradation is content-dependent — motivates adapting intensity to scene content |
| **Hein et al.** `[VERIFY]` | compound design | Coarse + fine guidance beats either alone — supports peripheral attenuation plus in-window salience lift |
| **Hata, Koike & Sato 2016** | formation ramp | Blur introduced gradually enough stays below awareness while still biasing gaze |
| **Padmanaban et al. 2017** (PNAS) | blur baseline | How blur and focal depth are perceived in HMDs |
| **Mori, Ikeda & Saito 2017** | framing | The definition of diminished reality the dissertation adopts |

## B. Sources behind the tests

| Source | What we take |
|---|---|
| **ISO 17488:2016** + **Jahn, Oehme, Krems & Gelau 2005** | The Detection Response Task adopted wholesale for the driving block: button-press latency indexes spare capacity, hits scored in a 100–2500 ms window |
| **Victor, Harbluk & Engström 2005** | The ~8°-radius "road centre" where driver gaze concentrates — sets where probes must *not* go |
| **Ball & Owsley 1993** (J. Am. Optom. Assoc. 64(1), 71–79; PMID 8454831 — confirmed via PubMed 2026-07-30) | Useful Field of View: the 35° distractor eccentricity sits in the zone of maximal involuntary capture. Note the paper defines and validates the UFOV *test*; the ≈30° figure used for the outer band boundary is the conventional UFOV extent, not a number stated in this paper — check against the source before relying on it |
| **Caine 2016** (CHI) | The modal CHI sample size is 12; small-N power comes from repeated measures, not headcount |
| **Cheng et al. 2022** | N=16 and N=12 as nearest-neighbour DR sample sizes; the Friedman + Bonferroni-Wilcoxon pattern for Likert batteries |
| **Sutton et al. 2022** | N=20 within-subject with semi-randomised order — the design norm this study matches |
| **McLaughlin et al. 2025** (Human Factors) | Two within-subject experiments analysed with mixed-effects models; the precedent for evaluating DR in simulation |
| **Hart & Staveland 1988** + **Hart 2006** + **Byers, Bittner & Hill 1989** | Raw (unweighted) NASA-TLX: Hart's retrospective endorses it, Byers showed it sacrifices no sensitivity |
| **Kim et al. 2018** | VRSQ (9 items, oculomotor + disorientation), chosen over full SSQ for session fit |
| **Kennedy et al. 1993** | The SSQ that VRSQ replaces |
| **Frontiers in Virtual Reality 2022** `[VERIFY]` | The zero-baseline critique: post-only sickness scores are uninterpretable, so VRSQ runs pre *and* post |
| **Higgins et al. 2022** + **Sitzmann et al. 2018** | Head pose validated as a gaze proxy at the eccentricities used — the licence for telemetry without an eye tracker |
| **Frontiers in Human Neuroscience 2025** (N=66) | Peripheral distractors more than doubled commission errors — the effect the study is powered to detect |
| **Lee & Kim 2025 (DiminishAR)** | Camouflaging one distracting object restored working memory to near-removal levels (N=60) |
| **Murphy et al. 2021** `[VERIFY]` + **2022** `[VERIFY]` | The focus-versus-awareness trade-off, formalised here as equivalence-tested H2b rather than an afterthought |
| **McNamara, Bailey & Grimm 2008** | Subtle modulation raised search accuracy 40.97 → 56.25 %, indistinguishable from an overt cue, with 0/18 noticing |
| **Sridharan, Bailey, McNamara & Grimm 2012** | The mammography-training extension (54.2 → 64.5–68.8 %) — task-level value of subtle guidance |
| **van Winsum 2018** | DRT/PDT performance drops under load — keeps hit rate off ceiling |
| **Yantis & Jonides 1984**; **Franconeri & Simons 2003**; **Simons 2000** | Sudden onsets capture attention automatically, static and gradual ones do not — why probes fade in |
| **Wallis, Dorr & Bex 2015** | Probes that modulate the underlying image survive multiplicative dimming with internal contrast intact |

## C. Statistics and analysis

| Source | What we take |
|---|---|
| **Lakens 2017** | The two-one-sided-tests procedure with a pre-registered margin — converts "no meaningful loss of awareness" from a null into a falsifiable claim |
| **Wobbrock et al. 2011** | The aligned rank transform, the standing HCI norm for non-normal aggregates |
| **The Transformation Trap** `[VERIFY]` | Critique of ART's error properties — motivates mixed models with Wilcoxon fall-backs instead |

## D. Platform and hardware constraints

| Source | What we take |
|---|---|
| **Bajireanu et al. 2025** (arXiv 2509.18929) `[VERIFY authors]` | The only published system rendering the PCA feed live: 720p30, thermal throttling at 5–10 minutes. The deployability argument rests on this |
| **arXiv 2601.02805 (2026)** `[VERIFY authors]` | The perceptual gap between video see-through displays and natural human vision — bounds what can even be tested |
| **Itoh et al. 2021** `[VERIFY]` | Optical see-through can add light but not remove photons; darkening needs per-pixel dimming hardware |
| **Ueda et al. (IlluminatedFocus)** `[VERIFY]` | Real-world defocus via electrically tunable lenses — the expensive optical alternative |
| **Programmable Peripheral Vision** (CHI EA 2022) `[VERIFY]` | Custom hardware for augmenting peripheral vision — the other optical route |
| **Patent US 12548271 / US 12524072** `[VERIFY numbers]` | Claim-level adjacency only: attention control in multi-user XR; blurred external video feed |

## E. Framing and positioning

| Source | What we take |
|---|---|
| **Visual Noise Cancellation 2024** `[VERIFY]` | The name — "visual noise cancellation" by analogy with the acoustic kind |
| **Exploring Diminished Reality for Attention Support** (DIS 2026) | Fifteen students with ADHD independently co-designed the same concept, with no prototype built |
| **Obscuring Undesirable Individuals** (CHI 2026) | Hiding specific people for social comfort — adjacent but not attention guidance |
| **Rothe, Buschek & Hußmann 2019** | The guidance-technique taxonomy used to situate everything else |
| **Barreiros et al.** `[VERIFY]` | AR visualisations must work pre-attentively to feel automatic — the same logic in reverse for diminishment |
| **Buchner et al.** `[VERIFY]` | AR can *reduce* cognitive load when it filters rather than adds |
| **Duan et al. 2022** (SARD) | Saliency in AR dataset |
| **Kruijff et al.** `[VERIFY]` | Which peripheral features most disrupt viewing — a priority list for what to suppress first |
| **Lu et al.** `[VERIFY]` | Subtle cues for visual search in AR |
| **Zhu et al.** `[VERIFY]` | AR-HUD contour highlighting reduces inattentional blindness, especially in bad weather |
| **Renner & Pfeiffer 2017** + follow-up `[VERIFY]` | Gaze-adaptive peripheral cues cut search time in narrow-FOV AR; multi-modal cuts it further |
| **Marquardt et al. 2020** | Audio-tactile guidance matched visual on accuracy and improved situational awareness |
| **Nielsen et al. 2016** | Overt guidance can simply fail in cinematic VR |
| **Grogorick et al. 2020** (CGF) | Stereo inverse brightness modulation — noted as impossible on an OS-owned passthrough layer |
| **Erickson et al.** `[VERIFY]` | Dichoptic colour cues; hue vs saturation vs value analysis informing the saturation/luminance choice |
| **Gaze Guidance using Artificial Colour Shifts** (AVI 2018) `[VERIFY]` | Sub-noticeable CMY displacement as a gentler chromatic alternative |
| **Gaze Navigation by Changing Visual Appearance** (VRST 2018) `[VERIFY]` | Projector pixel-shift blur as the real-world analogue of passthrough shader DR |
| **Depth-Based Subtle Gaze Guidance** (SAP 2015) `[VERIFY]` | Depth as a guidance channel |
| **Automatic Target Prediction and SGD** (SAP 2015) `[VERIFY]` | Cue termination without an eye-tracker trigger |
| **Flicker Augmentations** (CHI 2024) `[VERIFY]` | Flicker revisited as practical AR guidance; its subtle-vs-overt framing organises Chapter 2 |
| **Directing Attention Using AR** (c. 2011) `[VERIFY]` | The ISMAR-era overt-annotation baseline this project defines itself against |
| **Directing Driver Attention with AR Cues 2013** | AR cues in the driving context |
| **AR Warnings in Vehicles 2017** | Modality and specificity of in-vehicle warnings |
| **Saliency-Based Label Placement for AR** `[VERIFY]` | Salience used for placement — noted as repurposable for saliency-weighted diminishment |
| **Rahman et al. 2019** `[VERIFY]` | Gaze-data visualisation for educational VR |
| **Rodrigues et al. 2022** `[VERIFY]` | Gaze-contingent blur in VR boxing — methodological framework |
| **Richardson et al. 2021** `[VERIFY]` | How to alert users to critical events inside a diminished region — explicitly deferred to future work |
| **Yantaç et al.** `[VERIFY]` | Interactive DR walls filtering visual information for autistic individuals |
| **Yao et al. 2013**; **Li 2021** `[VERIFY]`; **User Attention in VR Art 2022** `[VERIFY]`; **Sitzmann et al. 2018** | Attention prediction and exploration behaviour in VR |
| **Hatta / Bourdon** `[VERIFY]` | Cited in ch6; bibliographic identity unresolved |

## F. Non-paper sources (~14)

| Source | Used for |
|---|---|
| Meta developer docs — Passthrough Styling | Tier-1 colour mapping / LUT limits |
| Meta developer docs — Passthrough Windows | Punch-through compositing |
| Meta developer docs — MR Motifs: Passthrough Transitioning | The sanctioned transition pattern |
| Meta — Quest v67 release note (Theatre View) `[VERIFY]` | The Level-1 exception that was withdrawn |
| Meta — Passthrough Camera Access (PCA) | The Tier-3 API the whole system depends on |
| Meta Horizon OS tooling docs | Perfetto tracing, OVR Metrics Tool for Chapter 5 |
| Apple 2024 — accessing the main camera | The comparable platform restriction |
| Magic Leap 2 | Segmented dimming as the hardware counterexample |
| ISO 17488:2016 | The DRT standard (also in §B) |
| US Patent 12548271 / 12524072 | Claim-level adjacency |
| Ultralytics YOLO11 (2024) | The detector behind SignPop's gate |
| Unity Technologies — Sentis | On-device inference runtime |
| Immersed (2022) | Commercial passthrough precedent |
| xrdevrob — QuestCameraKit (2025) | Community PCA reference implementation |

---

## Verification status

**23 of 104 sources carry a `[VERIFY]` marker** — roughly one in five. These are works read and
used whose full bibliographic identity was not confirmed against the source PDF. Several are load-
bearing: Cao 2021 (the GRAIN parameters *and* the grains-beat-blackout finding), Tariq 2022 (Blur's
noise term), Stewart 2020 (the chromatic-channel argument that excludes ChromaticCool), and
Bajireanu 2025 (the thermal ceiling the deployability argument rests on).

`chapters/VERIFY-citations.md` tracks these, but it was compiled 2026-07-05 and says "48 distinct
items" — it predates the current reference lists and needs regenerating before submission.

## How to reproduce

The audit parses the `## References` section of each `chapters/ch*.md`, keys each entry by first-
author surname plus year (falling back to the first three title words for title-first entries), and
collapses a hand-verified alias table for works listed under different leads in different chapters
— e.g. Bajireanu et al. 2025 appears by author in ch3 and by title in six other chapters.

Two counting decisions to be aware of, both defensible either way:

1. **Meta's documentation counts as six sources, not one.** They are six distinct pages doing six
   different jobs. Count them as one and the total is 99.
2. **Entries without a year are kept.** Filtering on "has a parenthesised year" silently dropped 22
   incompletely-recorded sources — all of them real, cited works that carry a `[VERIFY]` marker
   instead of a date. Dropping them would have undercounted by a fifth.
