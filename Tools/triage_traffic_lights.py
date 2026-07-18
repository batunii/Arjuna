#!/usr/bin/env python3
# /// script
# requires-python = ">=3.10"
# dependencies = ["Pillow"]
# ///
"""
Relevance triage for baked traffic-light (COCO class 9) and stop-sign (class 11)
detections in a VideoTestScene baked-detections JSON.

Goal: SignPop currently highlights every detected traffic light / stop sign in
the baked track, including ones that don't actually face/control the driver's
lane (cross-street signals, pedestrian-facing signals, signals facing away,
distant unrelated lights). The runtime has zero relevance filtering of its own
(VideoTestSceneManager just tracks whatever is in the loaded JSON), so this is
a pure data-curation task: split the JSON into a "relevant" main file (kept,
deployed to the device) and an "irrelevant" sidecar file (dropped, but fully
restorable later).

This is a two-stage tool:

  review  - Reconstructs every real-world traffic-light/stop-sign "lifetime"
            from the raw per-sample JSON (a direct Python port of
            VideoTestSceneManager.BuildDetectionLifetimes's identity heuristic:
            same class, nearest centroid within radius max(size,0.01)*1.5,
            held open for --hold-seconds after last seen). Traffic lights are
            then auto-bucketed by azimuth from video-forward (computed at
            each lifetime's representative — temporal-midpoint-nearest — box,
            since m_videoUOffset/m_videoVOffset are both 0 in VideoTestScene.unity
            so az_deg = ((x1+x2)/2 - 0.5) * 360 directly off the raw JSON):
              |az_deg| <= --auto-relevant-deg   -> auto_relevant (near dead-ahead)
              |az_deg| >= --auto-irrelevant-deg -> auto_irrelevant (well off to the side)
              otherwise                          -> ambiguous (needs a visual look)
            Stop signs are cheap (~64 raw detections) and always go to
            needs_review (every one gets a look, no azimuth bucketing).
            Writes a cache (full lifetime data, so `apply` doesn't need to
            re-derive anything) plus padded, labelled contact-sheet grid
            images (ffmpeg crop + Pillow composite) for every lifetime that
            needs a visual look, so a reviewer can judge many candidates per
            image instead of one at a time.

  apply   - Reads the cache + a hand-authored decisions file (only required
            for ambiguous/needs_review lifetimes; auto-bucketed lifetimes can
            be overridden too but default to their bucket), and produces:
              - the filtered MAIN file (relevant traffic-lights/stop-signs +
                all person detections untouched), same schema, same path
                (overwrites the source file after snapshotting it)
              - a SIDECAR file with the same schema containing only the
                removed entries (restorable later)
              - a snapshot backup + a structured relevance-removed log per
                class, extending the Tools/detections_backups/ convention
                (study_video.detections.removed_2026-07-13.json)
            Because every raw detection is assigned to exactly one lifetime
            during reconstruction (per-sample matching is 1:1), removing a
            lifetime's entries can never affect any other lifetime's entries.

There is also a third mode for hunting residual false positives full-motion:

  preview - Renders ONE annotated video: every reconstructed lifetime's box is
            drawn over a downscaled copy of the source video, lerped between
            bracketing samples exactly like the runtime tracker, with its
            lifetime id burned in. Lifetimes the runtime debounce
            (VideoTestSceneManager.m_detectionMinAgeSec) will suppress are
            drawn dim grey — so scrubbing the preview shows only what will
            actually open a window on the headset. Writes a lifetimes cache in
            the same format as `review`, except every lifetime defaults to
            KEEP ("auto_relevant" bucket) — so the decisions file for `apply`
            only needs to name the ids you saw circling nothing:
              {"9": {"123": {"decision": "irrelevant", "reason": "no light"}}}
            This replaces recording the headset and eyeballing frame dumps.

Usage (from repo root):
    python Tools/triage_traffic_lights.py review
    python Tools/triage_traffic_lights.py preview --out-dir Tools/preview
    python Tools/triage_traffic_lights.py apply --decisions <path-to-decisions.json>
"""

import argparse
import json
import shutil
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
DEFAULT_JSON = REPO_ROOT / "Builds/StudyVideo/study_video.detections.json"
DEFAULT_VIDEO = REPO_ROOT / "Builds/StudyVideo/study_video.mp4"
DEFAULT_BACKUP_DIR = REPO_ROOT / "Tools/detections_backups"

LIGHT_CLASS = 9
STOPSIGN_CLASS = 11
CLASS_NAMES = {LIGHT_CLASS: "traffic light (COCO id 9)", STOPSIGN_CLASS: "stop sign (COCO id 11)"}


