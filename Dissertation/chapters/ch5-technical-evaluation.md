# Chapter 5 — Technical Evaluation: Plan and Preliminary Results

## 5.1 Purpose and evaluative stance

Chapter 3 argued that diminished reality (DR) on a consumer video-see-through (VST) headset is best
understood as a design space organised by tiers of compositing access, and Chapter 4 described a
reference implementation that occupies that space. Neither chapter, however, is entitled to its
claims without measurement: "runs on consumer hardware", "native quality inside the window", and
"the oracle assumption is a stated, bounded idealisation" are all empirical statements. This chapter
specifies the benchmark protocol that will substantiate — or fail to substantiate — each of them
(contribution C3).

Two properties of the protocol are worth stating up front, because they mirror the evaluative ethos
of the user study in Chapter 6. First, every benchmark is **falsifiable**: each defines, in advance,
the measurement method, the metric, and the criterion or interpretation band against which the
result will be read. A benchmark that cannot fail is marketing, not evaluation. Second, the chapter
is written **before the benchmarks have been run**. With one exception — the offline detection bake
reported in Section 5.8, which was required during development — no measurement below has been
performed. All protocol text is therefore in the future tense, and no result is anticipated. The
completed benchmark tables will replace the placeholder rows in a later revision; the criteria will
not move.

The benchmarks require no participants. They will be executed during the user-study pilot week
(Chapter 6, Section 6.6), when the study build is frozen, so that the numbers reported describe the
exact binary that participants experience.

## 5.2 Apparatus and instrumentation

All measurements will be taken on the study hardware: a Meta Quest 3 (Snapdragon XR2 Gen 2, 8 GB
RAM), running the Horizon OS build current at the study freeze date, with the study APK identified
by build hash (Chapter 6, Section 6.10 pre-session checklist item 4). Instrumentation:

- **Perfetto system traces**, captured via the Meta developer tooling (`metavr` CLI / Perfetto
  capture), for GPU and CPU frame timing, render-stage breakdowns, and thread scheduling (Meta
  Horizon OS developer documentation).
- **OVR Metrics Tool overlay and logcat telemetry** for sustained frame rate, dropped/stale frame
  counts, battery level, and thermal warnings during long runs.
- **Through-the-lens photography** for the legibility benchmark: the headset mounted on a fixed
  stand, a digital camera with manual focus and exposure positioned at the left eyepiece, and
  printed optotype charts at marked distances under the study room's controlled lighting
  (Chapter 6, Section 6.4.0).
- **The offline detection oracle**: the full-resolution YOLO11 detection set produced by
  `Tools/bake_detections.py` over the study's driving footage (Section 5.8), used as ground truth
  for the oracle-gap benchmark.

Each benchmark will be run three times; medians are reported with min–max ranges. Trace files,
photographs, and analysis notebooks will be archived alongside the study data.

**Reproducibility envelope.** Every result in this chapter is specific to a tuple that will be
stated once with the results and applies to all benchmarks: device serial, Horizon OS version,
SDK version, APK build hash, refresh rate, eye-buffer resolution, and room lighting configuration.
The platform's history within this project — a passthrough capability deprecated mid-development
(Chapter 4) — is the argument for recording the tuple explicitly: on consumer XR hardware, a
benchmark without its platform coordinates is unreproducible within a single OS update cycle.

## 5.3 Benchmark T1 — Per-mode GPU cost and frame-budget headroom

**Claim defended.** "The system runs within the display budget of consumer hardware" — the
precondition for every other claim in the dissertation.

**Method.** For each rendering configuration, a 120-second Perfetto trace will be captured in the
study room with the headset worn (head gently moving, as tracking state affects load), and GPU
frame time extracted per frame. Configurations:

