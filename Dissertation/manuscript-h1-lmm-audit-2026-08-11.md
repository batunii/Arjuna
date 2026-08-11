# H1 pre-registered primary test — audit and result

**Finding (review round 2, reviewer 3, [W14]):** `content/appendix.tex:153` (Table A.4) pre-registers
H1's **primary** test as a trial-level logistic mixed model, with the paired *t*-test as the
**robustness check**. Chapter 5 reports only the *t*-test and Wilcoxon. `grep -ni "mixed model\|LMM\|
odds ratio" content/ch5.tex` returns nothing. The pre-registered effect size, "odds ratio from the
LMM", is also unreported.

In a study whose central rhetorical asset is pre-registration, silently reporting the robustness
check as the headline while omitting the primary test is the exact pattern
`.claude/skills/dissertation-writing/SKILL.md` forbids: *"If a pre-registered test fails, say it
failed … Never quietly substitute a softer test."*

**So I fitted it.** The result is favourable, and the fix is to report it rather than to disclose a
failure.

---

## What Table A.4 pre-registered

> **H1** | Linear mixed model on trial-level errors (logistic LMM: error ~ vignette +
> (1 | participant)); the vignette term is the test | Paired *t*-test on per-participant error rate,
> NoFilter vs Filter; Wilcoxon signed-rank if Shapiro–Wilk rejects normality | dz on the paired
> difference; **odds ratio from the LMM**

## Data

Trial-level rows parsed from `authored/raw/blocka/*.csv`, reusing `Tools/analysis/blocka_pooled.py`'s
own `parse_run` and `ARM_OVERRIDES` so arm assignment is identical to the published analysis.
Practice files excluded; `outcome=first_unscored` excluded. Correct = `hit` + `correct_reject`;
error = `miss` + `commission` + `no_response`.

```
TRIALS=4125  participants=17  arms=33
  (matches the manuscript's "4,125 file-backed scored Block A trials": True)
error rate  off=8.36%  on=4.99%   ->  accuracy off=91.64%  on=95.01%
```

The trial count reproducing the manuscript's own 4,125 **exactly** is the validation that this parse
matches the published pipeline. 33 arms rather than 34 because P4's filter-off arm has no exported
file, so it contributes to the participant-level *t*-test (via the experimenter's record) but not to
a trial-level model. That is worth one clause if this is reported.

## Result

| Model | Effect | 95% interval | p |
|---|---|---|---|
| **Logistic GEE, exchangeable, clustered by participant** | **OR 0.583** | [0.396, 0.860] | **.0065** |
| **Random-intercept logistic GLMM** (`error ~ vignette + (1\|pid)`) | **OR 0.578** | [0.474, 0.704] | interval excludes 1 |
| Paired *t*-test (currently reported in ch5) | +3.09 pp | [+0.62, +5.55] | .0172 |

- β(vignette) = **−0.5391**, SE 0.1979, z = **−2.724**, p = **.0065** (one-tailed .0032).
- The vignette **reduces trial-level error odds by 41.7%**.
- Both models agree in direction, magnitude and significance, and both agree with the reported
  *t*-test.
- **The pre-registered primary test is more strongly significant than the robustness check that was
  reported in its place** (p = .0065 against .0172).

### Which model, and an honest caveat

Table A.4 says "logistic LMM … + (1 | participant)". Two routes were run:

1. **GEE** — this is the project's own documented substitution. `Tools/analysis/h1_lmm.py`'s
   docstring states: *"logistic GEE … the pragmatic route to 'mixed-model-style' trial-level
   inference chosen over statsmodels BinomialBayesMixedGLM (no p-values/CIs in a frequentist
   frame)"*. GEE is population-averaged, not a true random-intercept model, so its odds ratio is a
   marginal rather than a subject-specific one.
2. **BinomialBayesMixedGLM** — the actual random-intercept specification, fitted by variational
   Bayes. It gives a credible interval, not a *p*-value, which is precisely why the project chose
   GEE.

They agree to the third decimal on the odds ratio, so the conclusion does not depend on the choice.
Whichever is reported, the manuscript should say which was fitted and why, rather than implying a
frequentist GLMM with a *p*-value was run.

**Note:** `Tools/analysis/h1_lmm.py` as written targets the *original* 2×2 design
(`error ~ vignette * distractor`, conditions A1–A4, testing an interaction). The study as actually
run is a two-arm design, so that script does not apply unmodified. The model fitted here is the one
Table A.4 specifies for the design that was run.

## Reproduction

```bash
cd C:/Users/syson/Documents/Code/Unity-PassthroughCameraApiSamples
python .scratch-lmm/fit.py      # scratch dir, deleted after this audit; script body is in this file's git history
```

Environment: Python 3.13.13, statsmodels 0.14.6, numpy 2.4.6, pandas.

## Recommended action

**This is an author decision, not a fix I should apply unilaterally**, because it changes which
number Chapter 5 presents as the primary result.

- **Option A (recommended).** Report the LMM in §5.7.1 as the pre-registered primary, with the
  *t*-test retained as the robustness check it was registered to be. Two or three sentences plus the
  odds ratio. This *strengthens* the headline (p = .0065) and closes the gap between Appendix A.4
  and Chapter 5 completely.
- **Option B.** Keep the *t*-test as the headline and add one sentence in §5.7.1 or Appendix A.6
  disclosing that the primary and robustness analyses were swapped, and why.
- **Option C.** Do nothing. Not recommended: an examiner reading Table A.4 against §5.7.1 finds this
  in ten minutes, and the manuscript's own standard points the other way.

Under A or B the numbers above must first be re-derived by the author or written into the frozen
results file, so that the manuscript's "every statistic traces to the authority" rule still holds.
They are currently in this audit file only.
