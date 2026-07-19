#!/usr/bin/env python3
"""
Blob-probe (informal driving harness) analysis — turns the raw
blobtargets_*.csv into a VALID attention measure, following the probe-detection
methods literature (see Dissertation/probe-target-design.md sec 4c).

Why this exists: the probe's physical detectability depends on the scene it sits
on and on the filter's local dimming, so a raw "miss" is ambiguous (inattention
vs invisibility). Because the SAME seeded targets appear over the SAME background
in both filter-ON and filter-OFF modes (paired, within-subject), per-target
difficulty CANCELS in the ON-OFF difference and cannot bias the mean effect
(Maxwell, Delaney & Kelley 2018) — it only costs power and can bias through ONE
channel: floor/ceiling truncation (Loftus 1978; Liu & Wang 2021). This module
implements the cheap, standard defenses:

  #2  Item screening   — per-target baseline (no-filter) detectability; drop
                          targets whose baseline hit-rate is at floor/ceiling
                          (default <0.15 or >0.90; pre-register your own cut).
  #4  ISO 17488 scoring — classify hits by reaction time: premature (<100 ms),
                          valid (100-2500 ms), late/unrequested (>2500 ms).
  #3  d' (approx)      — hits vs false alarms per mode with the Hautus (1995)
                          log-linear correction. NOTE: rigorous d' needs DEFINED
                          catch (no-signal) trials; this harness has none, so the
                          false-alarm RATE here uses target count as the
                          no-signal denominator and d' is a RELATIVE index only.

The screened target set + per-mode hit-rate / RT / d' are what feed the headline
filter-ON vs filter-OFF comparison (analysed per-item with crossed subject x item
mixed models, #5 — done in R on the formal StudyLogger data, not here).

CSV schema (BlobTargetController.k_logHeader):
    t_ms,mode,target_id,target_kind,t_start,t_end,duration_s,outcome,rt_s,angle_deg
outcomes: hit | miss | bad_aim_attempt | false_alarm

CLI:
    python Tools/analysis/blob_probe.py <blobtargets.csv | dir> [--out dir]
        [--baseline-label SUBSTR] [--floor 0.15] [--ceiling 0.90]
"""

import argparse
import math
import sys
from pathlib import Path

import numpy as np
import pandas as pd

# ISO 17488:2016 response window (seconds from probe onset).
ISO_MIN_S = 0.100
ISO_MAX_S = 2.500


def load(path: Path) -> pd.DataFrame:
    """Load one or many blobtargets_*.csv into a single frame (adds source_file)."""
    paths = sorted(path.glob("blobtargets_*.csv")) if path.is_dir() else [path]
    if not paths:
        raise FileNotFoundError(f"no blobtargets_*.csv in {path}")
    frames = []
    for p in paths:
        df = pd.read_csv(p, dtype={"mode": str, "outcome": str, "target_kind": str},
                         keep_default_na=False, na_values=[])
        df["source_file"] = p.name
        frames.append(df)
    df = pd.concat(frames, ignore_index=True)
    for col in ("target_id", "t_start", "t_end", "duration_s", "rt_s", "angle_deg"):
        df[col] = pd.to_numeric(df[col], errors="coerce")
    return df


def pick_baseline_mode(df: pd.DataFrame, hint: str | None) -> str:
    """Choose which mode label is the no-filter baseline for screening."""
    modes = df["mode"].unique().tolist()
    if hint:
        for m in modes:
            if hint.lower() in m.lower():
                return m
        raise SystemExit(f"--baseline-label '{hint}' matched none of: {modes}")
    for needle in ("baseline", "no filter", "nofilter"):
        for m in modes:
            if needle in m.lower():
                return m
    raise SystemExit(f"could not auto-detect a baseline mode among {modes}; "
                     f"pass --baseline-label")


