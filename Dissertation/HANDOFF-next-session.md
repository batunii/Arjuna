# Handoff — dissertation, next session

Written 2026-08-07. Start a **Windows PowerShell** session in the repo root, with the **Unity Editor
open and this project loaded**.

---

## 0. Why Windows, not WSL

The previous sessions ran in WSL and hit four separate walls. All of them disappear on Windows:

| Tool | WSL | Windows |
|---|---|---|
| Coplay MCP (Unity control) | server starts in WSL, can't reach the Editor on Windows | works |
| `metavr` | no linux-x64 binary at all | `win32-x64` supported |
| `adb` | not installed | `C:\Program Files\Unity\Hub\Editor\6000.0.61f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe` |
| `git` | **fails** — `git-lfs` missing, every command aborts | `C:\Program Files\Git\cmd\git.exe` + git-lfs present |

There is still **no LaTeX toolchain in WSL**, so the PDF has been rebuilt manually each time. Check
whether MiKTeX/TeX Live is on the Windows side; if so, `build.sh`/`build.bat` can be run directly.

**Coplay was registered at user scope on 2026-08-07** and verified to boot (FastMCP 3.4.6):

```
claude mcp add --scope user --transport stdio coplay-mcp \
  --env MCP_TOOL_TIMEOUT=720000 -- uvx --python ">=3.11" coplay-mcp-server@latest
```

It is in `~/.claude.json` (WSL side). If the Windows Claude Code install has its own config, run that
command again there. MCP servers load at **startup**, so it will not appear mid-session.

---

## 1. Where the dissertation stands

**`Dissertation/25377738-dissertation-submission/` is the only copy to edit.**

- 6 chapters, **21,384 words** of main text against a ~20,000 target. Appendix A is 2,754 and
  excluded from the count.
- Last clean build: 96 pages, 0 LaTeX errors, 0 undefined references, 0 overfull tables, no duplicate
  List-of-Tables entries, 64 bib entries clean in both directions, 21 labels / 21 references.
- Rewritten on 2026-08-06 from 34,086 words across 8 chapters. See `rewrite-factbase.md` and
  `pre-rewrite-snapshot-2026-08-06/`.

### Chapter map (older notes use the OLD numbers)

| New | Chapter | Was |
|---|---|---|
| 1 | Introduction | ch1 |
| 2 | Background and Related Work | ch2 |
| 3 | Design Space | ch3 |
| 4 | System Design and Implementation | ch4 + old ch5's detection bake |
| 5 | User Study: Design and Interim Results | old ch6 + old ch7 §7.2 |
| 6 | Discussion and Conclusion | old ch8 + old ch7 limitations/transfer |
| A | Study Protocol Artefacts | appendix + relocated protocol material |

Old ch5 (five benchmark specs, none run) was deleted. Old ch7 was dissolved.

### Assessed mark, against the real marking sheet

