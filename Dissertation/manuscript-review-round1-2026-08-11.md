# Manuscript review loop — Round 1 synthesis

**Target:** the uncommitted working-tree diff against `b88baaa7` in
`Dissertation/25377738-dissertation-submission/`, plus the two out-of-tree edits to
`Dissertation/rewrite-factbase.md` and `Dissertation/chapters/ch6-user-study.md`.

**Diff at time of review:** `13 files changed, 9344 insertions(+), 8765 deletions(-)`
(the bulk is the regenerated `thesis.pdf` binary; the textual diff is ~246 insertions / ~256
deletions across `thesis.tex`, `content/ch1–ch6.tex`, `content/appendix.tex`, `refs.bib`,
`content/figures/generate_all.py`).

**Rounds run:** 3 of a maximum 3 — **CAP REACHED, LOOP CLOSED.**
**Final state:** 18 files changed. Build converges, 0 warnings, 0 undefined refs/citations,
0 Type 3 fonts, 110 pages, abstract on one page.

### Round 2 and 3 record

**Round 2** (3 fresh reviewers on the 22-item diff). Verified no statistic changed. Two reviewer
conflicts adjudicated by the parent:
- *"inconclusive" gone?* Reviewer 2 said yes; **wrong** — their grep was case-sensitive. Two
  capitalised survivors at `ch6.tex:28` (conclusion chapter) and `appendix.tex:215`. Reviewer 1 was
  right. Fixed in round 3.
- *P4 disclosure.* Reviewer 2 demanded the dotted line and caption clause back; **rejected** as
  re-litigating approved decision B3(b). Reviewer 1's compatible narrower point (name the arm) was
  adopted instead.

**Major round-2 finding:** the pre-registered PRIMARY test for H1 (trial-level logistic model,
`appendix.tex:153` Table A.4) was never reported — Chapter 5 reported only the registered robustness
check. The parent fitted it (`Tools/analysis/h1_lmm_pooled.py`, new; authority record
`authored/analysis-2026-08-11-h1-primary-lmm.md`, new). Result is favourable: **OR 0.583
[0.396, 0.860], p = .0065**, versus p = .0172 for the robustness check. Author chose Option A —
report it as primary. Applied in round 3.

**Round 3** (17 items). Parent verification caught one **regression of a previously approved fix**:
the abstract compression (476 → 301 words) silently reverted "at least two 0.1-logMAR steps" to
"two", turning a lower bound into a point estimate and contradicting `ch4.tex:747`, `ch4.tex:765`
and `ch6.tex:18`. **Fixed by the parent directly and rebuilt.** All other approved fixes verified
still present.

### Post-cap corrections (author-requested, 2026-08-11)

**1. Cross-reference audit — all 61 hardcoded `Section N.N` references checked against the built
`thesis.toc`.** Result: **60 correct, 1 wrong.**
- `appendix.tex:192` claimed the sample-size deviation is "reported in Chapter 5, **Sections 5.3**
  and 5.7". §5.3 is *Participants, Ethics, and Apparatus*. The deviation is actually in the
  unnumbered chapter status note (`ch5.tex:6–9`) and §5.7 (`ch5.tex:322`). **Fixed by the parent.**
  Root cause: a typed-in number, not a `\ref`, left stale by the 8→6 chapter restructure — the same
  bug class as examiner item P5. The status note is unnumbered, so it cannot be cited by number at
  all, which is likely why someone reached for 5.3.
- Verified correct and left alone: `ch6.tex:116` →5.3 (inclusion criteria — genuinely in §5.3);
  `appendix.tex:266` and `ch5.tex:357` →5.1 (§5.1 spans lines 18–62; both the `ai2025` cite at
  line 30 and the 4,125 trial count at line 60 fall inside it).
- Earlier parent claim of "0 hardcoded refs" was a **grep artifact** — the pattern allowed `~` but
  not a space. Corrected.

**2. The abstract's "one-page limit" did not exist.** Checked `tcdthesis.sty:618` (the
`thesisabstract` environment imposes no length constraint), `README.md`, `Notes.md`, and the marking
sheet. No word or page limit anywhere. The constraint came from a round-2 reviewer's presentation
critique and was hardened into a requirement by the parent's round-3 brief, causing a 476→301 word
cut. **Reverted:** the abstract is restored to 499 words with full detail, now carrying the LMM
primary test (OR 0.583, p = .0065) alongside the participant-level gain, the achieved sample
(seventeen/fifteen pairs), the 80% power statement, and both pre-registered-shortfall disclosures.
Document is 111 pages; the abstract's second page holds 1,402 characters (versus 852 on the
Acknowledgments page), so it reads as deliberate rather than as a near-blank spill.

