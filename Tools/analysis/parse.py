#!/usr/bin/env python3
"""
Parse StudyLogger session CSVs into tidy trial-level DataFrames.

Condition labels are SEMANTIC (testing-strategy-v2.md section 4): the Latin
square permutes their order, never their meaning:
    A1 = vignette Off, distractors absent      A2 = Off, present
    A3 = vignette On (Hard Dark), absent       A4 = On, present
    B1 = vignette Off                          B2 = ColorPop on
As a sanity cross-check, mean dr_intensity per condition is compared against
the expected on/off state; a mismatch raises a warning column, never a silent
relabel.

Outputs (per session or whole dataset directory):
    cpt_trials   pid, condition, vignette, distractor, idx, kind, outcome,
                 rt_ms, is_error, t_in_cond_s
    probe_trials pid, condition, colorpop, id, band, az_deg, el_deg, outcome,
                 rt_ms, deferred_s
    conditions   pid, condition, t_start_ms, t_end_ms, duration_s,
                 mean_dr_intensity, dr_state_expected, dr_state_ok
    events       long table of all labelled events (for ad-hoc queries)

TLX scores and Block C Likert ratings are collected on paper/tablet, NOT in
the CSV; supply them separately (see h2_tost/likert docstrings).

CLI:
    python Tools/analysis/parse.py <session.csv | directory> --out out_dir
"""

import argparse
import sys
from pathlib import Path

import numpy as np
import pandas as pd

VIGNETTE_ON = {"A3", "A4", "B2", "C_COLORPOP", "C_SOFTDARK", "C_HARDDARK"}
DISTRACTOR_PRESENT = {"A2", "A4"}
ERROR_OUTCOMES = {"miss", "commission"}


def parse_payload(payload: str) -> dict:
    out = {}
    if isinstance(payload, str):
        for part in payload.split(";"):
            if "=" in part:
                k, _, v = part.partition("=")
                out[k.strip()] = v.strip()
    return out


def load_session(path: Path) -> pd.DataFrame:
    df = pd.read_csv(path, dtype={"pid": str, "block": str, "condition": str,
                                  "event": str, "payload": str},
                     keep_default_na=False, na_values=[])
    df["t_ms"] = df["t_ms"].astype("int64")
    for col in ("yaw_deg", "pitch_deg", "roll_deg", "head_speed_dps", "dr_intensity"):
        df[col] = pd.to_numeric(df[col], errors="coerce")
    return df


def _conditions(df: pd.DataFrame) -> pd.DataFrame:
    rows = []
    for cond, grp in df[df["event"].isin(["CONDITION_START", "CONDITION_END"])].groupby("condition"):
        starts = grp.loc[grp["event"] == "CONDITION_START", "t_ms"]
        ends = grp.loc[grp["event"] == "CONDITION_END", "t_ms"]
        if starts.empty or ends.empty:
            continue
        t0, t1 = int(starts.iloc[0]), int(ends.iloc[0])
        in_cond = df[(df["t_ms"] >= t0) & (df["t_ms"] <= t1)]
        mean_dr = float(in_cond["dr_intensity"].mean())
        expected_on = cond in VIGNETTE_ON
        rows.append({
            "pid": df["pid"].iloc[0], "condition": cond,
            "t_start_ms": t0, "t_end_ms": t1, "duration_s": (t1 - t0) / 1000.0,
            "mean_dr_intensity": mean_dr,
            "dr_state_expected": "on" if expected_on else "off",
            "dr_state_ok": bool((mean_dr > 0.25) == expected_on),
        })
    return pd.DataFrame(rows)


def _cpt_trials(df: pd.DataFrame, conditions: pd.DataFrame) -> pd.DataFrame:
    onsets = df[df["event"] == "CPT_ONSET"].copy()
    results = df[df["event"] == "CPT_RESULT"].copy()
    if onsets.empty:
        return pd.DataFrame(columns=["pid", "condition", "vignette", "distractor", "idx",
                                     "kind", "outcome", "rt_ms", "is_error", "t_in_cond_s"])
    start_by_cond = conditions.set_index("condition")["t_start_ms"].to_dict()
    rows = []
    res_map = {}
    for _, r in results.iterrows():
        p = parse_payload(r["payload"])
        res_map[(r["condition"], p.get("idx"))] = p
    for _, r in onsets.iterrows():
        p = parse_payload(r["payload"])
        res = res_map.get((r["condition"], p.get("idx")), {})
        outcome = res.get("outcome", "missing_result")
        rt = res.get("rt_ms")
        rows.append({
            "pid": r["pid"], "condition": r["condition"],
            "vignette": "on" if r["condition"] in VIGNETTE_ON else "off",
            "distractor": "present" if r["condition"] in DISTRACTOR_PRESENT else "absent",
            "idx": int(p.get("idx", -1)), "kind": p.get("kind", "?"),
            "outcome": outcome,
            "rt_ms": float(rt) if rt not in (None, "") else np.nan,
            "is_error": outcome in ERROR_OUTCOMES,
            "t_in_cond_s": (r["t_ms"] - start_by_cond.get(r["condition"], r["t_ms"])) / 1000.0,
        })
    return pd.DataFrame(rows)


