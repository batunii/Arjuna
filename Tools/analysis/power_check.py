#!/usr/bin/env python3
"""
Post-pilot SESOI-to-margin conversion and power check (pilot gate support).

Per testing-strategy-v2.md sections 2 and 6: the H1 SESOI is expressed as
"vignette recovers >= 50 % of the vignette-off distractor cost". After the
pilot measures that cost, this tool converts the SESOI into an ABSOLUTE
error-rate margin and reports achievable power at N = 20 for the one-tailed
paired t-test, plus the N needed for 80 % / 90 % power. Feed the numbers into
the post-pilot lock-in memo.

Inputs: the pilot's mean and SD of the per-participant distractor cost
(vignette off), and the expected SD of the benefit difference (defaults to
the cost SD — a conservative same-scale assumption; refine from pilot A3/A4
data if available).

CLI:
    python Tools/analysis/power_check.py --cost-mean 0.06 --cost-sd 0.05
    python Tools/analysis/power_check.py --cost-mean 0.06 --cost-sd 0.05 \\
        --benefit-sd 0.04 --n 20 --recovery 0.5
"""

import argparse
import json
import sys

import numpy as np
from scipy import stats

ALPHA = 0.05


def paired_t_power_one_tailed(effect_mean: float, sd: float, n: int, alpha: float = ALPHA) -> float:
    """Analytical power via the noncentral t distribution."""
    if sd <= 0 or n < 2:
        return float("nan")
    ncp = (effect_mean / sd) * np.sqrt(n)
    tcrit = stats.t.ppf(1 - alpha, df=n - 1)
    return float(1 - stats.nct.cdf(tcrit, df=n - 1, nc=ncp))


def n_for_power(effect_mean: float, sd: float, target: float, alpha: float = ALPHA) -> int:
    for n in range(4, 500):
        if paired_t_power_one_tailed(effect_mean, sd, n, alpha) >= target:
            return n
    return -1


def run_power_check(cost_mean: float, cost_sd: float, benefit_sd: float = None,
                    n: int = 20, recovery: float = 0.5) -> dict:
    benefit_sd = benefit_sd if benefit_sd is not None else cost_sd
    sesoi_abs = recovery * cost_mean
    dz = sesoi_abs / benefit_sd
    return {
        "inputs": {"pilot_cost_mean": cost_mean, "pilot_cost_sd": cost_sd,
                   "benefit_sd_assumed": benefit_sd, "n_planned": n,
                   "sesoi_recovery": recovery, "alpha_one_tailed": ALPHA},
        "sesoi_absolute_error_rate_margin": sesoi_abs,
        "dz_at_sesoi": dz,
        "power_at_n": paired_t_power_one_tailed(sesoi_abs, benefit_sd, n),
        "n_for_80pct": n_for_power(sesoi_abs, benefit_sd, 0.80),
        "n_for_90pct": n_for_power(sesoi_abs, benefit_sd, 0.90),
        "note": ("power for detecting a benefit exactly AT the SESOI; the true effect may be "
                 "larger (the trial-level GEE adds sensitivity beyond this floor)"),
    }


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--cost-mean", type=float, required=True,
                    help="pilot mean per-participant distractor cost (error-rate points, e.g. 0.06)")
    ap.add_argument("--cost-sd", type=float, required=True)
    ap.add_argument("--benefit-sd", type=float, default=None,
                    help="expected SD of the benefit difference (default: --cost-sd)")
    ap.add_argument("--n", type=int, default=20)
    ap.add_argument("--recovery", type=float, default=0.5, help="SESOI recovery proportion")
    args = ap.parse_args()
    result = run_power_check(args.cost_mean, args.cost_sd, args.benefit_sd, args.n, args.recovery)
    print(json.dumps(result, indent=2))
    if result["power_at_n"] < 0.8:
        print(f"\nWARNING: power {result['power_at_n']:.2f} < 0.80 at N={args.n} — "
              "discuss SESOI/stimulus tuning with the supervisor BEFORE data collection.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
