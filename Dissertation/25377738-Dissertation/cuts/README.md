# Removed material log — August 2026 tightening pass

## Restore points

- `../_pre-tighten-snapshot/` holds a byte-exact copy of every chapter as it stood
  before this pass. That is the authoritative restore point for anything.
- The files in this directory record specific removed blocks so a passage can be
  found without diffing a whole chapter.

## The rule used for cuts

A citation earns its place if it backs (a) a value we chose, (b) the claim that the
problem is real, or (c) a method we used. Literature that only surveys the field was cut.
Prose was cut where it narrated project history ("we tried X, it failed, so Y") rather
than stating the fact, or where it repeated something another chapter already owns.

## What was removed

| File | Contents |
|---|---|
| `ch2-removed-references.txt` | 27 reference entries cited only in Chapter 2 and backing no design decision |
| `ch4-removed-failure-museum-entries.txt` | Failure-museum items 5–8 (OneDrive builds, screenshots, controller state, orphaned scene objects). Items 6 and 7 survive as one compressed paragraph |
| `ch5-removed-criteria-summary-table.txt` | §5.9, a table restating the T1–T5 criteria already given in §5.3–5.7 |
| `ch7-removed-decision-row-prose.txt` | Long-form restatement of Chapter 6's decision table, rows 1–5 |

Prose cuts not listed above are recoverable from the snapshot directory. The largest were:
Chapter 2 §2.2 (additive-guidance survey), Chapter 2 §2.6.3 (educational contexts, a
scenario the study does not run), Chapter 1 §1.3 (a 320-word duplicate of Chapter 6 §6.1.2),
Chapter 6 §6.5.4 (task-form decision history), and Chapter 8 §8.1 (chapter-by-chapter recap).
