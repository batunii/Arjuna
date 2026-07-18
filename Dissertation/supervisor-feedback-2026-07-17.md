# Supervisor Feedback — John Dingliana, 2026-07-17 (email)

Two emails following the in-person discussion. Each point below: John's suggestion (condensed),
current status, and what remains. Status reflects the codebase as of 2026-07-18
(branch `Test/Optimization`, commits `94ab7cf3` / `07c284bf`).

All points are explicitly **optional** ("take with a grain of salt and exercise your own
judgement") — but each maps to a concrete validity threat worth either fixing or discussing in
the dissertation (his point 6b: anything not fixed for time reasons should be *discussed*, not
hidden).

---

## Point 0 — Falsifiable hypotheses / don't accidentally "prove" the null

> Define the hypothesis clearly; design so the null isn't accidentally supported just because
> conditions are vague/obfuscating.

**Status: DONE (formal study).** `testing-strategy-v2.md` already has pre-committed directional
hypotheses (H1 interaction with SESOI, H2b as TOST equivalence with a 10 pp margin), a
manipulation check (MC1), pilot gates, and a decision table (§8.3) where every outcome — including
failure — is a conclusive row. Nothing to change; cite this when presenting.

## Point 1 — Blob targets pop out too much (ceiling risk) + YOLO artefacts

> Grey opaque spheres pop out; near-100% hit rates in both conditions would be uninformative.
> Reduce pop-out, but not so subtle that a fixating participant can't recognise the target.
> Also beware artefacts ("persistent YOLO-driven spotlights") distracting users or being
> mistaken for targets.

**Status: artefact half DONE; salience tuning OPEN (pilot-style).**
- The YOLO-artefact concern is addressed as of 2026-07-18: single-sample blips never render
  (lifetime-aware filter), speck-sized detections gated (`m_detectionMinSizeDeg`), 43 sub-second
  "flasher" lifetimes scrubbed from the bake after a full visual contact-sheet review (all 170
  surviving rendered traffic-light lifetimes are human-verified real), and windows open/close in
  sync with the object (no more late/leading windows). See
  `.agent-docs/systems/video-test-scene.md`.
- Blob salience: `BlobTargetController` exposes size (`m_blobSizeDeg` 1.0°), colour/alpha
  (`m_blobColor`, muted 0.5-alpha tone). Needs empirical tuning against the ceiling: if pilot
  hit-rates approach 100% in the no-filter mode, dim/shrink further. NOT yet piloted.

## Point 2 — Bifurcate analysis: central window vs peripheral (YOLO-driven) areas

> Success on central targets tests "helps drivers attend the road"; success on peripheral
> targets tests "the filter doesn't block critical peripheral events". Brightness induction may
> even RAISE peripheral salience. Optional (time permitting): decoy targets in trivially
> unimportant defocus areas, to test suppression of irrelevant attention.

**Status: OPEN — small, well-defined work.**
- The blob CSV (`blobtargets_*.csv`) has no region tag. Now that the harness locks a fixed
  window (±25°az/±15°el), region (inside window / near edge / periphery) is computable at
  selection time — add a `region` column and split hit-rates in analysis.
- Note for discussion: person-blob targets are now *by design* near-region only
  (predicted-strength ≥ 0.7 selection filter), i.e. the person test measures "noticing people
  entering/near the focus region", not all-periphery. Sign targets remain unconstrained.
- Decoy targets in defocus areas: NOT built. Cheap to add via the same lifetime pool
  (select from low-relevance areas), but only if time permits.
- "Brightness induction" (contrast against the dimmed surround raising salience): worth one
  line in the dissertation discussion either way.

## Point 3 — Targets in defocus areas must appear BEHIND the filter  ⚠ NOT FIXED

> If a target occurs in (or moves into) a defocus area it should be filtered/defocused too —
> otherwise it artificially pops out and you measure the experimental artefact, not the app.

**Status: OPEN — this is the most important unfixed item.** The blob is a separate world-space
sphere at render queue 4150, drawn *on top of* the vignette sphere (queue 3000) at fixed
colour/alpha regardless of mode or region. In Hard Dark, a blob in the blacked-out periphery is
fully visible — exactly the artefact John describes; in SignPop the blob is equally visible in
mode 1 and mode 2 over *dimmed* vs *natural* surroundings.
Fix direction: `BlobTargetController` queries the manager for the filter state at the blob's
az/el (inside window? inside a detection carve-out? current dim level) and scales the blob's
alpha/brightness to match what a real object there would suffer.
`VideoTestSceneManager.PersonPredictedStrength` and `ActiveRect` already expose most of the
needed geometry.

## Point 4 — Lock the window size for evaluation

> Letting users choose window size is a confound; keep it constant for the evaluation (fine as
> an end-application feature).

**Status: DONE.** Formal Block A: participant paints once, locked across all four conditions
(protocol §4.1); Block B window is pre-set and fixed. Informal harness: `TestModeSequencer` now
locks a fixed windscreen window (±25°az/±15°el, `m_lockVideoWindow`) on every video-mode entry —
identical geometry in filter-on and no-filter modes.

## Point 5 — Desktop study (PilotTools)

> (a) Avoid ceiling (perfect scores with no blocking). (b) Avoid/detect subjectivity: lock
> window size; add an independent outlier measure (e.g. count how often participants look
> outside the window). Since it's shorter, consider extra conditions, e.g. blur vs darkness.

**Status: OPEN — decide how much the desktop study matters first.**
`PilotTools/block-a-cpt.html` and `block-a-multitarget.html` exist as browser pilots. If they
graduate to a reported study: fix distractor difficulty against ceiling, hard-code the layout,
and add a cheap "looked away" proxy (e.g. mouse-leaves-task-area events, or webcam-free
attention checks). Blur-vs-dark as an extra condition is attractive because the desktop cost per
condition is low — mirrors the VR modes (Soft Dark vs Hard Dark vs blur).

## Point 6 — Shape task is bottom-up / reactive; working-memory alternative

> The shape-selection task measures reactive attention, not sustained/working-memory focus
> (studying/working). Example alternative: show shape sequences, then ask pseudo-randomised
> recall questions (what shape was green? what came after the square?…) with accuracy +
> completion time, randomised reveal speed. BUT: costs real implementation time and session
> length — be wary before committing.

**Status: OPEN DECISION — scope call, not code.** Honest options: (a) keep the reactive task
and explicitly scope claims to reactive attention (one paragraph in limitations); (b) build the
sequence-recall variant for the desktop study only (cheapest venue). John himself flags the
cost; default recommendation is (a) unless the desktop study becomes central.

## Point 6b (second "6") — Time constraints and post-study improvements

> Not everything can be perfect: discuss known-but-unfixed issues in the presentation and
> dissertation. Hedge: make a few improvements AFTER the experiments based on results/feedback —
> a positive feedback loop and extra technical contribution.

**Status: process note — adopt.** Keep `incidents.md` + this file as the source for the
"known limitations" section. Reserve a small post-study iteration (e.g. blob-behind-filter fix,
salience retune) to present as the improvement loop.

---

## Follow-up email — abstract the phone/distractor content (desktop study)

> Experiments needn't literally replicate reality. The phone *image* is fine, but what plays on
> it can be abstracted to something that challenges the task: colourful animation, clip-art
> shapes (NOT identical to the task shapes), or notification alerts with clickbait text.

**Status: LARGELY BUILT.** `Tools/tiktok_captions.py` + `Tools/distractor_lines.json` +
`Tools/clickbait_script_1.txt` already generate karaoke-caption clickbait reels
(`PilotTools/video-reel.html`, wired into `block-a-cpt.html` side margins). Gap vs the
suggestion: content is real gameplay/compilation footage, not shape clip-art — deliberately so
for `block-a-multitarget.html`, where circle/square distractor content would confound the
circle-vs-square task (John's own caveat: "albeit not identical to the ones in the task").
A shapes-based reel variant for the CPT (non-shape) task is an easy add if wanted.

---

## Priority order (suggested, next session)

1. **Point 3 — blob-behind-filter** (validity of the headline informal comparison; ~contained
   code change).
2. **Point 2 — region tag in blob CSV** (tiny; enables the central/peripheral split).
3. **Point 1 — blob salience pilot pass** (self-pilot, tune size/alpha against ceiling).
4. Points 5/6 — desktop-study scope decision with remaining time budget in view.
