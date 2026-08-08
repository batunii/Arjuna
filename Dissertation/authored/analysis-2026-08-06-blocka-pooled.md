# Block A — pooled analysis across task versions (2026-08-06)

Headline result for Block A (1-back CPT, filter ON vs filter OFF), pooled across all task
versions per the analysis decision of 2026-08-06. Supersedes the n=11 figure.

Regenerate with:

```
python Tools/analysis/blocka_pooled.py Dissertation/authored/raw/blocka
```

The tool reads the staged CSVs directly, so re-running it after any new session updates
every number here.

---

## 1. Result

**n = 14 paired participants.**

| Measure | Value |
|---|---|
| Filter | 95.53% (SD 4.58) |
| NoFilter | 92.19% (SD 7.13) |
| **Mean delta** | **+3.34 pp** (SD 5.21, median +2.28) |
| 95% CI | +0.33 .. +6.34 (excludes zero) |
| Paired t | t(13) = 2.40, **p = .0322** |
| Wilcoxon signed-rank | W = 19.0, **p = .0353** |
| Effect size | dz = 0.641 |
| Direction | 10/14 positive |

Significant on both the parametric and non-parametric test.

## 2. Per participant

Accuracy = (hit + correct_reject) / scored trials, excluding the unscored first trial.
Delta = Filter − NoFilter.

| pid | version | NoFilter | Filter | delta |
|---|---|---|---|---|
| P2 | 84x2.5s | 97.6% | 100.0% | +2.41 |
| P3 | 84x2.5s | 95.2% | 98.8% | +3.61 |
| P4 | 84x2.5s | 98.4%* | 100.0% | +1.60 |
| P6 | 105x2s | 84.6% | 92.3% | +7.69 |
| P9 | 140x1.8s | 95.0% | 100.0% | +5.04 |
| P10 | 140x1.8s | 97.8% | 100.0% | +2.16 |
| P13 | 140x2s+lures | 100.0% | 97.1% | −2.88 |
| P15 | 140x2s+lures | 74.8% | 90.6% | +15.83 |
| P17 | 140x2s+lures | 91.4% | 89.9% | −1.44 |
| P20 | 140x2s+lures | 86.3% | 91.4% | +5.04 |
| P25 | 140x2s+lures | 86.3% | 96.4% | +10.07 |
| P26 | 140x2s+lures | 89.2% | 86.3% | −2.88 |
| P27 | 140x2s+lures | 96.4% | 95.7% | −0.72 |
| P33 | 84x2.5s | 97.6% | 98.8% | +1.20 |

\* **P4's NoFilter is experimenter-reported, not measured** — no CSV was ever exported for
that arm (`analysis-2026-07-22.md` §6, `HANDOFF.md:318`). It is the only reported-rather-than-
measured value in the analysis and **must be flagged in the write-up**.
`blocka_pooled.py` prints a `NOTE` line for it on every run.

## 3. Provenance and set construction

**Arm resolution.** Runs from 2026-07-23 onward stamp filter state in the run-start payload
and the `dr_intensity` column. Earlier runs do not; those arms come from documented
assignments, never inferred:

| pid | assignment | source |
|---|---|---|
| P2 | A3 = Filter, A4 = NoFilter | ledger window verification |
| P3 | A1 = NoFilter, A2 = Filter | clean run timing |
| P33 | A3 = Filter, A4 = NoFilter | experimenter re-classification, 2026-07-22 |
| P4 | A2 = Filter | plan-order assignment (`analysis-2026-07-22.md` §6) |

**Excluded: P1** (both runs, the only exclusion). Its CPT rounds ran ~15 minutes after the
Unity blocks closed, so the arm cannot be verified from ledger windows the way P2's and P3's
were. Including it would take the analysis to n=15 and requires an experimenter record of
which round carried the filter.

**Practice runs are excluded throughout** (`blocka_timecourse.py:87`), as are runs with fewer
than 30 scored trials.

**Verification against previously recorded figures** — the tool reproduces the historical
subsets, confirming the set matches the one used before:

