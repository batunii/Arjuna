#!/usr/bin/env python3
# /// script
# requires-python = ">=3.10"
# dependencies = ["ultralytics>=8.3", "numpy"]
# ///
"""
Offline detection baker for VideoTestScene (replaces the 640x360 in-editor bake).

Runs tiled YOLO inference over the full-resolution video on the PC, so small
objects (distant traffic lights) that the runtime/editor bake missed are
recovered. Writes the same JSON schema VideoDetectionTrack expects, so the
scene picks it up automatically (m_useBakedDetections) with no Unity changes.

IMPORTANT: bake from the exact clip the headset plays
(Assets/StreamingAssets/DebugVideo.mp4) — the track is keyed by video time.
Baking from the long DevVideos source would misalign every timestamp.

By default only the FORWARD view is baked (az ±85°, el ±50° around the video
centre) — the driver never looks behind, rear/pole detections wasted runtime
slots, and concentrating the tile budget on the forward crop roughly doubles
the angular resolution per tile (better small-light recall at the same cost).
Boxes are still written in FULL-frame normalized coords, so the JSON schema
and Unity side are unchanged. Note: if the scene's m_videoUOffset is nonzero,
pass --az-center-deg = -(m_videoUOffset * 360) so the crop tracks video-forward.

Usage (from the repo root — uv resolves the dependencies automatically):
    uv run Tools/bake_detections.py                      # forward crop, yolo11l
    uv run Tools/bake_detections.py --max-seconds 10 --model yolo11s.pt  # smoke test
    uv run Tools/bake_detections.py --model yolo11x.pt   # max recall (slow)
    uv run Tools/bake_detections.py --az-fov 360 --el-fov 180 --cols 3 --rows 2  # full 360 bake

Output: Assets/StreamingAssets/DebugVideo.detections.json

Box convention: normalized [0,1], standard image coords (y=0 at top).
VideoTestSceneManager.m_yoloFlipY = true (the default) converts these to the
shader's bottom-up video-texture coords. If detection zones ever appear at
mirror-image elevations, toggle that flag rather than editing this script.
"""

import argparse
import json
import shutil
import subprocess
import sys
import time
from pathlib import Path

import numpy as np

REPO_ROOT = Path(__file__).resolve().parent.parent
DEFAULT_VIDEO = REPO_ROOT / "Assets/StreamingAssets/DebugVideo.mp4"
DEFAULT_OUT = REPO_ROOT / "Assets/StreamingAssets/DebugVideo.detections.json"

# COCO ids the runtime treats as first-class (traffic light, stop sign).
# They sort first within each sample so the runtime's 16-slot cap keeps them.
# NOTE: COCO has no generic road-sign class — anything beyond stop signs needs
# a model fine-tuned on a traffic-sign dataset (e.g. Mapillary MTSD).
PRIORITY_CLASSES = {9, 11}


def probe_video(path: Path) -> tuple[int, int, float]:
    """Return (width, height, duration_seconds) via ffprobe."""
    out = subprocess.check_output(
        [
            "ffprobe", "-v", "error", "-select_streams", "v:0",
            "-show_entries", "stream=width,height", "-show_entries",
            "format=duration", "-of", "json", str(path),
        ],
        text=True,
    )
    info = json.loads(out)
    stream = info["streams"][0]
    return int(stream["width"]), int(stream["height"]), float(info["format"]["duration"])


def frame_reader(path: Path, width: int, height: int, interval: float):
    """Yield RGB frames sampled every `interval` seconds, decoded via ffmpeg."""
    fps = 1.0 / interval
    proc = subprocess.Popen(
        [
            "ffmpeg", "-v", "error", "-i", str(path),
            "-vf", f"fps={fps}", "-f", "rawvideo", "-pix_fmt", "rgb24", "-",
        ],
        stdout=subprocess.PIPE,
    )
    frame_bytes = width * height * 3
    try:
        while True:
            buf = proc.stdout.read(frame_bytes)
            if len(buf) < frame_bytes:
                break
            yield np.frombuffer(buf, dtype=np.uint8).reshape(height, width, 3)
    finally:
        proc.stdout.close()
        proc.terminate()


