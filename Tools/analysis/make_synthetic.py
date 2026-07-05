#!/usr/bin/env python3
"""
Synthetic session generator — pipeline test harness, NOT study data.

Emits StudyLogger-contract CSVs with plausible embedded effects: a real
distractor cost with the vignette off, ~60 % of it recovered with the vignette
on, faster central-probe RTs and a small (<10 pp) peripheral hit-rate drop
under ColorPop. Two participants skip Block C. Matching synthetic TLX and
Block C ratings CSVs are written alongside.

Blocks A and B/C run in DIFFERENT scenes/builds, so by default each
participant produces TWO files (matching the real deployment):
    study_P<pid>_..._090000.csv   plan=PassthroughScene_BlockA
    study_P<pid>_..._103000.csv   plan=VideoScene_BlocksBC
`--single-file` emits one combined file (both plans' CONFIG rows coexist) to
test that path. Practice trials appear under block=PRACTICE (practice probe
ids 901-906) and must be excluded by consumers.

CPT no-go sequence matches the C# generator: exactly 20 % no-go, no two
consecutive no-gos, go-runs <= 8.

Purpose: prove the validator + parse -> h1 -> h2 -> likert -> report pipeline
end-to-end before any real participant exists. Everything is seeded.

CLI:
    python Tools/analysis/make_synthetic.py --out synth_data            # N=20, 5 Hz
    python Tools/analysis/make_synthetic.py --out solo --n 1 --hz 30
    python Tools/analysis/make_synthetic.py --out combo --n 1 --single-file
"""

import argparse
import csv
import json
import sys
from pathlib import Path

import numpy as np

REPO_ROOT = Path(__file__).resolve().parent.parent.parent
SCHEDULE_DIR = REPO_ROOT / "Assets/StreamingAssets/StudySchedules"
HEADER = ["t_ms", "pid", "block", "condition", "yaw_deg", "pitch_deg", "roll_deg",
          "head_speed_dps", "dr_intensity", "mode", "event", "payload"]

EPOCH_MS = 1_767_225_600_000  # fixed synthetic epoch (2026-01-01)
LATIN_A = [["A1", "A2", "A4", "A3"], ["A2", "A3", "A1", "A4"],
           ["A3", "A4", "A2", "A1"], ["A4", "A1", "A3", "A2"]]
LATIN_C = [["C_COLORPOP", "C_SOFTDARK", "C_HARDDARK"],
           ["C_SOFTDARK", "C_HARDDARK", "C_COLORPOP"],
           ["C_HARDDARK", "C_COLORPOP", "C_SOFTDARK"]]
MODE_OF = {"A1": "NONE", "A2": "NONE", "A3": "HARDDARK", "A4": "HARDDARK",
           "B1": "NONE", "B2": "COLORPOP", "C_COLORPOP": "COLORPOP",
           "C_SOFTDARK": "SOFTDARK", "C_HARDDARK": "HARDDARK"}
DR_OF = {"A1": 0.0, "A2": 0.0, "A3": 1.0, "A4": 1.0, "B1": 0.0, "B2": 1.0,
         "C_COLORPOP": 1.0, "C_SOFTDARK": 0.75, "C_HARDDARK": 1.0}
CPT_TRIALS = 140
CPT_PERIOD_S = 1.5
PLAN_A = "PassthroughScene_BlockA"
PLAN_BC = "VideoScene_BlocksBC"


