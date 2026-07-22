# Focus-Window Size for the Authored-Pool Driving Study — Research Note

> Compiled 2026-07-21 (web-verified same day). Question: given the deliberate dose architecture
> (small clear core + wide graded falloff — a windscreen-sized window leaves the whole usable view
> normal and guides nothing), where exactly should the core and the gradient sit?
> Companion to `authored/HANDOFF.md` ("Probe + window revision" section) and
> `probe-target-design.md`. Design owner: Shreyansh; current scene value 4×4° + 24° soft edge.

---

## 1. Geometry being tuned

The filter strength at scene position (az, el) with window half-extents (hw, hh) and soft edge S:

```
t = smoothstep(0, S, max(|az| − hw, |el| − hh, 0))      // box metric, degrees
```

- t = 0 inside the nominal window; ramps outward; t = 1 at (half-extent + S).
- 50% strength at half-extent + S/2. With S = 24°: hw 4° → 50% at 16°, full at 28°.
- The perceived "clear" region extends well past nominal (dimming below t≈0.2 is barely
  noticeable) — the perceived window is roughly nominal + S/3.
- Eccentricity here is SCENE eccentricity (window is video-locked at az=0), not retinal:
  participants foveate peripheral targets *through* the filter when they look at them.

## 2. Verified perceptual anchors (web-checked 2026-07-21)

| Anchor | Value | Status | Relevance |
|---|---|---|---|
| Driver "road centre" gaze region — Percent Road Centre (Victor, Harbluk & Engström 2005, *Transp. Res. F* 8, 167–190) | Circle of **8° radius** around the most frequent gaze angle (10° in the distraction-detection algorithm variant); baseline driving PRC ≈ 70–80% | **VERIFIED** (secondary sources incl. distraction-algorithm FOT paper) | The clear core should cover what a normal driver actually watches → core half-width ≈ 8° |
| PRC > 92% (10° radius) flags cognitive-distraction gaze concentration | — | VERIFIED (same sources) | Gaze funneled into the core is *plausible driver behaviour*, not an artificial lab state |
| Peripheral Detection Task stimulus eccentricity (Jahn, Oehme, Krems & Gelau 2005, *Transp. Res. F* 8, 255–275) | **11–23° horizontal** ("the original PDT used stimuli presented at 11 to 23 degrees horizontal eccentricity") | **VERIFIED** (van Winsum / Carnetsoft methods page quoting the original) | The standard "peripheral but must remain detectable" band for driving workload probes → our graded zone should span it |
| ISO 17488 *remote* DRT LED placement | ~**20° left/right, ~10° above horizontal** (single remote LED) | PARTIALLY verified (secondary source — a remote-LED DRT methods paper; the standard itself is paywalled). **Check against the ISO PDF before citing in the dissertation.** | Sits inside the 11–23° band; same implication |
| Useful Field of View (UFOV) | ≈ **30° diameter** around the fovea (single fixation, no eye movement); **shrinks under cognitive load** (GC-UFOV driving studies) | **VERIFIED** (UFOV assessment literature; drivers-attention review arXiv:2104.05677) | The gradient's 50% point should fall *inside* ±15° so the manipulation is felt where attention operates |
| Fovea / parafovea | ~1° / ~4–5° radius | Textbook | Absolute lower bound for the core |
| Probe conspicuity (van Winsum, Carnetsoft DRT study) | "Highly conspicuous LEDs may have failed to detect peripheral sensitivity effects **because the stimuli were too easily detected regardless of workload**"; PDT response window 2,500 ms, onsets every 3–5 s | VERIFIED (methods page) | Independent support for the low-conspicuity **black ring** (John Point 1: avoid ceiling) — and our ISO RT scoring (100–2500 ms) and probe cadence match the paradigm |

**Correction vs. earlier session notes:** "11–23°" belongs to the Jahn PDT, *not* ISO 17488's
remote DRT (which is ~20°/10° up). Both land in the same 10–23° band, so no conclusion changes.

## 3. What the actual target pool says (dose per target, 82-target v2 pool, sets A/B = 40/40)

Dose = t at the target's (az_mid, el_mid), bins: clear <0.1 / light 0.1–0.4 / mid 0.4–0.7 /
heavy 0.7–0.9 / full >0.9.

| Config (hw×hh + S) | A: clear/light/mid/heavy/full | A median | B: clear/light/mid/heavy/full | B median |
|---|---|---|---|---|
| **4×4 + 24 (current)** | 5 / 7 / 6 / 6 / 16 | **0.74** | 9 / 8 / 5 / 3 / 15 | 0.55 |
| 6×5 + 24 | 8 / 6 / 8 / 4 / 14 | 0.62 | 13 / 6 / 5 / 2 / 14 | 0.42 |
| **8×6 + 24 (recommended)** | 9 / 8 / 7 / 3 / 13 | **0.50** | 14 / 8 / 3 / 1 / 14 | 0.33 |
| 10×8 + 20 | 10 / 7 / 6 / 3 / 14 | 0.49 | 17 / 5 / 3 / 1 / 14 | 0.30 |
| 12×8 + 16 | 13 / 4 / 5 / 3 / 15 | 0.49 | 18 / 3 / 4 / 1 / 14 | 0.25 |
| 25×15 + 10 (windscreen, rejected) | 27 / 1 / 1 / 0 / 11 | 0.00 | 26 / 2 / 1 / 1 / 10 | 0.00 |

