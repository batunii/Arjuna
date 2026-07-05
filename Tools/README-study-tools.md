# Study Tools — CSV Contract, Usage, Session-Day Sequence

Python tooling for the user study in `Dissertation/testing-strategy-v2.md`.
Install deps once: `pip install --user -r Tools/analysis/requirements.txt`.

## The CSV contract (StudyLogger)

Filename `study_P<pid>_<yyyyMMdd_HHmmss>.csv`. One row per frame (~60 Hz heartbeat) plus event rows.
Columns: `t_ms,pid,block,condition,yaw_deg,pitch_deg,roll_deg,head_speed_dps,dr_intensity,mode,event,payload`
(`payload` = `key=value` pairs joined by `;`; heartbeats have empty `event`).

Blocks A and B/C run in **different scenes/builds**, so each participant produces up to TWO CSVs.
The file declares itself via a CONFIG row: `plan=PassthroughScene_BlockA` (4×A conditions, painted
window, ≥4 TLX) or `plan=VideoScene_BlocksBC` (2×B conditions with 24 probes each, fixed window,
≥2 TLX, 3×C conditions or `BLOCKC_SKIPPED`). Conditions are SEMANTIC labels: A1=off/absent,
A2=off/present, A3=on/absent, A4=on/present, B1=off, B2=ColorPop. Practice trials appear under
`block=PRACTICE` (practice probe ids 901–906) and are excluded everywhere.

Event labels: `SESSION_START/END, CONFIG, GUARD_FAIL, PRACTICE_START/END, BLOCK_START/END,
WINDOW_LOCKED(source=painted|fixed), CONDITION_START/END, CPT_RUN_START/END, CPT_PANEL_PLACED,
CPT_ONSET(idx;kind), CPT_RESULT(idx;outcome[;rt_ms]), PROBE_SCHEDULE, PROBE_ONSET(id;band;az;el),
PROBE_RESULT(id;outcome[;rt_ms]), PROBE_DEFERRED(id;delay_s), PRESS, FALSE_ALARM, MODE_SET,
SAMPLER_MODE_START/END, BLOCKC_SKIPPED(reason), TLX_START/END, BREAK_START/END, INCIDENT`.

## Tools

| Tool | Purpose | Run |
|---|---|---|
| `make_probe_schedule.py` | Deterministic Block B probe schedules → `Assets/StreamingAssets/StudySchedules/` | `python Tools/make_probe_schedule.py --all` (canonical seeds B1=101, B2=202 — already generated; regenerating with the same seeds is a no-op) |
| `validate_session.py` | Integrity check, run before the participant leaves; plan-aware; exit 0/1 | `python Tools/validate_session.py <csv>` |
| `analysis/parse.py` | CSVs → tidy `cpt_trials`/`probe_trials`/`conditions`/`events` tables | `python Tools/analysis/parse.py <dir> --out parsed` |
| `analysis/h1_lmm.py` | H1: GEE interaction + paired robustness + SESOI bootstrap + MC1 | `python Tools/analysis/h1_lmm.py parsed/cpt_trials.csv` |
| `analysis/h2_tost.py` | H2a central RT; H2b peripheral TOST (Δ=10 pp) | `python Tools/analysis/h2_tost.py parsed/probe_trials.csv --events-csv parsed/events.csv` |
| `analysis/likert.py` | Block C Friedman + Bonferroni Wilcoxon (paper ratings CSV: `pid,mode,item,rating`) | `python Tools/analysis/likert.py ratings.csv --n-recruited 24` |
| `analysis/power_check.py` | Post-pilot SESOI→absolute margin + power at N=20 (lock-in memo input) | `python Tools/analysis/power_check.py --cost-mean 0.06 --cost-sd 0.05` |
| `analysis/report.py` | Everything + Holm + decision-table row → markdown | `python Tools/analysis/report.py <sessions_dir> --tlx-csv tlx.csv --ratings-csv ratings.csv --out results.md` |
| `analysis/make_synthetic.py` | Synthetic pipeline test data (NOT study data) | `python Tools/analysis/make_synthetic.py --out synth` |

Paper-entered data formats: TLX `pid,condition,mental,physical,temporal,performance,effort,frustration`
(0–100, steps of 5); Block C ratings `pid,mode,item,rating` (modes COLORPOP/SOFTDARK/HARDDARK; items
comfort/focus_benefit/willingness; 1–7).

## Session-day sequence (per participant, before they leave)

```powershell
adb pull /sdcard/Android/data/<package>/files/. .\logs\incoming\
python Tools\validate_session.py .\logs\incoming\study_P07_<stamp>.csv   # BOTH files
# FAIL -> apply testing-strategy-v2.md section 8.4 re-run rules NOW; log in incidents.md
# PASS -> archive to two locations:
Copy-Item .\logs\incoming\study_P07_* .\logs\archive\ ; <cloud sync folder>
```

Analysis runs only after data collection closes: `parse.py` on the archive dir, then `report.py`.
Dry-run the whole pipeline anytime with `make_synthetic.py` output.
