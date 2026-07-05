#!/usr/bin/env python3
"""
H2 analysis: Block B probe detection under ColorPop.

Per testing-strategy-v2.md section 8.1:
  * H2a (benefit): per-participant MEDIAN correct-hit RT to CENTRAL probes,
    B2 (ColorPop) vs B1 (Off): one-tailed paired t (B2 < B1) + Wilcoxon + dz;
    central hit rate reported descriptively for non-inferiority.
  * H2b (safety, equivalence): per-participant PERIPHERAL hit-rate difference
    (B2 - B1), TOST with margin Δ = 0.10 (10 percentage points). Both
    one-sided tests reported; TOST p = max of the two. The safety-relevant
    lower bound (drop no worse than -Δ) is also reported alone as the
    non-inferiority p. 90 % CI on the difference (consistent with TOST α=.05).
  * False alarms per condition, descriptive.

CLI:
    python Tools/analysis/h2_tost.py parsed/probe_trials.csv [--margin 0.10]
"""

import argparse
import json
import sys
from pathlib import Path

import numpy as np
import pandas as pd
from scipy import stats


def _paired(wide: pd.DataFrame, col_a: str, col_b: str):
    """Return aligned numpy arrays for participants present in both columns."""
    ok = wide[[col_a, col_b]].dropna()
    return ok[col_a].to_numpy(), ok[col_b].to_numpy(), ok.index


def h2a_central_rt(probes: pd.DataFrame) -> dict:
    hits = probes[(probes["band"] == "central") & (probes["outcome"] == "hit")]
    med = hits.groupby(["pid", "condition"])["rt_ms"].median().unstack("condition")
    b1, b2, idx = _paired(med, "B1", "B2")
    diff = b2 - b1                                    # negative = faster under ColorPop
    t = stats.ttest_rel(b2, b1, alternative="less")
    w = stats.wilcoxon(diff, alternative="less")
    dz = float(diff.mean() / diff.std(ddof=1))
    hit_rate = (probes[probes["band"] == "central"]
                .groupby(["pid", "condition"])["outcome"]
                .apply(lambda s: (s == "hit").mean()).unstack("condition"))
    return {
        "n_participants": int(len(idx)),
        "median_rt_ms": {"B1_off": float(np.mean(b1)), "B2_colorpop": float(np.mean(b2))},
        "mean_diff_ms": float(diff.mean()), "dz": dz,
        "t_p_one_sided": float(t.pvalue), "wilcoxon_p": float(w.pvalue),
        "central_hit_rate": {"B1_off": float(hit_rate["B1"].mean()),
                             "B2_colorpop": float(hit_rate["B2"].mean())},
        "supported": bool(t.pvalue < 0.05 and diff.mean() < 0),
    }


def h2b_peripheral_tost(probes: pd.DataFrame, margin: float = 0.10) -> dict:
    peri = probes[probes["band"] == "peripheral"]
    rate = (peri.groupby(["pid", "condition"])["outcome"]
            .apply(lambda s: (s == "hit").mean()).unstack("condition"))
    b1, b2, idx = _paired(rate, "B1", "B2")
    diff = b2 - b1                                    # negative = peripheral awareness cost
    n = len(diff)
    mean, sd = float(diff.mean()), float(diff.std(ddof=1))
    se = sd / np.sqrt(n)
    # TOST: H01 diff <= -margin (test diff > -margin); H02 diff >= +margin (test diff < +margin)
    t_lower = (mean + margin) / se
    t_upper = (mean - margin) / se
    p_lower = float(1 - stats.t.cdf(t_lower, df=n - 1))   # p for diff > -margin
    p_upper = float(stats.t.cdf(t_upper, df=n - 1))       # p for diff < +margin
    p_tost = max(p_lower, p_upper)
    tcrit = stats.t.ppf(0.95, df=n - 1)
    ci90 = [mean - tcrit * se, mean + tcrit * se]
    equivalent = p_tost < 0.05
    harmful = (mean < -margin) and not equivalent
    return {
        "n_participants": int(n), "margin": margin,
        "hit_rate": {"B1_off": float(np.mean(b1)), "B2_colorpop": float(np.mean(b2))},
        "mean_diff": mean, "sd_diff": sd, "ci90_diff": [float(ci90[0]), float(ci90[1])],
        "p_lower_noninferiority": p_lower, "p_upper": p_upper, "p_tost": float(p_tost),
        "equivalent_within_margin": bool(equivalent),
        "conclusive_harm": bool(harmful),
        "verdict": ("H2b PASSES: peripheral hit rate equivalent within ±10 pp"
                    if equivalent else
                    ("H2b FAILS with evidence of harm > 10 pp — conclusive awareness cost (decision-table row 2)"
                     if harmful else
                     "H2b NOT ESTABLISHED: equivalence not shown, harm not shown — report CI as bounded estimate")),
    }


def false_alarm_summary(probes: pd.DataFrame, events: pd.DataFrame = None) -> dict:
    """False alarms live as FALSE_ALARM events, not probe rows; summarised from events if given."""
    if events is None or events.empty:
        return {"note": "pass the events table for false-alarm counts"}
    fa = events[events["event"] == "FALSE_ALARM"]
    per = fa.groupby(["pid", "condition"]).size().unstack("condition").fillna(0)
    return {c: {"mean": float(per[c].mean()), "max": int(per[c].max())}
            for c in per.columns if c.startswith("B")}


def run_h2(probes: pd.DataFrame, events: pd.DataFrame = None, margin: float = 0.10) -> dict:
    return {
        "h2a_central": h2a_central_rt(probes),
        "h2b_peripheral": h2b_peripheral_tost(probes, margin),
        "false_alarms": false_alarm_summary(probes, events),
    }


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("probe_csv", type=Path, help="parsed probe_trials.csv")
    ap.add_argument("--events-csv", type=Path, help="parsed events.csv (for false alarms)")
    ap.add_argument("--margin", type=float, default=0.10)
    args = ap.parse_args()
    probes = pd.read_csv(args.probe_csv)
    events = pd.read_csv(args.events_csv) if args.events_csv else None
    result = run_h2(probes, events, args.margin)
    print(json.dumps(result, indent=2))
    print(f"\nH2a supported: {result['h2a_central']['supported']}")
    print(f"H2b: {result['h2b_peripheral']['verdict']}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
