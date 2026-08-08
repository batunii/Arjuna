# Rewrite factbase — verified facts, with provenance

Written 2026-08-06, immediately before the manuscript rewrite, so that no verified number depends on
prose that is about to be discarded. Pre-rewrite manuscript is snapshotted at
`Dissertation/pre-rewrite-snapshot-2026-08-06/`.

This file lives outside `25377738-dissertation-submission/` on purpose. That folder's README states
it holds the PDF, its sources, and nothing else.

**Verified at time of writing.** Both analysis scripts were re-run against the raw data and reproduce
`Dissertation/authored/RESULTS-FROZEN-2026-08-06.md` exactly:

```
python3 Tools/analysis/blocka_pooled.py Dissertation/authored/raw/blocka
python3 Tools/analysis/blockb_pooled.py Dissertation/authored/raw
```

---

## Rule zero

`Dissertation/authored/RESULTS-FROZEN-2026-08-06.md` is the **sole authority for every statistic in
the manuscript**. It is regenerated from the scripts above and never hand-edited. It carries its own
"Superseded figures — do not use" table listing the earlier n = 11 / n = 10 numbers. Any figure in
that column appearing in the rewrite is a bug.

Do not re-derive Block B statistics from raw CSVs without splitting by eccentricity band. A naive
pooled hit-rate produces a materially different and wrong picture. This has happened twice.

---

## Study shape

15 people tested across an initial pilot day and four further collection days. **Block A yields 14
usable pairs, Block B yields 13**, overlapping but not identical sets.

The figure **11** appears nowhere. It was the previous interim analysis and survived in four places
in the old manuscript (ch1 twice, ch6 once, ch8 twice) after the rest was updated. Likewise Block B
is **1,040 marker presentations** (40 markers × 2 conditions × 13 pairs), not 880 (which was the
11-pair figure).

**Block B exclusions, documented and never inferred:**

| Participant | Reason |
|---|---|
| P3 | 539 false alarms on the filter run; hits provably looser (Mann-Whitney p = .0301). Block A retained. |
| P9 | Experimenter decision on the day: confused during the video blocks. Block A retained. |
| P15 | Own Block B excluded (206 FA / 246 presses); P15 and P16 join as one participant. |

---

## Block A — H1, workstation focus (Hard Dark), n = 14

| | |
|---|---|
| Filter | 95.53% (SD 4.58) |
| NoFilter | 92.19% (SD 7.13) |
| Mean delta | **+3.34 pp** (SD 5.21, median +2.28) |
| 95% CI | +0.33 to +6.34 (excludes zero) |
| Paired t | t(13) = 2.40, p = .0322 |
| Wilcoxon | W = 19.0, p = .0353 |
| Effect size | dz = 0.641 |
| Direction | 10/14 improved |
| Achieved power | 60% (n = 22 needed for 80%) |

Pooled across the four task versions per the 2026-08-06 decision. Kruskal-Wallis across versions
H = 13.59, p = .0035, driven by the deliberate v4 difficulty change (lures added, distractor reel
switched, after v3 ceilinged at 95–100%). Within the test versions the only timing difference is
1.8 s vs 2.0 s SOA, and median correct RT sits between 630 and 950 ms across every run, so nobody is
response-limited under either.

**Per-participant pairs** (these are what `fig_slopegraph_h1.png` plots; the figure previously
plotted 14 *invented* values):

| pid | NoFilter | Filter | delta |
|---|---|---|---|
| P2 | 97.6 | 100.0 | +2.41 |
| P3 | 95.2 | 98.8 | +3.61 |
| P4 | **98.4** | 100.0 | +1.60 |
| P6 | 84.6 | 92.3 | +7.69 |
| P9 | 95.0 | 100.0 | +5.04 |
| P10 | 97.8 | 100.0 | +2.16 |
| P13 | 100.0 | 97.1 | −2.88 |
| P15 | 74.8 | 90.6 | +15.83 |
| P17 | 91.4 | 89.9 | −1.44 |
| P20 | 86.3 | 91.4 | +5.04 |
| P25 | 86.3 | 96.4 | +10.07 |
| P26 | 89.2 | 86.3 | −2.88 |
| P27 | 96.4 | 95.7 | −0.72 |
| P33 | 97.6 | 98.8 | +1.20 |

**P4's NoFilter value (98.4%) is experimenter-reported, not measured.** No CSV was exported for that
arm. It is the only such value in the analysis, and the manuscript must say so where the number
appears. Accuracy has a hard ceiling of 100% — no participant value may exceed it.

---

## Block B — H2b, pooled safety check, n = 13

