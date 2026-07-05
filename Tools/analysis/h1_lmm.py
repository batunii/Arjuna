#!/usr/bin/env python3
"""
H1 analysis: does the vignette reduce the distractor cost? (primary endpoint)

Per testing-strategy-v2.md section 8.1:
  * PRIMARY (trial level): logistic GEE  error ~ vignette * distractor,
    clustered by participant, exchangeable working correlation. This is the
    pragmatic route to "mixed-model-style" trial-level inference chosen over
    statsmodels BinomialBayesMixedGLM (no p-values/CIs in a frequentist frame)
    — the interaction term is the test. A negative interaction coefficient
    means the distractor effect shrinks when the vignette is on.
  * ROBUSTNESS (participant level): per-participant error rates per condition;
    benefit_i = [er(A2)-er(A1)] - [er(A4)-er(A3)]; one-tailed paired t-test
    (benefit > 0) + Wilcoxon signed-rank; Cohen's dz.
  * SESOI: proportion of the vignette-off distractor cost recovered,
    group-level ratio mean(benefit)/mean(cost_off), with a seeded bootstrap
    90 % CI (10,000 resamples). Default SESOI: >= 0.5 recovery.
  * MANIPULATION CHECK MC1: cost_off > 0, one-tailed paired t + Wilcoxon.

Outputs a dict (importable) and a printed summary (CLI):
    python Tools/analysis/h1_lmm.py parsed/cpt_trials.csv [--sesoi 0.5]
"""

import argparse
import json
import sys
from pathlib import Path

import numpy as np
import pandas as pd
from scipy import stats

BOOT_N = 10_000
BOOT_SEED = 20260705


def participant_rates(cpt: pd.DataFrame) -> pd.DataFrame:
    """Per-participant error rate per semantic condition (A1..A4), wide."""
    cpt = cpt.copy()
    cpt["is_error"] = cpt["is_error"].astype(bool)   # guard against object upcast
    rates = (cpt.groupby(["pid", "condition"])["is_error"].mean().unstack("condition")
             .astype(float))
    missing = {"A1", "A2", "A3", "A4"} - set(rates.columns)
    if missing:
        raise ValueError(f"missing conditions in CPT data: {sorted(missing)}")
    rates["cost_off"] = rates["A2"] - rates["A1"]
    rates["cost_on"] = rates["A4"] - rates["A3"]
    rates["benefit"] = rates["cost_off"] - rates["cost_on"]
    return rates


def gee_interaction(cpt: pd.DataFrame) -> dict:
    import statsmodels.api as sm
    import statsmodels.formula.api as smf
    d = cpt.copy()
    d["error"] = d["is_error"].astype(int)
    d["vig"] = (d["vignette"] == "on").astype(int)
    d["dis"] = (d["distractor"] == "present").astype(int)
    model = smf.gee("error ~ vig * dis", groups="pid", data=d,
                    family=sm.families.Binomial(),
                    cov_struct=sm.cov_struct.Exchangeable())
    fit = model.fit()
    coef = fit.params["vig:dis"]
    se = fit.bse["vig:dis"]
    ci = fit.conf_int().loc["vig:dis"]
    # One-sided p for the directional H1 (interaction < 0: cost shrinks when on)
    z = coef / se
    p_one_sided = float(stats.norm.cdf(z))
    return {
        "coef_log_odds": float(coef), "se": float(se),
        "odds_ratio": float(np.exp(coef)),
        "ci95_or": [float(np.exp(ci[0])), float(np.exp(ci[1]))],
        "p_two_sided": float(fit.pvalues["vig:dis"]),
        "p_one_sided_directional": p_one_sided,
        "n_trials": int(len(d)), "n_participants": int(d["pid"].nunique()),
    }


