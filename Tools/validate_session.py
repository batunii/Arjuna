#!/usr/bin/env python3
"""
Post-session integrity validator for user-study CSV logs.

Run this BEFORE the participant leaves (testing-strategy-v2.md section 10.3):
a FAIL here triggers the re-run rules in section 8.4 while re-running is still
possible.

Blocks A and B/C run in DIFFERENT scenes/builds, so a participant produces up
to TWO CSVs. Expectations are read from the file's own CONFIG rows:

  plan=PassthroughScene_BlockA
      4 A conditions (210 s each), WINDOW_LOCKED (source=painted) before the
      first A condition, >= 4 TLX_START
  plan=VideoScene_BlocksBC
      2 B conditions (240 s, 24 PROBE_ONSET + 24 PROBE_RESULT each),
      WINDOW_LOCKED (source=fixed) before the first B condition,
      >= 2 TLX_START, and EITHER all three C_* conditions (75 s each) OR a
      BLOCKC_SKIPPED event

A file containing both plans' CONFIG rows (or, with a NOTE, recognisable
conditions from both when no plan row exists) is checked against both sets.
Rows with block=PRACTICE (practice CPT trials; practice probes ids 901-906)
are excluded from all condition-level counts.

Common checks (any plan): header contract, monotonic t_ms, no heartbeat gap
> 1 s inside a condition, CONDITION_START/END pairing and ±5 % duration,
SESSION_START/END present, >= 130 CPT_ONSET per A condition (skipped with a
NOTE when the file has no CPT events — the paper Option P task), and a WARN
when a B condition's probe hit rate is 0 % or 100 %.

Usage:
    python Tools/validate_session.py path/to/study_P07_20260712_141502.csv

Exit code: 0 = PASS (warnings allowed), 1 = FAIL or unreadable file.
"""

import argparse
import csv
import sys
from collections import defaultdict
from pathlib import Path

EXPECTED_HEADER = ["t_ms", "pid", "block", "condition", "yaw_deg", "pitch_deg",
                   "roll_deg", "head_speed_dps", "dr_intensity", "mode", "event", "payload"]

PLAN_A = "PassthroughScene_BlockA"
PLAN_BC = "VideoScene_BlocksBC"
NOMINAL_S = {"A1": 210, "A2": 210, "A3": 210, "A4": 210,
             "B1": 240, "B2": 240,
             "C_COLORPOP": 75, "C_SOFTDARK": 75, "C_HARDDARK": 75}
DURATION_TOL = 0.05
MAX_HEARTBEAT_GAP_MS = 1000
PROBES_PER_B = 24
MIN_CPT_PER_A = 130
A_CONDITIONS = {"A1", "A2", "A3", "A4"}
B_CONDITIONS = {"B1", "B2"}
C_CONDITIONS = {"C_COLORPOP", "C_SOFTDARK", "C_HARDDARK"}


class Findings:
    def __init__(self):
        self.items = []

    def add(self, level, msg):
        self.items.append((level, msg))

    def ok(self, msg):
        self.add("PASS", msg)

    def fail(self, msg):
        self.add("FAIL", msg)

    def warn(self, msg):
        self.add("WARN", msg)

    def note(self, msg):
        self.add("NOTE", msg)

    @property
    def failed(self):
        return any(lvl == "FAIL" for lvl, _ in self.items)


def parse_payload(payload):
    out = {}
    for part in (payload or "").split(";"):
        if "=" in part:
            k, _, v = part.partition("=")
            out[k.strip()] = v.strip()
    return out


def load_rows(path, f):
    with open(path, newline="", encoding="utf-8-sig") as fh:
        reader = csv.reader(fh)
        try:
            header = next(reader)
        except StopIteration:
            f.fail("file is empty")
            return None
        if [h.strip() for h in header] != EXPECTED_HEADER:
            f.fail(f"header mismatch: got {header}")
            return None
        f.ok("header matches contract")
        rows = []
        for lineno, raw in enumerate(reader, start=2):
            if not raw or all(not c for c in raw):
                continue
            if len(raw) != len(EXPECTED_HEADER):
                f.fail(f"line {lineno}: {len(raw)} columns (expected {len(EXPECTED_HEADER)})")
                continue
            try:
                t = int(float(raw[0]))
            except ValueError:
                f.fail(f"line {lineno}: unparseable t_ms {raw[0]!r}")
                continue
            rows.append({"t_ms": t, "pid": raw[1], "block": raw[2], "condition": raw[3],
                         "event": raw[10], "payload": raw[11], "lineno": lineno})
        return rows


