@echo off
REM Full build. All five passes are required; pass 4 still reports
REM "Label(s) may have changed", and pass 5 settles the cross-references.
REM `latexmk -pdf thesis` does the same thing and iterates to convergence on its own.
pdflatex -interaction=nonstopmode thesis
bibtex thesis
pdflatex -interaction=nonstopmode thesis
pdflatex -interaction=nonstopmode thesis
pdflatex -interaction=nonstopmode thesis