def cpt_kind_sequence(rng, n=CPT_TRIALS):
    """Exactly 20 % no-go, no two consecutive no-gos, go-runs <= 8 (C# parity)."""
    n_nogo = n // 5
    n_go = n - n_nogo
    # go-run gaps around the no-gos: ends 0..8, interior 1..8, sum = n_go
    gaps = [2] + [4] * (n_nogo - 1) + [2]
    deficit = n_go - sum(gaps)
    while deficit != 0:
        i = int(rng.integers(0, len(gaps)))
        lo = 0 if i in (0, len(gaps) - 1) else 1
        if deficit > 0 and gaps[i] < 8:
            gaps[i] += 1
            deficit -= 1
        elif deficit < 0 and gaps[i] > lo:
            gaps[i] -= 1
            deficit += 1
    for _ in range(600):                       # shuffle mass between gaps, keep bounds
        i, j = rng.integers(0, len(gaps), 2)
        lo_i = 0 if i in (0, len(gaps) - 1) else 1
        if gaps[i] > lo_i and gaps[j] < 8:
            gaps[i] -= 1
            gaps[j] += 1
    seq = []
    for k, g in enumerate(gaps):
        seq += ["go"] * int(g)
        if k < n_nogo:
            seq.append("nogo")
    assert len(seq) == n and seq.count("nogo") == n_nogo
    assert "nogonogo" not in "".join("nogo" if s == "nogo" else "g" for s in seq)
    return seq


class SessionBuilder:
    def __init__(self, pid, rng, hz, t_start_ms):
        self.pid, self.rng, self.hz = pid, rng, hz
        self.t = t_start_ms
        self.rows = []
        self.block, self.condition, self.mode, self.dr = "", "", "NONE", 0.0
        self.yaw = 0.0

    def emit(self, event="", payload="", t=None):
        t = self.t if t is None else t
        self.yaw += float(self.rng.normal(0, 0.8))
        self.rows.append([t, self.pid, self.block, self.condition,
                          round(self.yaw, 2), round(float(self.rng.normal(0, 3)), 2),
                          round(float(self.rng.normal(0, 1)), 2),
                          round(abs(float(self.rng.normal(5, 4))), 2),
                          self.dr, self.mode, event, payload])

    def heartbeats(self, seconds):
        step = int(1000 / self.hz)
        end = self.t + int(seconds * 1000)
        while self.t < end:
            self.emit()
            self.t += step
        self.t = end

    def advance(self, seconds):
        end = self.t + int(seconds * 1000)
        while self.t < end:
            self.emit()
            self.t += 1000
        self.t = end

    def timed_events(self, end_offset_ms, events):
        """Heartbeat to t+end_offset_ms, interleaving pre-timestamped events."""
        end = self.t + end_offset_ms
        step = int(1000 / self.hz)
        ev = iter(sorted(events))
        nxt = next(ev, None)
        while self.t < end:
            while nxt and nxt[0] <= self.t:
                self.emit(nxt[1], nxt[2], t=nxt[0])
                nxt = next(ev, None)
            self.emit()
            self.t += step
        while nxt:
            self.emit(nxt[1], nxt[2], t=min(nxt[0], end - 1))
            nxt = next(ev, None)
        self.t = end


def cpt_condition(b, cond, err_p, rng):
    t0 = b.t
    b.condition, b.mode, b.dr = cond, MODE_OF[cond], DR_OF[cond]
    b.emit("MODE_SET", f"mode={MODE_OF[cond]}")
    b.emit("CONDITION_START")
    b.emit("CPT_RUN_START")
    events = []
    kinds = cpt_kind_sequence(rng)
    for i, kind in enumerate(kinds):
        onset = t0 + int(i * CPT_PERIOD_S * 1000) + 200
        err = rng.random() < err_p
        if kind == "go":
            outcome = "miss" if err else "hit"
            rt = "" if err else f";rt_ms={max(200, rng.normal(450, 80)):.0f}"
        else:
            outcome = "commission" if err else "correct_reject"
            rt = f";rt_ms={max(200, rng.normal(430, 90)):.0f}" if err else ""
        events.append((onset, "CPT_ONSET", f"idx={i};kind={kind}"))
        events.append((onset + 700, "CPT_RESULT", f"idx={i};outcome={outcome}{rt}"))
    events.append((t0 + 209_800, "CPT_RUN_END", ""))
    b.timed_events(210_000, events)
    b.emit("CONDITION_END")
    b.condition, b.mode, b.dr = "", "NONE", 0.0


