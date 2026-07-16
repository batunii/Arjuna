# System: Distractor Content Pipeline (TikTok-style clips)

Last updated: 2026-07-16.

A second, exploratory source of Block A tablet distractor content, alongside the real study's
deterministic canvas reel ([TabletApp/distractor-reel.html](<../../TabletApp/README.md>)). Produces
real pre-rendered video clips — fast-cut gameplay/compilation footage with animated, word-by-word
"TikTok-style" captions — for piloting how genuinely high-fidelity short-form video feels as
peripheral distraction, compared to the procedural shapes/bursts. **Not the real study's checked-in
stimulus** (testing-strategy-v2 §10.1's "no improvised stimuli" applies to what a real session
plays; this pipeline is pilot/exploratory tooling, same status as `PilotTools/`).

---

## Files

| Path | Role |
|---|---|
| `RawFootage/` | Source clips, git-ignored (`/RawFootage/` in `.gitignore` — multi-GB, kept out of both git and `Assets/` so Unity never tries to auto-import them as VideoClips). Currently 4 clips, all cropped/scaled to the same ~0.59 portrait aspect ratio (640×1080 or 1080×1822): `video1_subwaysurfers.mp4`, `video2_whittling.mp4`, `video3_horror.mp4` (a Minecraft-parkour clip, name is a holdover from an early guess), `video4_afv_clips.mp4` (cropped from a 1080×1920 *AFV*-branded home-video compilation — broadcast content, not original gameplay footage like the other three, worth keeping in mind if this pool is ever used beyond local research/testing). |
| `Tools/tiktok_captions.py` | The generator (see below). |
| `Tools/clickbait_script_1.txt` … `_4.txt` | Example caption scripts (`_1`/`_2` ≈ 1 min each, `_3`/`_4` ≈ 5 min each) — original clickbait-hook + general-trivia one-liners, not copied from anywhere. |
| `Assets/_Scratch/tiktok_clip_1.mp4`, `tiktok_clip_2.mp4` | ~60s outputs from scripts 1/2. |
| `Assets/_Scratch/tiktok_5min_1.mp4`, `tiktok_5min_2.mp4` | ~300s outputs from scripts 3/4 — the two currently wired into `PilotTools/block-a-cpt.html`'s side margins (see below). |
| `PilotTools/video-reel.html` | Minimal muted/looping `<video>` player, parameterized by `?src=`, that plays these clips inside the pilot tool's side-margin iframes. |

## `Tools/tiktok_captions.py`

Cuts to a different random background clip for every sentence (never the same clip twice in a
row), burning word-by-word "karaoke" captions on top of each — words accumulate one at a time, the
newly-landed word pops bigger/highlighted then settles once the next word lands, on a translucent
rounded card. Once a sentence's hold finishes, the card floats upward and fades out before the next
clip cuts in.

```
python Tools/tiktok_captions.py --lines Tools/my_script.txt --out Assets/_Scratch/out.mp4
```

- `--videos` (default: scans `RawFootage/`) — folder(s) and/or file(s) to draw random clips from;
  new files dropped into `RawFootage/` are picked up automatically, no flag changes needed.
- `--lines`: a `.txt` file, one sentence per line. Duration is optional per line — plain text is
  auto-timed from word count/length; prefix a line with `<seconds> |` to pin an exact duration
  (e.g. `6 | Wait, this fact genuinely broke my brain.`). A `.json` list of strings/`{"text",
  "duration"}` objects also works. If a line's words don't fit the requested duration even at
  minimum per-word pace, it just runs a little long rather than truncating words.
- `--pace`: multiplier on auto-timed word/hold durations (used at `0.65` for the "fast, flashy"
  clips currently in `Assets/_Scratch/`).
- `--width`/`--height`: canonical output size (default: first source video's own dimensions).
  Every clip is scale-to-cover + centre-cropped to this size before compositing, so mismatched
  source resolutions/aspect ratios (e.g. `video4_afv_clips.mp4` at 1080×1822 vs. the others at
  640×1080) still concatenate cleanly.
- **Colour cycling:** each sentence rotates through 3 base/highlight colour pairs — White/Yellow →
  White/Red → Yellow/Red (`COLOR_PALETTES`, indexed by sentence position) — so consecutive cuts
  don't all look identical.
- **Font size:** `BASE_FONT_SIZE`/`HILITE_FONT_SIZE` (72/98 at the 1080px design-reference width,
  scaled proportionally to actual output width) — bumped up once already from an initial 58/78 pass
  that read too small.

Pipeline per sentence: render that sentence's caption frames (PIL) → encode as an alpha `.mov`
(`qtrle`) → extract a same-length background segment from a randomly-picked source video (random
start offset, scale-to-cover + crop to canonical size) → composite caption over background
(`ffmpeg overlay`, keeps the source clip's own audio) → concat all per-sentence segments
(`-c copy`, since every segment shares identical codec/dims/fps) into the final output.

## `PilotTools/video-reel.html`

Sibling to `TabletApp/distractor-reel.html` — same device-robustness conventions (Screen Wake
Lock, best-effort fullscreen skipped when `?embedded=1`, `touch-action: none`) — but plays a real
`<video autoplay loop muted playsinline>` instead of drawing procedural shapes. `?src=` picks which
clip. Muted + looped to match the real reel's "pure visual distractor, no audio" convention.

`PilotTools/block-a-cpt.html`'s setup screen has a **Distractor content** selector (`Video clips` /
`Procedural reel`), defaulting to `Video clips` — on load and on change it swaps the side-margin
iframe `src` between `video-reel.html?src=../Assets/_Scratch/tiktok_5min_{1,2}.mp4` and
`../TabletApp/distractor-reel.html`. See [PilotTools/README.md](<../../PilotTools/README.md>) for
the full layout (side videos sized/placed like tablets propped either side, framed main task area).

## Known constraints / not yet done

- `video3_horror.mp4`'s name doesn't match its actual content (Minecraft parkour) — a naming
  artifact from before the clip was previewed; harmless but worth renaming if it causes confusion.
- No photosensitivity-safety pass on these clips (unlike `distractor-reel.html`'s documented
  flash-timing limits) — they're real gameplay/compilation footage with their own cuts and text
  pop-ins, not analyzed against the same criteria. Relevant only if this content ever moves beyond
  local pilot use.
- `RawFootage/video4_afv_clips.mp4` is broadcast content (America's Funniest Home Videos), not
  original/gameplay footage like the other three — flagged here so it isn't mistaken for the same
  category of source material.
- No automated test/CI for `tiktok_captions.py`; verified so far by manual frame extraction and
  visual inspection only.

Related: [PilotTools/README.md](<../../PilotTools/README.md>), [TabletApp/README.md](<../../TabletApp/README.md>),
[Study Tooling](<study-tooling.md>).
