# Probe / Blob Target Design — Natural-but-Detectable (Supervisor Point 1)

> Compiled 2026-07-18 to answer John's Point 1 (email 2026-07-17): the current opaque grey spheres
> "pop out quite a lot, as they are a bit unnatural compared to the background", risking ~100% hit
> rates in both conditions and an uninformative result. Goal: a probe that does **not** pop out
> pre-attentively, yet **is** reliably recognised as a click target once the participant attends to
> that region. Sources: the existing corpus (see `lit-review-48papers.md` §2, §8) plus a targeted
> stimulus-design search (SGD, DRT/ISO 17488, gaze-contingent psychophysics, onset-capture theory).
> This note is design-justification material for the dissertation and the probe-design spec. The
> probe styles and methods here are now IMPLEMENTED and wired into an end-to-end authoring → split →
> screening → experiment pipeline — see **`authored-pool-pipeline.md`** for the operational pipeline,
> tools, and current data status (as of 2026-07-19).

---

## 1. Why the current blob pops out — four independent pre-attentive cues

`BlobTargetController.cs` today (`Assets/.../Study/BlobTargetController.cs`) draws a **world-space
sphere primitive** at render queue 4150, on top of the vignette, with:

- `m_blobColor = (0.40, 0.38, 0.30, 0.5)` — a flat, desaturated grey-brown at 0.5 alpha (line 68);
- fixed `m_blobSizeDeg = 1.0°` regardless of the real object it stands in for (line 66);
- `ShowBlob()` → `SetActive(true)` — an **abrupt** appearance (lines 442–450);
- constant colour/alpha **independent of the background** behind it and **independent of filter
  state** (drawn over the vignette, not through it — this is also Point 3).

Each of four properties is, on its own, a documented pre-attentive salience cue — so the blob pops
out four times over regardless of how muted the *colour* is:

| Property (current) | Why it captures attention pre-attentively | Source |
|---|---|---|
| **Hard circular silhouette** | A sharp edge is a high-spatial-frequency boundary; flat disc over textured video is a first-order salience anomaly | Itti/Koch/Niebur 1998; Barreiros (pre-attentive features) |
| **Flat, texture-free fill** | Zero-spatial-frequency patch over natural video texture reads as a foreign object even at low alpha | Wallis, Dorr & Bex 2015 (edge-density masking) |
| **Abrupt onset** | Sudden onset produces a luminance *transient* that captures attention exogenously — the strongest capture cue there is | Yantis & Jonides 1984, 1990; Cole, Kuhn & Skarratt 2011 |
| **Background-independent contrast** | Fixed grey-brown is bright against dark road / dark against sky → effective contrast (and salience) varies uncontrolled with scene content | Wallis et al. 2015 (local-contrast masking) |

Lowering the alpha on the existing hard-edged, abruptly-appearing disc addresses none of these except
partially the last. That is why John's fix is "make it not *unnatural*", not "make it dimmer".

---

## 2. What the literature says a subtle-but-detectable probe looks like

Four bodies of work converge on the same recipe.

**Subtle Gaze Direction (Bailey, McNamara, Sudarsanam & Grimm 2009, *ACM TOG* 28(4); Grogorick,
Stengel, Eisemann & Magnor 2017, *SAP '17* — the immersive/HMD port).** Their peripheral cue is a
**~0.76–1° region with a radial Gaussian fall-off**, a **~±10% luminance modulation** of the
*underlying* pixels, luminance preferred over warm–cool chroma. This is the empirically-tuned
"just detectable in the periphery" envelope. Our task **inverts** SGD's purpose (find-by-attention,
not covert steering) — so we sit our probe *at or just above* this envelope so it survives foveation,
but the stimulus engineering (soft Gaussian window, sub-degree size, luminance over chroma) transfers
directly. Grogorick 2017 adds the HMD fix: **elongate the stimulus with eccentricity** so it is
*perceived* circular over a wide-FOV projection — relevant to compositing over 360° video.

