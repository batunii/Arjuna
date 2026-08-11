# Examiner Review — Findings and Fix List (2026-08-08)

**What this is.** A critical marking pass of the submitted thesis PDF
(`Dissertation/25377738-dissertation-submission/thesis.pdf`), scored against the TCD
marking sheet (`Dissertation/Dissertation-Marking-Sheet-Sample.pdf`) and checked against
the commitments made to the supervisor in `Dissertation/Reply-back.pdf`. Only those three
documents were used. This file is written for an agent to pick up and apply the fixes.

**Where to fix.** The live LaTeX source is
`Dissertation/25377738-dissertation-submission/` (`thesis.tex`, `content/ch1.tex` …
`content/ch6.tex`, `content/appendix.tex`). Other trees (`submission/`,
`25377738-Dissertation/`, `pre-rewrite-snapshot-2026-08-06/`, `_pre-tighten-snapshot/`)
are snapshots — do not edit them. **Before editing anything, confirm with Shrey whether
the PDF has already been submitted**; if it has, this list becomes the errata/camera-ready
list, not a pre-submission fix list. Rebuild the PDF after fixes and re-check every item's
acceptance criterion. Page numbers below are body page numbers of the submitted PDF.

**Ground rule for the agent.** Never alter a reported statistic, count, or claim to
"make it consistent" without checking the underlying data/analysis scripts (repo:
`github.com/batunii/Arjuna`, analysis scripts referenced in ch4 §4.1). Where a number is
wrong, fix the number from data; where the prose overstates, fix the prose.

