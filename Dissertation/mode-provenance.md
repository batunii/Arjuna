# Mode Provenance — the 10 documented filters

Created 2026-07-29. One row per documented filter: what proposes it, whether it was evaluated, and
if not, why not.

**TintedDark was removed from the documented set on 2026-07-29** — it had no proposing paper and no
mechanism the dark family didn't already have, so there was nothing to defend. The set is **ten**.
The `VignetteMode` enum and the `_TintMode` shader branch still carry it; the code was left alone
deliberately, since deleting the enum entry would break the serialized scene value that currently
selects it. Removed from the corpus, retained as dead code.

This is a **provenance audit**, not a design document. Every claim below is traced to a file and
line in this repo; where the corpus marks a citation unverified I carry the `[VERIFY]` marker
through rather than laundering it. Where a mode has *no* proposing paper I say so plainly instead
of retro-fitting one.

## How to read the status column

| Status | Meaning |
|---|---|
| **Evaluated** | Carries a confirmatory claim in the user study. |
| **Evaluated (subjective only)** | Sampler-level Likert data, no confirmatory block. |
| **Superseded** | Was the working mode; retired on evidence. Still in shader + enum. |
| **Exploratory** | Literature-derived design point, in shader + enum, no evaluation claim. |
| **Instrument** | Built to serve the study apparatus rather than as a candidate mode. |

## Summary

Numbers below are `VignetteMode` **enum values**, so 3 is absent — that was TintedDark, removed from
the documented set. Ten rows, values 0-2 and 4-10.

| Enum | Mode | Status | Proposed by / reason to exist | Why not carried forward |
|---|---|---|---|---|
| 0 | **Blur** | Superseded | Patney et al. 2016 (contrast restoration); Tariq et al. 2022 `[VERIFY]` (perceptual noise) | Grogorick et al. 2018: naive whole-periphery blur announces itself through contrast collapse. Also the heaviest shader path. |
| 1 | **SoftDark** | Evaluated (subjective only) | Fernandes & Feiner 2016; Lin et al. 2020; Teixeira & Palmisano 2021 | A full confirmatory block doesn't fit the ~53 min session budget. |
| 2 | **HardDark** | **Evaluated** (Block A) | Strongest available manipulation — maximises detection probability | — (selected) |
| 4 | **ChromaticCool** | Exploratory | Bailey et al. 2009 — SGD's warm–cool chromatic modulation | It is the *losing arm of its own source study*. Luminance beat warm–cool chroma. |
| 5 | **ColorPop** | Superseded by SignPop | Sutton et al. 2022 (saliency modulation + sigmoid recipe); channel choice from Bailey 2009, Grogorick et al. 2017 | Pops *any* red/orange/green. The tested form is SignPop, which gates it to real signals. |
| 6 | **ConspicuitySqueeze** (UI: **FLATTEN**) | Exploratory | Veas et al. 2011 — flatten centre–surround contrast toward the local mean | **Rejected on pilot observation:** flattening toward the local mean *brightens* the periphery. Mechanism confirmed in shader — see below. |
| 7 | **GranulatedPeriphery** (UI: **GRAIN**) | Exploratory | Cao, Grandi & Kopper 2021 — granulated rest frames; Size 1–7° and Density 25–75 % taken directly | Lost an informal head-to-head against SignPop (less subtle); grains occlude *detail* beneath the pattern, which is the wrong failure mode for driving. Formal test dropped for scope/time. |
| 8 | **OutlinedDark** | Exploratory | Cheng et al. 2022 — context-preserving outlines reduce anxiety | **Rejected on pilot observation:** the outlines read as a comic-book style that testers found more interesting than the task. |
| 9 | **SpotLift** | Exploratory | **No paper.** Focus brightness lift over a soft peripheral dim | Excluded by *platform*, not theory: OS passthrough cannot be brightened, so it is video-only. |
| 10 | **SignPop** | **Evaluated** (simulation arm) | **No paper.** ColorPop gated to detected signals | — (selected; it is the advanced form of ColorPop) |

---

## 0 — Blur