**3. Parent correction — the marking sheet DOES weight presentation.** An earlier parent claim that
it did not was wrong: `pdftotext` without `-layout` had mangled the footnote. The sheet states
"Default weightings: Problem statement, motivation, and analysis: 10%, Background Research &
Literature Review (15%), Technical content and project execution (50%), Testing, evaluation,
critical analysis & conclusions (15%), **Report presentation and writing (10%)**". The reviewers'
10% figure was correct.

### Participant-privacy decisions (author, 2026-08-11)
| Item | Decision |
|---|---|
| Acknowledgement names (3) | Keep — all confirmed pilot-only |
| P15 Block A arm relabel | **Keep, never mention anywhere** — author's own corrected labelling error, true assignment known (unlike P1) |
| P9 "was confused" | Reworded to the task-comprehension rule. "Self-accepted" was **rejected as unsupported** by any record |
| Health incident `ch5.tex:145` | Keep verbatim — adverse-event reporting, anonymised |
| P3/P15/P43 exclusion counts | Keep — ordinary pre-registered reporting |
| Working-tree name↔ID mapping (22 places) | Deferred by author |
Full register: `participant-mentions-register-2026-08-11.md`.

---

**Round 1 status at the time of writing (historical):** decisions settled; fix worker `99b4496f`
running with 22 approved items.
**Files modified by the review round itself:** none. Reviewers were read-only; `git diff --shortstat`
was byte-identical before and after (`13 files changed, 9344 insertions(+), 8765 deletions(-)`).

---

## DECISION LOG (author, 2026-08-11) — all decisions settled

| Ref | Decision | Outcome |
|---|---|---|
| B1 | Fix the stray `I` | Approved — item [1] |
| B2 | Originally "remove P1 from both blocks"; **reversed** to "find a reason, otherwise don't" | Audited. **Block A exclusion justified and kept; Block B inclusion justified and kept. No statistic changes.** See `manuscript-p1-exclusion-audit-2026-08-11.md`. The wrong sentence at `ch5.tex:329` is still fixed — item [2] |
| B3 | P4 treated as canonical, extras purged | Approved as **option (b)**: canonical everywhere, figure line solid, ONE retained clause in §5.7 with the sensitivity check — items [3], [4] |
| B4 | Factbase edits (author asked what this was) | Explained; **restore approved** — item [16] |
| B5 | Protocol deviations | **Deferred by author.** Orphan references neutralised instead — item [4] |
| B6 | `\usepackage{lmodern}` | Approved — item [7] |
| B7 | `(Meta, 024a)` label bug | Approved — item [9] |
| D1 | Ch5 trim repair strategy | **Compact rewrite** (~120 words), not full restore — item [4] |
| D2 | Abstract shortfall disclosures | **Restore** — item [5] |
| D3 | `+17.6` vs `+17.8` | **Keep +17.8, label the estimator** as per-participant — item [8] |
| D4 | Font fix | **Approved** — item [7] |
| D5 | Acknowledgements / anonymity | All three named people are **pilot-only**; drop "also" — item [6]. Questionnaire filenames outside the submission folder will be name-stripped by the author before sharing |
| D6 | Four unaddressed examiner items (P8, P9, P10, P12) | **All four approved**, added mid-flight by steer — items [19]–[22] |

### Reversal recorded
The author's initial instruction was to remove P1 from both blocks. A full re-analysis was computed
and validated (`manuscript-reanalysis-P1-removed-2026-08-11.md`) before the instruction was
reversed. That artifact is marked **SUPERSEDED — DO NOT APPLY** and its scratch directory was
deleted so its n=14 numbers cannot be picked up by mistake. Net effect on the manuscript: none.

### Worker history
- `b0ceee18` — launched, then **stopped before any edit** because forked context forced thinking off
  on a prose-critical task. Working tree verified unchanged after the stop.
- `99b4496f` — relaunched with `context: fresh` (thinking high). Items [1]–[18], plus [19]–[22]
  delivered by steer. One writer against the worktree at a time throughout.

