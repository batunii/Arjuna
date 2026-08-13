# Rewrite factbase — verified facts, with provenance

Written 2026-08-06, immediately before the manuscript rewrite, so that no verified number depends on
prose that is about to be discarded. Pre-rewrite manuscript is snapshotted at
`Dissertation/pre-rewrite-snapshot-2026-08-06/`.

This file lives outside `25377738-dissertation-submission/` on purpose. That folder's README states
it holds the PDF, its sources, and nothing else.

**Verified at time of writing.** Both analysis scripts were re-run against the raw data and reproduce
the frozen results file exactly:

```
python3 Tools/analysis/blocka_pooled.py Dissertation/authored/raw/blocka
python3 Tools/analysis/blockb_pooled.py Dissertation/authored/raw
```

> **Updated 2026-08-12 — TESTING COMPLETE, authority moved forward again.** Testing completed at
> 22 people tested: the pre-registered twenty through the full two-block protocol, plus two
> Block-B-only supplements (P86, P90) added to offset Block B's response-validity exclusions
> (P3, P43, and the new P84). **`Dissertation/authored/analysis-2026-08-12-pooled-final.md`**
> now supersedes `analysis-2026-08-08-pooled-n17.md` and everything before it. The manuscript
> is on the n = 19 / n = 18 / ESQ n = 20 set: Block A +3.07 pp (p = .0085, dz = 0.678, power
> 88%), GEE OR 0.600 (p = .0025, 4,681 trials), Block B +7.78 pp (**now significant**, p = .026),
> 20–30° band +20.2 pp (p = .003, dz = 0.819, power 95%), ESQ A 81/90, B 56/100, C 55/80.
> Any section below still labelled n = 17 or earlier is a snapshot kept for the record and must
> not be quoted.

---

## Rule zero

`Dissertation/authored/analysis-2026-08-12-pooled-final.md` is the **sole authority for every statistic
in the manuscript**. It is regenerated from the scripts above plus `Tools/analysis/esq_extract.py`
and never hand-edited. It carries its
own "Superseded figures — do not use" table listing the earlier n = 17 / n = 15 / n = 16 numbers,
which in turn supersedes the earlier tables. Any figure from any
superseded column appearing in the rewrite is a bug.

Do not re-derive Block B statistics from raw CSVs without splitting by eccentricity band. A naive
pooled hit-rate produces a materially different and wrong picture. This has happened twice.

---

## Study shape

**18 people tested** across an initial pilot day and six further collection days. **Block A yields 17
usable pairs, Block B yields 15**, overlapping but not identical sets, and **16 end-of-session
questionnaires** were completed. The arithmetic closes as 18 less P1 for Block A, and 18 less P3, P9
and P43 for Block B.

The figures **11**, **14** and **13** appear nowhere as current counts. Each was a previous interim
analysis, and 11 survived in four places in the old manuscript (ch1 twice, ch6 once, ch8 twice) after
the rest was updated. Likewise Block B is **1,200 marker presentations** (40 markers × 2 conditions
× 15 pairs), not 1,040 (the 13-pair figure) and not 880 (the 11-pair figure).

**Block A exclusion, documented and never inferred:**

| Participant | Reason |
|---|---|
| P1 | CPT rounds ran about 15 minutes after the Unity blocks closed, so the arm assignment cannot be verified from ledger windows. Block B retained — the pilot-era >30% false-alarm flag on P1 is not the rule the study adopted, and five retained participants have worse rates (`manuscript-p1-exclusion-audit-2026-08-11.md`). |

**Block B exclusions, documented and never inferred:**

| Participant | Reason |
|---|---|
| P3 | 539 false alarms on the filter run; hits provably looser (Mann-Whitney p = .0301). Block A retained. |
| P43 | False-alarm counts of 81 and 54, the highest in the set by a wide margin and more than double the next participant's, on both arms. Same response-validity grounds as P3. Block A retained. |
| P9 | Experimenter decision on the day: confused during the video blocks. Block A retained. |
| P15 | Own Block B excluded (206 FA / 246 presses); P15 and P16 join as one participant, with P16's arms canonical. |

---

## Block A — H1, workstation focus (Hard Dark), n = 17

