# Pooled analysis — 2026-08-12 — FINAL (testing complete)

Supersedes `analysis-2026-08-08-pooled-n17.md` (and everything it superseded), and supersedes the
first 2026-08-12 draft of this document (see "Superseded figures" below). Regenerate with:

```
python Tools/analysis/blocka_pooled.py Dissertation/authored/raw/blocka
python Tools/analysis/blockb_pooled.py Dissertation/authored/raw
python Tools/analysis/esq_extract.py
```

**Testing is complete. 20 people were tested, meeting the pre-registered target exactly.**
Block A has 19 paired participants, Block B 17, the ESQ 20 forms (all 20; P1's and P6's forms and
P90's Section A were added 2026-08-13, superseding the earlier 18-form set).
P15+P16 are one participant throughout.

**Correction, 2026-08-13:** the first draft of this document counted 22 tested, listing a
participant "P84" and two Block-B-only supplements "P86" and "P90" alongside the twenty
full-protocol participants. That count was wrong on both points.

**P84 and P90 are the same participant.** The in-app participant ID was mistyped as 84 at the start
of that participant's 11 August session, so the session's Block A payload and CPT seeds carry 84
(`seed = m_pid * 10 + slot`) while the participant is 90. The Block A exports were relabelled to
`pid` 90 before analysis, 90 being the canonical ID. There is no separate P84: the ledger rows under
that ID are the same session. The originals, the run mapping, the seed evidence and the timestamp
restoration are documented in **`provenance/README-p90-blocka-provenance.md`**, which should be read
alongside this file by anyone auditing the Block A set.

**P86 is not in the analysed set.** No session or questionnaire files exist under that ID and it
contributes to no result.

The tested count is therefore 20, matching the recruitment target, with no supplementary
participants. P9's discretionary exclusion stands.

## Block A — 1-back CPT, filter on vs off, n = 19

| | Value |
|---|---|
| Filter | 95.08% (SD 4.42) |
| NoFilter | 92.01% (SD 6.57) |
| **Mean delta** | **+3.07 pp** (SD 4.52, median +2.16) |
| 90% CI | +1.27 … +4.87 |
| 95% CI | **+0.89 … +5.25** (excludes zero) |
| Paired t | t(18) = 2.95, **p = .0085** |
| Wilcoxon | W = 23.0, **p = .0065** |
| Effect size | dz = 0.678, 14/19 positive |
| Achieved power | **88.4%** (one-tailed) |

Sessions added since the n = 17 analysis: P51 +2.16 pp, P90 +3.60 pp. Neither is a reversal, and
the estimate moved by 0.02 pp — the stability-under-accumulation argument continues to hold
(+3.34 at 14, +3.09 at 17, +3.07 at 19).

### SESOI (decision table)

Off-arm error rate 7.99 pp → SESOI (half) = 3.99 pp. Observed reduction 3.07 pp = **38% of the
distractor-laden error rate**, 90% CI [+1.27, +4.87] = **16% to 61%** of that error rate. The 90%
interval still spans the SESOI, so **H1 remains decision-table row 4**: real-signed, significant,
adequately powered, but not claimable at the pre-set size.

### Pre-registered primary test — trial-level logistic model, n = 19

4,681 file-backed scored trials across the 37 file-backed arms of the 19 pairs (P4's filter-off
arm is the experimenter's record and contributes to the paired test only). Error rate off 8.61%,
on 5.29%.

| Model | Effect | 95% interval | p |
|---|---|---|---|
| **Logistic GEE, exchangeable, clustered by participant** | **OR 0.600** | [0.431, 0.836] | **.0025** (z = −3.020) |
| Random-intercept logistic GLMM (VB) | OR 0.594 | CrI [0.496, 0.712] | interval excludes 1 |

The vignette **reduces trial-level error odds by 40.0%**. β = −0.5109, SE 0.1692. Both estimators
agree with each other and with the paired test; the pre-registered primary remains stronger than
the robustness check (.0025 vs .0085).

### Robustness

| Subset | n | Delta | 95% CI | t p | W p | dz |
|---|---|---|---|---|---|---|
| **Full pool** | 19 | +3.07 | +0.89 … +5.25 | .0085 | .0065 | 0.678 |
| Without the 2 newest (= published n=17) | 17 | +3.09 | +0.62 … +5.55 | .0172 | .0151 | 0.645 |
| Short-version five (P2 P3 P4 P6 P33) | 5 | +3.30 | +0.05 … +6.56 | .0478 | .0625 | 1.261 |
| 140-trial fourteen | 14 | +2.98 | +0.03 … +5.94 | .0483 | .0392 | 0.582 |
| Without P4 (experimenter-reported arm) | 18 | +3.15 | +0.84 … +5.45 | .0104 | .0086 | 0.678 |

Same effect at the same size in both task-version halves (+3.30 vs +2.98); pooling still combines
two measurements of one effect. Dropping P4's reported arm now *strengthens* the result slightly
(p = .0104), so the disclosure sentence keeps its "changes nothing" conclusion with the new figures.

Kruskal–Wallis across the four task versions (39 scored runs): **H = 16.70, p = .0008** — versions
differ, which is the premise for the robustness split, not a threat to it.

## Block B — video-scene hit rate, n = 17

| | Value |
|---|---|
| Filter | 57.50% (SD 9.14) |
| NoFilter | 50.15% (SD 15.60) |
| **Mean delta** | **+7.35 pp** (SD 13.82, median +7.50) |
| 90% CI | **+1.50 … +13.20** (excludes zero) |
| 95% CI | +0.25 … +14.46 (excludes zero) |
| Paired t | t(16) = 2.19, **p = .0434** |
| Wilcoxon | W = 29.0, **p = .0435** |
| Effect size | dz = 0.532, 10/17 positive |
| Achieved power | 67.5% (one-tailed) |
| **Non-inferiority vs −10 pp** | **p = .000046 — passes** |
| TOST ±10 pp | lower p = .000046, upper p = .2206 → **equivalence not established** |
| False alarms per run | off 6.9 vs on 11.6 (10/17 pressed more under filter) |

**The pooled Block B difference remains significant on both tests with P84 and P86 removed.**
At n = 15 it was +6.00 pp at p = .105; adding P51 (+5.00 pp) and P90 (+30.00 pp) — the two
sessions that actually remain on disk — moves it to +7.35 pp at p = .0434, with both the 90% and
95% intervals clear of zero, though more narrowly than the withdrawn P84/P86 draft reported.

**H2b's verdict holds but the statistic must stay honest.** The refuting observation
(equivalence not established AND a drop worse than 10 pp) did not occur — the estimate is a rise.
The pre-registered two-sided TOST is still not established, and only on the upper side: the data
cannot exclude a *benefit* larger than 10 pp. The matched one-directional instrument,
non-inferiority against −10 pp, passes decisively at p = .000046. Report all three; the
paired-test significance is reported as the observed pooled benefit, not as a substitute for the
registered safety statistic.

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
| P51 | 50.0% | 55.0% | +5.00 |
| P90 | 42.5% | 72.5% | +30.00 |

1,360 scored marker presentations across the 17 analysed pairs (17 × 2 × 40).

## Block B — eccentricity bands (mid-lifetime proxy), n = 17

| Band | NoFilter | Filter | Pooled Δ | Per-pid Δ | 90% CI | t p | W p | dz | Power |
|---|---|---|---|---|---|---|---|---|---|
| <10° | 75.0% | 78.3% | +3.26 | −3.68 | [−12.00, +4.60] | .452 | .372 | −0.187 | 18% |
| 10–20° | 40.8% | 50.2% | +9.43 | +9.74 | [−0.20, +19.70] | .108 | .149 | 0.413 | 49% |
| **20–30°** | **38.9%** | **59.6%** | **+20.64** | **+20.82** | **[+10.10, +31.50]** | **.004** | **.008** | **0.824** | **95%** |
| >30° | 54.4% | 55.4% | +0.98 | +0.98 | [−5.70, +7.70] | .802 | .916 | 0.062 | 8% |

Power is one-tailed throughout, matching the primary contrast's convention. **The 20–30° band
remains the Block B result**: +20.8 pp per participant, p = .004, dz = 0.824, 95% achieved power.
It is still by far the largest band effect. The pooled and per-participant
estimators are close (+20.64 vs +20.82). The 10–20° band trends positive (+9.7 pp) but its 90%
interval now spans zero ([−0.20, +19.70]) and its tests do not reach significance (p = .108),
so it is reported as a trend, not a finding — weaker than in the withdrawn P84/P86 draft, where
the same interval cleared zero. The <10° band still differs in sign between the pooled and
per-participant estimators, as before.

Raw band counts: <10 `84/112 → 72/92`; 10–20 `95/233 → 122/243`; 20–30 `51/131 → 84/141`;
>30 `111/204 → 113/204`.

Band eccentricity is still the target's mid-lifetime position, a weak proxy (median lifetime
swing 17.2°, ~31% of catches land in a different band than the mid position). Unchanged caveat.

## End-of-session questionnaire, n = 18

Extracted from the 20 completed forms by `Tools/analysis/esq_extract.py`. **Validation: the
extractor reproduces the recorded 16-form totals exactly (A 71/80, B 46/80, C 45/64, and every
recorded per-item count) before the two 2026-08-11/12 forms (P51, P90) are added.**

Section A was answered by all 20 respondents. P90's Section A was supplied on 2026-08-13, having
been blank in the earlier extract. C3 was skipped by everyone, so Section C is scored over four
items; Section D was not administered.

| Construct | Favourable | Rate |
|---|---|---|
| A — perceived focus benefit | **89/100** | 89% |
| B — perceived awareness cost | **58/100** | 58% |
| C — visual comfort | **55/80** | 69% |

Per item: A1 19/20, A3 19/20, A2 18/20, A5 17/20, A4 16/20. B2 ("still felt aware") 17/20 in
favour; the two items running against the filter are B3 ("noticed side events later") 9/20 and
B1 ("worried I would miss something") 10/20; B4 10/20, B5 12/20. C: C4 16/20, C2 15/20, C1 13/20,
C5 11/20.

P25 remains the one strong dissenter on focus benefit (A 1/5).

## Set construction

**Full-protocol participants (20):** P1 P2 P3 P4 P6 P9 P10 P13 P15/16 P17 P20 P25 P26 P27 P30
P33 P41 P43 P51 P90.

**Block A (19 = 20 − P1).** P1's CPT rounds ran about 15 minutes after the session's Unity blocks
closed, so the arm assignment cannot be verified from the session ledger
(`manuscript-p1-exclusion-audit-2026-08-11.md`, verified from raw timestamps). Settled and final.

**Block B (17 = 20 − P3 − P9 − P43).** Exclusions, all documented:

- **P3** — 539 false alarms on the filter run (557 across the pair); hits provably looser
  (Mann-Whitney p = .0301).
- **P43** — 135 false alarms across the pair (81 and 54), both arms extreme.
- **P9** — experimenter decision on the day: the response rule was not understood during the
  participant's first video block. The one discretionary exclusion; disclosed as such.
- **P15's own Block B** (206 FA / 246 presses); the canonical pair is P16's, P15+P16 one person.

**The pre-registered response-validity rule** (Appendix A): a pair is excluded when its false alarms
across both conditions together outnumber its 80 scheduled markers. Separation on the achieved data:
highest retained pair 66 (P16), lowest excluded 135 (P43); any threshold between those two selects
the same set.

> **Correction, 2026-08-12.** Earlier drafts of this file described this rule as an *amendment* to a
> 30%-false-alarm-rate screen. That was wrong. The pre-registered rule is, and always was, the
> combined absolute form above; the 30%-rate form was introduced by an earlier automated drafting pass
> and was never part of the pre-registration. The manuscript states the combined rule as
> pre-registered throughout, and no amendment is claimed. Settled — do not reopen.

Block A is retained for P3, P9 and P43 — the exclusions concern the marker task alone.

**P4's filter-off value (98.4%) is the experimenter's contemporaneous record.** Still the only
reported-rather-than-measured value. Dropping it now slightly *strengthens* the pool
(p = .0085 → .0104 with dz unchanged), so the disclosure keeps its "changes nothing" character.

## Superseded figures — do not use

| 2026-08-08 (n = 17/15/16) | Withdrawn 2026-08-12 draft (n = 19/18/20, incl. P84/P86) | This document (n = 19/17/18) |
|---|---|---|
| Block A +3.09 pp, p = .0172, dz = 0.645, 12/17, power 81.6% | +3.07 pp, p = .0085, dz = 0.678, 14/19, power 88.4% | **+3.07 pp, p = .0085, dz = 0.678, 14/19, power 88.4% (unchanged)** |
| H1 GEE OR 0.583, p = .0065, 4,125 trials, 33 arms | OR 0.600, p = .0025, 4,681 trials, 37 arms | **OR 0.600, p = .0025, 4,681 trials, 37 arms (unchanged)** |
| Block B +6.00 pp, p = .1046, dz = 0.448, 8/15 | +7.78 pp, p = .0260, dz = 0.575, 11/18 | **+7.35 pp, p = .0434, dz = 0.532, 10/17** |
| Non-inferiority p = .0002; TOST upper p = .133 | p = .000017; TOST upper p = .248 | **p = .000046; TOST upper p = .221** |
| 20–30° band +17.55/+17.78, p = .014, dz = 0.722, power 84% | +20.21/+20.19, p = .0029, dz = 0.819, power 95% | **+20.64/+20.82, p = .004, dz = 0.824, power 95%** |
| 10–20° band, 90% CI clears zero | 90% CI [+0.80, +19.65] clears zero | **90% CI [−0.20, +19.70] spans zero — no longer a finding** |
| 1,200 marker presentations, fifteen pairs | 1,440, eighteen pairs | **1,360, seventeen pairs** |
| ESQ A 71/80, B 46/80, C 45/64 (n = 16) | A 76/85, B 51/90, C 50/72 (n = 18) | **A 89/100, B 58/100, C 55/80 (n = 20)** |
| "eighteen people tested, two short of twenty" | "22 tested: pre-registered 20 full-protocol, plus 2 Block-B-only supplements" | **"20 tested, meeting the pre-registered target exactly"** |

The withdrawn 2026-08-12 draft counted "P84" and "P86" as separate participants. P84 is the same
participant as P90 under a mistyped in-app ID, and P86 has no files and is in no result, so the
tested set is 20. See the correction note at the top of this document and
`provenance/README-p90-blocka-provenance.md`.

---

## Leave-one-out sensitivity (added 2026-08-12)

Regenerate with `python Tools/analysis/loo_sensitivity.py Dissertation/authored/raw`. The script
reuses `blocka_pooled.parse_run` and `blockb_pooled`'s band logic, and independently reproduces the
published full-sample figures (n = 19 / +3.07 pp; n = 17 / +7.35 pp; band +20.82 pp) as a cross-check.

Each result refitted with every participant removed in turn:

| Result | Full sample | Drop largest delta | LOO p range | Significant |
|---|---|---|---|---|
| **Block A (H1)** | +3.07 pp, p = .0085, dz = 0.678 | P15 (+15.83) → +2.36 pp, **p = .0091**, dz = 0.693 | .0046 … .0175 | **19/19** |
| **Block B 20–30° (H2a)** | +20.82 pp, p = .0037, dz = 0.824 | P90 (+60.32) → +18.35 pp, **p = .0077**, dz = 0.768 | .0003 … .0081 | **17/17** |
| Block B pooled (H2b) | +7.35 pp, p = .0434, dz = 0.532 | P17 (+35.00) → +5.62 pp, p = .0857, dz = 0.460 | .0165 … .0863 | 7/17 |

Notes:

- Both **pre-registered** results survive removal of any single participant. H2a survives removal of
  P90 specifically, whose +60.32 pp band delta is the largest in the set.
- The pooled H2b test is the only fragile one. Its two largest contributors are P17 (+35.00 pp) and
  P90 (+30.00 pp); dropping either returns p ≈ .086. This is a borderline p = .0434 at n = 17, not a
  single-participant artefact.
- **H2b's verdict is unchanged in all 17 leave-one-out samples**, because every one remains a *rise*
  and the pre-committed refutation requires a drop greater than 10 pp with equivalence unestablished.
- Reported in Chapter 5, Section 5.7, subsection "Leave-one-out robustness".