---

## Round 1: three fresh-context reviewers

| # | Angle | Verdict |
|---|---|---|
| R1 | Factual and statistical integrity, internal consistency | Revision risk **high**, narrowly located |
| R2 | Citations, bibliography, build/validation correctness | Citation integrity **strong**; 1 build blocker + 4 must-fix |
| R3 | Style-contract compliance, argument coherence | Two of three things the diff does are sound; the Ch5 trim is not shippable |

All three independently converged on the same core failure without shared context.

---

## What the diff does well (verified, do not regress)

- **Bibliography metadata is honest, not plausible-looking.** R2 resolved all four upgraded entries
  against the Crossref REST API: `exploring` (10.1145/3800645.3813095), `obscuring`
  (10.1145/3772318.3790918, 10 authors in order), `yantis1984` (10.1037/0096-1523.10.5.601),
  `wallis2015` (10.1167/15.8.3). Every field matches the publisher record. The two
  `VERIFY BEFORE SUBMISSION` markers were **resolved, not deleted**.
- **Cite-key ↔ bib-key correspondence is a perfect bijection.** 64 cited, 64 defined, 0 missing,
  0 orphans, 0 duplicates. `thesis.log` shows zero undefined references or citations.
- **The statistical core reproduces.** R1 independently recomputed H1 (t = 2.660, dz = 0.645,
  95% CI [0.627, 5.553]), the H2b TOST (p = .00021 / .1334), all four H2a band rows, the SESOI
  arithmetic, and the new `4,125` trial count — the last from the raw CSVs in
  `authored/raw/blocka/`, to the unit. No reported statistic could be broken.
- **The de-overclaiming pass is largely correct science.** Converting absolute assertions to
  model-relative or search-relative claims (`ch2.tex:14`, `ch2.tex:24`, `ch4.tex:181`,
  `ch2.tex:209–212`) is the right epistemic move.
- **No limitation, ethics statement, threat to validity, or hypothesis statement was lost** from
  ch6/appendix. REAMS approval (27 May 2026, ref 6061) survives. All eleven ch6 limitations survive.
- **Examiner items P1, P2, P5, P7 are correctly discharged.** `\figplaceholder` usage is now zero;
  all 24 figure targets resolve on disk.

---

## BLOCKERS — verified by the parent, not just reported

Each of the following was re-checked directly by the orchestrating session against the named file.

### B1. Stray literal `I` typeset in the results chapter
`content/ch5.tex:372` reads `\end{figure}I`. **Parent-verified** by grep. It compiles silently and
renders as a one-character paragraph on body p. 63 of the submitted PDF, between "Figure 5.4 shows
every participant's pair." and the §5.7.2 heading. Found independently by R1 and R3.

### B2. The Block B exclusion sentence is factually inverted
`content/ch5.tex:329`: *"Three participants are excluded from Block B (including p1)."*

**Parent-verified against `authored/analysis-2026-08-08-pooled-n17.md`:**

| Line | Says |
|---|---|
| `:80` | `\| P1 \| 67.5% \| 62.5% \| −5.00 \|` — **P1 is IN the Block B analysed set** |
| `:146` | "**P1 is excluded from Block A.**" — the exclusion is Block A, not Block B |
| `:154` | "**P9** — experimenter decision on the day: the participant was confused during the video blocks" |
| `:156` | "**P15's own Block B** (206 FA / 246); … P15 and P16 join as one participant" |
| `:159` | "Block A is retained for P3, P9 and P43" |

So the sentence (a) asserts a P1 Block B exclusion the analysis file contradicts, (b) deletes P9 —
which `examiner-review-fixes-2026-08-08.md` explicitly forbids ("Do not delete the P9 disclosure"),
and (c) drops the P15/P16 merge, the only text reconciling "eighteen tested" with n = 17 / n = 15.
As written the arithmetic gives n = 16, not the reported 15. Lowercase `p1` also breaks the P-ID
convention. Found independently by R1 and R3.

### B3. The single experimenter-reported value is no longer disclosed anywhere
`grep -rn "experimenter-report" content/` returns nothing. **Parent-verified** in the
`generate_all.py` diff: the explanatory comment and the `P4 (NoFilter reported)` legend entry were
both deleted, the variable was renamed `reported` → `dotted`, and the line style changed `'--'` →
`':'`. **The visual distinction survives with its meaning removed.**

