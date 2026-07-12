#!/usr/bin/env python3
# /// script
# requires-python = ">=3.10"
# dependencies = []
# ///
"""
LISA Traffic Light Dataset -> VideoTestScene flat-clip stimulus.

Builds (1) an mp4 from a LISA clip's frame sequence and (2) a detections.json with
the GROUND-TRUTH traffic-light boxes mapped into the video sphere's equirect space —
SignPop is then gated by hand annotations instead of a detector (zero detection error).

Dataset: https://www.kaggle.com/datasets/mbornoe/lisa-traffic-light-dataset
Typical layout:
    dayTrain/dayClip5/frames/*.jpg                        (frame sequence)
    Annotations/dayTrain/dayClip5/frameAnnotationsBOX.csv (boxes, semicolon CSV)

The angular mapping MUST match VideoTestSceneManager's Flat Clip Mode settings:
--hfov = m_flatHFovDeg, --el-center-deg = m_flatElCenterDeg. The clip is treated as
a pinhole image centred at sphere azimuth 0. Boxes are written full-equirect
normalized, y = 0 at top (matches m_yoloFlipY = true), class 9 (COCO traffic light)
so the runtime class filter passes them.

Timing: video time = frame / fps BY CONSTRUCTION (the mp4 is assembled at --fps), so
box alignment always holds; --fps only changes playback speed (LISA capture ~16 fps).

Usage:
    uv run Tools/lisa_to_detections.py --frames <framesDir> --csv <frameAnnotationsBOX.csv>
    # options: --fps 16 --hfov 60 --el-center-deg 0 --max-seconds 0
    #          --out-video Assets/StreamingAssets/FlatClip.mp4 (json path derived)

APK note: StreamingAssets is bundled into the APK. For long clips sideload instead:
    adb push FlatClip.mp4            <app files dir>/flat_clip.mp4
    adb push FlatClip.detections.json <app files dir>/flat_clip.detections.json
Then enable m_flatClipMode on VideoTestSceneManager and disable BakeOnPlay on
VideoDetectionBaker so the low-res editor bake never overwrites the ground truth.
"""

import argparse
import csv
import json
import math
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
DEFAULT_VIDEO = REPO_ROOT / "Assets/StreamingAssets/FlatClip.mp4"

LIGHT_CLASS = 9      # COCO traffic light — passes the runtime class filter
MAX_PER_SAMPLE = 24  # runtime keeps the first 16 lights/signs per sample


def probe_size(image: Path) -> tuple[int, int]:
    out = subprocess.check_output(
        ["ffprobe", "-v", "error", "-select_streams", "v:0",
         "-show_entries", "stream=width,height", "-of", "json", str(image)],
        text=True)
    s = json.loads(out)["streams"][0]
    return int(s["width"]), int(s["height"])