> **STATUS 2026-08-08: P1–P7 APPLIED and verified in a rebuilt `thesis.pdf` (107 pages,
> same as before; latexmk/MiKTeX).** Details per item below. P3's true count was computed
> by re-running `Tools/analysis/blocka_pooled.py` parsing over `authored/raw/blocka`:
> 33 file-backed runs = **4,125 scored trials** (84-trial versions score 83, 105→104,
> 140→139, since a 1-back cannot score its first trial), plus P4's one
> experimenter-reported arm (that arm stays disclosed in §5.7 only — repeating it in
> §5.2 was trimmed as bloat on Shrey's review). §5.2 now reads "4,125 file-backed scored
> Block A trials (task versions and provenance: Section 5.7)"; §5.4 is qualified with
> "in the final task version … (Section 5.7)". P2: Table A.3 row now reads
> "VR-experience form" (parenthetical trimmed — the rename alone kills the
> contradiction). P4: exclusions sentence now separates the two rule-based exclusions
> (P3, P43) from P9's same-day discretionary call in one clause with its documented
> reason (participant misunderstood the Block B response instruction) — incident-log
> framing trimmed. P5: ch3 now uses `\ref{sec:priorart}` (label already existed in ch2) →
> renders "Section 2.5.1". P6: `generate_all.py` annotation corrected to +17.8 pp and
> `fig_eccentricity_h2a.png` regenerated — figure, Table 5.2 (+17.78) and abstract (17.8)
> now agree. P7: Table A.4 header first cell changed to "ID" (no more column collision);
> the body-p.36 green artifact was a hyperref citation link box split across the page
> break, fixed by wrapping that one `\citep` in `\NoHyper…\endNoHyper`. P1: both bib
> notes verified (Yantis & Jonides 1984 JEP:HPP 10(5) 601–621; Wallis, Dorr & Bex 2015
> JoV 15(8):3), markers replaced with DOIs. Build note: the 135
> "name{cite.X} has been referenced but does not exist" hyperref warnings are chronic
> (present in the original build; citation boxes render, link anchors are dummies) — not
> introduced by these edits, not in P1–P7 scope. **Changes are uncommitted** — review
> with `git diff` before committing. P8–P12 and the bibliography/bloat passes remain
> open.

---

## Mark

**69 / 100**

| Criterion | Weight | Mark |
|---|---|---|
| Problem statement, motivation, and analysis | 10% | 75 |
| Background research & literature review | 15% | 66 |
| Technical content and project execution | 50% | 71 |
| Testing, evaluation, critical analysis & conclusions | 15% | 72 |
| Report presentation and writing | 10% | 55 |

The engineering and evaluative honesty are distinction-grade; presentation hygiene and
recurrent overclaiming drag the total below 70. Fixing the P1–P7 items below is worth
several points on its own because they sit directly on the marking sheet's
"internally consistent … adheres to academic conventions with respect to referencing"
criterion.

---

## Priority 1 — Disqualifying / must fix

### P1. "VERIFY BEFORE SUBMISSION" left in the bibliography
- **Where:** Bibliography, entries *Wallis et al. (2015)* and *Yantis and Jonides (1984)*
  (PDF pp. 83–84). Source: the `.bib` file / bibliography source in the submission tree.
- **Evidence:** Both entries end with the literal string "VERIFY BEFORE SUBMISSION."
  These citations underwrite the ring-marker design (Figure 5.2, §5.5) that carries H2a.
- **Fix:** Verify both references are real and correctly attributed (Wallis, Dorr & Bex
  2015, *Journal of Vision* 15(8):3; Yantis & Jonides 1984, *JEP:HPP* 10(5) 601–621),
  correct any errors, and delete the marker.
- **Verify:** `grep -ri "VERIFY BEFORE" Dissertation/25377738-dissertation-submission/`
  returns nothing; rebuilt PDF bibliography is clean.

### P2. Demographics contradiction (directly contradicts the reply-back commitment)
- **Where:** Appendix Table A.3 session timeline, row `00:00` (PDF p. 88) says
  "Welcome; written consent …; **demographics + VR-experience form**; VRSQ (pre)".
  Chapter 5 §5.3 (p. 55) and Limitation 10 (§6.3, p. 74) state "**No demographic
  instrument was administered**". Reply-back item 6 committed to the no-demographics
  framing.
- **Fix:** Determine which is true. Per §5.3, prior VR experience *was* recorded
  (four-level scale) but no age/gender/vision items. So the timeline row is stale text:
  change it to "VR-experience form (no demographic items)" or similar. If demographic
  data actually was collected, the body text and limitation must change instead — check
  the actual session materials before choosing.
- **Verify:** `grep -rn "demographics" content/appendix.tex` shows no contradiction with
  ch5/ch6 wording.

### P3. Trial-count arithmetic contradicted by the study's own history
- **Where:** §5.2 (p. 53): "the seventeen analysed workstation pairs contribute **4,760
  scored Block A trials**" (= 17 × 2 × 140). §5.7.1 (p. 63): "the first five participants
  ran a **shorter task version** and the remaining twelve ran the 140-trial version."
  Both cannot hold. Related: §5.4 (p. 56) presents Block A as uniformly "140 trials at a
  2.0 s stimulus onset asynchrony", which §5.7 (p. 62) undercuts (four task versions,
  a 1.8 s SOA variant, Kruskal–Wallis H = 16.36, p = .0010 across versions).
- **Fix:** Recompute the true scored-trial total from the trial logs / analysis scripts
  and correct §5.2. In §5.4, flag at first description that trial count/SOA varied across
  task versions and point forward to §5.7's provenance paragraph (one sentence is
  enough); do not leave the clean-design description standing unqualified.
- **Verify:** the number in §5.2 matches the analysis script's row count; §5.4 no longer
  states 140 × 2.0 s as universal.

### P4. Self-refuting exclusions sentence
- **Where:** §5.7 (p. 62): exclusions were made "under documented rules, **never
  inferred ones**" — immediately followed by "**P9 by experimenter decision on the
  day**". Experimenter discretion appears nowhere in Table A.2's pre-registered rules.
- **Fix:** Reword the framing sentence to stop claiming all exclusions were rule-driven;
  state P9's exclusion as what it was (a discretionary call, with its reason) and note it
  as a deviation, consistent with the thesis's own incident-log ethic. Do not delete the
  P9 disclosure.
- **Verify:** the paragraph no longer asserts "never inferred" while listing a
  discretionary exclusion.

---

## Priority 2 — Internal inconsistencies and mechanical errors

### P5. Dangling cross-reference
- **Where:** §3.5 (p. 26): "calibrated against the structured prior-art search reported
  in **Chapter 2, Section 2.5.2**". No §2.5.2 exists; the search is §2.5.1.
- **Fix:** Point the `\ref` at the §2.5.1 label. Also sweep for other hardcoded section
  numbers: `grep -rn "2\.5\.2" content/`.

