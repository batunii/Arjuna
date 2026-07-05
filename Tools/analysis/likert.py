#!/usr/bin/env python3
"""
Block C sampler analysis: Friedman + Bonferroni-corrected pairwise Wilcoxon.

Ratings are collected VERBALLY on paper (testing-strategy-v2.md section 4.3),
so this reads a hand-entered long-format CSV, not the session log:

    pid,mode,item,rating
    P01,COLORPOP,comfort,6
    P01,SOFTDARK,comfort,5
    ...

  mode  in {COLORPOP, SOFTDARK, HARDDARK}
  item  in {comfort, focus_benefit, willingness}
  rating 1-7

Per item: Friedman across the three modes (participants with all three modes
only — sampler skips drop the participant for that item), Kendall's W
(chi2 / (n*(k-1))), and pairwise Wilcoxon with Bonferroni x3. Exploratory only
per the pre-registration; sampler completion rate is reported alongside.

CLI:
    python Tools/analysis/likert.py ratings.csv
"""

import argparse
import itertools
import json
import sys
from pathlib import Path

import pandas as pd
from scipy import stats

MODES = ["COLORPOP", "SOFTDARK", "HARDDARK"]
ITEMS = ["comfort", "focus_benefit", "willingness"]


def run_likert(ratings: pd.DataFrame, n_recruited: int = None) -> dict:
    out = {"modes": MODES, "note": "exploratory only (testing-strategy-v2.md section 4.3 caveats)"}
    all_pids = ratings["pid"].nunique()
    out["sampler_completion"] = {
        "participants_with_ratings": int(all_pids),
        **({"of_recruited": n_recruited, "rate": round(all_pids / n_recruited, 3)}
           if n_recruited else {}),
    }
    per_item = {}
    for item in ITEMS:
        sub = ratings[ratings["item"] == item]
        wide = sub.pivot_table(index="pid", columns="mode", values="rating").dropna()
        missing = set(MODES) - set(wide.columns)
        if missing or len(wide) < 5:
            per_item[item] = {"skipped": f"insufficient data (missing {sorted(missing)}, n={len(wide)})"}
            continue
        n, k = len(wide), len(MODES)
        fr = stats.friedmanchisquare(*[wide[m] for m in MODES])
        kendalls_w = float(fr.statistic / (n * (k - 1)))
        pairs = {}
        for a, b in itertools.combinations(MODES, 2):
            diff = wide[a] - wide[b]
            if (diff == 0).all():
                p = 1.0
            else:
                p = float(stats.wilcoxon(wide[a], wide[b]).pvalue)
            pairs[f"{a} vs {b}"] = {
                "median_diff": float(diff.median()),
                "p_raw": p,
                "p_bonferroni": min(1.0, p * 3),
            }
        per_item[item] = {
            "n": n,
            "medians": {m: float(wide[m].median()) for m in MODES},
            "friedman_chi2": float(fr.statistic), "friedman_p": float(fr.pvalue),
            "kendalls_w": kendalls_w,
            "pairwise_wilcoxon": pairs,
        }
    out["items"] = per_item
    return out


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("ratings_csv", type=Path)
    ap.add_argument("--n-recruited", type=int, help="for the completion-rate denominator")
    args = ap.parse_args()
    ratings = pd.read_csv(args.ratings_csv)
    expected = {"pid", "mode", "item", "rating"}
    if set(ratings.columns) != expected:
        print(f"ERROR: columns must be exactly {sorted(expected)}", file=sys.stderr)
        return 1
    print(json.dumps(run_likert(ratings, args.n_recruited), indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main())