Three authorities require the disclosure:
- `.claude/skills/dissertation-writing/SKILL.md`: "If one number in a table came from an
  experimenter's note rather than an exported file, say so where the number appears."
- `authored/RESULTS-FROZEN-2026-08-06.md:29`: "**Must be flagged in the write-up:** P4's NoFilter
  value (98.4%) is experimenter-reported, not measured."
- `examiner-review-fixes-2026-08-08.md`, do-not-break list: "The disclosed experimenter-reported P4
  value and its sensitivity check (§5.7)."

R1 independently confirmed the underlying fact from raw data: `authored/raw/blocka/` contains exactly
one P4 file, which is the **Filter** arm. No NoFilter export exists.

Residue left behind: `ch5.tex:59` still says "**file-backed** scored Block A trials", a qualifier
whose antecedent was deleted, and which is precisely *why* the count is 4,125 and not 4,760.

### B4. The evidence record was edited to agree with the trimmed prose
**Parent-verified** in `git diff -- rewrite-factbase.md`. The diff deletes:
- the P9 exclusion row,
- the P15 exclusion row,
- the paragraph "**P4's NoFilter value (98.4%) is experimenter-reported, not measured.** … the
  manuscript must say so where the number appears."

The same deletions were applied to `chapters/ch6-user-study.md`. All three facts survive intact in
`authored/RESULTS-FROZEN-2026-08-06.md` and `authored/analysis-2026-08-08-pooled-n17.md`, which the
factbase's own Rule Zero names as the sole authority. This is the highest-reputational-risk item in
the set: it reads as an audit trail retrofitted to a write-up, in a pre-registered study.

### B5. Protocol deviations deleted, with three dangling pointers left behind
The paragraph "Three protocol deviations from the 2026-08-06 and 2026-08-07 sessions are reported
here because the protocol is pre-registered" is gone, along with the four-task-version account and
its Kruskal–Wallis justification. `SKILL.md` names pre-registered protocol deviations as one of only
two exceptions to its say-it-once rule.

Orphans created:
- `ch5.tex:59–60` "(task versions and **provenance**: Section 5.7)" → §5.7 no longer carries either
- `ch5.tex:177` "earlier sessions ran shorter versions of the same task (Section 5.7)" → same
- `ch5.tex:492` "the participant whose unexplained Block A re-run drop is recorded in Section 5.7"
  → `grep -rn P25 content/` now returns nothing
- `ch5.tex:317` still calls the sample shortfall "the study's **largest** protocol deviation" —
  a superlative with nothing left to be largest of
- `appendix.tex:242` still promises deviations "are reported rather than" hidden
- `ch5.tex:359–363` still argues from "the early task" and "the harder version", never introduced

This is also a direct regression against `supervisor-feedback-2026-07-23.md` Point B, which is
entirely about not aggregating across task versions. The deleted paragraph was the answer to it.

### B6. The submitted PDF has no usable text layer *(pre-existing, not caused by this diff)*
R2 ran `pdffonts thesis.pdf`: every body font is **Type 3, `uni = no`**, because `thesis.tex:29`
loads `\usepackage[T1]{fontenc}` with no scalable T1 family, so pdfTeX falls back to 600 dpi `.pk`
bitmaps. `pdftotext` returns `PFI hy the eriphery gosts omething` where the document says
"2.1 Why the Periphery Costs Something".

R2 verified the fix in an isolated sandbox: adding `\usepackage{lmodern}` before the `fontenc` line
produces embedded Type 1 fonts with `uni = yes` and exact text extraction, with page count and
layout unaffected.

**Consequence if unfixed:** the PDF cannot be searched or copy-pasted, and any text-extracting
submission portal or similarity checker receives garbage.

### B7. Seven citations render as `(Meta, 024a)` *(pre-existing)*
**Parent-verified.** `refs.bib:258,265,272,279` use `year = {2024a}`…`{2024d}`. `apalike.bst` builds
the citation label from the **last four characters** of the year, so `thesis.bbl:214` emits
`\bibitem[{Meta}, 024a]{meta2024a}` and natbib takes the in-text label from there. The reference
*list* is unaffected, which is why it survived proofreading. Affected sites: `ch2.tex:231,233,236`
and `ch3.tex:101,137,298,299`. `meta2025` with `year = {2025}` renders correctly.

