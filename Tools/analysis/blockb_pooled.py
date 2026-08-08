#!/usr/bin/env python3
"""Block B pooled — paired Filter vs NoFilter hit rate, plus the eccentricity-band table.

Set construction follows the documented exclusions in HANDOFF.md; nothing is inferred.
Bands join each scored target to `Dissertation/authored/pool_split.csv` on (set, point_id)
— the 82-target file the runs actually load, NOT `raw/pool_split.csv` (an earlier 58-row
draft; joining that one was the 2026-07-28 error recorded at HANDOFF.md:343).

Band eccentricity is the target's MID-LIFETIME position, which HANDOFF.md:382-385 records
as a weak proxy: targets move, median lifetime swing 17.2 deg, and ~31% of catches land in
a different band than the mid position. The press-time reconstruction is the accurate
method and is not yet ported into Tools/analysis.

Usage:
    python Tools/analysis/blockb_pooled.py Dissertation/authored/raw
"""
import csv
import re
import sys
from pathlib import Path

import numpy as np
from scipy import stats

POOL = Path("Dissertation/authored/pool_split.csv")

# Documented Block B exclusions (HANDOFF.md). Reason is printed so the set stays auditable.
EXCLUDE = {
    "3": "539 false alarms on the filter run; hits provably looser (Mann-Whitney p=.0301) "
         "— HANDOFF.md:365. Block A retained.",
    "9": "experimenter decision, same day: participant was confused during the video blocks "
         "— HANDOFF.md:200. Block A retained.",
    "43": "81/54 false alarms, the highest in the set by a wide margin and more than double "
          "the next participant's, on both arms. Same response-validity grounds as P3. "
          "Block A retained.",
}
# P15's own Block B was excluded (206 FA / 246 trials); her canonical Block B pair is P16's.
# Analysis joins P15 (Block A) + P16 (Block B) as one participant — HANDOFF.md:271-275.

BANDS = ["<10", "10-20", "20-30", ">30"]


def band_of(e):
    return "<10" if e < 10 else "10-20" if e < 20 else "20-30" if e < 30 else ">30"


