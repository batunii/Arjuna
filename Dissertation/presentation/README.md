# Dissertation Presentation

28-slide progress/defence deck for the dissertation, built from `Dissertation/framing.md` and
`Dissertation/chapters/ch1–ch6`. Every design-decision slide carries its research or quantified
backing in a footer; results slides are placeholders wired to a data layer so they fill in without
redesign when the benchmarks and study run.

## Files

| File | What it is |
|---|---|
| `dissertation-presentation.clan` | **Canonical artifact.** Open in CLAN Viewer. Holds the HTML view, `shared/data.yaml` (all volatile numbers), and the decision chain. |
| `deck.html` | Source: YAML frontmatter (structured data + decision) followed by the deck HTML. Edit this for big changes, then repack. |
| `deck-export.html` | Rendered copy for presenting in any browser (fullscreen it with F11). **Generated — do not edit**; regenerate after any repack. |

## Presenting

Open `deck-export.html` in a browser (or the `.clan` in CLAN Viewer). Navigate with ←/→, Space,
PgUp/PgDn, Home/End. Printing produces one page per slide.

## Editing workflows (fast → heavy)

Alias below: `clan` = `"C:\Program Files\CLAN Viewer\clan.exe"`.

**1. Results land / numbers change — `patch-data` (no HTML touched):**
```
clan patch-data dissertation-presentation.clan --agent you --action "T1 results" \
  '{"results":{"t1_gpu":{"status":"DONE","headline":"p95 2.1 ms (Hard Dark) … all study configs within budget"}}}'
```
Keys you omit are kept. The deck's JS binds `data-bind="path.to.key"` elements from
`shared/data.yaml` at load (in CLAN Viewer); the packed HTML defaults show elsewhere — so after
data patches, regenerate `deck-export.html` by repacking or keep presenting from the Viewer.

**2. Reword one slide — `patch-html` with the slide's stable id:**
```
clan patch-html dissertation-presentation.clan patch.html --selector '#s-hypotheses' \
  --patch-action replace --agent you --action "reworded H1 slide"
```
Slide ids: `s-title s-spine s-motivation s-constraint s-foundations s-gaps s-priorart s-tiers
s-walls s-absence s-hybrid s-arch s-fusion s-modes s-dark s-colorpop s-comfort s-failures s-oracle
s-teval s-study s-blocks s-hypotheses s-gates s-results-tech s-results-study s-transfer s-contrib`

**3. Restructure / add slides — edit `deck.html`, repack:**
```
clan pack-html --output dissertation-presentation.clan --agent you --action "why" \
  dissertation-presentation.clan deck.html
clan validate dissertation-presentation.clan
clan read human dissertation-presentation.clan --quiet > deck-export.html
```

## Slide map

1 Title · 2 Spine question · 3 Motivation · 4 Compositing constraint · 5 Research foundations ·
6 Gaps & position · 7 Prior-art search · 8 Three-tier taxonomy (C1) · 9 The walls ·
10 Window-as-absence · 11 Hybrid composite (novelty) · 12 Architecture · 13 Fusion sampling ·
14 Mode family (10 modes) · 15 Dark modes · 16 ColorPop · 17 Comfort & interaction ·
18 Failure museum · 19 Detector-free + oracle bake · 20 Benchmarks T1–T5 · 21 Study design ·
22 Blocks A/B/C · 23 Hypotheses · 24 Gates + decision table · 25 Results: technical (pending) ·
26 Results: study (pending) · 27 Transfer · 28 Contributions & status