def make_tiles(width: int, height: int, cols: int, rows: int, overlap: int):
    """Tile rects (x0, y0, x1, y1) with overlap, plus one full-frame rect."""
    tiles = []
    tile_w, tile_h = width // cols, height // rows
    for r in range(rows):
        for c in range(cols):
            x0 = max(0, c * tile_w - overlap)
            y0 = max(0, r * tile_h - overlap)
            x1 = min(width, (c + 1) * tile_w + overlap)
            y1 = min(height, (r + 1) * tile_h + overlap)
            tiles.append((x0, y0, x1, y1))
    tiles.append((0, 0, width, height))  # full-frame pass catches large objects
    return tiles


def nms_per_class(dets: list[dict], iou_thresh: float) -> list[dict]:
    """Greedy per-class NMS over normalized boxes; keeps highest-confidence."""
    kept = []
    for cls in {d["c"] for d in dets}:
        group = sorted((d for d in dets if d["c"] == cls),
                       key=lambda d: -d["conf"])
        while group:
            best = group.pop(0)
            kept.append(best)
            group = [d for d in group if iou(best, d) < iou_thresh]
    return kept


def iou(a: dict, b: dict) -> float:
    ix1, iy1 = max(a["x1"], b["x1"]), max(a["y1"], b["y1"])
    ix2, iy2 = min(a["x2"], b["x2"]), min(a["y2"], b["y2"])
    inter = max(0.0, ix2 - ix1) * max(0.0, iy2 - iy1)
    if inter <= 0.0:
        return 0.0
    area_a = (a["x2"] - a["x1"]) * (a["y2"] - a["y1"])
    area_b = (b["x2"] - b["x1"]) * (b["y2"] - b["y1"])
    return inter / (area_a + area_b - inter)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--video", type=Path, default=DEFAULT_VIDEO)
    ap.add_argument("--out", type=Path, default=DEFAULT_OUT)
    ap.add_argument("--model", default="yolo11l.pt",
                    help="ultralytics model (auto-downloads); yolo11x.pt = max recall (slow), "
                         "yolo11s.pt = fast smoke tests")
    ap.add_argument("--interval", type=float, default=0.25,
                    help="seconds between samples (runtime lookup tolerates 1.5x)")
    ap.add_argument("--imgsz", type=int, default=960,
                    help="inference size per tile; 1280 = better small-object recall")
    ap.add_argument("--conf", type=float, default=0.25)
    ap.add_argument("--iou", type=float, default=0.5, help="cross-tile merge NMS IoU")
    ap.add_argument("--az-fov", type=float, default=170.0,
                    help="horizontal FOV (deg) to bake, centred on video-forward; 360 = full")
    ap.add_argument("--el-fov", type=float, default=100.0,
                    help="vertical FOV (deg) to bake, centred on the horizon; 180 = full")
    ap.add_argument("--az-center-deg", type=float, default=0.0,
                    help="crop centre azimuth (deg); use -(m_videoUOffset * 360) if the scene "
                         "shifts the video. Must not wrap the equirect seam (az ±180)")
    ap.add_argument("--cols", type=int, default=2,
                    help="tile columns over the crop (use 3 for a full-360 bake)")
    ap.add_argument("--rows", type=int, default=2)
    ap.add_argument("--overlap", type=int, default=96, help="tile overlap in pixels")
    ap.add_argument("--max-seconds", type=float, default=0.0,
                    help="bake only the first N seconds (0 = all); use for smoke tests")
    ap.add_argument("--max-per-sample", type=int, default=24,
                    help="cap stored detections per sample (priority classes kept first)")
    args = ap.parse_args()

    if not shutil.which("ffmpeg") or not shutil.which("ffprobe"):
        print("ERROR: ffmpeg/ffprobe not on PATH.", file=sys.stderr)
        return 1
    if not args.video.exists():
        print(f"ERROR: video not found: {args.video}", file=sys.stderr)
        return 1
    try:
        from ultralytics import YOLO
    except ImportError:
        print("ERROR: ultralytics not installed. Run via: uv run Tools/bake_detections.py",
              file=sys.stderr)
        return 1

    width, height, duration = probe_video(args.video)
    if args.max_seconds > 0:
        duration = min(duration, args.max_seconds)

    # Forward-view crop (equirect: az 0 = video-forward at u 0.5, el 0 at v 0.5).
    # Tiling runs over the crop; boxes are written back in full-frame coords.
    az_fov = min(max(args.az_fov, 10.0), 360.0)
    el_fov = min(max(args.el_fov, 10.0), 180.0)
    crop_w = min(width,  int(round(width  * az_fov / 360.0)))
    crop_h = min(height, int(round(height * el_fov / 180.0)))
    u_center = 0.5 + args.az_center_deg / 360.0
    cx0 = max(0, min(width - crop_w, int(round(u_center * width - crop_w / 2))))
    cy0 = (height - crop_h) // 2
    tiles = make_tiles(crop_w, crop_h, args.cols, args.rows, args.overlap)
    total = int(duration / args.interval)

    print(f"Video : {args.video} ({width}x{height}, {duration:.1f}s)")
    print(f"Model : {args.model} @ imgsz {args.imgsz}, conf {args.conf}")
    print(f"Crop  : az {az_fov:.0f}° x el {el_fov:.0f}° -> {crop_w}x{crop_h} px at ({cx0},{cy0})")
    print(f"Tiles : {args.cols}x{args.rows} +full crop ({len(tiles)} inferences/sample)")
    print(f"Plan  : {total} samples every {args.interval}s", flush=True)

    model = YOLO(args.model)
    samples = []
    start = time.time()

    for k, frame in enumerate(frame_reader(args.video, width, height, args.interval)):
        t = k * args.interval
        if t >= duration:
            break

        # Forward-view region; tile coords are relative to this crop.
        fwd = frame[cy0:cy0 + crop_h, cx0:cx0 + crop_w]

        # BGR crops for ultralytics (cv2 convention); batch all tiles in one call.
        crops = [np.ascontiguousarray(fwd[y0:y1, x0:x1, ::-1])
                 for (x0, y0, x1, y1) in tiles]
        results = model.predict(crops, imgsz=args.imgsz, conf=args.conf, verbose=False)

        dets = []
        for (x0, y0, x1, y1), res in zip(tiles, results):
            for box, cls, conf in zip(res.boxes.xyxy.tolist(),
                                      res.boxes.cls.tolist(),
                                      res.boxes.conf.tolist()):
                dets.append({
                    "c": int(cls),
                    # Written in FULL-frame normalized coords (crop offset added back)
                    # so the JSON schema and Unity playback are unchanged.
                    "x1": (cx0 + x0 + box[0]) / width, "y1": (cy0 + y0 + box[1]) / height,
                    "x2": (cx0 + x0 + box[2]) / width, "y2": (cy0 + y0 + box[3]) / height,
                    "conf": float(conf),
                })

        merged = nms_per_class(dets, args.iou)
        # Priority classes first, then larger boxes — the runtime keeps the
        # first 16 that pass its class filter, so ordering decides survival.
        merged.sort(key=lambda d: (
            0 if d["c"] in PRIORITY_CLASSES else 1,
            -((d["x2"] - d["x1"]) * (d["y2"] - d["y1"])),
        ))
        merged = merged[: args.max_per_sample]

        samples.append({
            "t": round(t, 3),
            "d": [{"c": d["c"],
                   "x1": round(d["x1"], 4), "y1": round(d["y1"], 4),
                   "x2": round(d["x2"], 4), "y2": round(d["y2"], 4)}
                  for d in merged],
        })

        if (k + 1) % 10 == 0 or k == 0:
            elapsed = time.time() - start
            rate = elapsed / (k + 1)
            n_prio = sum(1 for d in merged if d["c"] in PRIORITY_CLASSES)
            print(f"[{k + 1}/{total}] t={t:6.2f}s  {len(merged):2d} dets "
                  f"({n_prio} lights/signs)  ~{rate * (total - k - 1) / 60:.1f} min left",
                  flush=True)  # visible immediately when stdout is a log file

    args.out.write_text(json.dumps({"interval": args.interval, "samples": samples},
                                   separators=(",", ":")))
    prio_total = sum(1 for s in samples for d in s["d"] if d["c"] in PRIORITY_CLASSES)
    print(f"\nDONE — {len(samples)} samples, {prio_total} traffic-light/stop-sign "
          f"detections total\n  -> {args.out}")
    print("The scene loads this automatically (m_useBakedDetections). "
          "Disable BakeOnPlay on VideoDetectionBaker so it doesn't overwrite it.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