**Detection Response Task (ISO 17488:2016) and the Peripheral Detection Task (Jahn et al. 2005;
van Winsum 2018, *Human Factors* 60(6)).** The standardised driving-workload probe — the closest
formal paradigm to our blob task — is **~1° in size, peripheral, modest supra-threshold luminance,
with 3–5 s temporal uncertainty and a ~100–2500 ms response window**. DRT deliberately keeps hit
rates *below* ceiling via modest luminance + peripheral placement + competing load — exactly the
mechanism John wants. van Winsum 2018 shows **stimulus alpha/transparency is a standard, graded
conspicuity knob** — we can calibrate the same way. (Our Block-B probe task already cites ISO 17488 /
Jahn — see `testing-strategy-v2.md §12`; the blob task should be described as a naturalistic DRT
variant.)

**Gaze-contingent contrast increments in natural video (Wallis, Dorr & Bex 2015, *Journal of Vision*
15(8):3).** The near-exact methodological analogue: probes embedded in freely-viewed natural movies.
Their probe is a **2° patch, Gaussian σ = 0.5°**, a **band-limited local-contrast increment
(×1.5–6.5 on existing local energy)**, with a **600 ms temporal envelope (120 ms plateau + 240 ms
Gaussian on/off ramps)** — a **smooth onset**. Crucially they **modulate the underlying pixels**
(scale a Laplacian-pyramid band in place) rather than overpaint a patch, preserving scene texture.
Detection rose with contrast but **plateaued ~85%** (uncertainty prevents ceiling), and thresholds
were **higher in high-edge-density regions** — i.e. a single global contrast will over-show the probe
in smooth sky and under-show it in cluttered street. Budget for **local, scene-adaptive contrast**.

**Onset-capture / change-blindness theory (Yantis & Jonides 1984; Jonides & Yantis 1988; Simons,
Franconeri & Reimer 2000; Cole, Kuhn & Skarratt 2011; Franconeri & Simons 2003).** Abrupt onsets and
motion/looming capture attention automatically; a **gradual (ramped), non-transient** change does not,
even though its end state is fully visible. This is the direct warrant for **ramping the blob on and
keeping it static** (no pulse, no growth).

---

## 3. Recommended blob spec (to pilot)

Design target: no pre-attentive pop-out (no abrupt onset, no hard edge, no flat fill, contrast a
small multiple of detection threshold), yet recognisable under direct attention.

| Parameter | Recommended | Basis |
|---|---|---|
| **Angular size** | ~0.75–2° (start ~1°); ideally **box-matched** to the real object, clamped to [0.8°, 2.5°] | DRT ≈1°; Wallis 2°; SGD 0.76°. Box-matching keeps difficulty tied to the real object, not decoupled |
| **Edge profile** | **Radial Gaussian fall-off** (σ ≈ 0.3–0.5 × radius) — never a hard boundary | Bailey 2009; Wallis 2015 |
| **Contrast type** | **Luminance offset** primary; isoluminant chroma as fallback if luminance still pops | SGD luminance > chroma; periphery is colour-insensitive |
| **Contrast magnitude** | **Weber ~8–20%** (small multiple of the ~3–5% peripheral threshold); calibrate per-region | Wallis ×1.5–6.5 band; SGD ±9.5%; DRT "easily detectable, not uncomfortable" |
| **Compositing** | **Modulate the underlying video pixels** (scale local contrast / multiply-darken) rather than overpaint a flat disc | Wallis 2015 (texture preservation) |
| **Onset** | **Ramp on over ~300–500 ms**, static thereafter; no pulsing/looming | Yantis & Jonides; Simons et al.; Cole et al.; Franconeri & Simons |
| **Scene-adaptivity** | Raise contrast in cluttered/high-edge regions, lower in smooth regions, to equalise detectability | Wallis 2015 (edge-density masking) |
| **Recognition affordance** | Keep it a coherent closed blob (reads as "a target" when foveated) but scene-consistent in colour statistics | change-blindness / hazard-perception scene-consistency |

