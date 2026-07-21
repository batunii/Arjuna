#!/usr/bin/env python3
"""Aggregate authored_results_*.csv (the presenter's click-probe files).

Per file: scored targets (practice set=P excluded), hits, misses, false alarms,
hit-rate, mean/median RT on hits. Then a roll-up per (pid, condition, set) so
repeated passes of the same block aggregate together.

Usage:
    python Tools/analysis/authored_report.py Dissertation/authored/raw [more dirs/files...]
    python Tools/analysis/authored_report.py raw/authored_results_P0_*.csv
"""
import csv
import statistics
import sys
from collections import defaultdict
from pathlib import Path


def iter_files(args):
    for a in args:
        p = Path(a)
        if p.is_dir():
            yield from sorted(p.glob("authored_results_*.csv"))
        elif p.exists():
            yield p


def load(path):
    rows = []
    with open(path, newline="", encoding="utf-8") as f:
        for r in csv.DictReader(f):
            rows.append(r)
    return rows


def summarise(rows):
    scored = [r for r in rows if r["set"] != "P" and r["outcome"] != "false_alarm"]
    hits = [r for r in scored if r["outcome"] == "hit"]
    misses = [r for r in scored if r["outcome"] == "miss"]
    fas = [r for r in rows if r["outcome"] == "false_alarm"]
    rts = [float(r["rt_s"]) for r in hits if r["rt_s"]]
    return {
        "targets": len(scored),
        "hits": len(hits),
        "misses": len(misses),
        "fa": len(fas),
        "hit_rate": len(hits) / len(scored) if scored else float("nan"),
        "rt_mean": statistics.mean(rts) if rts else float("nan"),
        "rt_median": statistics.median(rts) if rts else float("nan"),
    }


def fmt(s):
    return (f"{s['hits']:>3}/{s['targets']:<3} hit ({s['hit_rate']:.0%})  "
            f"{s['misses']:>3} miss  {s['fa']:>2} FA  "
            f"RT mean {s['rt_mean']:.2f}s median {s['rt_median']:.2f}s")


def main(args):
    files = list(iter_files(args or ["Dissertation/authored/raw"]))
    if not files:
        print("no authored_results_*.csv found");  return
    groups = defaultdict(list)
    print("== per file ==")
    for f in files:
        rows = load(f)
        if not rows:
            print(f"{f.name}: empty");  continue
        pid = rows[0]["pid"]
        cond = rows[0]["condition"]
        # non-practice target rows define the set of the run
        run_set = next((r["set"] for r in rows if r["set"] not in ("P", "")), "?")
        print(f"{f.name}\n    {fmt(summarise(rows))}")
        groups[(pid, cond, run_set)].extend(rows)
    print("\n== per (pid, condition, set) — passes pooled ==")
    for (pid, cond, run_set), rows in sorted(groups.items()):
        print(f"pid {pid:>4}  {cond:<9} set {run_set}:  {fmt(summarise(rows))}")


if __name__ == "__main__":
    main(sys.argv[1:])
