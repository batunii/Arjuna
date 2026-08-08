#!/usr/bin/env python3
"""Generate all matplotlib-based dissertation figures."""
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from matplotlib.patches import FancyBboxPatch, FancyArrowPatch
import numpy as np
import os

OUT = os.path.dirname(os.path.abspath(__file__))
os.makedirs(OUT, exist_ok=True)

plt.rcParams.update({
    'font.family': 'serif',
    'font.size': 11,
    'axes.titlesize': 13,
    'axes.labelsize': 11,
    'figure.dpi': 300,
    'savefig.dpi': 300,
    'savefig.bbox': 'tight',
    'savefig.pad_inches': 0.15,
})

# ============================================================
# FIGURE 1: Slopegraph — Block A per-participant accuracy (H1)
# ============================================================
def fig_slopegraph():
    # Measured per-participant accuracy, Block A, n = 14.
    # Source: python3 Tools/analysis/blocka_pooled.py Dissertation/authored/raw/blocka
    # These reproduce RESULTS-FROZEN-2026-08-06.md exactly: NoFilter 92.19 (SD 7.13),
    # Filter 95.53 (SD 4.58), delta +3.34, t(13) = 2.40, p = .0322, dz = 0.641, 10/14 up.
    # P4's NoFilter value is experimenter-reported rather than measured (no CSV was
    # exported for that arm), so P4 is drawn dashed and called out in the caption.
    pids = ['P2', 'P3', 'P4', 'P6', 'P9', 'P10', 'P13',
            'P15', 'P17', 'P20', 'P25', 'P26', 'P27', 'P33']
    nf = np.array([97.6, 95.2, 98.4, 84.6, 95.0, 97.8, 100.0,
                   74.8, 91.4, 86.3, 86.3, 89.2, 96.4, 97.6])
    f  = np.array([100.0, 98.8, 100.0, 92.3, 100.0, 100.0, 97.1,
                   90.6, 89.9, 91.4, 96.4, 86.3, 95.7, 98.8])
    reported = pids.index('P4')      # experimenter-reported NoFilter arm

    fig, ax = plt.subplots(figsize=(5, 6))
    for i in range(len(pids)):
        color = '#2c7bb6' if f[i] > nf[i] else '#d7191c'
        ls = '--' if i == reported else '-'
        lw = 0.8
        ax.plot([0, 1], [nf[i], f[i]], color=color, alpha=0.5, linewidth=lw, linestyle=ls)
        ax.scatter([0], [nf[i]], color=color, s=18, zorder=3, alpha=0.7)
        ax.scatter([1], [f[i]], color=color, s=18, zorder=3, alpha=0.7)

    # Group means
    ax.plot([0, 1], [92.19, 95.53], color='black', linewidth=2.5, zorder=4)
    ax.scatter([0, 1], [92.19, 95.53], color='black', s=60, zorder=5)

    ax.set_xlim(-0.3, 1.3)
    ax.set_ylim(72, 101)          # 100% is a hard ceiling on accuracy; never pad above it
    ax.set_xticks([0, 1])
    ax.set_xticklabels(['NoFilter\n(Vignette Off)', 'Hard Dark\n(Vignette On)'])
    ax.set_ylabel('Accuracy (%)')
    ax.set_title('Block A: Per-Participant Accuracy (N = 14)\n+3.34 pp, p = .032, $d_z$ = 0.641')

    blue_patch = mpatches.Patch(color='#2c7bb6', alpha=0.5, label='Improved (10)')
    red_patch = mpatches.Patch(color='#d7191c', alpha=0.5, label='Declined (4)')
    black_line = plt.Line2D([0], [0], color='black', linewidth=2.5, label='Group mean')
    dashed = plt.Line2D([0], [0], color='#2c7bb6', linewidth=0.8, linestyle='--',
                        label='P4 (NoFilter reported)')
    ax.legend(handles=[blue_patch, red_patch, black_line, dashed],
              loc='lower right', fontsize=8)
    ax.grid(axis='y', alpha=0.3)

    fig.savefig(os.path.join(OUT, 'fig_slopegraph_h1.png'))
    plt.close(fig)
    print("  ✓ fig_slopegraph_h1.png")