def probe_condition(b, cond, schedule, hit_c, hit_p, rt_mu, rng):
    t0 = b.t
    b.condition, b.mode, b.dr = cond, MODE_OF[cond], DR_OF[cond]
    b.emit("MODE_SET", f"mode={MODE_OF[cond]}")
    b.emit("PROBE_SCHEDULE", f"file=probes_{cond}.json;version={schedule['version']};seed={schedule['seed']}")
    b.emit("CONDITION_START")
    events = []
    for p in schedule["probes"]:
        onset = t0 + int(p["t"] * 1000)
        if rng.random() < 0.08:
            delay = float(rng.uniform(0.5, 2.5))
            events.append((onset, "PROBE_DEFERRED", f"id={p['id']};delay_s={delay:.1f}"))
            onset += int(delay * 1000)
        events.append((onset, "PROBE_ONSET",
                       f"id={p['id']};band={p['band']};az_deg={p['az_deg']};el_deg={p['el_deg']}"))
        hit = rng.random() < (hit_c if p["band"] == "central" else hit_p)
        if hit:
            rt = max(150, rng.normal(rt_mu, 70))
            events.append((onset + int(rt), "PROBE_RESULT",
                           f"id={p['id']};outcome=hit;rt_ms={rt:.0f}"))
        else:
            events.append((onset + 2500, "PROBE_RESULT", f"id={p['id']};outcome=miss"))
    for _ in range(int(rng.integers(0, 3))):
        events.append((t0 + int(rng.uniform(5, 230) * 1000), "FALSE_ALARM", ""))
    b.timed_events(240_000, events)
    b.emit("CONDITION_END")
    b.condition, b.mode, b.dr = "", "NONE", 0.0


def tlx_break(b, seconds=60):
    b.emit("TLX_START")
    b.advance(seconds)
    b.emit("TLX_END")


def session_header(b, plan, pid_num):
    b.emit("SESSION_START", "app_version=study-1.0.0;schedule_version=1")
    b.emit("CONFIG", f"plan={plan}")
    for k, v in [("m_enableMotion", "true"), ("bakedDetections", "off"),
                 ("latin_row", str((pid_num - 1) % 4)),
                 ("b_order", "B1_first" if pid_num % 2 else "B2_first")]:
        b.emit("CONFIG", f"key={k};value={v}")


def effects_a(pid_num, rng):
    base_err = float(np.clip(rng.normal(0.05, 0.015), 0.02, 0.12))
    cost = float(np.clip(rng.normal(0.07, 0.03), 0.015, 0.20))
    recovery = float(np.clip(rng.normal(0.60, 0.20), -0.2, 1.1))
    return {"A1": base_err, "A2": base_err + cost,
            "A3": base_err + 0.005, "A4": base_err + 0.005 + cost * (1 - recovery)}


def build_block_a(b, pid_num, rng):
    """Practice CPT + painted window + 4 A conditions with TLX each."""
    b.block = "PRACTICE"
    b.emit("PRACTICE_START")
    prac = []
    t0 = b.t
    for i in range(20):
        onset = t0 + int(i * CPT_PERIOD_S * 1000) + 200
        kind = "nogo" if i % 5 == 4 else "go"
        outcome = "hit" if kind == "go" else "correct_reject"
        rt = f";rt_ms={max(200, rng.normal(470, 90)):.0f}" if kind == "go" else ""
        prac.append((onset, "CPT_ONSET", f"idx={i};kind={kind}"))
        prac.append((onset + 700, "CPT_RESULT", f"idx={i};outcome={outcome}{rt}"))
    b.timed_events(35_000, prac)
    b.emit("PRACTICE_END")
    b.block = "A"
    b.emit("CPT_PANEL_PLACED", "dist_m=0.6")
    b.emit("WINDOW_LOCKED", "source=painted;azMin=-0.35;azMax=0.35;elMin=-0.30;elMax=0.10")
    b.advance(30)
    b.emit("BLOCK_START", "block=A")
    err_p = effects_a(pid_num, rng)
    for cond in LATIN_A[(pid_num - 1) % 4]:
        cpt_condition(b, cond, float(np.clip(err_p[cond], 0.005, 0.6)), rng)
        tlx_break(b, 60)
    b.emit("BLOCK_END", "block=A")


