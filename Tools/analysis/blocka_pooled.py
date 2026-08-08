#!/usr/bin/env python3
"""Block A pooled ACROSS task versions (user directive 2026-08-06).

`blocka_timecourse.py` never pools versions (supervisor directive 2026-07-23). This tool
does, and prints the per-version evidence first so the pooling assumption stays visible:
if the version means differ a lot, the pooled number is a mixture, not an effect.

Filter state comes from the run-start payload / dr_intensity where stamped. Pre-2026-07-23
runs are UNKNOWN and are resolved from the documented arm assignments in
`Dissertation/authored/analysis-2026-07-22.md` §6 and `HANDOFF.md` (see ARM_OVERRIDES).
Anything still unresolved is reported and dropped, never guessed.

Usage:
    python Tools/analysis/blocka_pooled.py Dissertation/authored/raw/blocka
"""
import csv
import re
import sys
from collections import defaultdict
from pathlib import Path

import numpy as np
from scipy import stats

CORRECT = ("hit", "correct_reject")
MIN_SCORED = 30

# pid -> {slot: arm}. Sources, per HANDOFF.md and analysis-2026-07-22.md §6:
#   P2  ledger-window verified   P3  clean timing   P33 experimenter re-classification
# P1 is deliberately absent: both CPT rounds ran ~15 min after the Unity blocks closed,
# so its filter state is unverifiable. P4 exported only A2 (A1's 98.4% is experimenter-
# reported with no file) so it cannot form a file-backed pair.
ARM_OVERRIDES = {
    "2":  {"A3": "FILTER", "A4": "NOFILTER"},
    "3":  {"A1": "NOFILTER", "A2": "FILTER"},
    "33": {"A3": "FILTER", "A4": "NOFILTER"},
    "4":  {"A2": "FILTER"},
}

# Arms with no exported CSV, carried from the experimenter's contemporaneous record.
# P4's A1 (NoFilter) = 98.4%, per analysis-2026-07-22.md §6 and HANDOFF.md:318. The
# provenance is printed so the source of the value stays visible in the output.
REPORTED_ARMS = {
    ("4", "NOFILTER"): (98.4, "experimenter record (HANDOFF.md:318)"),
}


def payload_dict(s):
    return dict(kv.split("=", 1) for kv in s.split(";") if "=" in kv)


def parse_run(path):
    trials = soa = None
    task = filt = condition = pid = None
    outcomes = []
    with path.open(encoding="utf-8-sig", newline="") as f:
        for row in csv.DictReader(f):
            ev = row.get("event", "")
            if ev in ("CPT_RUN_START", "PRACTICE_START"):
                d = payload_dict(row.get("payload", ""))
                trials = int(d.get("trials", 0)) or None
                soa = float(d.get("soa_s", 0)) or None
                task = d.get("task", "") or task
                filt = d.get("filter") or filt
                condition = row.get("condition")
                pid = row.get("pid")
            elif ev == "CPT_RESULT":
                o = payload_dict(row.get("payload", "")).get("outcome", "")
                if o and o != "first_unscored":
                    outcomes.append(o)
            if filt in (None, "", "NA") and row.get("dr_intensity") in ("0", "1"):
                filt = "FILTER" if row["dr_intensity"] == "1" else "NOFILTER"

    if condition in (None, "PRACTICE") or len(outcomes) < MIN_SCORED:
        return None
    if soa is None:
        soa = 2.5 if (trials or 84) == 84 else 2.0
    if filt not in ("FILTER", "NOFILTER"):
        filt = ARM_OVERRIDES.get(pid, {}).get(condition, "UNKNOWN")
    return {
        "file": path.name, "pid": pid, "condition": condition,
        "version": f"{trials or '?'}x{soa:g}s" + ("+lures" if "lures" in (task or "") else ""),
        "filter": filt,
        "n": len(outcomes),
        "correct": sum(o in CORRECT for o in outcomes),
        "acc": sum(o in CORRECT for o in outcomes) / len(outcomes) * 100,
    }


