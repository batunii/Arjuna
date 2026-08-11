# Block B re-analysis with P1 removed — verified number set

> ## SUPERSEDED — NOT APPLIED, AND MUST NOT BE APPLIED
>
> The author reversed this decision on 2026-08-11: *"Don't remove p1, as we don't have a reason to
> remove him from either experiments. Find if there is a reason, otherwise, don't."*
>
> A reason **was** then sought and the finding is recorded in
> `manuscript-p1-exclusion-audit-2026-08-11.md`. Outcome:
> - P1's **Block A** exclusion is justified and verified — it stays.
> - P1's **Block B** inclusion is justified and consistent — it stays.
>
> **Therefore no published number changes.** The n = 17 / n = 15 / ESQ n = 16 set stands exactly as
> published. The numbers below are retained only as a record of what was computed and as evidence
> that the analysis pipeline reproduces its published outputs. **Do not copy any figure from the
> "NEW (n=14)" columns into the manuscript.**

**Trigger:** author decision 2026-08-11, "P1 needs to be removed from both the batches, and if any
number or analysis includes that it needs to be updated." — **subsequently reversed.**

**Status:** numbers computed and validated, then **superseded. Never applied to the manuscript.**

**Note on scope:** P1 was *already* excluded from Block A (`analysis-2026-08-08-pooled-n17.md:146`,
and P1 is absent from the Block A slopegraph `pids` array). So "remove from both blocks" changes
**Block B only**. Every Block A statistic is unaffected.

---

## Method and validation

Re-run of `Tools/analysis/blockb_pooled.py` with `"1"` added to its `EXCLUDE` dict. The tracked
script was **not modified**; a scratch copy was used (`.scratch-reanalysis/blockb_noP1.py`).

Before trusting the re-run, the unmodified script was executed against the same raw data and
reproduced every published n=15 value exactly:

| Quantity | Script output | Published |
|---|---|---|
| Block B mean delta | `+6.00 pp (SD 13.39, median +7.50)` | +6.00 pp |
| paired t | `t(14) = 1.74, p = 0.1046` | t(14) = 1.74, p = .1046 |
| dz / positive | `dz = 0.448   positive: 8/15` | dz = 0.448, 8/15 |
| 20–30° band | `+17.55` pooled `+17.78` per-pid, `t p=0.014 W p=0.021` | +17.55 / +17.78, p = .014 / .021 |
| 20–30° 90% CI | `[+6.58, +28.97]` | `[+6.6, +29.0]` (ch5 Table 5.2) |
| 90% CI on H2b | `−0.0892 .. +12.0892` | −0.09 to +12.09 |
| non-inferiority p | `0.000196` | .0002 |
| TOST upper p | `0.1333` | .133 |
| Power method | `dz=0.645, n=17 → 81.6%` | 81.6% (Block A) |
| Power method | `dz=0.722, n=15 → 84.4%` | 84% (20–30° band) |

Every published figure reproduces. The re-run is trustworthy.

---

## NEW Block B numbers (n = 14)

### H2b — overall paired hit rate

| Quantity | OLD (n=15) | **NEW (n=14)** |
|---|---|---|
| pairs | 15 | **14** |
| Filter | 56.67% (SD 8.85) | **56.25% (SD 9.03)** |
| NoFilter | 50.67% (SD 16.54) | **49.46% (SD 16.47)** |
| mean delta | +6.00 pp (SD 13.39) | **+6.79 pp (SD 13.53)** |
| median | +7.50 | +7.50 |
| paired t | t(14) = 1.74, p = .1046 | **t(13) = 1.88, p = .0832** |
| Wilcoxon | W = 27.0, p = .1089 | **W = 21.0, p = .0862** |
| dz | 0.448 | **0.502** |
| positive | 8/15 | **8/14** |
| **90% CI** | **−0.09 to +12.09** (spans zero) | **+0.38 to +13.19** (**excludes zero**) |
| non-inferiority p | .000196 | **.000231** |
| TOST upper one-sided p | .1333 | **.1951** |
| TOST p (max) | .133 | **.195** |
| false alarms/run | off 7.3 vs on 12.7 (9/15) | **off 6.1 vs on 12.4 (9/14)** |

### H2a — eccentricity bands (mid-lifetime proxy)

**OLD (n = 15)**

| Band | Off pooled | On pooled | Pooled Δ | Per-pid Δ | dz | 90% CI | t p | W p | Power |
|---|---|---|---|---|---|---|---|---|---|
| <10° | 75.0% | 79.8% | +4.76 | −2.50 | −0.124 | [−11.67, +6.67] | .638 | .520 | 11.8% |
| 10–20° | 41.5% | 47.9% | +6.34 | +6.77 | +0.291 | [−3.80, +17.34] | .278 | .379 | 28.4% |
| **20–30°** | **40.2%** | **57.7%** | **+17.55** | **+17.78** | **0.722** | **[+6.58, +28.97]** | **.0143** | **.021** | **84.5%** |
| >30° | 55.0% | 55.6% | +0.56 | +0.56 | +0.034 | [−6.81, +7.92] | .896 | .894 | 6.4% |