| | |
|---|---|
| Filter | 55.58% (SD 9.02) |
| NoFilter | 50.00% (SD 17.02) |
| Mean delta | +5.58 pp (SD 13.27, median +7.50) |
| 90% CI | −0.98 to +12.14 |
| 95% CI | −2.44 to +13.60 |
| Paired t | t(12) = 1.51, p = .1557 |
| Wilcoxon | W = 21.0, p = .1753 |
| Effect size | dz = 0.420, 7/13 positive |
| TOST ±10 pp | p = .126 — two-sided equivalence **not** established |
| Non-inferiority vs −10 pp | **p = .0006 — passes** |

**H2b is not refuted.** Its pre-registered refuting observation requires equivalence not established
*and* a point estimate showing a drop greater than 10 pp. The estimate is a **rise** of 5.58 pp, so
the refuting observation did not occur.

The two-sided TOST fails only on the upper test (t = −1.20, p = .126): the data cannot exclude a
*benefit* above 10 pp. That is a specification mismatch, not a safety finding. Report both numbers.
**Never substitute the non-inferiority reading for the pre-registered one after seeing the data.**

---

## Block B — eccentricity bands (mid-lifetime proxy), n = 13

| Band | NoFilter | Filter | Pooled delta | Per-participant mean | 90% CI on per-pid diff | t p | W p |
|---|---|---|---|---|---|---|---|
| <10° | 62/84 = 73.8% | 57/72 = 79.2% | +5.36 | **−1.92** | [−12.6, +8.7] | .753 | .656 |
| 10–20° | 74/179 = 41.3% | 85/185 = 45.9% | +4.61 | +5.01 | [−5.3, +15.3] | .403 | .465 |
| **20–30°** | **39/101 = 38.6%** | **59/107 = 55.1%** | **+16.53** | **+16.36** | **[+4.2, +28.6]** | **.034** | **.049** |
| >30° | 85/156 = 54.5% | 88/156 = 56.4% | +1.92 | +1.92 | [−6.5, +10.4] | .692 | .938 |

20–30° dz ≈ 0.66, achieved power 59%. Only 20–30° is individually reliable, and it is three to eight
times larger than any other band. All four bands trend positive.

**Two estimators, and they disagree.** The pooled delta weights every marker presentation equally;
the per-participant mean weights every participant equally. Participants contribute unequal numbers
of markers to a band, so for `<10°` the two **disagree in sign** (+5.36 pooled, −1.92 per
participant). The t and Wilcoxon p-values belong to the per-participant estimator. The old manuscript
paired the pooled point estimate with the per-participant p-value in one sentence. **The rewrite must
pick one estimator per claim and say which it is.**

Band membership uses the target's mid-lifetime position, a weak proxy: targets move, median lifetime
swing 17.2°, and ~31% of catches land in a different band than the mid position assigns. The
press-time reconstruction is the accurate method and is not yet ported into `Tools/analysis`.

---

## Band boundaries — where they come from

Verified against `Dissertation/citation-ledger.md` and the 2026-07-23 supervisor email.

| Boundary | Source | What the source actually says |
|---|---|---|
| <10° | `victor2005` | Driver gaze concentrates in a road-centre region of roughly **8°** radius |
| 10–20°, 20–30° | `jahn2005` | The Peripheral Detection Task places probes at **11–23°** horizontal eccentricity |
| — | `iso2016` | ISO 17488 standardises the same logic with a fixed, background-independent stimulus |
| >30° | `ball1993` | Useful field of view ≈30°. **Conventional figure from that literature, not a value stated in the paper** — the manuscript already flags this and must keep doing so |

**Honest fit caveat, must be kept:** the published probe range stops at 23°, so it covers 10–20°
completely but only the lower part of 20–30° — which is the headline band. The boundaries are round
numbers chosen to bracket the published landmarks, not to reproduce any one study's geometry.

The 2026-07-23 email also argues the UFOV *shrinks under cognitive load*, which would strengthen the
case for 20–30°. **There is no bib entry supporting this.** Do not assert it without adding a source.

---

## End-of-session questionnaire, n = 13

| Construct | Favourable |
|---|---|
| A — perceived focus benefit | 57/65 |
| B — perceived awareness cost | 38/65 |
| C — visual comfort | 38/52 (four answered items; C3 answered by nobody) |

A1 12/13, A2 11/13, A3 12/13. **No longer unanimous** — the n = 10 write-up said all three were
answered identically by every participant, and that is no longer true. The extractor reproduces the
earlier n = 10 totals (A 47/50, B 29/50, C 30/40) exactly before adding the three 2026-08-06 sessions.

The subjective impression of lost awareness is **not** supported by the behavioural hit rate, which
shows no loss. That divergence is a finding to report, not a discrepancy to resolve.

