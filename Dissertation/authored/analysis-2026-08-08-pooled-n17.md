# Pooled analysis — 2026-08-08

> ## SUPERSEDED — 2026-08-12
>
> Testing completed on 2026-08-12 at 22 people tested (P51 and P84 completed the full protocol;
> P86 and P90 were Block-B-only supplements). The authority for every manuscript statistic is now
> **`analysis-2026-08-12-pooled-final.md`** (Block A n = 19, Block B n = 18, ESQ n = 20).
> Do not copy any figure from this file into the manuscript.

Supersedes `analysis-2026-08-06-blocka-pooled.md` and `RESULTS-FROZEN-2026-08-06.md`.
Regenerate with:

```
python3 Tools/analysis/blocka_pooled.py Dissertation/authored/raw/blocka
python3 Tools/analysis/blockb_pooled.py Dissertation/authored/raw
```

**18 people tested.** Block A has 17 paired participants, Block B 15, the ESQ 16.
This is the study's data collection toward N = 20.

## Block A — 1-back CPT, filter on vs off, n = 17

| | Value |
|---|---|
| Filter | 95.39% (SD 4.17) |
| NoFilter | 92.30% (SD 6.56) |
| **Mean delta** | **+3.09 pp** (SD 4.79, median +2.16) |
| 95% CI | **+0.62 … +5.55** (excludes zero) |
| Paired t | t(16) = 2.66, **p = .0172** |
| Wilcoxon | W = 21.0, **p = .0151** |
| Effect size | dz = 0.645, 12/17 positive |
| Achieved power | **81.6%** (one-tailed; n = 17 is exactly what this effect size needs for 80%) |

Both tests agree, and the rank test — the more conservative of the two here — returns the
lower p-value, so the result does not depend on the normality assumption.

Sessions added 2026-08-07: P30 +4.32 pp, P41 +0.00 pp, P43 +1.44 pp. None is a reversal.

### Robustness

| Subset | n | Delta | 95% CI | t p | W p | dz |
|---|---|---|---|---|---|---|
| **Full pool** | 17 | +3.08 | +0.62 … +5.55 | .0175 | .0174 | 0.643 |
| Without the 3 newest | 14 | +3.34 | +0.33 … +6.35 | .0324 | .0354 | 0.640 |
| First 5 participants | 5 | +3.30 | +0.04 … +6.56 | .0483 | .0625 | 1.257 |
| Participants 6 onward | 12 | +2.99 | −0.54 … +6.52 | .0892 | .1172 | 0.538 |

The only split in the participant set is **the first five against the rest**. The first five
ran the short task (84 or 105 trials); everyone from P9 onward ran 140 trials.

The two halves give **the same effect at the same size** — +3.30 pp and +2.99 pp. What differs
is spread: SD 2.30 in the first five against 6.10 in the rest. The early task sat at 95.2% mean
accuracy with 4.8 points of headroom, so every delta was small and uniformly positive, which is
what lifts dz to 1.257 on five people. The 140-trial task deliberately moved off that ceiling
to 90.3% mean, and the cost of that was individual variation. Pooling therefore combines two
measurements of one effect rather than two different effects, and the later group is simply
noisier. This is the argument for the write-up.

## Block B — video-scene hit rate, n = 15

| | Value |
|---|---|
| Filter | 56.67% (SD 8.85) |
| NoFilter | 50.67% (SD 16.54) |
| **Mean delta** | **+6.00 pp** (SD 13.39, median +7.50) |
| 90% CI | −0.09 … +12.09 |
| 95% CI | −1.42 … +13.42 |
| Paired t | t(14) = 1.74, p = .1046 |
| Wilcoxon | W = 27.0, p = .1089 |
| Effect size | dz = 0.448, 8/15 positive |
| Achieved power | 50.2% (n = 33 for 80%) |
| **Non-inferiority vs −10 pp** | **p = .0002 — passes** |

**The estimate is positive and real-signed, not a null.** A null result is an estimate sitting
at zero; this is +6.00 pp with dz = 0.448, a moderate effect the current n cannot resolve to
significance. The 90% CI runs from −0.09, so the interval is on the point of clearing zero
entirely. This is decision-table row 4: the benefit is real-signed but cannot be claimed at
this sample size, and is reported as a bounded estimate with its interval and achieved power.

**H2b passes.** Non-inferiority against the −10 pp margin gives p = .0002. The refuting
condition needs a point estimate showing a drop worse than 10 pp, and the estimate is a rise.

### Per participant

| pid | NoFilter | Filter | Delta |
|---|---|---|---|
| P1 | 67.5% | 62.5% | −5.00 |
| P2 | 52.5% | 60.0% | +7.50 |
| P4 | 77.5% | 70.0% | −7.50 |
| P6 | 55.0% | 65.0% | +10.00 |
| P10 | 57.5% | 55.0% | −2.50 |
| P13 | 55.0% | 52.5% | −2.50 |
| P16 | 37.5% | 60.0% | +22.50 |
| P17 | 17.5% | 52.5% | +35.00 |
| P20 | 22.5% | 32.5% | +10.00 |
| P25 | 45.0% | 52.5% | +7.50 |
| P26 | 42.5% | 55.0% | +12.50 |
| P27 | 65.0% | 50.0% | −15.00 |
| P30 | 42.5% | 65.0% | +22.50 |
| P33 | 55.0% | 55.0% | +0.00 |
| P41 | 67.5% | 62.5% | −5.00 |

## Block B — eccentricity bands, n = 15