### P6. Headline effect reported as three different values
- **Where:** Abstract (p. iii): "17.8 points"; Table 5.2 (p. 66): "+17.78 pp";
  Figure 5.6 annotation (p. 68): "**+17.6 pp**".
- **Fix:** Recompute from data; regenerate Figure 5.6 (its annotation likely predates the
  final n) or correct the text so all three agree (17.78 / "17.8" rounding is fine;
  17.6 vs 17.78 is not).
- **Verify:** grep the figure-generation script's output value and the two prose sites.

### P7. Table A.4 header typesetting collision
- **Where:** Appendix Table A.4 (p. 89): header renders as "Hypothesisimary test"
  (overlapping "Hypothesis" / "Primary test").
- **Fix:** Fix the column spec/width in `content/appendix.tex`.
- **Also:** p. 36 shows a stray green rule/box artifact at the page bottom (ColorPop
  section, ch4) — inspect the LaTeX around Figure 4.5/§4.5.2 for an unclosed box or
  spurious `\fbox`/rule and remove it.

---

## Priority 3 — Overclaiming (prose fixes, judgment required)

### P8. "Pre-registered" without a registry
- **Where:** Used throughout (abstract, ch1, ch5, ch6, appendix — dozens of instances).
  The actual mechanism is an internal frozen protocol + a "lock-in memo to the
  supervisor" (§A.3). No OSF/AsPredicted ID or timestamped external artefact exists.
- **Fix (least churn):** At first use (ch1 and ch5), define the term honestly: the
  protocol was frozen and lodged with the supervisor before data collection, with no
  external registry. Optionally s/pre-registered/pre-specified/ in the abstract and
  anywhere the word does load-bearing credibility work. Do not claim registration.
- **Related P8b:** Decision table row 4 (Table A.5, p. 91) contains "(Made unlikely by
  the pilot gate and trial counts. If it occurs, it is reported as such, not spun.)" —
  and row 4 is the outcome that occurred. With no external timestamp, this reads
  retro-fitted. Either remove the parenthetical or add a provenance note (date the table
  was locked, where the dated copy lives in the project record).

### P9. "Cannot return inconclusive" vs the row-4 outcome
- **Where:** §1.4 (p. 5) "the study is built so that it cannot return 'inconclusive'";
  §5.1 similar; abstract: "a pre-committed decision table under which every outcome is a
  conclusive result."
- **Why:** The primary result landed in the bounded-benefit gray zone (real direction,
  pre-set SESOI not claimable, "a larger sample would pin more tightly" — §6.1). The
  machinery handled it honestly, but the absolutist framing oversells.
- **Fix:** Soften to "every outcome maps to a pre-committed conclusion" (which is what
  the table actually delivers) rather than "conclusive".

### P10. Flagship hybrid composite carries zero participant data
- **Where:** The hybrid (Tier-3 processed periphery + Tier-2 native window) is the
  centrepiece novelty claim (abstract, §1.2, Table 2.1 gap 2, §3.2.4, §3.5), but no
  participant ever experienced it: Hard Dark (Tier 2 only) ran at the workstation and
  SignPop ran in the video scene. §6.3's mode-in-context confound acknowledges this only
  obliquely.
- **Fix:** Add one explicit sentence where the hybrid is first sold (ch1 §1.2 or ch3
  §3.2.4): the hybrid composite itself was validated technically (§4.11) but never
  user-tested; the study modes bracket it. This closes the "claim early, concede late"
  gap an examiner will notice.

### P11. Universal-negative novelty phrasing
- **Where:** §2.5.1 (p. 14): search "found **no publication, product, or open-source
  project … for any purpose**"; §2.3 (p. 11): "the first software-defined visual noise
  cancellation system on consumer video-passthrough hardware".
- **Fix:** Keep the scoped claim, soften the categorical reach: "the search found no …"
  (attribute the negative to the search, not the world) — the ch3 §3.5 wording already
  does this correctly; align ch2 with it. Reconsider "the first … system" or condition it
  on the search ("to the best of a documented search…").

### P12. Single studies elevated to "laws"
- **Where:** §2.1/§2.2: "the luminance-dominance law" (from Bailey et al. 2009 plus one
  dome study).