def per_target(df: pd.DataFrame) -> pd.DataFrame:
    """Per (mode, target) terminal-outcome counts and hit-rate.

    Each target resolves once per video loop as hit or miss (bad_aim_attempt is a
    non-terminal extra press; false_alarm has no target). Hit-rate aggregates over
    all loop occurrences of that target in that mode."""
    tgt = df[df["outcome"].isin(["hit", "miss"])].copy()
    g = (tgt.assign(is_hit=(tgt["outcome"] == "hit").astype(int))
            .groupby(["mode", "target_id", "target_kind"], as_index=False)
            .agg(n_occ=("is_hit", "size"), n_hit=("is_hit", "sum")))
    g["n_miss"] = g["n_occ"] - g["n_hit"]
    g["hit_rate"] = g["n_hit"] / g["n_occ"].where(g["n_occ"] > 0, np.nan)
    rt = (df[df["outcome"] == "hit"].groupby(["mode", "target_id"], as_index=False)
            .agg(mean_rt_s=("rt_s", "mean")))
    return g.merge(rt, on=["mode", "target_id"], how="left")


def screen(baseline: pd.DataFrame, floor: float, ceiling: float) -> pd.DataFrame:
    """#2 item screening on baseline detectability."""
    out = baseline[["target_id", "target_kind", "n_occ", "hit_rate"]].copy()
    out = out.rename(columns={"hit_rate": "baseline_hit_rate"})
    def reason(r):
        if r < floor:   return f"floor (<{floor:g})"
        if r > ceiling: return f"ceiling (>{ceiling:g})"
        return ""
    out["reason"] = out["baseline_hit_rate"].apply(reason)
    out["keep"] = out["reason"] == ""
    return out.sort_values("baseline_hit_rate")


def iso_rt(df: pd.DataFrame) -> pd.DataFrame:
    """#4 ISO 17488 RT classification of hits, per mode."""
    hits = df[df["outcome"] == "hit"].copy()
    if hits.empty:
        return pd.DataFrame(columns=["mode", "n_hit", "premature", "valid", "late"])
    hits["cls"] = np.where(hits["rt_s"] < ISO_MIN_S, "premature",
                    np.where(hits["rt_s"] <= ISO_MAX_S, "valid", "late"))
    piv = (hits.groupby(["mode", "cls"]).size().unstack(fill_value=0)
              .reindex(columns=["premature", "valid", "late"], fill_value=0)
              .reset_index())
    piv["n_hit"] = piv[["premature", "valid", "late"]].sum(axis=1)
    return piv[["mode", "n_hit", "premature", "valid", "late"]]


def _z(p: float) -> float:
    # inverse standard-normal CDF (Acklam); avoids a scipy dependency
    a = [-3.969683028665376e+01, 2.209460984245205e+02, -2.759285104469687e+02,
         1.383577518672690e+02, -3.066479806614716e+01, 2.506628277459239e+00]
    b = [-5.447609879822406e+01, 1.615858368580409e+02, -1.556989798598866e+02,
         6.680131188771972e+01, -1.328068155288572e+01]
    c = [-7.784894002430293e-03, -3.223964580411365e-01, -2.400758277161838e+00,
         -2.549732539343734e+00, 4.374664141464968e+00, 2.938163982698783e+00]
    d = [7.784695709041462e-03, 3.224671290700398e-01, 2.445134137142996e+00,
         3.754408661907416e+00]
    plow, phigh = 0.02425, 1 - 0.02425
    if p < plow:
        q = math.sqrt(-2 * math.log(p))
        return (((((c[0]*q+c[1])*q+c[2])*q+c[3])*q+c[4])*q+c[5]) / \
               ((((d[0]*q+d[1])*q+d[2])*q+d[3])*q+1)
    if p <= phigh:
        q = p - 0.5; r = q*q
        return (((((a[0]*r+a[1])*r+a[2])*r+a[3])*r+a[4])*r+a[5])*q / \
               (((((b[0]*r+b[1])*r+b[2])*r+b[3])*r+b[4])*r+1)
    q = math.sqrt(-2 * math.log(1 - p))
    return -(((((c[0]*q+c[1])*q+c[2])*q+c[3])*q+c[4])*q+c[5]) / \
            ((((d[0]*q+d[1])*q+d[2])*q+d[3])*q+1)