| | |
|---|---|
| Filter | 95.39% (SD 4.17) |
| NoFilter | 92.30% (SD 6.56) |
| Mean delta | **+3.09 pp** (SD 4.79, median +2.16) |
| 95% CI | +0.62 to +5.55 (excludes zero) |
| Paired t | t(16) = 2.66, p = .0172 |
| Wilcoxon | W = 21.0, p = .0151 |
| Effect size | dz = 0.645 |
| Direction | 12/17 improved |
| Achieved power | **81.6%** (one-tailed; n = 17 is exactly what this effect size needs for 80%) |

Pooled across the four task versions. The first five participants ran the short task (84 or 105
trials) at 95.2% mean accuracy with 4.8 points of headroom; everyone from P9 onward ran 140 trials,
which deliberately moved off that ceiling to 90.3% mean. The two halves give the same effect at the
same size, +3.30 pp against +2.99 pp, and differ only in spread (SD 2.30 against 6.10), so pooling
combines two measurements of one effect rather than two different effects.

Robustness, from the authority file:

| Subset | n | Delta | 95% CI | t p | W p | dz |
|---|---|---|---|---|---|---|
| **Full pool** | 17 | +3.08 | +0.62 … +5.55 | .0175 | .0174 | 0.643 |
| Without the 3 newest | 14 | +3.34 | +0.33 … +6.35 | .0324 | .0354 | 0.640 |
| First 5 participants | 5 | +3.30 | +0.04 … +6.56 | .0483 | .0625 | 1.257 |
| Participants 6 onward | 12 | +2.99 | −0.54 … +6.52 | .0892 | .1172 | 0.538 |

**Superseded n = 14 snapshot, kept for the record, do not quote:** Filter 95.53% (SD 4.58), NoFilter
92.19% (SD 7.13), delta +3.34 pp, 95% CI +0.33 to +6.34, t(13) = 2.40 p = .0322, W = 19.0 p = .0353,
dz = 0.641, 10/14 improved, power 60%. Kruskal-Wallis across the four versions at that sample was
H = 13.59, p = .0035, driven by the deliberate v4 difficulty change (lures added, distractor reel
switched, after v3 ceilinged at 95–100%). Within the test versions the only timing difference is
1.8 s vs 2.0 s SOA, and median correct RT sits between 630 and 950 ms across every run, so nobody is
response-limited under either.

**Per-participant pairs, n = 14 snapshot** (the current n = 17 set is the one
`fig_slopegraph_h1.png` plots, and it lives in `content/figures/generate_all.py:33–38`; an earlier
version of that figure plotted 14 *invented* values):

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
appears. Dropping it moves the Block A result from p = .0175 to p = .0212 and leaves the effect size
unchanged. Accuracy has a hard ceiling of 100% — no participant value may exceed it.

---

## Block B — H2b, pooled safety check, n = 15

| | |
|---|---|
| Filter | 56.67% (SD 8.85) |
| NoFilter | 50.67% (SD 16.54) |
| Mean delta | **+6.00 pp** (SD 13.39, median +7.50) |
| 90% CI | −0.09 to +12.09 |
| 95% CI | −1.42 to +13.42 |
| Paired t | t(14) = 1.74, p = .1046 |
| Wilcoxon | W = 27.0, p = .1089 |
| Effect size | dz = 0.448, 8/15 positive |
| Achieved power | 50.2% (n = 33 for 80%) |
| TOST ±10 pp | upper test p = .133 — two-sided equivalence **not** established |
| Non-inferiority vs −10 pp | **p = .0002 — passes** |

**H2b is not refuted.** Its pre-registered refuting observation requires equivalence not established
*and* a point estimate showing a drop greater than 10 pp. The estimate is a **rise** of 6.00 pp, so
the refuting observation did not occur.

The two-sided TOST fails only on the upper test: the data cannot exclude a *benefit* above 10 pp.
That is a specification mismatch, not a safety finding. Report both numbers.
**Never substitute the non-inferiority reading for the pre-registered one after seeing the data.**

**Per-participant pairs, n = 15** (NoFilter, Filter, delta): P1 67.5 / 62.5 / −5.00; P2 52.5 / 60.0 /
+7.50; P4 77.5 / 70.0 / −7.50; P6 55.0 / 65.0 / +10.00; P10 57.5 / 55.0 / −2.50; P13 55.0 / 52.5 /
−2.50; P16 37.5 / 60.0 / +22.50; P17 17.5 / 52.5 / +35.00; P20 22.5 / 32.5 / +10.00; P25 45.0 / 52.5 /
+7.50; P26 42.5 / 55.0 / +12.50; P27 65.0 / 50.0 / −15.00; P30 42.5 / 65.0 / +22.50; P33 55.0 / 55.0 /
+0.00; P41 67.5 / 62.5 / −5.00.