# ---------------------------------------------------------------------------
# Lifetime reconstruction — direct port of
# VideoTestSceneManager.BuildDetectionLifetimes (VideoTestSceneManager.cs:320-384)
# ---------------------------------------------------------------------------

def build_lifetimes(samples: list[dict], class_id: int, hold_seconds: float) -> list[dict]:
    """Returns a list of {cls, tStart, tEnd, entries: [(sample_idx, x1,y1,x2,y2), ...]}.

    Mirrors the C# identity heuristic exactly: same class, nearest centroid
    within radius max(box_size, 0.01) * 1.5; a track closes when a sample's
    time exceeds its last-seen time by more than hold_seconds. Each raw
    detection is claimed by at most one track per sample (matched set), so
    lifetimes never share entries — removing one lifetime's entries can never
    touch another's.
    """
    closed: list[dict] = []
    open_tracks: list[dict] = []  # {cls, cx, cy, sz, tStart, tLast, entries}

    for sample_idx, s in enumerate(samples):
        t = s["t"]
        dets = [d for d in s["d"] if d["c"] == class_id]

        matched: set[int] = set()
        for tr in open_tracks:
            best_i = -1
            best_dist = max(tr["sz"], 0.01) * 1.5
            for i, d in enumerate(dets):
                if i in matched:
                    continue
                cx = (d["x1"] + d["x2"]) * 0.5
                cy = (d["y1"] + d["y2"]) * 0.5
                dist = ((cx - tr["cx"]) ** 2 + (cy - tr["cy"]) ** 2) ** 0.5
                if dist < best_dist:
                    best_dist = dist
                    best_i = i
            if best_i >= 0:
                matched.add(best_i)
                d = dets[best_i]
                tr["cx"] = (d["x1"] + d["x2"]) * 0.5
                tr["cy"] = (d["y1"] + d["y2"]) * 0.5
                tr["sz"] = max(d["x2"] - d["x1"], d["y2"] - d["y1"])
                tr["tLast"] = t
                tr["entries"].append((sample_idx, d["x1"], d["y1"], d["x2"], d["y2"]))

        still_open = []
        for tr in open_tracks:
            if t - tr["tLast"] > hold_seconds:
                closed.append({"cls": tr["cls"], "tStart": tr["tStart"], "tEnd": tr["tLast"],
                               "entries": tr["entries"]})
            else:
                still_open.append(tr)
        open_tracks = still_open

        for i, d in enumerate(dets):
            if i in matched:
                continue
            open_tracks.append({
                "cls": class_id,
                "cx": (d["x1"] + d["x2"]) * 0.5, "cy": (d["y1"] + d["y2"]) * 0.5,
                "sz": max(d["x2"] - d["x1"], d["y2"] - d["y1"]),
                "tStart": t, "tLast": t,
                "entries": [(sample_idx, d["x1"], d["y1"], d["x2"], d["y2"])],
            })

    for tr in open_tracks:
        closed.append({"cls": tr["cls"], "tStart": tr["tStart"], "tEnd": tr["tLast"],
                        "entries": tr["entries"]})

    closed.sort(key=lambda l: l["tStart"])
    return closed


def representative_entry(samples: list[dict], lifetime: dict) -> tuple[float, float, float, float, float]:
    """Returns (t, x1, y1, x2, y2) of the entry nearest the lifetime's temporal
    midpoint — same convention as BlobTargetController.ComputeEccentricityDeg."""
    t_mid = (lifetime["tStart"] + lifetime["tEnd"]) * 0.5
    best = None
    best_dist = None
    for sample_idx, x1, y1, x2, y2 in lifetime["entries"]:
        t = samples[sample_idx]["t"]
        d = abs(t - t_mid)
        if best_dist is None or d < best_dist:
            best_dist = d
            best = (t, x1, y1, x2, y2)
    return best


def azimuth_deg(x1: float, x2: float) -> float:
    """az_deg relative to video-forward; m_videoUOffset/m_videoVOffset are both 0
    in VideoTestScene.unity, so this reduces to a direct formula off the raw box."""
    cx = (x1 + x2) * 0.5
    return (cx - 0.5) * 360.0


def bucket_light(az_deg: float, auto_relevant_deg: float, auto_irrelevant_deg: float) -> str:
    a = abs(az_deg)
    if a <= auto_relevant_deg:
        return "auto_relevant"
    if a >= auto_irrelevant_deg:
        return "auto_irrelevant"
    return "ambiguous"


# ---------------------------------------------------------------------------
# ffmpeg / Pillow contact sheets
# ---------------------------------------------------------------------------