---

## Apparatus — do not confuse these again

**Block A = workstation = Hard Dark = REAL HARDWARE.** World-locked panel, one shape at a time, press
if it matches the previous shape (1-back). Two physical tablets at ±35° azimuth, ~1 m from the
participant, task surface ~0.6 m. Window uses room-geometry anchoring (four corner rays captured on
release, remembered as physical points). Hard Dark's job is to occlude the tablets entirely.

**There is no eccentricity or angle-band analysis anywhere in Block A, and there must never be.**
Angle bands are a Block B concept only. Letting Block A apparatus questions drift into window angles
has been an actual, called-out mistake.

Chapter 6 fixes **no angular extent** for the Block A focus window — the participant paints it and
the experimenter verifies the panel is inside and both tablets outside. A figure asserting "±25°" is
inventing a number.

**Block B = driving = SignPop = SIMULATION.** Equirectangular video sphere, direction-locked window
in azimuth/elevation (the system default). This block owns the eccentricity bands, the ring-marker
task, and the 20–30° finding.

---

## Verified shader values

Checked against `Assets/PassthroughCameraApiSamples/ShaderSample/Shaders/CameraSphereVignette.shader`.

| Parameter | Value | Justification status |
|---|---|---|
| `_PopGreyDim` | 0.4 | **Has a reason:** a periphery approaching black stops carrying peripheral events at all, and a monitoring mode whose own dimming floors peripheral detection would fail H2b by construction. This track covers most peripheral pixels, so its floor decides whether the periphery stays usable. |
| `_PopBrightOut` | 0.65 | Signal colours stay detectable everywhere, merely privileged inside the window |
| `_PopPeriphDim` | 0.85 | Focus window wins on plain luminance (`bailey2009`, `grogorick2017`) |
| net outside-ROG | ≈0.65 × 0.85 ≈ **0.55** of natural | Deliberate midpoint: `cheng2022` (partial attenuation preferred over removal), `barhorstcates2016` (anxiety rises as field narrows) |
| `m_mode2MaxAlpha` (Soft Dark) | 0.75 | `cheng2022` preference for partial opacity |
| `m_softEdgeDeg` | 20° | Inside the ≈30° UFOV (`ball1993`); above the ≈15° head-yaw noise floor (`higgins2022`) |
| `m_popSoftEdgeDeg` | 32° | Colour/grey boundary reads harsher than a darkness boundary |
| `m_vignetteFormTime` | 3 s | `hata2016`: gradual change stays below awareness while still biasing gaze |
| Sigmoid α = 10, β = 0.5 | | `sutton2022`'s published recipe, used verbatim |
| Detection pulse ≈1 Hz | | `waldner2014`: low-frequency, low-amplitude, low annoyance, far below the photosensitivity band |
| Motion thresholds | Blur 30°/s (0.6 s), ColorPop 35°/s (0.7 s), Soft Dark 50°/s (1.0 s), Hard Dark 70°/s (1.5 s) | Inverts `norouzi2018`, where head-coupled vignetting *increased* sickness |

**Still bare — the skill forbids a magic number.** Either justify or drop from prose:
`_PopSatBoost` 1.7, `_PopBrightIn` 1.15, `_PopGlareKnee` 0.6, `_PopGlareInside` 0.35,
`_PopGlareOutside` 0.85, `_PopGuardRadius` 0.008.

**ColorPop pipeline order** (a figure had this wrong): classify ROG → **ROG track** (inside: sat
×1.7, sigmoid, bright ×1.15 / outside: colour kept ×0.65) **or other track** (inside: desat 0.25 /
outside: grey ×0.4, plus glare compression on bright, *unsaturated*, *non*-ROG pixels with the
blown-core guard) → **global periphery dim ×0.85 applied to both tracks** → output. Glare compression
is on the non-ROG branch. The periphery dim is a merge, not one branch's step.

---

## Detection oracle

`Tools/bake_detections.py` ran YOLO11 at full 3840×2160 resolution over the 8-minute-39-second study
clip, as 3×2 overlapping tiles plus a full-frame pass merged by per-class NMS, sampling every 0.5 s.

**13,939 detections across 1,040 sampled frames, 99.5% of samples containing at least one detection**
(median 14 per sample; 2,952 traffic lights, 10,987 people).

70 of the 80 authored Block B targets (88%) fall inside an active detection window while on screen.
33 of 246 post-debounce traffic-light lifetimes never clear the ~1.2° minimum apparent size.

The study uses **no live detection anywhere**. SignPop's gate is driven by this baked track.

---

## System facts

- Meta Quest 3. OS owns the passthrough layer: an application cannot read, shade, or spatially mask
  it, only composite on top or apply a global colour transform.