**Superseded n = 13 snapshot, kept for the record, do not quote:** Filter 55.58% (SD 9.02), NoFilter
50.00% (SD 17.02), delta +5.58 pp, 90% CI −0.98 to +12.14, 95% CI −2.44 to +13.60, t(12) = 1.51
p = .1557, W = 21.0 p = .1753, dz = 0.420, 7/13 positive, TOST upper p = .126, non-inferiority
p = .0006.

---

## Block B — eccentricity bands (mid-lifetime proxy), n = 15

| Band | NoFilter, pooled | Filter, pooled | Pooled delta | Per-participant mean | 90% CI on per-pid diff | t p | W p | dz | Power |
|---|---|---|---|---|---|---|---|---|---|
| <10° | 72/96 = 75.0% | 67/84 = 79.8% | +4.76 | **−2.50** | [−11.67, +6.67] | .638 | .520 | — | 12% |
| 10–20° | 86/207 = 41.5% | 102/213 = 47.9% | +6.34 | +6.77 | [−3.80, +17.34] | .278 | .379 | 0.291 | 28% |
| **20–30°** | **47/117 = 40.2%** | **71/123 = 57.7%** | **+17.55** | **+17.78** | **[+6.58, +28.97]** | **.014** | **.021** | **0.722** | **84%** |
| >30° | 99/180 = 55.0% | 100/180 = 55.6% | +0.56 | +0.56 | [−6.81, +7.92] | .896 | .894 | 0.034 | 6% |

Only 20–30° is individually reliable, and it is three times larger than any other band. It is the
only Block B measure that is both significant and adequately powered. All four bands trend positive.

**Two estimators, and they disagree.** The pooled delta weights every marker presentation equally;
the per-participant mean weights every participant equally. Participants contribute unequal numbers
of markers to a band, so for `<10°` the two **disagree in sign** (+4.76 pooled, −2.50 per
participant). The t and Wilcoxon p-values belong to the per-participant estimator. The old manuscript
paired the pooled point estimate with the per-participant p-value in one sentence. **The rewrite must
pick one estimator per claim and say which it is.** In particular, the figure annotation on the
pooled-bar panel must name its estimator, because +17.78 is the per-participant figure while
subtracting the pooled bar labels gives +17.55.

**Superseded n = 13 snapshot, kept for the record, do not quote:** <10° 73.8% → 79.2% (+5.36 pooled,
−1.92 per participant); 10–20° 41.3% → 45.9% (+4.61 / +5.01); 20–30° 38.6% → 55.1% (+16.53 /
+16.36, [+4.2, +28.6], t p .034, W p .049, dz ≈ 0.66, power 59%); >30° 54.5% → 56.4% (+1.92 / +1.92).

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

## End-of-session questionnaire, n = 16

| Construct | Favourable | Rate |
|---|---|---|
| A — perceived focus benefit | 71/80 | 89% |
| B — perceived awareness cost | 46/80 | 58% |
| C — visual comfort | 45/64 (four answered items; C3 answered by nobody) | 70% |

Per item: A1 15/16, A3 15/16, A2 14/16, A5 14/16, A4 13/16. **No longer unanimous** — the n = 10
write-up said the three focus items were answered identically by every participant, and that is no
longer true. Section B is where participants divide: B3 ("noticed side events later") 7/16 and B4
("comfortable not seeing everything") 7/16 both run against the filter, while B2 ("still felt
aware") runs 13/16 in favour. The extractor reproduces the earlier n = 13 totals (A 57/65, B 38/65,
C 38/52) exactly before the three newest forms are added, which validates the extraction.

P25 is the one strong dissenter on focus benefit (A 1/5), and is also the participant with the
unexplained Block A re-run accuracy drop (99.28% → 86.33% on the identical no-filter sequence about
three hours later).

**Superseded n = 13 snapshot, kept for the record, do not quote:** A 57/65, B 38/65, C 38/52, with
A1 12/13, A2 11/13, A3 12/13.

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