def bootstrap_recovery_ci(rates: pd.DataFrame, alpha: float = 0.10) -> dict:
    """Seeded bootstrap 90 % CI on mean(benefit)/mean(cost_off)."""
    rng = np.random.default_rng(BOOT_SEED)
    benefit = rates["benefit"].to_numpy()
    cost_off = rates["cost_off"].to_numpy()
    n = len(benefit)
    ratios = []
    for _ in range(BOOT_N):
        idx = rng.integers(0, n, n)
        c = cost_off[idx].mean()
        if abs(c) < 1e-9:
            continue  # degenerate resample: no distractor cost
        ratios.append(benefit[idx].mean() / c)
    ratios = np.array(ratios)
    point = benefit.mean() / cost_off.mean() if abs(cost_off.mean()) > 1e-9 else np.nan
    return {
        "recovery_point": float(point),
        "recovery_ci90": [float(np.quantile(ratios, alpha / 2)),
                          float(np.quantile(ratios, 1 - alpha / 2))],
        "boot_n_effective": int(len(ratios)),
    }


def run_h1(cpt: pd.DataFrame, sesoi: float = 0.5) -> dict:
    rates = participant_rates(cpt)
    benefit = rates["benefit"]
    cost_off = rates["cost_off"]

    t_mc1 = stats.ttest_1samp(cost_off, 0.0, alternative="greater")
    w_mc1 = stats.wilcoxon(cost_off, alternative="greater")
    t_ben = stats.ttest_1samp(benefit, 0.0, alternative="greater")
    w_ben = stats.wilcoxon(benefit, alternative="greater")
    dz = float(benefit.mean() / benefit.std(ddof=1))

    out = {
        "n_participants": int(len(rates)),
        "mc1_cost_off": {"mean": float(cost_off.mean()), "sd": float(cost_off.std(ddof=1)),
                         "t_p_one_sided": float(t_mc1.pvalue), "wilcoxon_p": float(w_mc1.pvalue),
                         "passes": bool(t_mc1.pvalue < 0.05)},
        "gee_primary": gee_interaction(cpt),
        "paired_benefit": {"mean": float(benefit.mean()), "sd": float(benefit.std(ddof=1)),
                           "dz": dz, "t_p_one_sided": float(t_ben.pvalue),
                           "wilcoxon_p": float(w_ben.pvalue)},
        "sesoi": sesoi,
        **bootstrap_recovery_ci(rates),
        "condition_means": rates[["A1", "A2", "A3", "A4"]].mean().round(4).to_dict(),
    }
    lo, hi = out["recovery_ci90"]
    if not out["mc1_cost_off"]["passes"]:
        verdict = "MC1 FAILED — no reliable distractor cost with vignette off; H1 not interpretable (decision-table row 5)"
    elif lo >= sesoi:
        verdict = f"H1 SUPPORTED at SESOI: 90 % CI [{lo:.2f}, {hi:.2f}] entirely >= {sesoi} (row 1/2 pending H2b)"
    elif hi < sesoi and lo > 0:
        verdict = f"Benefit real but below SESOI: 90 % CI [{lo:.2f}, {hi:.2f}] excludes {sesoi} (row 3/4 boundary)"
    elif hi < 0:
        verdict = f"H1 REFUTED: 90 % CI [{lo:.2f}, {hi:.2f}] excludes any benefit (row 3)"
    else:
        verdict = f"Estimate spans 0 and/or SESOI: 90 % CI [{lo:.2f}, {hi:.2f}] (row 4 — bounded conclusion)"
    out["verdict"] = verdict
    return out


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("cpt_csv", type=Path, help="parsed cpt_trials.csv")
    ap.add_argument("--sesoi", type=float, default=0.5,
                    help="minimum proportion of distractor cost recovered (default 0.5)")
    args = ap.parse_args()
    cpt = pd.read_csv(args.cpt_csv)
    result = run_h1(cpt, args.sesoi)
    print(json.dumps(result, indent=2))
    print(f"\nVERDICT: {result['verdict']}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