**NEW (n = 14, P1 removed)**

| Band | Off pooled | On pooled | Pooled Δ | Per-pid Δ | dz | 90% CI | t p | W p | Power |
|---|---|---|---|---|---|---|---|---|---|
| <10° | 73.9% | 77.6% | +3.72 | −2.68 | −0.128 | [−12.57, +7.22] | .640 | .520 | 11.7% |
| 10–20° | 40.1% | 49.0% | +8.90 | +9.34 | +0.429 | [−0.97, +19.66] | .133 | .187 | 45.0% |
| **20–30°** | **38.9%** | **57.8%** | **+18.87** | **+18.93** | **0.754** | **[+7.05, +30.82]** | **.0144** | **.023** | **84.7%** |
| >30° | 53.6% | 54.2% | +0.60 | +0.60 | +0.035 | [−7.36, +8.55] | .897 | .894 | 6.4% |

Raw band counts (n=14): <10 `68/92 → 59/76`; 10–20 `77/192 → 98/200`;
20–30 `42/108 → 67/116`; >30 `90/168 → 91/168`.

### Derived counts

| Quantity | OLD | NEW |
|---|---|---|
| Block B marker presentations | 1,200 (15 × 2 × 40) | **1,120 (14 × 2 × 40)** |
| "fifteen driving pairs" | fifteen | **fourteen** |
| Block A | n = 17, +3.09 pp, t(16) = 2.66, p = .0172, dz = 0.645, 12/17, power 81.6% | **unchanged** |
| End-of-session questionnaire | n = 16 | **unchanged** — no P1 form exists (16 `post-study-questionnaire-*.docx`, none for P1) |

---

## Two interpretive consequences that need prose, not find-and-replace

### 1. The H2b 90% CI now excludes zero
OLD: −0.09 to +12.09 (spans zero). NEW: **+0.38 to +13.19**.

Chapter 5 currently argues from a CI that spans zero. At n=14 the one-sided reading is now
significant at α=.05 (two-tailed p = .083, so one-tailed p = .042). This **cannot** be handled by
substituting numbers — §5.7.2's argument changes. It must be rewritten deliberately, and the
pre-committed decision table re-applied to the new value rather than back-fitted to it.

### 2. Equivalence got *further* from being established
TOST p rose from .133 to **.195**. The manuscript's existing verdict — "formal equivalence within
±10 points is not established at this sample" — still holds and is now more strongly true. The
non-inferiority result also still holds (p = .0002).

### 3. H2a is unaffected in substance and slightly stronger
Still the only significant, adequately powered band: p = .014, dz 0.754, power 84.7%. The headline
figure changes **+17.8 → +18.9** (per participant), pooled **+17.55 → +18.87**. Note that at n=14
the pooled and per-participant estimates nearly coincide (18.87 vs 18.93), which reduces but does
not remove the estimator-labelling issue behind decision D3.

---

## Every manuscript site that must change

Not yet applied. To be handed to the fix worker as an exact list.

- `thesis.tex` abstract — driving pairs count; any Block B figure quoted
- `content/ch1.tex:193` (C3) — "seventeen workstation and fifteen driving pairs"
- `content/ch5.tex:226` — "1,200 marker presentations across the fifteen usable pairs"
- `content/ch5.tex:325` — achieved sample statement
- `content/ch5.tex:329–333` — the exclusion sentence (also the B2 factual error)
- `content/ch5.tex` §5.7.2 — H2b: all statistics **and the argument**
- `content/ch5.tex:443` Table 5.2 — all four band rows
- `content/ch5.tex` §5.7.3 and the band discussion — dz, power, CI
- `content/ch6.tex` — any restatement of Block B figures
- `content/appendix.tex:189` — "seventeen workstation and fifteen driving pairs"
- `content/figures/generate_all.py` — `delta` array, band bars, annotation, N labels; regenerate PNGs
- `Dissertation/authored/` — a new frozen results file superseding
  `analysis-2026-08-08-pooled-n17.md`, generated from the scripts, not hand-edited
- `Dissertation/rewrite-factbase.md` — updated **forward** from the new frozen file

---

## Reproduction commands

```bash
cd C:/Users/syson/Documents/Code/Unity-PassthroughCameraApiSamples

# baseline reproduction (validates the method)
python Tools/analysis/blockb_pooled.py Dissertation/authored/raw

# P1-excluded re-run
sed 's|^EXCLUDE = {|EXCLUDE = {\n    "1": "excluded from both blocks per author decision 2026-08-11",|' \
    Tools/analysis/blockb_pooled.py > .scratch-reanalysis/blockb_noP1.py
python .scratch-reanalysis/blockb_noP1.py Dissertation/authored/raw
```

TOST/CI/dz/power were recomputed independently in Python (scipy 1.18.0, numpy 2.4.6) from the
per-participant delta vectors printed by the script, and the method was validated by reproducing
every published n=15 value first.

**Scratch directory `.scratch-reanalysis/` is untracked and safe to delete.**