# ============================================================
# FIGURE 2: Equivalence plot — H2b safety
# ============================================================
def fig_equivalence():
    fig, ax = plt.subplots(figsize=(7, 3))

    # Equivalence margin shading
    ax.axvspan(-10, 10, color='#e0e0e0', alpha=0.5, label='±10 pp margin')
    ax.axvline(-10, color='#d32f2f', linewidth=2, linestyle='-', label='Safety bound (−10)')
    ax.axvline(10, color='#888888', linewidth=1, linestyle='--', alpha=0.5)
    ax.axvline(0, color='black', linewidth=0.5, linestyle=':')

    # Point estimate and CI
    ax.plot(5.58, 0.5, 'o', color='#1565c0', markersize=10, zorder=5)
    ax.hlines(0.5, -0.98, 12.14, colors='#1565c0', linewidth=2.5, zorder=4)
    ax.plot([-0.98, 12.14], [0.5, 0.5], '|', color='#1565c0', markersize=12, zorder=5)

    ax.annotate('+5.58 pp', (5.58, 0.55), ha='center', fontsize=10, fontweight='bold', color='#1565c0')
    ax.annotate('90% CI: [−0.98, +12.14]', (5.58, 0.38), ha='center', fontsize=9, color='#1565c0')

    ax.set_xlim(-16, 18)
    ax.set_ylim(0, 1)
    ax.set_yticks([])
    ax.set_xlabel('Paired Difference in Pooled Hit Rate (pp)')
    ax.set_title('Block B: Pooled Hit Rate vs Safety Margin (H2b, N = 13)')
    ax.legend(loc='upper left', fontsize=9)

    fig.savefig(os.path.join(OUT, 'fig_equivalence_h2b.png'))
    plt.close(fig)
    print("  ✓ fig_equivalence_h2b.png")

# ============================================================
# FIGURE 3: Eccentricity band bar chart — H2a headline result
# ============================================================
def fig_eccentricity():
    # Measured pooled hit rates per band, Block B, n = 13.
    # Source: python3 Tools/analysis/blockb_pooled.py Dissertation/authored/raw
    # Reproduces RESULTS-FROZEN-2026-08-06.md exactly.
    #   <10    62/84  -> 57/72    10-20  74/179 -> 85/185
    #   20-30  39/101 -> 59/107   >30    85/156 -> 88/156
    bands = ['<10°', '10–20°', '20–30°', '>30°']
    nf   = [73.8, 41.3, 38.6, 54.5]
    filt = [79.2, 45.9, 55.1, 56.4]
    # The lower panel is the PAIRED PER-PARTICIPANT difference, which is the estimator
    # the t/Wilcoxon tests and the CI actually belong to. It is deliberately not the
    # pooled difference of the bars above: participants contribute unequal numbers of
    # markers to a band, so the two estimators can disagree, and for <10 they disagree
    # in sign (pooled +5.36, per-participant -1.92). Mixing them inside one glyph would
    # put an interval around a point it was not computed for, so the panel uses the
    # per-participant mean throughout and the caption states the difference.
    delta = [-1.92, +5.01, +16.36, +1.92]
    dlo   = [-12.6, -5.3, +4.2, -6.5]
    dhi   = [+8.7, +15.3, +28.6, +10.4]
    # Colour-pop gate strength across the bands, per Chapter 6, Section 6.11.3.
    gate_strength = [0, 20, 75, 100]

    x = np.arange(len(bands))
    w = 0.35

    fig, (ax1, ax3) = plt.subplots(2, 1, figsize=(8, 6.4), sharex=True,
                                   gridspec_kw={'height_ratios': [2.6, 1]})

    bars1 = ax1.bar(x - w/2, nf, w, color='#bdbdbd',
                    edgecolor='#616161', label='NoFilter', zorder=3)
    bars2 = ax1.bar(x + w/2, filt, w, color='#26a69a',
                    edgecolor='#00796b', label='SignPop', zorder=3)
    for xi, (a, b) in enumerate(zip(nf, filt)):
        ax1.text(xi - w/2, a + 1.5, f'{a:.1f}', ha='center', fontsize=8, color='#424242')
        ax1.text(xi + w/2, b + 1.5, f'{b:.1f}', ha='center', fontsize=8, color='#00695c')

    # Significance annotation on the one band whose interval excludes zero
    ax1.annotate('* p = .034\n+16.5 pp', xy=(2, 66), ha='center', fontsize=10,
                fontweight='bold', color='#d32f2f')

    ax1.set_ylabel('Pooled Hit Rate (%)')
    ax1.set_ylim(0, 100)
    ax1.set_title('Block B: Hit Rate by Eccentricity Band (H2a, N = 13)')
    ax1.legend(loc='upper left', fontsize=9)
    ax1.grid(axis='y', alpha=0.3, zorder=0)

    # Secondary axis: colour-pop gate strength
    ax2 = ax1.twinx()
    ax2.step(x, gate_strength, where='mid', color='#ff8f00', linewidth=2,
             linestyle='--', label='Gate strength', zorder=2)
    ax2.set_ylabel('Gate Strength (%)', color='#ff8f00')
    ax2.tick_params(axis='y', labelcolor='#ff8f00')
    ax2.set_ylim(0, 120)
    ax2.legend(loc='upper center', fontsize=9)

    # UFOV boundary sits between the 20-30 and >30 bands
    for a in (ax1, ax3):
        a.axvline(2.5, color='#5d4037', linewidth=1.5, linestyle=':', zorder=1)
    ax1.annotate('UFOV boundary ≈30°', xy=(2.55, 88), fontsize=8, color='#5d4037')

    # Lower panel: paired per-participant difference with its 90% interval.
    # Only the 20-30 band's interval excludes zero, which is the H2a claim.
    ax3.axhline(0, color='black', linewidth=0.8, linestyle=':')
    for xi in range(len(bands)):
        reliable = dlo[xi] > 0
        col = '#d32f2f' if reliable else '#757575'
        ax3.vlines(xi, dlo[xi], dhi[xi], color=col, linewidth=2.2, zorder=3)
        ax3.plot([xi - 0.06, xi + 0.06], [dlo[xi]] * 2, color=col, linewidth=2.2)
        ax3.plot([xi - 0.06, xi + 0.06], [dhi[xi]] * 2, color=col, linewidth=2.2)
        ax3.plot(xi, delta[xi], 'o', color=col, markersize=7, zorder=4)
        ax3.text(xi + 0.12, delta[xi], f'{delta[xi]:+.1f}', fontsize=8,
                 va='center', color=col, fontweight='bold' if reliable else 'normal')

    ax3.set_ylabel('Per-participant\ndifference (pp), 90% CI', fontsize=8)
    ax3.set_xlabel('Eccentricity Band')
    ax3.set_xticks(x)
    ax3.set_xticklabels(bands)
    ax3.set_ylim(-20, 34)
    ax3.grid(axis='y', alpha=0.3, zorder=0)

    fig.savefig(os.path.join(OUT, 'fig_eccentricity_h2a.png'))
    plt.close(fig)
    print("  ✓ fig_eccentricity_h2a.png")

