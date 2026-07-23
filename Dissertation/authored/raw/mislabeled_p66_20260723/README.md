# Mislabeled pid 66 → participant 6 (2026-07-23)

Participant 6's two video blocks were run with a mistyped pid (**66** instead of **6**;
confirmed by the experimenter the same day). The files here are the UNMODIFIED originals
as pulled from the headset; corrected copies (filename `_P6_`, `pid` column rewritten
66 → 6, everything else byte-identical) live in `../` and are the canonical versions for
analysis.

| Original (here) | Status |
|---|---|
| `authored_results_P66_NOFILTER-A_20260723_170602.csv` | Full run → corrected copy in `../` |
| `authored_results_P66_FILTER-B_20260723_172119.csv` | Full run → corrected copy in `../` |
| `authored_results_P66_NOFILTER-A_20260723_150405.csv` | **Aborted before the task began** (experimenter account, 2026-07-23): the participant felt uneasy and removed the headset before properly starting; the video kept playing unattended, so the logged targets (~26, up to ~220 s) were expiring in front of nobody. NOT scored, and — because she wasn't watching — **no practice-effect concern** for her real NoFilter-A run at 17:06. No ledger BLOCK row, consistent with the abort. |

Ledger notes (`../session_ledger.csv`): P6's PLAN row (pid 6, `BF`, `P-F`) and their
Block A BLOCK row are correctly pid 6; only the two video BLOCK rows carry pid 66.
There is **no PLAN row for 66**, so no counterbalance cell was consumed by the typo —
the auto-assignment is unaffected. The device-side files/ledger were left untouched
(never adb-push the ledger; the P66 filenames on-device still match these originals
byte-for-byte, so `awipe`'s size-match still works).

Seed caveat: the runs were seeded/labelled as pid 66 at runtime, so any payload fields
derived from pid reflect 66. Analysis keys on the `pid` column, which is now 6.