**Does:** periphery re-rendered from the camera feed with progressive Gaussian blur and delayed
desaturation. Two-ring Gaussian (centre + 8 taps at r + 8 at 2r, σ = r); the fine/coarse difference
doubles as a local Laplacian driving a world-anchored perceptual noise term; a contrast-restoration
term re-amplifies residual local contrast (`_BlurContrastRestore` = 0.8). Desaturation starts at
t = 0.3, blur only at t = 0.5 — colour mutes before structure softens (ch4-system.md:283-298).

**Proposed by:** Patney et al. (2016) for the contrast-restoration term; Tariq et al. (2022)
`[VERIFY]` for the noise term. Neither *proposes* blur as a diminishment mode — both are foveated-
rendering papers whose findings were imported to make an already-chosen mode tolerable.

**Why not carried forward.** This is the best-documented rejection in the corpus, and it is
mechanistic rather than aesthetic. Patney established that peripheral blur is detected *primarily
through the contrast reduction it causes*; Grogorick et al. (2018) then found naive whole-periphery
blur irritating. ch2-related-work.md:328-332 draws the conclusion explicitly:

> This chain of evidence is why the present system's superseded Blur mode was retired and why its
> dark modes attenuate luminance (which the periphery tolerates) rather than blurring structure
> (which it resents).

Two aggravating factors: Walton et al. (2021) `[VERIFY]` show ventral metamers degrade far more
gracefully than Gaussian blur, so blur is not even the best structural manipulation available; and
Blur is the heaviest shader path in the family — 17 taps per camera, doubled near the stitch
(ch4-system.md:316-318).

## 1 — SoftDark

**Does:** identical to HardDark but with the alpha ceiling at `m_mode2MaxAlpha` = 0.75, so peripheral
motion and events stay visible through the veil. Forms gradually over `m_vignetteFormTime` = 3 s.

**Reason to exist.** Four grounds, set out at ch6-user-study.md:212-225. It is the **minimal
manipulation** — pure luminance attenuation, the only unconfounded member of the family, where
ColorPop bundles desaturation, re-grading, glare compression and dimming; the graded **Tier-2** point
(dimmed *real* passthrough at native quality); the only mode with **no camera pipeline**, hence the
only one plausibly deployable for hour-long sessions; and the **awareness-preserving** midpoint where
the focus-versus-awareness trade-off actually lives.

Supporting literature: Fernandes & Feiner (2016) — soft, dynamically applied FOV vignette reduces
discomfort with no measurable presence loss; Lin et al. (2020) — static peripheral blur likewise;
Teixeira & Palmisano (2021) — reductions hold over repeated 10-minute exposures.

**Why not fully evaluated.** Session budget, stated as an explicit trade-off: *"A full confirmatory
block for Soft Dark does not fit the session budget; it therefore receives sampler-level subjective
data only"* (ch6-user-study.md:224). Appears in Block C only.

## 2 — HardDark — **evaluated, Block A**

**Does:** `periColor = black`, `_MaxVignetteAlpha` = 1.0. At full formation the periphery is entirely
occluded. `_SimpleMode = 1` samples no textures and takes an early return — negligible GPU cost, no
camera permission, no thermal exposure.

**Reason to exist / why selected.** Not a literature import but a study-design choice:
*"preferred over Soft Dark for the confirmatory test because it is the strongest manipulation,
maximising the probability of detecting the mechanism if it exists"* (ch6-user-study.md:203-206).
Paired with the workstation paradigm because dark modes *remove* peripheral signal and that paradigm
measures what removal should buy — reduced distractor cost.

**Counter-evidence accepted going in:** Barhorst-Cates et al. (2016) — anxiety elevated in *all*
restricted conditions, rising monotonically as the field narrows; Biocca et al. — attention
tunnelling. The system does not answer these; it acknowledges them (ch2-related-work.md:352-363).

## 4 — ChromaticCool

**Does:** shares Blur's shader branch. Periphery keeps its chroma and shifts cool
(`coolTint = {0.70, 0.85, 1.00}`); focus window shifts warm — **video only**, since passthrough's
focus is transparent and cannot be tinted.

**Proposed by:** **Bailey, McNamara, Sudarsanam & Grimm (2009), "Subtle Gaze Direction," ACM TOG
28(4)** — starred as a base paper at lit-review-48papers.md:197-200. The warm–cool chromatic
modulation is theirs.

