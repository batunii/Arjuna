#!/usr/bin/env python3
"""
Block B (video / authored-target) time-course report: does performance fade in the later
part of the clip, and does it fade MORE with the filter on?

Per run (one authored_results_*.csv), scored targets only (set A/B, outcome hit|miss):
  - hit rate in the early vs late half of the clip (split at the run's median target onset)
  - median hit RT per half, and RT consistency (CV of hit RTs) per half
  - RT drift: OLS slope of hit RT on target onset time, in ms per minute of clip
  - false alarms per half (false_alarm rows carry only the wall-clock t_ms; halves are
    split at the midpoint of the run's wall-clock span)
Then groups by (set, condition) and, where a set has both Filter and NoFilter runs, prints
the DIFFERENCE-IN-DIFFERENCES: (late - early) under Filter minus (late - early) under
NoFilter. That contrast is the fair fatigue comparison - within a set, both arms see the
IDENTICAL targets in the identical order, so target-difficulty differences between clip
halves cancel out. A raw late-half drop on its own does NOT prove fatigue (late targets
are different targets); the cross-arm contrast is the number to read.

Caveats: probe style / window size / dim level changed across pilot days (see HANDOFF.md),
so runs from different days are different instruments - the per-run table prints the date
stamp; only compare like with like. Practice (set P) and set ALL rows are excluded from
scoring. Pids >= 900 are experimenter pilots.

Usage:
    python Tools/analysis/blockb_timecourse.py                 # default Dissertation/authored/raw
    python Tools/analysis/blockb_timecourse.py path/to/dir_or_files...
"""

import csv
import re
import statistics
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent.parent
DEFAULT_DIR = REPO_ROOT / "Dissertation/authored/raw"

MIN_SCORED_TARGETS = 10


def parse_run(path: Path):
    try:
        with path.open(encoding="utf-8-sig", newline="") as f:
            rows = list(csv.DictReader(f))
    except OSError as e:
        print(f"WARNING: cannot read {path.name}: {e}", file=sys.stderr)
        return None
    if not rows or "outcome" not in rows[0]:
        print(f"WARNING: skipping {path.name} (no outcome column)", file=sys.stderr)
        return None

    targets, fas, tms_all = [], [], []
    pid = condition = tset = None
    for r in rows:
        pid = r.get("pid") or pid
        cond = (r.get("condition") or "").upper()
        condition = cond or condition
        s = r.get("set", "")
        try:
            tms_all.append(int(r["t_ms"]))
        except (KeyError, ValueError):
            pass
        if r["outcome"] == "false_alarm":
            fas.append(int(r["t_ms"]))
            continue
        if s in ("P", "ALL") or r["outcome"] not in ("hit", "miss"):
            continue
        tset = s or tset
        try:
            t_start = float(r["t_start"])
        except (TypeError, ValueError):
            continue
        rt = None
        if r["outcome"] == "hit":
            try:
                rt = float(r["rt_s"])
            except (TypeError, ValueError):
                pass
        targets.append((t_start, r["outcome"] == "hit", rt))

    if len(targets) < MIN_SCORED_TARGETS:
        return None
    targets.sort()
    m = re.search(r"_(\d{8})_(\d{6})\.csv$", path.name)
    return {
        "file": path.name, "pid": pid, "condition": condition, "set": tset,
        "date": f"{m.group(1)}" if m else "?", "targets": targets,
        "fas": sorted(fas), "tspan": (min(tms_all), max(tms_all)) if tms_all else None,
    }


def half_metrics(targets):
    onsets = [t for t, _, _ in targets]
    mid = statistics.median(onsets)
    early = [x for x in targets if x[0] <= mid]
    late = [x for x in targets if x[0] > mid]
    # median splits can be lopsided when onsets tie; rebalance by count if needed
    if not late:
        h = len(targets) // 2
        early, late = targets[:h], targets[h:]

    def hit_rate(b):
        return 100.0 * sum(1 for _, h, _ in b if h) / len(b) if b else None

    def rts(b):
        return [rt for _, h, rt in b if h and rt is not None]

    def med_rt(b):
        r = rts(b)
        return statistics.median(r) * 1000 if r else None

    def cv(b):
        r = rts(b)
        if len(r) < 4:
            return None
        return 100.0 * statistics.stdev(r) / statistics.mean(r)

    pts = [(t, rt) for t, h, rt in targets if h and rt is not None]
    slope = None
    if len(pts) >= 6:
        mx = sum(t for t, _ in pts) / len(pts)
        my = sum(r for _, r in pts) / len(pts)
        num = sum((t - mx) * (r - my) for t, r in pts)
        den = sum((t - mx) ** 2 for t, _ in pts)
        if den:
            slope = (num / den) * 1000 * 60  # s RT per s clip -> ms per minute

    return {
        "n": len(targets), "n_early": len(early), "n_late": len(late),
        "hit_early": hit_rate(early), "hit_late": hit_rate(late),
        "hit_delta": (hit_rate(late) - hit_rate(early))
                     if None not in (hit_rate(early), hit_rate(late)) else None,
        "rt_early": med_rt(early), "rt_late": med_rt(late),
        "cv_early": cv(early), "cv_late": cv(late),
        "cv_delta": (cv(late) - cv(early)) if None not in (cv(early), cv(late)) else None,
        "slope": slope,
    }


