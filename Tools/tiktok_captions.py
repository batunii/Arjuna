#!/usr/bin/env python3
"""
Builds a TikTok-style captioned video that cuts to a different background
clip for every sentence, picked at random from a pool of source videos (e.g.
RawFootage/*.mp4) -- mimicking the real short-form-video feel of cutting to
new footage on every beat, with word-by-word "karaoke" captions burned on
top of each clip.

Caption look: words accumulate one at a time, the newly-landed word pops
bigger/yellow then settles to white once the next word lands, sitting on a
translucent rounded card. Once a sentence finishes its hold, the card floats
upward and fades out before the next sentence's clip cuts in and its
caption starts forming from scratch.

Usage:
    python Tools/tiktok_captions.py --lines Tools/my_script.txt \
        --out Assets/_Scratch/captioned_clip.mp4

By default it scans RawFootage/ for video files and picks a random one
(never the same one twice in a row) for each sentence. Pass --videos to
point at a different folder or an explicit list of files.

--lines format: a .txt file, one sentence per line, blank lines ignored.
Duration is optional per line -- if you don't specify one, it's auto-timed
from the word count/length (like real reading pace); to pin an exact
duration for a line, prefix it with "<seconds> | ":

    Wait, this game is actually insane.
    4.0 | You will not believe what happens next.
    Okay this part gets crazy fast.

A .json file also works: a list where each item is either a plain string
(auto duration) or {"text": "...", "duration": 4.0}.
"""

import argparse
import json
import random
import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

REPO_ROOT = Path(__file__).resolve().parent.parent
FONT_PATH = REPO_ROOT / "Tools/fonts/arialbd.ttf"
DEFAULT_VIDEO_DIR = REPO_ROOT / "RawFootage"
VIDEO_EXTS = (".mp4", ".mov", ".mkv", ".webm")

FPS = 30

# ---- word-reveal timing (seconds, scaled by --pace) ----
WORD_SEC = 0.30
MIN_WORD_SEC = 0.22
HOLD_SEC = 0.9        # extra hold once the full sentence is built (auto-duration lines)
FLOAT_SEC = 0.35      # float-up + fade-out transition once a sentence completes
LEAD_IN_SEC = 0.15    # blank beat after each cut, before that sentence's caption starts forming

# ---- caption layout (design reference: 1080px-wide video; scaled to actual width) ----
REF_W = 1080
BASE_FONT_SIZE = 72
HILITE_FONT_SIZE = 98
MAX_TEXT_WIDTH = 880
WORD_GAP = 18
LINE_PITCH = 98
CARD_PAD_X = 44
CARD_PAD_Y = 52
CARD_COLOR = (0, 0, 0, 150)
CARD_CENTER_Y_FRAC = 0.60
FLOAT_DISTANCE_FRAC = 0.14  # of video height

# ---- colour cycling: each sentence rotates to the next pair so consecutive
# cuts don't all look the same -- (base word colour, landing/highlight colour) ----
_WHITE = (255, 255, 255, 255)
_YELLOW = (255, 225, 60, 255)
_RED = (255, 64, 64, 255)
COLOR_PALETTES = [
    (_WHITE, _YELLOW),
    (_WHITE, _RED),
    (_YELLOW, _RED),
]

LINE_RE = re.compile(r"^\s*(\d+(?:\.\d+)?)\s*\|\s*(.+)$")


def word_duration(word: str, pace: float) -> float:
    return max(MIN_WORD_SEC, WORD_SEC * (0.6 + 0.15 * len(word) / 5)) * pace


class Word:
    __slots__ = ("text", "x", "y", "w", "h")

    def __init__(self, text, x, y, w, h):
        self.text, self.x, self.y, self.w, self.h = text, x, y, w, h