**Why not carried forward — the sharpest rejection in the set.** It is the *losing arm of its own
source study*. SGD tested luminance modulation against warm–cool chromatic modulation and luminance
won (ch2-related-work.md:183). That result generalises: *"in every head-to-head comparison in the
guidance literature, luminance manipulations outperform chromatic ones (Bailey et al., 2009;
Grogorick et al., 2017; Waldin et al., 2017)"* (ch2-related-work.md:38-40) — the luminance-dominance
law. Stewart et al. (2020) `[VERIFY]` close it off from the perceptual side: the periphery notices
contrast loss and motion change most, and **chromatic detail loss least** (ch2-related-work.md:315).

A mode whose entire mechanism sits in the channel the periphery notices least, tested head-to-head
against luminance and beaten, is correctly excluded.

**Implementation note (2026-07-29).** Until today the shader implemented only the cool half, and
applied it *after* Blur's full desaturation — so the tint acted on an already-grey periphery, and
the mode was visually indistinguishable from Blur. It was running Blur's luminance manipulation with
a rounding error on top. Now split by channel: Blur keeps full desaturation, ChromaticCool retains
chroma (`_CoolDesatScale` = 0.15) and adds the warm counterpart in the focus window
(`_WarmStrength` = 0.5, video only). The warm–cool *opposition* is what the source relies on;
neither tint alone reproduces it.

## 5 — ColorPop — **superseded by SignPop**

**Does:** four-level salience re-grading of the camera feed —
`outside-other < outside-ROG < inside-other < inside-ROG` — gated by red/orange/green membership
(the colour classes of signal lamps and signage) and interpolated by the window coordinate.
Includes glare compression with a blown-core guard, and a global periphery dim
(ch4-system.md:223-281).

**Proposed by:** **Sutton, Langlotz, Plopski, Zollmann, Itoh & Regenbrecht (2022), "Look over
there!", UIST 2022** — the modern anchor for saliency modulation. Its published sigmoidal
midtone-contrast recipe is used directly (α = 10, β = 0.5, normalised; ch4-system.md:249-251). The
decision to work luminance and saturation with **no hue shifts** comes from Bailey et al. 2009 and
Grogorick et al. 2017 — the same luminance-dominance law that excluded ChromaticCool
(ch4-system.md:226-227).

**Why selected.** On-device comparison across the whole mode family:
*"ColorPop was the standout and became the dissertation's dynamic-scenario mode"*
(ch4-system.md:228-229). Paired with the probe paradigm because it re-grades salience rather than
removing signal — measuring what re-grading buys (faster focal detection, H2a) and what it must not
cost (peripheral awareness, H2b).

**Its own limitation, recorded:** it is the Tier-3 mode, and *"the mode with the richest effect is
the one whose Tier-3 pipeline carries the platform's thermal ceiling"* (ch4-system.md:322-323).

**Why the tested mode is SignPop, not ColorPop.** ColorPop's keep is defined by *colour class* — a
hue band plus a saturation floor. That makes it indiscriminate: any sufficiently saturated red passes,
so brake lights, neon signage, advertising hoardings and a red jacket all pop exactly as brightly as
a traffic signal. The claim under test is not "red things become easier to see"; it is that
**road-relevant signals** become easier to see. A filter that pops any red cannot support that claim
— the periphery fills with false positives and the effect stops being selective, which is the whole
premise of the noise-cancelling analogy the deck runs on (slide 37: good noise cancelling lets *the
announcement* through, not every sound).

SignPop is therefore not a variant to be compared against ColorPop — it is ColorPop with the defect
corrected, and it is the form that was evaluated. ColorPop remains reachable because the gate is a
single uniform (`_PopDetGate` = 0), which makes it the natural ablation control if the difference
between "any red" and "detected signals only" ever needs measuring directly.

## 6 — ConspicuitySqueeze (UI: "FLATTEN")

**Does:** flattens peripheral centre–surround contrast toward the local mean, plus gentle
desaturation and a slight dim.

**Proposed by:** **Veas, Mendez, Feiner & Schmalstieg (2011), "Directing Attention and Influencing
Memory with Visual Saliency Modulation," CHI 2011** — cited by name in the shader and at
ch4-system.md:327-328.

**Why not carried forward — rejected on pilot observation, with a mechanical cause.** In use the
periphery went **milky and brighter** rather than merely uncompetitive, and the brightening itself
drew attention — the opposite of what the mode is for.