**Concrete pilot starting point:** a ~1° Gaussian-windowed **local luminance increment of ~12–15%
Weber contrast**, applied by **scaling the underlying video's contrast** (not overpainting), **ramped
on over ~500 ms**, static thereafter, with per-region normalisation against local edge density. Then
titrate contrast until the no-filter (mode 2) hit rate lands in an **informative band (~60–85%)** —
echoing Wallis's ~85% ceiling and DRT's deliberately sub-ceiling design. Watch the `false_alarm`
column in `blobtargets_*.csv` while doing this: John's second worry (Point 1) is participants
mistaking artefacts for targets, and that column is the direct measure of it.

---

## 4. Implementation routes (choose at build time)

The two "modulate underlying pixels" requirements and John's Point 3 (blob must render **behind** the
filter) point at the same architectural move: the blob should stop being a separate opaque overlay.

- **Route A — pragmatic (soft billboard). ✅ IMPLEMENTED 2026-07-18 (`BlobTargetController.cs`).**
  The sphere is replaced with a **double-sided camera-facing billboard** carrying a **procedural
  radial Gaussian alpha texture** (soft rim, no hard silhouette); the material is the SelectionDotMat
  (Standard) template forced into **Fade / alpha-blend** with a **black albedo tint** so the blend is
  `video*(1-α)` — a **texture-preserving darkening** (shadow-like), not a flat overpaint. Alpha
  **ramps in over `m_onsetRampSeconds` (0.5 s)** keyed to video time (identical in modes 1 & 2), and
  size is **box-matched** to the real detection, clamped to `[m_blobMinSizeDeg 0.8°, m_blobMaxSizeDeg
  2.5°]`. Defaults: `m_blobColor (0,0,0,0.12)` (α **is** the core Weber contrast → 12% dimming; an
  earlier 0.40 default was wrong — 40% contrast pops out), `m_edgeGaussianSigma 0.18`.
  This kills all four pop-out cues except per-region contrast calibration (contrast is still
  billboard-global).
  **Known ceiling of Route A:** it is a separate overlay quad that never samples the video, so it can
  only add/subtract a *tint* — it cannot truly modulate the scene's own pixels (local desaturation /
  contrast dip), which is what "not a foreign object" ultimately requires. Darkening also competes
  with the Dark filter modes (a dark probe can read as filter, not target). If a faint tint still
  reads as unnatural in-headset, that is Route A's ceiling → go to Route B. *Achieved via alpha-blend toward black — no multiply blend mode or custom shader
  needed, so it reuses the proven device-safe material path.* **Remaining: self-pilot the α / size /
  tint against the ~60–85% no-filter hit-rate band; watch `false_alarm`.** Not yet built or piloted
  on device.
- **Route B — principled (shader-composited modulation). ✅ IMPLEMENTED 2026-07-18** (superseded
  Route A after the overlay read as a distracting dark spot). The blob target is **no longer a
  world-space object** — `BlobTargetController` pushes the active target's az/el + angular radius +
  onset-ramped strength to `CameraSphereVignette.shader` each frame (via
  `VideoTestSceneManager.SetBlobProbe`), which composites it as a **local modulation of the real
  scene pixels**: a soft **desaturation** (`m_blobDesat` 0.7) + **gentle dim** (`m_blobDim` 0.08),
  Gaussian-feathered (`m_blobSigmaFrac` 0.5), box-matched size, ramped onset. Because it modulates
  the *already-filtered* colour (applied last in `frag`), it reads as a natural haze/smudge on the
  scene — texture fully preserved, never a foreign object — and **a target in a defocused/dimmed area
  is filtered too: on the blacked-out Hard-Dark periphery it disappears entirely. That delivers
  supervisor Point 3 for free.** Hit feedback is a brief green flash mixed in the same shader term.
  **Remaining: self-pilot `m_blobDesat`/`m_blobDim` against the ~60-85% no-filter hit-rate band
  (watch `false_alarm`); add per-region contrast calibration against local edge density (Wallis 2015)
  if uniform strength proves too content-dependent.** Compiles clean; not yet piloted on device.

Route A (soft alpha-blend overlay) was built first but rejected: even toned down it is an added tint,
not a modulation of the real scene, and darkening competes with the Dark filter modes. Its code has
been removed in favour of Route B.

