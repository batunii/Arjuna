# Front Matter

---

## Title Page

<br>

**Guiding User Attention in Real-World Tasks Using XR Overlays**

*A Multi-Mode Diminished-Reality Attention-Guidance System on Consumer Video-See-Through Hardware*

<br>

**[SHREYANSH SONI]**

<br>

A dissertation submitted in partial fulfilment of the requirements for the degree of

**Master of Science**

<br>

School of Computer Science and Statistics

**Trinity College Dublin, The University of Dublin**

<br>

Supervisor: **[Dr. JOHN DIGLIANA]**

August 2026

---

## Declaration

I declare that this dissertation has not been submitted as an exercise for a degree at this or any
other university and that it is entirely my own work.

I agree to deposit this dissertation in the University's open access institutional repository or
allow the Library to do so on my behalf, subject to Irish Copyright Legislation and Trinity College
Library conditions of use and acknowledgement.

I consent / do not consent to the examiner retaining a copy of the dissertation beyond the examining
period, should they so wish (EU GDPR May 2018).

**Use of generative artificial intelligence:** [Complete per TCD's current policy on generative-AI
use in research writing — state which tools were used, for which purposes (e.g. literature-search
assistance, drafting support, code assistance), and affirm that the research design, system
implementation decisions, and intellectual contributions are the author's own.]

<br>

Signed: ______________________  Date: ______________________

---

## Abstract

Extended-reality headsets are conventionally used to *add* information to the world. This
dissertation investigates the opposite operation: using a consumer video-see-through (VST) headset to
*subtract* — dimming, muting, or re-grading the visual periphery so that attention is guided towards,
and held on, a user-chosen region of the real world. The work asks one question with two halves: can
subtractive attention guidance be delivered on consumer XR hardware, and, when it is delivered, does
it actually help human attention?

The technical half confronts a defining constraint of the Meta Quest 3 platform: the operating system
owns the passthrough layer, which applications may neither read nor spatially modify. The
dissertation contributes a three-tier design space of diminished reality (DR) under this compositing
constraint, and a working reference implementation spanning it: a world-locked *focus window* rendered
as an absence in an overlay sphere, through which native passthrough passes at full quality, with
three peripheral treatments — SignPop (salience re-grading of the camera feed), Soft Dark (partial
overlay attenuation), and Hard Dark (full peripheral occlusion) — plus motion-coupled suppression for
comfort. A structured prior-art search found no published or shipped precedent for the system's
hybrid composite of native passthrough and a shader-processed camera feed. Frame-cost measurement
confirms the study configurations render inside the 72 Hz budget, and a legibility comparison shows
the native-passthrough window resolving roughly one to two logMAR lines finer than the camera-fed
path, which is the quantitative case for the architecture's central design decision.

The human half contributes a pre-registered, fully within-subjects user study built to be falsifiable
rather than merely hopeful, carrying a single primary hypothesis, an equivalence-bounded
situational-awareness safety test, and a pre-committed decision table under which every outcome is a
conclusive result. Eighteen participants were tested. **Peripheral dimming improved focal-task
accuracy under sustained distraction by 3.09 percentage points (95 % CI +0.62 to +5.55, *p* = .017
parametric and .015 rank, *d*<sub>z</sub> = 0.645, *n* = 17), at 82 % achieved power.** In a
dynamic driving-scene detection task, salience re-grading did not degrade peripheral awareness
(non-inferiority against a 10-point margin, *p* = .0002, *n* = 15), and improved detection
specifically in the 20–30° eccentricity band by 17.55 points (*p* = .014, *d*<sub>z</sub> = 0.722,
84 % power) — the band immediately outside the focus window's soft edge, which is where the
architecture predicts the effect should appear. Sixteen participants rated the system as helping
concentration on 89 % of items, while reporting a subjective awareness cost the behavioural data do
not show. The pre-registered decision table resolves to its positive row: the system works, with the
dynamic-scene benefit bounded to the near periphery.

---

## Acknowledgements

[ACKNOWLEDGEMENTS — supervisor, pilot participants, School of Computer Science and Statistics,
family/friends, as appropriate.]

---

## Summary of Contributions

This dissertation makes four contributions:

- **C1 — Design space.** A taxonomy of diminished-reality capability under compositing constraints on
  consumer video-see-through hardware (three access tiers: the OS-owned passthrough layer, overlay
  compositing, and camera re-render), with the achievable effects, ceilings, and costs of each tier
  characterised empirically (Chapter 3).

- **C2 — Reference implementation.** An open, working multi-mode DR attention-guidance system on
  Quest 3 passthrough — the world-locked focus-window-as-absence architecture, world-direction fusion
  sampling for the monocular camera feed, and motion-coupled comfort suppression — including
  documented failure modes and dead ends (Chapter 4).

- **C3 — Technical evaluation.** Measured per-mode rendering cost against the 72 Hz frame budget and
  a legibility comparison across rendering paths, quantifying the quality advantage of the
  native-passthrough window over the camera-fed path, together with results from the offline
  detection pipeline and an honest account of the configurations that fail their criteria
  (Chapter 5).

- **C4 — Pre-registered user study and its results.** A falsifiable, within-subjects evaluation of
  peripheral diminishment under controlled distraction, with equivalence-bounded safety hypotheses
  and a pre-committed decision table, executed on 18 participants: focal-task accuracy improved by
  3.09 percentage points at 82 % power, peripheral awareness was non-inferior, and the dynamic-scene
  benefit localised to the 20–30° band immediately outside the focus window (Chapter 6).

---

## Table of Contents

1. Introduction
2. Background and Related Work
3. A Design Space for Diminished Reality under Compositing Constraints
4. System Design and Implementation
5. Technical Evaluation
6. User Study Design and Results
7. Discussion
8. Conclusion and Future Work

[List of Figures — generate at assembly]
[List of Tables — generate at assembly]
[List of Abbreviations: AR, DR, VST, OST, HMD, PCA, FOV, VNC, CPT, DRT, TLX, VRSQ, SUS, SESOI, TOST,
LMM, ROG, SGD]