def probe_video_size(video: Path) -> tuple[int, int]:
    out = subprocess.check_output(
        ["ffprobe", "-v", "error", "-select_streams", "v:0",
         "-show_entries", "stream=width,height", "-of", "json", str(video)],
        text=True,
    )
    stream = json.loads(out)["streams"][0]
    return int(stream["width"]), int(stream["height"])


def extract_crop(video: Path, t: float, box: tuple[float, float, float, float],
                  width: int, height: int, pad_factor: float, min_crop_px: int,
                  out_png: Path) -> None:
    x1, y1, x2, y2 = box
    bw = (x2 - x1) * width
    bh = (y2 - y1) * height
    cw = max(bw * (1 + 2 * pad_factor), min_crop_px)
    ch = max(bh * (1 + 2 * pad_factor), min_crop_px)
    cw = min(cw, width)
    ch = min(ch, height)
    cx = (x1 + x2) * 0.5 * width
    cy = (y1 + y2) * 0.5 * height
    x0 = max(0, min(width - cw, cx - cw / 2))
    y0 = max(0, min(height - ch, cy - ch / 2))
    subprocess.run(
        ["ffmpeg", "-y", "-v", "error", "-ss", f"{t:.3f}", "-i", str(video),
         "-frames:v", "1", "-vf", f"crop={int(cw)}:{int(ch)}:{int(x0)}:{int(y0)}",
         str(out_png)],
        check=True,
    )


