# Adaptive Visual Noise Cancellation for Attention Guidance on a Consumer Passthrough Headset

M.Sc. Computer Science (Augmented and Virtual Reality), Trinity College Dublin.
Student ID 25377738. Supervisor: John Dingliana.

This folder is self-contained. It holds the submitted PDF and everything needed to
rebuild it from source, and nothing else.

## The deliverable

`thesis.pdf` — 128 pages, built from the sources here.

## Contents

| Path | What it is |
|---|---|
| `thesis.pdf` | The built dissertation |
| `thesis.tex` | Document root: title page, declaration, abstract, chapter includes |
| `content/ch1.tex` … `ch8.tex` | The eight chapters |
| `content/appendix.tex` | Appendix A, study protocol artefacts |
| `refs.bib` | Bibliography, 72 entries, natbib + apalike |
| `thesis.bbl` | Formatted bibliography, included so the PDF rebuilds even without BibTeX |
| `tcdthesis.sty` | The official TCD M.Sc. thesis class |
| `TrinityCrest.png` | Crest used by the title page |
| `build.sh` / `build.bat` | Full build, all four passes |

## Rebuilding

```
./build.sh          # or build.bat on Windows
```

Equivalently:

```
pdflatex thesis
bibtex   thesis
pdflatex thesis
pdflatex thesis
```

All four passes are required. Pass 1 writes `thesis.aux` and the per-chapter
`content/*.aux` files; BibTeX reads those to build the bibliography; passes 3 and 4
pull in the bibliography, the table of contents, the lists of tables and figures, and
settle the page cross-references.

**Do not delete `content/*.aux` between passes 1 and 2.** Every `\citation{}` record
lives in the per-chapter aux files, not in `thesis.aux`, so removing them makes BibTeX
emit an empty bibliography with no error.

One non-obvious dependency: `tcdthesis.sty` sets `\pagestyle{fancy}` but does not load
`fancyhdr`, so `thesis.tex` loads it explicitly. Removing that line makes the first
`\chapter` fail on `\undefinedpagestyle`.

## Status of the work reported

Data collection was still open at submission. The study targets N = 20; fifteen people
had been tested, giving fourteen usable paired participants in Block A and thirteen in
Block B. All results are reported as interim throughout, and Chapter 6 Section 6.11
states this explicitly rather than in a footnote.

Figures are reserved as sized placeholder boxes with their captions in place. Twenty-one
are pending artwork.