def detect_plans(rows, f):
    """Plans from CONFIG rows; fallback inference from present conditions."""
    plans = set()
    for r in rows:
        if r["event"] == "CONFIG":
            p = parse_payload(r["payload"])
            plan = p.get("plan") or (p.get("value") if p.get("key") == "plan" else None)
            if plan:
                plans.add(plan)
    if plans:
        known = plans & {PLAN_A, PLAN_BC}
        for unknown in sorted(plans - known):
            f.warn(f"unknown plan '{unknown}' in CONFIG — ignored")
        if known:
            f.ok(f"plan(s) from CONFIG: {sorted(known)}")
            return known
    conds = {r["condition"] for r in rows if r["event"] == "CONDITION_START"}
    inferred = set()
    if conds & A_CONDITIONS:
        inferred.add(PLAN_A)
    if conds & (B_CONDITIONS | C_CONDITIONS):
        inferred.add(PLAN_BC)
    if inferred:
        f.note(f"no plan CONFIG row — inferred {sorted(inferred)} from conditions present")
    else:
        f.fail("no plan CONFIG row and no recognisable conditions")
    return inferred


def condition_spans(rows, f):
    spans = {}
    open_cond = None
    for r in rows:
        if r["event"] == "CONDITION_START":
            if open_cond is not None:
                f.fail(f"CONDITION_START '{r['condition']}' while '{open_cond[0]}' still open "
                       f"(line {r['lineno']})")
            open_cond = (r["condition"], r["t_ms"])
        elif r["event"] == "CONDITION_END":
            if open_cond is None or open_cond[0] != r["condition"]:
                f.fail(f"CONDITION_END '{r['condition']}' without matching start (line {r['lineno']})")
                open_cond = None
                continue
            if r["condition"] in spans:
                f.fail(f"condition '{r['condition']}' appears twice")
            spans[r["condition"]] = (open_cond[1], r["t_ms"])
            open_cond = None
    if open_cond is not None:
        f.fail(f"condition '{open_cond[0]}' never ended")
    return spans


