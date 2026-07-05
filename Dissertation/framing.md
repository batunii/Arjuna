# Dissertation Framing — Two Parts, One Spine
## Guiding User Attention in Real-World Tasks Using XR Overlays
### MSc Dissertation — Trinity College Dublin

> Companion to `testing-strategy-v2.md` (the user-study methodology). This document fixes the
> intellectual structure of the dissertation: what is being claimed, under what assumptions, in what
> order, and how each claim is defended. It is written to be shown to the supervisor and to be
> enforced against the dissertation's own text during writing.

---

## 1. The Spine

The dissertation asks **one** question with two halves:

> **Can subtractive attention guidance be delivered on consumer XR hardware — and when it is
> delivered, does it actually help human attention?**

- **Part T (Technical): "Can it be built?"** What degree of diminishment control is achievable on a
  consumer video-see-through (VST) headset **without OS-level access to the passthrough layer**, and
  at what cost to image quality, field of view, latency, and comfort?
- **Part H (Human): "Does it work?"** Under controlled distraction, does peripheral diminishment
  reduce the cost distractors impose on a focal task, and does salience re-grading speed focal
  detection without unacceptably degrading peripheral awareness? (Formalised as H1, H2a, H2b in
  `testing-strategy-v2.md` — those hypotheses and their decision table are the only confirmatory
  claims the dissertation makes.)

Neither half stands alone: Part T without Part H is an engineering report; Part H without Part T is a
psychology study run on a video player. Their coupling is deliberate and must be stated in the
introduction, not left for the reader to infer.

---

## 2. Part H — Concept Evaluation Under an Explicit Ideal-System Assumption

### 2.1 What this actually is (name the method)

Part H does **not** "assume we have magic glasses." It uses two named, citable methodological moves:

1. **Simulation-based evaluation.** The dynamic scenario (driving footage on the video sphere) is a
   controlled simulation of the deployment context, in the tradition of Cheng et al. (CHI 2022), who
   evaluated DR concepts entirely inside VR simulations because real DR was not feasible. The
   simulation exists to give every participant identical, repeatable, event-dense stimuli — something
   a real street never provides.
2. **Oracle perception, precisely scoped.** The evaluated modes are deliberately **detector-free**:
   ColorPop keys on luminance and colour, the dark modes on window geometry alone — so the study's
   findings do not depend on any perception stack at all. Where detection *does* enter the work, it
   enters as an oracle: the pre-baked full-resolution YOLO11 detections (9,170 detections over 720
   samples) validate the driving stimulus's event density and anchor the Part T oracle-gap benchmark
   (§3.3), standing in for the perfect detector that future detector-dependent variants would
   require. This **decouples the perception problem from the interaction question** twice over.
   Consequence, to be stated in the methods chapter:

   > *The human-factors findings of this study remain valid regardless of future improvements in
   > object detection — first because the evaluated modes require no detector, and second because
   > detector-dependent extensions are characterized against a measured oracle bound that live
   > systems approach from below.*

### 2.2 The claim-scoping rules (binding on the dissertation's own text)

| Rule | Wording that is allowed | Wording that is banned |
|---|---|---|
| **No driving claims.** A seated participant passively watching passenger-view footage licenses zero claims about driving. | "a dynamic, high-event-rate monitoring stimulus"; "driving footage as stimulus context" | "drive testing", "while driving", "road safety", "driver performance" |
| **No product claims.** The study tests attention mechanisms, not a wearable product. | "the guidance mechanism", "the effect on attention under controlled distraction" | "users would benefit in daily life", "ready for deployment" |
| **No unfalsifiable claims.** Every confirmatory statement traces to H1/H2a/H2b and the pre-committed decision table. | "H1 was supported at the pre-registered effect size" | "the results suggest the system is beneficial" (as a headline claim) |
| **Oracle stays visible.** Every Part H conclusion that depends on detection is flagged as oracle-conditioned. | "under oracle perception" | silently generalising ColorPop/detection results to live systems |

### 2.3 What transfers from the simulation and what does not (table for the limitations section)

The tempting argument — *"it works on a heavy headset, so lighter glasses can only be better"* — is
valid **only for comfort**, not for the attention effect. The dissertation does the examiner's
skepticism for them:

| Simulation property | Direction of bias vs. real deployment | Transfer status |
|---|---|---|
| Headset weight / bulk | Conservative for comfort ratings only; orthogonal to attention effects | Comfort findings transfer as lower bound; attention findings unaffected either way |
| Monoscopic equirect video (no stereo depth) | Unknown — peripheral motion in depth may capture attention differently | Does **not** transfer automatically; flagged |
| Passenger viewpoint, no vehicle control | Removes task coupling; real operators split attention with motor control | Does **not** transfer; this is why no driving claims are made |
| Video dynamic range vs. real-scene luminance | Compresses glare; ColorPop's glare suppression is under-tested | Flagged; glare results are indicative only |
| Identical stimuli for all participants | Strengthens internal validity at the cost of ecological validity | The trade is explicit and deliberate |
| Oracle detection (100 % recall, zero latency) | Best case for detection-dependent modes | Bounded by the Part T oracle-vs-live benchmark (§3.3) |
| Real passthrough + physical distractors (Block A) | None — this block runs on the actual system | Transfers directly; this is the bridge (§4) |

---

## 3. Part T — The Design Space of Diminished Reality Under Compositing Constraints

### 3.1 The precise problem statement (not "we made DR additive")

On Quest 3, the OS owns the passthrough layer: an application **cannot read, modify, or subtract from
it** — it can only composite content *on top*. Subtractive DR must therefore be synthesized through
purely overlay means. On true optical-see-through (OST) glasses the constraint is harsher still: the
display physically adds light and cannot darken the world at all. The dissertation's technical
question is what diminishment is achievable at each tier of access, and it answers empirically for
the VST case:

| Tier | Access | What DR is achievable | Cost / ceiling | Where the modes live |
|---|---|---|---|---|
| **1 — OS passthrough layer** | Styling API only (`OVRPassthroughLayer`: brightness/contrast/saturation, 3D colour LUTs, edge tint) | **Global colour remapping only.** No blur, no per-pixel spatial masks, no world-locked regions; the layer can never be *read*. (Surface-projected passthrough, which allowed per-surface styling, is deprecated at SDK v83.) A global desaturation would be possible here — a windowed/peripheral effect is not. | Colour-only, whole-layer | (The absence of *spatial* control is the finding) |
| **2 — Overlay compositing** | Draw on top with alpha | Dimming, blackout, occlusion; shaped *absence* of overlay passes OS-native quality through | Cannot recolour or filter reality, only attenuate/occlude it | Soft Dark, Hard Dark; the world-locked focus window |
| **3 — Camera re-render (PCA)** | Full pixel control of the camera feed | Arbitrary manipulation: blur, desaturation, salience re-grading, glare compression | Mono 1280×960, ~85–90° FOV, added latency, binocular-fusion burden | Blur, ColorPop |

The architectural insight to foreground: **the focus window is an absence, not a rendering.** Inside
the window nothing is drawn, so the user sees OS passthrough at full native quality and full ~110°
FOV; the manipulation lives only in the periphery. This inverts the naive design (re-render
everything, accept Tier-3 quality everywhere) and is why the system avoids the acuity ceiling exactly
where the user is asked to look.

Supporting technical contributions (each already implemented and documented in the repo):

- **World-direction camera sampling** for binocular fusion of the mono camera feed (screen-space UV
  sampling produced double vision; sampling by world direction through a head-centred pinhole is
  eye-independent and fuses correctly).
- **Motion-based suppression** as a comfort mechanism (effect fades during fast head rotation,
  restores on settle; grounded in Norouzi et al. SAP 2018 — head-coupled vignetting increases
  sickness, so the effect is decoupled instead).
- **The failure museum** (deliberately reported, not hidden): screen-space sampling double vision;
  surface-projected passthrough deprecated at SDK v83; `Shader.Find` returning null for stripped
  shaders on Android; downsampled YOLO missing small traffic lights → the full-resolution offline
  baker. Documented dead ends are reusable knowledge and signal research maturity.

### 3.2 Honest novelty position (write it this way, verify before submission)