def sdt(df: pd.DataFrame) -> pd.DataFrame:
    """#3 d' (approx) and criterion per mode, Hautus (1995) log-linear correction.

    APPROX: no defined catch trials in this harness, so false-alarm opportunities
    are taken as the number of signal (target) occurrences. d' is a RELATIVE index
    across modes, not an absolute sensitivity — add real catch trials for rigor."""
    rows = []
    for mode, grp in df.groupby("mode"):
        n_hit = int((grp["outcome"] == "hit").sum())
        n_miss = int((grp["outcome"] == "miss").sum())
        n_signal = n_hit + n_miss
        n_fa = int((grp["outcome"] == "false_alarm").sum())
        if n_signal == 0:
            continue
        # log-linear: +0.5 to hits & FAs, +1 to each trial total
        h = (n_hit + 0.5) / (n_signal + 1.0)
        f = (n_fa + 0.5) / (n_signal + 1.0)          # signal count as FA denominator (approx)
        dprime = _z(h) - _z(f)
        crit = -0.5 * (_z(h) + _z(f))
        rows.append({"mode": mode, "n_signal": n_signal, "n_hit": n_hit,
                     "hit_rate": n_hit / n_signal, "n_false_alarm": n_fa,
                     "fa_rate_approx": n_fa / n_signal,
                     "dprime_approx": round(dprime, 3), "criterion_approx": round(crit, 3)})
    return pd.DataFrame(rows)


def analyse(df: pd.DataFrame, baseline_hint: str | None, floor: float, ceiling: float) -> dict:
    pt = per_target(df)
    base_mode = pick_baseline_mode(df, baseline_hint)
    baseline = pt[pt["mode"] == base_mode]
    screen_tbl = screen(baseline, floor, ceiling)
    keep_ids = set(screen_tbl.loc[screen_tbl["keep"], "target_id"])

    # Per-mode hit-rate on the SCREENED target set (the headline comparison input).
    screened = pt[pt["target_id"].isin(keep_ids)]
    per_mode = (screened.groupby("mode", as_index=False)
                .agg(n_targets=("target_id", "nunique"),
                     n_hit=("n_hit", "sum"), n_occ=("n_occ", "sum")))
    per_mode["hit_rate_screened"] = per_mode["n_hit"] / per_mode["n_occ"]

    return {"baseline_mode": base_mode, "per_target": pt, "screening": screen_tbl,
            "per_mode_screened": per_mode, "iso_rt": iso_rt(df), "sdt": sdt(df),
            "n_dropped": int((~screen_tbl["keep"]).sum())}


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("path", type=Path, help="a blobtargets_*.csv or a directory of them")
    ap.add_argument("--baseline-label", default=None,
                    help="substring identifying the no-filter baseline mode (auto if omitted)")
    ap.add_argument("--floor", type=float, default=0.15, help="drop targets below this baseline hit-rate")
    ap.add_argument("--ceiling", type=float, default=0.90, help="drop targets above this baseline hit-rate")
    ap.add_argument("--out", type=Path, default=None, help="optional dir to write tidy CSVs")
    args = ap.parse_args()

    df = load(args.path)
    r = analyse(df, args.baseline_label, args.floor, args.ceiling)

    print(f"baseline mode: {r['baseline_mode']!r}")
    print(f"targets dropped by screening (floor {args.floor:g} / ceiling {args.ceiling:g}): "
          f"{r['n_dropped']}")
    print("\n#2 SCREENING (baseline detectability per target):")
    print(r["screening"].to_string(index=False))
    print("\nPER-MODE hit-rate on SCREENED targets (headline comparison input):")
    print(r["per_mode_screened"].to_string(index=False))
    print("\n#4 ISO 17488 RT classification (hits):")
    print(r["iso_rt"].to_string(index=False))
    print("\n#3 d' (APPROX - needs real catch trials for rigor):")
    print(r["sdt"].to_string(index=False))

    if args.out:
        args.out.mkdir(parents=True, exist_ok=True)
        for name in ("per_target", "screening", "per_mode_screened", "iso_rt", "sdt"):
            dest = args.out / f"blob_{name}.csv"
            r[name].to_csv(dest, index=False)
            print(f"wrote {dest}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