---

## Fixes worth doing now (unambiguous — no scope decision, restore the project's own rules)

| # | Fix | File |
|---|---|---|
| F1 | Delete the stray `I` | `ch5.tex:372` |
| F2 | Rewrite the exclusion sentence from `analysis-2026-08-08-pooled-n17.md:146–159`; fix `p1` → `P1` | `ch5.tex:329` |
| F3 | Restore the P4 measured-vs-reported disclosure, its `p = .0175` vs `.0212` sensitivity check, the figure caption clause, and the legend entry; regenerate the PNG | `ch5.tex`, `generate_all.py` |
| F4 | Revert the deletions in `rewrite-factbase.md` and `chapters/ch6-user-study.md` | both |
| F5 | Fix `year = {2024a}`→`{2024}` ×4 (and disambiguate via the author field if distinct labels are wanted) | `refs.bib:258,265,272,279` |
| F6 | Replace `\citealp{...}'s` with `\citet` — a year cannot take a possessive | `ch4.tex:358,426` |
| F7 | Figure caption: "The three study treatments" → "The two evaluated treatments and Soft Dark". Soft Dark was never shown to a participant (`ch6.tex:75`) | `ch1.tex:112` |
| F8 | Abstract: "salience **by regrading** the periphery" → "salience **re-grading**" (matches ch1/ch3/ch4/ch5 and is a grammatical noun phrase) | `thesis.tex:98` |
| F9 | Abstract: "**about** two standard acuity-chart steps" → "**at least** two". §4.11.2 argues explicitly that the measurement is a lower bound (`ch4.tex:748`, `ch6.tex:19`) | `thesis.tex:105` |
| F10 | Ch1 opening: the only contraction in the manuscript ("don't"), a clause with no antecedent ("that could achieve that"), and a comma splice | `ch1.tex:17–22` |
| F11 | Delete document meta-narration: "Because they are discussed from here on, the system's modes need naming now…" | `ch1.tex:98` |
| F12 | Split the semicolon used as sentence glue | `ch5.tex:177` |
| F13 | Acknowledgements sentence is ungrammatical: "to those" has no antecedent, "along with" appears twice | `thesis.tex:125–128` |
| F14 | Fix the dangling "luminance-dominance law **above**" — §2.1 was rewritten and no longer introduces the phrase | `ch2.tex:61` |
| F15 | Restore "The full query protocol is archived in the project record." `ch6.tex:50` still leans on "the documented search protocol" as the entitlement for the novelty claim | `ch2.tex:245` |
| F16 | Update `README.md`: 18 tested / 17 / 15 (says fifteen/fourteen/thirteen); zero `\figplaceholder` remain (says four); collection ended (says still open); point at `analysis-2026-08-08-pooled-n17.md` as the authority | `README.md:13,65,76` |
| F17 | Build recipe does not converge: R2 ran the documented four passes on a clean copy and pass 4 still emits "Label(s) may have changed". A fifth pass clears it and reproduces the committed PDF byte-for-byte (6,605,573 B vs 6,605,567 B at four passes) | `README.md:44`, `build.sh`, `build.bat` |

---

## DECISIONS REQUIRED — loop paused here

These are not reviewer suggestions I can apply unilaterally. Each has a scope, presentation, or
ethics consequence.

### D1. How to repair the Chapter 5 trim — restore, or remove the pointers?
The manuscript was deliberately cut from 34,086 to 21,129 words. Restoring ~60 lines of provenance
and deviation reporting spends that budget back. Two coherent repairs:

- **(a) Restore from `HEAD`** the exclusions list, the provenance paragraph, and the three protocol
  deviations, then re-apply only the genuine improvements on top ("up to 140", the 4,125 count).
  Highest integrity, costs ~350 words.
- **(b) Write one compact §5.7 provenance paragraph** covering the four task versions and why pooling
  is justified, the P4 experimenter-reported value plus its sensitivity check, the P41/P43 ledger
  gap, and P25's re-run. Repairs `ch5.tex:60`, `:177` and `:492` at once for ~120 words.

R1 recommends (b); R3 recommends (a). **I recommend (b)**, because it discharges every mandated
disclosure without spending the trimming gain, but this is your call.