- **Fix:** s/law/regularity (or "consistent finding")/ — two small studies do not make a
  law.

---

## Priority 4 — Bibliography hygiene (beyond P1)

- **Title-as-author entries:** "Exploring Diminished Reality for Attention Support
  (2026)" and "Obscuring Undesirable Individuals (2026)" are cited with their titles in
  the author position. Fetch the real author lists from the ACM DL pages already cited
  and fix the `.bib` entries (entry keys will keep citations working).
- **"et al." inside reference author lists:** Patney et al., Duan et al., McLaughlin
  et al. — expand to full author lists per convention.
- **Editorial self-talk inside entries:** the Jocher & Qiu (YOLO11) note "DOI not yet
  assigned … at time of writing" and the Unity Sentis renaming note are defensible but
  should be trimmed to citation-relevant content or moved to a footnote.
- **2026-dated entries deserve a spot-check** (two CHI/DIS 2026 papers, two 2026 patents,
  arXiv 2601.02805): confirm each resolves to a real document matching the claims made of
  it. Given P1, the burden of proof is on the bibliography now.

---

## Priority 5 — Bloat / repetition (optional, lower value per hour)

- The 18-vs-20 shortfall is restated ≥6 times (abstract, §1.6, ch5 status note, §5.7,
  §6.3 item 7, §6.1, §A.6.3). Keep abstract + ch5 status note + limitation; compress the
  rest to a pointer.
- The oracle-conditioning caveat appears in §1.3, §1.6, §4.8.2, §5.1, §6.3, §A.7; the
  no-driving-claims disclaimer in §1.6, §2.6, §6.4, §A.8. Same treatment: one canonical
  statement plus pointers.
- §1.1's three-page carrel/headphone/Mahābhārata opening would carry at one page. Only
  cut if word count is under pressure; this is taste, not error.
- Aphoristic section-closers ("A design space is defined as much by its walls as by its
  rooms", "One sphere, one shader, one manager", etc.) are dense enough to read as
  machine-polished. Thin the weakest ones; keep the few that earn their place.

---

## Reply-back commitment audit (for reference — mostly kept)

| Commitment (Reply-back item) | Status in thesis |
|---|---|
| 1. Academic title, no colourful title | Kept |
| 2. Define focus window vs detection aperture at first use in ch4 | Kept (§4.2.2) |
| 3. Mechanism-first filter naming | Half-kept: code names lead, descriptions clarify (§4.5); John accepted this variant, low priority |
| 4. Correct Norouzi sickness claim; report own unwell participant accurately | Kept (§2.4, §5.3) |
| 5. REAMS dates/reference; blank consent forms in appendix | Kept (§5.3, §A.9: approved 27 May 2026, ref 6061) |
| 6. No demographics; report sampling frame; carry as limitation | Kept in body, **contradicted by Table A.3 — see P2** |
| 7. Acknowledge no velocity term | Kept (§4.8.1, §6.3 item 11) |
| 8. Aperture cap 8/16, no prioritisation; system chapter + limitations + future work | Kept (§4.8.1, §6.3 item 11, §6.5) |

## Strengths — do not break while fixing

- The measured technical validation (§4.11) and its honest miss (stale-frame criterion
  failed in both video arms) — leave the miss reported as a miss.
- The disclosed experimenter-reported P4 value and its sensitivity check (§5.7).
- The per-participant vs pooled estimator discussion (§5.7.3) and the sign-difference
  explanation.
- The failure museum (§4.10) and Table 4.1's built-but-not-evaluated honesty
  (TintedDark as dead code, etc.).
- The H2b two-sided/one-sided mismatch disclosure (§5.7.2, §6.6) — keep both numbers.

## Suggested execution order for the agent

1. P1 (bibliography markers) — 15 min, highest damage per minute.
2. P2, P4, P5, P7 (mechanical contradictions/typos) — small, safe edits.
3. P3 and P6 — require opening the analysis scripts/data before touching numbers.
4. P8–P12 — prose judgment; make minimal edits, preserve the author's voice.
5. P4-bib hygiene sweep, then optional P5-bloat pass only if word count allows.
6. Rebuild PDF, re-run every "Verify" line above, diff page count and spot-check
   Figures 5.4–5.7 and all appendix tables for reflow breakage.