# ============================================================
# FIGURE 4: Diverging stacked bar — questionnaire
# ============================================================
def fig_questionnaire():
    constructs = ['Perceived\nFocus Benefit', 'Perceived\nAwareness Cost', 'Visual\nComfort']
    fav = [57, 38, 38]
    total = [65, 65, 52]
    unfav = [t - f for f, t in zip(fav, total)]
    fav_pct = [f/t*100 for f, t in zip(fav, total)]
    unfav_pct = [u/t*100 for u, t in zip(unfav, total)]

    fig, ax = plt.subplots(figsize=(8, 3.5))
    y = np.arange(len(constructs))

    ax.barh(y, fav_pct, color='#26a69a', edgecolor='#00796b', label='Favourable', height=0.5)
    ax.barh(y, [-u for u in unfav_pct], color='#ef5350', edgecolor='#c62828', label='Unfavourable', height=0.5)

    for i in range(len(constructs)):
        ax.text(fav_pct[i]/2, i, f'{fav[i]}/{total[i]}', ha='center', va='center',
                fontsize=10, fontweight='bold', color='white')
        if unfav[i] > 3:
            ax.text(-unfav_pct[i]/2, i, f'{unfav[i]}/{total[i]}', ha='center', va='center',
                    fontsize=10, fontweight='bold', color='white')

    ax.set_yticks(y)
    ax.set_yticklabels(constructs)
    ax.set_xlabel('Response Share (%)')
    ax.set_title('End-of-Session Questionnaire (N = 13)')
    ax.axvline(0, color='black', linewidth=0.8)
    ax.set_xlim(-50, 100)
    ax.legend(loc='lower right', fontsize=9)
    ax.grid(axis='x', alpha=0.3)

    fig.savefig(os.path.join(OUT, 'fig_questionnaire.png'))
    plt.close(fig)
    print("  ✓ fig_questionnaire.png")