| # | Configuration | Tier (Ch. 3) | Scene |
|---|---|---|---|
| 1 | Baseline: passthrough scene, all effects off | — | `CameraSphereVignette` |
| 2 | Soft Dark (overlay only, `_SimpleMode`) | 2 | `CameraSphereVignette` |
| 3 | Hard Dark (overlay only) | 2 | `CameraSphereVignette` |
| 4 | Blur (live PCA feed, 9-tap kernel + desaturation) | 3 | `CameraSphereVignette` |
| 5 | ColorPop on video sphere (study Block B/C configuration) | 3 (applied to video) | `VideoTestScene` |
| 6 | Baseline video sphere, effects off | — | `VideoTestScene` |

Captures are taken worn rather than benched because head motion exercises the code paths that
matter: tracking-state updates, the motion-suppression fade logic, and — for configuration 4 — the
per-frame camera texture updates. A benched headset with a static view under-reports all three.
CPU main-thread and render-thread times are extracted alongside GPU time, because an overlay sphere
is cheap on the GPU but the manager scripts (window recomputation, selection dots, logging hooks)
spend on the CPU; a CPU-bound stale frame is indistinguishable from a GPU-bound one at the display.

**Metrics.** Median and 95th-percentile GPU frame time (ms); median CPU main/render thread time
(ms); stale-frame rate (%); headroom against the display budget (13.9 ms at 72 Hz; 11.1 ms at
90 Hz, if the application targets 90 Hz — the target refresh rate and eye-buffer resolution of the
frozen build will be stated with the results, since both scale GPU cost).

**Criteria.** *Pass* for a configuration used in the user study (2, 3, 5, 6): 95th-percentile GPU
frame time within budget and stale-frame rate < 1 % — jitter in a study stimulus is a confound, not
merely a defect. *Interpretation band* for configuration 4 (Blur, not used in the study): its cost
relative to configuration 2 quantifies the price of Tier-3 camera re-rendering versus Tier-2
overlay compositing — a design-space datum in its own right, reported whether or not it passes.

[Figure 5.1 — Bar chart: median and p95 GPU frame time per configuration against the frame-budget
line.]

## 5.4 Benchmark T2 — Focus-window world-locking stability

**Claim defended.** "The focus window is world-locked": the rectangle stays fixed to the room, not
to the head, under natural head motion. If the window visibly swims or lags, the architectural
claim of Chapter 4 fails, and the study's premise that participants attend to a stable spatial
region weakens.

**Method.** Two complementary measurements. (a) *Log-level:* during scripted head sweeps
(slow ~30°/s and fast ~90°/s yaw, three repetitions each), per-frame logs of head pose and the
window's rendered azimuth/elevation bounds will be captured; since the window is defined in world
azimuth/elevation, its logged bounds should be constant while head pose varies — any variation is
implementation error. (b) *Display-level:* through-the-lens video at the camera's highest frame
rate, with a printed reference edge aligned to the window edge; frame-by-frame inspection gives the
apparent displacement of the rendered edge against the physical edge during the sweep, in degrees
of visual angle.

**Metrics.** Maximum apparent window-edge displacement (°) during the fast sweep; stale-frame rate
during sweeps.

**Criteria.** *Pass:* apparent displacement ≤ 1° during the 90°/s sweep and no perceptible tearing
in the through-lens footage. *Fail* triggers an implementation fix before the pilot's dry runs —
this benchmark is deliberately scheduled before participant-facing sessions.

## 5.5 Benchmark T3 — Legibility across rendering paths

**Claim defended.** The central architectural insight of Chapter 4: routing the focus window
through the *absence* of overlay preserves native OS passthrough quality, while the Tier-3 camera
re-render path is measurably worse. This asymmetry is the reason the architecture exists; it should
be demonstrated, not asserted. Published psychophysics already shows Quest 3 passthrough falls
short of natural vision acuity (arXiv 2026 VST perceptual-gap study); this benchmark measures the
further gap *between the two in-headset paths*.

