# Chapter 5 — Technical Evaluation

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
result will be read. A benchmark that cannot fail is marketing, not evaluation. Second, every
criterion was fixed before the corresponding measurement was taken, so the numbers below can be
read against a standard that was not adjusted to fit them; where the executed method departed from
the stated one, the deviation is disclosed with the result rather than folded quietly into the
method text.

This chapter reports two benchmarks against the platform — frame cost (Section 5.3) and legibility
across rendering paths (Section 5.4) — together with the offline detection bake (Section 5.6) that
the system's detection gate depends on. These are the measurements that bear on claims made
elsewhere in the dissertation: frame cost establishes that the study configurations render inside
budget, legibility quantifies the quality argument for the window architecture, and the bake
characterises the oracle the driving block runs on. Latency, world-locking stability and thermal
endurance were scoped out for the time available; Section 5.9 states what each would have measured
and Chapter 8 carries them as future work. None of the benchmarks requires participants.

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
  `Tools/bake_detections.py` over the study's driving footage (Section 5.6), used as ground truth
  for the oracle-gap benchmark.

Each benchmark will be run three times; medians are reported with min–max ranges. Trace files,
photographs, and analysis notebooks will be archived alongside the study data.

**Reproducibility envelope.** Every result in this chapter is specific to a tuple that will be
stated once with the results and applies to all benchmarks: device serial, Horizon OS version,
SDK version, APK build hash, refresh rate, eye-buffer resolution, and room lighting configuration.
The platform's history within this project — a passthrough capability deprecated mid-development
(Chapter 4) — is the argument for recording the tuple explicitly: on consumer XR hardware, a
benchmark without its platform coordinates is unreproducible within a single OS update cycle.

## 5.3 Per-mode GPU cost and frame-budget headroom

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
| 5 | SignPop on video sphere (study Block B/C configuration) | 3 (applied to video) | `VideoTestScene` |
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

![Figure 5.1 — Median and 95th-percentile GPU frame time per configuration against the 72 Hz frame
budget.](figures/fig5-1-t1-frame-cost.png)