# ============================================================
# FIGURE 5: Compositing stack diagram
# ============================================================
def fig_compositing_stack():
    # The camera feed is drawn as its own source feeding the application layer, never as
    # an arrow into the OS passthrough layer: Tier 3 supplies a separate, lower-grade copy
    # of the world, not access to the layer the user actually sees (Chapter 3, Section 3.4).
    fig, ax = plt.subplots(figsize=(11, 5.6))
    ax.set_xlim(0, 14)
    ax.set_ylim(0.2, 7.9)
    ax.axis('off')

    SX, SW = 3.9, 4.6                      # stack left edge and width
    layers = [
        (6.0, '#fff9c4', 'Compositor Output\n(what the user sees)'),
        (3.6, '#c8e6c9', 'Application Render Layer\n(app-controlled)'),
        (1.2, '#bbdefb', 'OS Passthrough Layer\n(never readable by an app)'),
    ]
    for y, color, label in layers:
        ax.add_patch(FancyBboxPatch((SX, y), SW, 1.2, boxstyle="round,pad=0.1",
                                    facecolor=color, edgecolor='#424242', linewidth=1.5))
        ax.text(SX + SW / 2, y + 0.6, label, ha='center', va='center',
                fontsize=10, fontweight='bold')

    ax.text(SX + SW / 2, 5.55, 'composited back to front by the OS',
            ha='center', va='center', fontsize=8, style='italic', color='#616161')

    # Tier 3 source: the PCA camera feed is an input to the application layer.
    ax.add_patch(FancyBboxPatch((0.35, 3.6), 2.6, 1.2, boxstyle="round,pad=0.1",
                                facecolor='#ffe0b2', edgecolor='#e65100', linewidth=1.5))
    ax.text(1.65, 4.2, 'Forward RGB\ncameras (PCA)', ha='center', va='center',
            fontsize=9, fontweight='bold', color='#e65100')
    ax.annotate('', xy=(SX - 0.05, 4.2), xytext=(3.05, 4.2),
                arrowprops=dict(arrowstyle='->', color='#e65100', lw=1.8))

    # Tier callouts. Label and body are one text block so they cannot overlap.
    TX = SX + SW + 0.55
    callouts = [
        (1.8, '#1565c0', 'Tier 1: global colour remap only',
         '(Styling API). No spatial control,\nso no world-locked region.'),
        (4.2, '#2e7d32', 'Tier 2: alpha overlay',
         'Can occlude or dim, cannot read\nor recolour what lies behind it.'),
    ]
    for y, col, head, body in callouts:
        ax.annotate('', xy=(SX + SW + 0.05, y), xytext=(TX - 0.1, y),
                    arrowprops=dict(arrowstyle='->', color=col, lw=1.6))
        ax.text(TX, y + 0.28, head, fontsize=9.5, fontweight='bold',
                va='center', color=col)
        ax.text(TX, y - 0.22, body, fontsize=8.5, va='center', color=col)

    ax.text(0.35, 2.9, 'Tier 3: full pixel control', fontsize=9.5, fontweight='bold',
            color='#e65100')
    ax.text(0.35, 2.15,
            'of a separate, lower-grade copy:\nmono 1280$\\times$960, ~85–90° FOV,\n'
            'against the OS layer\'s ~110°.',
            fontsize=8.5, va='center', color='#e65100')

    ax.set_title('Quest 3 Compositing Stack and Three Tiers of Access',
                 fontsize=13, fontweight='bold')
    fig.savefig(os.path.join(OUT, 'fig_compositing_stack.png'))
    plt.close(fig)
    print("  ✓ fig_compositing_stack.png")

# ============================================================
# FIGURE 6: Two-axis design space plot
# ============================================================
def fig_design_axes():
    fig, ax = plt.subplots(figsize=(7, 5.5))

    modes = {
        'Soft Dark': (0.3, 0.5, 200, '#26a69a'),
        'Hard Dark': (0.2, 0.85, 200, '#ef5350'),
        'SignPop': (0.8, 0.35, 80, '#ffa726'),
    }
    for name, (x, y, s, c) in modes.items():
        ax.scatter(x, y, s=s, color=c, edgecolors='#424242', linewidth=1.5, zorder=5)
        offset = (10, 12) if name != 'Hard Dark' else (10, -18)
        ax.annotate(name, (x, y), textcoords='offset points', xytext=offset,
                   fontsize=11, fontweight='bold', color=c)

    # Trade-off frontier
    xx = np.linspace(0, 1, 100)
    ax.fill_between(xx, 1 - xx * 0.5, 1.0, alpha=0.05, color='gray')
    ax.plot(xx, 1 - xx * 0.5, '--', color='#bdbdbd', linewidth=1, alpha=0.6)

    ax.set_xlabel('Fidelity / Control →', fontsize=12)
    ax.set_ylabel('Suppression / Awareness Cost →', fontsize=12)
    ax.set_xlim(0, 1)
    ax.set_ylim(0, 1)
    ax.set_title('Design Space: Three Study Modes')

    # Legend for marker size
    for sz, label in [(200, 'High (Tier 2)'), (80, 'Low (Tier 3)')]:
        ax.scatter([], [], s=sz, color='gray', edgecolors='#424242', linewidth=1, label=f'Deployability: {label}')
    ax.legend(loc='upper left', fontsize=9, title='Marker size = Deployability')
    ax.grid(alpha=0.2)

    fig.savefig(os.path.join(OUT, 'fig_design_axes.png'))
    plt.close(fig)
    print("  ✓ fig_design_axes.png")