An alpha-blended dark overlay is, by itself, just compositing — **not** claimed as novel. The claimed
contribution is: *a characterized design space of DR under compositing constraints, with a working
reference implementation, measured trade-offs, and documented failure modes.* A structured prior-art
search (§6.1, run 2026-07-05) found **no publication, product, or open-source project** that
composites system passthrough with a live shader-processed camera feed in one view — for any purpose
— while every constituent mechanic has documented precedent that must be cited: camera-feed geometry
with GPU effects (QuestCameraKit), head-centred fade sphere revealing passthrough (Meta MR Motifs
"Passthrough Transitioning"), alpha punch-through windows (Meta Passthrough Windows docs; Immersed
"Passthrough Portals", 2022), OS peripheral dimming in immersive-only mode (Quest v67 "Theatre
View"), and saliency modulation of reality on an optical see-through rig (Sutton et al. UIST 2022).
The defensible novelty claim is therefore the **composite** — native passthrough revealed through a
world-locked window in an effect-processed PCA sphere, exploiting the quality/FOV asymmetry between
the two layers — plus its application to peripheral attention guidance on a consumer standalone HMD,
together with the world-direction fusion sampling for the mono feed.

**Explicit non-transfer statement (make it before the examiner does):** the Tier-2 dark overlay works
because a VST screen is opaque and fully under render control. It does **not** transfer to OST
glasses, whose additive optics cannot darken the world. What transfers to OST is the *taxonomy* (OST
collapses to Tier 3 applied at the display, e.g. dimming layers/segments) and the human-factors
findings about what peripheral diminishment does to attention.

### 3.3 Part T's evaluation — benchmarks, not vibes

Part T must carry numbers or it reads as a development diary. All are obtainable without
participants, using the existing Meta tooling (Perfetto traces, logcat, metavr):

| Benchmark | Method | What it defends |
|---|---|---|
| Per-mode GPU frame cost & headroom vs. 72/90 Hz budget | Perfetto trace per mode, idle scene vs. active | "Runs on consumer hardware" claim |
| Overlay/window update latency (head motion → window stability) | Trace + high-speed capture through the lens or frame counters | World-locking quality |
| Legibility/acuity: OS-passthrough window vs. Tier-3 camera periphery | Eye chart / grating photographed through the lens at fixed distance, both paths | The window-as-absence architecture's whole point, quantified |
| On-device YOLO throughput & recall vs. offline oracle | Live inference benchmark vs. the baked ground truth on the same footage | **Quantifies the oracle gap — the single table that ties Part T to Part H** |
| Thermal/battery over a 45-min session | Device telemetry during a dry run | Study feasibility + deployment realism |

The oracle-gap table deserves emphasis: it measures exactly how far today's live perception is from
the assumption Part H evaluates under. That number converts "we assumed ideal detection" from a
hand-wave into a stated, measured distance.

---

## 4. The Bridge — Why the User Study Belongs to Both Parts

State this explicitly in the study chapter's opening; it is the joint that makes the dissertation one
piece of work:

- **Block A** (Hard Dark, real passthrough, physical distractor tablets) evaluates the **actual
  Part T artifact** on real hardware — the today-system's effect on attention. Tier 2, no simulation,
  no oracle.
- **Block B** (ColorPop, video sphere, scripted machine-timed probes) evaluates the **Part H
  concept** in the simulated deployment context — the future-system's effect on attention. ColorPop
  itself is detector-free; no live or baked detections run in the study protocol (the oracle
  detections serve stimulus validation and the §3.3 oracle-gap benchmark). Tier 3 capability,
  simulated context.
- **Block C** (compact mode sampler, skippable) spans the design space subjectively: all three modes
  on one stimulus, including **Soft Dark's only user data**. Soft Dark's retention rationale — the
  minimal unconfounded manipulation, the graded Tier-2 point, the only camera-free (hence
  hour-deployable) mode, the awareness-preserving midpoint — is recorded in
  `testing-strategy-v2.md` §3.5.

The study is therefore not an appendix to either part: it is the experiment that runs the same
guidance principle at both ends of the feasibility spectrum. If Block A and Block B agree in
direction, the simulation methodology itself gains credibility; if they diverge, that divergence is a
reportable finding about what simulation-based XR evaluation misses.

---

## 5. Contributions List (introduction chapter, verbatim candidates)

- **C1 — Design space.** A taxonomy of diminished-reality capability under compositing constraints on
  consumer VST hardware (three access tiers), with the achievable effects, ceilings, and costs of
  each tier characterized empirically.
- **C2 — Reference implementation.** An open, working multi-mode DR attention-guidance system on
  Quest 3 passthrough — world-locked focus-window-as-absence architecture, world-direction fusion
  sampling for mono camera feeds, motion-based comfort suppression — including documented failure
  modes and dead ends.
- **C3 — Technical benchmarks.** Measured per-mode performance, latency, legibility across rendering
  paths, and the oracle-vs-live perception gap.
- **C4 — Pre-registered user study.** A falsifiable, within-subjects evaluation (N = 20) of
  peripheral diminishment under controlled distraction, with a pre-committed decision table covering
  positive, negative, and qualified outcomes (`testing-strategy-v2.md`).

## 5.1 Chapter skeleton

1. **Introduction** — the compositing constraint as motivating problem; spine question; C1–C4.
2. **Related work** — attention guidance (SGD family, saliency modulation), DR (Mori survey; Cheng
   2022; McLaughlin 2025), VST/OST display constraints. Position against Cheng and McLaughlin and
   state the delta: *they simulated DR inside VR; this work runs on real passthrough hardware and
   pairs the simulation with a real-hardware block.*
3. **Design space** (Part T) — the tier taxonomy; what is impossible and why.
4. **System** (Part T) — architecture, window-as-absence, fusion sampling, motion suppression,
   failure museum.
5. **Technical evaluation** (Part T) — §3.3 benchmark tables, ending on the oracle-gap table as the
   hand-off to Part H.
6. **User study** (Part H + bridge) — `testing-strategy-v2.md`, prefaced by §2's method naming and
   the transfer table.
7. **Discussion** — decision-table outcome; Block A vs. Block B convergence; what transfers to OST
   (and the explicit non-transfer of the dark overlay); oracle caveats; future work.
8. **Conclusion.**

---

## 6. Prior-Art & Pre-Submission Checklist

- [x] **Targeted novelty search — DONE 2026-07-05** (see §6.1 for outcome and the citable search
      protocol). Re-run a lighter refresh search in the month before submission (the PCA ecosystem
      moves fast), and obtain the CHI 2026 Vision Pro DR paper's full PDF via the library to confirm
      its implementation details (ACM DL blocked the fetch; characterized from abstract only).
- [ ] Verify every banned-wording rule (§2.2) against the final text — literal string search for
      "driv" in claims sections.
- [ ] Every confirmatory sentence in the abstract/conclusion maps to a decision-table row.
- [ ] Oracle-gap benchmark completed **before the discussion chapter is finalised** (a pre-results
      draft of Chapter 7 exists, written in conditional tense; revise it when the T4 benchmark
      lands).
- [ ] Cheng 2022 / McLaughlin 2025 delta stated in related work, not implied.
- [ ] Failure museum entries each carry the evidence (log/commit/date) so they read as engineering
      record, not anecdote.

### 6.1 Prior-art search outcome (2026-07-05)

Structured search across ISMAR/CHI/UIST/IEEE VR/DIS/arXiv 2022–2026, Meta developer documentation and
blog, GitHub (including all 136 forks of `oculus-samples/Unity-PassthroughCameraApiSamples`), XR press
(UploadVR, RoadToVR), and platform API documentation (visionOS, Varjo). 26 web queries + 4 GitHub API
queries; the full query list is archived for the dissertation's related-work chapter (a documented
search protocol is itself citable evidence of novelty diligence).