**Method.** A printed optotype chart (Landolt C or Snellen layout, sized to span logMAR-equivalent
steps at the test distance) will be placed at 0.6 m (the Block A task distance) and photographed
through the left lens under three display conditions with identical placement and lighting:
(1) through the open focus window (native OS passthrough); (2) through the Tier-3 camera-feed path
(the PCA sphere rendering the same region, effects set to identity); (3) direct photograph without
the headset, as reference. Two additional captures at 2 m probe the far condition. Two independent
raters (the author and one colleague, blind to condition labels on the crops) will score the
smallest resolvable line per image.

**Metrics.** Smallest resolvable optotype line per path and distance, expressed as approximate
logMAR steps relative to the no-headset reference; the *window-versus-periphery gap* in lines.

**Method limitation, stated in advance.** A camera at the eyepiece is not a human eye: it differs
in aperture, focus behaviour, and processing, so absolute acuity values from this method are
indicative rather than clinical. The benchmark's claim structure is deliberately *relative* — the
same camera, same chart, same lighting, same placement across the three conditions — so that the
window-versus-periphery *gap* is meaningful even where absolute values are not. Where the published
psychophysical measurements for Quest 3 passthrough (Wang et al., 2026) overlap with condition (1),
they serve as an external sanity check on the method.

**Criteria.** *Interpretation, not pass/fail:* the architecture is vindicated if the window path
resolves at least one optotype line finer than the camera-feed path at 0.6 m. If the two paths
measure equal, the window-as-absence design loses its quality argument (though it retains the FOV
and latency arguments), and the dissertation will say so. The measured gap also feeds the Chapter 6
threat analysis: it quantifies exactly how much acuity the Block A task had to be designed around.

[Figure 5.2 — Side-by-side through-lens crops of the same chart region: native window vs Tier-3
periphery vs no-headset reference.]

## 5.6 Benchmark T4 — The oracle gap: on-device detection versus the offline oracle

**Claim defended.** Part H's oracle-perception assumption (Chapter 6, Section 6.2) is legitimate
only if it is stated as a *measured distance* from present reality, not a hand-wave. This benchmark
produces that measurement — the single table that ties Part T to Part H.

One clarification keeps this benchmark honest. The user study itself uses no live detection
anywhere: ColorPop is luminance-keyed and detector-free by design, and all study stimuli are
scripted (Chapter 6, Section 6.4.0). The oracle-gap benchmark therefore does not validate the study
protocol; it bounds the *idealisation* under which detector-dependent guidance variants (the
salience-highlighting modes prototyped earlier in the project, and any future object-conditioned
mode) are discussed. Without this number, "assume perfect detection" is untestable optimism; with
it, the assumption has a stated size.

**Method.** The deployable on-device configuration (YOLO11n via Unity Sentis, FP16, at the
camera-feed resolution the live pipeline would receive) will be run on the Quest 3 against the same
180-second driving footage used for the offline bake, frame-matched to the oracle's 720 sampled
frames. Oracle detections (full-resolution YOLO11 offline; Section 5.8) are treated as ground
truth. A live detection matches an oracle detection when their boxes overlap at IoU ≥ 0.5 with the
same class label.

**Metrics and the oracle-gap table (template — values to be measured):**

| Metric | Offline oracle (assumption) | On-device live (reality) | Gap |
|---|---|---|---|
| Recall vs oracle (traffic lights + stop signs) | 1.00 by definition | *to be measured* | — |
| Precision vs oracle | 1.00 by definition | *to be measured* | — |
| Throughput (inferences/s) | offline (not real-time) | *to be measured* | — |
| End-to-end latency, frame capture → detection available (ms) | 0 by assumption | *to be measured* | — |
| Small-target recall (boxes < 20 px at capture resolution) | 1.00 by definition | *to be measured* | — |

**Matching procedure detail.** Frame correspondence is exact (both pipelines index the same video
timestamps), so matching reduces to per-frame box assignment: greedy assignment by descending IoU,
each oracle box matched at most once, unmatched live boxes counted as false positives against the
oracle, unmatched oracle boxes as misses. Confidence thresholds for the live pipeline will be swept
and the operating point reported alongside the curve, so that the headline recall number is not an
artefact of one threshold choice.