# ============================================================
# FIGURE 7: Dissertation structure diagram
# ============================================================
def fig_structure():
    fig, ax = plt.subplots(figsize=(10, 4.5))
    ax.set_xlim(0, 12)
    ax.set_ylim(0, 5)
    ax.axis('off')

    def box(x, y, w, h, text, color, ec='#424242'):
        rect = FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.12",
                              facecolor=color, edgecolor=ec, linewidth=1.2)
        ax.add_patch(rect)
        ax.text(x + w/2, y + h/2, text, ha='center', va='center', fontsize=9, fontweight='bold')

    def arrow(x1, y1, x2, y2):
        ax.annotate('', xy=(x2, y2), xytext=(x1, y1),
                   arrowprops=dict(arrowstyle='->', lw=1.5, color='#616161'))

    # Part T (top)
    ax.text(0.3, 4.3, 'Part T — Technical', fontsize=11, fontweight='bold', color='#1565c0')
    box(0.5, 3.3, 2.2, 0.9, 'Design Space\n(Ch 3)', '#bbdefb')
    box(3.3, 3.3, 2.2, 0.9, 'System\n(Ch 4)', '#90caf9')
    box(6.1, 3.3, 2.2, 0.9, 'Benchmarks\n(Ch 5)', '#64b5f6')
    arrow(2.7, 3.75, 3.3, 3.75)
    arrow(5.5, 3.75, 6.1, 3.75)

    # Part H (bottom)
    ax.text(0.3, 2.0, 'Part H — Human', fontsize=11, fontweight='bold', color='#2e7d32')
    box(0.5, 1.0, 2.2, 0.9, 'Study Design\n(Ch 6)', '#c8e6c9')
    box(3.3, 1.0, 2.2, 0.9, 'Interim Results\n(Ch 6)', '#a5d6a7')
    box(6.1, 1.0, 2.2, 0.9, 'Discussion\n(Ch 7)', '#81c784')
    arrow(2.7, 1.45, 3.3, 1.45)
    arrow(5.5, 1.45, 6.1, 1.45)

    # Bridge
    box(8.8, 2.2, 2.4, 0.9, 'Workstation Block\n(Real Hardware)', '#fff9c4', ec='#f9a825')
    arrow(8.3, 3.5, 8.8, 2.9)
    arrow(8.3, 1.6, 8.8, 2.4)

    # Conclusion
    box(9.0, 0.5, 2.0, 0.6, 'Conclusions\n(Ch 8)', '#e0e0e0')
    arrow(8.3, 1.3, 9.0, 0.9)

    ax.set_title('Two-Part Structure of the Dissertation', fontsize=13, fontweight='bold', pad=15)
    fig.savefig(os.path.join(OUT, 'fig_dissertation_structure.png'))
    plt.close(fig)
    print("  ✓ fig_dissertation_structure.png")

# ============================================================
# FIGURE 8: Motion suppression state machine
# ============================================================
def fig_motion_suppression():
    fig, ax = plt.subplots(figsize=(9, 5))
    ax.set_xlim(0, 10)
    ax.set_ylim(0, 6)
    ax.axis('off')

    def state_box(x, y, w, h, text, color):
        rect = FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.15",
                              facecolor=color, edgecolor='#424242', linewidth=1.5)
        ax.add_patch(rect)
        ax.text(x + w/2, y + h/2, text, ha='center', va='center', fontsize=10, fontweight='bold')

    # States
    state_box(0.5, 3.5, 2.0, 1.2, 'ACTIVE\n(Effect On)', '#a5d6a7')
    state_box(3.8, 4.5, 2.2, 1.0, 'FADING\nOUT', '#fff9c4')
    state_box(7.2, 3.5, 2.2, 1.2, 'SUPPRESSED\n(Effect Off)', '#ef9a9a')
    state_box(3.8, 1.5, 2.2, 1.0, 'FADING\nIN', '#fff9c4')

    # Transitions
    ax.annotate('', xy=(3.8, 5.0), xytext=(2.5, 4.3),
               arrowprops=dict(arrowstyle='->', lw=1.5, color='#616161'))
    ax.text(2.5, 5.1, 'speed > threshold', fontsize=8, ha='center', color='#616161')

    ax.annotate('', xy=(7.2, 4.4), xytext=(6.0, 5.0),
               arrowprops=dict(arrowstyle='->', lw=1.5, color='#616161'))
    ax.text(6.6, 5.3, 'fade done', fontsize=8, ha='center', color='#616161')

    ax.annotate('', xy=(6.0, 2.0), xytext=(7.2, 3.5),
               arrowprops=dict(arrowstyle='->', lw=1.5, color='#616161'))
    ax.text(7.0, 2.7, 'speed < thresh\nfor hold_time', fontsize=8, ha='center', color='#616161')

    ax.annotate('', xy=(2.5, 2.5), xytext=(3.8, 2.0),
               arrowprops=dict(arrowstyle='->', lw=1.5, color='#616161'))
    ax.text(2.5, 1.5, 'fade done', fontsize=8, ha='center', color='#616161')

    # Focus-arrival shortcut
    ax.annotate('', xy=(2.0, 3.5), xytext=(7.5, 3.5),
               arrowprops=dict(arrowstyle='->', lw=1.5, color='#d32f2f', linestyle='dashed'),
               zorder=1)
    ax.text(4.9, 3.1, 'focus-arrival shortcut', fontsize=8, ha='center',
            color='#d32f2f', fontstyle='italic')

    # Threshold table
    ax.text(5.0, 0.5, 'Thresholds: Blur 30°/s · ColorPop 35°/s · Soft Dark 50°/s · Hard Dark 70°/s',
            fontsize=9, ha='center', color='#424242',
            bbox=dict(boxstyle='round', facecolor='#f5f5f5', edgecolor='#bdbdbd'))

    ax.set_title('Motion-Based Suppression State Machine', fontsize=13, fontweight='bold')
    fig.savefig(os.path.join(OUT, 'fig_motion_suppression.png'))
    plt.close(fig)
    print("  ✓ fig_motion_suppression.png")

