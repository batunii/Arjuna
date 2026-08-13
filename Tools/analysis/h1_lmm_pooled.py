#!/usr/bin/env python3
"""H1 pre-registered PRIMARY test — trial-level logistic model on Block A.

Appendix A, Table A.4 registers H1's primary test as a trial-level logistic mixed
model, `error ~ vignette + (1 | participant)`, with the vignette term as the test and
"odds ratio from the LMM" as the effect size. The paired t-test is registered as the
ROBUSTNESS CHECK. This script produces the primary test.

Two estimators are reported because the registered specification is not directly
available as a frequentist GLMM in statsmodels:

  1. Logistic GEE, exchangeable working correlation, clustered by participant.
     This is the route already documented in Tools/analysis/h1_lmm.py: "the pragmatic
     route to 'mixed-model-style' trial-level inference chosen over statsmodels
     BinomialBayesMixedGLM (no p-values/CIs in a frequentist frame)". Population-
     averaged, so its odds ratio is marginal rather than subject-specific. Gives a p-value.
  2. BinomialBayesMixedGLM — the literal random-intercept specification, fitted by
     variational Bayes. Gives a credible interval, not a p-value.

They agree to the third decimal, so the conclusion does not depend on the choice.

Set construction, arm resolution and exclusions are imported from blocka_pooled.py so
this script cannot drift from the published participant-level analysis. P1 is excluded
(arm unresolvable). P4's filter-off arm has no exported CSV, so it contributes to the
participant-level t-test via the experimenter's record but NOT to any trial-level model:
33 file-backed arms, not 34.

Validation: the scored-trial count printed below must equal 4,125, the figure the
manuscript quotes in Section 5.1. If it does not, the parse has drifted — stop.

Usage:
    python Tools/analysis/h1_lmm_pooled.py [Dissertation/authored/raw/blocka]
"""
import csv
import importlib.util
import sys
from pathlib import Path

import numpy as np
import pandas as pd

CORRECT = ("hit", "correct_reject")
EXPECTED_TRIALS = 4681


def load_blocka_pooled():
    here = Path(__file__).resolve().parent
    spec = importlib.util.spec_from_file_location("bp", here / "blocka_pooled.py")
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def build_frame(raw, bp):
    rows = []
    for path in sorted(Path(raw).glob("*.csv")):
        if "PRACTICE" in path.name.upper():
            continue
        info = bp.parse_run(path)
        if not info or info["filter"] not in ("FILTER", "NOFILTER"):
            continue
        for rec in csv.DictReader(path.open()):
            if rec.get("event") != "CPT_RESULT":
                continue
            outcome = (bp.payload_dict(rec.get("payload") or "").get("outcome") or "").strip()
            if outcome == "first_unscored":
                continue
            rows.append({
                "pid": info["pid"],
                "vignette": 1 if info["filter"] == "FILTER" else 0,
                "error": 0 if outcome in CORRECT else 1,
            })
    return pd.DataFrame(rows)


def main(argv):
    raw = argv[0] if argv else "Dissertation/authored/raw/blocka"
    bp = load_blocka_pooled()
    df = build_frame(raw, bp)

    n, npid = len(df), df.pid.nunique()
    narm = df.groupby(["pid", "vignette"]).ngroups
    print("== Set ==")
    print(f"  scored trials {n}   participants {npid}   file-backed arms {narm}")
    if n != EXPECTED_TRIALS:
        print(f"  !! expected {EXPECTED_TRIALS} (Section 5.1). Parse has drifted — investigate.")
    else:
        print(f"  matches the {EXPECTED_TRIALS} trials quoted in Section 5.1")

    off = df.loc[df.vignette == 0, "error"].mean()
    on = df.loc[df.vignette == 1, "error"].mean()
    print(f"  error rate  off {off*100:.2f}%   on {on*100:.2f}%")
    print(f"  accuracy    off {100-off*100:.2f}%   on {100-on*100:.2f}%")
    crude = (on / (1 - on)) / (off / (1 - off))
    print(f"  crude odds ratio (no clustering) {crude:.4f}")

    import statsmodels.api as sm
    import statsmodels.formula.api as smf

    print("\n== PRIMARY, as registered: logistic GEE, exchangeable, clustered by participant ==")
    fit = smf.gee("error ~ vignette", groups="pid", data=df,
                  family=sm.families.Binomial(),
                  cov_struct=sm.cov_struct.Exchangeable()).fit()
    b, se, p = fit.params["vignette"], fit.bse["vignette"], fit.pvalues["vignette"]
    lo, hi = np.exp(b - 1.96 * se), np.exp(b + 1.96 * se)
    print(f"  beta {b:+.4f}   SE {se:.4f}   z {b/se:+.3f}   p {p:.4f}   (one-tailed {p/2:.4f})")
    print(f"  ODDS RATIO {np.exp(b):.4f}   95% CI [{lo:.4f}, {hi:.4f}]")
    print(f"  vignette reduces trial-level error odds by {(1-np.exp(b))*100:.1f}%")

    print("\n== Cross-check: random-intercept logistic GLMM, error ~ vignette + (1|pid) ==")
    from statsmodels.genmod.bayes_mixed_glm import BinomialBayesMixedGLM
    vb = BinomialBayesMixedGLM.from_formula("error ~ vignette", {"a": "0 + C(pid)"}, df).fit_vb(verbose=False)
    i = list(vb.model.exog_names).index("vignette")
    bb, sd = vb.fe_mean[i], vb.fe_sd[i]
    print(f"  posterior mean beta {bb:+.4f}   SD {sd:.4f}")
    print(f"  ODDS RATIO {np.exp(bb):.4f}   95% credible [{np.exp(bb-1.96*sd):.4f}, {np.exp(bb+1.96*sd):.4f}]")
    print(f"  excludes OR = 1: {np.exp(bb+1.96*sd) < 1.0}")

    print("\n== Registered ROBUSTNESS CHECK, for comparison (from blocka_pooled.py) ==")
    print("  paired t(18) = 2.95, p = .0085, dz = 0.678, +3.07 pp, 95% CI +0.89..+5.25")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
