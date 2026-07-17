#!/usr/bin/env python3
"""
Assembles a silent, TikTok-style vertical distractor video for the Block A
information-retention/source-confusion test:

  - Background: fast cuts between several bright abstract images, each with a
    quick digital zoom-in, cycling continuously (the "fast moving pics"
    look of real short-form-video backgrounds).
  - Captions: word-by-word "karaoke" reveal, matching real auto-caption
    styles -- words appear one at a time and accumulate into the full
    sentence; the word currently "landing" is drawn larger/highlighted, then
    settles to normal size once the next word appears. Layout is computed
    once per sentence (stable line breaks) so words don't jump around as
    more of them appear.

No audio -- pure visual distractor, looped like any other video asset by
phone-prop playback.

Usage:
    python Tools/build_distractor_video.py --bg-dir Assets/_Scratch --lines Tools/distractor_lines.json \
        --out Assets/_Scratch/distractor_phone.mp4
"""

import argparse
import json
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

REPO_ROOT = Path(__file__).resolve().parent.parent
FONT_PATH = REPO_ROOT / "Tools/fonts/arialbd.ttf"

VIDEO_W, VIDEO_H = 1080, 1920
FPS = 30

# ---- word-reveal timing ----
WORD_SEC = 0.30          # on-screen time per newly-revealed word
MIN_WORD_SEC = 0.22
HOLD_SEC = 0.9           # extra hold once the full sentence is built
GAP_SEC = 0.35           # blank beat between sentences
LEAD_IN_SEC = 0.3

# ---- caption layout ----
BASE_FONT_SIZE = 58
HILITE_FONT_SIZE = 78
HILITE_COLOR = (255, 225, 60, 255)   # warm yellow -- classic caption "pop" colour
BASE_COLOR = (255, 255, 255, 255)
MAX_TEXT_WIDTH = 880
WORD_GAP = 16
LINE_PITCH = 78
CARD_PAD_X = 40
CARD_PAD_Y = 46
CARD_COLOR = (0, 0, 0, 150)
CARD_CENTER_Y = int(VIDEO_H * 0.60)

# ---- background fast-cut ----
BG_CUT_SEC = 1.3          # how long each background image is shown before cutting to the next
BG_ZOOM_END = 1.22        # quick zoom-in amount over that image's on-screen time


def word_duration(word: str) -> float:
    return max(MIN_WORD_SEC, WORD_SEC * (0.6 + 0.15 * len(word) / 5))


class Word:
    __slots__ = ("text", "x", "y", "w", "h")

    def __init__(self, text, x, y, w, h):
        self.text, self.x, self.y, self.w, self.h = text, x, y, w, h


def layout_sentence(words: list[str], font_base: ImageFont.FreeTypeFont,
                     font_hilite: ImageFont.FreeTypeFont) -> tuple[list[list[Word]], int, int]:
    """Greedy word-wrap. Every word reserves a slot sized to its LARGER of
    base/highlighted rendering (every word gets a turn being highlighted), so
    the temporary size bump never overlaps a neighbour regardless of which
    word is currently active. Returns (lines_of_Word, block_w, block_h)."""
    dummy = Image.new("RGBA", (10, 10))
    d = ImageDraw.Draw(dummy)

    lines: list[list[Word]] = [[]]
    cursor_x = 0
    for w in words:
        base_bbox = d.textbbox((0, 0), w, font=font_base)
        hilite_bbox = d.textbbox((0, 0), w, font=font_hilite)
        ww = max(base_bbox[2] - base_bbox[0], hilite_bbox[2] - hilite_bbox[0])
        wh = max(base_bbox[3] - base_bbox[1], hilite_bbox[3] - hilite_bbox[1])
        if lines[-1] and cursor_x + ww > MAX_TEXT_WIDTH:
            lines.append([])
            cursor_x = 0
        lines[-1].append(Word(w, cursor_x, 0, ww, wh))
        cursor_x += ww + WORD_GAP

    block_w = max((sum(word.w for word in ln) + WORD_GAP * (len(ln) - 1) for ln in lines if ln), default=0)
    block_h = len(lines) * LINE_PITCH

    # Centre each line horizontally within block_w, and assign y per line.
    for li, ln in enumerate(lines):
        line_w = sum(word.w for word in ln) + WORD_GAP * (len(ln) - 1)
        x_off = (block_w - line_w) / 2
        cx = x_off
        for word in ln:
            word.x = cx
            word.y = li * LINE_PITCH
            cx += word.w + WORD_GAP

    return lines, int(block_w), int(block_h)


