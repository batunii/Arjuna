# P90 Block A — filename collision, pid relabel, and timestamp restoration

**Internal analysis/provenance record. Not part of the dissertation text.**
Date: 2026-08-12 · Session date: 2026-08-11 · Affects: Block A only

## What happened

The headset wrote all five of this session's Block A runs to a **single reused filename**,
`blockA_nback1_P90_A3_NOFILTER_2026-08-11T15-28-29-233Z.csv`. On download the browser
disambiguated them with `(1)`–`(5)` suffixes. The five originals are retained at
`C:/Users/syson/Downloads/` and are the authoritative source for this session.

The runs were subsequently split into four correctly-named files in
`Dissertation/authored/raw/blocka/` (commit `91aaed7`, "fix P90 block-A filenames"). During that
pass two changes were made to the copies:

1. the `pid` column was set from `84` to `90`;
2. the `t_ms` column was flattened to a single Excel-rounded value (`1.78646E+12`) in all rows,
   an unintended spreadsheet round-trip that destroyed per-trial timing.

## Participant identity

The payload disagreed with the filename at source:

| Field | Value | Origin |
|---|---|---|
| device filename | `P90` | reused/stale filename on device |
| `pid` column | `84` | runtime `m_pid` entered at session start |
| CPT seed | `841` / `842` | computed at runtime as `m_pid * 10 + slot` (`ConditionSequencer.cs:434`) |
| practice seed | `999` | fixed constant (`ConditionSequencer.cs:520`) |

Per the author, **84 and 90 are the same participant**; the in-app ID was mistyped at session
start, so the payload and seed carry `84` while the participant is `90`. `90` is therefore the
canonical ID and the analysed `pid` column is set to `90`.

This is the same class of defect as the previously documented P66 → P6 relabel: payload fields
derived from `m_pid` retain the mistyped value, and analysis keys on the corrected `pid` column.

## Run mapping

Matched by CPT seed and by an MD5 signature over all trial columns excluding `t_ms` and `pid`.

| Original (Downloads) | Seed | Rows | Analysed file (`raw/blocka/`) |
|---|---|---|---|
| `…-233Z(3).csv` | 841 | 283 | `blockA_nback1_P90_A2_FILTER_2026-08-11T15-21-57-653Z.csv` |
| `…-233Z(1).csv` | 842 | 283 | `blockA_nback1_P90_A3_NOFILTER_2026-08-11T15-21-57-653Z.csv` |
| `…-233Z(2).csv` | 842 | 283 | byte-identical duplicate of `(1)`; not used |
| `…-233Z(5).csv` | 999 | 83 | `blockA_nback1_P90_PRACTICE1_FAIL_FILTER_…csv` |
| `…-233Z(4).csv` | 999 | 83 | `blockA_nback1_P90_PRACTICE2_PASS_FILTER_…csv` |

Original run start times (UTC, from restored `t_ms`): practices 15:18:02 and 15:20:14,
A2 (`seed=841`) 15:22:00, A3 (`seed=842`) 15:28:38.

## Restoration performed 2026-08-12

Each analysed file was rebuilt from its matched original with `pid` set to `90` and `t_ms` intact.

- **Trial data unchanged** — the MD5 signature over all non-`t_ms`, non-`pid` columns is identical
  before and after for all four files.
- **`t_ms` restored** — 283/283, 282/283, 83/83, 83/83 distinct values (was 1 in every file).
  The 282/283 file contains one genuine duplicate timestamp, present in the original.
- **Analysis unchanged** — `blocka_pooled.py` returns n = 19, Filter 95.08% (SD 4.42),
  NoFilter 92.01% (SD 6.57), +3.07 pp, t(18) = 2.95, p = .0085, W = 23.0, dz = 0.678, 14/19
  positive, identical to the pre-restoration run. `t_ms` is not an input to accuracy.
- **Pre-restoration copies** retained in `p90-pre-restore-backup/` beside this file, outside
  `raw/blocka/` so the analysis glob (`blockA_nback1_*.csv`) cannot pick them up.

Per-trial timing for this session is now usable again; it was not recoverable from the repository
copies alone.

## Consistency of the available data

Cross-checking every exported file against the ledgers as they stand on disk:

| pid | Block A files | Block B files | ledger `passthrough` | ledger `video` |
|---|---|---|---|---|
| 84 | none | none | 2 (0811) | **0** |
| 90 | A2, A3 + 2 practice | FILTER-A, NOFILTER-B | 2 (0812, 2.4 s apart) | 2 (0812, 12.1 min apart) |

This is internally consistent with a single participant and a single run of each block:

1. **Block A, 11 August.** Run under a mistyped in-app ID, so the ledger logged the session as
   pid 84 with two `passthrough` blocks, and the exported CSVs carried `pid=84` / `seed=841,842`.
   The ID was corrected to 90 and the exports relabelled accordingly.
2. **Block B, 12 August.** Run under the correct ID, logged as pid 90 with two `video` blocks
   12.1 minutes apart, and exported as `authored_results_P90_*`.
3. pid 90's two `passthrough` rows on 12 August are **2.4 seconds apart** — block markers written
   as the sequencer stepped past Block A, which had been completed the previous day. They are not a
   second Block A run.
4. **pid 84 has zero `video` blocks and zero exported files.** There is no second Block B run in the
   available data, and no Block B record under pid 84.

pid 84 therefore falls into the same category as pids 8, 12 and 66: ledger rows with no exported
files, marking a session label that was superseded. It is not a participant with missing data.

## Open question — not resolved

The CPT runs are timestamped 15:22:00 and 15:28:38 UTC, **6.6 minutes apart**. The corresponding
`passthrough` BLOCK rows for this session in `session_ledger_20260811.csv` are at 15:40:00 and
15:52:00 UTC, **12.0 minutes apart** — 12 to 24 minutes after the runs they should bracket, with a
different spacing.

The practical consequence is a cross-reference a reader has to be told about: the Block A **data**
sits under pid 90, while the ledger blocks corroborating a real Block A run sit under pid 84. Anyone
tracing pid 90 through the ledger alone finds only the 2.4-second passthrough markers and no run.
The arm assignment for this session therefore rests on the exported filenames and payloads, with the
ledger corroboration reachable only via the superseded pid-84 rows. That is followable but not
self-evident, which is the reason for this file.

The most likely benign explanation for the offset is that the CPT panel was run standalone ahead of
the sequencer blocks that day. Confirming it from the session record would close the item.

**Reproducibility limit.** `session_ledger_20260811.csv` as it now stands carries Excel-rounded
timestamps (21 distinct values across 123 rows, e.g. `1.78646E+12`), so per-block gap analysis is no
longer computable from that file. `session_ledger_20260812.csv` retains full precision. The CPT run
times above come from the restored `t_ms` in the Downloads originals.

**Handling note.** Opening these CSVs in a spreadsheet silently truncates the millisecond epochs;
this is what damaged the Block A exports and the 0811 ledger. Inspect them with `head`, `grep`, a
text editor, or the analysis scripts instead.