| Claim | Verdict |
|---|---|
| Hybrid composite: system passthrough revealed through a window in a live shader-processed PCA camera-feed sphere | **Nothing found** — no paper, product, or open-source project, for any purpose |
| Peripheral passthrough manipulation for attention guidance on a standalone consumer HMD | **Nothing found as an implemented system**; concept-level near-precedents only (McLaughlin 2025 = VR-simulated; DIS 2026 ADHD co-design = formative, no prototype; DiminishAR / CHI 2026 AVP = object-targeted occlusion; Sutton UIST 2022 = OST lab rig) |
| Dark-overlay-only vignette over passthrough | **Near precedent, no direct**: Quest v67 "Theatre View" dims the periphery but only in immersive mode, explicitly not over passthrough; Vision Pro surroundings dimming is global, not windowed |

Must-cite near-precedents for the related-work chapter: Sutton et al. UIST 2022
(dl.acm.org/doi/10.1145/3526113.3545633); McLaughlin et al. Human Factors 2025
(doi 10.1177/00187208251325169); "Exploring DR for Attention Support" (ADHD co-design), DIS 2026
(dl.acm.org/doi/10.1145/3800645.3813095); CHI 2026 Vision Pro person-obscuring DR
(dl.acm.org/doi/10.1145/3772318.3790918); DiminishAR (arXiv 2403.03875); Cheng et al. CHI 2022;
native MR compositing on Quest 3 via PCA (arXiv 2509.18929 — cites on-device feasibility/thermal
limits, 720p30 with 5–10 min before throttling: useful corroboration for Part T benchmarks);
QuestCameraKit (github.com/xrdevrob/QuestCameraKit); Meta MR Motifs "Passthrough Transitioning";
Meta Passthrough Windows + Immersed "Passthrough Portals" (2022); Quest v67 Theatre View; Meta
Passthrough Styling API (colour LUTs — the reason Tier 1 reads "colour-only", not "nothing");
visionOS enterprise-only camera access; patents US 12548271 and US 12524072 (claim-level adjacency,
no evidence of implementation — one sentence each).

## 7. Effort Allocation Warning

Part T's remaining cost is low: the engineering exists, benchmarks are traces and tables, the failure
museum is already in the session logs. **Part H is where the risk lives** — tooling build, pilot
gates, 24 participants, one month. The schedule must protect Part H; Part T writing fills the gaps
between sessions, never the reverse. If time is lost, cut Part T polish (e.g. drop the thermal
benchmark) before touching the pilot or N.

---

*Prepared 2026-07-05. Companion documents: `testing-strategy-v2.md` (study methodology),
`.agent-docs/research/attention-guidance-research.md` (literature notes),
`.agent-docs/systems/focus-vignette.md` (system spec).*