- PCA feed: **mono 1280×960, ~85–90° horizontal FOV**, against the system layer's ~110°.
- Three tiers: 1 = OS passthrough styling (colour only, no spatial control); 2 = overlay compositing
  (attenuate/occlude, a shaped *absence* passes native passthrough through untouched); 3 = camera
  re-render via PCA (arbitrary per-pixel, at camera-copy fidelity).
- **Tier 3 is a separate, lower-grade copy of the world, not access to the layer the user sees.** A
  diagram drawing a Tier 3 arrow into the OS passthrough layer contradicts the chapter's core claim.
- `VignetteMode` enum has 11 entries. Two evaluated (SignPop, Hard Dark). Soft Dark specified but
  never shown to a participant. Blur superseded. ColorPop is SignPop's base pipeline. Five
  exploratory (ChromaticCool, ConspicuitySqueeze, GranulatedPeriphery, OutlinedDark, SpotLift).
  TintedDark is dead code kept only because removing the entry breaks a serialized scene value.
- Prior-art search, 2026-07-05: 26 web queries and 4 GitHub API queries across ISMAR, CHI, UIST,
  IEEE VR, DIS, arXiv 2022–2026, Meta developer docs, all 136 forks of the PCA samples repo, trade
  press, visionOS and Varjo API surfaces. **No publication, product, or open-source project
  composites system passthrough with a live shader-processed camera feed in one view, for any
  purpose.**
- Thermal: `laghari2025` projects 720p30 PCA-style compositing throttling within 5–10 minutes. That
  is a **simulation-based projection, not a measured run** — say so.

---

## Figures — 15 built, all data-bearing ones verified

`content/figures/generate_all.py` regenerates them. Block A and the band chart now draw from measured
data; both previously contained fabricated or wrong values.

Labels in use: `fig:structure`, `fig:stack`, `fig:hybrid`, `fig:axes`, `fig:window`, `fig:fusion`,
`fig:colorpop`, `fig:suppression`, `fig:interaction`, `fig:oracle`, `fig:blockalayout`, `fig:h1`,
`fig:h2b`, `fig:h2a`, `fig:esq`. Table labels: `tab:tiers`, `tab:gaps`.

Six placeholders remain (`\figplaceholder`), needing photography or headset screenshots: one in ch1,
three in ch4, two in ch5. If ch5 is deleted its two placeholders go with it.

---

## LaTeX facts worth not rediscovering

- `\textwidth` is **155 mm = 441 pt** (`tcdthesis.sty:219`).
- A `p{w}` column costs `w` **plus 12 pt of `\tabcolsep`**. Four `p` columns summing to 0.90 overflow.
  Keep four-column sums ≤ 0.88; five columns need `\tabcolsep` reduced as well.
- `tcdthesis.sty` defaults `\@textpagestyle` to `fancy` and applies it at the first `\chapter`, but
  never loads `fancyhdr` and never configures that style. Only `\thesisdraft` changes it. `thesis.tex`
  therefore selects `plain` explicitly — page numbers bottom centre, no header, no rule.
- Every `longtable` needs `\endfirsthead` as well as `\endhead`, or the caption reprints on every page
  and logs a duplicate List of Tables entry.
- `\verb` cannot line-break. Long identifiers in narrow table columns need `\texttt{}` with `\-` hints.
- **Three copies of the submission exist.** `25377738-dissertation-submission/` is the live one.
  `25377738-Dissertation/` and `submission/` are stale builds with zero embedded figures, as is
  `25377738-dissertation-submission.zip`. Delete or clearly mark them.
- No LaTeX toolchain in WSL here, and `git` is unusable (`git-lfs` not installed, so every git command
  aborts). Back up by copying.

---

## Open issues the rewrite must resolve

1. **H2a is stated two ways.** The old ch6 hypothesis table pre-registers it as the 20–30° band
   specifically; the old ch7 §7.2 says the pre-registered hypothesis asked whether SignPop speeds
   detection generally, and calls the band result a surprise. One is the pre-registered wording.
   Pick it, state it once, and let the band result be either confirmatory-as-designed or exploratory.
2. **The `<10°` estimator conflict** above.
3. **Bare shader numbers** above.
4. **Orphaned citations.** Deleting ch5 orphans `jocher2024` and `unity2024` (rehome to ch4's
   detector text) and `meta2025a`, `meta2025b` (profiling tools — remove from `refs.bib`). Dissolving
   ch7 orphans `magic2024` (rehome to the OST-transfer paragraph in the conclusion).
5. **Word budget.** Main text was 34,086 words. Target ~20,000, appendices excluded.
