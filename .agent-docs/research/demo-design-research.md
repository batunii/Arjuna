# Demo-Design Research: Making the Attention-Guidance Effect Demonstrable (compiled 2026-07-08)

Deep-research run answering: *why doesn't the current demo (Times Square clip + "click traffic
lights") show the effect working, and what stimulus/task/filter combination will?*
Method: 6 search angles → 22 primary sources fetched → 108 claims extracted → top 15
adversarially verified (14 confirmed, 1 refuted). Companion to
`attention-guidance-research.md` (mode-design literature) and
`../../Dissertation/testing-strategy-v2.md` (formal study).

---

## Verdict: the culprit is the video + task pairing, not the filter

Two independent lines of evidence:

1. **Conspicuity ceiling (direct precedent for our null).** Rusch et al. 2013
   (Transportation Research Part F, N=27 driving simulator): AR cueing significantly improved
   detection of pedestrians (p=.03) and warning signs (p<.01) but showed **no benefit for
   vehicles** (F(2,117)=1.73, p=.18) — because vehicles were already visible before the cue.
   Guidance cannot demonstrate benefit on targets the viewer finds anyway.
   ColorPop **deliberately keeps traffic lights near-natural everywhere** (outside-window ROG
   dimmed only ×0.8 — design decision 2026-07-03), so "find lights anywhere" is a task the
   filter is engineered NOT to dominate. The null result is the filter working as designed.
   https://pmc.ncbi.nlm.nih.gov/articles/PMC3891797/ (verified)

2. **The validated driving-video paradigm is built the opposite way.** Hazard-perception (HP)
   clips are engineered so that ~85% of ALL viewers detect the hazard (TRL PPR2071: 86%
   experienced vs 84% learner responded within windows) — detection is near-ceiling by design,
   and **discrimination comes from response EARLINESS**, not from whether the target is found.
   Our Times Square clip fails detection itself (targets hard to find unfiltered), which is the
   worst possible regime for showing a guidance effect.

Also relevant: no verified 2023–2026 evidence ranks periphery-filter mechanisms against each
other in dynamic video — so **changing the filter is not evidence-mandated**. Matching the
existing suppression modes to the right task is.

---

## The validated stimulus + task template (hazard-perception test format)

All parameters below are from verified primary sources (2023 systematic review of 61 HP
studies, PMC10182720; TRL PPR2071 DVSA validation 2025; Sun & Hua 2019, PMC6428408;
Crundall et al. 2021 RAC Foundation report; 2025 Elsevier instrumented-vehicle paper,
S2046043025000450):

| Element | Validated spec |
|---|---|
| Clips | **12–24 short clips, ~10–16 s each** (DVSA: 14 clips / 15 hazards) — NOT one long continuous clip |
| Hazard | **Exactly one discrete, pre-identified, timestamped "developing hazard" per clip**, preceded by anticipatory clues (pedestrian stepping out, car pulling in, cyclist swerve) |
| Scoring window | **2–4.5 s window** opening when the hazard first becomes detectable; clicks outside score zero (defeats spam-clicking) |
| Metric | **Click latency from window onset** (primary) + hit/miss within window. A 16-clip/~20-min test detected a group difference at t(125)=2.85, p=.003 |
| Response | Single button press — no eye tracker needed |
| Hazard definition | Can be derived **objectively from real footage** via kinematic criteria: time headway < 2 s, critical speed (2025 Elsevier method) |
| Foveation forcing (optional) | Add a secondary identification judgment (e.g. "pedestrian or cyclist?") to force fixation of the target |
| 360° precedent | Crundall's team built **360° VR HP tests from 360-camera footage of real drives** (12 clips, naturally occurring hazards) — directly transferable to our equirect video sphere |

Dynamic video HP tests dominate the field (42/61 studies vs 9 static), reliably discriminate
driver experience, and static-frame variants correlate only weakly with them.

**Backup task format — hazard prediction (occlusion):** play the clip until the hazard just
begins to develop, cut to black, ask "What happens next?" (4-option forced choice). In direct
comparisons (Crundall & Kroll 2018; Ventsislavova et al. 2019) this discriminated safe from
less-safe drivers **better** than button-press timing, because button-press has known noise
modes (anticipators press early and score zero; skilled viewers see early but delay pressing).
Caveat: the format's inventors commercialize it (Esitu), though the comparisons are
independent peer-reviewed AAP papers.

---

## Recommended demo design (sanity check before the formal study)