def fa_split(run):
    if not run["fas"] or not run["tspan"]:
        return (len(run["fas"]), 0)
    lo, hi = run["tspan"]
    mid = (lo + hi) / 2
    early = sum(1 for t in run["fas"] if t <= mid)
    return (early, len(run["fas"]) - early)


def group_mean(vals):
    vals = [v for v in vals if v is not None]
    if not vals:
        return None
    return statistics.mean(vals)


def f(v, nd=0, sign=False):
    if v is None:
        return "-"
    return f"{v:+.{nd}f}" if sign else f"{v:.{nd}f}"


def main() -> int:
    tokens = sys.argv[1:] or [str(DEFAULT_DIR)]
    files = []
    for t in tokens:
        p = Path(t)
        if p.is_dir():
            files += sorted(p.glob("authored_results_*.csv"))  # top level only, skips duplicates/
        elif p.is_file():
            files.append(p)
    runs, seen = [], set()
    for r in (parse_run(p) for p in files):
        if not r:
            continue
        # duplicate-presenter runs (2026-07-22 bug) wrote twin CSVs ~1 ms apart with the
        # same pid/condition/set and identical per-target outcomes -- keep the first
        key = (r["pid"], r["condition"], r["set"], tuple((t, h) for t, h, _ in r["targets"]))
        if key in seen:
            print(f"NOTE: skipping duplicate run {r['file']}", file=sys.stderr)
            continue
        seen.add(key)
        runs.append(r)
    if not runs:
        print("No usable runs (>= 10 scored set-A/B targets).", file=sys.stderr)
        return 1
    for r in runs:
        r["m"] = half_metrics(r["targets"])
        r["fa"] = fa_split(r)

    print(f"{len(runs)} run(s). Split = early/late half of the clip (median target onset).\n")
    print("== Per run ==")
    print(f"{'file':<58} {'cond':<9} {'set':<3} {'n':>3} {'hit% e/l':>10} "
          f"{'RTmed e/l ms':>14} {'CV e/l %':>10} {'drift':>11} {'FA e/l':>7}")
    for r in runs:
        m = r["m"]
        print(f"{r['file']:<58} {r['condition']:<9} {r['set'] or '?':<3} {m['n']:>3} "
              f"{f(m['hit_early'])+'/'+f(m['hit_late']):>10} "
              f"{f(m['rt_early'])+'/'+f(m['rt_late']):>14} "
              f"{f(m['cv_early'])+'/'+f(m['cv_late']):>10} "
              f"{(f(m['slope'], 0, True)+' ms/m') if m['slope'] is not None else '-':>11} "
              f"{r['fa'][0]:>3}/{r['fa'][1]}")

    print("\n== Group means by (set, condition) ==")
    keys = sorted({(r["set"], r["condition"]) for r in runs})
    groups = {}
    for k in keys:
        grp = [r["m"] for r in runs if (r["set"], r["condition"]) == k]
        groups[k] = grp
        print(f"  set {k[0]} {k[1]:<9} (n={len(grp)}):  "
              f"hit late-early {f(group_mean([m['hit_delta'] for m in grp]), 1, True)} pp   "
              f"RT drift {f(group_mean([m['slope'] for m in grp]), 0, True)} ms/min   "
              f"CV late-early {f(group_mean([m['cv_delta'] for m in grp]), 1, True)} pp")

    printed = False
    for s in sorted({k[0] for k in keys}):
        fg, ng = groups.get((s, "FILTER")), groups.get((s, "NOFILTER"))
        if not fg or not ng:
            continue
        if not printed:
            print("\n== Fatigue contrast: (late - early) FILTER minus NOFILTER, same set = same targets ==")
            print("   (positive hit diff = filter holds up BETTER late; positive CV/drift diff = filter")
            print("    degrades consistency/speed MORE late)")
            printed = True
        for label, key in [("hit-rate change", "hit_delta"), ("RT drift ms/min", "slope"),
                           ("CV change", "cv_delta")]:
            fv, nv = group_mean([m[key] for m in fg]), group_mean([m[key] for m in ng])
            if fv is None or nv is None:
                continue
            print(f"  set {s}  {label:<18} FILTER {fv:+.1f}  vs  NOFILTER {nv:+.1f}   diff {fv - nv:+.1f}")

    print("\nNOTE: probe/window/dim configs changed across pilot days (HANDOFF.md) - the date is")
    print("in each filename; only contrast runs from the same instrument era. Late-half raw drops")
    print("alone are NOT fatigue (different targets live there); read the cross-arm contrast.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