def _probe_trials(df: pd.DataFrame) -> pd.DataFrame:
    onsets = df[df["event"] == "PROBE_ONSET"].copy()
    if onsets.empty:
        return pd.DataFrame(columns=["pid", "condition", "colorpop", "id", "band",
                                     "az_deg", "el_deg", "outcome", "rt_ms", "deferred_s"])
    res_map, defer_map = {}, {}
    for _, r in df[df["event"] == "PROBE_RESULT"].iterrows():
        p = parse_payload(r["payload"])
        res_map[(r["condition"], p.get("id"))] = p
    for _, r in df[df["event"] == "PROBE_DEFERRED"].iterrows():
        p = parse_payload(r["payload"])
        defer_map[(r["condition"], p.get("id"))] = float(p.get("delay_s", 0))
    rows = []
    for _, r in onsets.iterrows():
        p = parse_payload(r["payload"])
        key = (r["condition"], p.get("id"))
        res = res_map.get(key, {})
        rt = res.get("rt_ms")
        rows.append({
            "pid": r["pid"], "condition": r["condition"],
            "colorpop": r["condition"] == "B2",
            "id": int(p.get("id", -1)), "band": p.get("band", "?"),
            "az_deg": float(p.get("az_deg", np.nan)), "el_deg": float(p.get("el_deg", np.nan)),
            "outcome": res.get("outcome", "missing_result"),
            "rt_ms": float(rt) if rt not in (None, "") else np.nan,
            "deferred_s": defer_map.get(key, 0.0),
        })
    return pd.DataFrame(rows)


def parse_session(path: Path) -> dict:
    df = load_session(path)
    # Blocks A and B/C may arrive as separate files (different scenes/builds);
    # parse_dataset() concatenates them per pid. PRACTICE rows (practice CPT
    # trials; practice probes ids 901-906) never enter the trial tables.
    main = df[df["block"] != "PRACTICE"]
    conditions = _conditions(main)
    return {
        "conditions": conditions,
        "cpt_trials": _cpt_trials(main, conditions),
        "probe_trials": _probe_trials(main),
        "events": df[df["event"] != ""][["t_ms", "pid", "block", "condition", "event", "payload"]],
    }


def parse_dataset(directory: Path) -> dict:
    """Parse every study_P*.csv in a directory into concatenated tables."""
    paths = sorted(directory.glob("study_P*.csv"))
    if not paths:
        raise FileNotFoundError(f"no study_P*.csv files in {directory}")
    merged = {}
    for p in paths:
        for name, table in parse_session(p).items():
            merged.setdefault(name, []).append(table)
    out = {}
    for name, tables in merged.items():
        # Drop empty frames before concat: an empty table (e.g. cpt_trials from
        # a Blocks-B/C-only file) is all-object dtype and would upcast the
        # concatenated result, breaking numeric stats downstream.
        nonempty = [t for t in tables if not t.empty]
        out[name] = (pd.concat(nonempty, ignore_index=True) if nonempty
                     else tables[0])
    return out


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("path", type=Path, help="a session CSV or a directory of them")
    ap.add_argument("--out", type=Path, default=Path("parsed"),
                    help="output directory for tidy CSVs")
    args = ap.parse_args()

    tables = parse_dataset(args.path) if args.path.is_dir() else parse_session(args.path)
    args.out.mkdir(parents=True, exist_ok=True)
    for name, table in tables.items():
        dest = args.out / f"{name}.csv"
        table.to_csv(dest, index=False)
        print(f"wrote {dest} ({len(table):,} rows)")
    bad = tables["conditions"][~tables["conditions"]["dr_state_ok"]]
    if not bad.empty:
        print("WARNING: dr_intensity does not match expected vignette state for:")
        print(bad[["pid", "condition", "mean_dr_intensity", "dr_state_expected"]].to_string(index=False))
    return 0


if __name__ == "__main__":
    sys.exit(main())
