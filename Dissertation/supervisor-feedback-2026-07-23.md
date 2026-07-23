# Supervisor Feedback — John Dingliana, 2026-07-23 (email reply to Day-1 pilot update)

Reply to the Day-1 naive-pilot email (`authored/email-john-2026-07-23.md`). Overall tone:
"fairly rigorous and well thought out"; discussion scheduled for 2026-07-24. Three
time-limited points, mapped below. Companion to `supervisor-feedback-2026-07-17.md`.

---

## Point A — Inferential/uncertainty analysis expected in the final evaluation

> Beyond aggregated improvement counts/percentages: distribution of responses (SD, IQR,
> combined plot of all responses), confidence intervals (95% CI), OR significance tests
> (paired t-test, Wilcoxon signed-rank). May push MSc expectations; adding one or more
> raises chances of a distinction-level grade in the evaluation component.

**Status: ALREADY COMMITTED in the pre-registered plan — say so tomorrow.**
`testing-strategy-v2.md` §8 specifies, per hypothesis: trial-level logistic LMM with
crossed subject×item random effects (H1), paired t-test with Wilcoxon fallback (H2a),
TOST equivalence with 90% CI (H2b), dz / odds-ratio effect sizes, and Shapiro-Wilk-gated
test selection. That is a superset of every example John lists. What to add on top
(cheap, distinction-signalling):
- **Combined response plots** — per-participant paired dot/line plots (all responses
  visible, not just means) for every headline contrast; John explicitly names this.
- **Report IQRs/SDs alongside every median** in the results chapter (the pilot analyses
  so far are deliberately descriptive; the dissertation tables must carry dispersion).
- Note for framing: the Day-1 write-ups already refuse p-values at n=4 — keep that
  discipline and cite it as reasoning, not omission.

## Point B (MORE IMMEDIATE) — Version churn shrinks usable n

> Uncertainty is driven by sample count and response variance. Responses from different
> test versions generally should not be aggregated; smaller samples per version = higher
> statistical uncertainty. Be careful about modifying the tests as you go — or at all.

**Status: THE live risk — this is aimed squarely at the iteration history.** Versions to
date: probe black → yellow on-top → (rope pending); window 4×4 → 6×5 → 6×4; periphery dim
7% → 28% → 21% → 15%; Block A 140-trial CPT → 84-trial 1-back (→ hardened variant
pending); pool v1 → v2 re-split (→ 2 s re-screen pending). Every pending change forks the
dataset again. Mitigations already in place: the pilot-gated design never intended Day-1
data to aggregate with the formal study (gate evidence only); within-participant pairing
keeps each version internally valid. But John's point stands: **the formal N≈20 must run
on ONE frozen configuration.**

## Point C — Commit now (or max one more iteration), then collect

> Weigh improving the experiment further vs biting the bullet: current setup or max one
> more iteration, then get as many samples as possible and report for good or bad. With
> decent analysis depth, even a "bad" result is a valuable outcome.

**Decision framework adopted (pending 2026-07-24 discussion): ONE final iteration bundle,
then freeze.** Everything already queued goes in it — nothing gets a second turn:

1. **Probe = rope ring** (decided; in build) — validate with ONE self-pilot pass against
   the 60–85% band before freezing; fallback INSIDE the same iteration is yellow+outline
   (inspector-selectable, no rebuild). After freeze, no probe changes.
2. **Pool re-screen**: ≥2 s minimum duration, re-split, one baseline screening pass.
3. **Block A hardening**: shorter SOA and/or lure no-gos (NOT 2-back), piloted once, locked.
4. **Protocol fixes** (briefing script, FA-failing practice gate, CPT-round discipline,
   live FA counter) — procedure, not stimulus; zero aggregation cost.
5. **Window/dim: FREEZE at 6×4° + 24° edge, m_popGreyDim 0.21.** The 8×6 research note
   stays a documented alternative (limitations/future work), NOT a change.
6. **Decoys: NO new harness machinery.** The de-facto decoy analysis (unassisted persons,
   analysis-side only) covers Point 2 without forking the version. True decoy probes =
   future-work section.
7. **BF-cell recruitment** is sampling, not versioning — do freely.

After the bundle: freeze the build (tag it), run the remaining participant pool on it,
report for good or for bad. "Bad result + deep analysis = valuable outcome" is now the
explicit fallback framing — aligns with the §8.3 decision table where every outcome is a
conclusive row.

---

## Agenda seeds for the 2026-07-24 discussion

1. Confirm the one-iteration bundle contents (above) and the freeze point.
2. Point A: walk through §8's pre-registered tests; ask whether LMM + TOST + combined
   plots is the right "distinction-level" depth or whether he wants a specific extra.
3. Day-1 data's role: gate/pilot evidence + design-motivation narrative only, never
   pooled with formal results — confirm he agrees this is the right handling.
4. The set-confound (§3 of analysis-2026-07-22.md) and whether BF-forcing for the next
   sessions is acceptable counterbalance hygiene.
5. The felt-vs-measured awareness dissociation (ESQ B3) as a Discussion-chapter thread.