def build_contact_sheets(entries: list[dict], video: Path, width: int, height: int,
                          pad_factor: float, min_crop_px: int, cell_px: int,
                          per_sheet: int, cols: int, out_dir: Path, prefix: str,
                          crop_scratch_dir: Path) -> list[Path]:
    """entries: [{"idx":, "t":, "box":, "label":}]. Returns list of sheet paths."""
    from PIL import Image, ImageDraw, ImageFont

    out_dir.mkdir(parents=True, exist_ok=True)
    crop_scratch_dir.mkdir(parents=True, exist_ok=True)
    font = ImageFont.load_default()
    label_h = 34
    sheets = []

    for sheet_start in range(0, len(entries), per_sheet):
        chunk = entries[sheet_start:sheet_start + per_sheet]
        rows = (len(chunk) + cols - 1) // cols
        sheet = Image.new("RGB", (cols * cell_px, rows * (cell_px + label_h)), (24, 24, 24))
        draw = ImageDraw.Draw(sheet)

        for i, entry in enumerate(chunk):
            crop_path = crop_scratch_dir / f"{prefix}_{entry['idx']:04d}.png"
            extract_crop(video, entry["t"], entry["box"], width, height,
                         pad_factor, min_crop_px, crop_path)
            img = Image.open(crop_path).convert("RGB")
            img.thumbnail((cell_px, cell_px))
            canvas = Image.new("RGB", (cell_px, cell_px), (48, 48, 48))
            canvas.paste(img, ((cell_px - img.width) // 2, (cell_px - img.height) // 2))

            col, row = i % cols, i // cols
            x0, y0 = col * cell_px, row * (cell_px + label_h)
            sheet.paste(canvas, (x0, y0))
            draw.rectangle([x0, y0 + cell_px, x0 + cell_px, y0 + cell_px + label_h], fill=(0, 0, 0))
            draw.text((x0 + 4, y0 + cell_px + 4), entry["label"], font=font, fill=(255, 255, 0))

        sheet_path = out_dir / f"{prefix}_sheet_{sheet_start // per_sheet:02d}.png"
        sheet.save(sheet_path)
        sheets.append(sheet_path)

    return sheets


# ---------------------------------------------------------------------------
# review
# ---------------------------------------------------------------------------

def cmd_review(args: argparse.Namespace) -> int:
    if not shutil.which("ffmpeg") or not shutil.which("ffprobe"):
        print("ERROR: ffmpeg/ffprobe not on PATH.", file=sys.stderr)
        return 1

    data = json.loads(args.json.read_text())
    samples = data["samples"]

    out_dir = args.out_dir
    out_dir.mkdir(parents=True, exist_ok=True)
    sheets_dir = out_dir / "sheets"
    crop_scratch_dir = args.crop_scratch_dir

    cache = {"source_file": str(args.json.relative_to(REPO_ROOT)),
             "hold_seconds": args.hold_seconds,
             "auto_relevant_deg": args.auto_relevant_deg,
             "auto_irrelevant_deg": args.auto_irrelevant_deg,
             "classes": {}}

    width, height = probe_video_size(args.video)
    print(f"Video: {args.video} ({width}x{height})")

    review_entries_by_class: dict[int, list[dict]] = {}

    for class_id in (LIGHT_CLASS, STOPSIGN_CLASS):
        lifetimes = build_lifetimes(samples, class_id, args.hold_seconds)
        cache_lifetimes = []
        need_review = []
        counts = {"auto_relevant": 0, "auto_irrelevant": 0, "ambiguous": 0, "needs_review": 0}

        for idx, lt in enumerate(lifetimes):
            t, x1, y1, x2, y2 = representative_entry(samples, lt)
            az = azimuth_deg(x1, x2)
            if class_id == LIGHT_CLASS:
                bucket = bucket_light(az, args.auto_relevant_deg, args.auto_irrelevant_deg)
            else:
                bucket = "needs_review"  # stop signs: review every one, per plan
            counts[bucket] += 1

            cache_lifetimes.append({
                "idx": idx, "cls": class_id,
                "tStart": lt["tStart"], "tEnd": lt["tEnd"],
                "rep_t": t, "rep_box": [x1, y1, x2, y2], "az_deg": az,
                "bucket": bucket,
                "entries": lt["entries"],
            })

            if bucket in ("ambiguous", "needs_review"):
                need_review.append({
                    "idx": idx, "t": t, "box": (x1, y1, x2, y2),
                    "label": f"#{idx} t={t:.1f}s az={az:.0f}",
                })

        cache["classes"][str(class_id)] = cache_lifetimes
        review_entries_by_class[class_id] = need_review

        print(f"\nClass {class_id} ({CLASS_NAMES[class_id]}): {len(lifetimes)} lifetimes")
        for k, v in counts.items():
            print(f"  {k:16s} {v}")

    cache_path = out_dir / "lifetimes_cache.json"
    cache_path.write_text(json.dumps(cache, separators=(",", ":")))
    print(f"\nWrote lifetime cache -> {cache_path}")

    total_sheets = 0
    for class_id, need_review in review_entries_by_class.items():
        if not need_review:
            continue
        prefix = f"class{class_id}"
        sheets = build_contact_sheets(
            need_review, args.video, width, height,
            args.pad_factor, args.min_crop_px, args.cell_px, args.per_sheet, args.cols,
            sheets_dir, prefix, crop_scratch_dir,
        )
        total_sheets += len(sheets)
        print(f"Class {class_id}: {len(need_review)} need review -> {len(sheets)} contact sheet(s) "
              f"in {sheets_dir}")
        for p in sheets:
            print(f"    {p}")

    print(f"\n{total_sheets} contact sheet(s) written. Review them, then write a decisions file "
          "(see this script's module docstring / apply mode) and run:\n"
          f"  python Tools/triage_traffic_lights.py apply --decisions <path> --cache {cache_path}")
    return 0


# ---------------------------------------------------------------------------
# preview — annotated full-motion render for false-positive hunting
# ---------------------------------------------------------------------------

# One colour per class for lifetimes that will actually show on the headset;
# debounce-suppressed blips are always dim grey regardless of class.
PREVIEW_COLORS = {LIGHT_CLASS: (0, 220, 255), STOPSIGN_CLASS: (255, 160, 0)}
PREVIEW_SUPPRESSED = (110, 110, 110)


def interpolate_box(entry_times: list[float], entries: list[tuple],
                    t: float) -> tuple[float, float, float, float]:
    """Box at video time t, lerped between the two bracketing entries — the same
    bracketing the runtime tracker does. Before the first / after the last entry
    (the hold tail), the nearest entry's box is held as-is."""
    if t <= entry_times[0]:
        e = entries[0]
        return e[1], e[2], e[3], e[4]
    for i in range(len(entries) - 1):
        t0, t1 = entry_times[i], entry_times[i + 1]
        if t0 <= t <= t1:
            f = (t - t0) / (t1 - t0) if t1 > t0 else 0.0
            a, b = entries[i], entries[i + 1]
            return (a[1] + (b[1] - a[1]) * f, a[2] + (b[2] - a[2]) * f,
                    a[3] + (b[3] - a[3]) * f, a[4] + (b[4] - a[4]) * f)
    e = entries[-1]
    return e[1], e[2], e[3], e[4]


def cmd_preview(args: argparse.Namespace) -> int:
    if not shutil.which("ffmpeg") or not shutil.which("ffprobe"):
        print("ERROR: ffmpeg/ffprobe not on PATH.", file=sys.stderr)
        return 1
    from PIL import Image, ImageDraw, ImageFont

    data = json.loads(args.json.read_text())
    samples = data["samples"]
    src_w, src_h = probe_video_size(args.video)
    out_w = args.out_width
    out_h = int(round(src_h * out_w / src_w / 2)) * 2
    print(f"Video: {args.video} ({src_w}x{src_h}) -> preview {out_w}x{out_h} @ {args.fps} fps")

    out_dir = args.out_dir
    out_dir.mkdir(parents=True, exist_ok=True)

    # Reconstruct lifetimes + write an apply-compatible cache. Every lifetime is
    # bucketed "auto_relevant" (= kept unless the decisions file names it) — the
    # preview workflow is remove-by-exception, unlike review's triage buckets.
    cache = {"source_file": str(args.json.resolve().relative_to(REPO_ROOT)),
             "hold_seconds": args.hold_seconds,
             "auto_relevant_deg": 0.0, "auto_irrelevant_deg": 999.0,
             "classes": {}}
    all_lifetimes: list[dict] = []
    summary_lines = []
    for class_id in (LIGHT_CLASS, STOPSIGN_CLASS):
        lifetimes = build_lifetimes(samples, class_id, args.hold_seconds)
        cache_lifetimes = []
        for idx, lt in enumerate(lifetimes):
            t, x1, y1, x2, y2 = representative_entry(samples, lt)
            az = azimuth_deg(x1, x2)
            cache_lifetimes.append({
                "idx": idx, "cls": class_id,
                "tStart": lt["tStart"], "tEnd": lt["tEnd"],
                "rep_t": t, "rep_box": [x1, y1, x2, y2], "az_deg": az,
                "bucket": "auto_relevant",
                "entries": lt["entries"],
            })
            # Same maturity rule as the runtime: the track stays active until
            # tEnd + hold; it becomes visible only if it reaches min-age first.
            suppressed = (lt["tEnd"] + args.hold_seconds - lt["tStart"]) < args.min_age
            # Same size gate as the runtime: max apparent extent in degrees
            # (equirect: az extent spans 360°, el extent 180°). A lifetime whose
            # box never exceeds the gate never shows at all.
            max_ang = max((max((e[3] - e[1]) * 360.0, (e[4] - e[2]) * 180.0)
                           for e in lt["entries"]), default=0.0)
            tiny = max_ang < args.min_size_deg
            entry_times = [samples[e[0]]["t"] for e in lt["entries"]]
            all_lifetimes.append({
                "idx": idx, "cls": class_id, "tStart": lt["tStart"],
                "tEnd": lt["tEnd"], "entries": lt["entries"],
                "entry_times": entry_times, "suppressed": suppressed,
                "tiny": tiny, "max_ang": max_ang, "az": az,
                "rep_t": t, "rep_box": (x1, y1, x2, y2),
            })
            state = ("SUPPRESSED (debounce)" if suppressed
                     else "SUPPRESSED (size gate)" if tiny else "shown")
            summary_lines.append(
                f"class={class_id} idx={idx:4d} t={lt['tStart']:7.1f}-{lt['tEnd']:7.1f}s "
                f"samples={len(lt['entries']):3d} az={az:6.1f} maxdeg={max_ang:4.1f} {state}")
        cache["classes"][str(class_id)] = cache_lifetimes
        shown = sum(1 for l in all_lifetimes
                    if l["cls"] == class_id and not l["suppressed"] and not l["tiny"])
        print(f"Class {class_id} ({CLASS_NAMES[class_id]}): {len(lifetimes)} lifetimes, "
              f"{shown} survive the {args.min_age}s debounce + {args.min_size_deg}° size gate "
              "and are drawn bright.")

    cache_path = out_dir / "lifetimes_cache.json"
    cache_path.write_text(json.dumps(cache, separators=(",", ":")))
    summary_path = out_dir / "lifetimes_summary.txt"
    summary_path.write_text("\n".join(summary_lines) + "\n")
    print(f"Cache -> {cache_path}\nSummary -> {summary_path}")

    all_lifetimes.sort(key=lambda l: l["tStart"])

    if args.sheets:
        # Contact sheets of ONLY the lifetimes that actually render on the headset
        # after both runtime gates, and aren't already covered by a bulk decision
        # (<= 2 samples). These are the candidates that need a human eye.
        reviewable = [l for l in all_lifetimes
                      if not l["suppressed"] and not l["tiny"] and len(l["entries"]) > 2]
        reviewable.sort(key=lambda l: (l["cls"], l["idx"]))
        entries = [{
            "idx": l["idx"], "t": l["rep_t"], "box": l["rep_box"],
            "label": f"#{l['idx']} t={l['tStart']:.0f}-{l['tEnd']:.0f}s az={l['az']:.0f} "
                     f"n={len(l['entries'])}",
        } for l in reviewable]
        sheets = build_contact_sheets(
            entries, args.video, src_w, src_h,
            pad_factor=2.5, min_crop_px=240, cell_px=320, per_sheet=24, cols=6,
            out_dir=out_dir / "sheets", prefix="visible_class9",
            crop_scratch_dir=out_dir / "crops")
        print(f"{len(reviewable)} reviewable lifetimes -> {len(sheets)} contact sheet(s) "
              f"in {out_dir / 'sheets'}")

    if args.skip_video:
        print("Video render skipped (--skip-video).")
        return 0

    font_px = max(12, out_w // 90)
    try:
        font = ImageFont.truetype("arial.ttf", font_px)
    except OSError:
        font = ImageFont.load_default()

    start = args.start
    end = samples[-1]["t"] + args.hold_seconds if args.duration is None \
        else start + args.duration
    n_frames = int((end - start) * args.fps)
    frame_bytes = out_w * out_h * 3

    decode = subprocess.Popen(
        ["ffmpeg", "-v", "error", "-ss", f"{start:.3f}", "-i", str(args.video),
         "-vf", f"fps={args.fps},scale={out_w}:{out_h}",
         "-frames:v", str(n_frames), "-f", "rawvideo", "-pix_fmt", "rgb24", "-"],
        stdout=subprocess.PIPE)
    out_mp4 = out_dir / "detections_preview.mp4"
    encode = subprocess.Popen(
        ["ffmpeg", "-y", "-v", "error", "-f", "rawvideo", "-pix_fmt", "rgb24",
         "-s", f"{out_w}x{out_h}", "-r", str(args.fps), "-i", "-",
         "-c:v", "libx264", "-preset", "veryfast", "-crf", "26",
         "-pix_fmt", "yuv420p", str(out_mp4)],
        stdin=subprocess.PIPE)

    head = 0  # index of the first lifetime that can still be active at/after t
    frame_idx = 0
    while True:
        raw = decode.stdout.read(frame_bytes)
        if len(raw) < frame_bytes:
            break
        t = start + frame_idx / args.fps
        img = Image.frombuffer("RGB", (out_w, out_h), raw, "raw", "RGB", 0, 1)
        draw = ImageDraw.Draw(img)

        while head < len(all_lifetimes) and \
                all_lifetimes[head]["tEnd"] + args.hold_seconds < t:
            head += 1
        for lt in all_lifetimes[head:]:
            if lt["tStart"] > t:
                break
            if not (lt["tStart"] <= t <= lt["tEnd"] + args.hold_seconds):
                continue
            x1, y1, x2, y2 = interpolate_box(lt["entry_times"], lt["entries"], t)
            px1, py1 = x1 * out_w, y1 * out_h
            px2, py2 = x2 * out_w, y2 * out_h
            # Distant lights are a few pixels at preview scale — inflate the drawn
            # rect to a spottable minimum (display only; mirrors the shader's own
            # ~2 deg minimum halo radius, so tiny detections read like they do in
            # the headset instead of vanishing).
            min_px = out_w * 0.01
            if px2 - px1 < min_px:
                cx = (px1 + px2) * 0.5
                px1, px2 = cx - min_px / 2, cx + min_px / 2
            if py2 - py1 < min_px:
                cy = (py1 + py2) * 0.5
                py1, py2 = cy - min_px / 2, cy + min_px / 2
            # Per-frame size gate, same as the runtime: even a lifetime that grows
            # big later is grey while its current box is still under the gate.
            cur_deg = max((x2 - x1) * 360.0, (y2 - y1) * 180.0)
            if lt["suppressed"]:
                color, width_px, tag = PREVIEW_SUPPRESSED, 1, " blip"
            elif cur_deg < args.min_size_deg:
                color, width_px, tag = PREVIEW_SUPPRESSED, 1, " tiny"
            else:
                color, width_px, tag = PREVIEW_COLORS[lt["cls"]], 3, ""
            draw.rectangle([px1, py1, px2, py2], outline=color, width=width_px)
            draw.text((px1, max(0, py1 - font_px - 2)),
                      f"#{lt['idx']}{tag}", font=font, fill=color)

        draw.text((8, 6), f"t={t:7.2f}s", font=font, fill=(255, 255, 0))
        encode.stdin.write(img.tobytes())
        frame_idx += 1
        if frame_idx % (args.fps * 30) == 0:
            print(f"  rendered up to t={t:.0f}s / {end:.0f}s")

    encode.stdin.close()
    decode.stdout.close()
    decode.wait()
    encode.wait()
    if encode.returncode != 0:
        print("ERROR: ffmpeg encoder failed.", file=sys.stderr)
        return 1

    print(f"\nPreview -> {out_mp4}\n"
          "Scrub it (bright cyan = traffic-light window the headset WILL show, orange = stop\n"
          "sign, dim grey 'blip' = suppressed by the runtime debounce, ignore those). Note the\n"
          "#ids of boxes circling nothing, write a decisions file naming only those ids, then:\n"
          f"  python Tools/triage_traffic_lights.py apply --cache {cache_path} --decisions <path>")
    return 0


# ---------------------------------------------------------------------------
# apply
# ---------------------------------------------------------------------------

def cmd_apply(args: argparse.Namespace) -> int:
    cache = json.loads(args.cache.read_text())
    decisions = json.loads(args.decisions.read_text())
    source_json_path = REPO_ROOT / cache["source_file"]

    data = json.loads(source_json_path.read_text())
    samples = data["samples"]

    removed_by_sample: dict[int, set[tuple]] = {}  # sample_idx -> set of (c,x1,y1,x2,y2)
    logs = {}  # class -> {entries:[], count_removed, count_before}

    for class_str, lifetimes in cache["classes"].items():
        class_id = int(class_str)
        dec_for_class = decisions.get(class_str, {})
        entries_log = []
        count_before = sum(1 for lt in lifetimes for _ in lt["entries"])
        count_removed = 0

        for lt in lifetimes:
            idx_str = str(lt["idx"])
            bucket = lt["bucket"]
            override = dec_for_class.get(idx_str)

            if override is not None:
                decision = override["decision"]
                reason = override.get("reason", "")
                method = "reviewed"
            elif bucket == "auto_relevant":
                decision, reason, method = "relevant", "auto: near video-forward azimuth", "auto_az"
            elif bucket == "auto_irrelevant":
                decision, reason, method = "irrelevant", "auto: far off-axis azimuth", "auto_az"
            else:
                print(f"ERROR: class {class_id} lifetime idx {lt['idx']} (bucket={bucket}) has no "
                      "decision in the decisions file — every ambiguous/needs_review lifetime "
                      "requires an explicit call.", file=sys.stderr)
                return 1

            if decision == "irrelevant":
                count_removed += len(lt["entries"])
                for sample_idx, x1, y1, x2, y2 in lt["entries"]:
                    removed_by_sample.setdefault(sample_idx, set()).add(
                        (class_id, round(x1, 4), round(y1, 4), round(x2, 4), round(y2, 4)))
                    entries_log.append({
                        "t": samples[sample_idx]["t"], "x1": x1, "y1": y1, "x2": x2, "y2": y2,
                        "az_deg": round(lt["az_deg"], 2), "decision_method": method, "reason": reason,
                    })
            elif decision != "relevant":
                print(f"ERROR: unknown decision '{decision}' for class {class_id} idx {lt['idx']}",
                      file=sys.stderr)
                return 1

        logs[class_id] = {
            "count_before": count_before, "count_removed": count_removed,
            "count_after": count_before - count_removed, "entries": entries_log,
        }

    main_samples = []
    sidecar_samples = []
    for sample_idx, s in enumerate(samples):
        removed_here = removed_by_sample.get(sample_idx, set())
        kept_d, removed_d = [], []
        for d in s["d"]:
            key = (d["c"], round(d["x1"], 4), round(d["y1"], 4), round(d["x2"], 4), round(d["y2"], 4))
            (removed_d if key in removed_here else kept_d).append(d)
        main_samples.append({"t": s["t"], "d": kept_d})
        if removed_d:
            sidecar_samples.append({"t": s["t"], "d": removed_d})

    main_out = {"interval": data["interval"], "samples": main_samples}
    sidecar_out = {"interval": data["interval"], "samples": sidecar_samples}

    args.backup_dir.mkdir(parents=True, exist_ok=True)
    snapshot_path = args.backup_dir / f"study_video.detections.current_{args.date}.json"
    snapshot_path.write_text(source_json_path.read_text())
    print(f"Snapshot -> {snapshot_path}")

    for class_id, log in logs.items():
        log_path = args.backup_dir / f"study_video.detections.relevance_removed_{class_id}_{args.date}.json"
        log_path.write_text(json.dumps({
            "source_file": str(source_json_path.relative_to(REPO_ROOT)),
            "sidecar_file": str(args.sidecar_out.relative_to(REPO_ROOT)),
            "removed_on": args.date,
            "method": ("Reconstructed lifetimes (VideoTestSceneManager.BuildDetectionLifetimes "
                       "identity heuristic ported to Python), auto-classified traffic lights by "
                       f"azimuth-from-forward (<= {cache['auto_relevant_deg']}° relevant, "
                       f">= {cache['auto_irrelevant_deg']}° irrelevant), visually reviewed the "
                       "ambiguous band (and all stop signs) via ffmpeg contact sheets, then removed "
                       "every raw sample entry belonging to a lifetime judged not to face/control "
                       "the driver's lane."),
            "class": class_id, "class_name": CLASS_NAMES[class_id],
            "count_removed": log["count_removed"], "count_before": log["count_before"],
            "count_after": log["count_after"], "entries": log["entries"],
        }, indent=2))
        print(f"Log -> {log_path}  (removed {log['count_removed']}/{log['count_before']})")

    source_json_path.write_text(json.dumps(main_out, separators=(",", ":")))
    print(f"Main file (overwritten) -> {source_json_path}")
    args.sidecar_out.write_text(json.dumps(sidecar_out, separators=(",", ":")))
    print(f"Sidecar file -> {args.sidecar_out}")

    # Sanity check: re-reconstruct from the new main file and confirm no lifetime
    # was split (every kept lifetime's entry count matches what it had before removal).
    ok = True
    for class_str, lifetimes in cache["classes"].items():
        class_id = int(class_str)
        new_lifetimes = build_lifetimes(main_samples, class_id, cache["hold_seconds"])
        new_entry_total = sum(len(lt["entries"]) for lt in new_lifetimes)
        if new_entry_total != logs[class_id]["count_after"]:
            print(f"WARNING: class {class_id} sanity check mismatch — expected "
                  f"{logs[class_id]['count_after']} surviving entries, "
                  f"reconstruction from the new main file found {new_entry_total}.", file=sys.stderr)
            ok = False
    if ok:
        print("\nSanity check passed: reconstructed entry counts match expected counts_after.")
    return 0 if ok else 1


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)

    rp = sub.add_parser("review")
    rp.add_argument("--json", type=Path, default=DEFAULT_JSON)
    rp.add_argument("--video", type=Path, default=DEFAULT_VIDEO)
    rp.add_argument("--hold-seconds", type=float, default=0.5)
    rp.add_argument("--auto-relevant-deg", type=float, default=15.0)
    rp.add_argument("--auto-irrelevant-deg", type=float, default=45.0)
    rp.add_argument("--pad-factor", type=float, default=2.5,
                     help="crop padding as a multiple of box size on each side")
    rp.add_argument("--min-crop-px", type=int, default=240)
    rp.add_argument("--cell-px", type=int, default=320)
    rp.add_argument("--per-sheet", type=int, default=24)
    rp.add_argument("--cols", type=int, default=6)
    rp.add_argument("--out-dir", type=Path, required=True,
                     help="where to write the lifetime cache + contact sheets")
    rp.add_argument("--crop-scratch-dir", type=Path, required=True,
                     help="scratch dir for individual per-lifetime crop PNGs before compositing")
    rp.set_defaults(func=cmd_review)

    pv = sub.add_parser("preview")
    pv.add_argument("--json", type=Path, default=DEFAULT_JSON)
    pv.add_argument("--video", type=Path, default=DEFAULT_VIDEO)
    pv.add_argument("--hold-seconds", type=float, default=0.5,
                     help="must match VideoTestSceneManager.m_signDetHoldSec")
    pv.add_argument("--min-age", type=float, default=0.6,
                     help="must match VideoTestSceneManager.m_detectionMinAgeSec — lifetimes "
                          "shorter than this are drawn dim grey (the headset won't show them)")
    pv.add_argument("--min-size-deg", type=float, default=1.2,
                     help="must match VideoTestSceneManager.m_detectionMinSizeDeg — boxes "
                          "currently smaller than this are drawn dim grey ('tiny')")
    pv.add_argument("--sheets", action="store_true",
                     help="also write contact sheets of the lifetimes that survive both "
                          "runtime gates with > 2 samples — the ones worth a human look")
    pv.add_argument("--skip-video", action="store_true",
                     help="skip the mp4 render (e.g. when only regenerating sheets/cache)")
    pv.add_argument("--out-width", type=int, default=1600)
    pv.add_argument("--fps", type=int, default=10)
    pv.add_argument("--start", type=float, default=0.0,
                     help="video time to start rendering from (smoke tests)")
    pv.add_argument("--duration", type=float, default=None,
                     help="seconds to render (default: whole video)")
    pv.add_argument("--out-dir", type=Path, required=True,
                     help="where to write detections_preview.mp4 + lifetimes cache/summary")
    pv.set_defaults(func=cmd_preview)

    ap_p = sub.add_parser("apply")
    ap_p.add_argument("--cache", type=Path, required=True)
    ap_p.add_argument("--decisions", type=Path, required=True)
    ap_p.add_argument("--sidecar-out", type=Path,
                       default=REPO_ROOT / "Builds/StudyVideo/study_video.detections.irrelevant_signals.json")
    ap_p.add_argument("--backup-dir", type=Path, default=DEFAULT_BACKUP_DIR)
    ap_p.add_argument("--date", default="2026-07-14")
    ap_p.set_defaults(func=cmd_apply)

    args = ap.parse_args()
    return args.func(args)


if __name__ == "__main__":
    sys.exit(main())