def layout_sentence(words, font_base, font_hilite, max_text_width, word_gap, line_pitch):
    """Greedy word-wrap. Every word reserves a slot sized to the LARGER of its
    base/highlighted rendering, so the temporary size bump never overlaps a
    neighbour regardless of which word is currently active."""
    dummy = Image.new("RGBA", (10, 10))
    d = ImageDraw.Draw(dummy)

    lines = [[]]
    cursor_x = 0
    for w in words:
        base_bbox = d.textbbox((0, 0), w, font=font_base)
        hilite_bbox = d.textbbox((0, 0), w, font=font_hilite)
        ww = max(base_bbox[2] - base_bbox[0], hilite_bbox[2] - hilite_bbox[0])
        wh = max(base_bbox[3] - base_bbox[1], hilite_bbox[3] - hilite_bbox[1])
        if lines[-1] and cursor_x + ww > max_text_width:
            lines.append([])
            cursor_x = 0
        lines[-1].append(Word(w, cursor_x, 0, ww, wh))
        cursor_x += ww + word_gap

    block_w = max((sum(word.w for word in ln) + word_gap * (len(ln) - 1) for ln in lines if ln), default=0)
    block_h = len(lines) * line_pitch

    for li, ln in enumerate(lines):
        line_w = sum(word.w for word in ln) + word_gap * (len(ln) - 1)
        x_off = (block_w - line_w) / 2
        cx = x_off
        for word in ln:
            word.x = cx
            word.y = li * line_pitch
            cx += word.w + word_gap

    return lines, int(block_w), int(block_h)