**Threat to this benchmark, inherited by its consumers.** The oracle is full-resolution YOLO11, not
human annotation; it is a *reference*, and its own errors are unknown. Treating it as ground truth
is defensible for measuring the *gap between offline and on-device pipelines* — both face the same
footage, and the offline pipeline strictly dominates in resolution and compute — but the absolute
detection quality of either pipeline against human-verified truth is not established here, and no
claim in this dissertation depends on it. A 100-frame human-annotated spot check is listed as
optional hardening if pilot-week time allows; if skipped, that is stated with the results.

**Criteria.** *Interpretation bands, pre-stated:* live recall ≥ 0.8 of oracle at ≥ 10 inferences/s
would place detector-driven guidance within near-term engineering reach, and the oracle assumption
would be a modest idealisation; recall below 0.5 or throughput below 2 inferences/s would mean the
assumption is a genuine idealisation of present hardware, and every detector-conditioned statement
in Chapter 7 inherits that qualifier. The small-target row exists because the development history
(Section 5.8) shows small traffic lights are precisely where downsampled pipelines fail first.

## 5.7 Benchmark T5 — Thermal and battery endurance

**Claim defended.** The deployability argument of Chapter 3, Section 3.5-adjacent reasoning (and
testing-strategy §3.5): Soft Dark, the camera-free Tier-2 mode, is claimed to be the only mode
plausibly wearable for hour-long sessions. The prior expectation comes from the one published
feasibility study of PCA-based on-device compositing, which projects 720p30 segmentation-based
compositing for only 5–10 minutes before thermal throttling — a simulation-based estimate, not a
measured on-device run (Laghari et al., 2025, arXiv:2509.18929). The present system's Tier-3 path is
shader-only — lighter than segmentation — so the outcome is genuinely uncertain, which is what
makes the benchmark informative.

**Method.** Four 45-minute continuous runs on a fully charged, thermally rested device in the study
room: (1) Soft Dark, passthrough scene; (2) Hard Dark, passthrough scene; (3) Blur (live PCA feed),
passthrough scene; (4) ColorPop on the video sphere. Battery percentage, thermal-warning events
(logcat), sustained frame rate, and stale-frame rate will be logged at one-minute resolution.

**Pre-stated expectation (TB1).** The camera-free configurations (1, 2, 4) complete 45 minutes
without thermal throttling; the live-PCA configuration (3) may not. Confirmation would ground the
Soft Dark deployability claim empirically; refutation in either direction is reported as found.

**Study-feasibility criterion.** Separately from TB1: the configurations the user study actually
uses must sustain their block durations with margin — Hard Dark for ≥ 25 minutes (Block A plus
tutorial) and the video-sphere configurations for ≥ 15 minutes (Blocks B and C). Failure here would
require a protocol change and would be caught before the pilot's dry runs. Note that the study, by
design, never runs live PCA: the longest-exposure mode (Hard Dark) is a Tier-2 overlay, which is
what makes the 53-minute session thermally plausible in the first place.

## 5.8 Preliminary results: the offline detection bake

One component of the technical programme has already been executed, because development required
it: the offline detection oracle over the study's driving footage.

**Context and motivation.** An earlier development pipeline ran detection on a 640×360 downsample
of the footage and systematically missed small traffic lights — targets that are fully visible to a
human viewer but subtend few pixels at that resolution. This failure (documented in the Chapter 4
failure museum) motivated a full-resolution offline baking approach: if detections are to serve as
ground truth or as pre-scripted salience input, they must be computed at a resolution the live
pipeline cannot afford, which is exactly what makes them an *oracle* in the sense of Section 5.6.

**Procedure.** `Tools/bake_detections.py` ran YOLO11 at full frame resolution over 720 sampled
frames spanning the 180-second driving clip (`DebugVideo.mp4`), detecting traffic lights and stop
signs, on 2026-07-04.

