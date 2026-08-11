# P1 exclusion audit — is there a reason?

**Question from the author (2026-08-11):** "Don't remove p1, as we don't have a reason to remove him
from either experiments. Find if there is a reason, otherwise, don't."

**Answer:**

| Block | Current treatment | Is there a documented reason? | Verdict |
|---|---|---|---|
| **Block A** (workstation CPT) | **excluded** | **Yes — verified from raw timestamps** | **Keep excluded** |
| **Block B** (driving markers) | **included** | Only a superseded pilot-era flag; not an outlier under the adopted rule | **Keep included** |

**Net effect on the manuscript: no published statistic changes.** n = 17 (Block A), n = 15
(Block B), ESQ n = 16 all stand as published.

---

## Block A — the exclusion reason is real, and I verified it from the raw data

### The claim under test
`analysis-2026-08-08-pooled-n17.md:146`: "**P1 is excluded from Block A.** Its CPT rounds ran about
15 minutes after the Unity blocks closed, so the arm assignment cannot be verified from ledger
windows. Settled and final."

### Why arm assignment needs the ledger at all
Day-1 (22 July) Block A CSVs predate both the `filter=` payload and the `dr_intensity` column, so
the file itself carries **no arm label** — only the slot (`A1`, `A2`). `HANDOFF.md:294–296`.

The documented recovery method (`HANDOFF.md:296–300`) matches each CPT run's `t_ms` span against
that participant's passthrough `BLOCK` rows. `BLOCK` is written at block **completion**, so a CPT
run that sits inside a block ends 16–19 s *before* its `BLOCK` row. That signature recovered
P2, P3 and P33.

### The measurement
Parsed directly from `raw/blocka/*_P1_*.csv` (absolute `t_ms`) and `raw/session_ledger.csv`
(pid `1` — note the ledger uses bare numeric IDs, which is why a `P1` grep returns nothing):

```
P1 CPT run spans
  blockA_nback1_P1_A1_...  run 15:45:11 -> 15:48:42 UTC   (171 rows)
  blockA_nback1_P1_A2_...  run 15:50:42 -> 15:54:13 UTC   (171 rows)

P1 ledger passthrough BLOCK rows (block COMPLETION times)
  idx=2  passthrough  Filter     completed 15:31:35 UTC
  idx=3  passthrough  NoFilter   completed 15:34:02 UTC
```

**Both passthrough blocks completed ~11 and ~14 minutes before the first CPT run even started.**
No block window contains either CPT run, so the required 16–19 s signature cannot exist, and the
headset filter state during P1's two CPT rounds is genuinely unrecorded.

### Two independent documents say the same thing
- `HANDOFF.md:299–301`: "**P1 and P4 NOT recoverable** — both passthrough BLOCK rows were logged
  BEFORE their CPT runs started (blocks X-advanced without the CPT inside), so no window contains
  them."
- `analysis-2026-07-22.md:13`: "Both CPT rounds ran ~15 min AFTER Unity blocks closed — filter state
  unverifiable."
- `analysis-2026-07-22.md:157–158`: "P4 100 vs ~98.4 — A1=NoFilter assumed from plan order and score
  is experimenter-reported; **P1 unassignable**."

### Could plan-order assignment rescue it, as it did for P4 and P33?
P1's `PLAN` row is `PLAN,…,1,BF,V-N`, and the ledger shows the passthrough pair ran Filter (idx 2)
then NoFilter (idx 3). So the *intended* order is known.

It does not rescue P1, for a reason specific to P1: for P33 and P4 the CPT rounds ran **inside or
adjacent to** their passthrough blocks, so plan order plus timing identifies the arm. P1's CPT
rounds ran as a **separate session 11–14 minutes after both passthrough blocks were already
X-advanced and closed**. Plan order tells you what order the *Unity blocks* ran in; it says nothing
about which filter state the headset was in during a CPT session conducted afterwards. Assigning an
arm here would be inference, not recovery — precisely what the factbase's "documented and never
inferred" rule forbids.

**Conclusion: the Block A exclusion is justified. Keep it. Do not attempt to restore P1 to Block A.**

---

## Block B — I looked for a reason to exclude, and it does not survive scrutiny

I found one candidate and rejected it. Recording both, because it is the kind of thing an examiner
reading the working files could raise.

### The candidate
`analysis-2026-07-22.md:13` flags P1: "**FA-flagged both runs** (47% / 39% of presses were false
alarms; **>30% rule**)". P1 is currently *in* Block B, so on its face this looks like the same
response-validity ground used to exclude P3 and P43.

### Why it does not hold
The ">30% of presses" rule was a **pilot-era screening heuristic from 22 July** and is not the rule
the study finally adopted. The adopted Block B rule is narrower — an absolute false-alarm count far
outside the set *plus* corroboration (P3: "hits provably looser, Mann-Whitney p = .0301"; P43:
"more than double the next participant's, on both arms").

Applying the pilot >30% rule to the retained n = 15 set shows it was abandoned for good reason —
it would remove a third of the sample, including participants with far worse rates than P1:

| pid | FA% off | FA% on | >30% on either arm |
|---|---|---|---|
| **P1** | **47.1%** | **39.0%** | **YES** |
| P16 | 48.3% | 68.4% | YES |
| P17 | 41.7% | 40.0% | YES |
| P20 | 74.3% | 63.9% | YES |
| P25 | 21.7% | 57.1% | YES |
| P30 | 46.9% | 52.7% | YES |
| all others | ≤ 24.3% | ≤ 24.3% | — |

P1 is **not** an outlier in the retained set: P20, P16 and P30 are worse on both arms, and P1's
absolute counts (24 and 16) are ordinary next to P3's 539 and P43's 81/54.

**Conclusion: excluding P1 from Block B would be inconsistent with how five other retained
participants were treated. Keep P1 in Block B.**

---

## What this means for the manuscript

1. **No statistic changes.** The published n = 17 / n = 15 / n = 16 set stands. The
   `manuscript-reanalysis-P1-removed-2026-08-11.md` figures are superseded and must not be used.
2. **`content/ch5.tex:329` is still wrong and still must be fixed**, but in the direction the
   analysis file already documents. It currently reads:

   > "Three participants are excluded from Block B (**including p1**)."

   P1 is *in* Block B. The correct statement is that **P1 is excluded from Block A only**, because
   its CPT rounds ran outside the ledger windows that identify the arm, and the three Block B
   exclusions are **P3, P43 and P9**, plus P15's own arms (P15 and P16 joining as one participant).
3. **The Block A exclusion reason should be stated in the manuscript**, since it is currently
   asserted without one. One clause is enough: P1's CPT rounds ran after the Unity blocks closed, so
   the arm assignment cannot be verified from the session ledger and the pair is not used.

---

## Commands and files used

```bash
cd .../Dissertation/authored
# ledger rows for P1 (pid is the bare number 1, not "P1")
awk -F, '$3=="1"' raw/session_ledger.csv
# CPT run spans from absolute t_ms
python - <<'EOF'   # see session transcript; parses raw/blocka/*_P1_*.csv
EOF
```

Sources: `raw/session_ledger.csv`, `raw/blocka/blockA_nback1_P1_A1_*.csv`,
`raw/blocka/blockA_nback1_P1_A2_*.csv`, `HANDOFF.md:294–301`, `analysis-2026-07-22.md:13,157–158`,
`analysis-2026-08-08-pooled-n17.md:146,149–159`, and the `blockb_pooled.py` false-alarm output.