### D2. Restore the abstract's two pre-registered-shortfall disclosures?
The abstract now asserts only the favourable readings: "a significant accuracy gain … with no
measured loss of peripheral awareness, and the detection benefit concentrated **exactly** in the
region … the system's design predicts", plus a meta-promise that any weaker verdict appears later.

The deleted sentences stated that the primary effect is smaller than its pre-set SESOI, and that the
two-sided equivalence statistic is not established. Both are the chapter's actual verdicts
(`ch5.tex:349–352`, `ch5.tex:388–390`). `SKILL.md`: "If a pre-registered test fails, say it failed."

Also inside this decision: "concentrated **exactly**" overclaims against `ch5.tex:461–466` ("roughly
31% of catches land in a different band"; "the honest reading is a general positive trend with one
band carrying almost all of the signal"), and the abstract now gives no analysed n (18 tested; every
analysis is n = 17, 15 or 16). Was the removal a deliberate framing choice or trimming collateral?

### D3. The `+17.6` vs `+17.8` figure annotation — reviewers directly conflicted, and I resolved it
R3 marked this as a correctly-discharged examiner item (P6). R1 marked it a blocker. **Parent
adjudication: both numbers are real and they are different estimators.**

- `analysis-2026-08-08-pooled-n17.md:102` gives the 20–30° band **pooled** delta as **+17.55**
  (bars 40.2% → 57.7%).
- `ch5.tex:443` Table 5.2 gives **+17.78**, and the table's own caption says "the difference column
  and its interval are **per participant**, which is what the tests use".
- `generate_all.py:204` annotates `ax1`, whose ylabel is **`'Pooled Hit Rate (%)'`**.

So the diff moved the **per-participant** number onto the **pooled** panel. The examiner's P6 note
asked for text/figure agreement and reasonably assumed a single estimator; the real defect was an
estimator-labelling ambiguity, not a wrong value. `generate_all.py`'s own comment block forbids
exactly this mixing. Options:

- **(a)** revert to `+17.6 pp` on the pooled panel and add "(pooled)" / "(per participant)" to the
  two panel annotations — resolves the estimator ambiguity permanently but partly reverses an
  examiner-requested change;
- **(b)** keep `+17.8` and add "(per participant)" to the annotation while the bars stay pooled —
  minimum churn, still invites a reader who subtracts the bar labels to get 17.5;
- **(c)** leave as is.

**I recommend (a).** Your call, since it touches an examiner instruction.

### D4. Add `\usepackage{lmodern}`?
This fixes B6 and makes the PDF searchable. R2 verified page count and layout are unaffected in a
sandbox build, but it changes every glyph in the document from a bitmap to an outline this close to
submission, and I have not independently re-run that check. Do you want it in?

### D5. Acknowledgements — ethics question only you can answer
`thesis.tex:125–128` names three people, one described as having "**also** piloted the study for me".
The "also" reads as "in addition to being one of the eighteen". The manuscript otherwise uses
pseudonymous P-IDs (§5.3) and reproduces the consent form and information leaflet in Appendix A.9.