def build_video(frames: list[Path], fps: float, out: Path) -> None:
    """Assemble frames into an mp4 via the concat demuxer (no numbering assumptions)."""
    dur = 1.0 / fps
    with tempfile.NamedTemporaryFile("w", suffix=".txt", delete=False,
                                     encoding="utf-8") as f:
        f.write("ffconcat version 1.0\n")
        for p in frames:
            f.write(f"file '{p.resolve().as_posix()}'\nduration {dur:.6f}\n")
        # concat spec: repeat the last file so its duration entry applies
        f.write(f"file '{frames[-1].resolve().as_posix()}'\n")
        list_path = f.name
    out.parent.mkdir(parents=True, exist_ok=True)
    subprocess.check_call(
        ["ffmpeg", "-y", "-v", "error", "-f", "concat", "-safe", "0",
         "-i", list_path, "-r", f"{fps}", "-c:v", "libx264", "-crf", "18",
         "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(out)])
    Path(list_path).unlink(missing_ok=True)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--frames", type=Path, required=True,
                    help="directory with the LISA clip's frame images (*.jpg)")
    ap.add_argument("--csv", type=Path, required=True,
                    help="frameAnnotationsBOX.csv for the same clip")
    ap.add_argument("--out-video", type=Path, default=DEFAULT_VIDEO,
                    help="output mp4; the .detections.json path is derived from it")
    ap.add_argument("--fps", type=float, default=16.0,
                    help="assembly frame rate (LISA capture is ~16 fps)")
    ap.add_argument("--hfov", type=float, default=60.0,
                    help="horizontal FOV of the source camera (deg) — must equal "
                         "m_flatHFovDeg on VideoTestSceneManager")
    ap.add_argument("--el-center-deg", type=float, default=0.0,
                    help="clip-centre elevation on the sphere (deg) — must equal "
                         "m_flatElCenterDeg")
    ap.add_argument("--max-seconds", type=float, default=0.0,
                    help="only convert the first N seconds (0 = whole clip)")
    args = ap.parse_args()

    if not shutil.which("ffmpeg") or not shutil.which("ffprobe"):
        print("ERROR: ffmpeg/ffprobe not on PATH.", file=sys.stderr)
        return 1
    frames = sorted([*args.frames.glob("*.jpg"), *args.frames.glob("*.png")])
    if not frames:
        print(f"ERROR: no frames in {args.frames}", file=sys.stderr)
        return 1
    if not args.csv.exists():
        print(f"ERROR: csv not found: {args.csv}", file=sys.stderr)
        return 1

    if args.max_seconds > 0:
        frames = frames[: max(1, int(args.fps * args.max_seconds))]
    n = len(frames)
    width, height = probe_size(frames[0])

    # Pinhole -> equirect mapping shared with FlatClipToEquirect.shader /
    # CompositeFlatToEquirect(): x/z = tan(az), y/z = tan(el)/cos(az).
    tan_h = math.tan(math.radians(args.hfov / 2))
    tan_v = tan_h * height / width
    el_c = math.radians(args.el_center_deg)

    def to_equirect(px: float, py: float) -> tuple[float, float]:
        xn = px / width * 2 - 1
        yn = 1 - 2 * py / height              # image y-down -> y-up
        az = math.atan(tan_h * xn)
        el = math.atan(tan_v * yn * math.cos(az))
        u = 0.5 + az / (2 * math.pi)
        y_top = 0.5 - (el + el_c) / math.pi   # json convention: y = 0 at top
        return u, y_top

    # Boxes grouped by frame index (annotation Filename basename -> position in
    # the sorted frame list — robust to numbering gaps and path prefixes).
    index_of = {p.name: i for i, p in enumerate(frames)}
    boxes: dict[int, list[tuple[float, float, float, float]]] = {}
    skipped = 0
    with open(args.csv, newline="", encoding="utf-8") as f:
        for row in csv.DictReader(f, delimiter=";"):
            idx = index_of.get(Path(row["Filename"]).name)
            if idx is None:
                skipped += 1
                continue
            u1, t1 = to_equirect(float(row["Upper left corner X"]),
                                 float(row["Upper left corner Y"]))
            u2, t2 = to_equirect(float(row["Lower right corner X"]),
                                 float(row["Lower right corner Y"]))
            boxes.setdefault(idx, []).append(
                (min(u1, u2), min(t1, t2), max(u1, u2), max(t1, t2)))

    print(f"Frames: {n} @ {args.fps} fps ({n / args.fps:.1f}s), {width}x{height}")
    print(f"Map   : hfov {args.hfov}°, el-center {args.el_center_deg}°")
    print("Building video...", flush=True)
    build_video(frames, args.fps, args.out_video)

    samples = []
    for k in range(n):
        frame_boxes = sorted(boxes.get(k, []),
                             key=lambda b: -((b[2] - b[0]) * (b[3] - b[1])))
        samples.append({
            "t": round(k / args.fps, 4),
            "d": [{"c": LIGHT_CLASS,
                   "x1": round(b[0], 4), "y1": round(b[1], 4),
                   "x2": round(b[2], 4), "y2": round(b[3], 4)}
                  for b in frame_boxes[:MAX_PER_SAMPLE]],
        })

    out_json = args.out_video.with_suffix(".detections.json")
    out_json.write_text(json.dumps({"interval": 1.0 / args.fps, "samples": samples},
                                   separators=(",", ":")))

    total = sum(len(s["d"]) for s in samples)
    covered = sum(1 for s in samples if s["d"])
    print(f"\nDONE — {total} ground-truth light boxes over {n} frames "
          f"({100 * covered / n:.0f}% of frames have >=1)")
    if skipped:
        print(f"  ({skipped} annotation rows referenced frames outside the converted range)")
    print(f"  -> {args.out_video}\n  -> {out_json}")
    print("Unity: enable m_flatClipMode on VideoTestSceneManager, set "
          f"m_flatHFovDeg = {args.hfov} and m_flatElCenterDeg = {args.el_center_deg}, "
          "and disable BakeOnPlay on VideoDetectionBaker.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