Findings:

1. **The windscreen config empirically confirms the dose critique**: bimodal (nothing on the
   gradient), median dose 0.00 — the filter would be absent from the usable view.
2. **4×4's weakness is the clear end of set A**: only 5 near-clear targets — the unfiltered
   anchor of a dose-response curve is nearly unmeasurable in that set. 8×6 raises it to 9 while
   costing almost nothing at the full end (13 vs 16; the ≥30° mass is unaffected by any config).
3. **8×6 + 24 has the most even A-set spread** (9/8/7/3/13) — best leverage for estimating a
   dose-response; manipulation stays strong (31/40 A and 26/40 B targets carry dose > 0.1).
4. **Practice targets**: id 0 sits at dose 0.74 under 4×4 — a heavily filtered ring that must be
   clicked before the video resumes (possible stall in filter blocks). 0.50 under 8×6. id 58 ≈ 0
   everywhere.
5. **Structural set imbalance**: A runs ~0.17–0.19 hotter than B in median dose at every config
   (the split balanced raw eccentricity; dose is a nonlinear transform of it). Absorbed across
   participants by the AutoSession pairing rotation; within-participant, handle by analysing dose
   as a continuous covariate. Methods note: raw A-vs-B hit-rate gaps (e.g. PILOT999's 80%/57%)
   partly reflect set composition, not condition.

## 4. Recommendation

**Core 8×6° half-extents, keep the 24° soft edge.** Profile → anchor mapping:

| Profile feature | 8×6 + 24 | Anchor it matches |
|---|---|---|
| Clear core edge | 8° az | PRC road-centre radius (8°) — the filter never dims what a normal driver watches |
| 50% strength | 20° az | Inside/at the UFOV edge (±15–20°) — manipulation clearly present in the attended field |
| Graded zone | 8–32° | Spans the PDT/DRT probe band (11–23°) — "peripheral but detectable" is *graded*, the funnel concept |
| Full strength | 32° az | Beyond UFOV and beyond the DRT band — far periphery fully diminished |

4×4 remains defensible (dose maximal), at the cost of: core smaller than the road-centre region
(filter encroaches on what the scenario says to attend), 5-target clear anchor in set A, and the
practice-target risk. Decision owner: Shreyansh. Not applied as of this note — scene still 4×4.

## 5. Sources

- Victor, Harbluk & Engström 2005, Sensitivity of eye-movement measures to in-vehicle task
  difficulty, *Transp. Res. F* 8:167–190 — [ScienceDirect](https://www.sciencedirect.com/science/article/abs/pii/S1369847805000161);
  PRC definition quoted via [distraction-detection FOT paper (ResearchGate)](https://www.researchgate.net/publication/237683246_COMPARISON_OF_TWO_EYE-GAZE_BASED_REAL-TIME_DRIVER_DISTRACTION_DETECTION_ALGORITHMS_IN_A_SMALL-SCALE_FIELD_OPERATIONAL_TEST)
- Jahn, Oehme, Krems & Gelau 2005, Peripheral detection as a workload measure in driving,
  *Transp. Res. F* 8:255–275 — [Semantic Scholar](https://www.semanticscholar.org/paper/9aa51ed1d2c45e8e31c2f8b3b1765a7ca36ed398);
  11–23° + conspicuity finding quoted via [van Winsum / Carnetsoft DRT study](https://cs-driving-simulator.com/the-effects-of-cognitive-and-visual-workload-on-peripheral-detection-in-the-detection-response-task/)
- ISO 17488:2016 DRT — [ISO](https://www.iso.org/standard/59887.html); remote-LED placement via
  [remote LED DRT paper (ResearchGate)](https://www.researchgate.net/publication/327866153_Utilizing_a_Remote_LED_Stimulus_to_Concurrently_Measure_Cognitive_and_Visual_Task_Demand) — **verify against the standard before citing**
- UFOV ≈30° diameter, load-shrinkage — [BrainHQ/UFOV](https://www.brainhq.com/partners/brainhq-for-clinicians/ufov/),
  [GC-UFOV simulated driving (Tufts PDF)](https://bpb-us-e1.wpmucdn.com/sites.tufts.edu/dist/8/3318/files/2015/10/Measuring-the-useful-field-of-view-during-simulated-driving-with-gaze-contingent-displays.pdf),
  [drivers' attention review, arXiv:2104.05677](https://arxiv.org/pdf/2104.05677)