def main(argv):
    raw = Path(argv[0]) if argv else Path("Dissertation/authored/raw")
    pool = {(r["set"], r["point_id"]): float(r["eccentricity_deg"])
            for r in csv.DictReader(POOL.open())}

    arms = {}
    for p in sorted(raw.glob("authored_results_*.csv")):
        m = re.match(r"authored_results_(P\d+)_(FILTER|NOFILTER|BASELINE)-([AB])_", p.name)
        if not m:
            continue
        pid, arm = m.group(1)[1:], m.group(2)
        if arm == "BASELINE":
            continue
        rows = list(csv.DictReader(p.open()))
        scored = [r for r in rows if r["set"] not in ("P", "") and r["outcome"] != "false_alarm"]
        if not scored:
            continue
        fa = sum(r["outcome"] == "false_alarm" for r in rows)
        d = arms.setdefault(pid, {}).setdefault(arm, {"hit": 0, "n": 0, "fa": 0, "band": {}})
        d["fa"] += fa
        for r in scored:
            d["hit"] += r["outcome"] == "hit"
            d["n"] += 1
            k = (r["set"], r["target_id"])
            if k in pool:
                b = band_of(pool[k])
                h, n = d["band"].get(b, (0, 0))
                d["band"][b] = (h + (r["outcome"] == "hit"), n + 1)

    print("== Excluded (documented) ==")
    for pid, why in EXCLUDE.items():
        print(f"  P{pid}: {why}")
    print("  P15: own Block B excluded (206 FA/246); canonical pair is P16's — "
          "P15+P16 join as one participant (HANDOFF.md:271-275).")

    pairs = {p: a for p, a in arms.items()
             if "FILTER" in a and "NOFILTER" in a and p not in EXCLUDE}
    incomplete = [p for p, a in arms.items()
                  if not ("FILTER" in a and "NOFILTER" in a) and p not in EXCLUDE]
    if incomplete:
        print(f"\n  unpaired, not in set: {', '.join('P' + p for p in sorted(incomplete, key=int))}")

    print(f"\n== Paired hit rate ==")
    print(f"  {'pid':>5}{'NoFilter':>16}{'Filter':>16}{'delta':>9}{'FA off/on':>12}")
    rows = []
    for pid in sorted(pairs, key=int):
        f, n = pairs[pid]["FILTER"], pairs[pid]["NOFILTER"]
        fr, nr = f["hit"] / f["n"] * 100, n["hit"] / n["n"] * 100
        rows.append((pid, nr, fr, fr - nr, n["fa"], f["fa"]))
        print(f"  {'P' + pid:>5}{f'{n[chr(104)+chr(105)+chr(116)]}/{n[chr(110)]} {nr:.1f}%':>16}"
              f"{f'{f[chr(104)+chr(105)+chr(116)]}/{f[chr(110)]} {fr:.1f}%':>16}"
              f"{fr - nr:>+9.2f}{f'{n[chr(102)+chr(97)]}/{f[chr(102)+chr(97)]}':>12}")

    d = np.array([r[3] for r in rows])
    fa_arr = np.array([r[2] for r in rows])
    na_arr = np.array([r[1] for r in rows])
    k = len(d)
    print(f"\n  n = {k} pairs")
    print(f"  Filter   {fa_arr.mean():.2f}%  (SD {fa_arr.std(ddof=1):.2f})")
    print(f"  NoFilter {na_arr.mean():.2f}%  (SD {na_arr.std(ddof=1):.2f})")
    print(f"  mean delta {d.mean():+.2f} pp  (SD {d.std(ddof=1):.2f}, median {np.median(d):+.2f})")
    ci = stats.t.ppf(.975, k - 1) * d.std(ddof=1) / np.sqrt(k)
    print(f"  95% CI  {d.mean() - ci:+.2f} .. {d.mean() + ci:+.2f}")
    t, pt = stats.ttest_rel(fa_arr, na_arr)
    print(f"  paired t({k - 1}) = {t:.2f}, p = {pt:.4f}")
    w, pw = stats.wilcoxon(fa_arr, na_arr)
    print(f"  Wilcoxon W = {w:.1f}, p = {pw:.4f}")
    print(f"  dz = {d.mean() / d.std(ddof=1):.3f}   positive: {(d > 0).sum()}/{k}")

    fa_off = np.array([r[4] for r in rows], float)
    fa_on = np.array([r[5] for r in rows], float)
    print(f"\n  false alarms per run: off {fa_off.mean():.1f} vs on {fa_on.mean():.1f}"
          f"  ({(fa_on > fa_off).sum()}/{k} pressed more under filter)")

    # ---- band table ----
    print(f"\n== Eccentricity bands (mid-lifetime proxy) ==")
    print(f"  {'band':<8}{'NoFilter':>18}{'Filter':>18}{'delta':>9}{'per-pid':>26}")
    for b in BANDS:
        nh = sum(pairs[p]["NOFILTER"]["band"].get(b, (0, 0))[0] for p in pairs)
        nn = sum(pairs[p]["NOFILTER"]["band"].get(b, (0, 0))[1] for p in pairs)
        fh = sum(pairs[p]["FILTER"]["band"].get(b, (0, 0))[0] for p in pairs)
        fn = sum(pairs[p]["FILTER"]["band"].get(b, (0, 0))[1] for p in pairs)
        per = []
        for p in pairs:
            a = pairs[p]["NOFILTER"]["band"].get(b, (0, 0))
            c = pairs[p]["FILTER"]["band"].get(b, (0, 0))
            if a[1] and c[1]:
                per.append(c[0] / c[1] * 100 - a[0] / a[1] * 100)
        pa = np.array(per)
        extra = ""
        if len(pa) > 1:
            tt, pp = stats.ttest_1samp(pa, 0)
            try:
                _, pwb = stats.wilcoxon(pa)
            except ValueError:
                pwb = float("nan")
            extra = f"{pa.mean():+.2f} pp (n={len(pa)}) t p={pp:.3f} W p={pwb:.3f}"
        print(f"  {b:<8}{f'{nh}/{nn} {nh/nn*100:.1f}%' if nn else '-':>18}"
              f"{f'{fh}/{fn} {fh/fn*100:.1f}%' if fn else '-':>18}"
              f"{(fh/fn*100 - nh/nn*100) if nn and fn else 0:>+9.2f}{extra:>26}")


if __name__ == "__main__":
    main(sys.argv[1:])
