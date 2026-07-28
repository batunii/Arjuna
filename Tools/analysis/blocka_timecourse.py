#!/usr/bin/env python3
"""
Block A time-course / consistency report: does performance DRIFT differently with the
headset filter on vs off?

Per run (one blockA_nback1_*.csv), from the CPT_RESULT rows:
  - accuracy by thirds (start/mid/end) and the late-run decline (end - start)
  - median correct-response RT by thirds
  - RT consistency by thirds (CV of correct RTs; lower = steadier)
  - RT drift: OLS slope of correct RT on trial index, scaled to ms per MINUTE of task
    time (slope/trial x trials/min) so different-SOA versions stay comparable
Then groups runs by (version, filter state) and prints group means, plus a FILTER vs
NOFILTER contrast per version. Versions (trials x SOA x task) are NEVER pooled (supervisor
directive 2026-07-23) - v1 = 84x2.5s, v2 = 105x2.0s, v3 = 140x1.8s, v4 = 140x1.8s+lures
(2026-07-27: same timing as v3, so the run-start payload's task=nback1_lures is the
distinguisher - the "+lures" suffix in the version key keeps them apart).

Filter state comes from the run-start payload (filter=FILTER|NOFILTER, present from
2026-07-23) or the dr_intensity column (1/0); older files without either are UNKNOWN -
assign those manually against the session ledger's Block A windows if needed.

Usage:
    python Tools/analysis/blocka_timecourse.py                # default raw/blocka dir
    python Tools/analysis/blocka_timecourse.py path/to/dir_or_files...
"""

import csv
import re
import statistics
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent.parent
DEFAULT_DIR = REPO_ROOT / "Dissertation/authored/raw/blocka"

MIN_SCORED_TRIALS = 30  # runs shorter than this (early aborts) are skipped
CORRECT_OUTCOMES = ("hit", "correct_reject")


def payload_dict(payload: str) -> dict:
    out = {}
    for part in payload.split(";"):
        if "=" in part:
            k, v = part.split("=", 1)
            out[k] = v
    return out


def parse_run(path: Path):
    """Returns a run dict or None if the file isn't a usable main run."""
    trials = soa = None
    task = ""
    filt = None
    condition = None
    pid = None
    records = []  # (idx, correct, rt_s or None)
    aborted = False
    with path.open(encoding="utf-8-sig", newline="") as f:
        for row in csv.DictReader(f):
            ev = row.get("event", "")
            pl = row.get("payload", "")
            if ev in ("CPT_RUN_START", "PRACTICE_START"):
                d = payload_dict(pl)
                trials = int(d.get("trials", 0)) or None
                soa = float(d.get("soa_s", 0)) or None
                task = d.get("task", "") or task
                filt = d.get("filter") or filt
                condition = row.get("condition")
                pid = row.get("pid")
            elif ev == "CPT_RUN_ABORTED":
                aborted = True
            elif ev == "CPT_RESULT":
                d = payload_dict(pl)
                outcome = d.get("outcome", "")
                if outcome == "first_unscored" or not outcome:
                    continue
                m = re.search(r"rt_ms=(\d+)", pl)
                records.append((
                    int(d.get("idx", len(records) + 1)),
                    outcome.startswith(CORRECT_OUTCOMES),
                    int(m.group(1)) / 1000.0 if m else None,
                ))
            # dr_intensity column (v2+, 2026-07-23 tie-in) as filter fallback
            if filt in (None, "", "NA") and row.get("dr_intensity") in ("0", "1"):
                filt = "FILTER" if row["dr_intensity"] == "1" else "NOFILTER"

    if condition in (None, "PRACTICE"):
        return None
    if len(records) < MIN_SCORED_TRIALS:
        return None
    if soa is None:
        soa = 2.5 if (trials or 84) == 84 else 2.0  # pre-soa_s files were v1
    return {
        "file": path.name, "pid": pid, "condition": condition, "aborted": aborted,
        "version": f"{trials or '?'}x{soa:g}s" + ("+lures" if "lures" in task else ""),
        "filter": filt if filt in ("FILTER", "NOFILTER") else "UNKNOWN",
        "soa": soa, "records": sorted(records),
    }


def run_metrics(run: dict) -> dict:
    recs = run["records"]
    n = len(recs)
    third = (n + 2) // 3
    bins = [recs[:third], recs[third:2 * third], recs[2 * third:]]

    def acc(b):
        return 100.0 * sum(1 for _, c, _ in b if c) / len(b) if b else None

    def correct_rts(b):
        return [rt for _, c, rt in b if c and rt is not None]

    def med(b):
        r = correct_rts(b)
        return statistics.median(r) * 1000 if r else None

    def cv(b):
        r = correct_rts(b)
        if len(r) < 4:
            return None
        return 100.0 * statistics.stdev(r) / statistics.mean(r)

    pts = [(i, rt) for i, c, rt in recs if c and rt is not None]
    slope_ms_min = None
    if len(pts) >= 8:
        mx = sum(i for i, _ in pts) / len(pts)
        my = sum(r for _, r in pts) / len(pts)
        num = sum((i - mx) * (r - my) for i, r in pts)
        den = sum((i - mx) ** 2 for i, _ in pts)
        if den:
            slope_ms_min = (num / den) * 1000 * (60 / run["soa"])

    accs = [acc(b) for b in bins]
    return {
        "n": n,
        "acc": acc(recs),
        "acc_thirds": accs,
        "acc_decline": (accs[2] - accs[0]) if None not in (accs[0], accs[2]) else None,
        "rt_thirds": [med(b) for b in bins],
        "cv": cv(recs),
        "cv_thirds": [cv(b) for b in bins],
        "cv_change": None if None in (cv(bins[0]), cv(bins[2])) else cv(bins[2]) - cv(bins[0]),
        "slope": slope_ms_min,
    }


