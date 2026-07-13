#!/usr/bin/env python3
"""
Cut + transcode a source clip into the sideloaded study_video.mp4 the video
scene plays (see VideoTestSceneManager.cs: persistentDataPath/study_video.mp4,
falls back to bundled StreamingAssets/DebugVideo.mp4 if absent).

Always use this script rather than an ad-hoc ffmpeg command for this pipeline —
it bakes in two things that are each expensive to get wrong:

  * `-movflags +faststart` (moov atom moved to the front). Without it, a large
    (~GB) file played from a raw filesystem path (sideload) can trip the Quest's
    hardware HEVC decoder into a continuous "queueOutputs: Allocation failed"
    failure loop — confirmed on-device 2026-07-12. Bundled StreamingAssets
    playback tolerated the missing flag fine (different Android read path), so
    this only shows up once you sideload, which is exactly the point of this
    pipeline. `Tools/lisa_to_detections.py` already followed this convention;
    this script brings the NoHo/study-video pipeline in line with it.
  * Sideloading over bundling. Bundling in StreamingAssets is capped by the
    APK zip format's ~4 GiB single-entry limit; sideloading via
    `adb push <file> /sdcard/Android/data/<package>/files/study_video.mp4`
    has no such ceiling, which is what actually lets the clip run long.

Usage (from the repo root):
    python Tools/encode_study_video.py --source SOURCE.mp4 --start 600 --end 1120
    python Tools/encode_study_video.py --source SOURCE.mp4 --start 600 --end 1120 --crf 27

Output (default): Builds/StudyVideo/study_video.mp4 — push it with:
    adb push Builds/StudyVideo/study_video.mp4 /sdcard/Android/data/<package>/files/study_video.mp4
"""

import argparse
import shutil
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
DEFAULT_OUT = REPO_ROOT / "Builds/StudyVideo/study_video.mp4"

# 5760x2880 matches VideoTestSceneManager.cs's hardcoded m_videoRT size — the
# equirect video sphere's render target. Changing this requires a matching
# code change there; it is not just an encode-time knob.
TARGET_WIDTH = 5760
TARGET_HEIGHT = 2880


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--source", type=Path, required=True, help="source video (any resolution/container)")
    ap.add_argument("--start", type=float, required=True, help="clip start, seconds")
    ap.add_argument("--end", type=float, required=True, help="clip end, seconds")
    ap.add_argument("--out", type=Path, default=DEFAULT_OUT)
    ap.add_argument("--crf", type=int, default=27,
                    help="x265 CRF (default 27). Lower = better quality + bigger file. "
                         "CRF 20 on a 520 s NoHo-style clip produced ~4.76 GB (over the "
                         "APK's bundling limit, though irrelevant once sideloaded); "
                         "CRF 27 produced ~1.96 GB at comparable perceived quality.")
    ap.add_argument("--preset", default="medium", help="x265 preset (default medium)")
    ap.add_argument("--audio-source", type=Path, default=None,
                    help="separate audio-only source, if --source is video-only "
                         "(e.g. a yt-dlp video-only download needing its matching audio track)")
    args = ap.parse_args()

    if not shutil.which("ffmpeg"):
        print("ERROR: ffmpeg not on PATH.", file=sys.stderr)
        return 1
    if not args.source.exists():
        print(f"ERROR: source not found: {args.source}", file=sys.stderr)
        return 1
    if args.end <= args.start:
        print("ERROR: --end must be after --start", file=sys.stderr)
        return 1

    args.out.parent.mkdir(parents=True, exist_ok=True)

    cmd = ["ffmpeg", "-y", "-ss", str(args.start), "-to", str(args.end), "-i", str(args.source)]
    if args.audio_source:
        cmd += ["-ss", str(args.start), "-to", str(args.end), "-i", str(args.audio_source)]

    cmd += ["-vf", f"scale={TARGET_WIDTH}:{TARGET_HEIGHT}",
            "-c:v", "libx265", "-preset", args.preset, "-crf", str(args.crf), "-tag:v", "hvc1",
            "-c:a", "aac", "-b:a", "128k",
            "-movflags", "+faststart"]  # moov-at-front — see module docstring
    if args.audio_source:
        cmd += ["-map", "0:v:0", "-map", "1:a:0"]
    cmd += [str(args.out)]

    print(f"Encoding {args.end - args.start:.0f}s @ {TARGET_WIDTH}x{TARGET_HEIGHT}, "
          f"crf={args.crf}, preset={args.preset} -> {args.out}")
    result = subprocess.run(cmd)
    if result.returncode != 0:
        print("ERROR: ffmpeg failed.", file=sys.stderr)
        return 1

    size_mb = args.out.stat().st_size / (1024 * 1024)
    print(f"\nDONE — {args.out} ({size_mb:.0f} MB)")
    print("Push it to the device with:")
    print(f"  adb push {args.out} /sdcard/Android/data/<package>/files/study_video.mp4")
    return 0


if __name__ == "__main__":
    sys.exit(main())