That observation is not merely subjective; the shader explains it. `CameraSphereVignette.shader:792`
pulls each pixel's luminance toward the local mean:

```hlsl
float outLum = lerp(lumC, lumM, _SqueezeLum * tEff);
```

Any pixel *darker* than its neighbourhood is therefore lifted, and `_SqueezeLum` = 0.5 moves it
halfway. The only compensating darkening is `_SqueezeDim` = 0.9 at line 800 — a 10 % dim at full
eccentricity. In any scene containing dark regions the lift dominates the dim, so the periphery
brightens overall. The shader comment even states the intent — *"Goal is 'nothing pops out', not
darkness"* — which is precisely why it was left too bright.

This is the one mode where the implementation, not the source paper, is on trial. Veas et al.
modulated saliency on a controlled display; flattening toward a local mean on a live camera feed of
an unconstrained scene is a different proposition, and the brightening is an artefact of the port
rather than a fault in the original finding. A fairer test would raise `_SqueezeDim` (or clamp the
luminance lift to non-positive) before concluding anything about the technique.

Worth recording that this is otherwise the *purest* implementation of the salience model the
dissertation rests on: Itti–Koch make contrast the master variable and *"any manipulation that
flattens it repels gaze"* (ch2-related-work.md:36-37), and this mode flattens contrast and nothing
else.

## 7 — GranulatedPeriphery (UI: "GRAIN")

**Does:** world-locked static noise grains whose density ramps with eccentricity
(`_GrainDensityMin` 0.25 → `_GrainDensityMax` 0.75, `_GrainScale` 90).

**Proposed by:** **Cao, Grandi & Kopper (2021), "Granulated Rest Frames Outperform Field of View
Restrictors on Visual Search Performance," Frontiers in Virtual Reality 2:604889** (verified against
the PDF, 2026-07-30) — parameters taken directly: Cao's Size is the degrees of FOV each grain covers
(levels 1°, 4°, 7°) and Density spans 25–75 %, so `_GrainDensityMin/Max` 0.25 → 0.75 reproduces
Cao's density range exactly.

**Naming.** Enum `GranulatedPeriphery`, shader toggle `_GrainMode`, on-screen label **GRAIN**. Nothing
in the running build says "Granulated".

**Why not carried forward — an informal head-to-head, then a scope decision.** Before the study was
designed, GRAIN and ColorPop/SignPop were shown side by side to the **first few early candidates**
(informal viewers during development — *not* the pilot participants of Part 8, a distinction worth
keeping in the write-up since the two groups carry different evidential weight). Two things came back:

1. **SignPop felt more subtle and simply better.** The pop-based filter changed the scene without
   announcing that a filter was present; the grains announced themselves.
2. **The grains read as potentially annoying, specifically for driving** — the pattern *hides detail
   beneath itself*. In a road scene the thing you need is often fine detail: which state a light is
   in, what a sign actually says. A filter that stipples over that is failing at the one job the
   scenario cares about.

A formal comparison remained possible and was **explicitly dropped for the thesis's scope and
timeline**, not because the question was uninteresting.

**What Cao actually shows.** The comparator is a **field-of-view restrictor** (the Fernandes &
Feiner 2016 technique), *not* a blackout, and the outcome is **visual search performance** — search
time and amount of head rotation, N=20 within-subjects. Six of the nine GRF variants produced
significantly fewer search movements than the FOV restrictor, and **15 of 20 participants preferred
GRFs**, reporting that they could still use peripheral vision to find targets (11 of those 15 voted
against the highest Density). So the finding is about preserving awareness that *something is there*
while an occluder is present.

Note what Cao does **not** establish. Despite the mode's origin as a cybersickness technique, this
paper is explicit that *"the effect of GRFs on reducing VIMS remains to be determined by future
work."* It is a search-performance and preference result, not a cybersickness result, and should not
be cited as one.

**Why this answers Cao rather than merely deferring.** The driving scenario needs something Cao did
not measure: legibility of the signal **itself**. Grains preserve event presence while degrading
exactly the fine detail this task depends on, so Cao's result doesn't transfer to it cleanly. That is
a limit on the transfer of a valid finding to a different task, which is a much stronger position
than "we ran out of time" — and it is worth stating in those terms, because the alternative reading
(that the study ignored a paper saying its tested mode was beaten) is the one an examiner who knows
Cao will otherwise reach for.

