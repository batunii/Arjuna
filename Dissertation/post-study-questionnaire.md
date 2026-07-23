# End-of-Session Subjective Opinion Questionnaire (ESQ)

> Companion to `testing-strategy-v2.md` §7. Fills the gap the existing instruments leave:
> VRSQ = sickness, NASA-TLX = per-condition workload, SUS = usability, Block C Likert = 3 quick
> per-mode items. **This instrument = the participant's considered opinion of the guidance
> effects themselves** — perceived focus benefit, perceived awareness cost, visual comfort,
> acceptance, and preference — collected once, at the end, when they have experienced everything.
>
> Status: **exploratory** (feeds RQ3 / EQ1–EQ3 and the Discussion chapter). No confirmatory
> claims are made from these items; report medians + IQRs and quote the open answers.

---

## Administration

- **When:** end of session, headset off, **after** the post-VRSQ and SUS, **before** the
  study-aims debrief (opinions must be captured before the hypotheses are revealed —
  demand-characteristics defense, `testing-strategy-v2.md` §9).
- **How:** tablet or paper, self-completed; experimenter available for clarification but does
  not paraphrase items. Open questions may be answered verbally and transcribed by the
  experimenter if the participant prefers (note which).
- **Time:** ~5 minutes (23 rated items + 1 ranking + 1 multi-select + 4 open).
- **Framing script (read aloud, verbatim):**
  > "Last questionnaire. This one asks what *you* thought of the visual effects you saw today —
  > there are no right answers, and critical answers are just as useful to us as positive ones.
  > 'The effects' means the ways the display changed: the darkened or greyed-out edges of your
  > vision, and the highlighted areas. Answer for how it felt overall, across the whole session."

**Scale for Sections A–D:** 7-point agreement — 1 = Strongly disagree, 2 = Disagree,
3 = Somewhat disagree, 4 = Neither agree nor disagree, 5 = Somewhat agree, 6 = Agree,
7 = Strongly agree.

Items marked **(R)** are negatively keyed and reverse-scored before aggregation. Positively and
negatively keyed items are mixed within each section to counter acquiescence bias. Item order
below is the administration order.

---

## Section A — Perceived focus benefit
*Construct: did the effects subjectively help concentration on the task area? (EQ2 analogue,
system-level.)*

| ID | Item | Key |
|---|---|---|
| A1 | When the effect was on, it was easier to keep my attention on the task in front of me. | + |
| A2 | I found myself looking away from the task just as often with the effect on as off. | (R) |
| A3 | The effect made it obvious where I was supposed to be looking. | + |
| A4 | The effect made no real difference to how well I could concentrate. | (R) |
| A5 | Things happening at the edges of my vision bothered me less when the effect was on. | + |

## Section B — Perceived awareness cost
*Construct: did participants feel cut off from their surroundings? (Subjective analogue of the
H2b safety question; feeds the focus-vs-awareness trade-off discussion — McLaughlin 2025.)*

| ID | Item | Key |
|---|---|---|
| B1 | With the edges of my vision darkened or greyed out, I was worried I would miss something important. | (R) |
| B2 | I still felt aware of what was going on around me when the effect was on. | + |
| B3 | When something appeared off to the side, I felt I noticed it later than I would have without the effect. | (R) |
| B4 | I felt comfortable *not* being able to see everything around me clearly. | + |
| B5 | Being cut off from the edges of my vision made me uneasy or anxious. | (R) |

## Section C — Visual comfort of the effects
*Construct: comfort of the manipulations themselves (EQ1 analogue), distinct from headset
comfort and from sickness (covered by VRSQ).*

| ID | Item | Key |
|---|---|---|
| C1 | The effects were comfortable to look at for the length of each round. | + |
| C2 | The boundary between the clear area and the altered edges was distracting in itself. | (R) |
| C3 | The way the effect faded in and out (for example when I turned my head) felt natural. | + |
| C4 | The darkened or greyed-out areas caused me eye strain or visual discomfort. | (R) |
| C5 | I stopped noticing the effect after a while — it faded into the background. | + |

