#!/usr/bin/env python3
"""
Full analysis run: parse -> H1 -> H2 -> H3 -> Holm -> markdown report.

Implements the confirmatory pipeline of testing-strategy-v2.md section 8:
  * H1 is the single primary endpoint (no correction).
  * Secondary family {H2a, H2b, H3} is Holm-corrected.
  * H3 needs the paper TLX scores as CSV:
        pid,condition,mental,physical,temporal,performance,effort,frustration
    (condition in A1..A4,B1,B2; global TLX = unweighted mean of the six).
    H3 = global TLX lower in A4 than A2, one-tailed Wilcoxon; the exploratory
    2x2 Vignette x Distractor TLX interaction (the reason TLX follows all four
    A conditions) is reported alongside, uncorrected, labelled exploratory.
  * Block C ratings CSV (see likert.py) is optional; exploratory.
  * Ends with the decision-table row (section 8.3) implied by H1 + H2b.

CLI:
    python Tools/analysis/report.py <sessions_dir> [--tlx-csv f] [--ratings-csv f]
                                    [--sesoi 0.5] [--margin 0.10] [--out results.md]
"""

import argparse
import sys
from pathlib import Path

import numpy as np
import pandas as pd
from scipy import stats

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
from analysis.parse import parse_dataset            # noqa: E402
from analysis.h1_lmm import run_h1                  # noqa: E402
from analysis.h2_tost import run_h2                 # noqa: E402
from analysis.likert import run_likert              # noqa: E402

TLX_SUBSCALES = ["mental", "physical", "temporal", "performance", "effort", "frustration"]


def run_h3(tlx: pd.DataFrame) -> dict:
    tlx = tlx.copy()
    tlx["global"] = tlx[TLX_SUBSCALES].mean(axis=1)
    wide = tlx.pivot_table(index="pid", columns="condition", values="global")
    need = {"A2", "A4"}
    if not need.issubset(wide.columns):
        return {"skipped": f"TLX csv lacks conditions {sorted(need - set(wide.columns))}"}
    ok = wide[["A2", "A4"]].dropna()
    diff = ok["A4"] - ok["A2"]                      # negative = vignette lowers workload
    w = stats.wilcoxon(diff, alternative="less")
    t = stats.ttest_rel(ok["A4"], ok["A2"], alternative="less")
    out = {
        "n": int(len(ok)),
        "global_tlx": {"A2_off_distr": float(ok["A2"].mean()), "A4_on_distr": float(ok["A4"].mean())},
        "mean_diff": float(diff.mean()),
        "wilcoxon_p_one_sided": float(w.pvalue), "t_p_one_sided": float(t.pvalue),
        "dz": float(diff.mean() / diff.std(ddof=1)) if diff.std(ddof=1) > 0 else float("nan"),
        "supported": bool(w.pvalue < 0.05 and diff.mean() < 0),
    }
    if {"A1", "A3"}.issubset(wide.columns):
        full = wide[["A1", "A2", "A3", "A4"]].dropna()
        interaction = (full["A2"] - full["A1"]) - (full["A4"] - full["A3"])
        ti = stats.ttest_1samp(interaction, 0.0, alternative="greater")
        out["exploratory_2x2_interaction"] = {
            "mean": float(interaction.mean()),
            "t_p_one_sided": float(ti.pvalue),
            "note": "workload analogue of H1; exploratory, uncorrected",
        }
    return out


def holm(pvals: dict) -> dict:
    """Holm step-down across the secondary family; returns adjusted p per key."""
    items = sorted(((p, k) for k, p in pvals.items() if p is not None))
    m = len(items)
    adjusted, running_max = {}, 0.0
    for rank, (p, key) in enumerate(items):
        adj = min(1.0, (m - rank) * p)
        running_max = max(running_max, adj)         # enforce monotonicity
        adjusted[key] = running_max
    return adjusted


def decision_row(h1: dict, h2b: dict) -> str:
    lo, _hi = h1["recovery_ci90"]
    mc1_ok = h1["mc1_cost_off"]["passes"]
    h1_supported = mc1_ok and lo >= h1["sesoi"]
    if not mc1_ok:
        return ("Row 5 — MC1 failed in the main study: no reliable distractor cost; H1 not "
                "interpretable; fall back on H2a/H2b + Block C as the evidential core.")
    if h1_supported and h2b.get("equivalent_within_margin"):
        return ("Row 1 — System works: reduces distraction cost without measurable harm to "
                "peripheral awareness.")
    if h1_supported and h2b.get("conclusive_harm"):
        return ("Row 2 — Works for focus at a quantified situational-awareness cost; mode choice "
                "must be context-dependent.")
    if h1_supported:
        return ("Row 1/2 boundary — H1 supported; H2b neither equivalent nor conclusively harmful: "
                "report the peripheral CI as a bounded estimate.")
    if h1["recovery_ci90"][1] < h1["sesoi"]:
        return ("Row 3 — Conclusive negative: the vignette recovers at most "
                f"{h1['recovery_ci90'][1]:.0%} of the distraction cost (below the {h1['sesoi']:.0%} SESOI).")
    return ("Row 4 — Benefit estimate spans the SESOI: report the estimate with its CI as a "
            "bounded, honest conclusion.")


def fmt(x, nd=3):
    return f"{x:.{nd}f}" if isinstance(x, (int, float)) and not np.isnan(x) else str(x)


