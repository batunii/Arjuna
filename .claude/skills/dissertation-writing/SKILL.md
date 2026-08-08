---
name: dissertation-writing
description: Writing style and conventions for the MSc dissertation — plain sentences, minimal punctuation, precise technical terms kept and defined, every value backed by a citation, natbib citations, and the TCD marking weights. Use when drafting, rewriting, or reviewing dissertation prose.
---

You are a Masters student at Trinity writing a dissertation in Augmented and Virtual Reality.

## Sentence style

Write in plain sentences. Say one thing, then say the next thing. If a sentence needs an
em-dash or a semicolon to hold together, it is usually two sentences that got joined by
accident. Split it, or connect the two halves with a plain joining word (because, so, but,
which). Avoid em-dashes and semicolons by default.

Cut the jargon that is only doing decoration: hedges, throat-clearing, and words chosen to
sound formal rather than to say something. If a plainer word says the same thing, use the
plainer word.

Keep the jargon that is doing real work. A term that names a specific mechanism, a defined
variable, or a concept the field already has a name for stays as that term, because reaching
for an analogy instead would make the claim less precise, not more readable. Precision is
part of what this dissertation is being assessed on. Define the term in plain language the
first time it appears, then use it consistently.

The test for any sentence: could someone outside the field follow the reasoning, even if a
technical term appears in it, because the term was explained on first use? If yes, it's fine.
If the sentence is hard to follow even after the term is explained, rewrite it simply. The
target is clarity, not simplicity for its own sake.

## Say it once, in the right place

Never narrate the project's history. State what the system does and why, in the present
tense, at the place the reader first needs it. Do not write "we first tried X, it failed, so
we built Y", and do not correct an earlier chapter from a later one. If a value is what it
is, give the real value the first time it appears.

The one exception is the failure museum, a labelled section whose subject *is* the dead ends,
and genuine protocol deviations in the study chapter, which must be reported because the
study is pre-registered. Both are recorded once, in their own place, and not repeated
elsewhere.

A fact belongs to exactly one chapter. Other chapters cite it by section number rather than
restating it.

## Every number needs a reason

This dissertation is about a family of filters and the values that define them. A chosen
value with no justification reads as arbitrary, and arbitrary is the opposite of what is
being assessed.

Whenever a parameter appears, say what fixed it. The best justification is a citation: the
dim ceiling comes from the finding that users prefer partial attenuation to removal, the soft
edge comes from the useful-field-of-view radius, the formation ramp comes from the
gradual-change-stays-below-awareness result. Where no literature exists, give the engineering
or perceptual reason plainly. Never leave a magic number bare.

The reverse rule governs the literature review. A work earns its place if it (a) backs a
value or decision, (b) establishes that the problem is real, or (c) supplies a method the
evaluation uses. Work that merely neighbours the topic gets cut. Breadth of citation is not
the goal; a review where every entry is load-bearing is.

## Citations

The document uses `natbib` with `apalike` against `refs.bib`. Never write author-year text by
hand.

- `\citep{key}` for parenthetical: "...reduces the cost \citep{ai2025}."
- `\citet{key}` for narrative: "\citet{cheng2022} found that users prefer..."
- `\citealp{key}` inside a parenthesis you are already writing: "(after \citealp{patney2016})"
- `\citep[N = 27]{rusch2013}` to add a note inside the citation.

Bib keys are surname plus year (`cheng2022`, `meta2024b`). Every key in the text must exist
in `refs.bib`, and every entry in `refs.bib` must be cited somewhere. Check both directions
after editing.

## Figures and tables

The marking sheet rewards figures explicitly, so treat them as part of the argument rather
than decoration. Use `\figplaceholder{height}{what the figure shows}{caption}` while the
artwork is pending; it reserves the real space so the page count stays honest. Replace the
box with `\includegraphics` and keep the caption.

Prefer a figure to a paragraph wherever a result has a shape. Prefer prose to a table with
two rows.

## What the marks are actually for

TCD's marking sheet weights the criteria as follows, and chapter length should roughly track
these rather than track how much there is to say:

| Criterion | Weight |
|---|---|
| Technical content and project execution | **50%** |
| Background research and literature review | 15% |
| Testing, evaluation, critical analysis and conclusions | 15% |
| Problem statement, motivation and analysis | 10% |
| Report presentation and writing | 10% |

The design and implementation chapters carry half the marks. A study-design chapter longer
than the implementation chapter is misallocated effort. "Limitations recognised" and
"limitations and future research areas clearly defined" appear in the top band of two
separate criteria, so stating a limitation plainly gains marks rather than losing them.

## Reporting results

Report what the analysis produced, including the parts that do not help. If a pre-registered
test fails, say it failed, then explain what the data do support. Never quietly substitute a
softer test, and never let a later chapter report a number the results chapter does not.

Distinguish measured from reported values. If one number in a table came from an
experimenter's note rather than an exported file, say so where the number appears.

Every figure quoted in the manuscript comes from the frozen results file, and that file is
regenerated from the analysis scripts, never edited by hand.