# ============================================================
# FIGURE 9: Oracle pipeline flowchart
# ============================================================
def fig_oracle_pipeline():
    fig, ax = plt.subplots(figsize=(11, 3.5))
    ax.set_xlim(0, 11)
    ax.set_ylim(0, 4)
    ax.axis('off')

    steps = [
        (0.3, 2.0, 1.8, 1.0, 'Source Video\n(8m 39s)', '#bbdefb'),
        (2.7, 2.0, 1.8, 1.0, 'Tiled YOLO11\nFull-Res\nInference', '#90caf9'),
        (5.1, 2.0, 1.8, 1.0, 'NMS Merge\n(Cross-Tile\nDedup)', '#64b5f6'),
        (7.5, 2.0, 1.8, 1.0, 'JSON Track\n(13,939\nDetections)', '#42a5f5'),
        (9.0, 2.0, 1.8, 1.0, 'Deterministic\nPlayback →\nShader Islands', '#1e88e5'),
    ]
    for x, y, w, h, text, color in steps:
        rect = FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.12",
                              facecolor=color, edgecolor='#424242', linewidth=1.2)
        ax.add_patch(rect)
        ax.text(x + w/2, y + h/2, text, ha='center', va='center', fontsize=8.5, fontweight='bold')

    for i in range(len(steps) - 1):
        x1 = steps[i][0] + steps[i][2]
        x2 = steps[i+1][0]
        y = steps[i][1] + steps[i][3]/2
        ax.annotate('', xy=(x2, y), xytext=(x1, y),
                   arrowprops=dict(arrowstyle='->', lw=1.5, color='#424242'))

    ax.text(5.5, 1.2, '1,040 frames sampled · 99.5% with ≥1 detection · 2,952 traffic lights · 10,987 people',
            fontsize=9, ha='center', color='#424242',
            bbox=dict(boxstyle='round', facecolor='#fff9c4', edgecolor='#f9a825'))

    ax.set_title('Offline Oracle Pipeline', fontsize=13, fontweight='bold')
    fig.savefig(os.path.join(OUT, 'fig_oracle_pipeline.png'))
    plt.close(fig)
    print("  ✓ fig_oracle_pipeline.png")

# ============================================================
# FIGURE 10: Block A plan view
# ============================================================
def fig_block_a_layout():
    fig, ax = plt.subplots(figsize=(7, 7))
    ax.set_xlim(-4.1, 4.1)
    ax.set_ylim(-1.2, 4.0)
    ax.set_aspect('equal')
    ax.axis('off')

    # Participant
    circle = plt.Circle((0, 0), 0.3, color='#1565c0', zorder=5)
    ax.add_patch(circle)
    ax.text(0, -0.6, 'Participant', ha='center', fontsize=10, fontweight='bold')

    # Forward direction
    ax.annotate('', xy=(0, 0.85), xytext=(0, 0.32),
               arrowprops=dict(arrowstyle='->', lw=2, color='#1565c0'))

    # Scale: 1 unit = 0.5 m, so the task surface at 0.6 m and the tablets at about 1 m
    # (Chapter 6, Section 6.5.2) sit at their real relative distances rather than level.
    import math
    desk = plt.Rectangle((-1.4, 0.95), 2.8, 0.5, facecolor='#e0e0e0',
                         edgecolor='#424242', linewidth=1.5)
    ax.add_patch(desk)
    ax.text(0, 1.02, 'Desk', ha='center', va='center', fontsize=8, color='#424242')
    panel = plt.Rectangle((-0.5, 1.16), 1.0, 0.13, facecolor='#1565c0',
                          edgecolor='#0d47a1', linewidth=1.0, zorder=4)
    ax.add_patch(panel)
    ax.annotate('Task panel\n(0.6 m, world-locked)', xy=(-0.5, 1.22), xytext=(-3.5, 0.75),
                fontsize=8, color='#0d47a1', va='center',
                arrowprops=dict(arrowstyle='->', color='#0d47a1', lw=0.9))

    # Distractor tablets at +/-35 deg, about 1 m out. Labels sit outside the rectangles.
    for angle_deg in (35, -35):
        a = math.radians(90 - angle_deg)
        r = 2.0
        tx, ty = r * math.cos(a), r * math.sin(a)
        ax.add_patch(plt.Rectangle((tx - 0.24, ty - 0.30), 0.48, 0.60,
                                   facecolor='#ef9a9a', edgecolor='#c62828', linewidth=1.2))
        ha = 'left' if tx > 0 else 'right'
        ax.text(tx + (0.38 if tx > 0 else -0.38), ty, 'Distractor\ntablet (1 m)',
                ha=ha, va='center', fontsize=8, fontweight='bold', color='#c62828')

    # Painted focus window. Chapter 6 fixes no angular extent for Block A: the participant
    # paints it and the experimenter verifies the panel is inside and both tablets outside,
    # so the arc is drawn as indicative and carries no degree figure.
    theta = np.linspace(math.radians(90 - 28), math.radians(90 + 28), 60)
    r_arc = 2.75
    ax.plot(r_arc * np.cos(theta), r_arc * np.sin(theta), '-', color='#2e7d32', linewidth=2)
    ax.text(0, 3.05, 'Painted focus window\n(panel inside, both tablets outside)',
            ha='center', fontsize=8.5, color='#2e7d32')

    # Angular annotations
    for angle_deg in [35, -35]:
        angle_rad = math.radians(90 - angle_deg)
        ax.plot([0, 1.75 * math.cos(angle_rad)], [0, 1.75 * math.sin(angle_rad)],
               '--', color='#c62828', linewidth=0.8, alpha=0.6)
    ax.text(0.72, 0.62, '$\\pm$35°', fontsize=9, color='#c62828')

    # FOV arc
    theta_fov = np.linspace(math.radians(90-55), math.radians(90+55), 60)
    r_fov = 3.5
    ax.plot(r_fov * np.cos(theta_fov), r_fov * np.sin(theta_fov), ':', color='#616161', linewidth=1)
    ax.text(-3.05, 2.55, 'Headset\nFOV ~110°', fontsize=8, color='#616161')

    ax.set_title('Plan View: Block A Apparatus and Distractor Placement', fontsize=12, fontweight='bold')
    fig.savefig(os.path.join(OUT, 'fig_block_a_layout.png'))
    plt.close(fig)
    print("  ✓ fig_block_a_layout.png")