The residual honesty: the head-to-head here was informal and against **SignPop**, judged by eye.
Cao's own comparison — grains versus a FOV restrictor — was never reproduced, and neither was grains
versus HardDark. HardDark is a *stronger* manipulation than Cao's comparator, so it is not a stand-in
for it in either direction.

## 8 — OutlinedDark

**Does:** near-blackout with monochrome luminance edges added back
(edge = |centre − 4-tap local mean|, `_EdgeGain` 12, `_OutlineDimAlpha` 0.92).

**Proposed by:** **Cheng, Yin, Yan, Gugenheimer & Lindlbauer (2022), "Towards Understanding
Diminished Reality," CHI 2022** — specifically the finding that context-preserving outlines reduce
anxiety (ch4-system.md:329-331).

**Why not carried forward — rejected on pilot observation.** The restored edges read as a
**comic-book / cel-shaded style**, and pilot testers found that style *interesting* — which in this
context means distracting. The mode became the thing being looked at rather than the thing that lets
you look elsewhere.

This is a coherent rejection, and it is worth stating as a finding rather than a preference: Cheng et
al. established that people *want* a hint of what was removed, and this mode supplies one — but the
hint has a visual style of its own, and style competes for attention. The lesson generalises to any
diminishment that *adds* structure back: whatever you add must be less interesting than what you
removed. `_EdgeBrightness` (0.35) and `_EdgeGain` (12) are the two knobs that control how stylised
the edges look, and neither was swept before the mode was set aside — so the finding is about this
setting, not about outlines in principle.

Note this does **not** discharge the anxiety question. Barhorst-Cates et al. (2016) documented an
anxiety cost for blackout, HardDark carries it, and OutlinedDark was the candidate answer. Rejecting
the candidate on distraction grounds leaves the anxiety liability of the *tested* mode unaddressed.

## 9 — SpotLift (UI: "SPOTLIGHT")

**Does:** focus window brightened (`_SpotLiftAmp` 0.08) over a soft peripheral dim
(`_SpotDimAlpha` 0.35).

**Reason to exist. No proposing paper.** It is the additive inverse of the family's logic: every
other mode degrades the periphery, this one privileges the centre.

**Why not carried forward — the only purely technical exclusion.** *"video mode only, since OS
passthrough cannot be brightened"* (ch4-system.md:331-332). The brightness lift lives in the
video-mode output path; in passthrough the focus window is transparent and the OS compositor owns
those pixels. Not rejected on evidence — unavailable on the target platform.

Worth noting it is the one mode pointing at the direction ch7-discussion.md:173 identifies as the
way forward — *"toward the additive salience modulation"* — and it cannot run on the hardware.

## 10 — SignPop — **evaluated, the simulation arm**

**Does:** ColorPop with the ROG keep gated by the detection track. Only confirmed lights/signs get
the full kept treatment; undetected ROG (neon, ads, brake lights — or a bake miss) degrades to
`_PopDetFallback` = 0.35, *dimmed but never hidden*. Gate = 0 is plain ColorPop
(CameraSphereVignette.shader:700-705).

**Reason to exist. No proposing paper — it is the advanced form of ColorPop.** The design argument is
given in full under ColorPop above: *any red should not come through, only actual signals should*.
Colour class alone cannot make that distinction, so the keep is gated by the detection track.

The graceful-miss fallback carries equal weight: undetected red/orange/green degrades to a partial
keep (`_PopDetFallback` = 0.35), *dimmed but never hidden*. A real signal the detector missed must not
end up **less** visible than it would have been with no filter at all — a filter that can hide a
traffic light it failed to recognise is not shippable, whatever it does on average.

**This is the evaluated filter**, not an instrument sitting beside the study: `AuthoredTargetPresenter.cs:97`
(`m_filterMode = VignetteMode.SignPop`) and `TestModeSequencer.cs:62-66`. Every
`authored_results_*_FILTER-*.csv` in `Dissertation/authored/raw/` was produced with it, and the deck's
simulation result (+18.6 points in the 20–30° ring) is a SignPop result.

---

## Gaps this audit turned up

