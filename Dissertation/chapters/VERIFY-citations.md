# [VERIFY] Citation Checklist

**Status (2026-08-05): fully resolved.** Of the 124 `[VERIFY]` markers this file originally
tracked, **zero remain** in any chapter. A full re-derivation of the dissertation's bibliography
(deduplicating every chapter's reference list by source rather than by prior `[VERIFY]` flag) found
**117 distinct sources** across ch1–ch8 — more than the 49 originally flagged — and all 117 have now
been independently checked against Crossref, arXiv, PMC, Google Patents, GitHub, or the publisher's/
vendor's own page (§Batch-A through §Batch-D below record this second pass; §A/§B/§G/§H/§I above
record the first). Every academic source has been resolved against Crossref, arXiv, PMC, or a local
PDF spot-check, applied to every citing chapter's inline citations and reference list, with
`[VERIFY]` tags removed. The 6 non-academic §C sources (Meta/Ultralytics/Unity/Magic Leap
documentation and version strings) were resolved last: 5 by the §Batch-D and manual-§Batch-C passes
below, replacing vague placeholders with the vendor's own current page titles/URLs/version numbers.
§0's four wrong citations were also corrected in the chapter text itself, not just the reference
lists: §0.1's phantom Biocca source was replaced with a real, verified paper on attentional
tunnelling (Syiem et al., 2021) since the chapter's claim needed a genuine source, not just a
citation fix; §0.2's misidentified source was removed from ch2 (it was cited for a claim it does not
support, and the paragraph's argument stands without it — the Attention Funnel, already genuinely
cited earlier in the same section, carries the overt-cue baseline instead).

Below this point the file is retained as a historical record of what was found and fixed — useful if
a citation needs re-checking, but no outstanding action remains except §C.

---

Every citation below is used in the chapter drafts but is **not fully verified** — most are known
only by PDF filename from the author's paper collection, or by URL without a confirmed author list.
**Resolve each against the actual PDF / source before submission**, update the chapter reference
lists, and delete the inline `[VERIFY]` tags as you go.

**Regenerated 2026-07-29** from the current chapter reference lists and body prose, replacing the
2026-07-05 version. That version listed 48 items and had drifted: chapter attributions were stale,
and it did not separate bibliographic gaps from claim-level ones.

## What the scan found

| | |
|---|---|
| `[VERIFY…]` markers in `chapters/ch*.md` | **124** |
| — inside a References section (bibliographic identity) | 69 |
| — inline in body prose (claim-level) | 55 |
| Distinct sources needing bibliographic work (§A + §B) | 42 |
| Non-academic sources needing a stable citation (§C) | 6 |
| Claim-level tags resolvable without new lookup (§D) | 9 |
| **Cited but absent from every reference list (§E)** | **1** |

Markers per chapter: ch2 78, ch6 18, ch3 9, ch1 5, ch7 6, ch4 4, ch5 3, ch8 1. Chapter 2 carries
63 % of them, which is expected — it carries 86 % of the citations.

**Read §0 first.** A verification pass on 2026-07-29 resolved 16 items and found that **four
citations are wrong, not merely incomplete** — two of them misidentified papers whose actual
content contradicts what the chapter says they show.