# ============================================================
# FIGURE 11: ColorPop grading pipeline
# ============================================================
def fig_colorpop_pipeline():
    # Order and branch membership follow Chapter 4, Section 4.5.2. Two points the earlier
    # draft of this figure had backwards: glare compression acts on bright, UNSATURATED,
    # NON-ROG pixels (it is on the "other" track, not the kept-colour one), and the global
    # periphery dim multiplies BOTH tracks, so it is a merge rather than one branch's step.
    fig, ax = plt.subplots(figsize=(12.5, 4.6))
    ax.set_xlim(0, 13.4)
    ax.set_ylim(-0.1, 5.4)
    ax.axis('off')

    def box(x, y, w, h, text, color, fs=8.2):
        ax.add_patch(FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.12",
                                    facecolor=color, edgecolor='#424242', linewidth=1.2))
        ax.text(x + w / 2, y + h / 2, text, ha='center', va='center',
                fontsize=fs, fontweight='bold')

    def arrow(x0, y0, x1, y1, col='#424242'):
        ax.annotate('', xy=(x1, y1), xytext=(x0, y0),
                    arrowprops=dict(arrowstyle='->', lw=1.2, color=col))

    box(0.15, 2.25, 1.5, 0.9, 'Input\nfragment', '#e0e0e0')

    dx, dy = 2.85, 2.70
    ax.add_patch(plt.Polygon([(dx, dy - 0.66), (dx + 0.85, dy), (dx, dy + 0.66),
                              (dx - 0.85, dy)],
                             facecolor='#fff9c4', edgecolor='#424242', linewidth=1.2))
    ax.text(dx, dy, 'R/O/G?', ha='center', va='center', fontsize=9, fontweight='bold')

    box(4.15, 3.50, 2.75, 1.05,
        'ROG track\ninside: sat $\\times$1.7, sigmoid, bright $\\times$1.15\n'
        'outside: colour kept, $\\times$0.65', '#ffab91', fs=7.6)
    box(4.15, 0.90, 2.75, 1.05,
        'Other track\ninside: slight desat 0.25\noutside: grey $\\times$0.4',
        '#cfd8dc', fs=7.6)
    box(7.45, 0.90, 2.45, 1.05,
        'Glare compress\nbright + unsaturated\n+ blown-core guard', '#ffccbc', fs=7.6)
    box(10.45, 3.50, 2.75, 1.05,
        'Global periphery dim\n$\\times$0.85, ROG included', '#b0bec5', fs=7.8)
    box(10.45, 0.90, 2.75, 1.05, 'Output\nfragment', '#e0e0e0')

    arrow(1.65, 2.70, dx - 0.87, 2.70)
    arrow(dx + 0.55, dy + 0.42, 4.15, 3.90, '#2e7d32')
    ax.text(3.22, 3.28, 'kept colour', fontsize=7.5, color='#2e7d32', fontweight='bold')
    arrow(dx + 0.55, dy - 0.42, 4.15, 1.55, '#c62828')
    ax.text(3.02, 2.10, 'everything else', fontsize=7.5, color='#c62828', fontweight='bold')
    arrow(6.90, 1.42, 7.45, 1.42)
    arrow(6.90, 4.02, 10.45, 4.02)
    arrow(9.90, 1.60, 10.45, 3.70)
    arrow(11.82, 3.50, 11.82, 1.95)

    ax.text(6.7, 0.18,
            'warm hue [0, 0.18], sat floor 0.35 $\\cdot$ green band 0.22–0.48 '
            '$\\cdot$ glare knee luma 0.6',
            fontsize=8, ha='center', color='#616161',
            bbox=dict(boxstyle='round', facecolor='#f5f5f5', edgecolor='#bdbdbd'))

    ax.set_title('ColorPop Grading Pipeline', fontsize=13, fontweight='bold')
    fig.savefig(os.path.join(OUT, 'fig_colorpop_pipeline.png'))
    plt.close(fig)
    print("  ✓ fig_colorpop_pipeline.png")

