#!/usr/bin/env python3
"""
Merge raw authoring passes into the target pool for the DR driving study
(pipeline stage 2 — see Dissertation/authored-pool-pipeline.md).

- Merges every authored_points_*.csv in the raw directory.
- Dedups by detection identity (cls, t_start, t_end) — re-marking the same
  object across passes never double-counts.
- Applies a uniform minimum-duration filter (default >=1.0 s). Note: the bake's
  lifetimes are quantized to 0.5 s steps, so sub-1 s means exactly 0.5 s blips.
- Tags region from the locked study window: centre if |az|<=25 deg and
  |el|<=15 deg, else periphery (same rule as split_pool.py).
- point_id is stable across rebuilds: rows already present in --existing keep
  their id; genuinely new detections are appended with the next free ids, in
  (t_start, cls) order. This keeps prior authored_results_*.csv linkable.

CLI:
    python Tools/analysis/merge_pool.py Dissertation/authored/raw \
        --existing Dissertation/authored/pool_uniform_1s.csv \
        --min-duration 1.0 --out Dissertation/authored/pool_uniform_1s.csv
"""

import argparse, csv, sys
from pathlib import Path
from collections import Counter

FIELDS = ("point_id", "cls", "kind", "t_start", "t_end", "duration_s",
          "az_mid_deg", "el_mid_deg", "eccentricity_deg", "box_height_deg", "region")


def key(r):
    return (r["cls"], r["t_start"], r["t_end"])


def region(r):
    return "centre" if abs(float(r["az_mid_deg"])) <= 25 and abs(float(r["el_mid_deg"])) <= 15 else "periphery"


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("raw_dir", type=Path, help="directory containing authored_points_*.csv")
    ap.add_argument("--existing", type=Path, default=None,
                    help="previous pool CSV whose point_ids must be preserved")
    ap.add_argument("--min-duration", type=float, default=1.0)
    ap.add_argument("--out", type=Path, required=True)
    args = ap.parse_args()

    files = sorted(args.raw_dir.glob("authored_points_*.csv"))
    if not files:
        sys.exit(f"no authored_points_*.csv in {args.raw_dir}")

    merged = {}
    for f in files:
        for r in csv.DictReader(open(f, newline="")):
            merged.setdefault(key(r), r)
    print(f"{len(files)} files -> {len(merged)} unique detections")

    kept = {k: r for k, r in merged.items() if float(r["duration_s"]) >= args.min_duration}
    print(f">= {args.min_duration:.1f} s filter: {len(kept)} kept, {len(merged) - len(kept)} dropped")

    ids, next_id = {}, 0
    if args.existing and args.existing.exists():
        for r in csv.DictReader(open(args.existing, newline="")):
            ids[key(r)] = int(r["point_id"])
        next_id = max(ids.values()) + 1 if ids else 0
        stale = [k for k in ids if k not in kept]
        if stale:
            print(f"note: {len(stale)} existing pool rows no longer pass the filter (ids kept reserved)")

    new_keys = sorted((k for k in kept if k not in ids),
                      key=lambda k: (float(kept[k]["t_start"]), kept[k]["cls"]))
    for k in new_keys:
        ids[k] = next_id
        next_id += 1
    print(f"ids: {len(kept) - len(new_keys)} preserved, {len(new_keys)} new appended")

    rows = sorted(kept.values(), key=lambda r: ids[key(r)])
    with open(args.out, "w", newline="") as fh:
        w = csv.DictWriter(fh, fieldnames=list(FIELDS))
        w.writeheader()
        for r in rows:
            out = {f: r.get(f, "") for f in FIELDS}
            out["point_id"] = ids[key(r)]
            out["region"] = region(r)
            w.writerow(out)

    print(f"wrote {args.out}: {len(rows)} targets, "
          f"kind x region: {dict(Counter((r['kind'], region(r)) for r in rows))}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