def build_blocks_bc(b, pid_num, rng, schedules, skip_c):
    """Practice probes (ids 901-906) + fixed window + B conditions + C sampler."""
    b.block = "PRACTICE"
    b.emit("PRACTICE_START")
    b.emit("PROBE_SCHEDULE", "file=probes_practice.json;version=1;seed=0")
    prac = []
    t0 = b.t
    for j, pid_probe in enumerate(range(901, 907)):
        onset = t0 + 3000 + j * 7000
        band = "central" if j % 2 == 0 else "peripheral"
        prac.append((onset, "PROBE_ONSET", f"id={pid_probe};band={band};az_deg=0;el_deg=0"))
        outcome = "hit" if j != 2 else "miss"
        rt = f";rt_ms={max(200, rng.normal(520, 90)):.0f}" if outcome == "hit" else ""
        prac.append((onset + 600, "PROBE_RESULT", f"id={pid_probe};outcome={outcome}{rt}"))
    b.timed_events(50_000, prac)
    b.emit("PRACTICE_END")
    b.block = "B"
    b.emit("WINDOW_LOCKED", "source=fixed;azMin=-0.30;azMax=0.30;elMin=-0.15;elMax=0.20")
    b.advance(10)
    b.emit("BLOCK_START", "block=B")
    rt_off = float(rng.normal(520, 40))
    rt_on = rt_off - float(rng.normal(50, 25))
    peri_off = float(np.clip(rng.normal(0.80, 0.05), 0.5, 0.98))
    peri_on = float(np.clip(peri_off - rng.normal(0.03, 0.04), 0.4, 0.98))
    b_order = ["B1", "B2"] if pid_num % 2 else ["B2", "B1"]
    for cond in b_order:
        if cond == "B1":
            probe_condition(b, cond, schedules["B1"], 0.85, peri_off, rt_off, rng)
        else:
            probe_condition(b, cond, schedules["B2"], 0.87, peri_on, rt_on, rng)
        tlx_break(b, 60)
    b.emit("BLOCK_END", "block=B")
    if skip_c:
        b.emit("BLOCKC_SKIPPED", "reason=fatigue")
        return
    b.block = "C"
    b.emit("BLOCK_START", "block=C")
    for cond in LATIN_C[(pid_num - 1) % 3]:
        b.condition, b.mode, b.dr = cond, MODE_OF[cond], DR_OF[cond]
        b.emit("MODE_SET", f"mode={MODE_OF[cond]}")
        b.emit("SAMPLER_MODE_START", f"mode={MODE_OF[cond]}")
        b.emit("CONDITION_START")
        b.heartbeats(75)
        b.emit("CONDITION_END")
        b.emit("SAMPLER_MODE_END", f"mode={MODE_OF[cond]}")
        b.condition, b.mode, b.dr = "", "NONE", 0.0
        b.advance(45)
    b.emit("BLOCK_END", "block=C")


def close_session(b):
    b.block = ""
    b.advance(60)
    b.emit("SESSION_END")


def write_rows(pid, stamp, rows, out_dir):
    path = out_dir / f"study_{pid}_20260101_{stamp}.csv"
    with open(path, "w", newline="", encoding="utf-8") as fh:
        w = csv.writer(fh)
        w.writerow(HEADER)
        w.writerows(rows)
    return path