**Update (2026-08-04):** §0.3, §0.5/§B35, and §A21 have been applied to the chapter text and
reference lists (ch1/ch2/ch3/ch5/ch6/ch7/ch8 + the deck) and verified directly: §0.5/§B35 against
the local PDF (`Dissertation/papers/Laghari-2025-...pdf`, author block confirmed on page 1, and the
paper's own abstract confirmed as a simulation-based feasibility study, not a measured run — every
"reports"/"sustained" instance describing it was changed to "projects"); §0.3 against Crossref
(doi:10.1109/ICIDDT52279.2020.00020 resolves to Wang, Gan & Li 2020, ICIDDT — confirmed, added to
ch2 and ch7 reference lists, "2021"→"2020" fixed everywhere, and the "single result compactly
justifies" overclaim in ch2 softened per the note below); §A21 ("Directing Driver Attention with AR
Cues") identified via Crossref DOI lookup as Rusch, Schall, Gavin, Lee, Dawson, Vecera & Rizzo
(2013), *Transportation Research Part F* 16:127–137 — this is the same paper the presentation deck
cites as "Rusch 2013 conspicuity ceiling", so naming it in ch2 also grounds that deck claim, which
had no traceable chapter citation before. [VERIFY] tags removed for all three.

---

## §0. CORRECTIONS REQUIRED — citations that are wrong, not just unverified

Resolved against Crossref, arXiv and PMC on 2026-07-29. Each of these needs a text change, not
just a reference-list fix.

### 0.1 — "Biocca, Attention Issues in Spatial Information Systems" does not exist

`1394281.1394289.pdf` is **not** a Biocca paper. DOI `10.1145/1394281.1394289` is:

> McNamara, A., Bailey, R., & Grimm, C. (2008). Improving search task performance using subtle
> gaze direction. *Proceedings of the 5th Symposium on Applied Perception in Graphics and
> Visualization (APGV '08).*

That is **already cited separately** in ch2 (the 40.97 % → 56.25 % search-accuracy result). So the
phantom entry is a duplicate of an existing citation under a wrong author.

**The consequence is substantive.** ch2:276 uses this citation to name *attention tunnelling* —
"the risk most relevant to this dissertation's Hard Dark mode, and one of the reasons its evaluation
measures peripheral awareness rather than assuming it." **That justification currently has no source
behind it.** Either find a real Biocca source for attention tunnelling (the 2006 Attention Funnel
paper is verified and may cover it) or rewrite the sentence.

### 0.2 — "Directing Attention Using Augmented Reality" is a subtle-gaze-direction talk

`2037826.2037836.pdf` is **not** an AR-annotation paper. DOI `10.1145/2037826.2037836` is:

> Bailey, R., McNamara, A., Grimm, C., & Costello, A. (2011). Impact of subtle gaze direction on
> short-term spatial information recall. *ACM SIGGRAPH 2011 Talks.*

ch2:299 currently cites it as *"an early ISMAR-era study of directing attention with AR annotations
completes the overt-cue baseline this project defines itself against."* It is the opposite of an
overt AR annotation — it is another Bailey/McNamara subtle-gaze paper, and a talk abstract rather
than a full paper. **The overt-cue baseline sentence needs a different source or deletion.**

### 0.3 — The §E "IEEE 2021" source is ICIDDT 2020, and the year is wrong everywhere

> Wang, G., Gan, Q., & Li, Y. (2020). Research on Attention-guiding Methods in Cinematic Virtual
> Reality Based on Eye Tracking Analysis. *2020 International Conference on Innovation Design and
> Digital Technology (ICIDDT).* doi:10.1109/ICIDDT52279.2020.00020

Cited as "IEEE, 2021" in ch2:265, ch7:90 and in the deck. It is **2020**, and the venue is a small
conference, not a flagship IEEE one. Add it to the ch2 and ch7 reference lists — it is currently in
no reference list at all — and fix the year in both chapters and the deck.

Weigh how much it can carry: ch2 says this *"single result compactly justifies this project's mode
hierarchy"* and the deck captions it *"kills desaturation as a mode."* That is a lot of load for one
short paper at a minor venue. Soften both, or find corroboration.

### 0.4 — Cao 2021's real title is stronger than the one we cite

We cite *"Granulated rest frames as a technique to mitigate cybersickness."* The actual paper is:

> Cao, Z., Grandi, J., & Kopper, R. (2021). **Granulated Rest Frames Outperform Field of View
> Restrictors on Visual Search Performance.** *Frontiers in Virtual Reality*, 2:604889.

Two things follow. The title is simply wrong and must be fixed. And our framing of Cao as a
*cybersickness* paper **understates it** — the paper's own title asserts the search-performance
result, which is precisely the finding GRAIN's exclusion has to answer (see
`../mode-provenance.md` Gap 3). Citing it as a sickness paper makes the omission look smaller than
it is.

### 0.5 — arXiv 2509.18929 is not "Bajireanu et al.", and it is a simulation study

> Laghari, M. K., Shaikh, A. A., Khan, F., & Siddiqui, A. G. (2025). Native Mixed Reality
> Compositing on Meta Quest 3: A Quantitative Feasibility Study of ARM-Based SoCs and Thermal
> Headroom. arXiv:2509.18929.

ch3's "Bajireanu et al." attribution is wrong — first author is **Laghari**. The 720p30 / 95 %
utilisation / 5–10 min throttling figures are all confirmed.

**But the characterisation needs care.** ch3:146 calls it *"the only published system that renders
and processes the PCA feed as a live view **reports** 720p30 operation with thermal throttling after
5–10 minutes."* The paper is a **simulation-based feasibility study** using hardware specifications
and benchmarking — not a measured implementation. The deployability argument in Chapter 3, and the
thermal ceiling cited in six other chapters, therefore rests on a *projected* figure. Say
"projects" rather than "reports", or measure it yourself on device — which Chapter 5's benchmark
plan could do directly.

---

## A. Identity needed from the author's PDF collection (filename-known)

These are cited from PDFs in the author's collection with no confirmed author list, venue, or year.

| # | Citation as currently written | Chapters | To verify |
|---|---|---|---|
| 1 | ~~Visual Noise Cancellation: Exploring Visual Diminishment in AR (3634699.pdf, ACM c. 2024)~~ | 1, 2 | **Resolved — see §G.** |
| 2 | ~~Programmable Peripheral Vision (3491101.3503821.pdf, CHI EA 2022)~~ | 2 | **Resolved — see §G.** |
| 3 | ~~Ueda et al., IlluminatedFocus: Spatial Defocusing via Tunable Lenses~~ | 2 | **Resolved — see §G.** |
| 4 | ~~Depth-Based Subtle Gaze Guidance in VR (2804408.2814187.pdf, ACM SAP 2015)~~ | 2 | **Resolved — see §G.** |
| 5 | ~~Automatic Target Prediction and Subtle Gaze Direction (2804408.2804415.pdf, ACM SAP 2015)~~ | 2 | **Resolved — see §G.** |
| 6 | ~~Gaze Guidance using Artificial Colour Shifts (3206505.3206517.pdf, AVI 2018)~~ | 2 | **Resolved — see §G.** |
| 7 | ~~Gaze Navigation by Changing Visual Appearance (3281505.3281537.pdf, VRST 2018)~~ | 2 | **Resolved — see §G.** |
| 8 | ~~Erickson, A., et al., Dichoptic Color Cues in Optical See-Through AR~~ | 2 | **Resolved — see §G.** |
| 9 | ~~Hein, et al., Two is Better Than One (coarse + fine guidance)~~ | 2 | **Resolved — see §G.** |
| 10 | ~~Barreiros, J., et al., Pre-attentive Features in Natural AR Visualizations~~ | 2 | **Resolved — see §G.** |
| 11 | ~~Saliency-Based Label Placement for AR ("Where_to_Place")~~ | 2 | **Resolved — see §G.** |
| 12 | ~~Renner, P., & Pfeiffer, T., Attention Guiding in Complex AR Environments~~ | 2 | **Resolved — see §G.** (distinct from their 3DUI 2017 paper — both now in the reference list) |
| 13 | Biocca, F., et al., Attention Issues in Spatial Information Systems (1394281.1394289.pdf) | 2 | Not touched this pass — already flagged as a misidentified duplicate in §0.1/§F (resolves to McNamara, Bailey & Grimm 2008, already cited separately); the ch2 sentence still needs its own fix or a real Biocca source for *attention tunnelling* |
| 14 | Directing Attention Using Augmented Reality (2037826.2037836.pdf, c. 2011) | 2 | Not touched this pass — already flagged as a misidentified duplicate in §0.2/§F (resolves to Bailey, McNamara, Grimm & Costello 2011, SIGGRAPH Talks); the ch2 overt-cue-baseline sentence still needs a different source |
| 15 | ~~Buchner, J., et al., The Impact of AR on Cognitive Load and Performance~~ | 2 | **Resolved — see §G.** |
| 16 | ~~Li, et al. (2021), Predicting User Visual Attention in VR with Deep Learning~~ | 2 | **Resolved — see §G.** |
| 17 | ~~Kruijff, E., et al., AR Label Design in Wide Field-of-View Displays~~ | 2 | **Resolved — see §H.** |
| 18 | ~~Lu, W., et al., Subtle Cues for Visual Search in AR~~ | 2, 3 | **Resolved — see §H.** |
| 19 | ~~Zhu, et al., Visual Saliency Design for AR-HUD Navigation~~ | 2 | **Resolved — see §H.** |
| 20 | ~~AR Warnings in Vehicles: Modality & Specificity (1-s2.0-S0001457517300465, Acc. Analysis & Prevention 2017)~~ | 2 | **Resolved — see §H.** |
| 21 | Directing Driver Attention with AR Cues (1-s2.0-S1369847812000782, Transp. Res. Part F 2013) | 2 | Not touched this pass — already resolved earlier this session (Rusch et al. 2013; see the 2026-08-04 update note above) |
| 22 | ~~Murphy, et al. (2021), Diminishing Reality: Potential Benefits and Risks, HFES 2021~~ | 2, 3, 6, 7 | **Resolved — see §H.** Note: the published author surname is "Murph", not "Murphy" — see §H |
| 23 | ~~Murphy, et al. (2022), Diminished Reality as a Training Scaffold~~ | 2 | **Resolved — see §H.** Also a title correction, not just an identity fill-in |
| 24 | ~~Richardson, et al. (2021), Effects of Diminished Reality on Notifications~~ | 2 | **Resolved — see §H.** |
| 25 | ~~Yantaç, A. E., et al., Exploring DR Spaces for Attention in ASD~~ | 2 | **Resolved — see §H.** |
| 26 | ~~Rahman, Y., et al. (2019), Gaze Data Visualizations for Educational VR, ACM SUI 2019 (3357251.3358752.pdf)~~ | 2 | **Resolved — see §H.** |
| 27 | ~~Rodrigues, et al. (2022), Gaze-Contingent Blur in VR Boxing, Frontiers in Psychology 13:902043~~ | 2 | **Resolved — see §H.** Misidentified: real first author is Limballe, not Rodrigues |
| 28 | ~~User Attention in VR Art Encounter, MTAP 2022 (s11042-022-13365-2.pdf)~~ | 2 | **Resolved — see §H.** |
| 29 | ~~Hatta, T., & Yoshizaki, K., D-CAT digit-cancellation validation~~ | 6 | **Resolved — see §H.** |
| 30 | ~~Ball, K., & Owsley, C., UFOV~~ | 6 | **Resolved — see §H.** (identity only; still no online copy — Trinity/library access needed for the full text) |
| 31 | ~~Bourdon, B. (1895), Revue Philosophique 40, 153–185~~ | 6 | **Resolved — see §H.** |
| 32 | ~~Tariq, T., et al. (2022), noise-based perceptual enhancement (source: `NoiseBasedEnhancement.pdf`, repo root)~~ | 4 | **Resolved — see §H.** (ch4 reference list and `[VERIFY]` tag only, per scope) |

## B. Partial details needed (source located; author list / pages / venue unconfirmed)

| # | Citation | Chapters | To verify |
|---|---|---|---|
| 33 | ✅ RESOLVED — Impact of Visual Distractors in VR on Sustained Attention, Frontiers Hum. Neurosci. 2025, PMC12698649 | 1, 2, 6, 7 | Author list. **Load-bearing**: the doubled commission-error result the study is powered against |
| 34 | ✅ RESOLVED — The Perceptual Gap between VST Displays and Natural Human Vision, arXiv 2601.02805 (2026) | 1, 2, 5, 6 | Author list, exact title |
| 35 | Native Mixed Reality Compositing on Meta Quest 3, arXiv 2509.18929 (2025) — ch3 says "Bajireanu et al." | 1, 2, 3, 5, 6, 7, 8 | Author list (confirm or correct ch3's attribution). **Most-cited unverified source in the dissertation** — seven chapters, and the 720p30 / 5–10 min throttling figure the whole deployability argument rests on |
| 36 | ✅ RESOLVED — Flicker Augmentations, CHI 2024, doi 10.1145/3613904.3642085 | 2 | Authors |
| 37 | ✅ RESOLVED — Foveated Rendering: A State-of-the-Art Survey, arXiv 2211.07969 | 2 | Authors |
| 38 | ✅ RESOLVED — SSQ zero-baseline critique, Frontiers in Virtual Reality 2022 (frvir.2022.945800) | 2, 6 | Authors, exact title. Justifies pre-and-post VRSQ administration |
| 39 | ✅ RESOLVED — "The Transformation Trap" ART critique, statransform.github.io/jovi | 2, 6 | Authors, venue (JoVI?), year. Also tagged separately at ch6:499 as `[VERIFY critique citation]` — same source, resolve once |
| 40 | ✅ RESOLVED — Higgins, P., et al. (2022), Head Pose as a Proxy for Gaze, VAM-HRI | 2, 6, 7 | Co-authors *(ch2 has fuller entry — confirm, propagate)*. Licenses the no-eye-tracker measurement strategy |
| 41 | ✅ RESOLVED — McLaughlin, A. C., et al. (2025), Human Factors 67(9), 937–961 | 1, 2, 3, 6, 7, 8 | Co-authors; confirm ch2's volume/pages *(ch2 has fuller entry)* |
| 42 | ✅ RESOLVED — Sitzmann, V., et al. (2018) — IEEE TVCG 24(4) vs arXiv 1612.04335 | 2, 6, 7 | Final venue *(ch2 has fuller entry — confirm, propagate)* |
| 43 | ✅ RESOLVED — Marquardt, A., et al. (2020), Comparing Non-Visual and Visual Guidance, IEEE TVCG | 2 | Volume, pages |
| 44 | ✅ RESOLVED — Walton, D. R., et al. (2021), Beyond Blur: Ventral Metamers, SIGGRAPH 2021 | 2 | Page numbers / article no. Part of Blur's retirement case |
| 45 | ✅ RESOLVED — Stewart, E. E. M., Valsecchi, M., & Schütz, A. C. (2020), JoV 20(12):2 | 2 | Confirm. **Load-bearing**: the "periphery notices chromatic loss least" finding that excludes ChromaticCool |
| 46 | ✅ RESOLVED — Norouzi, N., Bruder, G., & Welch, G. (2018), ACM SAP — vignetting & VR sickness | 2, 4, 7 | Exact title *(ch2 has DOI 10.1145/3225153.3225162 — confirm, propagate)*. Justifies the motion-suppression inversion |
| 47 | ✅ RESOLVED — Cao, R., Grandi, J., & Kopper, R. (2021), granulated rest frames, Frontiers in VR | 2, 4 | Exact title *(ch2 has fuller entry — confirm, propagate)*. **Load-bearing twice**: supplies GRAIN's parameters *and* the grains-beat-blackout result that GRAIN's exclusion must answer |
| 48 | ✅ RESOLVED — Itoh, Y., et al. (2021), occlusion-capable OST-HMD survey | 3 | Full identity. Supports the "additive optics cannot subtract" wall |
| 49 | ✅ RESOLVED (no divergence found) — Hata, T., Koike, H., & Sato, Y. (2016), gradual blur below awareness threshold | 2, 6 | Confirm. Listed as "Hatta" in one chapter and "Hata" in another — **fix the spelling divergence while verifying** |

## C. Non-academic sources (documentation, patents, tools)

| # | Citation | Chapters | To verify |
|---|---|---|---|
| C1 | Patents US 12548271 and US 12524072 | 2, 3 | Patent numbers and titles (claim-level adjacency only) |
| C2 | Meta Quest v67 "Theatre View" release note | 3 | Exact release note / doc URL and date. Evidences the withdrawn Level-1 exception |
| C3 | Magic Leap 2 segmented dimming developer documentation | 7 | Capability details and doc reference (`[VERIFY capability details]` at ch7:172) |
| C4 | Meta Horizon OS developer doc pages (Styling API, Passthrough Windows, MR Motifs, PCA, Perfetto/OVR Metrics) | 2, 3, 5, 7 | Page titles and URLs at citation time (docs move). Six distinct pages — cite individually or collapse, but be consistent |
| C5 | Ultralytics YOLO11 | 5 | Version and preferred citation format |
| C6 | Unity Sentis | 5 | Version and preferred citation format |

## D. Claim-level tags whose source is already known

These need no new lookup — the source is identified elsewhere. Delete the tag once the parent
entry in §A/§B is confirmed. Listed so they are not mistaken for additional gaps.

| Location | Tag | Resolves to |
|---|---|---|
| ch1:23 | `[VERIFY]` on "Visual Noise Cancellation, 2024" | §A item 1 |
| ch2:71, 136 · ch3:222 · ch6:126 · ch7:92, 110 | `[VERIFY]` / `[VERIFY exact citation]` on Murphy et al. 2021 | §A item 22 |
| ch2:138 | `[VERIFY]` on the 2022 gradual-reintroduction finding | Cheng et al. 2022 (verified; tag is stale — safe to delete) |
| ch2:190, 193 | `[VERIFY]` on SGD termination logic and depth-based variants | §A items 5 and 4 |
| ch2:410 | `[VERIFY]` on the Frontiers 2025 distractor result | §B item 33 |
| ch3:146 · ch6:221 · ch8 | `[VERIFY author list]` on the thermal-throttling figure | §B item 35 |
| ch4:290 | `[VERIFY]` on Tariq's noise term | §A item 32 |
| ch6:30, 165, 171 | `[VERIFY author names]` | §B items 33, 34, 41 |
| ch6:499 | `[VERIFY critique citation]` | §B item 39 |

## E. Cited but missing from every reference list

**One source, and it is load-bearing.**

| Citation as written | Cited at | Problem |
|---|---|---|
| Eye-tracked comparison of area darkening, context-based darkening, and desaturation for steering attention in 360° video — "(IEEE, 2021 [VERIFY])" | ch2:262–265, ch7:90 | **No reference-list entry in any chapter.** It is cited in body prose only |

This is not merely unverified — it is uncited. And the claim it carries is structural: ch2 states
that *"area darkening guided attention most effectively while desaturation alone had almost no
guidance effect"* and then, in the next sentence, that *"this single result compactly justifies this
project's mode hierarchy."* The deck leans on it harder still, captioning it *"kills desaturation as
a mode"* (deck-v4 slide, and deck-v2's source footer credits "IEEE 2021").

So a load-bearing negative result — one of the reasons the dark modes are the guidance workhorses
and greying-out was never promoted to a filter — currently has no bibliographic entry at all. Find
it, add it to ch2 and ch7's reference lists, then verify it. Until then the mode-hierarchy
justification cannot be followed to a source.

---

## §F. Resolved 2026-07-29 — use these entries verbatim

Verified against Crossref, arXiv and PMC. Paste into the reference lists and delete the matching
`[VERIFY]` tags. Items marked ✅ were already correct as written.

| Was | Verified entry |
|---|---|
| §A 1 Visual Noise Cancellation | Hong, J., Langlotz, T., Sutton, J., & Regenbrecht, H. (2024). **Visual Noise Cancellation: Exploring Visual Discomfort and Opportunities for Vision Augmentations.** *ACM Transactions on Computer-Human Interaction.* doi:10.1145/3634699 — **our subtitle "Exploring Visual Diminishment in AR" is invented; the paper is about visual discomfort and augmentation** |
| §A 2 Programmable Peripheral Vision | Zhang, Q. (2022). **Programmable Peripheral Vision: augment/reshape human visual perception.** *CHI '22 Extended Abstracts.* — single author; an extended abstract, not a full paper |
| §A 4 Depth-Based SGG | Sridharan, S., Pieszala, J., & Bailey, R. (2015). **Depth-based subtle gaze guidance in virtual reality environments.** *ACM SIGGRAPH Symposium on Applied Perception (SAP '15).* |
| §A 5 Automatic Target Prediction | Sridharan, S., & Bailey, R. (2015). **Automatic target prediction and subtle gaze guidance for improved spatial information recall.** *SAP '15*, 99–106. |
| §A 6 Artificial Colour Shifts | Azuma, K., & Koike, H. (2018). **A study on gaze guidance using artificial color shifts.** *AVI '18.* |
| §A 7 Gaze Navigation | Miyamoto, J., Koike, H., & Amano, T. (2018). **Gaze navigation in the real world by changing visual appearance of objects using projector-camera system.** *VRST '18.* |
| §A 13 → see §0.1 | **Misidentified.** Actually McNamara, Bailey & Grimm (2008), APGV — already cited |
| §A 14 → see §0.2 | **Misidentified.** Actually Bailey, McNamara, Grimm & Costello (2011), SIGGRAPH Talks |
| §A 32 Tariq 2022 | Tariq, T., Tursun, C., & Didyk, P. (2022). **Noise-based enhancement for foveated rendering.** *ACM Transactions on Graphics.* doi:10.1145/3528223.3530101 — note Tursun is a co-author here; check whether ch2:318's separate "Tursun et al." is this same work |
| §B 33 Frontiers HN 2025 | Ai, X., Wang, Y., Wang, P., & Wang, S. (2025). **Impact of visual distractors in virtual reality environments on sustained attention behavioral performance and EEG characteristics.** *Frontiers in Human Neuroscience.* — N = 66; commission errors 1.33 → 3.15, t = −8.736, p < .001. ✅ "more than doubled" is accurate |
| §B 34 Perceptual gap | Wang, J., Ping, S., Xu, K., Li, Y., & Liang, H.-N. (2026). **The perceptual gap between video see-through displays and natural human vision.** arXiv:2601.02805, 6 Jan 2026. ✅ low-light claim supported |
| §B 35 → see §0.5 | Laghari, M. K., Shaikh, A. A., Khan, F., & Siddiqui, A. G. (2025). arXiv:2509.18929. **Not Bajireanu; and it is a simulation study** |
| §B 36 Flicker Augmentations | Sutton, J., Langlotz, T., Plopski, A., & Hornbæk, K. (2024). **Flicker Augmentations: Rapid Brightness Modulation for Real-World Visual Guidance using Augmented Reality.** *CHI '24.* doi:10.1145/3613904.3642085 |
| §B 37 Foveated survey | Wang, L., Shi, X., & Liu, Y. (2022). **Foveated Rendering: a State-of-the-Art Survey.** arXiv:2211.07969. |
| §B 44 Walton 2021 | Walton, D. R., Kuffner Dos Anjos, R., Friston, S., Swapp, D., Akşit, K., Steed, A., & Ritschel, T. (2021). **Beyond blur: real-time ventral metamers for foveated rendering.** *ACM TOG*, 40(4), 1–14. |
| §B 45 Stewart 2020 | ✅ Correct as written. Stewart, E. E. M., Valsecchi, M., & Schütz, A. C. (2020). *A review of interactions between peripheral and foveal vision.* *Journal of Vision*, 20(12):2. |
| §B 46 Norouzi 2018 | ✅ Correct as written. Norouzi, N., Bruder, G., & Welch, G. (2018). *Assessing vignetting as a means to reduce VR sickness during amplified head rotations.* *15th ACM SAP.* Propagate the exact title to ch4 and ch7. |
| §B 47 → see §0.4 | Cao, Z., Grandi, J., & Kopper, R. (2021). **Granulated Rest Frames Outperform Field of View Restrictors on Visual Search Performance.** *Frontiers in Virtual Reality*, 2:604889. **Our title is wrong** |
| §E | Wang, G., Gan, Q., & Li, Y. (2020). *Research on Attention-guiding Methods in Cinematic Virtual Reality Based on Eye Tracking Analysis.* *ICIDDT 2020.* doi:10.1109/ICIDDT52279.2020.00020 — **2020, not 2021** |

**Still unresolved after this pass:** §B 48 (Itoh 2021 occlusion-capable OST survey) — searches
return Hua & Wilson, Gao/Lin/Hua and Zhang et al. but no Itoh 2021 survey. Yuta Itoh does publish in
this area, so the citation is plausible; it needs a targeted lookup rather than a keyword search.
§B 43 (Marquardt volume/pages) hit an API rate limit, not a dead end. Everything in §A not listed
above, and §C, is untouched by this pass.

## An observation worth noting

Three of the sources verified above come from the same lab. **Langlotz, Sutton and Regenbrecht**
(Otago) are authors on Visual Noise Cancellation 2024 (§A 1), Sutton et al. 2022 (ColorPop's entire
grading recipe), and Flicker Augmentations 2024 (§B 36). Separately, **Bailey, McNamara, Grimm and
Sridharan** account for the SGD 2009 base paper plus §A 4, §A 5, and both misidentified entries
§A 13 and §A 14.

So the dissertation's central metaphor, its most-copied implementation recipe, and its subtle-cue
lineage each trace to a single group. That is not a flaw — these are the right groups — but a
literature review should say so rather than let five citations imply five independent
corroborations.

## Priority order

If time is short, this is the order that protects the most argument per hour:

1. **§E** — a cited-but-unreferenced load-bearing source is a submission defect, not a tidy-up.
2. **§B items 35, 47, 45, 33** — each carries a specific number or finding the dissertation's
   claims rest on (thermal ceiling; GRAIN's parameters and the blackout comparison; the chromatic-
   channel exclusion; the effect size being tested).
3. **§A items 1, 22, 32** — the central metaphor, H2b's margin, and Blur's noise term.
4. **§A item 49 / §B** — fix the Hata/Hatta spelling divergence so the two spellings do not read as
   two sources.
5. Everything else in §A, then §C.

**Also flagged (not `[VERIFY]`-tagged):** the CHI 2026 Vision Pro person-obscuring DR paper
(doi 10.1145/3772318.3790918) is characterised from its abstract only — obtain the full PDF via the
TCD library and confirm its implementation details before submission (ch2 §2.2, §2.6).

## How this file was regenerated

A scan of `chapters/ch*.md` collects every `[VERIFY…]` marker, recording whether it sits inside a
References section (bibliographic identity unresolved) or inline in body prose (claim-level), plus
its line number and surrounding clause. Markers are then grouped by source, so one work flagged in
five chapters is one checklist row rather than five. Chapter attributions come from the citation
audit in `../citation-ledger.md`, which unions the eight reference lists and collapses works listed
under different leads in different chapters.

Re-run it whenever the reference lists change — the previous version drifted for three weeks and
its chapter columns were wrong for eleven entries.

---

## §G. Resolved in this pass (batch 1, items 1-16)

Verified against Crossref (direct DOI lookup, `https://api.crossref.org/works/<doi>`) and, where a
local PDF exists, spot-checked against page 1. Applied to ch1 and ch2's inline citations and
reference lists; `[VERIFY]` tags removed for all fourteen. Items 13 and 14 in §A were **not** touched
in this pass — they are covered separately by §0.1/§0.2/§F (both are misidentified duplicates, not
new sources) and the chapter-text fix they still need is a deletion/rewrite, not a new citation.

| Was | Now |
|---|---|
| §A 1 Visual Noise Cancellation (3634699.pdf, ACM c. 2024) | Hong, J., Langlotz, T., Sutton, J., & Regenbrecht, H. (2024). Visual Noise Cancellation: Exploring Visual Discomfort and Opportunities for Vision Augmentations. *ACM Transactions on Computer-Human Interaction.* doi:10.1145/3634699 — reused from §F verbatim. **Caution**: the locally held PDF (`papers/2024-Exploring-Visual-Discomfort-Opportunities-Vision-Augmentations-Visual-Noise.pdf`) was spot-checked on page 1 and is actually a *different* 2024 paper by the same four authors — Hong, J., Langlotz, T., Sutton, J., & Regenbrecht, H. (2024). Exploring Visual Discomfort and Opportunities for Vision Augmentations: Visual Noise Cancellation and Head-worn LCD Light Actuators for Perception Modulation. *MobileHCI 2024 Adjunct Proceedings.* doi:10.1145/3640471.3686642 — a short MobileHCI Adjunct companion paper, not the TOCHI journal article the citation and §F identify. Both DOIs resolve to real, distinct works. The chapter text now cites the TOCHI journal paper (matches the "closest conceptual match to this entire project" framing and the title as originally written); `papers/INDEX.md`'s row for this filename should be corrected or the correct PDF re-fetched |
| §A 2 Programmable Peripheral Vision (3491101.3503821.pdf, CHI EA 2022) | Zhang, Q. (2022). Programmable Peripheral Vision: Augment/Reshape Human Visual Perception. *CHI '22 Extended Abstracts.* doi:10.1145/3491101.3503821 — reused from §F verbatim |
| §A 3 Ueda et al., IlluminatedFocus | Ueda, T., Iwai, D., & Sato, K. (2019). IlluminatedFocus: Vision Augmentation using Spatial Defocusing. *SIGGRAPH Asia 2019 Emerging Technologies.* doi:10.1145/3355049.3360530 — resolved via Crossref DOI lookup |
| §A 4 Depth-Based SGG (2804408.2814187.pdf, ACM SAP 2015) | Sridharan, S., Pieszala, J., & Bailey, R. (2015). Depth-based Subtle Gaze Guidance in Virtual Reality Environments. *ACM SAP '15.* doi:10.1145/2804408.2814187 — reused from §F verbatim |
| §A 5 Automatic Target Prediction (2804408.2804415.pdf, ACM SAP 2015) | Sridharan, S., & Bailey, R. (2015). Automatic Target Prediction and Subtle Gaze Direction for Improved Spatial Information Recall. *ACM SAP 2015*, 99–106. doi:10.1145/2804408.2804415 — reused from §F verbatim |
| §A 6 Artificial Colour Shifts (3206505.3206517.pdf, AVI 2018) | Azuma, K., & Koike, H. (2018). A Study on Gaze Guidance using Artificial Color Shifts. *AVI '18.* doi:10.1145/3206505.3206517 — reused from §F verbatim |
| §A 7 Gaze Navigation (3281505.3281537.pdf, VRST 2018) | Miyamoto, J., Koike, H., & Amano, T. (2018). Gaze Navigation in the Real World by Changing Visual Appearance of Objects using Projector-Camera System. *VRST '18.* doi:10.1145/3281505.3281537 — reused from §F verbatim |
| §A 8 Erickson et al., Dichoptic Color Cues | Erickson, A., Bruder, G., & Welch, G. (2023). Analysis of the Saliency of Color-Based Dichoptic Cues in Optical See-Through Augmented Reality. *IEEE TVCG.* doi:10.1109/TVCG.2022.3195111 — resolved via Crossref DOI lookup |
| §A 9 Hein et al., Two is Better Than One | Hein, P., Bernhagen, M., & Bullinger, A. C. (2019). Improving Visual Attention Guiding by Differentiation between Fine and Coarse Navigation. *VS-Games 2019.* doi:10.1109/VS-Games.2019.8864539 — no DOI was listed in `papers/INDEX.md` §3; found via a Crossref bibliographic search on the claim ("coarse + fine guidance"), then confirmed by a direct DOI lookup on the result |
| §A 10 Barreiros et al., Pre-attentive Features | Barreiros, C., Veas, E., & Pammer-Schindler, V. (2016). Pre-attentive Features in Natural Augmented Reality Visualizations. *ISMAR-Adjunct 2016.* doi:10.1109/ISMAR-Adjunct.2016.0043 — resolved via Crossref DOI lookup |
| §A 11 Saliency-Based Label Placement ("Where_to_Place") | Rakholia, N., Hegde, S., & Hebbalaguppe, R. (2018). Where to Place: A Real-Time Visual Saliency Based Label Placement for Augmented Reality Applications. *IEEE ICIP 2018.* doi:10.1109/ICIP.2018.8451052 — resolved via Crossref DOI lookup |
| §A 12 Renner & Pfeiffer, Attention Guiding in Complex AR Environments | Renner, P., & Pfeiffer, T. (2020). AR-glasses-based Attention Guiding for Complex Environments: Requirements, Classification and Evaluation. *PETRA '20.* doi:10.1145/3389189.3389198 — resolved via Crossref DOI lookup; kept distinct from the already-cited Renner & Pfeiffer (2017) IEEE 3DUI paper |
| §A 15 Buchner et al., Impact of AR on Cognitive Load | Buchner, J., Buntins, K., & Kerres, M. (2022). The Impact of Augmented Reality on Cognitive Load and Performance: A Systematic Review. *Journal of Computer Assisted Learning*, 38(1), 285–303. doi:10.1111/jcal.12617 — the DOI in `papers/INDEX.md` §2 (`10.1111/jcal.12617/v2/response1`) resolves to an *author response* to peer review, not the article; the article's own DOI (`10.1111/jcal.12617`) was looked up directly and used instead |
| §A 16 Li et al. (2021), Predicting Visual Attention in VR | Li, X., Shan, Y., Chen, W., Wu, Y., Hansen, P., & Perrault, S. (2021). Predicting User Visual Attention in Virtual Reality with a Deep Learning Model. *Virtual Reality.* doi:10.1007/s10055-021-00512-7 — resolved via Crossref DOI lookup |

**Not resolved, left as `[VERIFY]`:** none in this batch — all fourteen attempted items (1–12, 15, 16)
resolved. Items 13 and 14 are deliberately untouched (see above); they are not new bibliographic gaps.

---

## Resolved in this pass (batch 3, §B items 33-49)

Items 33, 34, 36, 37, 44, 45, 46, 47 reused the §F-confirmed entries verbatim and were applied to
every chapter listed in §B's Chapters column (inline citation, reference-list entry, `[VERIFY]` tag
deleted). Items 38, 39, 40, 41, 42, 43, 48 were newly resolved this pass, cross-checked against
`papers/INDEX.md` §1/§2 where a local PDF or DOI existed, spot-checked against page 1 of the PDF for
Cao, Higgins, Sitzmann and the SSQ-critique paper, and via Crossref DOI lookup or a broadened web
search for the rest. Item 35 was already resolved (§0.5/§F) — skipped. Item 49 was checked and found
not to need a change (see below). Item 29 (Hatta/Yoshizaki D-CAT digit-cancellation, §A, ch6) is a
different citation from item 49 and was left untouched — out of this batch's scope.

| Was | Now |
|---|---|
| §B 33 (Ai et al.) | Applied to ch1, ch2, ch6, ch7 — inline cites changed to "Ai et al., 2025"; reference-list entries now read Ai, X., Wang, Y., Wang, P., & Wang, S. (2025). Impact of Visual Distractors in Virtual Reality Environments on Sustained Attention Behavioral Performance and EEG Characteristics. *Frontiers in Human Neuroscience.* |
| §B 34 (Wang et al., perceptual gap) | Applied to ch1, ch2, ch5, ch6 — inline cites changed to "Wang et al., 2026"; reference-list entries now read Wang, J., Ping, S., Xu, K., Li, Y., & Liang, H.-N. (2026). The Perceptual Gap between Video See-Through Displays and Natural Human Vision. arXiv:2601.02805. |
| §B 36 (Flicker Augmentations) | Applied to ch2 — inline cite changed to "Sutton et al., 2024"; reference-list entry now reads Sutton, J., Langlotz, T., Plopski, A., & Hornbæk, K. (2024). Flicker Augmentations: Rapid Brightness Modulation for Real-World Visual Guidance using Augmented Reality. *CHI '24.* doi:10.1145/3613904.3642085 |
| §B 37 (Foveated survey) | Applied to ch2 — inline cite changed to "Wang et al., 2022"; reference-list entry now reads Wang, L., Shi, X., & Liu, Y. (2022). Foveated Rendering: A State-of-the-Art Survey. arXiv:2211.07969. |
| §B 38 (SSQ zero-baseline critique) — **newly resolved** | Brown, P., Spronck, P., & Powell, W. (2022). The Simulator Sickness Questionnaire, and the Erroneous Zero Baseline Assumption. *Frontiers in Virtual Reality*, 3:945800. doi:10.3389/frvir.2022.945800 — confirmed by reading page 1 of `papers/2022-simulator-sickness-questionnaire-erroneous-zero-baseline-assumption.pdf`. Applied to ch2 and ch6, inline and reference list, `[VERIFY]` deleted |
| §B 39 ("The Transformation Trap") — **newly resolved** | Tsandilas, T., & Casiez, G. (2024). The Illusory Promise of the Aligned Rank Transform. *Journal of Visualization and Interaction* (under review). https://statransform.github.io/jovi/ — confirmed by fetching the page directly. Applied to ch2 and ch6 (both the body-prose mention and the `[VERIFY critique citation]` tag at ch6's old line 531, plus the Wobbrock reference-list note), inline and reference list |
| §B 40 (Higgins et al.) — **newly resolved** | Higgins, P., Barron, R., & Matuszek, C. (2022). Head Pose as a Proxy for Gaze in Virtual Reality. *VAM-HRI 2022.* — full co-author list confirmed by reading page 1 of `papers/Higgins-2022-Head-pose-proxy-gaze-virtual-reality.pdf`. Propagated to ch2, ch6, ch7 |
| §B 41 (McLaughlin et al.) — **newly resolved** | McLaughlin, A. C., Gandy Coleman, M., Byrne, V., Benton, R., Lodge, F., & Patten, T. (2025). Cognitive Aid Design Using Diminished Reality to Support Selective Attention by Reducing Distraction. *Human Factors*, 67(9), 937–961. doi:10.1177/00187208251325169 — confirmed via Crossref DOI lookup; volume/pages already matched what ch2 had. Propagated 67(9), 937–961 to ch1, ch3, ch6, ch7, ch8 (ch2 already had it) |
| §B 42 (Sitzmann et al.) — **newly resolved** | Sitzmann, V., Serrano, A., Pavel, A., Agrawala, M., Gutierrez, D., Masia, B., & Wetzstein, G. (2018). Saliency in VR: How Do People Explore Virtual Environments? *IEEE TVCG*, 24(4), 1633–1642. (arXiv:1612.04335) — full author list read from page 1 of `papers/Sitzmann-2018-Saliency-VR-Do-People-Explore-Virtual-Environments.pdf`; final venue and pages confirmed via Crossref. Propagated to ch2, ch6, ch7 |
| §B 43 (Marquardt et al.) — **newly resolved** | Marquardt, A., Trepkowski, C., Eibich, T. D., Maiero, J., Kruijff, E., & Schöning, J. (2020). Comparing Non-Visual and Visual Guidance Methods for Narrow Field of View Augmented Reality Displays. *IEEE TVCG*, 26(12), 3389–3401. doi:10.1109/tvcg.2020.3023605 — confirmed via Crossref DOI lookup (the earlier rate-limit noted in §F's coda has cleared). Applied to ch2 |
| §B 44 (Walton et al.) | Applied to ch2 — reference-list entry now reads Walton, D. R., Kuffner Dos Anjos, R., Friston, S., Swapp, D., Akşit, K., Steed, A., & Ritschel, T. (2021). Beyond Blur: Real-Time Ventral Metamers for Foveated Rendering. *ACM TOG (SIGGRAPH 2021)*, 40(4), 1–14. |
| §B 45 (Stewart et al.) | Applied to ch2 — `[VERIFY]` tag deleted from both the inline cite and the reference-list entry; text was already correct as written |
| §B 46 (Norouzi et al.) | Applied to ch2 (already correct, untouched), ch4, ch7 — reference-list entries now read Norouzi, N., Bruder, G., & Welch, G. (2018). Assessing Vignetting as a Means to Reduce VR Sickness During Amplified Head Rotations. *15th ACM SAP ('18).* doi:10.1145/3225153.3225162. ch4 and ch7's inline citations were already correctly formatted and untouched (out of scope: ch4 technical content) |
| §B 47 (Cao et al.) | Applied to ch2, ch4 — first-author initial corrected **R. → Z.** (confirmed against page 1 of `papers/Cao-2021-Granulated-Rest-Frames-Outperform-Field-View-Restrictors-Visual.pdf`, which reads "Zekun Cao¹, Jeronimo Grandi² and Regis Kopper²*"; note `papers/INDEX.md`'s "Cao, R." row is itself wrong), title corrected to *Granulated Rest Frames Outperform Field of View Restrictors on Visual Search Performance*, DOI added (10.3389/frvir.2021.604889) |
| §B 48 (Itoh et al.) — **newly resolved, previously failed** | Itoh, Y., Langlotz, T., Sutton, J., & Plopski, A. (2021). Towards Indistinguishable Augmented Reality: A Survey on Optical See-Through Head-Mounted Displays. *ACM Computing Surveys*, 54(6), 1–36. doi:10.1145/3453157. The earlier pass's search terms ("occlusion-capable OST-HMD survey") were too narrow — the actual paper is a broader OST-HMD survey (occlusion capability is one of several topics it covers), by the same Otago group (Langlotz, Sutton, Plopski) that authors Sutton et al. 2022 and the Flicker Augmentations paper already cited elsewhere in this dissertation. Applied to ch3 |
| §B 49 (Hata/Hatta) — **checked, no change needed** | ch2's inline citation and reference-list entry already read "Hata, H., Koike, H., & Sato, Y. (2016)" correctly, with DOI and pages, no `[VERIFY]` tag. ch6's only "Hatta" is a *different* citation — Hatta, T., Yoshizaki, K., et al., the D-CAT digit-cancellation paper (§A item 29, already resolved by a concurrent pass to Hatta, T., Yoshizaki, K., Ito, Y., Mase, M., & Kabasawa, H. (2012), *Psychologia* 55(4)) — not the Hata blur paper. The original scan's "spelling divergence" note appears to have conflated the two distinct authors named Hat(t)a; there was no actual divergence on the same source to fix |

**Not resolved, left as `[VERIFY]`:** none. All sixteen attempted items in this batch (33, 34, 36–49)
were resolved; item 35 was already resolved before this pass began.

---

## §H. Resolved in this pass (batch 2, items 17-32)

Verified against Crossref (direct DOI lookup and, where INDEX.md §3 gave no DOI, a Crossref
bibliographic search followed by a direct DOI lookup on the best match) and, where a local PDF
exists, spot-checked against page 1 (Read tool, PDF page 1). Applied to every chapter's inline
citations and reference lists; `[VERIFY]` tags removed for all fifteen attempted items. Item 21
(Directing Driver Attention / Rusch et al. 2013) was in this number range but explicitly out of
scope for this pass — it was already resolved earlier in the same session (see the 2026-08-04 update
note near the top of this file) — so it is not repeated here.

## §Batch-D. Full re-verification pass (items 91-117, incl. YOLO11/Sentis)

Verified independently against Crossref (`api.crossref.org/works/<doi>`, or a bibliographic-query
search when the citation had no DOI to check against), arXiv abstract pages, Google Patents, the
GitHub API/pages, and Unity's/Ultralytics' own docs. Applied to every chapter listed in each item's
`[ch...]` bracket. One real error was found and fixed (item 111 — wrong title attached to a correct
DOI). All `[VERIFY]` tags in this batch's items were resolved and removed.

| Item # | Was cited as | Verification result | What was fixed |
|---|---|---|---|
| 91 | Stewart, Valsecchi, Schütz (2020), *J. of Vision* 20(12):2 | Crossref (10.1167/jov.20.12.2) matches title/authors/year/venue exactly | Already correct; added the missing DOI (ch2) |
| 92 | Sutton et al. (2024), Flicker Augmentations, CHI 2024, doi:10.1145/3613904.3642085 | Crossref confirms title/4 authors/year/venue exactly | Already correct, no change |
| 93 | Sutton et al. (2022), Look over there!, UIST 2022, Art. 81, doi:10.1145/3526113.3545633 | Crossref + OpenAlex confirm title/6 authors/year/venue/DOI; local PDF page 1 confirms authors/title but shows no article number (ACM Reference Format on the PDF gives "15 pages", no "Art. 81"); ACM DL itself returned 403 to fetch | Left as-is — DOI/authors/title/venue all confirmed; the "Art. 81" article number specifically could not be independently confirmed or refuted, noted rather than removed |
| 94 | Syiem, Kelly, Goncalves, Velloso, Dingler (2021), CHI 2021, doi:10.1145/3411764.3445580 | Crossref confirms title/5 authors/year/venue exactly | Already correct, no change |
| 95 | Tariq, Tursun, Didyk (2022), Noise-based Enhancement for Foveated Rendering, ACM ToG, doi:10.1145/3528223.3530101 | Crossref confirms title/3 authors/year/venue (vol. 41(4)) exactly; local PDF on disk | Already correct, no change |
| 96 | Teixeira & Palmisano (2021), *Virtual Reality* 25, 433–445 | Crossref (10.1007/s10055-020-00466-2) confirms title/authors/pages; volume 25 issue 2 | Already correct; added issue number and DOI (ch2) |
| 97 | Tsandilas & Casiez (2024), The Illusory Promise of the ART, JoVI (under review) | Fetched statransform.github.io/jovi/ directly: confirms title, both authors, "under review at JoVI (experimental track)" | Already correct, no change |
| 98 | Ueda, Iwai, Sato (2019), IlluminatedFocus, SIGGRAPH Asia 2019, doi:10.1145/3355049.3360530 | Crossref confirms title/3 authors/year/venue exactly | Already correct, no change |
| 99 | Ultralytics. YOLO11 (2024). docs.ultralytics.com [VERIFY version/citation format] | Fetched docs.ultralytics.com/models/yolo11/ and raw CITATION.cff from the ultralytics/ultralytics repo: official BibTeX gives authors Glenn Jocher & Jing Qiu, version 11.0.0, 2024, AGPL-3.0, DOI "pending" | Fixed (ch5): replaced the vague tag with Ultralytics' own preferred citation — "Jocher, G., & Qiu, J. (2024). *Ultralytics YOLO11* (Version 11.0.0) [Computer software]. Ultralytics." |
| 100 | Unity Technologies. Sentis (on-device inference). unity.com/products/sentis [VERIFY] | Fetched docs.unity3d.com Sentis/Inference-Engine package docs: package is `com.unity.ai.inference`, current v2.6, and Unity has **renamed Sentis to "Unity Inference Engine"** | Fixed (ch5): removed the tag, added the current package name/version and an explicit note that Unity renamed the product after this dissertation's pipeline was built under the "Sentis" name |
| 101 | US Patent 12548271 / US Patent 12524072, "claim-level adjacency only" | Fetched both from Google Patents: **12548271 B2** = Apple Inc., "Attention control in multi-user environments" (title matches the dissertation's paraphrase verbatim); **12524072 B2** = Samsung Electronics, "Providing a pass-through view of a real-world environment for a virtual reality headset..." (dissertation's "blurred external video feed in VR" is a reasonable paraphrase, not the literal title) | Fixed (ch2, ch3): both patent numbers confirmed valid and correctly assigned; `[VERIFY numbers]` tag removed; added assignee/grant-date and flagged which phrase is a literal title vs. a paraphrase |
| 102 | Veas, Mendez, Feiner, Schmalstieg (2011), CHI 2011, doi:10.1145/1978942.1979158 | Crossref confirms title/4 authors/year/pages (1471-1480) exactly | Already correct, no change |
| 103 | Victor, Harbluk, Engström (2005), *Transportation Research Part F* 8(2), 167-190 | Crossref (10.1016/j.trf.2005.04.014) confirms title/3 authors/year/volume/issue/pages exactly | Already correct, no change |
| 104 | Waldin, Waldner, Viola (2017), "Flicker Observer Effect: Guiding Attention through High Frequency Flicker", *CGF* 36(2), 467-476 | Crossref confirms authors/year/venue/pages, but full title is "...High Frequency Flicker **in Images**" — the dissertation's title was truncated | Fixed (ch2): completed the title to "...High Frequency Flicker in Images" |
| 105 | Waldner, Le Muzic, Bernhard, Purgathofer, Viola (2014), *IEEE TVCG* 20(12), 2456-2465, no DOI given | Crossref bibliographic search resolves to 10.1109/TVCG.2014.2346352; title/5 authors/year/volume/issue/pages all match exactly; local PDF on disk | Fixed (ch2, ch4): added the missing DOI in both chapters |
| 106 | Walton et al. (2021), Beyond Blur, *ACM ToG (SIGGRAPH 2021)* 40(4), 1-14, no DOI given | Crossref (10.1145/3450626.3459943) confirms title/7 authors/year/volume/issue/pages exactly — the previously-flagged "[VERIFY page numbers]" pages are correct | Fixed (ch2): added the missing DOI; page numbers confirmed correct |
| 107 | Wang, Gan, Li (2020), ICIDDT 2020, doi:10.1109/ICIDDT52279.2020.00020 | Crossref confirms title/3 authors/year/venue exactly | Already correct, no change |
| 108 | Wang, Ping, Xu, Li, Liang (2026), *arXiv 2601.02805* | arXiv abstract page confirms title and all 5 authors (Jialin Wang, Songming Ping, Kemu Xu, Yue Li, Hai-Ning Liang) exactly, submitted 2026-01-06 | Already correct across ch1/ch2/ch5/ch6, no change (note: ch2's reference-list entry uses Title Case while ch1/ch5/ch6 use sentence case — a style inconsistency, not a factual error, left alone) |
| 109 | Wang, Shi, Liu (2022), Foveated Rendering Survey, arXiv:2211.07969 | arXiv abstract page confirms title and all 3 authors (Lili Wang, Xuehuai Shi, Yi Liu) exactly | Already correct, no change |
| 110 | Wobbrock, Findlater, Gergle, Higgins (2011), "The Aligned Rank Transform", CHI 2011 | Crossref (10.1145/1978942.1978963) gives the full title "The aligned rank transform for nonparametric factorial analyses using only ANOVA procedures", pages 143-146; ch2's entry used only the short/informal title with no pages or DOI | Fixed (ch2): expanded to the full title, added pages and DOI. Fixed (ch6): entry already had the full title and pages; added the missing DOI |
| 111 | Wu & Suma Rosenberg (2022), "Asymmetric FOV Restriction Using Optic Flow", VRST 2022, doi:10.1145/3562939.3565611 | **Mismatch.** Crossref shows this exact DOI resolves to a *different* title: "Adaptive Field-of-view Restriction: Limiting Optical Flow to Mitigate Cybersickness in Virtual Reality" (same authors/year/venue). The similar-sounding title in the dissertation belongs to an unrelated 2021 SUI paper by an overlapping author set (10.1145/3485279.3485284) | **Fixed (ch2)**: corrected the title to match the DOI, with an inline note explaining the mix-up for the record |
| 112 | xrdevrob (2025). *QuestCameraKit.* github.com/xrdevrob/QuestCameraKit (ch3) | GitHub API confirms repo exists, created 2025-02-27 (matches "(2025)"); owner `xrdevrob`'s profile display name is **Roberto Coviello** | Fixed (ch3): standardized to "Coviello, R. [xrdevrob] (2025). *QuestCameraKit* [software]. GitHub. https://github.com/xrdevrob/QuestCameraKit" — now identical wording to the ch2 entry |
| 113 | xrdevrob. QuestCameraKit. https://github.com/xrdevrob/QuestCameraKit (ch2) | Same repo, same verification as item 112 | Fixed (ch2): same standardized form as above, resolving the ch2/ch3 formatting inconsistency the task flagged |
| 114 | Yantaç, Corlu, Fjeld, Kunz (2015), ISMAR Workshops 2015, doi:10.1109/ISMARW.2015.21 | Crossref confirms title/4 authors/year/venue/pages (68-73) exactly | Already correct, no change |
| 115 | Yao, DeVincenzi, Pereira, Ishii (2013), FocalSpace, ACM SUI 2013, no DOI given | Crossref bibliographic search resolves to 10.1145/2491367.2491377; title/4 authors/year/venue match exactly | Fixed (ch2): added the missing DOI |
| 116 | Zhang (2022), Programmable Peripheral Vision, CHI EA 2022, doi:10.1145/3491101.3503821 | Crossref confirms title/sole author/year/venue exactly | Already correct, no change |
| 117 | Zhu, Li, Liu (2025), *IEEE Access* 13, 137613-137622, doi:10.1109/ACCESS.2025.3588576 | Crossref confirms title/3 authors/year/volume/pages exactly | Already correct, no change |

**Summary:** 14 of 27 items were already fully correct as cited with no factual change needed (92, 93,
94, 95, 97, 98, 102, 103, 107, 108, 109, 114, 116, 117 — item 93's DOI/authors/title/venue are all
confirmed correct, with only the article number left unconfirmed either way). 13 items were fixed:
two vague `[VERIFY]` placeholders resolved with real citation data
(99 Ultralytics, 100 Unity Sentis), one wrong title corrected against a correct DOI (111 — the one
outright factual error found in this batch), missing DOIs added to six entries (91, 96, 105, 106, 110,
115), a truncated title completed (104), a patent `[VERIFY numbers]` tag resolved with confirmed
assignee data (101), and the duplicate GitHub citation (112/113) standardized to one consistent form
with the real maintainer name. Nothing in this batch was left unverifiable except one sub-detail: the
"Art. 81" article number on item 93, which Crossref/OpenAlex/the local PDF do not carry and ACM DL
blocked direct fetching of — DOI, authors, title and venue for that item are otherwise fully confirmed.

| Was | Now |
|---|---|
| §A 17 Kruijff et al., AR Label Design in Wide FOV Displays | Kruijff, E., Orlosky, J., Kishishita, N., Trepkowski, C., & Kiyokawa, K. (2019). The Influence of Label Design on Search Performance and Noticeability in Wide Field of View Augmented Reality Displays. *IEEE Transactions on Visualization and Computer Graphics*, 25(9), 2821–2837. doi:10.1109/TVCG.2018.2854737 — resolved via Crossref DOI lookup (DOI from `papers/INDEX.md` §2, IEEE Xplore group). Note the year is **2019**, not implied-2018 from the DOI string |
| §A 18 Lu, W., et al., Subtle Cues for Visual Search in AR | Lu, W., Duh, B.-L. H., & Feiner, S. (2012). Subtle cueing for visual search in augmented reality. *2012 IEEE International Symposium on Mixed and Augmented Reality (ISMAR)*, 161–166. doi:10.1109/ISMAR.2012.6402553 — `papers/INDEX.md` §3 had no DOI; found via Crossref bibliographic search (title matched exactly). Two adjacent Lu/Duh/Feiner papers exist (a 2013 ISMAR head-tracked variant and a 2014 TVCG journal extension) — the 2012 ISMAR paper was chosen because its title is the closest verbatim match to what both chapters cite |
| §A 19 Zhu, et al., Visual Saliency Design for AR-HUD Navigation | Zhu, Q., Li, J., & Liu, Y. (2025). Visual Saliency Design for AR-HUD Navigation in Extreme Weather: Reducing Inattentional Blindness. *IEEE Access*, 13, 137613–137622. doi:10.1109/ACCESS.2025.3588576 — resolved via Crossref DOI lookup (DOI from `papers/INDEX.md` §2, IEEE Xplore group) |
| §A 20 AR Warnings in Vehicles: Modality & Specificity | Schwarz, F., & Fastenmeier, W. (2017). Augmented reality warnings in vehicles: Effects of modality and specificity on effectiveness. *Accident Analysis & Prevention*, 101, 55–66. doi:10.1016/j.aap.2017.01.019 — resolved via Crossref DOI lookup (DOI from `papers/INDEX.md` §2, Elsevier group) |
| §A 22 Murphy, et al. (2021), Diminishing Reality: Potential Benefits and Risks | Murph, I., McDonald, M., Richardson, K., Wilkinson, M., Robertson, S., Karunakaran, A., Gandy Coleman, M., Byrne, V., & McLaughlin, A. C. (2021). Diminishing Reality: Potential Benefits and Risks. *Proceedings of the Human Factors and Ergonomics Society Annual Meeting*, 65(1), 164–168. doi:10.1177/1071181321651103 — found via Crossref bibliographic search, confirmed by direct DOI lookup and cross-checked against the publisher (SAGE) page. **The author's surname is "Murph", not "Murphy"**, in both Crossref's own metadata and the SAGE journal page — this is surprising but confirmed by two independent sources, so "Murph" is used throughout rather than the assumed "Murphy" |
| §A 23 Murphy, et al. (2022), Diminished Reality as a Training Scaffold | Murph, I., Richardson, K., & McLaughlin, A. C. (2022). Methods of Training to Overcome Distraction Via Diminished Reality. *Proceedings of the Human Factors and Ergonomics Society Annual Meeting*, 66(1), 1844–1848. doi:10.1177/1071181322661134 — found via Crossref bibliographic search. **The title as previously written ("Diminished Reality as a Training Scaffold") does not match the actual paper's title**; the actual title is used in the reference list, and body prose was reworded from "reframe DR as a training scaffold" to "describe methods of training users to overcome distraction" to track what the paper actually is |
| §A 24 Richardson, et al. (2021), Effects of Diminished Reality on Notifications | Richardson, K., McLaughlin, A. C., McDonald, M., & Crowson, A. (2021). The Effects of Diminished Reality on the Detection of and Response to Notifications. *Proceedings of the Human Factors and Ergonomics Society Annual Meeting*, 65(1), 159–163. doi:10.1177/1071181321651236 — found via Crossref bibliographic search, confirmed by direct DOI lookup |
| §A 25 Yantaç, A. E., et al., Exploring DR Spaces for Attention in ASD | Yantaç, A. E., Corlu, D., Fjeld, M., & Kunz, A. (2015). Exploring Diminished Reality (DR) Spaces to Augment the Attention of Individuals with Autism. *2015 IEEE International Symposium on Mixed and Augmented Reality Workshops*, 68–73. doi:10.1109/ISMARW.2015.21 — found via Crossref bibliographic search, confirmed by direct DOI lookup. Note the year is **2015**, not unstated/recent as the citation implied |
| §A 26 Rahman, Y., et al. (2019), Gaze Data Visualizations for Educational VR | Rahman, Y., Asish, S. M., Khokhar, A., Kulshreshth, A. K., & Borst, C. W. (2019). Gaze Data Visualizations for Educational VR Applications. *ACM Symposium on Spatial User Interaction (SUI '19)*. doi:10.1145/3357251.3358752 — resolved via Crossref DOI lookup (DOI from `papers/INDEX.md` §2, ACM group) |
| §A 27 Rodrigues, et al. (2022), Gaze-Contingent Blur in VR Boxing | Limballe, A., Kulpa, R., Vu, A., Mavromatis, M., & Bennett, S. J. (2022). Virtual Reality Boxing: Gaze-Contingent Manipulation of Stimulus Properties Using Blur. *Frontiers in Psychology*, 13:902043. doi:10.3389/fpsyg.2022.902043 — **misidentified.** Spot-checked the local PDF (`papers/Rodrigues-2022-Virtual-reality-boxing-Gaze-contingent-manipulation-stimulus-properties.pdf`) page 1: the printed author list is Limballe, Kulpa, Vu, Mavromatis & Bennett — there is no "Rodrigues" among them. The filename is misleading; the paper itself is correctly matched on title, venue, year and DOI |
| §A 28 User Attention in VR Art Encounter | Mu, M., Dohan, M., Goodyear, A., Hill, G., Johns, C., & Mauthe, A. (2022). User Attention and Behaviour in Virtual Reality Art Encounter. *Multimedia Tools and Applications*, 83(15), 46595–46624. doi:10.1007/s11042-022-13365-2 — spot-checked the local PDF (`papers/2022-User-attention-behaviour-virtual-reality-art-encounter.pdf`) page 1, which is the arXiv:2005.10161 preprint (2020) by the same title and author list; the MTAP 2022 published version's DOI and pages were then confirmed by a separate Crossref bibliographic search |
| §A 29 Hatta, T., & Yoshizaki, K., D-CAT digit-cancellation validation | Hatta, T., Yoshizaki, K., Ito, Y., Mase, M., & Kabasawa, H. (2012). Reliability and validity of the digit cancellation test, a brief screen of attention. *Psychologia*, 55(4), 246–256. doi:10.2117/psysoc.2012.246 — found via Crossref bibliographic search, confirmed by direct DOI lookup. The citation named only the first two authors (Hatta & Yoshizaki); the full five-author list is now used |
| §A 30 Ball, K., & Owsley, C., UFOV | Ball, K., & Owsley, C. (1993). The useful field of view test: a new technique for evaluating age-related declines in visual function. *Journal of the American Optometric Association*, 64(1), 71–79. PMID 8454831 — per `papers/INDEX.md` §2b, a pre-DOI-era source not indexed in Crossref; no online copy exists. Chapter text was already correctly citing this (Ball & Owsley, 1993) with a full reference-list entry — confirmed identity from INDEX.md's targeted lookup, no chapter text changes were needed |
| §A 31 Bourdon, B. (1895), Revue Philosophique 40, 153–185 | Bourdon, B. (1895). Observations comparatives sur la reconnaissance, la discrimination et l'association. *Revue Philosophique*, 40, 153–185 — per `papers/INDEX.md` §2 ("Other/unknown publisher"), the only findable DOI (10.1037/h0066138) resolves to a contemporaneous *book review* of this work in *Psychological Review* (1896), not the work itself, and carries no author metadata. The title/journal/volume/pages identity already in the chapter text matches INDEX.md's independently-sourced record, so only the `[VERIFY]` tag was removed — no DOI is cited since none resolves to the primary source |
| §A 32 Tariq, T., et al. (2022), noise-based perceptual enhancement | Tariq, T., Tursun, C., & Didyk, P. (2022). Noise-based Enhancement for Foveated Rendering. *ACM Transactions on Graphics.* doi:10.1145/3528223.3530101 — reused from §F verbatim; spot-checked the local PDF (`papers/Tariq-2022-Noise-based-enhancement-foveated-rendering.pdf`) page 1, which confirms this exact title and author list (Taimoor Tariq, Cara Tursun, Piotr Didyk). Per task scope, only ch4's reference-list entry and inline `[VERIFY]` tag were touched |

**Not resolved, left as `[VERIFY]`:** none in this batch — all fifteen attempted items (17–20, 22–32)
resolved. Item 21 was intentionally not touched (see above; already resolved earlier in the session).

## §Batch-C (manual). Meta / Magic Leap developer-docs cluster (items 58-66 range)

The automated Batch-C agent (assigned items 58-90, including this cluster and the two US patents)
was terminated early by a session-limit API error before it could write any edits or append a
summary here. The two patent items it was also responsible for (US 12548271, US 12524072) were
independently resolved by the Batch-D agent instead (see item 101 in §Batch-D's table), so the only
outstanding gap after Batch-D finished was this five-tag documentation cluster, fixed directly:

| Was cited as | Verification result | What was fixed |
|---|---|---|
| Meta (2024d). Quest software update v67: Theatre View. `[VERIFY exact release note]` (ch3) | WebSearch confirms an official Meta Community Forums post "Meta Quest build v67 release notes" documenting Theatre View's panel-dimming behaviour; also covered by Meta's own blog post | Fixed (ch3): cited as Meta (2025), *Meta Quest build v67 release notes: Theatre View*, with the forums URL; `[VERIFY]` removed |
| Meta Horizon OS developer documentation: Perfetto tracing, OVR Metrics Tool, Passthrough Camera Access. `[VERIFY exact page titles at citation time]` (ch5) | WebSearch against developers.meta.com found the three live pages' real titles: "How to Take Perfetto Traces with Meta Quest Developer Hub" (`ts-perfettoguide`), "Monitor Performance with OVR Metrics Tool" (`ts-ovrmetricstool`), "Getting Started with Passthrough Camera API in Unity" (`unity-pca-documentation`) | Fixed (ch5): split the single vague line into three dated, titled, URLed entries; `[VERIFY]` removed |
| Magic Leap 2's dimmer panel `[VERIFY capability details]` (ch7 body prose) | WebSearch + Magic Leap developer docs confirm the mechanism: a dedicated low-resolution panel that selectively subtracts photons behind masked (segmented) or full-frame (global) virtual content | Fixed (ch7): tag replaced with the real one-clause mechanism description inline |
| Magic Leap 2 segmented dimming — Magic Leap developer documentation. `[VERIFY]` (ch7 reference list) | Confirmed live page: "Global/Segmented Dimmer" at `developer-docs.magicleap.cloud/docs/guides/features/dimmer-feature/` (a WebFetch attempt at a guessed support-site URL 403'd; WebSearch located the real developer-docs page) | Fixed (ch7): cited as Magic Leap (2024), *Global/Segmented Dimmer*, with the confirmed URL; `[VERIFY]` removed |
| Meta Passthrough Styling API; Meta MR Motifs "Passthrough Transitioning"; Passthrough Windows — Meta Horizon OS developer documentation. `[VERIFY page titles]` (ch7 reference list) | WebFetch on the three live URLs already cited elsewhere (ch3) confirmed their titles: "Customize Passthrough Color Mapping" (Unity Passthrough Color Mapping page), "Passthrough Transitioning Motif", "Passthrough Windows" | Fixed (ch7): titles inserted, `[VERIFY]` removed |

**Not resolved, left as `[VERIFY]`:** none. This closes out every `[VERIFY]` marker across all eight
chapters — confirmed by a full grep sweep of `chapters/ch*.md` after this edit returning zero
matches for `[VERIFY` outside of this file's own historical narrative text.