# ============================================================
# FIGURE 12: Hybrid composite exploded view
# ============================================================
def fig_hybrid_exploded():
    fig, ax = plt.subplots(figsize=(8, 6))
    ax.set_xlim(0, 10)
    ax.set_ylim(0, 8)
    ax.axis('off')

    # Back layer: OS Passthrough
    rect1 = FancyBboxPatch((1, 5.5), 5, 2, boxstyle="round,pad=0.1",
                           facecolor='#bbdefb', edgecolor='#1565c0', linewidth=1.5, alpha=0.8)
    ax.add_patch(rect1)
    ax.text(3.5, 6.5, 'OS Passthrough\n(Full ~110° FOV, Native Quality)', ha='center',
            va='center', fontsize=10, fontweight='bold', color='#0d47a1')

    # Middle layer: Effect sphere with hole
    rect2 = FancyBboxPatch((1.5, 3.0), 5, 2, boxstyle="round,pad=0.1",
                           facecolor='#a5d6a7', edgecolor='#2e7d32', linewidth=1.5, alpha=0.8)
    ax.add_patch(rect2)
    # Hole in the middle
    hole = FancyBboxPatch((3.0, 3.5), 2.0, 1.0, boxstyle="round,pad=0.05",
                          facecolor='white', edgecolor='#d32f2f', linewidth=2, linestyle='--')
    ax.add_patch(hole)
    ax.text(4.0, 4.0, 'Focus Window\n(α = 0)', ha='center', va='center',
            fontsize=9, fontweight='bold', color='#d32f2f')
    ax.text(2.0, 3.3, 'Processed\nPeriphery', ha='center', fontsize=8, color='#1b5e20')
    ax.text(5.5, 4.5, 'Camera Feed\nSphere', ha='center', fontsize=8, color='#1b5e20')

    # Bottom: Fused result
    rect3 = FancyBboxPatch((2, 0.5), 5, 2, boxstyle="round,pad=0.1",
                           facecolor='#fff9c4', edgecolor='#f57f17', linewidth=1.5, alpha=0.8)
    ax.add_patch(rect3)
    # Show fused view: native center + processed edges
    inner = FancyBboxPatch((3.5, 0.9), 2.0, 1.2, boxstyle="round,pad=0.05",
                           facecolor='#bbdefb', edgecolor='#1565c0', linewidth=1)
    ax.add_patch(inner)
    ax.text(4.5, 1.5, 'Native\nPassthrough', ha='center', fontsize=8, color='#0d47a1')
    ax.text(2.8, 1.0, 'Processed', fontsize=7, color='#1b5e20')
    ax.text(6.0, 1.0, 'Processed', fontsize=7, color='#1b5e20')
    ax.text(4.5, 0.3, "User's Fused View", ha='center', fontsize=10, fontweight='bold', color='#e65100')

    # Arrows between layers
    ax.annotate('', xy=(3.5, 5.5), xytext=(3.5, 5.0),
               arrowprops=dict(arrowstyle='->', lw=1.5, color='#616161'))
    ax.annotate('', xy=(4.5, 3.0), xytext=(4.5, 2.5),
               arrowprops=dict(arrowstyle='->', lw=1.5, color='#616161'))

    # Labels on arrows
    ax.text(7.0, 5.2, 'Passthrough shows\nthrough the hole', fontsize=8, color='#616161', fontstyle='italic')
    ax.text(7.0, 2.7, 'Layers composited\nby OS', fontsize=8, color='#616161', fontstyle='italic')

    ax.set_title('Hybrid Composite: Exploded Layer View', fontsize=13, fontweight='bold')
    fig.savefig(os.path.join(OUT, 'fig_hybrid_exploded.png'))
    plt.close(fig)
    print("  ✓ fig_hybrid_exploded.png")


# ============================================================
# RUN ALL
# ============================================================
if __name__ == '__main__':
    print("Generating figures...")
    fig_slopegraph()
    fig_equivalence()
    fig_eccentricity()
    fig_questionnaire()
    fig_compositing_stack()
    fig_design_axes()
    fig_structure()
    fig_motion_suppression()
    fig_oracle_pipeline()
    fig_block_a_layout()
    fig_colorpop_pipeline()
    fig_hybrid_exploded()
    print(f"\nAll figures saved to: {OUT}")