def render_state(lines, block_w, block_h, reveal_upto, font_base, font_hilite,
                  video_w, video_h, card_pad_x, card_pad_y, card_center_y,
                  base_color, hilite_color):
    """reveal_upto: global word index (0-based) of the current/highlighted word.
    Words with global index < reveal_upto are drawn at base size; == reveal_upto
    is drawn highlighted/larger; > reveal_upto are not drawn at all."""
    img = Image.new("RGBA", (video_w, video_h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    origin_x = (video_w - block_w) // 2
    origin_y = card_center_y - block_h // 2

    card_box = [origin_x - card_pad_x, origin_y - card_pad_y,
                origin_x + block_w + card_pad_x, origin_y + block_h + card_pad_y]
    draw.rounded_rectangle(card_box, radius=max(8, int(card_pad_x * 0.7)), fill=CARD_COLOR)

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
                          font=font_hilite, fill=hilite_color,
                          stroke_width=3, stroke_fill=(0, 0, 0, 255))
            else:
                bbox = draw.textbbox((0, 0), word.text, font=font_base)
                w2, h2 = bbox[2] - bbox[0], bbox[3] - bbox[1]
                draw.text((cx - w2 / 2 - bbox[0], cy - h2 / 2 - bbox[1]), word.text,
                          font=font_base, fill=base_color,
                          stroke_width=2, stroke_fill=(0, 0, 0, 255))
            gi += 1
    return img


def apply_float_fade(img: Image.Image, offset_y: int, alpha_mult: float) -> Image.Image:
    """Shift the fully-built sentence card upward by offset_y px and scale its
    alpha by alpha_mult -- used for the float-up-and-fade transition."""
    shifted = Image.new("RGBA", img.size, (0, 0, 0, 0))
    shifted.paste(img, (0, offset_y), img)
    if alpha_mult < 1.0:
        r, g, b, a = shifted.split()
        a = a.point(lambda v: int(v * alpha_mult))
        shifted = Image.merge("RGBA", (r, g, b, a))
    return shifted


def fit_word_durations(natural_durs: list[float], budget: float) -> tuple[list[float], float, bool]:
    """Fit word-reveal durations + hold into `budget` seconds. Returns
    (final_durs, hold, overflowed). If the words don't fit even at their
    MIN_WORD_SEC floor, they're kept at that floor and `overflowed=True`
    signals the caller that the actual duration will run a bit longer than
    requested rather than clipping words."""
    natural_sum = sum(natural_durs)
    if natural_sum <= budget:
        return natural_durs, budget - natural_sum, False

    min_sum = MIN_WORD_SEC * len(natural_durs)
    if min_sum >= budget:
        return [MIN_WORD_SEC] * len(natural_durs), 0.0, True

    scale = budget / natural_sum
    final = [max(MIN_WORD_SEC, d * scale) for d in natural_durs]
    return final, max(0.0, budget - sum(final)), False


def build_sentence_timeline(text: str, explicit_duration, pace: float, long_mult: float = 1.0):
    """Returns (words, word_durs, hold, total_duration, overflowed).

    long_mult > 1 stretches the CUT, not the caption: word-reveal pacing is
    unchanged, the fully-built card just holds longer before floating away,
    so the background clip stays on screen ~long_mult x its normal time."""
    words = text.split()
    natural_durs = [word_duration(w, pace) for w in words]
    overhead = LEAD_IN_SEC + FLOAT_SEC

    if explicit_duration is not None:
        budget = max(0.3, explicit_duration - overhead)
        final_durs, hold, overflowed = fit_word_durations(natural_durs, budget)
        total = explicit_duration if not overflowed else (overhead + sum(final_durs))
    else:
        final_durs, hold, overflowed = natural_durs, HOLD_SEC * pace, False
        total = overhead + sum(final_durs) + hold

    if long_mult > 1.0:
        extra = (long_mult - 1.0) * total
        hold += extra
        total += extra

    return words, final_durs, hold, total, overflowed


def render_sentence_captions(text: str, explicit_duration, video_w: int, video_h: int,
                              pace: float, work_dir: Path, idx: int,
                              long_mult: float = 1.0) -> tuple[Path, float]:
    base_color, hilite_color = COLOR_PALETTES[idx % len(COLOR_PALETTES)]
    scale = video_w / REF_W
    base_font_size = max(10, round(BASE_FONT_SIZE * scale))
    hilite_font_size = max(12, round(HILITE_FONT_SIZE * scale))
    max_text_width = round(MAX_TEXT_WIDTH * scale)
    word_gap = round(WORD_GAP * scale)
    line_pitch = round(LINE_PITCH * scale)
    card_pad_x = round(CARD_PAD_X * scale)
    card_pad_y = round(CARD_PAD_Y * scale)
    card_center_y = int(video_h * CARD_CENTER_Y_FRAC)
    float_distance = int(video_h * FLOAT_DISTANCE_FRAC)

    font_base = ImageFont.truetype(str(FONT_PATH), base_font_size)
    font_hilite = ImageFont.truetype(str(FONT_PATH), hilite_font_size)

    words, word_durs, hold, total, overflowed = build_sentence_timeline(text, explicit_duration, pace, long_mult)
    if overflowed:
        print(f"  [sentence {idx}] WARNING: too many words for the requested duration "
              f"even at minimum pace -- running {total:.2f}s instead.", file=sys.stderr)

    frames_dir = work_dir / f"caption_frames_{idx}"
    frames_dir.mkdir(parents=True, exist_ok=True)
    concat_lines = []
    frame_idx = 0

    blank = Image.new("RGBA", (video_w, video_h), (0, 0, 0, 0))
    blank_path = frames_dir / "blank.png"
    blank.save(blank_path)
    concat_lines.append(f"file 'blank.png'\nduration {LEAD_IN_SEC:.3f}\n")

    if not words:
        concat_lines.append("file 'blank.png'\n")
        list_path = frames_dir / "concat.txt"
        list_path.write_text("\n".join(concat_lines), encoding="utf-8")
        return _run_caption_concat(list_path, frames_dir, idx), total

    lines, block_w, block_h = layout_sentence(words, font_base, font_hilite, max_text_width, word_gap, line_pitch)

    full_img = None
    for gi, w in enumerate(words):
        is_last = gi == len(words) - 1
        state_dur = word_durs[gi] + (hold if is_last else 0.0)
        img = render_state(lines, block_w, block_h, gi, font_base, font_hilite,
                            video_w, video_h, card_pad_x, card_pad_y, card_center_y,
                            base_color, hilite_color)
        if is_last:
            full_img = img
        fname = f"f{frame_idx:04d}.png"
        img.save(frames_dir / fname)
        concat_lines.append(f"file '{fname}'\nduration {state_dur:.3f}\n")
        frame_idx += 1

    float_frame_count = max(1, round(FLOAT_SEC * FPS))
    frame_dur = 1.0 / FPS
    for fi in range(float_frame_count):
        p = (fi + 1) / float_frame_count
        eased = 1.0 - (1.0 - p) ** 3
        offset_y = -int(float_distance * eased)
        alpha_mult = max(0.0, (1.0 - p) ** 1.5)
        frame = apply_float_fade(full_img, offset_y, alpha_mult)
        fname = f"f{frame_idx:04d}.png"
        frame.save(frames_dir / fname)
        concat_lines.append(f"file '{fname}'\nduration {frame_dur:.3f}\n")
        frame_idx += 1

    concat_lines.append(f"file 'f{frame_idx - 1:04d}.png'\n")
    list_path = frames_dir / "concat.txt"
    list_path.write_text("\n".join(concat_lines), encoding="utf-8")

    return _run_caption_concat(list_path, frames_dir, idx), total


def _run_caption_concat(list_path: Path, frames_dir: Path, idx: int) -> Path:
    out_path = frames_dir.parent / f"captions_{idx}.mov"
    cmd = [
        "ffmpeg", "-y", "-v", "error",
        "-f", "concat", "-safe", "0", "-i", str(list_path),
        "-vf", f"fps={FPS}",
        "-c:v", "qtrle",
        str(out_path),
    ]
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        print(f"FFMPEG (captions, sentence {idx}) FAILED", file=sys.stderr)
        print(result.stderr[:3000], file=sys.stderr)
        sys.exit(1)
    return out_path


def probe_video(video_path: Path) -> tuple[int, int, float]:
    cmd = [
        "ffprobe", "-v", "error", "-select_streams", "v:0",
        "-show_entries", "stream=width,height,duration",
        "-show_entries", "format=duration",
        "-of", "json", str(video_path),
    ]
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0 or not result.stdout.strip():
        print(f"ERROR: ffprobe failed on {video_path}", file=sys.stderr)
        print(result.stderr[:2000], file=sys.stderr)
        sys.exit(1)
    data = json.loads(result.stdout)
    stream = data["streams"][0]
    w, h = int(stream["width"]), int(stream["height"])
    dur = stream.get("duration") or data.get("format", {}).get("duration")
    if dur is None:
        print(f"ERROR: could not determine duration for {video_path}", file=sys.stderr)
        sys.exit(1)
    return w, h, float(dur)


def discover_videos(tokens: list[str]) -> list[Path]:
    found: list[Path] = []
    for token in tokens:
        p = Path(token)
        if p.is_dir():
            for f in sorted(p.iterdir()):
                if f.suffix.lower() in VIDEO_EXTS:
                    found.append(f)
        elif p.is_file():
            found.append(p)
        else:
            print(f"WARNING: --videos entry not found, skipping: {token}", file=sys.stderr)
    # de-dupe, preserve order
    seen = set()
    out = []
    for f in found:
        rp = f.resolve()
        if rp not in seen:
            seen.add(rp)
            out.append(f)
    return out


def pick_video(pool: list[Path], last: Path) -> Path:
    if len(pool) == 1:
        return pool[0]
    choices = [p for p in pool if p != last] if last is not None else pool
    return random.choice(choices)


def extract_segment(video_path: Path, video_duration: float, seg_duration: float,
                     target_w: int, target_h: int, work_dir: Path, idx: int) -> tuple[Path, float]:
    max_start = max(0.0, video_duration - seg_duration - 0.1)
    start = random.uniform(0.0, max_start) if max_start > 0 else 0.0
    actual_dur = min(seg_duration, video_duration)

    out_path = work_dir / f"bg_{idx}.mp4"
    # scale-to-cover + centre-crop so videos of any source resolution/aspect
    # land on a consistent target size before compositing/concat.
    vf = (f"scale={target_w}:{target_h}:force_original_aspect_ratio=increase,"
          f"crop={target_w}:{target_h},fps={FPS}")
    cmd = [
        "ffmpeg", "-y", "-v", "error",
        "-ss", f"{start:.3f}", "-i", str(video_path),
        "-t", f"{actual_dur:.3f}",
        "-vf", vf,
        "-c:v", "libx264", "-pix_fmt", "yuv420p",
        "-c:a", "aac", "-ar", "48000", "-ac", "2", "-b:a", "128k",
        str(out_path),
    ]
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        print(f"FFMPEG (background segment {idx}, {video_path.name}) FAILED", file=sys.stderr)
        print(result.stderr[:3000], file=sys.stderr)
        sys.exit(1)
    return out_path, start


def composite_segment(bg_path: Path, captions_path: Path, duration: float, work_dir: Path, idx: int) -> Path:
    out_path = work_dir / f"segment_{idx}.mp4"
    cmd = [
        "ffmpeg", "-y", "-v", "error",
        "-i", str(bg_path),
        "-i", str(captions_path),
        "-filter_complex", "[0:v][1:v]overlay=format=auto:shortest=1[outv]",
        "-map", "[outv]", "-map", "0:a?",
        "-t", f"{duration:.3f}",
        "-c:v", "libx264", "-pix_fmt", "yuv420p", "-r", str(FPS),
        "-c:a", "aac", "-ar", "48000", "-ac", "2", "-b:a", "128k",
        str(out_path),
    ]
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        print(f"FFMPEG (composite segment {idx}) FAILED", file=sys.stderr)
        print(result.stderr[:3000], file=sys.stderr)
        sys.exit(1)
    return out_path


def concat_segments(segment_paths: list[Path], out_path: Path, work_dir: Path) -> None:
    list_path = work_dir / "final_concat.txt"
    list_path.write_text(
        "\n".join(f"file '{p.resolve().as_posix()}'" for p in segment_paths), encoding="utf-8")
    cmd = [
        "ffmpeg", "-y", "-v", "error",
        "-f", "concat", "-safe", "0", "-i", str(list_path),
        "-c", "copy",
        str(out_path),
    ]
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        print("FFMPEG (final concat) FAILED", file=sys.stderr)
        print(result.stderr[:3000], file=sys.stderr)
        sys.exit(1)


def load_script_lines(lines_path: Path) -> list[tuple[str, float | None]]:
    """Returns list of (text, explicit_duration_or_None)."""
    if lines_path.suffix.lower() == ".json":
        data = json.loads(lines_path.read_text(encoding="utf-8"))
        out = []
        for item in data:
            if isinstance(item, str):
                if item.strip():
                    out.append((item.strip(), None))
            else:
                text = item.get("text", "").strip()
                if text:
                    out.append((text, item.get("duration")))
        return out

    out = []
    for raw in lines_path.read_text(encoding="utf-8").splitlines():
        line = raw.strip()
        if not line:
            continue
        m = LINE_RE.match(line)
        if m:
            out.append((m.group(2).strip(), float(m.group(1))))
        else:
            out.append((line, None))
    return out


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--videos", nargs="+", default=[str(DEFAULT_VIDEO_DIR)],
                     help="folder(s) and/or file(s) to pick background clips from at random (default: RawFootage/)")
    ap.add_argument("--lines", type=Path, required=True, help=".txt (one sentence per line, optional 'seconds | text') or .json")
    ap.add_argument("--out", type=Path, required=True)
    ap.add_argument("--pace", type=float, default=1.0, help="multiplier on auto-timed word/hold durations; >1 = slower reveal")
    ap.add_argument("--long-sources", nargs="+", default=[],
                     help="case-insensitive filename substrings; cuts backed by a matching source "
                          "video hold ~--long-mult x longer on screen (word pacing unchanged)")
    ap.add_argument("--long-mult", type=float, default=2.0,
                     help="cut-duration multiplier applied when the picked source matches --long-sources (default 2.0)")
    ap.add_argument("--width", type=int, default=None, help="canonical output width (default: first source video's width)")
    ap.add_argument("--height", type=int, default=None, help="canonical output height (default: first source video's height)")
    ap.add_argument("--seed", type=int, default=None, help="random seed, for reproducible video picks")
    ap.add_argument("--keep-work-dir", action="store_true", help="don't delete intermediate frames/segments (for debugging)")
    args = ap.parse_args()

    if not shutil.which("ffmpeg") or not shutil.which("ffprobe"):
        print("ERROR: ffmpeg/ffprobe not on PATH.", file=sys.stderr)
        return 1
    if not FONT_PATH.exists():
        print(f"ERROR: font not found: {FONT_PATH}", file=sys.stderr)
        return 1

    if args.seed is not None:
        random.seed(args.seed)

    pool = discover_videos(args.videos)
    if not pool:
        print(f"ERROR: no video files found in --videos: {args.videos}", file=sys.stderr)
        return 1
    print(f"Video pool ({len(pool)}): {[p.name for p in pool]}")

    sentences = load_script_lines(args.lines)
    if not sentences:
        print("ERROR: no sentences found in --lines.", file=sys.stderr)
        return 1

    print("Probing source videos...")
    info = {p: probe_video(p) for p in pool}
    for p, (w, h, d) in info.items():
        print(f"  {p.name}: {w}x{h}, {d:.1f}s")

    ref_path = pool[0]
    target_w = args.width or info[ref_path][0]
    target_h = args.height or info[ref_path][1]
    print(f"Canonical output size: {target_w}x{target_h}")

    work_dir = Path(tempfile.mkdtemp(prefix="tiktok_captions_")) if not args.keep_work_dir \
        else args.out.parent / "_tiktok_captions_work"
    work_dir.mkdir(parents=True, exist_ok=True)

    segment_paths = []
    last_video = None
    total_runtime = 0.0
    long_subs = [s.lower() for s in args.long_sources]
    for idx, (text, explicit_duration) in enumerate(sentences):
        video_path = pick_video(pool, last_video)
        last_video = video_path
        _, _, video_duration = info[video_path]
        is_long = any(s in video_path.name.lower() for s in long_subs)
        long_mult = args.long_mult if is_long else 1.0

        captions_path, seg_duration = render_sentence_captions(
            text, explicit_duration, target_w, target_h, args.pace, work_dir, idx, long_mult)

        bg_path, start = extract_segment(video_path, video_duration, seg_duration, target_w, target_h, work_dir, idx)
        print(f"  [{idx}] \"{text[:40]}{'...' if len(text) > 40 else ''}\" "
              f"<- {video_path.name} @{start:.1f}s, {seg_duration:.2f}s{' (long cut)' if is_long else ''}")

        segment_path = composite_segment(bg_path, captions_path, seg_duration, work_dir, idx)
        segment_paths.append(segment_path)
        total_runtime += seg_duration

    args.out.parent.mkdir(parents=True, exist_ok=True)
    print(f"Concatenating {len(segment_paths)} segments ({total_runtime:.1f}s total)...")
    concat_segments(segment_paths, args.out, work_dir)

    if not args.keep_work_dir:
        shutil.rmtree(work_dir, ignore_errors=True)

    print(f"DONE -> {args.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