**Probe STYLE (added 2026-07-18, after on-device check):** a pure desaturation/dim probe has a second
problem beyond subtlety — it sits *exactly on* the YOLO detection carve-out (the probe targets ARE
those detections), and the carve-out is itself a local brighten/clear. A subtle contrast modulation
there is indistinguishable from "the filter revealing an object", so it fails to read as its own
click-target. Fix: give the probe a distinct *shape/effect*, not another contrast change.
`m_probeStyle` (in `CameraSphereVignette.shader` `ApplyBlob`) now offers:
- **Desaturate** — the original colour-loss + dim smudge (blends most; can be mistaken for the window).
- **Halo** — a soft bright RING / outline (a shape, not a fill) — clearly distinct from the filled
  carve-out.
- **Bubble** (default) — a glassy droplet that refracts/**magnifies the scene in angular space**
  (`_BlobLens`, ~1.8× at core) with chromatic fringing, plus a legibility-tuned **double edge**
  (bright scene-tinted outer rim + dark inner ring — a Fresnel-like light/dark pair that the eye
  detects far more reliably than a single soft ring, and shows even over flat road/sky), a small
  **specular glint**, and a mild contrast-crisp of the magnified content. All scaled by `_BlobRim`
  (main legibility knob) / `_BlobLens` (magnification). Most distinct and ecologically natural for a
  windscreen; still a modulation of the real pixels, so scene-consistent and filter-respecting
  (Point 3). Legibility comes from *contrast structure* (edges, refraction), not brightness/colour
  pop — so it stays recognisable-when-attended without becoming pre-attentively salient. An earlier
  version was too subtle (the lens displaced by the tiny in-blob UV offset → invisible; only a flat
  white rim showed); fixed by doing the refraction in angular space and adding the double edge/glint.
- **LocalContrastRing** (default, added 2026-07-18) — the measurement-honest option, added in response
  to the content-dependence critique (§4b). A coloured ring rendered at a contrast **controlled relative
  to the local surround**: a luminance step that transitions smoothly from bright-on-dark to dark-on-
  bright (`smoothstep` on local luma), plus a chosen hue (`m_blobRingColor`, default cyan — a colour the
  grayscale filter never produces). Because the ring carries its *own* contrast (a fixed step we set)
  rather than borrowing it from the scene, its detectability is ~constant across dark/bright/textured
  backgrounds — the property DRT/ISO 17488 requires (§4b). `m_blobRim` here is the luminance contrast
  step (0.4–0.6 = clear but not blaring). Still applied after the filter, so it still respects Point 3
  (fades in the blacked-out periphery — that region's reduced hit-rate is the measured "does the filter
  block peripheral events" effect, per the bifurcated analysis, not a within-region confound).

There is also a debug mode (`m_debugObviousBlob`, default off) that renders each probe as a big solid
magenta patch — used once on-device to confirm the pipeline; leave off for the real study.

**Chosen resolution to content-dependence (§4b):** (1) use `LocalContrastRing` so within-region probe
visibility is content-independent; (2) keep the probe ON the object (do not offset it off the detection,
which would change the construct from "noticing the object" to "noticing a nearby dot"); (3) solve
distinctness-from-the-YOLO-window via colour/form, not spatial offset; (4) bifurcate window vs periphery
in analysis (John Point 2) so the filter's legitimate reduction of peripheral visibility reads as the
effect, not a confound; (5) baseline-normalise residual variance. Bubble is kept as a face-valid
secondary option.

---

## 4b. Is the refractive-droplet probe backed by research? (verification, 2026-07-18)

Layered answer — the components are supported; the specific refractive *form* is defensible-but-novel
as a *measurement instrument*, and the literature raises one serious, specific objection.

- **Paradigm (STRONG):** a naturalistic detection/probe task indexing spare attention — DRT/ISO 17488;
  Jahn, Oehme, Krems & Gelau 2005; van Winsum 2018 (*Human Factors*, 10.1177/0018720818776880).
- **Non-salience by design (STRONG, but double-edged):** refraction is a **second-order
  (contrast/texture-defined)** cue — Wolfe & Horowitz's guiding-attribute taxonomy (2004 *Nat Rev
  Neurosci* 10.1038/nrn1411; 2017 *Nat Hum Behav* 0058) lists distortion/refraction/lustre as
  **non-efficient** channels, so it won't pop out (good) — but Ashida, Seiffert & Osaka 2001 (*JOSA A*
  10.1364/JOSAA.18.002255) show second-order targets are searched **serially/effortfully**, so hit
  rate reflects attention *and* second-order signal strength (bad for clean measurement).
- **Distinct-from-the-filter (STRONG):** Duncan & Humphreys 1989 (*Psych Review*
  10.1037/0033-295X.96.3.433) — findability needs the probe dissimilar from the co-located DR
  carve-out. Justifies a distinct form.
- **In-scene lens/subtle-cue precedent (SUPPORTS as a device, not as metrology):** Bier et al. 1993
  Magic Lenses (10.1145/166117.166126); Furnas 1986 fisheye; Lu, Duh & Feiner 2012/2014 "subtle
  cueing" (IEEE ISMAR / TVCG 10.1109/TVCG.2013.241) — barely-perceptible in-scene modification guides
  search. No precedent for a distortion probe as a *calibrated detection metric*.
- **Windscreen droplet ecological validity (NEUTRAL→CHALLENGING):** change-blindness driving work
  (Beanland 2017; Galpin 2009) shows scene-target detectability varies by object type; automotive-vision
  literature treats droplets as detection-**degrading occluders** — plausible but adds a confound.

**The core objection (a viva examiner WILL raise this):** the probe's physical detectability is
content-dependent and **confounded with the independent variable** — the DR filter locally dims/reduces
contrast, and the probe is applied *after* the filter (Point 3), so a lower hit rate in a filtered
region could be reduced attention OR reduced local contrast the probe needs. Crucially this follows
from the **Point-3 decision** (probe respects the filter), so it applies to *any* filter-respecting
probe; the refractive/second-order droplet only makes it worse. Also note the probe's actual
findability now comes from the first-order add-ons (chromatic fringe = colour channel; bright
edge/glint) — the second-order refraction is the least controllable component.

**Mandatory mitigation (regardless of probe style):** (1) measure per-region, per-condition **baseline
detectability (d′, full-attention/single-task)** and normalise dual-task hit rates to it; (2) calibrate
probe strength to a criterion so baseline detectability is *equated* across filtered vs unfiltered
regions (the refractive analogue of ISO 17488's fixed-luminance LED); (3) report detectability as a
controlled variable and test the filter×region interaction against baseline. **Fallback:** a first-order,
background-independent marker (calibrated edge/contrast increment) satisfies the DRT standard by
construction; keep the droplet as a secondary face-validity condition only if pilot shows its baseline
detectability can be equated across regions.

Write-up stance: cite the layers above; frame the refractive form as a **design contribution + named
limitation** (Point 6b), not prior-art-validated. (Several DOIs from this pass are format-correct but
unopened — verify Jahn 2005, Cavanagh & Mather 1989, Leung & Apperley 1994, Beanland 2017, Galpin 2009,
van Winsum threshold-model, Lu/Duh/Feiner 2014 before citing.)

## 4c. Making the probe a VALID attention measure — test procedure (methods, 2026-07-18)

Answers "how do comparable studies test this, and what do we do about content-dependence." Key result
from the methods literature: because the **same seeded targets appear over the same background in both
filter-ON and filter-OFF, within subject**, per-target difficulty is held physically constant across the
comparison and therefore **cancels in the within-target ON−OFF difference — it cannot bias the mean
effect** (Maxwell, Delaney & Kelley 2018; Kirk 2013). It can only (i) cost statistical power and (ii)
bias through **one** channel: **floor/ceiling truncation** — a target already invisible with the filter
off, or trivially visible with it on, can't move, distorting the difference and any difficulty×condition
interaction (Loftus 1978; Liu & Wang 2021; Wagenmakers et al. 2012). So per-participant calibration
(staircase/QUEST) is **largely redundant here**; the cheap standard defenses are a baseline pass +
item screening.

**Minimum viable procedure (satisfies a methods examiner for this paired design): #1 + #2 + #4 + #5.**

- **#1 No-filter full-attention baseline block** — probes shown with the filter OFF, detect-only, run
  first. Measures each target's baseline detectability (the "is this even seeable, absent the filter?"
  reference). Standard dual-task/DRT logic: measure the probe task alone, then under the manipulation
  (Pashler 1994; Kahneman 1973; Posner 1980). **Implemented**: first entry in `TestModeSequencer`
  (`"Baseline (detect only, no filter)"`). The single most important addition.
- **#2 Item screening** — drop targets whose baseline hit-rate is at floor/ceiling (default <0.15 or
  >0.90; pre-register the cut). Item norming and floor/ceiling exclusion is mainstream (Snodgrass &
  Vanderwart 1980; classical-test-theory difficulty band p≈0.2–0.8). **Implemented**: `blob_probe.py`
  `screen()`.
- **#3 Catch trials → d′ (best add-on, PARTLY implemented)** — d′ = z(H) − z(F) separates sensitivity
  (visibility) from bias/attention, and is undefined without a false-alarm rate, i.e. **defined
  no-signal (catch) trials** (Stanislaw & Todorov 1999; Macmillan & Creelman 2005; Hautus 1995
  log-linear correction; 50/50 signal/no-signal default, Wolfe et al. 2005 on prevalence). Attention
  moves d′, not just criterion (Carrasco 2011; Hawkins et al. 1990). `blob_probe.py` computes an
  **approximate** d′ now (using target count as the FA denominator) — flagged as relative only;
  **rigorous d′ needs real catch trials added to the harness** (documented next step).
- **#4 ISO 17488 scoring** — classify responses by RT from onset: premature (<100 ms), valid
  (100–2500 ms), late/unrequested (>2500 ms); RT on hits only (ISO 17488:2016; Stojmenova & Sodnik
  2018). Note the ISO DRT deliberately holds the probe *constant* and does NOT calibrate per person —
  published precedent for our not-calibrating. **Implemented**: `blob_probe.py` `iso_rt()`.
- **#5 Crossed subject×item mixed models** — analyse hits (logistic) and RT with random intercepts +
  condition slopes for subjects AND items; a by-item random intercept absorbs each target's baseline
  difficulty automatically, recovering power without discarding data (Baayen, Davidson & Bates 2008;
  Barr et al. 2013; treating items as fixed inflates Type I error, Clark 1973). Analysis-stage (R), on
  the formal StudyLogger data.

Not needed here: **per-participant staircase/QUEST calibration** (Levitt 1971; Watson & Pelli 1983) —
its bias-removal job is already done by the paired design, and its only unique benefit (keeping targets
off floor/ceiling) is delivered far more cheaply by #1+#2. Implement only if a reviewer demands equated
absolute thresholds.

**Tooling:** `Tools/analysis/blob_probe.py` runs #2/#3/#4 on `blobtargets_*.csv` (per-target baseline
screening, ISO RT bins, approximate d′) — verified on synthetic data. Practice/learning from the
baseline pass is equal across the later modes, so it cancels in the paired ON−OFF difference.

Citation-verify before submission (flagged by the search as format-correct but unopened): Posner/Snyder/
Davidson 1980, Snodgrass & Vanderwart 1980 DOIs, ISO 17488 §6.7 practice count. High-confidence anchors:
ISO 17488, Pashler 1994, Levitt 1971, Watson & Pelli 1983, Stanislaw & Todorov 1999, Hautus 1995,
Baayen 2008, Barr 2013, Loftus 1978, Wolfe et al. 2005.

## 4d. Does "shuffle 40 targets across modes to average out background" hold? (Monte-Carlo test, 2026-07-19)

Tested the proposed design (pool of ~40 clickable points, assigned across filter modes, generalising
over ~20 participants) with a Monte-Carlo simulation (`true attention effect = 0.097` hit-rate gap;
20k draws). Verdict: **holds for the confound it targets, under two conditions, and misses one.**

| Scenario | Counterbalanced (each target in both modes across sample) | One-off random/fixed split |
|---|---|---|
| Background varies by target (content-dependence) | bias **+0.000**, sd 0.004 — recovers true effect | bias ~0 *on average* but sd 0.050, 90% range [0.015, 0.182] |
| + floor targets (hard backgrounds) | still unbiased, but true effect diluted 0.097→0.075 (power loss) | sd 0.090, range **crosses zero** [−0.077, +0.221] — can flip sign |
| Filter ALSO dims the probe (condition-dependence) | **bias +0.083 → est 0.180 vs true 0.097 (~1.9×)** | same bias +0.082 — counterbalancing does nothing |

Conclusions: (1) the shuffle argument is **valid for background/content-dependence** — but only as
**systematic counterbalancing** (Latin-square rotation of target-sets across the 20 participants), NOT a
single random deal, which is unbiased only in expectation and dangerously variable in one study;
(2) **still screen floor targets** — they don't bias the counterbalanced estimate but dilute power and
blow up the uncounterbalanced variance; (3) the shuffle **cannot** fix the filter dimming the probe
itself (condition-dependence) — that biases every design ~2× and needs d′/catch trials or the
window-vs-periphery split. Implication: pool + baseline-screen + **counterbalanced set rotation** +
crossed-mixed-models; plus catch trials for the condition-dependence channel.

**Ring vs coloured translucent bubble:** every fragility above traces to variance in per-target baseline
visibility. The **LocalContrastRing minimises it** (contrast set relative to local surround → low
variance, fewer floor targets, more power) and its persistent **colour** cue resists the filter's
luminance dimming (the un-counterbalanceable confound) better than refraction does. The bubble borrows
scene contrast → higher variance, and only wins on ecological naturalness. **Recommendation: ring for
the measured study.** Empirical decider (run in pilot): compare per-target baseline hit-rate *variance*
of the two via `blob_probe.py` — lower variance = more content-independent = better probe.

## 5. Dissertation-grade citations added by this note

Primary / parameter-bearing (cite with confidence):
Bailey, McNamara, Sudarsanam & Grimm 2009 (*ACM TOG* 28(4), 10.1145/1559755.1559757);
Grogorick, Stengel, Eisemann & Magnor 2017 (*SAP '17*, 10.1145/3119881.3119890);
ISO 17488:2016 (DRT); van Winsum 2018 (*Human Factors* 60(6), 10.1177/0018720818776880);
Wallis, Dorr & Bex 2015 (*Journal of Vision* 15(8):3, 10.1167/15.8.3);
Yantis & Jonides 1984 (*JEP:HPP* 10(5)); Jonides & Yantis 1988 (*Perception & Psychophysics* 43(4));
Simons, Franconeri & Reimer 2000 (*Perception* 29(10)); Cole, Kuhn & Skarratt 2011 (*APP* 73(5));
Franconeri & Simons 2003 (*Perception & Psychophysics* 65(7));
Rensink, O'Regan & Clark 1997 (*Psychological Science* 8(5)).

Supporting / secondary:
McNamara, Bailey & Grimm 2008/2009 (APGV / *ACM TAP* 6(3) — quote ratios, not its odd absolute sizes);
Krajancich, Kellnhofer & Wetzstein 2023 (*ACM TOG* 42(4)) for attention-gated peripheral sensitivity;
Road Hazard Stimuli dataset 2023 (*Behavior Research Methods*, 10.3758/s13428-023-02299-8) for
scene-consistent naturalistic targets + click response modality.

Caveats to flag in the write-up: (1) SGD is *covert* guidance — cite for the stimulus envelope, and
state explicitly that our use inverts it; (2) the ISO ~2 cd/m² figure is an ambient-tuned default, not
a fixed contrast — report our own calibrated value; (3) Wallis's edge-density masking means a single
global contrast across a busy driving scene is not defensible — budget for local calibration (Route B).