**Are Sai Eeshwar Diwakar, Diksha Chottani and Yash Jain among the eighteen analysed participants, or
pilot-only?** If pilot-only, the fix is one word (drop "also", say "who piloted the study before the
protocol was frozen"). If any is among the eighteen, the naming needs to come out.

### D6. Four unaddressed examiner items — in scope for this pass?
R3 found these open, and none is caused by this diff:

- **P8** — "pre-registered" is used ~20 times and never defined. `grep -rn "registry\|OSF\|AsPredicted" content/`
  returns nothing; the only mechanism named is `appendix.tex:59`'s "one-page lock-in memo to the
  supervisor". Fix: one clause at first use in ch1 §1.4 and ch5 §5.1.
- **P9** — the abstract was softened, but `ch1.tex:176` ("The study is built so that it cannot return
  'inconclusive'."), `ch5.tex:25`, and `appendix.tex:38` were not, so the abstract and the chapters
  now disagree about how strong the claim is.
- **P10** — the hybrid composite novelty claim (`ch1.tex:104–107`, `ch3.tex:164–174`) carries no
  concession that no participant ever experienced it.
- **P12** — `ch2.tex:141` "This dissertation can be read as **the first**…" is now the only
  unconditioned universal left after the diff softened its neighbours.

---

## Optional / deferred

- Four `Overfull \hbox` > 10 pt, all pre-existing, all unbreakable `\verb` identifiers
  (`ch4.tex:13` at 50.08 pt ≈ 1 cm past the margin, `:236`, `:470`, `ch5.tex:422`).
- `refs.bib` hygiene: `patents` prints the author's own verification memos into the printed
  bibliography verbatim ("title confirmed verbatim via Google Patents… claim-level adjacency only");
  `sitzmann2018` has venue+pages inside `title`; `caine2016` is split mid-phrase at `(pp`;
  `wallis2015` dropped its subtitle; three entries use `and others`.
- Two `@inproceedings` among 62 `@misc` now render in a visibly different format.
- Ch2 §2.8 no longer ends on its positioning claim (the `rusch2013` paragraph was moved after it).
- C1 dropped "three access tiers"; the abstract dropped the quantified GPU figure.
- `appendix.tex:189` conflates pairs with participants ("two short of the pre-registered twenty").
- `ch4.tex:200` says SignPop is inside eleven enum entries; `ch4.tex:404` says it is an eleventh mode
  on top of ten. One is wrong. Pre-existing.
- Supervisor 2026-07-17 Points 2 and 5 (decoy probes, desktop study) appear nowhere, including in
  ch6 future work, against that file's own rule that unfixed items be discussed not hidden.

## Feedback not acted on, with reason

- **R3's [W6] "4,125 does not trace to the frozen results file"** — **overridden.** R1 independently
  reproduced 4,125 from the raw CSVs in `authored/raw/blocka/` by counting `CPT_RESULT` rows minus
  `outcome=first_unscored`, excluding practice files, across the 33 file-backed arms of the 17
  analysed pairs. The number is correct. R3's process point stands — it should be written into the
  frozen record — but it is a bookkeeping task, not a manuscript blocker.
- **R3's [W27] hedge-inflation list** — judgement calls on individual sentences, deferred as polish.
- **R2's [W9] bib style consistency** — cosmetic, and the new form is the better one.
- **R1's [W23] PDF text layer** — same finding as R2's B6, folded into D4.

---

## Why the loop stopped after round 1

Reviewers surfaced six decisions (D1–D6) with scope, presentation, and ethics consequences that the
orchestrator should not settle alone: how much of a deliberately trimmed chapter to restore, whether
to reverse an examiner-requested figure change, whether to change the whole document's font
rendering days before submission, and a participant-anonymity question that depends on facts only the
author holds.

Per the loop policy, this is a stop condition. No fix worker was launched and no file was modified.

## Validation performed by the orchestrator

| Check | Command | Result |
|---|---|---|
| Diff unchanged after review | `git diff --shortstat -- .` | `13 files changed, 9344 insertions(+), 8765 deletions(-)` — identical to pre-review |
| Stray `I` present | `grep -n 'end{figure}I' content/ch5.tex` | `372:\end{figure}I` |
| Exclusion sentence | `grep -n "excluded from Block B" content/ch5.tex` | `329: … (including p1)` |
| P1/P9/P15 ground truth | `grep -n "P9\|P1 \|P15" ../authored/analysis-2026-08-08-pooled-n17.md` | lines 80, 146, 154, 156, 159 — contradict `ch5.tex:329` |
| Factbase deletions | `git diff -- rewrite-factbase.md` | P9 row, P15 row, and the P4 disclosure paragraph removed |
| Meta year fields | `grep -n "year = {2024[a-d]}" refs.bib` | lines 258, 265, 272, 279 |
| Rendered label bug | `grep -n "024[a-d]" thesis.bbl` | `\bibitem[{Meta}, 024a]` ×4 |
| Estimator conflict | `grep -rn "17.78\|17.55"` + read `ch5.tex:430–443`, `generate_all.py:183,204` | pooled = 17.55, per-participant = 17.78; `ax1` ylabel is `'Pooled Hit Rate (%)'` |

**Not run by the orchestrator:** LaTeX rebuild, PDF font/extraction check. Both were performed by R2
in isolated sandboxes under `.pi-subagents/tmpwork/` (untracked); I have not independently reproduced
them.

## Untracked artifacts left on disk

`25377738-dissertation-submission/.pi-subagents/` — reviewer outputs, verification scripts
(`citecheck.py`, `bblcheck.py`, `filecheck.py`, `stylecheck.py`, `overfull.py`), and two sandbox
builds. Untracked; safe to delete.
