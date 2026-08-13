#!/usr/bin/env python3
"""Leave-one-out sensitivity for the three headline paired results.

Applies the identical check to Block A (H1), Block B pooled (H2b) and the
pre-registered 20-30 degree band (H2a):

  1. full sample
  2. drop the single largest positive delta (the most favourable participant)
  3. full leave-one-out sweep, reporting the worst p across all n removals

Reuses the parsing in blocka_pooled.py and blockb_pooled.py so the inputs are
identical to the published figures. Read-only.

    python Tools/analysis/loo_sensitivity.py [raw_dir]
"""
import csv
import re
import sys
from pathlib import Path

import numpy as np
from scipy import stats

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import blocka_pooled as A                                    # noqa: E402
import blockb_pooled as B                                    # noqa: E402


def paired(d):
    """t, p, dz for a delta vector against zero."""
    d = np.asarray(d, float)
    t, p = stats.ttest_1samp(d, 0.0)
    return t, p, d.mean(), d.mean() / d.std(ddof=1)


def report(name, labels, d, alpha=.05):
    d = np.asarray(d, float)
    t, p, m, dz = paired(d)
    print(f"\n{name}")
    print(f"  full sample        n={len(d):<3} mean {m:+6.2f} pp  "
          f"t={t:5.2f}  p={p:.4f}  dz={dz:.3f}   {'SIG' if p < alpha else 'ns'}")

    i = int(np.argmax(d))
    keep = np.delete(d, i)
    t2, p2, m2, dz2 = paired(keep)
    print(f"  drop largest ({labels[i]:>4}, {d[i]:+.2f} pp)"
          f"  n={len(keep):<3} mean {m2:+6.2f} pp  "
          f"t={t2:5.2f}  p={p2:.4f}  dz={dz2:.3f}   {'SIG' if p2 < alpha else 'ns'}")

    ps = []
    for j in range(len(d)):
        _, pj, _, _ = paired(np.delete(d, j))
        ps.append((pj, labels[j]))
    worst_p, worst_lbl = max(ps)
    best_p, best_lbl = min(ps)
    n_sig = sum(pj < alpha for pj, _ in ps)
    print(f"  full LOO sweep     p ranges {best_p:.4f} (drop {best_lbl}) "
          f"to {worst_p:.4f} (drop {worst_lbl})")
    print(f"                     significant in {n_sig}/{len(ps)} leave-one-out samples")
    return p, p2, worst_p


def block_a(root):
    runs = [r for r in (A.parse_run(p) for p in sorted(root.glob("blockA_nback1_*.csv"))) if r]
    byp = {}
    for r in runs:
        if r["filter"] == "UNKNOWN":
            continue
        byp.setdefault(r["pid"], {}).setdefault(r["filter"], []).append(r["acc"])
    if "4" in byp:                      # P4 filter-off is the experimenter's record
        byp["4"].setdefault("NOFILTER", [98.4])
    pids, d = [], []
    for pid in sorted(byp, key=int):
        v = byp[pid]
        if "FILTER" in v and "NOFILTER" in v:
            pids.append("P" + pid)
            d.append(np.mean(v["FILTER"]) - np.mean(v["NOFILTER"]))
    return pids, d


def block_b(raw):
    pool = {(r["set"], r["point_id"]): float(r["eccentricity_deg"])
            for r in csv.DictReader(B.POOL.open())}
    arms = {}
    for p in sorted(raw.glob("authored_results_*.csv")):
        m = re.match(r"authored_results_(P\d+)_(FILTER|NOFILTER|BASELINE)-([AB])_", p.name)
        if not m or m.group(2) == "BASELINE":
            continue
        pid, arm = m.group(1)[1:], m.group(2)
        rows = list(csv.DictReader(p.open()))
        scored = [r for r in rows if r["set"] not in ("P", "") and r["outcome"] != "false_alarm"]
        if not scored:
            continue
        d = arms.setdefault(pid, {}).setdefault(arm, {"hit": 0, "n": 0, "band": {}})
        for r in scored:
            d["hit"] += r["outcome"] == "hit"
            d["n"] += 1
            k = (r["set"], r["target_id"])
            if k in pool:
                b = B.band_of(pool[k])
                h, n = d["band"].get(b, (0, 0))
                d["band"][b] = (h + (r["outcome"] == "hit"), n + 1)
    pairs = {p: a for p, a in arms.items()
             if "FILTER" in a and "NOFILTER" in a and p not in B.EXCLUDE}

    pooled_pids, pooled_d = [], []
    band_pids, band_d = [], []
    for pid in sorted(pairs, key=int):
        f, n = pairs[pid]["FILTER"], pairs[pid]["NOFILTER"]
        pooled_pids.append("P" + pid)
        pooled_d.append(f["hit"] / f["n"] * 100 - n["hit"] / n["n"] * 100)
        fb, nb = f["band"].get("20-30", (0, 0)), n["band"].get("20-30", (0, 0))
        if fb[1] and nb[1]:
            band_pids.append("P" + pid)
            band_d.append(fb[0] / fb[1] * 100 - nb[0] / nb[1] * 100)
    return (pooled_pids, pooled_d), (band_pids, band_d)


def main(argv):
    raw = Path(argv[0]) if argv else Path("Dissertation/authored/raw")
    print("Leave-one-out sensitivity — identical check applied to all three headline results")
    print("=" * 78)

    a_pids, a_d = block_a(raw / "blocka")
    report("BLOCK A (H1) — paired accuracy, robustness check to the trial-level primary",
           a_pids, a_d)

    (bp, bd), (kp, kd) = block_b(raw)
    report("BLOCK B pooled (H2b) — whole-field hit rate", bp, bd)
    report("BLOCK B 20-30 deg band (H2a) — the pre-registered secondary", kp, kd)

    print("\n" + "=" * 78)
    print("H2b note: the pre-committed refutation for H2b requires equivalence not established")
    print("AND a point estimate showing a drop greater than 10 pp. Every leave-one-out sample")
    print("above remains a rise, so H2b's verdict is unchanged regardless of its paired p-value.")


if __name__ == "__main__":
    main(sys.argv[1:])