**Results (measured 2026-08-07).** The instrument actually used was the OVR Metrics Tool CSV export
rather than Perfetto: it reports `app_gpu_time_microseconds`, `stale_frame_count`,
`cpu_utilization_percentage` (already the worst-core figure, not an average — confirmed in the
tool's own documentation) and `gpu_utilization_percentage` directly, at roughly 1 Hz, which is the
four metrics this benchmark needs without a separate trace-parsing step. Two further deviations from
the planned protocol are stated here rather than left implicit: each configuration was captured once
in a single continuous session per scene (not three repeated 120 s runs), and the headset was worn
under ordinary use rather than a scripted head-motion sweep. Wall-clock switch times were logged live
against the CSV timestamps; the passthrough-scene run covered 14:40:56–14:49:31 and the video-scene
run 15:02:20–15:08:41 (raw log:
`Dissertation/authored/raw/technical-T1-T3-20260807/session-log.md`). Device: Quest 3, serial
`2G0YC5ZG5F051R`, refresh rate measured at 72 Hz (budget 13.89 ms), study APK sha256 prefix
`51227f964135900e`; Horizon OS build version was not captured this session, a gap noted rather than
guessed.

| # | Configuration | n (≈1 Hz samples) | GPU frame time, median / p95 (ms) | Stale-frame rate | GPU util, median / p95 | Verdict |
|---|---|---|---|---|---|---|
| 1 | Baseline (passthrough, effects off) | 103 | 3.22 / 3.36 | 0.27 % | 41 / 44 % | not a study config |
| 2 | Soft Dark | 89 | 3.25 / 3.35 | 0.00 % | 41 / 42 % | **Pass** |
| 3 | Hard Dark | 158 | 3.17 / 3.36 | 0.05 % | 41 / 42 % | **Pass** |
| 4 | Blur (Tier-3, not used in study) | 164 | 8.01 / 8.15 | 0.11 % | 77 / 90 % | interpretation band |
| 5 | Sign Pop (video) | 255 | 8.62 / 11.44 | **7.35 %** | 70 / 85 % | **Fail** (stale-frame rate) |
| 6 | Baseline video (effects off) | 126 | 10.06 / 12.40 | **8.02 %** | 80 / 90 % | **Fail** (stale-frame rate) |

Every configuration clears the GPU-frame-time half of the criterion with wide margin; even the
video scene's worst p95 (12.40 ms) sits under the 13.89 ms budget. The failure is entirely in the
stale-frame-rate half. Soft Dark and Hard Dark pass cleanly (0.00 % and 0.05 %), matching the
expectation that a Tier-2 overlay is nearly free. Blur, not a study configuration, is reported as the
pre-stated interpretation band: at 8.01 ms median it costs roughly 2.5× a Tier-2 overlay and pushes
GPU utilisation from ~41 % to 77 % median, which is the measured price of Tier-3 camera
re-rendering that Section 5.5's legibility result (below) has to be weighed against.

Both video-scene configurations fail the < 1 % stale-frame criterion by a wide margin, and this is
the chapter's most consequential frame-cost finding. Because the *baseline* video configuration — no
vignette treatment at all — fails just as badly as Sign Pop (8.02 % vs 7.35 % stale), the failure is
not attributable to the Sign Pop shader path; a config with less shader work failed slightly worse.
The most likely explanation, offered here as interpretation rather than a further-measured fact, is
that the video scene's 4K decode and texture upload contend with the compositor for the same frame
window regardless of which vignette mode is active; the elevated `gpu_level` (DVFS boost step 3–4
during video-scene runs versus a steady 2 throughout the passthrough scene) is consistent with the
GPU being under load from something other than the vignette shader itself. This is a genuine failure
against a pre-stated criterion, reported as such: **the video-scene stimulus, as currently
implemented, does not meet the study's own jitter-freedom bar**, independent of which filter
condition is shown, and this should be weighed before the video-scene blocks of the user study are
run at scale. Optimising the video decode/texture-upload path, or reducing source resolution, is the
indicated fix; it is not diagnosed further here.

## 5.4 Legibility across rendering paths

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

**Criteria.** *Interpretation, not pass/fail:* the architecture's quality argument holds if the window path
resolves at least one optotype line finer than the camera-feed path at 0.6 m. If the two paths
measure equal, the window-as-absence design loses its quality argument (though it retains the FOV
and latency arguments), and the dissertation will say so. The measured gap also feeds the Chapter 6
threat analysis: it quantifies exactly how much acuity the Block A task had to be designed around.

![Figure 5.2 — Same chart region, native OS passthrough (left) vs Tier-3 camera path (right); row
labels are the printed logMAR values, read directly rather than by pixel position because the
camera path's narrower field of view changes the chart's apparent scale in
frame.](figures/fig5-2-t3-legibility.png)

**Results (measured 2026-08-07).** Three deviations from the planned method are stated here. First,
the capture instrument was an MQDH screen recording of the rendered output (2560×1370, ~24 fps, 81 s
continuous), not through-the-lens photography; this is arguably the cleaner instrument, since it
captures the eye-buffer content itself rather than a photograph of it through headset optics, but it
was not the pre-stated method and is recorded as a substitution. Second, only the two in-headset
paths were captured, native and Tier-3 camera; no independent no-headset reference photograph was
taken, so the *absolute* logMAR readings below cannot be cross-checked against the Wang et al.
(2026) sanity check as planned. Third, the finest-resolvable-row read below is a single experimenter
pass off extracted frames rather than two independent blind raters, so the result is indicative and
is reported as such. What was preserved from the plan: 0.6 m distance, identical chart,
identical lighting, headset resting (not worn) so placement was constant across conditions, and a
continuous single recording rather than separate captures per condition, which is a *stronger*
control on placement than the plan called for.

The chart was taped at 600 mm, the headset rested on a stand facing it, and the recording ran through
an alternating native → camera → native → camera sequence (four segments, ~15–45 s each), confirmed
by the on-screen `T3: NATIVE PATH` debug label the study code itself prints (the label is occluded
during the camera-path segments, since the full-cover re-render draws over it — itself confirmation
that the camera path is a true full-frame replacement, not a partial overlay). In every segment the
chart was legible and never black, so the benchmark's precondition (config B needs the PCA feed live)
is satisfied.

| Condition | Approx. finest resolved row | Behaviour |
|---|---|---|
| Native (OS passthrough) | ~0.6–0.7 logMAR | ring-gap orientation stays discriminable |
| Tier-3 camera path | ~0.8–0.9 logMAR | gap orientation degrades into a blurred ring 1–2 rows earlier |

The native path resolves roughly one to two logMAR lines finer than the camera path, which satisfies
the pre-stated criterion (window ≥ 1 line finer) even under the conservative single-rater read: the
gap would have to be entirely an artefact of rating error to overturn the direction of the result,
and the two conditions are separated by more than a single row. This is reported as an *indicative*
pass pending the two-rater blind confirmation, not a final number.

One further, unplanned finding: the camera path shows a visibly narrower field of view than native
passthrough at the same physical position and distance — the same chart fills noticeably more of the
frame once the Tier-3 path is active (Figure 5.2 shows this directly: the title and key rows, both
present in the native crop, run off the top and bottom of the camera-path crop despite identical
chart placement). Tier-3 camera re-rendering therefore costs the architecture on three axes measured
across this chapter, not one: GPU time (Section 5.3, ~2.5× a Tier-2 overlay), legibility (this
section), and field of view.

## 5.5 Measurements not taken

Three measurements in the original programme were not run, and each bounds something the
dissertation claims elsewhere. They are named here so the boundary is visible rather than implied.

**World-locking stability.** Chapter 4 claims the focus window is locked to the room rather than the
head. The measurement would sweep the head at a controlled rate and record apparent displacement of
the window edge against a physical reference, with a pass at one degree or less and no visible
tearing. The claim currently rests on the implementation and on the absence of participant reports
of window drift, neither of which is a measurement.

**The oracle gap.** SignPop's detection gate runs on an offline, full-resolution bake
(Section 5.6), not on a live detector. The measurement would run a quantised detector on-device
over the same footage and report recall, precision and throughput against that bake. Without it,
the driving-block result is conditioned on oracle-quality detection and cannot be extrapolated to a
deployed system that must detect in real time. This is the more consequential of the three, and
Chapter 7 treats it as the principal boundary on how far the Block B finding travels.

**Thermal endurance.** The deployability argument in Chapter 3 holds that Soft Dark, the camera-free
mode, is the only one plausibly wearable for hour-long sessions, on the basis of a published
projection for on-device camera processing rather than a measurement of this system. Session lengths
in this study were around forty-five minutes and no thermal throttling was observed, but that is an
absence of incident, not a characterisation.

## 5.6 The offline detection bake

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
rest of the dissertation. First, it confirms the footage is genuinely event-dense, which its role as
the Block B stimulus depends on. Second, it fixes a versioned, full-resolution detection set that a
live pipeline could later be scored against, rather than a moving target.

**What this result does not establish.** It says nothing about on-device feasibility, and nothing
about detection quality in absolute terms: YOLO11 at full resolution is the *reference* here, not
verified truth against human annotation. Both are boundaries on the driving-block finding rather
than on this measurement, and Section 5.5 states them.

## 5.7 Summary of criteria and reporting rules

| Benchmark | Metric | Pre-stated criterion / band | Result (2026-08-07) |
|---|---|---|---|
| GPU cost | p95 frame time; stale % | Study configs: within budget, stale < 1 % | **Mixed.** GPU time passes everywhere. Soft Dark and Hard Dark pass outright. Sign Pop and the video baseline fail on stale-frame rate (7.35 %, 8.02 %) — a video-scene decode/upload issue, not a shader-cost issue (Section 5.3) |
| Legibility | Optotype lines, window vs Tier-3 path | Window ≥ 1 line finer at 0.6 m supports the architecture | **Indicative pass.** Native resolves ~1–2 logMAR rows finer than Tier-3 (Section 5.4), on a single-rater read |

Three further measurements were scoped out for the time available: apparent edge drift of the
world-locked window under fast head rotation, the gap between live on-device detection and the
offline oracle, and thermal endurance across a session. The first two bound claims this dissertation
does make — window stability and the reach of the driving-block finding respectively — and are
carried as future work in Chapter 8, Section 8.3.

Two reporting rules bind this chapter. Results are given for every benchmark run, including failures
and anomalies, with trace archives retained; and no criterion or band was adjusted after the data
were seen.

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