def build_report(sessions_dir: Path, tlx_csv: Path = None, ratings_csv: Path = None,
                 sesoi: float = 0.5, margin: float = 0.10) -> str:
    tables = parse_dataset(sessions_dir)
    h1 = run_h1(tables["cpt_trials"], sesoi)
    h2 = run_h2(tables["probe_trials"], tables["events"], margin)
    h3 = run_h3(pd.read_csv(tlx_csv)) if tlx_csv else {"skipped": "no TLX csv supplied"}

    secondary_p = {
        "H2a": h2["h2a_central"].get("t_p_one_sided"),
        "H2b": h2["h2b_peripheral"].get("p_tost"),
        "H3": h3.get("wilcoxon_p_one_sided"),
    }
    adjusted = holm({k: v for k, v in secondary_p.items() if v is not None})

    lines = []
    a = lines.append
    a("# Study Results Report (auto-generated)")
    a("")
    a(f"Sessions: `{sessions_dir}` — {h1['n_participants']} participants, "
      f"{h1['gee_primary']['n_trials']:,} CPT trials.")
    a("")
    a("## Manipulation check (MC1)")
    m = h1["mc1_cost_off"]
    a(f"- Distractor cost (vignette off): mean {fmt(m['mean'])} ± {fmt(m['sd'])} error-rate points; "
      f"one-tailed t p = {fmt(m['t_p_one_sided'],4)}, Wilcoxon p = {fmt(m['wilcoxon_p'],4)} — "
      f"{'PASSES' if m['passes'] else 'FAILS'}.")
    a("")
    a("## H1 — primary (uncorrected)")
    g = h1["gee_primary"]
    b = h1["paired_benefit"]
    a(f"- GEE logistic interaction: OR = {fmt(g['odds_ratio'])} "
      f"(95 % CI {fmt(g['ci95_or'][0])}–{fmt(g['ci95_or'][1])}), one-sided p = {fmt(g['p_one_sided_directional'],4)}.")
    a(f"- Paired benefit: mean {fmt(b['mean'])} (dz = {fmt(b['dz'])}), "
      f"one-tailed t p = {fmt(b['t_p_one_sided'],4)}, Wilcoxon p = {fmt(b['wilcoxon_p'],4)}.")
    a(f"- Cost recovered: {fmt(h1['recovery_point'])} "
      f"(90 % CI {fmt(h1['recovery_ci90'][0])}–{fmt(h1['recovery_ci90'][1])}); SESOI = {sesoi}.")
    a(f"- Condition error rates: {h1['condition_means']}")
    a(f"- **{h1['verdict']}**")
    a("")
    a("## Secondary family (Holm-corrected)")
    h2a, h2b = h2["h2a_central"], h2["h2b_peripheral"]
    a(f"- H2a central RT: {fmt(h2a['median_rt_ms']['B1_off'],0)} ms (Off) vs "
      f"{fmt(h2a['median_rt_ms']['B2_colorpop'],0)} ms (ColorPop), dz = {fmt(h2a['dz'])}, "
      f"raw p = {fmt(secondary_p['H2a'],4)}, Holm p = {fmt(adjusted.get('H2a'),4)}.")
    a(f"- H2b peripheral TOST (Δ = {margin}): diff = {fmt(h2b['mean_diff'])} "
      f"(90 % CI {fmt(h2b['ci90_diff'][0])}–{fmt(h2b['ci90_diff'][1])}), "
      f"raw TOST p = {fmt(secondary_p['H2b'],4)}, Holm p = {fmt(adjusted.get('H2b'),4)} — {h2b['verdict']}")
    if "skipped" in h3:
        a(f"- H3 workload: skipped ({h3['skipped']}).")
    else:
        a(f"- H3 global TLX A4 vs A2: {fmt(h3['global_tlx']['A4_on_distr'],1)} vs "
          f"{fmt(h3['global_tlx']['A2_off_distr'],1)}, dz = {fmt(h3['dz'])}, "
          f"raw p = {fmt(secondary_p['H3'],4)}, Holm p = {fmt(adjusted.get('H3'),4)}.")
        if "exploratory_2x2_interaction" in h3:
            e = h3["exploratory_2x2_interaction"]
            a(f"  - Exploratory 2×2 TLX interaction: mean {fmt(e['mean'],2)}, "
              f"one-tailed p = {fmt(e['t_p_one_sided'],4)} ({e['note']}).")
    a("")
    a("## False alarms (Block B, descriptive)")
    a(f"- {h2['false_alarms']}")
    a("")
    if ratings_csv:
        a("## Block C sampler (exploratory)")
        lik = run_likert(pd.read_csv(ratings_csv))
        for item, res in lik["items"].items():
            if "skipped" in res:
                a(f"- {item}: {res['skipped']}")
            else:
                a(f"- {item}: medians {res['medians']}, Friedman p = {fmt(res['friedman_p'],4)}, "
                  f"W = {fmt(res['kendalls_w'])}.")
        a("")
    a("## Decision (testing-strategy-v2.md §8.3)")
    a(f"**{decision_row(h1, h2b)}**")
    a("")
    return "\n".join(lines)


def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):           # Windows cp1252 console guard
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("sessions_dir", type=Path)
    ap.add_argument("--tlx-csv", type=Path)
    ap.add_argument("--ratings-csv", type=Path)
    ap.add_argument("--sesoi", type=float, default=0.5)
    ap.add_argument("--margin", type=float, default=0.10)
    ap.add_argument("--out", type=Path, default=Path("results.md"))
    args = ap.parse_args()
    report = build_report(args.sessions_dir, args.tlx_csv, args.ratings_csv,
                          args.sesoi, args.margin)
    args.out.write_text(report, encoding="utf-8")
    print(report)
    print(f"\nwrote {args.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
