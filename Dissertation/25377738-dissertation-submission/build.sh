#!/bin/sh
# Full build. All five passes are required:
#   1. pdflatex  writes thesis.aux + content/*.aux (citations, labels) and .toc/.lof/.lot
#   2. bibtex    reads thesis.aux, follows \@input into content/*.aux, writes thesis.bbl
#   3. pdflatex  pulls in .bbl, .toc, .lof, .lot
#   4. pdflatex  moves page numbers in the cross-references, and still warns
#                "Label(s) may have changed" because filling in the lists of tables and
#                figures shifts the page breaks the longtables sit across
#   5. pdflatex  settles those cross-references
#
# `latexmk -pdf thesis` does the same thing and iterates to convergence on its own.
#
# Never delete content/*.aux between steps 1 and 2 — bibtex needs them, and without
# them it silently produces an EMPTY bibliography.
set -e
pdflatex -interaction=nonstopmode thesis
bibtex thesis
pdflatex -interaction=nonstopmode thesis
pdflatex -interaction=nonstopmode thesis
pdflatex -interaction=nonstopmode thesis
echo
echo "--- build summary ---"
grep -c "Citation .* undefined" thesis.log 2>/dev/null | sed 's/^/undefined citations: /' || true
grep -c "Reference .* undefined" thesis.log 2>/dev/null | sed 's/^/undefined references: /' || true
grep -c "Label(s) may have changed" thesis.log 2>/dev/null | sed 's/^/unsettled cross-references (rerun needed if nonzero): /' || true
grep -oE "Output written on thesis.pdf \([0-9]+ pages" thesis.log || true