1. **Footage**: 8–16 short driver-POV clips, one timestamped hazard each. Sources, in order
   of preference:
   - **Road Hazard Stimuli dataset** — 750 annotated naturalistic dashcam clips (434 hazard /
     316 no-hazard) with spatial, temporal, and categorical hazard annotations, openly shared
     (Behavior Research Methods 2023, https://link.springer.com/article/10.3758/s13428-023-02299-8).
     Flat dashcam, not equirect — project onto the sphere's forward sector (the focus window
     covers the windscreen region anyway; periphery can be letterboxed or ambient).
   - Cut our own from dashcam/360 footage using the kinematic criteria (headway < 2 s) to
     define hazard windows. DVSA/Esitu/CGI clip sets are proprietary — no public equirect HP
     set was found.
2. **Task**: "Pull the trigger as soon as you see a developing hazard." One hazard per clip,
   2–4.5 s scoring window keyed to video time, latency + hit logged. The existing
   `ProbeScheduler`/`StudyLogger` machinery already does timestamped windows + press matching
   — swap probe onsets for hazard windows.
3. **Conditions**: filter ON vs OFF, alternating clips (or ABBA), focus window pre-set on the
   road-ahead region. For the demo, use **Soft/Hard Dark** (suppression) — the hazard appears
   *inside or entering* the window while peripheral clutter is suppressed; that is the
   task-mechanism match. ColorPop's match remains the focal-probe task already specified in
   testing-strategy-v2 Block B.
4. **What "working" looks like**: mean click latency measurably earlier with filter ON, and
   subjectively — hazards feel like they "arrive sooner" when the periphery is quiet. Because
   detection is near-ceiling by design, even 2–4 self-run passes give a feelable difference if
   one exists.

---

## Caveats (state honestly)

- The specific hypothesis "dense Times-Square scenes floor out guidance effects" was **not
  directly confirmed** — the one claim asserting published support for starting with
  less-complex scenes was refuted in verification. The sparse-clip recommendation rests on
  indirect evidence (validated tests are deliberately built that way; detection engineered
  near-ceiling). Open question: window-scored hazards might still work on dense footage.
- The filter-mechanism head-to-head angle (dim vs blur vs desat vs motion damping, 2023–2026)
  produced **no surviving verified claims**. Two extracted-but-unverified leads worth reading:
  - **"Less is More! Visual Suppression for Bottom-up and Top-down Attention in Dynamic
    Environments" (CHI 2026, N=38)**: dimming outperformed blur, strong intensity beat weak,
    Dim-Strong best overall — if it holds up, direct support for Hard Dark as the demo mode.
    https://dl.acm.org/doi/10.1145/3772318.3790982
  - **"Watch out for the hazard! Blurring peripheral vision facilitates hazard perception in
    driving" (Accident Analysis & Prevention 2020)** — gaze-contingent peripheral blur improved
    hazard perception in driving clips; the clear-central/blurred-peripheral condition is
    almost exactly our focus-window geometry. (Its existence claim verified 2-0 in run 1.)
    https://www.sciencedirect.com/science/article/abs/pii/S000145752031575X
- Rusch et al. is a single 2013 simulator study (N=27); its mapping to ColorPop is analogical.
- Counterbalancing a *single self-run user* across a small clip set (practice/anticipation
  effects) has no validated recipe — validated tests assume between-subjects or large pools.
  For a sanity demo this is acceptable; don't over-read the numbers.

## Key sources (all verified primary)

- Systematic review of HP tests (61 studies, 2023): https://pmc.ncbi.nlm.nih.gov/articles/PMC10182720/
- TRL PPR2071 DVSA CGI-clip validation (scoring windows, anti-cheat, near-ceiling detection): https://www.trl.co.uk/uploads/trl/documents/PPR2071-Validation-of-CGI-hazard-perception-clips.pdf
- Crundall et al. 2021, RAC Foundation (360° VR HP test; hazard-prediction format): https://www.racfoundation.org/research/safety/a-comparison-of-virtual-reality-and-non-virtual-reality-approaches-to-hazard-perception-training-testing
- Sun & Hua 2019 (clip/window parameters): https://pmc.ncbi.nlm.nih.gov/articles/PMC6428408/
- Kinematic hazard criteria from instrumented-vehicle footage (2025): https://www.sciencedirect.com/science/article/pii/S2046043025000450
- Rusch et al. 2013 (conspicuity ceiling): https://pmc.ncbi.nlm.nih.gov/articles/PMC3891797/
- Road Hazard Stimuli dataset (750 open annotated clips): https://link.springer.com/article/10.3758/s13428-023-02299-8
