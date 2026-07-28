# System: Distractor Content Pipeline (TikTok-style clips)

Last updated: 2026-07-27.

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
| `RawFootage/` | Source clips, git-ignored (`/RawFootage/` in `.gitignore` — multi-GB, kept out of both git and `Assets/` so Unity never tries to auto-import them as VideoClips). Currently 9 clips: the original 4, cropped/scaled to the same ~0.59 portrait aspect ratio (640×1080 or 1080×1822) — `video1_subwaysurfers.mp4`, `video2_whittling.mp4`, `video3_horror.mp4` (a Minecraft-parkour clip, name is a holdover from an early guess), `video4_afv_clips.mp4` (cropped from a 1080×1920 *AFV*-branded home-video compilation — broadcast content, not original gameplay footage like the other three) — plus 5 Bollywood music-video clips added 2026-07-23 (`video5_bolly_laila`, `video6_bolly_aajkiraat`, `video7_bolly_taras`, `video8_bolly_dilbar`, `video9_bolly_kusukusu`; YouTube-sourced 1080p landscape, middle sections only — first 30 s and last 10 s of each song trimmed off; the generator's scale-to-cover centre-crop handles the landscape→portrait conversion). Like `video4`, the Bollywood clips are broadcast/commercial content — fine for local research/pilot use, keep in mind if this pool is ever used beyond that. **+4 protest news Shorts added 2026-07-27** (`video10_protest_lathicharge`, `video11_protest_rahuldetained`, `video12_protest_jantarmantar`, `video13_protest_parliamentst`; YouTube Shorts, 1080×1920 portrait, 30–47 s each, used whole — no trim needed at that length; Delhi protest / police-clash news footage, same broadcast-content caveat) **and +5 ad/meme Shorts later the same day** (`video14_ad_megasaving` 1200², `video15_ad_perfume` 2160², `video16_ad_sale50` 720×1280, `video17_football_prank`, `video18_dogs_eating`; 11–27 s, used whole — the square/small sources are fine, every clip is scale-to-cover centre-cropped anyway, and even 11 s exceeds the ~5–7 s normal cut length) → pool now 18 clips. |
| `Tools/tiktok_captions.py` | The generator (see below). |
| `Tools/clickbait_script_1.txt` … `_4.txt` | Example caption scripts (`_1`/`_2` ≈ 1 min each, `_3`/`_4` ≈ 5 min each) — original clickbait-hook + general-trivia one-liners, not copied from anywhere. |
| `Assets/_Scratch/tiktok_clip_1.mp4`, `tiktok_clip_2.mp4` | ~60s outputs from scripts 1/2. |
| `Assets/_Scratch/tiktok_5min_1.mp4`, `tiktok_5min_2.mp4` | Outputs from scripts 3/4 over the full 9-source pool with `--long-sources bolly` (~8 min each since 2026-07-23) — the "New videos" option in `PilotTools/block-a-cpt.html`'s distractor-set selector (the default). |
| `Assets/_Scratch/tiktok_5min_3.mp4`, `tiktok_5min_4.mp4` | Same scripts rebuilt from the original 4 sources only (no Bollywood clips, no long cuts, ~300s) — the "Original videos" option in the selector, for runs that want the pre-2026-07-23 distractor content. |
| `Assets/_Scratch/tiktok_5min_5.mp4`, `tiktok_5min_6.mp4` | Same scripts over the full 18-source pool (incl. the 4 protest + 5 ad/meme Shorts) with `--long-sources bolly --seed 5/6` (2026-07-27) — the "Newest videos (protest + ad/meme clips)" option, the selector default, stamped `distractors=PROTEST`. Built first from 13 sources, then regenerated in place from 18 the same day — safe because no PROTEST-stamped run existed yet, so the stamp has exactly one meaning. `_1/_2` were deliberately NOT regenerated, so day-2 `NEW` runs stay byte-identical to what their participants saw. |
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
- `--long-sources` / `--long-mult` (added 2026-07-23): filename substrings (case-insensitive)
  marking sources whose cuts should stay on screen longer — when the random pick lands on a
  matching source, that sentence's post-build caption hold is extended so the cut runs
  `--long-mult`× (default 2.0) its normal length; word-reveal pacing is untouched. Used as
  `--long-sources bolly` for the current reels, so the five Bollywood music-video clips hold
  ~2× longer than the gameplay clips (they're the richer distractor). Side effect: reel length
  grows with the share of long cuts (the nominal 5-min reels now run ~7–8 min); harmless — the
  player loops, and a longer reel just repeats less within a 3:30 run.
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

`PilotTools/block-a-cpt.html`'s side-margin iframes load `video-reel.html?src=...&embedded=1`
pointing at one of three reel pairs, chosen by the setup screen's **Distractor videos** selector
(added 2026-07-23, third pair 2026-07-27): "Newest videos (protest + ad/meme clips)" =
`tiktok_5min_{5,6}.mp4` (default), "New videos" = `tiktok_5min_{1,2}.mp4`, "Original videos" =
`tiktok_5min_{3,4}.mp4`. The screens stay dark until the run starts (and re-darken at run end);
the choice is stamped into the run-start payload (`distractors=PROTEST|NEW|ORIGINAL`). This selector is
narrower than the one removed 2026-07-22 (video clips vs procedural reel — that decision stands);
it only picks which clip pool the reels were built from. See
[PilotTools/README.md](<../../PilotTools/README.md>) for the full layout (side videos
sized/placed like tablets propped either side, framed main task area).

## Known constraints / not yet done

- `video3_horror.mp4`'s name doesn't match its actual content (Minecraft parkour) — a naming
  artifact from before the clip was previewed; harmless but worth renaming if it causes confusion.
- No photosensitivity-safety pass on these clips (unlike `distractor-reel.html`'s documented
  flash-timing limits) — they're real gameplay/compilation footage with their own cuts and text
  pop-ins, not analyzed against the same criteria. Relevant only if this content ever moves beyond
  local pilot use.
- `RawFootage/video4_afv_clips.mp4` (America's Funniest Home Videos), the five
  `video[5-9]_bolly_*` music-video clips, the four `video1[0-3]_protest_*` news Shorts, and the
  five `video1[4-8]_*` ad/meme Shorts are broadcast/commercial content, not original/gameplay
  footage like the first three — flagged here so they aren't mistaken for the same category of
  source material (local research/pilot use only).
- The protest Shorts are real news footage of police/protester clashes (lathi charge, detentions,
  injuries) — emotionally salient by design (that's the distractor rationale), but worth a thought
  for participant comfort/ethics if this pool is used beyond self-pilots.
- No automated test/CI for `tiktok_captions.py`; verified so far by manual frame extraction and
  visual inspection only.

Related: [PilotTools/README.md](<../../PilotTools/README.md>), [TabletApp/README.md](<../../TabletApp/README.md>),
[Study Tooling](<study-tooling.md>).
