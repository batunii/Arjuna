#!/usr/bin/env python3
"""
Probe-schedule generator for user-study Block B (driving probe detection).

Emits the deterministic JSON schedule ProbeScheduler.cs plays back, per
Dissertation/testing-strategy-v2.md section 4.2:

  * 24 probes per condition: 12 central (< 10 deg eccentricity from the
    focus-window centre at az=0, el=0) and 12 peripheral (25-40 deg).
  * Inter-onset jitter uniform 6-12 s; the last onset lands at <= 216 s so the
    4-minute segment keeps ~24 s of slack for head-yaw deferrals.
  * Bands pseudo-randomly interleaved with no 3 identical in a row.
  * Elevations within +/-15 deg; peripheral probes balanced 6 left / 6 right.
  * Fully seeded: the same (condition, seed) always yields the same schedule.

Usage (from the repo root):
    python Tools/make_probe_schedule.py --condition B1 --seed 101
    python Tools/make_probe_schedule.py --condition B2 --seed 202
    python Tools/make_probe_schedule.py --all          # canonical B1+B2 pair

Output (default): Assets/StreamingAssets/StudySchedules/probes_<condition>.json

Angle convention matches the vignette shader: azimuth positive to the right,
elevation positive up, both in degrees relative to the fixed Block B
focus-window centre.
"""

import argparse
import json
import math
import random
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
DEFAULT_OUT_DIR = REPO_ROOT / "Assets/StreamingAssets/StudySchedules"

N_PROBES = 24
N_CENTRAL = 12
N_PERIPHERAL = 12
CENTRAL_ECC = (3.0, 9.0)        # deg; keep off the exact fixation point
PERIPHERAL_ECC = (25.0, 40.0)   # deg
EL_LIMIT = 15.0                 # deg, |elevation| ceiling for every probe
GAP_RANGE = (6.0, 12.0)         # s, uniform inter-onset jitter (first gap = delay from start)
MAX_SPAN_S = 216.0              # s, last onset must not exceed this
DURATION_MS = 300
CANONICAL = {"B1": 101, "B2": 202}


def band_sequence(rng: random.Random) -> list:
    """12 central + 12 peripheral, shuffled until no 3 identical run."""
    seq = ["central"] * N_CENTRAL + ["peripheral"] * N_PERIPHERAL
    while True:
        rng.shuffle(seq)
        if not any(seq[i] == seq[i + 1] == seq[i + 2] for i in range(len(seq) - 2)):
            return list(seq)


def onset_times(rng: random.Random) -> list:
    """24 cumulative onsets from uniform 6-12 s gaps, resampled until span <= 216 s."""
    while True:
        gaps = [rng.uniform(*GAP_RANGE) for _ in range(N_PROBES)]
        times = []
        t = 0.0
        for g in gaps:
            t += g
            times.append(round(t, 2))
        if times[-1] <= MAX_SPAN_S:
            return times


def central_position(rng: random.Random) -> tuple:
    ecc = rng.uniform(*CENTRAL_ECC)
    theta = rng.uniform(0.0, 2.0 * math.pi)
    az = ecc * math.cos(theta)
    el = ecc * math.sin(theta)
    return round(az, 2), round(el, 2)


def peripheral_position(rng: random.Random, side: int) -> tuple:
    """side: -1 left, +1 right. Eccentricity 25-40 deg with |el| <= 15 deg."""
    while True:
        ecc = rng.uniform(*PERIPHERAL_ECC)
        # |el| = ecc*|sin(theta)| <= EL_LIMIT bounds theta near the horizontal.
        max_sin = min(1.0, EL_LIMIT / ecc)
        theta = rng.uniform(-math.asin(max_sin), math.asin(max_sin))
        az = ecc * math.cos(theta) * side
        el = ecc * math.sin(theta)
        if abs(el) <= EL_LIMIT:
            return round(az, 2), round(el, 2)


def build_schedule(condition: str, seed: int) -> dict:
    rng = random.Random(seed)
    bands = band_sequence(rng)
    times = onset_times(rng)
    # Balanced peripheral sides: 6 left, 6 right, order shuffled.
    sides = [-1] * (N_PERIPHERAL // 2) + [1] * (N_PERIPHERAL // 2)
    rng.shuffle(sides)
    side_iter = iter(sides)

    probes = []
    for i, (band, t) in enumerate(zip(bands, times)):
        if band == "central":
            az, el = central_position(rng)
        else:
            az, el = peripheral_position(rng, next(side_iter))
        probes.append({
            "id": i,
            "t": t,
            "az_deg": az,
            "el_deg": el,
            "band": band,
            "duration_ms": DURATION_MS,
        })
    return {"version": 1, "condition": condition, "seed": seed, "probes": probes}


def sanity_check(schedule: dict) -> None:
    probes = schedule["probes"]
    assert len(probes) == N_PROBES
    assert sum(p["band"] == "central" for p in probes) == N_CENTRAL
    assert probes[-1]["t"] <= MAX_SPAN_S
    gaps = [probes[0]["t"]] + [b["t"] - a["t"] for a, b in zip(probes, probes[1:])]
    assert all(GAP_RANGE[0] - 0.01 <= g <= GAP_RANGE[1] + 0.01 for g in gaps), gaps
    for p in probes:
        ecc = math.hypot(p["az_deg"], p["el_deg"])
        assert abs(p["el_deg"]) <= EL_LIMIT + 0.01
        if p["band"] == "central":
            assert ecc < 10.0
        else:
            assert 25.0 - 0.01 <= ecc <= 40.0 + 0.01
    peri_az = [p["az_deg"] for p in probes if p["band"] == "peripheral"]
    assert sum(a < 0 for a in peri_az) == 6 and sum(a > 0 for a in peri_az) == 6
    for i in range(len(probes) - 2):
        assert not (probes[i]["band"] == probes[i + 1]["band"] == probes[i + 2]["band"])


def write_schedule(condition: str, seed: int, out_dir: Path) -> Path:
    schedule = build_schedule(condition, seed)
    sanity_check(schedule)
    out_dir.mkdir(parents=True, exist_ok=True)
    out = out_dir / f"probes_{condition}.json"
    out.write_text(json.dumps(schedule, indent=2) + "\n", encoding="utf-8")
    return out


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--condition", choices=["B1", "B2"], help="condition label for the schedule")
    ap.add_argument("--seed", type=int, help="RNG seed (canonical: B1=101, B2=202)")
    ap.add_argument("--all", action="store_true",
                    help="emit the canonical B1 (seed 101) and B2 (seed 202) schedules")
    ap.add_argument("--out-dir", type=Path, default=DEFAULT_OUT_DIR)
    args = ap.parse_args()

    if args.all:
        jobs = list(CANONICAL.items())
    elif args.condition and args.seed is not None:
        jobs = [(args.condition, args.seed)]
    else:
        ap.error("either --all, or both --condition and --seed")

    for condition, seed in jobs:
        out = write_schedule(condition, seed, args.out_dir)
        span = json.loads(out.read_text(encoding="utf-8"))["probes"][-1]["t"]
        print(f"wrote {out}  (seed {seed}, last onset {span:.1f} s)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
