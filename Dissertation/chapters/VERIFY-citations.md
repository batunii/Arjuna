# [VERIFY] Citation Checklist

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
| 1 | Visual Noise Cancellation: Exploring Visual Diminishment in AR (3634699.pdf, ACM c. 2024) | 1, 2 | Full bibliographic identity. **Supplies the dissertation's central metaphor** (ch2 §2.2: "the analogy is productive because it imports an architecture") — verify first in this section |
| 2 | Programmable Peripheral Vision (3491101.3503821.pdf, CHI EA 2022) | 2 | Authors, exact title, venue |
| 3 | Ueda et al., IlluminatedFocus: Spatial Defocusing via Tunable Lenses | 2 | Full reference |
| 4 | Depth-Based Subtle Gaze Guidance in VR (2804408.2814187.pdf, ACM SAP 2015) | 2 | Authors, exact title |
| 5 | Automatic Target Prediction and Subtle Gaze Direction (2804408.2804415.pdf, ACM SAP 2015) | 2 | Authors, exact title |
| 6 | Gaze Guidance using Artificial Colour Shifts (3206505.3206517.pdf, AVI 2018) | 2 | Authors |
| 7 | Gaze Navigation by Changing Visual Appearance (3281505.3281537.pdf, VRST 2018) | 2 | Authors |
| 8 | Erickson, A., et al., Dichoptic Color Cues in Optical See-Through AR | 2 | Full reference. Anchors the hue-versus-saturation decision at ch2:219 |
| 9 | Hein, et al., Two is Better Than One (coarse + fine guidance) | 2 | Full reference. Anchors the compound-design justification at ch2:289 |
| 10 | Barreiros, J., et al., Pre-attentive Features in Natural AR Visualizations | 2 | Full reference |
| 11 | Saliency-Based Label Placement for AR ("Where_to_Place") | 2 | Full reference |
| 12 | Renner, P., & Pfeiffer, T., Attention Guiding in Complex AR Environments | 2 | Full reference (distinct from their 3DUI 2017 paper — keep both) |
| 13 | Biocca, F., et al., Attention Issues in Spatial Information Systems (1394281.1394289.pdf) | 2 | Full reference. Names *attention tunnelling*, the risk Hard Dark carries |
| 14 | Directing Attention Using Augmented Reality (2037826.2037836.pdf, c. 2011) | 2 | Authors, venue |
| 15 | Buchner, J., et al., The Impact of AR on Cognitive Load and Performance | 2 | Full reference; also anchors claim-level tags at ch2:59 |
| 16 | Li, et al. (2021), Predicting User Visual Attention in VR with Deep Learning | 2 | Full reference |
| 17 | Kruijff, E., et al., AR Label Design in Wide Field-of-View Displays | 2 | Full reference; anchors claim-level tags at ch2:295–296 |
| 18 | Lu, W., et al., Subtle Cues for Visual Search in AR | 2, 3 | Full reference; anchors claim-level tags at ch2:295 and ch3:174 |
| 19 | Zhu, et al., Visual Saliency Design for AR-HUD Navigation | 2 | Full reference |
| 20 | AR Warnings in Vehicles: Modality & Specificity (1-s2.0-S0001457517300465, Acc. Analysis & Prevention 2017) | 2 | Authors. Body text at ch2:382 cites it with "N = 88, within-subject" — confirm |
| 21 | Directing Driver Attention with AR Cues (1-s2.0-S1369847812000782, Transp. Res. Part F 2013) | 2 | Authors. Body text at ch2:384 cites "N = 27" — confirm |
| 22 | Murphy, et al. (2021), Diminishing Reality: Potential Benefits and Risks, HFES 2021 | 2, 3, 6, 7 | Full reference (first author's full name, pages). **Load-bearing**: sets H2b's 10-point equivalence margin (ch6:124) |
| 23 | Murphy, et al. (2022), Diminished Reality as a Training Scaffold | 2 | Full reference |
| 24 | Richardson, et al. (2021), Effects of Diminished Reality on Notifications | 2 | Full reference |
| 25 | Yantaç, A. E., et al., Exploring DR Spaces for Attention in ASD | 2 | Full reference |
| 26 | Rahman, Y., et al. (2019), Gaze Data Visualizations for Educational VR, ACM SUI 2019 (3357251.3358752.pdf) | 2 | Confirm authors/DOI |
| 27 | Rodrigues, et al. (2022), Gaze-Contingent Blur in VR Boxing, Frontiers in Psychology 13:902043 | 2 | Confirm authors |
| 28 | User Attention in VR Art Encounter, MTAP 2022 (s11042-022-13365-2.pdf) | 2 | Authors, exact title |
| 29 | Hatta, T., & Yoshizaki, K., D-CAT digit-cancellation validation | 6 | Full reference (journal, year) |
| 30 | Ball, K., & Owsley, C., UFOV | 6 | Decide and verify the exact UFOV reference used. Justifies the 35° distractor eccentricity (ch6:245) |
| 31 | Bourdon, B. (1895), Revue Philosophique 40, 153–185 | 6 | Confirm against a citable secondary source |
| 32 | Tariq, T., et al. (2022), noise-based perceptual enhancement (source: `NoiseBasedEnhancement.pdf`, repo root) | 4 | Full identity from the PDF (title page is CID-encoded; check visually). Supplies Blur's perceptual-noise term |

## B. Partial details needed (source located; author list / pages / venue unconfirmed)

| # | Citation | Chapters | To verify |
|---|---|---|---|
| 33 | Impact of Visual Distractors in VR on Sustained Attention, Frontiers Hum. Neurosci. 2025, PMC12698649 | 1, 2, 6, 7 | Author list. **Load-bearing**: the doubled commission-error result the study is powered against |
| 34 | The Perceptual Gap between VST Displays and Natural Human Vision, arXiv 2601.02805 (2026) | 1, 2, 5, 6 | Author list, exact title |
| 35 | Native Mixed Reality Compositing on Meta Quest 3, arXiv 2509.18929 (2025) — ch3 says "Bajireanu et al." | 1, 2, 3, 5, 6, 7, 8 | Author list (confirm or correct ch3's attribution). **Most-cited unverified source in the dissertation** — seven chapters, and the 720p30 / 5–10 min throttling figure the whole deployability argument rests on |
| 36 | Flicker Augmentations, CHI 2024, doi 10.1145/3613904.3642085 | 2 | Authors |
| 37 | Foveated Rendering: A State-of-the-Art Survey, arXiv 2211.07969 | 2 | Authors |
| 38 | SSQ zero-baseline critique, Frontiers in Virtual Reality 2022 (frvir.2022.945800) | 2, 6 | Authors, exact title. Justifies pre-and-post VRSQ administration |
| 39 | "The Transformation Trap" ART critique, statransform.github.io/jovi | 2, 6 | Authors, venue (JoVI?), year. Also tagged separately at ch6:499 as `[VERIFY critique citation]` — same source, resolve once |
| 40 | Higgins, P., et al. (2022), Head Pose as a Proxy for Gaze, VAM-HRI | 2, 6, 7 | Co-authors *(ch2 has fuller entry — confirm, propagate)*. Licenses the no-eye-tracker measurement strategy |
| 41 | McLaughlin, A. C., et al. (2025), Human Factors 67(9), 937–961 | 1, 2, 3, 6, 7, 8 | Co-authors; confirm ch2's volume/pages *(ch2 has fuller entry)* |
| 42 | Sitzmann, V., et al. (2018) — IEEE TVCG 24(4) vs arXiv 1612.04335 | 2, 6, 7 | Final venue *(ch2 has fuller entry — confirm, propagate)* |
| 43 | Marquardt, A., et al. (2020), Comparing Non-Visual and Visual Guidance, IEEE TVCG | 2 | Volume, pages |
| 44 | Walton, D. R., et al. (2021), Beyond Blur: Ventral Metamers, SIGGRAPH 2021 | 2 | Page numbers / article no. Part of Blur's retirement case |
| 45 | Stewart, E. E. M., Valsecchi, M., & Schütz, A. C. (2020), JoV 20(12):2 | 2 | Confirm. **Load-bearing**: the "periphery notices chromatic loss least" finding that excludes ChromaticCool |
| 46 | Norouzi, N., Bruder, G., & Welch, G. (2018), ACM SAP — vignetting & VR sickness | 2, 4, 7 | Exact title *(ch2 has DOI 10.1145/3225153.3225162 — confirm, propagate)*. Justifies the motion-suppression inversion |
| 47 | Cao, R., Grandi, J., & Kopper, R. (2021), granulated rest frames, Frontiers in VR | 2, 4 | Exact title *(ch2 has fuller entry — confirm, propagate)*. **Load-bearing twice**: supplies GRAIN's parameters *and* the grains-beat-blackout result that GRAIN's exclusion must answer |
| 48 | Itoh, Y., et al. (2021), occlusion-capable OST-HMD survey | 3 | Full identity. Supports the "additive optics cannot subtract" wall |
| 49 | Hata, T., Koike, H., & Sato, Y. (2016), gradual blur below awareness threshold | 2, 6 | Confirm. Listed as "Hatta" in one chapter and "Hata" in another — **fix the spelling divergence while verifying** |

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
