# P25 Block A — superseded first attempt (2026-08-06)

**Status: RESOLVED. P25 re-ran Block A the same evening and the re-run is canonical.**
The complete pair now lives in `../blocka/`:

| Canonical run (in `../blocka/`) | Slot | Seed | Raw | Accuracy | RT median |
|---|---|---|---|---|---|
| `blockA_nback1_P25_A4_FILTER_2026-08-06T17-34-08-338Z.csv` | A4 | 253 | 134/139 | 96.40% | 839 ms |
| `blockA_nback1_P25_A2_NOFILTER_2026-08-06T17-40-48-700Z.csv` | A2 | 251 | 120/139 | 86.33% | 946 ms |

P25 delta **+10.07 pp** (filter better), RT 107 ms faster under filter. Both `140x2s` gen v4,
`nback1_lures`, PROTEST distractors — the same version as P26/P27. The two runs are six
minutes apart, so fatigue and time-of-day are matched across arms.

## Why the files in this folder are not used

The first attempt produced only a **no-filter** main run. The ledger marks the
passthrough-Filter block complete at 15:54:46 local, but the only 140-trial run from that
attempt started 15:55:20 local — after that block closed. Everything recorded under the
first attempt's Filter arm is practice.

| File (here) | Trials | Raw | Accuracy | Status |
|---|---|---|---|---|
| `blockA_nback1_P25_A2_NOFILTER_2026-08-06T14-55-20-503Z.csv` | 140 | 138/139 | 99.28% | Valid run, superseded by the 17:40 re-run |
| `blockA_nback1_P25_PRACTICE_FILTER_2026-08-06T14-52-03-546Z.csv` | 40 | 37/39 | 94.87% | Practice |
| `blockA_nback1_P25_PRACTICE_FILTER_2026-08-06T14-50-25-672Z.csv` | 40 | 29/39 | 74.36% | Practice |
| `blockA_nback1_P25_PRACTICE_FILTER_2026-08-06T14-47-08-094Z.csv` | 28 | 6/26 | 23.08% | Practice, aborted (10 no-response) |

Substituting a practice run for the missing main run was considered and rejected on
2026-08-06: practice uses the fixed `PRACTICE_SEED` (identical for every participant),
runs 40 trials rather than 140 (a separate version key), and P25's practice sequence
climbed 23.08% → 74.36% → 94.87%, so the number would have measured learning.
`Tools/analysis/blocka_timecourse.py` drops `PRACTICE` runs at `:87` for these reasons.

Filenames are UTC; local times above are UTC+1.

## Two provenance notes for the write-up

**Slot A2 was re-used.** The re-run's no-filter arm is slot A2 / seed 251 — the same seed as
the superseded 14:55 run, so P25 saw that trial sequence twice. This is not a familiarity
confound: a 1-back task is a running comparison that does not reward sequence knowledge, and
the repeat scored *worse* (86.33% vs 99.28%), which is the opposite of what prior exposure
would produce. The filter arm (A4 / seed 253) was a sequence P25 had not seen.
`block-a-cpt.html:627-635` warns on slot re-use but is a confirm dialog, not a hard block —
the same route by which P9 ended up with two A3 runs.

**The 99.28% → 86.33% drop on identical trials is unexplained** and is worth a line in the
incident log. Same participant, same sequence, ~3 hours apart, 322 ms slower. It bears on
which session state is representative, independent of the filter manipulation.

Block B for P25 is unaffected throughout and remains canonical in `../`.