| Criterion | Weight | Mark |
|---|---|---|
| Problem statement, motivation, analysis | 10% | 78 |
| Background research and literature review | 15% | 75 |
| **Technical content and project execution** | **50%** | **65** |
| Testing, evaluation, critical analysis | 15% | 72 (pending the author's own read) |
| Report presentation and writing | 10% | 74 |
| **Weighted total** | | **≈ 69.8** |

**On the boundary.** The 50%-weighted criterion is held down by one thing: *no technical measurement
of the artefact exists*. Everything below flows from that.

---

## 2. What's left, in priority order

### 2.1 T1 — frame cost (about a day, low risk, biggest single lever)

Moves technical 65 → ~68, total → ~71.7.

Use **OVR Metrics Tool**, not Perfetto. Its CSV gives T1's four metrics directly:
`APP T` (µs, GPU frame time), `STALE`, `CPU U`/`GPU U`, `CPU L`/`GPU L`. Budget is **13.88 ms at
72 fps**. Note `CPU U` is the *worst core*, not an average.

1. Install OVR Metrics Tool from the Horizon Store. Preset **Basic**.
2. Start CSV recording (or use the in-app "Record all captured metrics to csv files" toggle):
   ```powershell
   $adb = "C:\Program Files\Unity\Hub\Editor\6000.0.61f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
   & $adb shell am broadcast -n com.oculus.ovrmonitormetricsservice/.SettingsBroadcastReceiver -a com.oculus.ovrmonitormetricsservice.ENABLE_CSV
   ```
3. **Passthrough scene, one continuous run**, headset worn, gentle head movement, ~2 min each:
   effects off → Soft Dark → Hard Dark → Blur. **Write down the wall-clock time of each switch.**
4. **Video scene, second run**: effects off → SignPop, ~2 min each.
5. Stop with the same broadcast but `DISABLE_CSV`, then:
   ```powershell
   & $adb pull /sdcard/Android/data/com.oculus.ovrmonitormetricsservice/files/CapturedMetrics/ C:\Users\syson\Downloads\metrics
   ```
6. Hand over the CSVs **and the switch timestamps**.

**Pre-stated criteria — do not change these after seeing the data.** Pass for the four study
configurations: 95th-percentile GPU frame time within budget and stale-frame rate < 1%. Blur is an
*interpretation band*, not pass/fail — its cost relative to Soft Dark is the price of Tier-3 camera
re-rendering, which is a design-space datum either way. A configuration missing budget is still a
reportable result.

### 2.2 T3 — legibility (an afternoon; the code is already wired)

Moves technical to ~71, total → ~73.

**Defends the claim the whole architecture rests on**: that native passthrough through the window
beats the Tier-3 camera re-render. Currently argued from spec sheets only.

The chart is already generated: **`C:\Users\syson\Downloads\acuity_chart_600mm.pdf`** — Landolt C,
logMAR 1.3 to 0.0 in 0.1 steps, sized for 600 mm. Print A4 at 100%, no scaling, verify the 50 mm bar.
(Ignore `acuity_chart_T3.pdf` — that was for a discarded method.)

**Method — two configurations, one continuous recording, headset never moves:**

1. Tape the chart flat at **600 mm**, roughly eye height.
2. Build and deploy to the headset.
3. Rest the headset on something stable facing the chart.
4. Record at the highest resolution MQDH offers (Cast 2.0 Full-Res or Cinematic 4K beats the 1080p
   default).
5. Effects off (native passthrough) ~10 s → press **X** ~10 s → **X** → **X**. Don't move it.
6. Hand over the file.

**Criterion, pre-stated:** the architecture is vindicated if the native path resolves **at least one
logMAR step finer** than the camera path.

**Why a screen recording is a valid instrument here.** Both paths share everything downstream of the
compositor, so the resolution difference originates upstream and survives into the capture. PCA is
1280×960, so recording above that means the camera path is not capture-limited. The native path may
be undersampled at 1080p (Quest 3 is 2064×2208/eye), which *understates* its advantage — conservative,
and the criterion is one-sided, so a lower bound answers it. State that bound in the write-up.

**Do not** use a laptop screen instead of paper: moiré is resolution-dependent, and the two paths
sample at different resolutions, so the artefact would differ between them.

**Watch on the day:** config B needs the PCA feed live. Confirm the view shows camera pixels, not
black, before the real take.

### 2.3 Three figure placeholders (same session as T3)

`\figplaceholder` remains in ch1 (×1) and ch4 (×2):

| Where | What | Capture |
|---|---|---|
| Ch 4 §4.5.4 | Mode triptych on one video frame | screencap works — video scene is app-rendered |
| Ch 1 | Three treatments from the user's viewpoint | shoot in the video scene, or record over passthrough |
| Ch 4 §4.3 | Hybrid composite: native window vs processed periphery | **needs passthrough visible** — record via the Meta app / MQDH |

Passthrough *is* captured by on-device recording (confirmed from `IRLFILTERS.mp4`, which shows
OutlinedDark over the real lab). The black-capture limitation applies to **Link and PC casting**, and
even that was fixed in Feb 2026.

Closing these takes presentation 74 → ~78.

### 2.4 Testing chapter review

The author said they would read Chapter 5 and report back. Its mark (72) is provisional pending that.

### 2.5 Optional: the last ~1,380 words

21,384 against ~20,000. Prose tightening is exhausted — further cuts need to be structural. Candidates,
in order of least damage: the failure museum (~460 words, but it is high-value and the skill
explicitly protects it), Chapter 2's comfort-limits section, Chapter 6's future-work list.

Ask the author before cutting. 7% over may be acceptable.

---

## 3. The T3 code change

All in `Assets/PassthroughCameraApiSamples/ShaderSample/Scripts/CameraSphereVignetteManager.cs`.
**Not a new mode** — no enum entry, no shader branch, no mode-cycle change. (The failure museum
records that touching that enum once broke a serialized scene value.)

| Line ~ | Change |
|---|---|
| 188 | `s_passthroughModeId` property ID |
| 204 | `s_noiseAmpId` — `_NoiseAmp` was never uploaded from the manager |
| 662 | `m_fullCoverTest` bool |
| 758 | **X button** toggles it, and calls `SetDebug()` to print `T3: CAMERA PATH` / `T3: NATIVE PATH` so the recording identifies its own segments |
| 536 | Identity-grading override while the toggle is on: `_MaxBlurRadius` 0, `_BlurContrastRestore` 0, `_NoiseAmp` 0, `_DesatDelay` 0.9, `_DesatCurveExp` 50 |
| 966 | `_PassthroughMode` ← 0 when on, so the camera feed covers the whole view |

Values are pushed **at runtime**, never written to the material, so the study configuration is
untouched and everything reverts when the toggle goes off. Gated behind `!m_studyMinimalUi`, so the
Block A study build cannot reach it. Braces, parens and quotes verified balanced; not yet compiled.

---

## 4. Sources of truth — read before writing any number

1. **`Dissertation/authored/RESULTS-FROZEN-2026-08-06.md`** — the authority for every statistic.
   Regenerated by `Tools/analysis/blocka_pooled.py` and `blockb_pooled.py`, never hand-edited. It
   carries a "superseded figures — do not use" table; any figure from that column appearing in the
   manuscript is a bug.
2. **`Dissertation/rewrite-factbase.md`** — verified facts with provenance: N = 14/13, the
   per-participant Block A pairs, band boundaries and their real sources, verified shader values,
   apparatus facts, LaTeX gotchas.
3. **`Dissertation/pre-rewrite-snapshot-2026-08-06/`** — the 8-chapter manuscript, including the
   deleted benchmark chapter with all five specs in full.

### Cautions

- **Block A is real hardware; Block B is the simulation.** There is no eccentricity or angle-band
  analysis in Block A and there must never be. Angle bands are Block B only.
- **The `<10°` band has two estimators that disagree in sign** — pooled +5.36, per-participant −1.92.
  Chapter 5 reports both and labels which is which. Don't collapse them.
- **P4's NoFilter 98.4% is experimenter-reported**, not measured. The only such value.
- **Three stale copies of the submission exist**: `25377738-Dissertation/`, `submission/`, and
  `25377738-dissertation-submission.zip`. All predate the figures and the rewrite; all have **zero**
  embedded images. Delete them, or the wrong PDF gets submitted.

---

## 5. Suggested order for the next session

1. Confirm Coplay tools are visible (Unity Editor open). If they time out, it's the WSL/Windows
   boundary — but on Windows this should not arise.
2. **T1** — install, capture, pull CSVs. Hand them over with switch times.
3. **T3** — print chart, build, one recording. Hand it over.
4. Same session: the three figure captures.
5. Then: write both results sections, regenerate the T1 bar chart, replace the three placeholders,
   rebuild, re-verify (word count, statistics against the frozen file, bib both directions,
   labels/refs, `thesis.log` for overfull alignments and duplicate LoT entries).

Expected after all of it: technical ~71, presentation ~78, **total ≈ 73**.