def check_window_locked(rows, spans, f, cond_prefix, source, plan_name):
    starts = [t0 for c, (t0, _) in spans.items() if c.startswith(cond_prefix)]
    if not starts:
        return
    first = min(starts)
    locked = [r for r in rows if r["event"] == "WINDOW_LOCKED" and r["t_ms"] <= first]
    if not locked:
        f.fail(f"{plan_name}: no WINDOW_LOCKED before the first Block {cond_prefix} condition")
        return
    sources = {parse_payload(r["payload"]).get("source") for r in locked}
    if source in sources:
        f.ok(f"{plan_name}: WINDOW_LOCKED (source={source}) precedes Block {cond_prefix}")
    elif sources == {None}:
        f.note(f"{plan_name}: WINDOW_LOCKED present but has no source payload "
               f"(expected source={source})")
    else:
        f.fail(f"{plan_name}: WINDOW_LOCKED before Block {cond_prefix} has source "
               f"{sorted(s for s in sources if s)} (expected '{source}')")


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("csv_path", type=Path)
    args = ap.parse_args()

    f = Findings()
    rows = load_rows(args.csv_path, f)
    if rows is None:
        report(args.csv_path, f)
        return 1

    main_rows = [r for r in rows if r["block"] != "PRACTICE"]
    n_practice = len(rows) - len(main_rows)
    if n_practice:
        f.note(f"{n_practice:,} PRACTICE rows excluded from condition-level checks")

    events = defaultdict(list)
    for r in main_rows:
        if r["event"]:
            events[r["event"]].append(r)

    bad = [r["lineno"] for a, r in zip(rows, rows[1:]) if r["t_ms"] < a["t_ms"]]
    if bad:
        f.fail(f"t_ms not monotonic at lines {bad[:5]}{'...' if len(bad) > 5 else ''}")
    else:
        f.ok(f"t_ms monotonic across {len(rows):,} rows")

    for marker in ("SESSION_START", "SESSION_END"):
        if events[marker]:
            f.ok(f"{marker} present")
        else:
            f.fail(f"{marker} missing")

    plans = detect_plans(rows, f)
    spans = condition_spans(main_rows, f)
    conds_present = set(spans)

    tlx_required = 0
    if PLAN_A in plans:
        tlx_required += 4
        missing = A_CONDITIONS - conds_present
        if missing:
            f.fail(f"{PLAN_A}: missing conditions {sorted(missing)}")
        else:
            f.ok(f"{PLAN_A}: all four A conditions present")
        check_window_locked(main_rows, spans, f, "A", "painted", PLAN_A)
    if PLAN_BC in plans:
        tlx_required += 2
        missing = B_CONDITIONS - conds_present
        if missing:
            f.fail(f"{PLAN_BC}: missing conditions {sorted(missing)}")
        else:
            f.ok(f"{PLAN_BC}: both B conditions present")
        check_window_locked(main_rows, spans, f, "B", "fixed", PLAN_BC)
        c_present = C_CONDITIONS & conds_present
        if c_present == C_CONDITIONS:
            f.ok("all three Block C conditions present")
        elif events["BLOCKC_SKIPPED"]:
            reason = parse_payload(events["BLOCKC_SKIPPED"][0]["payload"]).get("reason", "?")
            f.note(f"Block C skipped (reason: {reason}) — {len(c_present)}/3 C conditions present")
        else:
            f.fail(f"Block C incomplete ({sorted(c_present)}) with no BLOCKC_SKIPPED event")

    n_tlx = len(events["TLX_START"])
    if plans:
        if n_tlx >= tlx_required:
            f.ok(f"TLX_START count {n_tlx} >= {tlx_required}")
        else:
            f.fail(f"TLX_START count {n_tlx} < {tlx_required}")

    unexpected = conds_present - set(NOMINAL_S)
    for cond in sorted(unexpected):
        f.warn(f"{cond}: unknown condition label, duration not checked")

    session_has_cpt = bool(events["CPT_ONSET"])
    if PLAN_A in plans and not session_has_cpt:
        f.note("no CPT events — assuming paper task (Option P); CPT count checks skipped")

    for cond, (t0, t1) in sorted(spans.items()):
        nominal = NOMINAL_S.get(cond)
        if nominal is not None:
            dur = (t1 - t0) / 1000.0
            if abs(dur - nominal) <= nominal * DURATION_TOL:
                f.ok(f"{cond}: duration {dur:.1f} s within ±5 % of {nominal} s")
            else:
                f.fail(f"{cond}: duration {dur:.1f} s outside ±5 % of {nominal} s")

        in_cond = [r["t_ms"] for r in main_rows if t0 <= r["t_ms"] <= t1]
        gaps = [b - a for a, b in zip(in_cond, in_cond[1:])]
        worst = max(gaps) if gaps else 0
        if worst > MAX_HEARTBEAT_GAP_MS:
            f.fail(f"{cond}: heartbeat gap {worst} ms > {MAX_HEARTBEAT_GAP_MS} ms")
        else:
            f.ok(f"{cond}: max heartbeat gap {worst} ms")

        if cond in B_CONDITIONS:
            n_on = sum(1 for r in events["PROBE_ONSET"] if r["condition"] == cond)
            n_res = sum(1 for r in events["PROBE_RESULT"] if r["condition"] == cond)
            if n_on == PROBES_PER_B and n_res == PROBES_PER_B:
                f.ok(f"{cond}: {n_on} probe onsets / {n_res} results")
            else:
                f.fail(f"{cond}: {n_on} probe onsets / {n_res} results "
                       f"(expected {PROBES_PER_B}/{PROBES_PER_B})")
            results = [parse_payload(r["payload"]).get("outcome")
                       for r in events["PROBE_RESULT"] if r["condition"] == cond]
            if results:
                hit_rate = sum(o == "hit" for o in results) / len(results)
                if hit_rate in (0.0, 1.0):
                    f.warn(f"{cond}: probe hit rate {hit_rate:.0%} — ceiling/floor, "
                           "check probe calibration")

        if cond in A_CONDITIONS and session_has_cpt:
            n_cpt = sum(1 for r in events["CPT_ONSET"] if r["condition"] == cond)
            if n_cpt >= MIN_CPT_PER_A:
                f.ok(f"{cond}: {n_cpt} CPT trials >= {MIN_CPT_PER_A}")
            else:
                f.fail(f"{cond}: {n_cpt} CPT trials < {MIN_CPT_PER_A}")

    report(args.csv_path, f)
    return 1 if f.failed else 0


def report(path, f):
    print(f"\n=== validate_session: {path} ===")
    order = {"FAIL": 0, "WARN": 1, "NOTE": 2, "PASS": 3}
    for lvl, msg in sorted(f.items, key=lambda x: order[x[0]]):
        print(f"  [{lvl}] {msg}")
    n = lambda lvl: sum(1 for l, _ in f.items if l == lvl)
    verdict = "FAIL" if f.failed else "PASS"
    print(f"--- {verdict}: {n('FAIL')} fail, {n('WARN')} warn, {n('NOTE')} note, {n('PASS')} pass ---\n")


if __name__ == "__main__":
    sys.exit(main())