def fmt(v, unit="", nd=1):
    return "-" if v is None else f"{v:+.{nd}f}{unit}" if unit == " ms/min" else f"{v:.{nd}f}{unit}"


def fmt_thirds(vals, nd=0):
    return " / ".join("-" if v is None else f"{v:.{nd}f}" for v in vals)


def group_mean(vals):
    vals = [v for v in vals if v is not None]
    if not vals:
        return None, None
    return (statistics.mean(vals), statistics.stdev(vals) if len(vals) > 1 else 0.0)


def main() -> int:
    tokens = sys.argv[1:] or [str(DEFAULT_DIR)]
    files = []
    for t in tokens:
        p = Path(t)
        if p.is_dir():
            files += sorted(p.glob("blockA_nback1_*.csv"))
        elif p.is_file():
            files.append(p)
    if not files:
        print(f"No blockA_nback1_*.csv found in: {tokens}", file=sys.stderr)
        return 1

    runs, seen = [], set()
    for f in files:
        r = parse_run(f)
        if r is None:
            continue
        # re-exports keep the run's original stamp -> same pid/condition/first-record key
        key = (r["pid"], r["condition"], r["version"], r["records"][0])
        if key in seen:
            continue
        seen.add(key)
        r["metrics"] = run_metrics(r)
        runs.append(r)

    if not runs:
        print("No usable main runs (>= 30 scored trials).", file=sys.stderr)
        return 1

    print(f"{len(runs)} run(s)\n")
    print("== Per run ==")
    hdr = f"{'file':<58} {'filt':<9} {'acc%':>5} {'acc 1/2/3':>14} {'RTmed 1/2/3 ms':>18} {'CV 1/2/3 %':>14} {'drift':>12}"
    print(hdr)
    for r in runs:
        m = r["metrics"]
        drift = "-" if m["slope"] is None else f"{m['slope']:+.0f} ms/min"
        print(f"{r['file']:<58} {r['filter']:<9} {fmt(m['acc'], nd=0):>5} "
              f"{fmt_thirds(m['acc_thirds']):>14} {fmt_thirds(m['rt_thirds']):>18} "
              f"{fmt_thirds(m['cv_thirds']):>14} {drift:>12}"
              + ("  [aborted]" if r["aborted"] else ""))

    versions = sorted({r["version"] for r in runs})
    for ver in versions:
        vruns = [r for r in runs if r["version"] == ver]
        print(f"\n== Version {ver} - group means (mean +/- sd across runs) ==")
        by_filter = {}
        for g in ("FILTER", "NOFILTER", "UNKNOWN"):
            grp = [r["metrics"] for r in vruns if r["filter"] == g]
            if not grp:
                continue
            by_filter[g] = grp
            for label, key, unit in [
                ("overall accuracy", "acc", "%"),
                ("late-run accuracy change (end-start)", "acc_decline", " pp"),
                ("RT drift", "slope", " ms/min"),
                ("RT variability (CV)", "cv", "%"),
                ("CV change (end-start)", "cv_change", " pp"),
            ]:
                mean, sd = group_mean([m[key] for m in grp])
                val = "-" if mean is None else f"{mean:+.1f}{unit}" if "change" in label or "drift" in label else f"{mean:.1f}{unit}"
                sdtxt = "" if mean is None or len(grp) < 2 else f" +/- {sd:.1f}"
                print(f"  {g:<9} (n={len(grp)})  {label:<38} {val}{sdtxt}")
            print()

        if "FILTER" in by_filter and "NOFILTER" in by_filter:
            print(f"  -- FILTER vs NOFILTER contrast ({ver}) --")
            for label, key in [
                ("RT drift (positive = slowing faster)", "slope"),
                ("late-run accuracy change (negative = fading more)", "acc_decline"),
                ("RT variability CV (higher = less consistent)", "cv"),
                ("CV change over run (positive = losing consistency)", "cv_change"),
            ]:
                fm, _ = group_mean([m[key] for m in by_filter["FILTER"]])
                nm, _ = group_mean([m[key] for m in by_filter["NOFILTER"]])
                if fm is None or nm is None:
                    continue
                print(f"    {label:<52} FILTER {fm:+.1f}  vs  NOFILTER {nm:+.1f}  (diff {fm - nm:+.1f})")

    unknown = sum(1 for r in runs if r["filter"] == "UNKNOWN")
    if unknown:
        print(f"\nNOTE: {unknown} run(s) have UNKNOWN filter state (pre-2026-07-23 files) - "
              "match them to arms via the session ledger's Block A time windows before "
              "leaning on the contrast.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
