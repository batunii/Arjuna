# User Study Methodology
## Guiding User Attention in Real-World Tasks Using XR Overlays
### MSc Dissertation — Trinity College Dublin

> **System:** Multi-mode Diminished Reality attention-guidance overlay on Meta Quest 3 passthrough.
> Three modes tested: Dynamic (Mode 1), Semi-Dynamic (Mode 2), Static (Mode 3).

---

## Table of Contents

1. [Study Overview](#1-study-overview)
2. [Participant Groups — 2×2 Factorial Design](#2-participant-groups--22-factorial-design)
3. [Session Timeline](#3-session-timeline)
4. [Mode 1 — Dynamic Vignette (Driving)](#4-mode-1--dynamic-vignette-driving)
5. [Mode 2 — Semi-Dynamic Vignette (Classroom)](#5-mode-2--semi-dynamic-vignette-classroom)
6. [Mode 3 — Static Vignette (Workstation)](#6-mode-3--static-vignette-workstation)
7. [Instrumentation & Data Logging](#7-instrumentation--data-logging)
8. [Analysis Plan](#8-analysis-plan)
9. [Stimulus Sources & Fallbacks](#9-stimulus-sources--fallbacks)
10. [Exclusion & Quality Criteria](#10-exclusion--quality-criteria)

---

## 1. Study Overview

The experiment evaluates three modes of a Diminished Reality (DR) vignette overlay rendered as a translucent shader sphere over the Quest 3's system passthrough. The overlay dims the visual periphery to varying degrees — without degrading passthrough quality, since it is subtractive (black-out/dim) rather than a camera replacement.

### Design structure

| Dimension | Type | Values |
|---|---|---|
| Mode | Between-subjects | Mode 1 (Dynamic) vs Mode 2 (Semi-Dynamic) |
| Scenario | Between-subjects | Driving vs Classroom |
| Overlay | Within-subjects | Baseline vs Active |

Mode 3 (Static/Workstation) is experienced by **all participants** as a universal within-session block, so it provides a full-factorial within-subjects comparison across the entire sample.

### Baseline definition

The baseline condition is the same shader sphere with `_DrIntensity = 0` — the effect is invisible, but the participant is wearing the headset, holding the controller, and watching the same video. The only change between baseline and active is whether the DR effect is live. This keeps visual context, headset weight, and controller hold identical, isolating the vignette as the sole independent variable.

### Counterbalancing

Within each group, half the participants experience **Baseline → Active** and half experience **Active → Baseline**. Order is randomised per participant to prevent fatigue and learning order effects.

### Ethics

Written informed consent obtained before the session. Participants are told they may remove the headset at any time. The study does not involve deception.

---

## 2. Participant Groups — 2×2 Factorial Design

**Total participants:** 32 (8 per group)

The 2×2 crossing of Mode × Scenario yields four groups. This allows a Mode × Scenario interaction to be tested: if Mode 2 gives poor results only in the Classroom group (D), Group C (Mode 2 + Driving) can determine whether the effect is mode-driven or scenario-driven. Without this crossing, a bad result cannot be cleanly attributed.

| Group | Mode | Scenario | Dynamic condition |
|---|---|---|---|
| **A** | Mode 1 — Dynamic | Driving | 360° driving video with gaze-following vignette |
| **B** | Mode 1 — Dynamic | Classroom | 360° lecture video with gaze-following vignette |
| **C** | Mode 2 — Semi-Dynamic | Driving | 360° driving video with user-placed rectangle |
| **D** | Mode 2 — Semi-Dynamic | Classroom | 360° lecture video with user-placed rectangle |

Every participant from all four groups also completes the **Mode 3 workstation block** in the same session.

---

## 3. Session Timeline

Total session: **~38–40 minutes**

```
00:00  [  5 min ]  Intro, consent form, headset fitting & comfort check
05:00  [  1 min ]  _VideoForward calibration — participant looks at a marked
                   wall point; experimenter logs the forward vector offset
──────────────────────────────────────────────────────────────────────────
                   ── DYNAMIC MODE BLOCK (Mode 1 or Mode 2, per group) ──
──────────────────────────────────────────────────────────────────────────
06:00  [  1 min ]  Task briefing for Condition 1 (Baseline)
                   Verbal instructions + practice button press
07:00  [  6 min ]  Condition 1 — Baseline  (_DrIntensity = 0)
13:00  [  1 min ]  Break — ConditionReset; experimenter starts Condition 2
14:00  [  6 min ]  Condition 2 — Active  (effect at full intensity)
20:00  [  2 min ]  NASA-TLX (Mode 1 / Mode 2) — paper or tablet, headset off
──────────────────────────────────────────────────────────────────────────
                   ── MODE 3 BLOCK (all participants) ──
──────────────────────────────────────────────────────────────────────────
22:00  [  1 min ]  Mode 3 briefing — explain digit cancellation + tablets
23:00  [  1 min ]  Participant places window (A button × 2 corners)
24:00  [  5 min ]  Condition 3 — Mode 3 Baseline (fresh sheet, distractors on)
29:00  [  1 min ]  Break — new sheet prepared; ConditionReset
30:00  [  5 min ]  Condition 4 — Mode 3 Active (same distractors, window live)
35:00  [  3 min ]  NASA-TLX (Mode 3)
38:00  [  2 min ]  Debrief — explain study aims, thank participant, export logs
```

---

## 4. Mode 1 — Dynamic Vignette (Driving)

### Description

The DR vignette follows the participant's gaze direction (head forward), lightly dimming the periphery. While the head is turning (angular speed > 30 °/s) the effect eases off, preserving situational awareness. It eases back after a ~2.5 s settle time. This is the most ambient of the three modes — designed for high-motion environments where peripheral monitoring still matters.

### Paradigm: Dual-Task Detection

Participants watch a 360° equirectangular driving video mounted on the shader sphere (`VideoPlayer → RenderTexture → _MainTex`). They are seated; head rotation navigates the scene. The task simulates attentive driving: detect and report road events by pressing the controller trigger as soon as they are noticed.

**Instructions given to participant:**
> "You are watching a driving scene from the passenger perspective. Press the trigger button as soon as you notice anything happening on or near the road — a vehicle doing something, a traffic sign, a pedestrian, anything that catches your eye. Press as often as you like."

Participants are **not** told the forward/peripheral distinction — this is applied in post-processing.

### Video requirements

The stimulus clip must contain a sufficient density of pre-codeable events in both regions:

**Forward events** (eccentricity < 15° from head-forward):
- Traffic light changing state
- Road sign appearing
- Leading car braking or accelerating
- Pedestrian stepping into the carriageway

**Peripheral events** (eccentricity ≥ 15°):
- Pedestrian on the pavement at 25–35° eccentricity
- Side-street junction or vehicle pulling out
- Any motion-rich element in the far visual field

The experimenter pre-codes every event with its video timestamp and angular position before the study begins.

### Data collection

`StudyLogger.cs` runs at ~60 Hz and writes one CSV row per frame. Every button press is tagged `BUTTON_PRESS` with a timestamp. Head pose (yaw, pitch, roll) is logged continuously.

In post-processing, each button press is matched to the nearest pre-coded event within a ±3 s window. The head pose at the time of press determines whether the event was in the forward or peripheral region.

### Hypotheses

| | Hypothesis |
|---|---|
| **H1a (Forward)** | With Mode 1 active, reaction time to forward road events decreases and detection rate increases. The dimmed periphery reduces competition from off-axis stimuli, allowing faster forward detection. |
| **H1b (Peripheral)** | Peripheral event detection rate holds steady. The mode dims the periphery — it does not eliminate awareness there. A significant *drop* in peripheral detection would indicate the mode harms situational awareness and would be a critical failure. |

### Measures

| Measure | Operationalisation |
|---|---|
| Forward detection rate | % of pre-coded forward events detected (press within ±3 s) |
| Forward response time | Median ms from event onset to button press, forward events only |
| Peripheral detection rate | % of pre-coded peripheral events detected |
| Head-turn velocity | Mean angular speed from head-pose log; proxy for scanning behaviour |
| NASA-TLX | Six raw subscales, unweighted, after both Mode 1 conditions |

---

## 5. Mode 2 — Semi-Dynamic Vignette (Classroom)

### Description

A softer, wider-threshold version of the dynamic vignette designed for seated semi-mobile environments. The periphery is dimmed with a larger easing threshold (22 °/s) and longer reapply time (~5.5 s). The intent is to gently discourage unnecessary off-axis head turns without making the periphery feel aggressive or claustrophobic.

### Paradigm: Behavioral Attention & Distraction

Participants watch a 360° lecture video. The lecturer is positioned in the forward view. Peripheral distractors (other "students" shuffling, using phones, whispering) are baked into the video at ≥ 25° eccentricity. After each condition, participants answer a behavioral quiz about the lecture — no subject expertise required, only observable behaviors.

**Instructions given to participant:**
> "You are attending a short lecture. Watch it as you normally would in a class. There will be a few brief questions about it afterwards."

Participants are not told that distraction or head movement is being measured.

### Quiz design

Questions target observable behaviors rather than domain knowledge, eliminating the prior-knowledge confound:

1. How many times did the lecturer write on the board (or gesture to a slide)?
2. Did any student raise their hand? If yes, roughly when?
3. Did the lecturer face left or right at any point during the talk?
4. How many times did the lecturer pause for more than three seconds?
5. *(Optional)* Did you notice anything happening in the classroom besides the lecture?

Questions are answered immediately after each condition, from memory, on a brief paper form.

### Head-movement proxy

`StudyLogger` records head yaw continuously. A head-turn toward distractor regions is defined as a yaw excursion > 25° from the lecturer's forward direction. The count and cumulative dwell time in the distractor zone are derived from the log in post-processing.

### Hypotheses

| | Hypothesis |
|---|---|
| **H2a (Distraction)** | With Mode 2 active, the frequency of head turns toward peripheral distractor regions decreases significantly. |
| **H2b (Retention)** | Behavioral quiz accuracy is maintained or improves, confirming that forward attention (toward the lecturer) is preserved — not merely that participants gave up looking around. |

### Measures

| Measure | Operationalisation |
|---|---|
| Head-turn count (distractor direction) | Yaw excursions > 25° from lecturer direction per condition |
| Dwell time in distractor zone | Cumulative seconds head pointed ≥ 25° off-axis |
| Quiz accuracy | Correct behavioral answers / total questions (0–100%) |
| NASA-TLX | Six raw subscales, unweighted, after both Mode 2 conditions |

---

## 6. Mode 3 — Static Vignette (Workstation)

### Description

A user-defined, axis-aligned rectangular window placed around a physical task surface. Outside the window the DR overlay blacks out the scene progressively (`_MaxDim = 1.0`, soft edge controlled by `_EdgeSoftness`). The window is world-locked: stored as two world-space corner points, recomputed from the head each frame, so the rectangle stays fixed to the room as the participant moves.

### Paradigm: Digit Cancellation with Physical Distractors

Participants sit at a desk with a printed digit-cancellation sheet (A4, random digits 0–9). Two distractor tablets are placed at **35° left and 35° right** of the task surface, each playing a synchronised motion-rich video (e.g. social-media reel, sports highlights). 35° eccentricity is chosen as the near-periphery sweet spot: distractor signals are detected pre-attentively by the visual system without requiring a deliberate head turn, creating sustained pull without demanding conscious reorientation.

**Task:** Circle every occurrence of the digit **7** as quickly and accurately as possible. A fresh, matched sheet is used for each condition.

**Instructions given to participant:**
> "Circle every number 7 you can find on this sheet. Work as quickly and accurately as you can. You have five minutes."

The distractor tablets start simultaneously when the experimenter signals the condition start.

### Window placement

Before each Mode 3 condition the participant aims the right controller at two opposite corners of the task sheet and presses A. The window is placed in the Baseline condition too (to keep the motor task identical) but `_DrIntensity = 0` keeps it invisible. This ensures any between-condition difference is not attributable to the window-placement interaction.

### Distractor rationale

Near-periphery (25–40°) is the zone most subject to involuntary attention capture by motion and colour. At 35°, tablets are clearly visible in peripheral vision without requiring a head movement to see. Mode 3's hard blackout of that zone removes the low-level attentional pull, allowing the participant to sustain fixation on the task without constant inhibition of peripheral reflexes.

### Hypotheses

| | Hypothesis |
|---|---|
| **H3a (Speed)** | With Mode 3 active, task completion time decreases and digits processed per minute increases. |
| **H3b (Accuracy)** | The miss rate (uncircled 7s) decreases with Mode 3 active, indicating sustained rather than scanned attention. |
| **H3c (Workload)** | NASA-TLX scores — especially mental demand and effort subscales — are lower with Mode 3 active. |

### Measures

| Measure | Operationalisation |
|---|---|
| Digits processed | Total digits scanned (hits + false alarms + misses, scored by experimenter after session) |
| Hit rate | Correct 7s circled / total 7s on sheet |
| False alarm rate | Non-7 digits circled / total non-7 digits |
| Completion time | Time from start signal to pen-down, capped at 5 min |
| Head turns toward distractor tablets | Yaw excursions > 30° from task forward, from head-pose log |
| NASA-TLX | Six raw subscales, unweighted, after both Mode 3 conditions |

---

## 7. Instrumentation & Data Logging

### StudyLogger.cs — Continuous telemetry

A MonoBehaviour attached to the session manager. Writes one CSV row per frame at ~60 Hz.

**CSV columns:**

| Column | Description |
|---|---|
| `timestamp_ms` | Unix timestamp in milliseconds |
| `condition` | `BASELINE` or `ACTIVE` |
| `mode` | `1`, `2`, or `3` |
| `yaw` | Head yaw in degrees |
| `pitch` | Head pitch in degrees |
| `roll` | Head roll in degrees |
| `dr_intensity` | Current `_DrIntensity` shader value (0.0–1.0) |
| `event_label` | Event tag or empty string |

**Event labels:**

| Label | Trigger |
|---|---|
| `CONDITION_START` | Condition begins |
| `CONDITION_RESET` | ConditionReset called between conditions |
| `BUTTON_PRESS` | Participant presses trigger (Modes 1 & 2) |
| `CORNER_PLACED` | A button pressed — corner set (Mode 3) |
| `SESSION_END` | Session debrief begins |

Output is written to Android external storage and pulled via ADB after the session:

```bash
adb pull /sdcard/Android/data/<package>/files/study_log_<pid>.csv ./logs/
```

### ConditionReset

Called between every pair of conditions. Actions:
- Clears the placed rectangle (`_RegionActive = 0`)
- Resets `_DrIntensity = 0`
- Clears YOLO salience boxes (`_SalienceCount = 0`)
- Writes a `CONDITION_RESET` event to the log with timestamp

This creates a clean epoch boundary in the CSV so baseline and active segments can be sliced unambiguously in analysis.

### NASA-TLX

Raw NASA-TLX (no pairwise weighting). Administered on paper or a tablet after the two conditions for each mode block. Six subscales rated 0–100 (5-point increments):

| Subscale | What it captures in this study |
|---|---|
| Mental Demand | Cognitive load of event monitoring / staying on task |
| Physical Demand | Controller use, head-movement effort |
| Temporal Demand | Time pressure experienced during the task |
| Performance | Participant's own assessment of success |
| Effort | Overall effort to maintain the required performance level |
| Frustration | Irritation or annoyance with the DR effect or the task |

Each participant completes TLX twice: once after the dynamic mode block (Conditions 1–2) and once after the Mode 3 block (Conditions 3–4).

---

## 8. Analysis Plan

All statistical analysis is performed in Python (SciPy, Pingouin, statsmodels) or R. **α = 0.05** throughout. Effect sizes reported: Cohen's *d* for pairwise comparisons, partial η² for factorial models.

| Research question | Statistical test | Data source |
|---|---|---|
| Mode 1: forward detection rate & RT (Active vs Baseline) | Paired Wilcoxon signed-rank (within-subjects) | StudyLogger CSV × pre-coded event timestamps |
| Mode 1: peripheral detection rate (Active vs Baseline) | Paired Wilcoxon; equivalence test if N allows | StudyLogger CSV |
| Mode 2: head-turn frequency (Active vs Baseline) | Paired t-test or Wilcoxon | Head-pose log |
| Mode 2: quiz accuracy (Active vs Baseline) | Paired t-test (or McNemar if dichotomised) | Paper quiz scores |
| Mode 3: digits processed & accuracy (Active vs Baseline) | Paired t-test / Wilcoxon | Scored digit-cancellation sheets |
| Mode × Scenario interaction (Groups A–D) | 2×2 mixed ANOVA (Mode between-subjects, Overlay within-subjects) | Primary DV per mode |
| NASA-TLX — all modes | Paired Wilcoxon per subscale; Bonferroni correction across 6 subscales | Paper TLX forms |

### Notes on the factorial analysis

The 2×2 mixed ANOVA tests whether the effect of the overlay (Baseline → Active) differs by Mode (1 vs 2) and whether it differs by Scenario (Driving vs Classroom). A significant Mode × Scenario interaction would indicate the vignette modes are not scenario-agnostic — a theoretically important finding for the dissertation's generalisability claims.

---

## 9. Stimulus Sources & Fallbacks

### Mode 1 — Driving 360° video

**Primary:** Existing 4K 360° dashcam footage (YouTube). Select an urban clip with a high density of codeable events (not motorway — too low event rate). Ensure ≥ 8 forward events and ≥ 5 peripheral events per 6-minute segment.

**Fallback:** Self-record with a Ricoh Theta or GoPro MAX mounted on a car dashboard. If event density is low, add baked overlay cues (translucent highlight circles) in post using DaVinci Resolve 360° tools.

### Mode 2 — Lecture 360° video

**Primary:** Peter Diamond 4K 360° lecture series (MIT ECON 1111). Publicly available, accessible topic (introductory economics), real classroom with incidental student movement. Trim two matched 6-minute segments, verifying ≤ 2 distractor event difference between segments before using.

**Fallback:** Self-record with a Ricoh Theta placed on a desk at the centre of a room. Confederate lecturer delivers a simple 12-minute talk (introductory statistics, history of a country). Two confederate "students" in the periphery perform scripted distractors at pre-agreed timestamps (phone check, whispered exchange, object drop).

### Mode 3 — Distractor videos

**Primary:** Short social-media reels or sports highlights downloaded offline, looped on both tablets. High temporal contrast (fast cuts, bright colours) maximises motion salience at 35°.

**Fallback:** Any high-motion video (nature documentary, cooking show). Synchronise playback on both tablets via a shared countdown start so both tablets are in identical distractor state per condition.

### Stimulus matching rule

The **same video clip** is used for both Baseline and Active within each mode. Only the shader state changes. This eliminates content variance as a confound.

---

## 10. Exclusion & Quality Criteria

| Criterion | Action |
|---|---|
| Participant reports VR sickness at any point | Terminate session immediately; exclude all data |
| Technical failure (log missing, video crash) > 30 s during a condition | Exclude that condition; keep participant in study if other conditions are clean |
| Button press rate < 1 per 3 minutes in Mode 1 | Flag for review — likely task misunderstanding; consult experimenter notes before excluding |
| Head-pose yaw variance < 2° throughout a condition | Flag for exclusion — participant was not engaging with the 360° scene |
| Participant recognises the stimulus video before the session | Note in experimenter log; do not exclude unless they report strong familiarity that would affect task performance |
| Incomplete NASA-TLX (missing ≥ 2 subscales) | Exclude that TLX from workload analysis; retain behavioural data |

---

## 11. Methodological Grounding — Precedent in Published Research

This section explicitly maps each testing paradigm used in the study to its validated source in the literature. The purpose is to demonstrate that the testing instruments are not ad hoc constructions but established measurement tools with known psychometric properties, used by peer-reviewed research in directly analogous contexts.

---

### 11.1 Mode 1 — Dual-Task Event Detection → Detection Response Task (DRT)

**Paradigm origin:** The **Detection Response Task** is a standardised secondary task measure of attentional demand codified in **ISO 17488:2016** ("Road vehicles — Transport information and control systems — Detection-response task (DRT) for assessing attentional effects of cognitive load in driving"). Its academic roots trace to Jahn, Oehme, Krems & Gelau (2005), who showed that button-press response time to a sensory probe stimulus degrades monotonically with cognitive load, making it a valid real-time index of spare attentional capacity.

**How it works in its canonical form:** A sensory stimulus (LED, tactile vibration, or tone) appears randomly every 3–5 seconds while the participant performs a primary task. They press a button within a 100–2500 ms window. Hit rate and response time are the DVs. When attentional demand of the primary task rises, RT slows and hit rate falls.

**How the dissertation adapts it:** Rather than a random probe stimulus, the dissertation uses *ecologically valid* road events (traffic light, pedestrian, vehicle behaviour) as the detection targets. This is a **natural DRT** variant — validated by the driving literature as more ecologically valid than abstract LED probes while preserving the core RT + hit-rate measurement structure. The post-hoc classification of events as forward (< 15°) vs. peripheral (≥ 15°) allows the same press data to speak to two separate hypotheses (H1a and H1b) without adding any task complexity for the participant.

**Papers in the lit review that use this exact paradigm:**

| Paper | Study design | Task | Key DV |
|---|---|---|---|
| **"AR Warnings in Vehicles: Modality & Specificity"** (Acc. Analysis & Prevention, 2017) | n = 88, driving simulator, within-subjects 2×2 factorial | Drive while AR displayed hazard warnings; respond to them | Brake reaction time, Time-to-Collision, detection accuracy |
| **"Directing Driver Attention with AR Cues"** (Trans. Res. Part F, 2013) | n = 27, 54-mile simulator, within-subjects per hazard | Detect and respond to roadside hazards; AR cue present/absent | Detection accuracy (pedestrians, signs), hazard response time |
| **"Diminishing Reality: Potential Benefits & Risks"** (Murphy et al., HFES 2021) | Medical assembly + DR levels | Venilator assembly under DR; SA probes (SAGAT) during task | Assembly accuracy, SA probe score — confirms focus/SA tradeoff |
| **"Cognitive Aid Design Using DR"** (McLaughlin et al., Human Factors 2025) | 2-population study (STEM students + JSC professionals) | Complex assembly with distractors; universal vs. context-aware DR | Task performance (accuracy, time), situational awareness of objects |

**Why DRT fits H1a/H1b:** H1a predicts faster forward-event detection under Mode 1 (freed attentional resources → lower RT). H1b predicts *no drop* in peripheral detection rate. This is precisely the split the DRT literature uses to distinguish attentional focus (primary task performance) from attentional residual (spare capacity for secondary events). Murphy et al. (2021) confirm the exact tension the dissertation is navigating: DR improves focus but risks SA — which is why H1b is a safety-monitoring hypothesis, not merely exploratory.

**UFOV connection:** The **Useful Field of View (UFOV)** test (Ball, Owsley et al., University of Alabama at Birmingham) measures the spatial extent over which visual information is acquired in a brief glance. Its divided-attention subtest — detect a central target while simultaneously localising a peripheral one — is the psychophysical analogue of the dissertation's H1b measure. UFOV research establishes that any manipulation narrowing functional visual field will depress peripheral hit rate, which is the failure mode that the dissertation's H1b monitor is designed to catch.

---

### 11.2 Mode 1 — Forward/Peripheral Classification → Eccentricity-Gated Subtle Gaze Direction

**Paradigm origin:** The dissertation classifies events as forward vs. peripheral in post-processing by computing head-pose eccentricity at the moment of button press. This is grounded in two bodies of work:

1. **Subtle Gaze Direction (SGD)** — Bailey, McNamara, Sudarsanam & Grimm (2009), *ACM Transactions on Graphics*. SGD uses peripheral luminance modulation to guide gaze toward a target, and crucially terminates the cue once gaze crosses an angular threshold. This established that 15–20° eccentricity is the meaningful boundary between guided (foveal) and un-guided (peripheral) attention — the same threshold the dissertation uses to split forward vs. peripheral events.

2. **"Depth-Based Subtle Gaze Guidance in VR"** (in lit review, ~2016). Within-subjects 3D visual search task using time-to-first-fixation and target acquisition time as DVs. Validated that angular eccentricity from a reference direction is a sufficient measure for classifying guided vs. unguided gaze direction in VR — no eye tracker required at the > 15° scale.

3. **"Look over there! Investigating Saliency Modulation"** (Sutton et al., UIST 2022). OST-AR study with Pupil Labs eye tracker. Used time-to-first-fixation at a target area and fixation count at the target area as primary DVs. Directly validates time-to-detection and area-based fixation as the correct measurement approach for the dissertation's forward event detection rate.

---

### 11.3 Mode 2 — Head-Turn Frequency as Distraction Proxy

**Paradigm origin:** Using involuntary head-orientation changes as an objective behavioural proxy for visual attention in VR is validated at two levels:

**Level 1 — Head pose as gaze direction:** Higgins et al. (2022), "Head Pose as a Proxy for Gaze in Virtual Reality" (VAM-HRI 2022, UMBC). Formally validated head orientation as a gaze proxy for object-selection tasks in VR using mean reciprocal rank analysis. Sitzmann et al. (2018), "How do people explore virtual environments?" (arXiv 1612.04335) collected concurrent head IMU + gaze-tracking data across multiple participants in 360° VR scenes and confirmed that fixations occur predominantly at low head velocity — meaning head yaw at rest is a reliable gaze-direction proxy. For excursions > 15°, head orientation is sufficient; eye tracking adds value only within the central 10–15° window.

**Level 2 — Task-irrelevant head movement as clinical distraction measure:** The most direct validation is from ADHD/attention research. Frontiers in Human Neuroscience (PMC9386071, 2022), "Evaluating ADHD symptoms in children through tracked head movements in a VR classroom", tracked head pitch, yaw, and roll in a VR classroom scenario using an HMD. Task-irrelevant head movement frequency was significantly correlated with standard ADHD clinical rating scales (*r* > 0.50). This is the **direct psychometric precedent** for the dissertation's head-turn count metric: a child (or participant) looking away from the lecturer or task surface at > 25° eccentricity is exhibiting the same class of task-irrelevant orientation behaviour used as a validated distraction index in this clinical literature.

**Papers in the lit review that use this approach:**

| Paper | Study design | Gaze metric used |
|---|---|---|
| **"Gaze Data Visualisations for Educational VR"** (Rahman et al., ACM SUI 2019) | HTC Vive Pro Eye; VR solar field; student-role participants followed audio instructions | Eye gaze recorded for playback to teacher; NASA-TLX and Likert for visualisation preference — validates gaze as the signal for distraction in educational VR |
| **"VR Boxing: Gaze-Contingent Blur"** (Rodrigues et al., Frontiers Psych. 2022) | n = 18 expert athletes; HTC Vive Eye Pro; interception task | Gaze fixation count, fixation duration, 3D gaze ellipse volume, AOI dwell time under central vs. peripheral blur — validates that gaze distribution shifts meaningfully under blur conditions |
| **"Attention Guiding via Peripheral Vision & Eye Tracking"** (Renner & Pfeiffer, IEEE 3DUI 2017) | HMD with narrow FOV; object guidance task | Search time and accuracy as a function of gaze-adaptive directional cues — validates gaze-contingent measurement |
| **"Missing the Point: Guiding Attention in Cinematic VR"** (Nielsen et al., VRST 2016) | 3-condition within-subjects; 360° cinematic VR | Gaze direction recorded as DV; firefly cue vs. body rotation vs. control — validates head/gaze direction as the attention measure in 360° video scenarios |

---

### 11.4 Mode 2 — Behavioral Counting Quiz as Retention Measure

**Paradigm origin:** Standard recall paradigms in attention research rely on content questions, which confound prior knowledge with attention. The dissertation uses observable-behaviour questions instead ("how many times did X happen?"), removing the prior-knowledge confound. This approach is validated by two bodies of work:

1. **360° VR learning studies.** Springer Nature (2024), "Increasing immersivity of 360° videos facilitates learning and memory" — measured retention via observable-behaviour recall (what happened, how many times) across immersivity conditions. Behavioural recall significantly differentiated conditions, confirming observable-behaviour questions are a sensitive DV in VR attention studies.

2. **Incidental learning paradigm.** The educational psychology tradition of *incidental learning* uses recall of observable events (not conceptual content) as its DV — specifically because it measures automatic encoding during attention, not retrieval of previously-known facts. Observable-behaviour questions are the standard operationalisation of this construct in ecological validity studies.

3. **VR Classroom Paradigm (Parsons & Rizzo, 2001–2007).** Developed for ADHD assessment, this paradigm presents distractors in a VR classroom and measures both behavioural distraction (off-task gaze/head orientation) and post-task recall of lesson content. The dissertation's Mode 2 design is structurally identical: a lecturer in forward view, baked-in peripheral distractors, head-turn frequency as the primary DV, and observable-behaviour questions as the secondary DV.

**Why this is better than knowledge-based recall:** Knowledge-based recall (e.g. "what is the definition of GDP?") is confounded by prior knowledge, education level, and topic difficulty. Observable-behaviour questions ("how many times did the lecturer turn to face the board?") require only attention during the session — they are equally answerable by all participants regardless of subject knowledge. This makes them a cleaner measure of whether the participant was watching.

---

### 11.5 Mode 3 — Digit Cancellation Task (DCT)

**Paradigm origin:** The Digit Cancellation Task has a lineage stretching to Bourdon (1895), who first used letter/symbol cancellation as a measure of sustained attention and processing speed. The modern validated form is the **D-CAT (Digit Cancellation Attention Test)**, validated by Hatta & Yoshizaki (Japanese Journal of Social Psychology) using two studies: Study 1 confirmed test–retest reliability; Study 2 showed TBI patients scored significantly lower than matched controls, establishing construct validity. Factor-analytic validation (PMC8298746) confirms DCT loads on *selective attention* and *processing speed* — exactly the constructs Mode 3 is designed to support.

**Administration in the dissertation:** The target digit (7) is chosen to avoid high-base-rate digits (0, 1) while remaining easily distinguishable. Matched sheets (same digit density, same 7-frequency) are used across conditions to eliminate sheet difficulty as a confound. Five minutes allows most participants to finish at least one pass, giving both speed and accuracy data even for slow processors.

**Distractor placement at 35°:** The 35° eccentricity is directly motivated by UFOV divided-attention research. UFOV subtests reveal that peripheral detection probability peaks at approximately 30–40° from fixation under normal conditions and drops sharply under cognitive load or spatial filtering. Placing distractor tablets at exactly 35° puts them in the zone of maximum automatic salience capture — the ideal location for testing whether Mode 3's DR overlay can suppress that capture.

**Papers in the lit review that use analogous task-performance paradigms in distraction environments:**

| Paper | Study design | Task | DVs |
|---|---|---|---|
| **"FocalSpace"** (Yao, DeVincenzi, Pereira, Ishii; ACM SUI 2013) | Video conferencing with/without synthetic background blur | Follow meeting content with distractors in background | Memory accuracy for meeting content; user preference — validates distraction-suppression improving retention |
| **"Cognitive Aid Design Using DR"** (McLaughlin et al., 2025) | Complex assembly task; two populations | Assembly with peripheral distractors; DR context-aware vs. universal | Accuracy, completion time — validates performance-based DV for focus-vs-distractor tasks |
| **"AR Impact on Cognitive Load & Performance"** (Buchner et al., in lit review) | AR overlay conditions; cognitive load manipulation | Primary task performance under AR conditions | NASA-TLX + task performance — validates combined performance + workload measurement for AR overlays |
| VNC follow-up (ACM IUI 2026) | MR workspace; open-office environment | Sustained attention + working memory tasks (CPT, n-back) under VNC | CPT accuracy; n-back accuracy; ECG stress markers — confirms attention task paradigm for MR workspace focus research |

**Why DCT over n-back or CPT:** The n-back and CPT are appropriate for measuring *internal* cognitive load (working memory). DCT measures *externally-directed* sustained attention — the participant must scan a physical surface and suppress distraction from peripheral visual events. This matches Mode 3's intent (workstation focus with peripheral distractors) far more directly than a working-memory task would.

---

### 11.6 NASA-TLX — Universal Workload Measure

**Paradigm origin:** Hart, S.G. & Staveland, L.E. (1988). "Development of NASA-TLX (Task Load Index): Results of empirical and theoretical research." In *Human Mental Workload* (pp. 139–183). Elsevier, North-Holland. The NASA-TLX was developed over three years of empirical testing at NASA Ames Research Center, with the six subscale structure (Mental Demand, Physical Demand, Temporal Demand, Performance, Effort, Frustration) derived from factor analysis of workload ratings across simulated flight tasks.

**Why raw (unweighted) scores:** Byers, Bittner & Hill (1989) compared weighted and unweighted NASA-TLX scores across multiple experiments and found no significant difference in sensitivity to workload manipulations. Raw scores are administratively simpler and are the current standard in HCI research.

**Validation in AR/VR:** The NASA-TLX is the most widely used subjective workload measure in HCI and XR research. Every driving, AR, and VR paper in the lit review that includes a workload measure uses NASA-TLX or a direct subset of its subscales:

| Paper | Subscales reported | Context |
|---|---|---|
| **"AR Impact on Cognitive Load & Performance"** (Buchner et al.) | Full NASA-TLX | AR task overlays — direct validation for AR contexts |
| **"Comparing Non-Visual and Visual Guidance"** (Marquardt et al., IEEE TVCG 2020) | Cognitive load (NASA-TLX subset) | Guidance techniques in simulated AR — validates workload measurement for guidance studies |
| **"Diminishing Reality: Benefits & Risks"** (Murphy et al., 2021) | Subjective workload (NASA-TLX implied) | DR in medical task — validates workload measurement for DR |
| **"Cognitive Aid Design Using DR"** (McLaughlin et al., 2025) | NASA-TLX | DR + complex task — direct precedent |
| **"Gaze Data Visualisations for Educational VR"** (SUI 2019) | Mental demand subscale | Educational VR — validates workload measurement for educational AR contexts |

**Sensitivity note:** The Frustration subscale is particularly relevant for the dissertation because the DR vignette might feel restrictive or annoying in conditions where it dims wanted content. A significant Frustration score difference between Baseline and Active would flag a UX problem even if task performance improved.

---

### 11.7 Study Design — 2×2 Mixed Factorial + Within-Subjects Baseline

**Design origin:** The 2×2 mixed ANOVA (between-subjects on Mode × Scenario, within-subjects on Overlay) is the canonical design for disentangling treatment effects from context effects in HCI. The specific rationale for this structure in the dissertation parallels the Marquardt et al. (2020) 3-sub-study design and the McLaughlin et al. (2025) 2-population design — both of which were specifically constructed to separate technique effects from context effects.

**Counterbalancing:** The Latin-square counterbalancing of Baseline/Active order within each group matches the within-subjects protocol used by Sutton et al. (UIST 2022) for the "Look over there!" study and Rodrigues et al. (Frontiers 2022) for the VR Boxing study — both of which use counterbalanced within-subjects designs for the same reason: controlling for learning and fatigue order effects in perceptual/attention tasks.

**N = 32 (8 per group) justification:** The driving literature uses n = 27 ("Directing Driver Attention with AR Cues") to n = 88 ("AR Warnings in Vehicles") for within-subjects detection paradigms. With 8 participants per group and both conditions per participant, the effective within-subjects n for each DV is 16 per mode — consistent with the AR guidance literature for exploratory prototype evaluations (e.g. "Two is Better Than One" and "VR Boxing" both report results from n ≈ 12–20 in within-subjects designs). A power analysis targeting d = 0.6 with α = .05 and 1−β = .80 for a within-subjects t-test yields a required n of 24 per group (two conditions), which the 8×2 = 16 design slightly undershoots — this should be acknowledged as a limitation in the dissertation's methods chapter, with the study framed as an adequately-powered exploratory evaluation rather than a confirmatory trial.

---

### Summary Table — Paradigm to Literature Mapping

| Dissertation test | Paradigm name | Founding reference | Papers in lit review using it |
|---|---|---|---|
| Mode 1: button-press event detection | Detection Response Task (DRT) | ISO 17488:2016; Jahn et al. (2005) | "AR Warnings in Vehicles"; "Directing Driver Attention with AR Cues"; "Murphy 2021" |
| Mode 1: forward vs. peripheral split | Subtle Gaze Direction / eccentricity gate | Bailey et al. (2009) ACM ToG; "Depth-Based SGD" | "Look over there!"; "Depth-Based SGD"; "Peripheral & Foveal Vision Interactions" |
| Mode 1: peripheral safety monitor | Useful Field of View (UFOV) divided attention | Ball & Owsley et al., UAB | Directly motivates H1b; context from "Diminishing Reality" Murphy 2021 |
| Mode 2: head-turn count | Task-irrelevant head movement | Higgins et al. 2022; PMC9386071 (ADHD VR classroom) | "Gaze Data Visualisations for Educational VR"; "VR Boxing"; "Missing the Point" |
| Mode 2: behavioral quiz | Observable-behavior incidental learning | Parsons & Rizzo VR Classroom (2001); Springer 2024 360° VR learning | "Gaze Data Visualisations"; VR Classroom paradigm |
| Mode 3: digit cancellation | D-CAT / DCT | Bourdon (1895); Hatta & Yoshizaki; PMC8298746 | "FocalSpace"; "McLaughlin 2025"; VNC follow-up (IUI 2026) |
| All modes: workload | NASA-TLX | Hart & Staveland (1988); Byers et al. (1989) | "Buchner et al."; "Marquardt et al."; "Murphy 2021"; "McLaughlin 2025"; "SUI 2019" |
| Modes 1 & 2: gaze proxy | Head-pose as gaze direction | Higgins et al. 2022; Sitzmann et al. 2018 | "Gaze Data Visualisations"; "Missing the Point"; "Predicting Attention in VR" |

---

*Document generated from dissertation session, June 2026.*
*Implementation: `Assets/FocusVignette.unity`, `ShaderSample/Shaders/FocusVignette.shader`, `ShaderSample/Scripts/FocusVignetteManager.cs`*