def write_questionnaires(n, out_dir, rng, skip_pids):
    with open(out_dir / "tlx_scores.csv", "w", newline="", encoding="utf-8") as fh:
        w = csv.writer(fh)
        w.writerow(["pid", "condition", "mental", "physical", "temporal",
                    "performance", "effort", "frustration"])
        base_by_cond = {"A1": 30, "A2": 55, "A3": 32, "A4": 42, "B1": 40, "B2": 36}
        for i in range(1, n + 1):
            off = rng.normal(0, 6)
            for cond, base in base_by_cond.items():
                w.writerow([f"P{i:02d}", cond] +
                           [int(np.clip(base + off + rng.normal(0, 8), 0, 100) // 5 * 5)
                            for _ in range(6)])
    with open(out_dir / "ratings.csv", "w", newline="", encoding="utf-8") as fh:
        w = csv.writer(fh)
        w.writerow(["pid", "mode", "item", "rating"])
        centre = {"COLORPOP": {"comfort": 5.4, "focus_benefit": 5.0, "willingness": 4.8},
                  "SOFTDARK": {"comfort": 5.8, "focus_benefit": 4.6, "willingness": 5.0},
                  "HARDDARK": {"comfort": 3.8, "focus_benefit": 5.8, "willingness": 3.9}}
        for i in range(1, n + 1):
            if i in skip_pids:
                continue
            for mode, items in centre.items():
                for item, mu in items.items():
                    w.writerow([f"P{i:02d}", mode, item,
                                int(np.clip(round(rng.normal(mu, 1.0)), 1, 7))])


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--out", type=Path, required=True)
    ap.add_argument("--n", type=int, default=20)
    ap.add_argument("--hz", type=float, default=5.0,
                    help="synthetic heartbeat rate (contract is ~60; 5 keeps files small)")
    ap.add_argument("--seed", type=int, default=42)
    ap.add_argument("--single-file", action="store_true",
                    help="emit one combined CSV per participant (both plans coexist)")
    args = ap.parse_args()

    schedules = {}
    for cond in ("B1", "B2"):
        p = SCHEDULE_DIR / f"probes_{cond}.json"
        if not p.exists():
            print(f"ERROR: {p} missing — run Tools/make_probe_schedule.py --all first",
                  file=sys.stderr)
            return 1
        schedules[cond] = json.loads(p.read_text(encoding="utf-8"))

    args.out.mkdir(parents=True, exist_ok=True)
    rng = np.random.default_rng(args.seed)
    skip_pids = {7, 14} if args.n >= 10 else set()
    for i in range(1, args.n + 1):
        pid = f"P{i:02d}"
        t0 = EPOCH_MS + int(rng.integers(0, 10_000_000))
        skip_c = i in skip_pids
        if args.single_file:
            b = SessionBuilder(pid, rng, args.hz, t0)
            session_header(b, PLAN_A, i)
            b.emit("CONFIG", f"plan={PLAN_BC}")
            build_block_a(b, i, rng)
            b.block = ""
            b.emit("BREAK_START")
            b.advance(90)
            b.emit("BREAK_END")
            build_blocks_bc(b, i, rng, schedules, skip_c)
            close_session(b)
            path = write_rows(pid, "090000", b.rows, args.out)
            print(f"wrote {path} ({len(b.rows):,} rows, combined)")
        else:
            ba = SessionBuilder(pid, rng, args.hz, t0)
            session_header(ba, PLAN_A, i)
            build_block_a(ba, i, rng)
            close_session(ba)
            pa = write_rows(pid, "090000", ba.rows, args.out)
            bbc = SessionBuilder(pid, rng, args.hz, ba.t + 300_000)
            session_header(bbc, PLAN_BC, i)
            build_blocks_bc(bbc, i, rng, schedules, skip_c)
            close_session(bbc)
            pbc = write_rows(pid, "103000", bbc.rows, args.out)
            print(f"wrote {pa.name} ({len(ba.rows):,} rows) + {pbc.name} ({len(bbc.rows):,} rows)")
    write_questionnaires(args.n, args.out, rng, skip_pids)
    print(f"wrote {args.out / 'tlx_scores.csv'} and {args.out / 'ratings.csv'}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