## Section D — Acceptance & willingness to use
*Construct: would they actually use it? (EQ3 analogue; Discussion-chapter material.)*

| ID | Item | Key |
|---|---|---|
| D1 | If a headset I owned had this feature, I would turn it on for tasks that need concentration. | + |
| D2 | I would only use something like this if I had no other way to remove distractions (for example, leaving the room). | (R) |
| D3 | I can think of specific situations in my own work or study where I would want this. | + |
| D4 | Wearing a headset is too high a price for the focus benefit I experienced today. | (R) |
| D5 | I would recommend a feature like this to someone who is easily distracted. | + |
| D6 | I would want to be able to adjust the strength and size of the effect myself. | + *(reported separately — design-preference item, not part of the acceptance score)* |

## Section E — Comparative preference

**E1 (ranking).** "You saw the effect in two settings today: **(a)** the darkened window while
you did the task at the desk in the real room, and **(b)** the altered edges while you watched
the driving video. *(If Block C ran:)* You also briefly saw three variations at the end. Rank the
versions you saw from most to least preferred." *(Experimenter lists only the modes this
participant actually experienced; record the ranking.)*

**E2 (multi-select).** "In which of these situations, if any, could you imagine using an effect
like this? Tick all that apply."

- [ ] Studying or reading
- [ ] Focused desk work (writing, coding, drawing…)
- [ ] Working in a busy or shared space (office, café, library)
- [ ] Watching or monitoring something for long periods
- [ ] Gaming or entertainment
- [ ] None of these
- [ ] Other: ______________________

## Section F — Open questions
*Ask verbally if preferred; record verbatim. These feed the thematic coding table alongside the
Block C interview (`testing-strategy-v2.md` §8.2).*

- **F1.** What was the single best thing about the effects you saw today?
- **F2.** What was the single worst or most annoying thing?
- **F3.** Was there a moment when the effect clearly helped you, or clearly got in your way?
  Describe it.
- **F4.** If you could change one thing about how the effect looks or behaves, what would it be?

---

## Scoring & analysis notes

- Reverse-score all (R) items (score′ = 8 − score), then compute per-participant subscale means:
  **Focus benefit** (A1–A5), **Awareness cost** (B1–B5, higher = *more preserved* awareness after
  reversal), **Comfort** (C1–C5), **Acceptance** (D1–D5; D6 reported alone).
- Report: median + IQR per subscale and per item; Cronbach's α per subscale as a descriptive
  internal-consistency check (N = 20 is too small to validate the instrument — say so).
- Convergence checks worth one paragraph each in the Discussion: Focus-benefit subscale vs the
  H1 behavioural effect (do people who *felt* helped show the larger distractor-cost recovery?);
  Awareness-cost subscale vs H2b peripheral hit rates; Comfort subscale vs VRSQ delta.
- E1 rankings: frequency table (per-mode first-choice counts); no significance testing.
- F1–F4 + Block C interview: one thematic coding pass, shared codebook, quotes attributed by
  participant code.
- **Claim-scoping reminder** (`framing.md` §2.2): these are opinions about *the guidance
  mechanism as experienced in this session*. No driving claims, no product-readiness claims;
  "participants reported…" phrasing only.

## Pilot checklist (add to gate G4 dry runs)

- [ ] Completion time ≤ 6 min including open questions.
- [ ] No item requires clarification by more than one pilot participant (rewrite any that does).
- [ ] E1 wording works for a session where Block C was skipped.
- [ ] Tablet form (if used) enforces one response per row and allows skipping open items.

## Ethics note

Same instrument class as the approved questionnaires (Likert opinion items + open feedback);
no sensitive or identifying content. Covered by generic questionnaire wording in the approved
application — verify alongside the VRSQ row in the `testing-strategy-v2.md` §11 amendment table.
