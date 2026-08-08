#!/usr/bin/env python3
"""T1 — per-mode frame cost from the OVR Metrics Tool captures of 2026-08-07.

Segments follow Dissertation/authored/raw/technical-T1-T3-20260807/session-log.md exactly
(wall-clock switch times called out during the run). The CSV `Time Stamp` column is
milliseconds since capture start; capture start is the file-name suffix (local time).
A 5 s guard is trimmed at both ends of every segment so transition frames (mode formation,
the switch itself) do not contaminate a segment's statistics.

Pre-stated criteria (HANDOFF-next-session.md, written before the data was seen):
pass for a study configuration = 95th-percentile app GPU frame time within the 13.88 ms
72 Hz budget AND stale-frame rate < 1%. Blur is an interpretation band, not pass/fail.

Usage:
    python Tools/analysis/t1_framecost.py
"""
import csv
import os
from pathlib import Path

import numpy as np

ROOT = Path(__file__).resolve().parents[2]
RAW = ROOT / "Dissertation/authored/raw/technical-T1-T3-20260807"
FIGDIR = ROOT / "Dissertation/25377738-dissertation-submission/content/figures"

BUDGET_MS = 13.88          # 72 Hz frame budget
GUARD_S = 5                # trimmed from both ends of each segment

# (csv path, capture start as seconds into the day, [(config, seg start wall s, seg end wall s)])
def hms(h, m, s):
    return h * 3600 + m * 60 + s

PASSTHROUGH = (
    RAW / "ovr-metrics-block2-passthrough/CapturedMetrics/com.samples.passthroughcamera#UnityPlayerGameActivity-20260807_144005.csv",
    hms(14, 40, 5),
    [
        ("Effects off (passthrough)", hms(14, 40, 56), hms(14, 42, 39)),
        ("Soft Dark",                 hms(14, 42, 39), hms(14, 44, 8)),
        ("Hard Dark",                 hms(14, 44, 8),  hms(14, 46, 46)),
        ("Blur",                      hms(14, 46, 46), hms(14, 49, 31)),
    ],
)
VIDEO = (
    RAW / "ovr-metrics-block3-video/CapturedMetrics/com.samples.passthroughcamera#UnityPlayerGameActivity-20260807_150156.csv",
    hms(15, 1, 56),
    [
        ("SignPop (video scene)",     hms(15, 2, 20),  hms(15, 6, 35)),
        ("Effects off (video scene)", hms(15, 6, 35),  hms(15, 8, 41)),
    ],
)


def load(path):
    with open(path, newline="", encoding="utf-8") as fh:
        return list(csv.DictReader(fh))


def segment_stats(rows, cap_start, seg_start, seg_end):
    lo = seg_start - cap_start + GUARD_S
    hi = seg_end - cap_start - GUARD_S
    seg = [r for r in rows if lo <= float(r["Time Stamp"]) / 1000 <= hi]
    gpu_ms = np.array([float(r["app_gpu_time_microseconds"]) for r in seg]) / 1000
    stale = np.array([float(r["stale_frame_count"]) for r in seg])
    fps = np.array([float(r["average_frame_rate"]) for r in seg])
    cores = [np.array([float(r[f"cpu_utilization_percentage_core{i}"]) for r in seg])
             for i in range(8)]
    worst_core = np.max([c.mean() for c in cores])
    gpu_u = np.array([float(r["gpu_utilization_percentage"]) for r in seg]).mean()
    # stale_frame_count is per 1 s reporting interval; display runs at 72 Hz
    stale_rate = stale.sum() / (len(seg) * 72) * 100
    return {
        "n": len(seg),
        "gpu_p50": np.percentile(gpu_ms, 50),
        "gpu_p95": np.percentile(gpu_ms, 95),
        "gpu_max": gpu_ms.max(),
        "stale_rate": stale_rate,
        "stale_total": int(stale.sum()),
        "fps": fps.mean(),
        "worst_core": worst_core,
        "gpu_u": gpu_u,
    }


def main():
    results = []
    for path, cap_start, segments in (PASSTHROUGH, VIDEO):
        rows = load(path)
        for config, s, e in segments:
            st = segment_stats(rows, cap_start, s, e)
            st["config"] = config
            st["dur"] = e - s
            results.append(st)

    print(f"{'config':<28}{'dur':>5}{'n':>5}{'p50':>7}{'p95':>7}{'max':>7}"
          f"{'stale%':>8}{'fps':>6}{'cpuW%':>7}{'gpu%':>6}")
    for r in results:
        print(f"{r['config']:<28}{r['dur']:>4}s{r['n']:>5}{r['gpu_p50']:>7.2f}"
              f"{r['gpu_p95']:>7.2f}{r['gpu_max']:>7.2f}{r['stale_rate']:>8.3f}"
              f"{r['fps']:>6.1f}{r['worst_core']:>7.1f}{r['gpu_u']:>6.1f}")
    print(f"\nbudget: {BUDGET_MS} ms at 72 Hz; pass = p95 within budget AND stale < 1%")

    # ---- figure ----
    import matplotlib
    matplotlib.use("Agg")
    import matplotlib.pyplot as plt
    plt.rcParams.update({"font.family": "serif", "font.size": 11,
                         "figure.dpi": 300, "savefig.dpi": 300,
                         "savefig.bbox": "tight", "savefig.pad_inches": 0.15})

    labels = ["Effects off\n(passthrough)", "Soft Dark", "Hard Dark", "Blur",
              "SignPop\n(video)", "Effects off\n(video)"]
    order = [0, 1, 2, 3, 4, 5]
    p50 = [results[i]["gpu_p50"] for i in order]
    p95 = [results[i]["gpu_p95"] for i in order]
    stale = [results[i]["stale_rate"] for i in order]

    x = np.arange(len(labels))
    w = 0.38
    fig, ax = plt.subplots(figsize=(8, 4))
    ax.bar(x - w / 2, p50, w, color="#90caf9", edgecolor="#1565c0", label="Median", zorder=3)
    ax.bar(x + w / 2, p95, w, color="#1565c0", edgecolor="#0d47a1",
           label="95th percentile", zorder=3)
    ax.axhline(BUDGET_MS, color="#d32f2f", linewidth=1.8, linestyle="--", zorder=4)
    ax.annotate(f"72 Hz budget = {BUDGET_MS} ms", (len(labels) - 0.4, BUDGET_MS + 0.3),
                ha="right", fontsize=9, color="#d32f2f")
    for xi, (a, b, s) in enumerate(zip(p50, p95, stale)):
        ax.text(xi + w / 2, b + 0.25, f"{b:.1f}", ha="center", fontsize=8, color="#0d47a1")
    ax.set_xticks(x)
    ax.set_xticklabels(labels, fontsize=9)
    ax.set_ylabel("App GPU frame time (ms)")
    ax.set_ylim(0, 16)
    ax.set_title("Per-mode GPU frame cost against the 72 Hz budget (T1)")
    ax.legend(loc="upper left", fontsize=9)
    ax.grid(axis="y", alpha=0.3, zorder=0)
    out = FIGDIR / "fig_t1_framecost.png"
    fig.savefig(out)
    print(f"figure -> {out}")


if __name__ == "__main__":
    main()
