#!/bin/sh
# Full build. All four passes are required:
#   1. pdflatex  writes thesis.aux + content/*.aux (citations, labels) and .toc/.lof/.lot
#   2. bibtex    reads thesis.aux, follows \@input into content/*.aux, writes thesis.bbl
#   3. pdflatex  pulls in .bbl, .toc, .lof, .lot
#   4. pdflatex  settles page numbers in the cross-references
#
# Never delete content/*.aux between steps 1 and 2 — bibtex needs them, and without
# them it silently produces an EMPTY bibliography.
set -e
pdflatex -interaction=nonstopmode thesis
bibtex thesis
pdflatex -interaction=nonstopmode thesis
pdflatex -interaction=nonstopmode thesis
echo
echo "--- build summary ---"
grep -c "Citation .* undefined" thesis.log 2>/dev/null | sed 's/^/undefined citations: /' || true
grep -c "Reference .* undefined" thesis.log 2>/dev/null | sed 's/^/undefined references: /' || true
grep -oE "Output written on thesis.pdf \([0-9]+ pages" thesis.log || true