def main(argv):
    root = Path(argv[0]) if argv else Path("Dissertation/authored/raw/blocka")
    runs = [r for r in (parse_run(p) for p in sorted(root.glob("blockA_nback1_*.csv"))) if r]

    print(f"{len(runs)} scored run(s)\n")

    print("== Premise check: accuracy by version ==")
    byver = defaultdict(list)
    for r in runs:
        byver[r["version"]].append(r)
    print(f"  {'version':<16}{'n':>4}{'mean acc':>10}{'sd':>8}{'min':>7}{'max':>7}")
    for v in sorted(byver):
        a = [x["acc"] for x in byver[v]]
        print(f"  {v:<16}{len(a):>4}{np.mean(a):>9.1f}%{(np.std(a, ddof=1) if len(a) > 1 else 0):>8.1f}"
              f"{min(a):>7.1f}{max(a):>7.1f}")

    known = [r for r in runs if r["filter"] != "UNKNOWN"]
    unknown = [r for r in runs if r["filter"] == "UNKNOWN"]
    if len(byver) > 1:
        groups = [[x["acc"] for x in byver[v]] for v in sorted(byver) if len(byver[v]) > 1]
        if len(groups) > 1:
            H, p = stats.kruskal(*groups)
            print(f"\n  Kruskal-Wallis across versions (n>1 groups): H={H:.2f} p={p:.4f}"
                  f"  -> {'no detectable difference' if p >= .05 else 'VERSIONS DIFFER'}")

    if unknown:
        print(f"\n  dropped, filter state unresolved ({len(unknown)}):")
        for r in unknown:
            print(f"    {r['file']}  (pid {r['pid']}, {r['condition']})")

    # ---- pooled paired analysis ----
    pairs = {}
    for r in known:
        pairs.setdefault(r["pid"], {}).setdefault(r["filter"], []).append(r)

    # Fill arms that have no file from the experimenter's record (see REPORTED_ARMS).
    for (pid, arm), (acc, src) in REPORTED_ARMS.items():
        if pid in pairs and arm not in pairs[pid]:
            ver = next(iter(pairs[pid].values()))[0]["version"]
            pairs[pid][arm] = [{"file": f"<reported: {src}>", "pid": pid, "condition": "?",
                                "version": ver, "filter": arm, "n": 0, "correct": 0, "acc": acc}]
            print(f"\n  NOTE  P{pid} {arm} = {acc}% source: {src}")

    complete = {p: v for p, v in pairs.items() if "FILTER" in v and "NOFILTER" in v}
    incomplete = {p: v for p, v in pairs.items() if p not in complete}

    print(f"\n== Pooled across ALL versions — paired Filter vs NoFilter ==")
    if incomplete:
        print(f"  unpaired, excluded ({len(incomplete)}): "
              + ", ".join(f"P{p}({'/'.join(v)})" for p, v in sorted(incomplete.items(), key=lambda x: int(x[0]))))

    rows = []
    for pid, arms in sorted(complete.items(), key=lambda x: int(x[0])):
        f = np.mean([x["acc"] for x in arms["FILTER"]])
        n = np.mean([x["acc"] for x in arms["NOFILTER"]])
        rows.append((pid, arms["FILTER"][0]["version"], n, f, f - n))
    print(f"\n  {'pid':>5}  {'version':<16}{'NoFilter':>10}{'Filter':>9}{'delta':>9}")
    for pid, ver, n, f, d in rows:
        print(f"  {('P' + pid):>5}  {ver:<16}{n:>9.1f}%{f:>8.1f}%{d:>+9.2f}")

    d = np.array([r[4] for r in rows])
    f_all = np.array([r[3] for r in rows])
    n_all = np.array([r[2] for r in rows])
    k = len(d)
    print(f"\n  n = {k} pairs")
    print(f"  Filter   {f_all.mean():.2f}%  (SD {f_all.std(ddof=1):.2f})")
    print(f"  NoFilter {n_all.mean():.2f}%  (SD {n_all.std(ddof=1):.2f})")
    print(f"  mean delta {d.mean():+.2f} pp  (SD {d.std(ddof=1):.2f}, median {np.median(d):+.2f})")
    se = d.std(ddof=1) / np.sqrt(k)
    ci = stats.t.ppf(.975, k - 1) * se
    print(f"  95% CI  {d.mean() - ci:+.2f} .. {d.mean() + ci:+.2f}")
    t, pt = stats.ttest_rel(f_all, n_all)
    print(f"  paired t({k - 1}) = {t:.2f}, p = {pt:.4f}")
    try:
        w, pw = stats.wilcoxon(f_all, n_all)
        print(f"  Wilcoxon W = {w:.1f}, p = {pw:.4f}")
    except ValueError as e:
        print(f"  Wilcoxon: {e}")
    print(f"  dz = {d.mean() / d.std(ddof=1):.3f}   positive: {(d > 0).sum()}/{k}")


if __name__ == "__main__":
    main(sys.argv[1:])