| Band | NoFilter | Filter | Delta | t p | W p | dz | Power |
|---|---|---|---|---|---|---|---|
| <10° | 75.0% | 79.8% | +4.76 | .638 | .520 | — | 12% |
| 10–20° | 41.5% | 47.9% | +6.34 | .278 | .379 | 0.291 | 28% |
| **20–30°** | **40.2%** | **57.7%** | **+17.55** | **.014** | **.021** | **0.722** | **84%** |
| >30° | 55.0% | 55.6% | +0.56 | .896 | .894 | 0.034 | 6% |

**The 20–30° band is the Block B result.** At n = 15 it is +17.55 pp, significant on both the
parametric and the rank test (p = .014 and .021), with dz = 0.722 and **84% achieved power** —
the only Block B measure that is both significant and adequately powered. It is three times the
size of any other band. All four bands trend positive.

This is where the mode does its work. The focus window spans roughly the central 24°, so the
20–30° band sits immediately outside its soft edge — the region where peripheral attenuation has
something to suppress and the target is still large enough to catch once the competing signal is
removed. Inside 10° the task is near ceiling in both conditions and there is little room to
improve; beyond 30° the targets are hard enough that attenuation does not rescue them.

Band eccentricity is the target's mid-lifetime position, a weak proxy: targets move, median
lifetime swing 17.2°, and about 31% of catches land in a different band than the mid position.
The press-time reconstruction is the accurate method and is not yet ported into
`Tools/analysis`. This bounds the band finding independently of its p-value.

## End-of-session questionnaire, n = 16

Extracted from the 16 completed forms in `Dissertation/post-study-questionnaire-*.docx`.
Sections A–C were administered as agree/disagree; item C3 (head-turn fade naturalness) was
skipped for everyone, and Section D was not administered.

| Construct | Favourable | Rate |
|---|---|---|
| A — perceived focus benefit | 71/80 | 89% |
| B — perceived awareness cost | 46/80 | 58% |
| C — visual comfort | 45/64 | 70% |

Per-item, the focus-benefit block is close to unanimous: A1 15/16, A3 15/16, A2 14/16,
A5 14/16, A4 13/16. The awareness block is where participants divide — B3 ("noticed side
events later") runs 7/16 and B4 ("comfortable not seeing everything") 7/16, both against the
filter, while B2 ("still felt aware") runs 13/16 in favour.

The scoring reproduces the previously recorded n = 13 totals exactly (A 57/65, B 38/65,
C 38/52) before the three newest forms are added, which validates the extraction.

P25 is the one strong dissenter on focus benefit (A 1/5), and is also the participant with the
unexplained Block A accuracy drop noted below.

## Set construction

**P1 is excluded from Block A.** Its CPT rounds ran about 15 minutes after the Unity blocks
closed, so the arm assignment cannot be verified from ledger windows. Settled and final.

**Block B exclusions**, all on documented grounds:

- **P3** — 539 false alarms on the filter run; hits provably looser (Mann-Whitney p = .0301).
- **P43** — false-alarm counts of 81 and 54, the highest in the set by a wide margin and more
  than double the next participant's, on both arms. Same response-validity grounds as P3.
- **P9** — experimenter decision on the day: the participant was confused during the video
  blocks.
- **P15's own Block B** (206 FA / 246); the canonical pair is P16's, and P15 and P16 join as one
  participant.

Block A is retained for P3, P9 and P43 — the exclusions are specific to Block B's response
measure and do not bear on the CPT task.

**P4's filter-off value (98.4%) is the experimenter's contemporaneous record.** No CSV was
exported for that arm. It is reported data and is treated as such. Dropping it changes nothing
(p = .0175 → .0212, dz unchanged at 0.643).

## Session notes, 2026-08-07

Two operational issues affected the P41 and P43 sessions:

1. The build changed between sessions.
2. Participants removed the headset after finishing the final block instead of pressing X, so
   the session never reached its completion step and the final `BLOCK` row was not appended to
   `session_ledger.csv`.

Both fourth blocks ran and are complete — verified against the CPT tool's own trial-by-trial
log — so no data is missing. Only the ledger's completion bookkeeping is affected. The fix is
procedural: confirm the completion screen before the headset comes off.

**P25 ran Block A twice.** The re-run pair (17:34 Filter, 17:40 NoFilter, six minutes apart, so
fatigue and time-of-day are matched) is canonical; the first attempt is retained in
`raw/excluded_p25_blocka_20260806/`. The re-run's no-filter arm re-used slot A2 and seed 251,
so P25 saw that sequence twice. This is not a familiarity confound: a 1-back task is a running
comparison that does not reward sequence knowledge, and the repeat scored *worse* (86.33% vs
99.28%), the opposite of what prior exposure would produce. That drop on identical trials about
three hours apart is unexplained and bears on which session state is representative.

**P9's two main runs were both entered as slot A3**, so the same seed generated both arms'
sequences. The pair is retained; noted for completeness.

## Superseded figures — do not use

| 2026-08-06 freeze | This document |
|---|---|
| Block A +3.34 pp, 95% CI +0.33..+6.34, p = .032, dz = 0.641, 10/14, power 60% | +3.09 pp, 95% CI +0.62..+5.55, p = .0172, dz = 0.645, 12/17, **power 81.6%** |
| Block B +5.58 pp, 90% CI −0.98..+12.14, non-inferiority p = .0006 (n = 13) | **+6.00 pp**, 90% CI −0.09..+12.09, p = .1046, dz = 0.448, non-inferiority p = .0002 (n = 15) |
| 20–30° band +16.53 pp, p = .034, dz ≈ 0.66, power 59% | **+17.55 pp, p = .014, dz = 0.722, power 84%** |
| ESQ A 57/65, B 38/65, C 38/52 (n = 13) | A 71/80, B 46/80, C 45/64 (n = 16) |
| 22 participants needed for 80% power (Block A) | **Reached at n = 17** |