| Subset | Tool | Previously recorded |
|---|---|---|
| 2026-07-27 set (n=10, without P20) | +3.52 | +3.51 (`HANDOFF.md:318`) |
| With P20 (n=11) | +3.66 | +3.65 |

## 4. Pooling decision (2026-08-06)

Earlier analysis kept task versions in separate cells (`blocka_timecourse.py:13-14`, supervisor
directive 2026-07-23). **The decision of 2026-08-06 is to pool all versions**, on the grounds
that the pilot versions differ from the test but the test versions do not differ in any way
that affects performance: the v3/v4 SOA difference is 1.8 s vs 2.0 s, while median correct RT
across all runs sits between 630 and 950 ms, so no participant is response-limited under
either timing and a 200 ms window difference cannot plausibly drive an accuracy gap.

Recorded for completeness, since the version keys are visible in the data — the observed
per-version spread and the parameter differences behind it:

| version | runs | mean acc | SD | range | task params |
|---|---|---|---|---|---|
| 84x2.5s | 9 | 98.3% | 1.7 | 95.2–100.0 | `nback1` |
| 105x2s | 2 | 88.5% | 5.4 | 84.6–92.3 | `nback1` |
| 140x1.8s | 4 | 98.2% | 2.4 | 95.0–100.0 | `nback1`, distractors=NEW |
| 140x2s+lures | 14 | 90.9% | 6.4 | 74.8–100.0 | `nback1_lures`, target_p=0.5, lure_p=0.6, distractors=PROTEST |

Kruskal-Wallis across the four versions: H = 13.59, p = .0035. The v4 change added the lure
manipulation and switched the distractor reel, which was the deliberate response to v3's
95–100% ceiling (`HANDOFF.md:220`). `blocka_pooled.py` prints this table above the pooled
result on every run so the assumption stays visible to anyone reading the output.

## 5. Sessions added 2026-08-06

Three participants ran on 2026-08-06 (P25, P26, P27), taking Block A from n=11 to n=14. All
three are `140x2s` gen v4 with PROTEST distractors.

| pid | Block A delta | Block B (Filter vs NoFilter hit rate) | notes |
|---|---|---|---|
| P25 | +10.07 | 21/40 (52%) vs 18/40 (45%) | Block A re-run; Block B filter arm had **28 false alarms** |
| P26 | −2.88 | 22/40 (55%) vs 17/40 (42%) | clean session, 0 FA on filter arm |
| P27 | −0.72 | 20/40 (50%) vs 26/40 (65%) | Block B runs against the hypothesis in every band |

**P25 ran Block A twice.** The first attempt (14:47–14:55) produced only a no-filter main run;
the filter block was closed with practice only. P25 re-ran both arms at 17:34 (Filter, A4,
seed 253) and 17:40 (NoFilter, A2, seed 251) — six minutes apart, so fatigue and time-of-day
are matched across arms. The re-run pair is canonical; the first attempt is retained in
`raw/excluded_p25_blocka_20260806/` with a README.

Two provenance points on P25, both recorded in that README:

- The re-run's no-filter arm re-used slot A2 / seed 251, so P25 saw that sequence twice.
  This is not treated as a familiarity confound: a 1-back task is a running comparison that
  does not reward sequence knowledge, and the repeat scored *worse* (86.33% vs 99.28%), the
  opposite of what prior exposure would produce.
- That 99.28% → 86.33% drop on identical trials ~3 hours apart is **unexplained** and warrants
  a line in the incident log. It bears on which session state is representative, independently
  of the filter manipulation.

## 6. Open items

- **P1** — arm assignment unverifiable; resolving it would give n=15.
- **P4** — no-filter arm has no CSV; the 98.4% is experimenter-reported and must be flagged.
- **P25 Block B** — 28 false alarms on the filter arm. The false-alarm exclusion criterion is
  still undecided (`HANDOFF.md:205`); P3 was excluded from Block B on this basis via a
  Mann-Whitney test on hit `angle_deg`. P25 is a second candidate and the criterion should be
  settled before the freeze.
- **P9** — both main runs were entered as slot A3, so the same seed generated both arms'
  sequences. Pair retained; noted for completeness.