**Gap 1 — SignPop is undocumented in Chapter 4.** ch4-system.md:190 opens with *"The mode
enumeration in the codebase (`VignetteMode`) contains ten entries; three are evaluated in this
dissertation, one is their superseded ancestor, and six are retained design-exploration variants."*
Three + one + six = ten, and the enum holds **eleven**. SignPop is the missing one, and it is not a
minor omission: it is the filter the driving-scenario data was collected under. Chapter 4 currently
describes a system whose evaluated filter it never mentions.

**Gap 2 — the ch4 claim about the video cycle is now false.** ch4-system.md:333-335 states the video
mode cycle *"was deliberately trimmed to the three evaluated modes (`k_modeCycle = {ColorPop,
SoftDark, HardDark}`) on 2026-07-04, with the shader branches kept intact for the passthrough
scene."* As of 2026-07-29 both cycles carry all eleven modes. No evaluation claim depends on this —
the study path sets modes explicitly via `StudySetMode` and holds `StudyInputLock` — but the sentence
needs replacing with one that says the cycle was reopened for free-play exploration while the
evaluated set stayed at three.

**Gap 3 — every exclusion now has a reason, but the reasons are of unequal evidential weight.**
As of 2026-07-29 nothing is unexplained. The tiers are:

| Basis | Modes |
|---|---|
| Published evidence | Blur, ChromaticCool |
| Informal pre-study comparison | GRAIN |
| Pilot observation | FLATTEN, OutlinedDark |
| Platform constraint | SpotLift |
| No mechanism to defend | TintedDark (removed from the set) |

This is enough to support slide 47's claim that the filters were *"rejected on evidence, not on
taste"* — but only if the tiers are named. Three of the six rest on observation rather than data, and
one of those observations came from informal viewers rather than pilot participants. Presenting all
six as equally evidenced would overstate it; presenting them in tiers is both accurate and more
persuasive, because it shows the pruning was deliberate at every stage rather than uniform.

**What genuinely remains open — two unrun comparisons, not two unexplained modes:**

1. **Cao's actual comparison was never run.** Cao pitted grains against a *field-of-view restrictor*
   and measured *search performance*; the head-to-head here was grains against *SignPop*, judged by
   eye. Neither Cao's comparison nor grains-versus-HardDark was reproduced, and HardDark is a
   stronger manipulation than Cao's comparator rather than the same thing. The detail-occlusion
   argument (see GRAIN above) limits how far Cao's result transfers to a driving task, which is a
   real defence — but it is an argument, not a measurement.
2. **HardDark's anxiety liability is unaddressed.** Barhorst-Cates et al. (2016) found anxiety
   elevated in every restricted-field condition. OutlinedDark was the candidate answer and was set
   aside on distraction grounds. Setting aside the answer does not settle the question.

Both belong in the limitations section as stated gaps.

**Gap 3b — two of the three pilot rejections are settings-dependent, not technique-dependent.**
FLATTEN was rejected because it brightened, which is a consequence of `_SqueezeLum` = 0.5 against
`_SqueezeDim` = 0.9; OutlinedDark because it looked stylised, which is `_EdgeBrightness` = 0.35 and
`_EdgeGain` = 12. Neither parameter was swept before the mode was set aside. The honest phrasing is
*"rejected at the settings piloted"*, not *"rejected"* — otherwise the dissertation claims more than
it tested. Two sentences in the discussion chapter would cover it.

**Gap 4 — two of ten modes have no proposing source.** SpotLift and SignPop are engineering
variations rather than literature imports. That is defensible — SignPop especially, which exists to
make the measurement match the claim — but ch4-system.md:326 presents the exploratory set as *"each a
literature-derived point in the design space"*, which is not true of SpotLift. (TintedDark, the third
such case, was removed from the documented set on 2026-07-29 for exactly this reason.)

**Gap 5 — ChromaticCool had no citation in the code until today.** Every other literature-derived
mode names its source in the shader or the enum comment (Veas, Cao, Cheng, Patney, Tariq).
ChromaticCool named none, which is how it drifted into implementing only half of Bailey's
manipulation without anyone noticing. Now cited at both sites.

---

## References

Verification markers are carried through from `Dissertation/chapters/VERIFY-citations.md`; entries
marked `[VERIFY]` there have not been confirmed against the primary source.

- Bailey, R., McNamara, A., Sudarsanam, N., & Grimm, C. (2009). Subtle Gaze Direction. *ACM
  Transactions on Graphics*, 28(4), Art. 100. doi:10.1145/1559755.1559757
- Barhorst-Cates, E. M., Rand, K. M., & Creem-Regehr, S. H. (2016). The Effects of Restricted
  Peripheral Field-of-View on Spatial Learning while Navigating. *PLoS ONE*, 11(10), e0163785.
- Cao, R., Grandi, J. G., & Kopper, R. (2021). Granulated Rest Frames Outperform Field of View
  Restrictors on Visual Search Performance. *Frontiers in Virtual Reality*, 2:604889.
  doi:10.3389/frvir.2021.604889 (verified against the PDF, 2026-07-30)
- Cheng, Y., Yin, Y., Yan, Y., Gugenheimer, J., & Lindlbauer, D. (2022). Towards Understanding
  Diminished Reality. *CHI 2022*. doi:10.1145/3491102.3517452
- Fernandes, A. S., & Feiner, S. K. (2016). Combating VR Sickness through Subtle Dynamic
  Field-of-View Modification. *IEEE 3DUI 2016*, 201–210. doi:10.1109/3DUI.2016.7460053
- Grogorick, S., Stengel, M., Eisemann, E., & Magnor, M. (2017). Subtle Gaze Guidance for Immersive
  Environments. *ACM SAP 2017*. doi:10.1145/3119881.3119890
- Grogorick, S., Albuquerque, G., & Magnor, M. (2018). Comparing Unobtrusive Gaze Guiding Stimuli in
  Head-Mounted Displays. *IEEE ICIP 2018*, 2805–2809. doi:10.1109/ICIP.2018.8451784
- Itti, L., Koch, C., & Niebur, E. (1998). A Model of Saliency-Based Visual Attention for Rapid Scene
  Analysis. *IEEE TPAMI*, 20(11).
- Lin, Y.-X., et al. (2020). How the Presence and Size of Static Peripheral Blur Affects
  Cybersickness in Virtual Reality.
- Norouzi, N., Bruder, G., & Welch, G. (2018). Assessing Vignetting as a Means to Reduce VR Sickness
  during Amplified Head Rotations. *ACM SAP 2018*.
- Patney, A., et al. (2016). Towards Foveated Rendering for Gaze-Tracked Virtual Reality. *ACM
  Transactions on Graphics*, 35(6). doi:10.1145/2980179.2980246
- Stewart, E. E. M., Valsecchi, M., & Schütz, A. C. (2020). A Review of Interactions between
  Peripheral and Foveal Vision. *Journal of Vision*, 20(12):2. `[VERIFY]`
- Sutton, J., Langlotz, T., Plopski, A., Zollmann, S., Itoh, Y., & Regenbrecht, H. (2022). Look over
  there! Investigating Saliency Modulation for Visual Guidance with AR Glasses. *UIST 2022*,
  Art. 81. doi:10.1145/3526113.3545633
- Tariq, T., et al. (2022). Noise-based perceptual enhancement. `[VERIFY]`
- Teixeira, J., & Palmisano, S. (2021). Effects of Dynamic Field-of-View Restriction on Cybersickness
  and Presence in HMD-Based Virtual Reality. *Virtual Reality*, 25, 433–445.
- Veas, E., Mendez, E., Feiner, S., & Schmalstieg, D. (2011). Directing Attention and Influencing
  Memory with Visual Saliency Modulation. *CHI 2011*, 1471–1480. doi:10.1145/1978942.1979158
- Waldin, N., Waldner, M., & Viola, I. (2017). Flicker Observer Effect: Guiding Attention through
  High Frequency Flicker. *Computer Graphics Forum*, 36(2), 467–476. doi:10.1111/cgf.13141
- Waldner, M., Le Muzic, M., Bernhard, M., Purgathofer, W., & Viola, I. (2014). Attractive Flicker —
  Guiding Attention in Dynamic Narrative Visualizations. *IEEE TVCG*, 20(12), 2456–2465.
- Walton, D. R., et al. (2021). Beyond Blur: Real-Time Ventral Metamers for Foveated Rendering. *ACM
  Transactions on Graphics (SIGGRAPH 2021)*. `[VERIFY]`
- Wu, F., & Suma Rosenberg, E. (2022). Asymmetric FOV Restriction Using Optic Flow.