**Results.** The bake produced **9,170 detections across the 720 sampled frames**, with **97 % of
sampled frames containing at least one detection**. Two properties of this result matter for the
rest of the dissertation. First, it confirms the footage is genuinely event-dense — a precondition
for its role as the Block B and Block C stimulus, and a check that was previously done by eye.
Second, it establishes the oracle side of the T4 table: a fixed, versioned, full-resolution
detection set against which the live pipeline can be scored, rather than a moving target.

**What this result does not establish.** It says nothing about on-device feasibility (T4's live
column), nothing about detection quality in absolute terms (YOLO11 at full resolution is the
*reference*, not verified truth against human annotation — a limitation inherited by T4 and stated
there), and nothing about the user-facing system, which uses no detections at all in the study
configuration.

## 5.9 Summary of criteria and reporting rules

| Benchmark | Metric | Pre-stated criterion / band | On failure |
|---|---|---|---|
| T1 GPU cost | p95 frame time; stale % | Study configs: within budget, stale < 1 % | Optimise or change study config before pilot dry runs; report the failure |
| T2 World-locking | Apparent edge drift (°) at 90°/s | ≤ 1°, no tearing | Fix before pilot; benchmark re-run |
| T3 Legibility | Optotype lines, window vs Tier-3 path | Window ≥ 1 line finer at 0.6 m vindicates the architecture | Report that the quality argument fails; FOV/latency arguments stand or fall on their own numbers |
| T4 Oracle gap | Recall, precision, throughput, latency vs oracle | Bands: ≥0.8 recall @ ≥10 fps = near-term; <0.5 or <2 fps = genuine idealisation | No failure mode — the measured gap conditions Chapter 7's detector-dependent statements |
| T5 Endurance | Throttle-free minutes; battery/45 min | TB1: camera-free modes complete 45 min; study configs sustain block durations | Protocol change if study configs fail; TB1 refutation reported as a finding |

Three reporting rules bind the results revision of this chapter. Results will be reported for every
benchmark attempted, including failures and anomalies, with trace archives retained. No criterion
or band stated above will be adjusted after data are seen. And where a benchmark is dropped for
time (the pre-agreed sacrifice order puts T5 first — framing document, Section 7), the drop is
stated rather than silently omitted.

## References (this chapter)

- Meta (2025). *How to Take Perfetto Traces with Meta Quest Developer Hub.*
  developers.meta.com/horizon/documentation/unity/ts-perfettoguide/
- Meta (2025). *Monitor Performance with OVR Metrics Tool.*
  developers.meta.com/horizon/documentation/unity/ts-ovrmetricstool/
- Meta (2025). *Getting Started with Passthrough Camera API in Unity.*
  developers.meta.com/horizon/documentation/unity/unity-pca-documentation/
- Laghari, M. K., Shaikh, A. A., Khan, F., & Siddiqui, A. G. (2025). Native Mixed Reality Compositing
  on Meta Quest 3: A Quantitative Feasibility Study of ARM-Based SoCs and Thermal Headroom.
  arXiv:2509.18929. https://arxiv.org/abs/2509.18929
- Wang, J., Ping, S., Xu, K., Li, Y., & Liang, H.-N. (2026). The perceptual gap between video
  see-through displays and natural human vision. arXiv:2601.02805. https://arxiv.org/pdf/2601.02805
- Jocher, G., & Qiu, J. (2024). *Ultralytics YOLO11* (Version 11.0.0) [Computer software]. Ultralytics.
  https://github.com/ultralytics/ultralytics. Citation per Ultralytics' own CITATION.cff / docs
  (docs.ultralytics.com/models/yolo11/); DOI not yet assigned by Ultralytics at time of writing.
- Unity Technologies (2024). *Unity Sentis* (on-device neural network inference package).
  https://unity.com/products/sentis. Note: Unity has since renamed this package "Unity Inference
  Engine" (`com.unity.ai.inference`, v2.6 as of 2026); cited here under the "Sentis" name because
  that is the name under which it was integrated into this dissertation's pipeline.
- ISO 17488:2016 is cited in Chapter 6 for the detection-response paradigm; not used here.
