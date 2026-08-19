# Dissertation Chapters — Outline, Budgets, and Style Guide

**Title:** Guiding User Attention in Real-World Tasks Using XR Overlays
**Degree:** MSc, Trinity College Dublin. Target: ~100 pages total (~450 words/page equivalent).
**Status (2026-07-05):** All chapters DRAFTED and consistency-passed (36,535 words ≈ 81 prose pages;
~95–100 rendered with figures/tables). Empirical results do not exist yet — Chapters 5–7 present
pre-registered plans in future tense, plus the only real data (offline YOLO bake). Before submission:
resolve every item in `VERIFY-citations.md` against the PDF collection, replace figure placeholders,
consolidate per-chapter references into one bibliography, and complete the AI-use declaration in
`front-matter.md` per current TCD policy.

Actual word counts: front-matter 801 · ch1 2,129 · ch2 8,362 · ch3 3,760 · ch4 6,111 · ch5 3,109 ·
ch6 7,677 · ch7 3,250 · ch8 1,336.

## Chapter plan and page budgets

| File | Chapter | Pages | Content anchor |
|---|---|---|---|
| `front-matter.md` | Title page, abstract, declaration, acknowledgements | 4 | — |
| `ch1-introduction.md` | 1. Introduction | 7 | `../framing.md` §1, §5 |
| `ch2-related-work.md` | 2. Background and Related Work | 20 | `../lit-review-48papers.md`, `../../.agent-docs/research/attention-guidance-research.md`, prior-art search (`../framing.md` §6.1), evaluation-methods literature |
| `ch3-design-space.md` | 3. A Design Space for DR under Compositing Constraints | 10 | `../framing.md` §3 |
| `ch4-system.md` | 4. System Design and Implementation | 16 | `../../.agent-docs/systems/focus-vignette.md`, `../../FOCUS_VIGNETTE_PROGRESS.md` |
| `ch5-technical-evaluation.md` | 5. Technical Evaluation | 7 | `../framing.md` §3.3; benchmark session log; YOLO bake stats |
| `ch6-user-study.md` | 6. User Study Design and Results | 18 | `../testing-strategy-v2.md`; `../authored/analysis-2026-08-08-pooled-n17.md` (frozen numbers) |
| `ch7-discussion.md` | 7. Discussion | 8 | `../framing.md` §2.3, §4; decision table |
| `ch8-conclusion.md` | 8. Conclusion and Future Work | 4 | contributions C1–C4 |

References: each chapter carries its own reference list under a final `## References (this chapter)`
heading; lists are consolidated into one bibliography at assembly time.

## Style rules (binding)

1. **UK English**, formal academic register, first-person plural sparingly ("we") or passive.
2. **Claim scoping per `../framing.md` §2.2 is law**: no "drive testing"/road-safety claims, no
   product claims, oracle-perception conditioning stays visible, every confirmatory statement traces
   to H1/H2a/H2b and the decision table.
3. Citations as (Author, Year) inline; full entries in the chapter reference list with DOI/URL where
   known. Where a source's full bibliographic identity is uncertain (e.g. known only by PDF filename
   from `../lit-review-48papers.md`), cite best-effort and append **[VERIFY]** — these are collected
   at assembly time for the author to resolve against the actual PDFs.
4. Figures are placeholders: `[Figure N.M — description of what to create/screenshot]`. Tables are
   written out in full.
5. Mode names: **SignPop, Soft Dark, Hard Dark** are the modes the dissertation evaluates; ColorPop is the Tier-3 mode SignPop is built on. The older names
   (Dynamic/Semi-Dynamic/Static, Blur) appear only when describing the design evolution, explicitly
   marked as superseded. Scenario names: workstation (Block A), driving (Block B), sampler (Block C).
6. Honest register: failures, dead ends, and limitations are reported as content, not confessed in
   passing. The "failure museum" is a feature.
7. No fabricated data, no fabricated citations, no placeholder statistics presented as real. Results
   that don't exist yet are described as planned, in future tense, full stop.
8. Cross-references by chapter number (e.g. "see Chapter 3"); section-level cross-refs sparingly.
