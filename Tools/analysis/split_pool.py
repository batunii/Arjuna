#!/usr/bin/env python3
"""
Split an authored point pool into two counterbalance sets A/B that are matched
on the properties that drive difficulty, for the DR driving study.

Method (see Dissertation/probe-target-design.md sec 4c/4d):
  - STRATIFY on hard categorical keys (class x region): each stratum is split
    ~evenly, so A and B match on class and region by construction.
  - OPTIMISE the within-stratum assignment over many seeded random draws to also
    equalise the continuous covariates (duration, onset time, eccentricity),
    scored as summed |meanA-meanB| normalised by each covariate's pool SD.
  - Seeded => reproducible; A/B are interchangeable (per-participant rotation
    happens at runtime), so which set is "A" carries no meaning.

The pool is presented with SIMULTANEOUS probes (a ring per active target), so
there is no within-set temporal-overlap constraint. (If the study switches to
one-probe-at-a-time, that constraint would force dropping overlapping targets —
not handled here.)

Practice targets: --practice takes point_ids to set aside as set "P". They are
excluded from the A/B optimisation and written with set=P; the presenter shows
them in EVERY run (both sets, both conditions) as unscored warm-up clicks.

CLI:
    python Tools/analysis/split_pool.py <pool.csv> [--seed-tries 20000] [--out split.csv] [--practice 0,58]
Input columns: point_id,cls,kind,t_start,t_end,duration_s,az_mid_deg,el_mid_deg,
               eccentricity_deg,box_height_deg[,region]
"""

import argparse, csv, random, statistics, sys
from pathlib import Path
from collections import Counter, defaultdict

STRATA = ("kind", "region")
COVARS = ("duration_s", "t_start", "eccentricity_deg")  # balanced (soft)
W_OVERLAP = 0.05  # penalty per same-set temporally-overlapping pair: distributes clusters across
                  # sets so each set has fewer simultaneous rings on screen (lower per-set concurrency)


def load(path: Path):
    rows = list(csv.DictReader(open(path, newline="")))
    for r in rows:
        for c in ("t_start", "t_end", "duration_s", "az_mid_deg", "el_mid_deg", "eccentricity_deg"):
            r[c] = float(r[c])
        if "region" not in r or r["region"] == "":
            r["region"] = "centre" if abs(r["az_mid_deg"]) <= 25 and abs(r["el_mid_deg"]) <= 15 else "periphery"
    return rows


def imbalance(rows, assign, sds, overlaps):
    A = [r for r in rows if assign[r["point_id"]] == "A"]
    B = [r for r in rows if assign[r["point_id"]] == "B"]
    if not A or not B:
        return 1e9
    score = abs(len(A) - len(B)) * 0.5  # keep set sizes close
    for c in COVARS:
        ma = statistics.fmean(r[c] for r in A)
        mb = statistics.fmean(r[c] for r in B)
        score += abs(ma - mb) / (sds[c] or 1.0)
    # distribute overlapping clusters: penalise pairs that overlap in time AND land in the same set
    score += W_OVERLAP * sum(1 for a, b in overlaps if assign[a] == assign[b])
    return score


def overlapping_pairs(rows):
    ov = []
    for i in range(len(rows)):
        si, ei = float(rows[i]["t_start"]), float(rows[i]["t_end"])
        for j in range(i + 1, len(rows)):
            if si < float(rows[j]["t_end"]) and float(rows[j]["t_start"]) < ei:
                ov.append((rows[i]["point_id"], rows[j]["point_id"]))
    return ov


def split(rows, tries, seed=12345):
    sds = {c: (statistics.pstdev(r[c] for r in rows) or 1.0) for c in COVARS}
    overlaps = overlapping_pairs(rows)
    strata = defaultdict(list)
    for r in rows:
        strata[tuple(r[s] for s in STRATA)].append(r)
    rng = random.Random(seed)
    best, best_score = None, 1e18
    for _ in range(tries):
        assign = {}
        for members in strata.values():
            idx = list(range(len(members)))
            rng.shuffle(idx)
            half = len(members) // 2
            # random tie-break for the odd one so both sets sometimes get it
            extra = rng.random() < 0.5
            for k, i in enumerate(idx):
                assign[members[i]["point_id"]] = "A" if k < half + (extra and (len(members) % 2)) else "B"
        s = imbalance(rows, assign, sds, overlaps)
        if s < best_score:
            best_score, best = s, dict(assign)
    return best, best_score, sds


def report(rows, assign):
    for setname in ("A", "B"):
        sub = [r for r in rows if assign[r["point_id"]] == setname]
        byc = Counter((r["kind"], r["region"]) for r in sub)
        print(f"\nSet {setname}: n={len(sub)}")
        print("  class x region:", dict(byc))
        for c in COVARS:
            vals = [r[c] for r in sub]
            print(f"  {c}: mean {statistics.fmean(vals):.2f}, sd {statistics.pstdev(vals):.2f}")


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("pool", type=Path)
    ap.add_argument("--seed-tries", type=int, default=20000)
    ap.add_argument("--out", type=Path, default=None)
    ap.add_argument("--practice", type=str, default="",
                    help="comma-separated point_ids set aside as set P (shown every run, unscored)")
    args = ap.parse_args()

    practice_ids = {s.strip() for s in args.practice.split(",") if s.strip()}
    all_rows = load(args.pool)
    practice = [r for r in all_rows if r["point_id"] in practice_ids]
    rows = [r for r in all_rows if r["point_id"] not in practice_ids]
    missing = practice_ids - {r["point_id"] for r in practice}
    if missing:
        sys.exit(f"--practice ids not in pool: {sorted(missing)}")
    if practice:
        print("practice (set P):", [(r["point_id"], r["kind"], f"t={r['t_start']:.1f}") for r in practice])
    print(f"pool: {len(rows)}  class x region: {dict(Counter((r['kind'], r['region']) for r in rows))}")
    assign, score, _ = split(rows, args.seed_tries)
    for r in practice:
        assign[r["point_id"]] = "P"
    print(f"\nbest imbalance score: {score:.3f}  (lower = better matched)")
    report(rows, assign)

    if args.out:
        with open(args.out, "w", newline="") as fh:
            w = csv.DictWriter(fh, fieldnames=list(all_rows[0].keys()) + ["set"])
            w.writeheader()
            for r in all_rows:
                out = dict(r); out["set"] = assign[r["point_id"]]; w.writerow(out)
        print(f"\nwrote {args.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
