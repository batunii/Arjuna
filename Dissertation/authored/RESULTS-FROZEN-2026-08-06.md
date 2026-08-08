# Frozen results for the dissertation — 2026-08-06

Every number the manuscript quotes comes from here. Regenerate with:

```
python3 Tools/analysis/blocka_pooled.py Dissertation/authored/raw/blocka
python3 Tools/analysis/blockb_pooled.py Dissertation/authored/raw
```

15 people tested in total. Block A has 14 usable pairs, Block B has 13.

## Block A — H1, workstation focus (Hard Dark), n = 14

| | |
|---|---|
| Filter | 95.53% (SD 4.58) |
| NoFilter | 92.19% (SD 7.13) |
| Mean delta | **+3.34 pp** (SD 5.21, median +2.28) |
| 95% CI | +0.33 to +6.34 (excludes zero) |
| Paired t | t(13) = 2.40, p = .032 |
| Wilcoxon | W = 19.0, p = .035 |
| Effect size | dz = 0.641 |
| Direction | 10/14 improved |
| Achieved power | 60% (n = 22 needed for 80%) |

Pooled across task versions per the 2026-08-06 decision. Kruskal-Wallis across the four
versions H = 13.59, p = .0035, driven by the deliberate v4 difficulty change.

**Must be flagged in the write-up:** P4's NoFilter value (98.4%) is experimenter-reported,
not measured. No CSV was exported for that arm. It is the only reported-rather-than-measured
value in the analysis.

## Block B — pooled hit rate (H2b), n = 13

| | |
|---|---|
| Filter | 55.58% (SD 9.02) |
| NoFilter | 50.00% (SD 17.02) |
| Mean delta | +5.58 pp (SD 13.27, median +7.50) |
| 90% CI | −0.98 to +12.14 |
| 95% CI | −2.44 to +13.60 |
| Paired t | t(12) = 1.51, p = .156 |
| Wilcoxon | W = 21.0, p = .175 |
| Effect size | dz = 0.420, 7/13 positive |
| TOST ±10 pp | p = .126 (two-sided equivalence not established) |
| Non-inferiority vs −10 pp | **p = .0006 — passes** |

**H2b is not refuted.** Its pre-registered refuting observation requires equivalence not
established *and* a point estimate showing a drop greater than 10 pp. The estimate is a
**rise** of 5.58 pp, so the refuting observation did not occur.

The two-sided TOST is not established only because the upper test fails (t = −1.20,
p = .126): the data cannot exclude a *benefit* above 10 pp. That is a specification mismatch,
not a safety finding. H2b asks a one-directional question, and the matched instrument is the
non-inferiority test against −10 pp, which passes at p = .0006. Report both; do not
substitute one for the other after the fact.

## Block B — eccentricity bands (mid-lifetime proxy), n = 13

| Band | NoFilter | Filter | Delta | t p | W p |
|---|---|---|---|---|---|
| <10° | 73.8% | 79.2% | +5.36 | .753 | .656 |
| 10–20° | 41.3% | 45.9% | +4.61 | .403 | .465 |
| **20–30°** | **38.6%** | **55.1%** | **+16.53** | **.034** | **.049** |
| >30° | 54.5% | 56.4% | +1.92 | .692 | .938 |

20–30° dz ≈ 0.66, achieved power 59%. All four bands now trend positive; only 20–30° is
individually reliable, and it is three to eight times larger than any other band.

Band eccentricity is the target's mid-lifetime position, a weak proxy: targets move, median
lifetime swing 17.2°, and ~31% of catches land in a different band than the mid position.

## Block B exclusions (documented, not inferred)

- **P3** — 539 false alarms on the filter run; hits provably looser (Mann-Whitney p = .0301)
- **P9** — experimenter decision, participant confused during the video blocks
- **P15** — own Block B excluded (206 FA / 246); P15 and P16 join as one participant

Block A retained for P3 and P9.

## End-of-session questionnaire, n = 13

| Construct | Favourable |
|---|---|
| A, perceived focus benefit | 57/65 |
| B, perceived awareness cost | 38/65 |
| C, visual comfort | 38/52 (four answered items; C3 answered by nobody) |

A1 12/13, A2 11/13, A3 12/13. **No longer unanimous** — the n = 10 write-up said all three
were answered identically by every participant, and that is no longer true.

Scoring validated: the extractor reproduces the previously recorded n = 10 totals
(A 47/50, B 29/50, C 30/40) exactly before adding the three 2026-08-06 sessions.

## Superseded figures — do not use

| Old (n = 11 / n = 10) | New |
|---|---|
| Block A +3.65 pp, 90% CI +0.28..+7.02, p = .036, dz = 0.73, 9/11, power 59% | +3.34 pp, 95% CI +0.33..+6.34, p = .032, dz = 0.641, 10/14, power 60% |
| Block B pooled +3.9 pp, TOST CI −4.9..+12.6 | +5.58 pp, 90% CI −0.98..+12.14; safety claim holds, two-sided TOST p = .126 not established |
| 20–30° band +18.6 pp (40.0%→58.2%), p = .028, dz = 0.78, power 64% | +16.53 pp (38.6%→55.1%), p = .034, dz ≈ 0.66, power 59% |
| Bands: <10° −1.1, 10–20° +2.6, >30° −1.5 | +5.36, +4.61, +1.92 (all positive) |
| ESQ A 47/50, B 29/50, C 30/40; A1–A3 unanimous | A 57/65, B 38/65, C 38/52; 12/13, 11/13, 12/13 |
| 17 participants for 80% power | 22 |