def render_state(lines: list[list[Word]], block_w: int, block_h: int,
                  reveal_upto: int, font_base, font_hilite) -> Image.Image:
    """reveal_upto: global word index (0-based) of the current/highlighted word.
    Words with global index < reveal_upto are drawn at base size; == reveal_upto
    is drawn highlighted/larger; > reveal_upto are not drawn at all."""
    img = Image.new("RGBA", (VIDEO_W, VIDEO_H), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    origin_x = (VIDEO_W - block_w) // 2
    origin_y = CARD_CENTER_Y - block_h // 2

    card_box = [origin_x - CARD_PAD_X, origin_y - CARD_PAD_Y,
                origin_x + block_w + CARD_PAD_X, origin_y + block_h + CARD_PAD_Y]
    draw.rounded_rectangle(card_box, radius=28, fill=CARD_COLOR)

    gi = 0
    for ln in lines:
        for word in ln:
            if gi > reveal_upto:
                gi += 1
                continue
            cx = origin_x + word.x + word.w / 2
            cy = origin_y + word.y + word.h / 2
            if gi == reveal_upto:
                bbox = draw.textbbox((0, 0), word.text, font=font_hilite)
                w2, h2 = bbox[2] - bbox[0], bbox[3] - bbox[1]
                draw.text((cx - w2 / 2 - bbox[0], cy - h2 / 2 - bbox[1]), word.text,
                          font=font_hilite, fill=HILITE_COLOR,
                          stroke_width=3, stroke_fill=(0, 0, 0, 255))
            else:
                bbox = draw.textbbox((0, 0), word.text, font=font_base)
                w2, h2 = bbox[2] - bbox[0], bbox[3] - bbox[1]
                draw.text((cx - w2 / 2 - bbox[0], cy - h2 / 2 - bbox[1]), word.text,
                          font=font_base, fill=BASE_COLOR,
                          stroke_width=2, stroke_fill=(0, 0, 0, 255))
            gi += 1
    return img


def build_caption_track(lines_json: list[str], work_dir: Path) -> tuple[Path, float]:
    font_base = ImageFont.truetype(str(FONT_PATH), BASE_FONT_SIZE)
    font_hilite = ImageFont.truetype(str(FONT_PATH), HILITE_FONT_SIZE)

    frames_dir = work_dir / "caption_frames"
    frames_dir.mkdir(parents=True, exist_ok=True)
    concat_lines = []
    frame_idx = 0
    t = LEAD_IN_SEC

    blank = Image.new("RGBA", (VIDEO_W, VIDEO_H), (0, 0, 0, 0))
    blank_path = frames_dir / "blank.png"
    blank.save(blank_path)
    concat_lines.append(f"file '{blank_path.name}'\nduration {LEAD_IN_SEC:.3f}\n")

    print(f"Building {len(lines_json)} caption sentences...")
    for text in lines_json:
        words = text.split()
        lines, block_w, block_h = layout_sentence(words, font_base, font_hilite)

        for gi, w in enumerate(words):
            dur = word_duration(w)
            is_last = gi == len(words) - 1
            state_dur = dur + (HOLD_SEC if is_last else 0.0)
            img = render_state(lines, block_w, block_h, gi, font_base, font_hilite)
            fname = f"f{frame_idx:04d}.png"
            img.save(frames_dir / fname)
            concat_lines.append(f"file '{fname}'\nduration {state_dur:.3f}\n")
            frame_idx += 1
            t += state_dur

        concat_lines.append(f"file '{blank_path.name}'\nduration {GAP_SEC:.3f}\n")
        t += GAP_SEC

    # concat demuxer requires the last entry duplicated without a trailing duration line
    concat_lines.append(f"file '{blank_path.name}'\n")
    list_path = frames_dir / "concat.txt"
    list_path.write_text("\n".join(concat_lines), encoding="utf-8")

    out_path = work_dir / "captions.mov"
    cmd = [
        "ffmpeg", "-y", "-v", "error",
        "-f", "concat", "-safe", "0", "-i", str(list_path),
        "-vf", f"fps={FPS}",
        "-c:v", "qtrle",
        str(out_path),
    ]
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        print("FFMPEG (captions) FAILED", file=sys.stderr)
        print(result.stderr[:3000], file=sys.stderr)
        sys.exit(1)

    return out_path, t


def build_background_track(bg_images: list[Path], total_duration: float, work_dir: Path) -> Path:
    seg_dir = work_dir / "bg_segments"
    seg_dir.mkdir(parents=True, exist_ok=True)

    n_segments = int(total_duration / BG_CUT_SEC) + 1
    seg_paths = []
    for i in range(n_segments):
        img = bg_images[i % len(bg_images)]
        seg_path = seg_dir / f"seg{i:03d}.mp4"
        frames = int(BG_CUT_SEC * FPS)
        # Alternate zoom direction each cut so consecutive segments don't feel identical.
        zoom_expr = (f"min(zoom+{(BG_ZOOM_END - 1.0) / frames:.6f},{BG_ZOOM_END})" if i % 2 == 0
                     else f"max({BG_ZOOM_END}-on*{(BG_ZOOM_END - 1.0) / frames:.6f},1.0)")
        cmd = [
            "ffmpeg", "-y", "-v", "error",
            "-loop", "1", "-i", str(img),
            "-vf", (f"scale=-2:{VIDEO_H * 2}:flags=lanczos,"
                    f"zoompan=z='{zoom_expr}':d={frames}:s={VIDEO_W}x{VIDEO_H}:fps={FPS}"),
            "-t", f"{BG_CUT_SEC:.3f}",
            "-c:v", "libx264", "-pix_fmt", "yuv420p",
            str(seg_path),
        ]
        result = subprocess.run(cmd, capture_output=True, text=True)
        if result.returncode != 0:
            print(f"FFMPEG (bg segment {i}) FAILED", file=sys.stderr)
            print(result.stderr[:2000], file=sys.stderr)
            sys.exit(1)
        seg_paths.append(seg_path)

    list_path = seg_dir / "concat.txt"
    list_path.write_text("\n".join(f"file '{p.name}'" for p in seg_paths), encoding="utf-8")
    out_path = work_dir / "background.mp4"
    cmd = [
        "ffmpeg", "-y", "-v", "error",
        "-f", "concat", "-safe", "0", "-i", str(list_path),
        "-t", f"{total_duration:.3f}",
        "-c:v", "libx264", "-pix_fmt", "yuv420p",
        str(out_path),
    ]
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        print("FFMPEG (bg concat) FAILED", file=sys.stderr)
        print(result.stderr[:2000], file=sys.stderr)
        sys.exit(1)
    return out_path


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--bg-dir", type=Path, required=True, help="directory containing bg_*.png / distractor_bg.png background images")
    ap.add_argument("--lines", type=Path, required=True)
    ap.add_argument("--out", type=Path, required=True)
    ap.add_argument("--keep-work-dir", action="store_true", help="don't delete the intermediate frames/segments (for debugging)")
    args = ap.parse_args()

    if not shutil.which("ffmpeg"):
        print("ERROR: ffmpeg not on PATH.", file=sys.stderr)
        return 1
    bg_images = sorted(args.bg_dir.glob("*.png"))
    bg_images = [p for p in bg_images if "frame_check" not in p.name]
    if not bg_images:
        print(f"ERROR: no background images found in {args.bg_dir}", file=sys.stderr)
        return 1
    print(f"Using {len(bg_images)} background images: {[p.name for p in bg_images]}")

    lines_json = json.loads(args.lines.read_text())
    if not lines_json:
        print("ERROR: no lines in --lines JSON.", file=sys.stderr)
        return 1

    work_dir = Path(tempfile.mkdtemp(prefix="distractor_build_")) if not args.keep_work_dir \
        else args.out.parent / "_build_work"
    work_dir.mkdir(parents=True, exist_ok=True)

    captions_path, total_duration = build_caption_track(lines_json, work_dir)
    print(f"Total duration: {total_duration:.1f}s")

    background_path = build_background_track(bg_images, total_duration, work_dir)

    args.out.parent.mkdir(parents=True, exist_ok=True)
    cmd = [
        "ffmpeg", "-y", "-v", "error",
        "-i", str(background_path),
        "-i", str(captions_path),
        "-filter_complex", "[0:v][1:v]overlay=format=auto:shortest=1",
        "-t", f"{total_duration:.3f}",
        "-c:v", "libx264", "-pix_fmt", "yuv420p", "-r", str(FPS),
        "-an",
        str(args.out),
    ]
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        print("FFMPEG (final composite) FAILED", file=sys.stderr)
        print(result.stderr[:3000], file=sys.stderr)
        return 1

    if not args.keep_work_dir:
        shutil.rmtree(work_dir, ignore_errors=True)

    print(f"DONE -> {args.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
