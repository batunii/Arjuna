# Renumber spec — propagate the real testing data through the dissertation

Single source of truth: `Dissertation/authored/analysis-2026-08-12-pooled-final.md`.
Do **not** recompute or reinterpret anything. Propagate these values only.

## Canonical values

| Quantity | Correct value | Words |
|---|---|---|
| People tested | 20 | twenty |
| Supplementary / Block-B-only participants | **none** | — |
| Block A paired participants | 19 | nineteen |
| Block B paired participants | **17** | seventeen |
| ESQ forms completed | **18** | eighteen |
| Block A delta | +3.07 pp, 95% CI +0.89 to +5.25 | — |
| Block A trial-level | OR 0.600, p = .0025, 40% odds reduction | — |
| Block B pooled | +7.35 pp, p = .0434 | — |
| 20–30° band, per-participant | **+20.82 pp → write as "20.8 points"** | — |
| 20–30° band, pooled | +20.64 pp (only if explicitly labelled pooled) | — |
| 20–30° band p | **.004** | — |
| Power | 88% (H1), 95% (band), one-tailed | — |

Withdrawn values that must not survive anywhere: **22 / twenty-two tested**, **Block B eighteen**,
**ESQ twenty**, **+20.2 / 20.21**, **p = .003**, **+7.78**, **p = .026**, **dz 0.819**,
**ESQ A 81/90, B 56/100, C 55/80**, and any reference to **Block-B-only supplements**.

## Sites to fix

### 1. `thesis.tex:113-116` — abstract, sample sentence
Currently claims twenty-two tested, two driving-block supplements added to offset exclusions, and
eighteen driving pairs. Replace with: twenty tested, all completing the full two-block protocol and
meeting the pre-registered target; completed sample of nineteen workstation and **seventeen** driving
pairs. Delete the supplements clause entirely. Keep the existing power sentence intact.

### 2. `thesis.tex:124` — `20.2 points` → `20.8 points`

### 3. `thesis.tex:121` — `showed no loss` → `showed no measured loss`
One word. Restores wording removed from the previous version; consistent with the new
"Leave-one-out robustness" subsection, which shows the pooled rise does not survive leave-one-out.
Flag this in your report as a wording change rather than a number change.

### 4. `thesis.tex:137` — acknowledgements, `the twenty-two people` → `the twenty people`

### 5. `README.md:71-76` — "Status of the work reported"
Rewrite to: collection complete, twenty people tested meeting the pre-registered target, no
supplementary participants, Block A nineteen usable paired participants, Block B **seventeen**,
**eighteen** end-of-session questionnaires. **Delete** the sentence claiming a Block-B supplement is
reported as a protocol deviation in the Chapter 5 status note — that note says the opposite, so the
cross-reference is false.

### 6. `README.md:77-79` — regeneration provenance
Currently names only `blocka_pooled.py` and `blockb_pooled.py`. Add `esq_extract.py` (questionnaire
totals) and `loo_sensitivity.py` (leave-one-out table).

### 7. `content/appendix.tex:284` — threats table, order/learning row
Delete the trailing clause "The two Block-B-only supplements ran the driving block with no preceding
workstation block, so their pairs carry no cross-block fatigue". With twenty full-protocol
participants, "B is second for every full-protocol participant" stands unqualified. Keep the rest of
the row and the `\\` row terminator intact.

### 8. `content/figures/generate_all.py:206-209` — stale figure annotation
`ax1.annotate('* p = .003\n+20.2 pp (per participant)', ...)` → `p = .004` and `+20.8 pp`.
Update the explanatory comment above it that also says `+20.2`. This annotation currently contradicts
the lower panel of its own figure, which reads +20.8.

## Then

9. Regenerate the figures: `python content/figures/generate_all.py` (run from the directory its paths
   expect; check the script header). Confirm `fig_eccentricity_h2a.png` mtime updates and that the
   upper-panel annotation now reads p = .004 / +20.8.
10. Rebuild the PDF: `pdflatex` → `bibtex` → `pdflatex` × 2 on `thesis.tex`. Report exit codes. If no
    TeX toolchain is installed, say so plainly and mark the rebuild **blocked** — do not fake it.
11. Verify: grep the whole `Dissertation/25377738-dissertation-submission/` tree for every withdrawn
    value listed above and confirm zero hits in `.tex` and `.md`. Then grep the built PDF's text layer
    for `twenty-two` and confirm zero hits.

## Constraints

- Do **not** touch anything under `Dissertation/authored/raw/` — raw session data is out of scope.
- Do **not** change any statistic, recompute any analysis, or edit the authority file.
- Do **not** alter the new `\subsection{Leave-one-out robustness}` in `ch5.tex`.
- Do **not** touch the post-hoc power framing (88%/95%) — separate open decision, not in scope.
- `ch1.tex`, `ch2.tex`, `ch5.tex`, `ch6.tex` are believed already on the correct set. Verify by grep;
  fix only if a withdrawn value is actually present.
