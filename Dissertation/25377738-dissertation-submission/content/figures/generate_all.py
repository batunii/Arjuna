#!/usr/bin/env python3
"""Generate all matplotlib-based dissertation figures."""
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from matplotlib.patches import FancyBboxPatch, FancyArrowPatch
import numpy as np
import os
import sys

# Windows consoles default to cp1252, which cannot encode the U+2713 tick used in the
# progress lines below; without this the script dies after the first figure.
try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except (AttributeError, ValueError):  # pre-3.7, or a stream that cannot be reconfigured
    pass

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
    # Measured per-participant accuracy, Block A, n = 19.
    # Source: python3 Tools/analysis/blocka_pooled.py Dissertation/authored/raw/blocka
    # These reproduce analysis-2026-08-12-pooled-final.md exactly: NoFilter 92.01 (SD 6.57),
    # Filter 95.08 (SD 4.42), delta +3.07, t(18) = 2.95, p = .0085, dz = 0.678, 14/19 up.
    pids = ['P2', 'P3', 'P4', 'P6', 'P9', 'P10', 'P13',
            'P15', 'P17', 'P20', 'P25', 'P26', 'P27', 'P30', 'P33', 'P41', 'P43',
            'P51', 'P90']
    nf = np.array([97.6, 95.2, 98.4, 84.6, 95.0, 97.8, 100.0,
                   74.8, 91.4, 86.3, 86.3, 89.2, 96.4, 89.2, 97.6, 96.4, 92.8,
                   95.7, 83.5])
    f  = np.array([100.0, 98.8, 100.0, 92.3, 100.0, 100.0, 97.1,
                   90.6, 89.9, 91.4, 96.4, 86.3, 95.7, 93.5, 98.8, 96.4, 94.2,
                   97.8, 87.1])

    fig, ax = plt.subplots(figsize=(5, 6))
    for i in range(len(pids)):
        color = '#2c7bb6' if f[i] > nf[i] else '#757575' if f[i] == nf[i] else '#d7191c'
        ls = '-'
        lw = 0.8
        ax.plot([0, 1], [nf[i], f[i]], color=color, alpha=0.5, linewidth=lw, linestyle=ls)
        ax.scatter([0], [nf[i]], color=color, s=18, zorder=3, alpha=0.7)
        ax.scatter([1], [f[i]], color=color, s=18, zorder=3, alpha=0.7)

    # Group means
    ax.plot([0, 1], [92.01, 95.08], color='black', linewidth=2.5, zorder=4)
    ax.scatter([0, 1], [92.01, 95.08], color='black', s=60, zorder=5)

    ax.set_xlim(-0.3, 1.3)
    ax.set_ylim(72, 101)          # 100% is a hard ceiling on accuracy; never pad above it
    ax.set_xticks([0, 1])
    ax.set_xticklabels(['NoFilter\n(Vignette Off)', 'Hard Dark\n(Vignette On)'])
    ax.set_ylabel('Accuracy (%)')
    ax.set_title('Block A: Per-Participant Accuracy (N = 19)\n+3.07 pp, p = .009, $d_z$ = 0.678')

    blue_patch = mpatches.Patch(color='#2c7bb6', alpha=0.5, label='Improved (14)')
    red_patch = mpatches.Patch(color='#d7191c', alpha=0.5, label='Declined (4)')
    grey_patch = mpatches.Patch(color='#757575', alpha=0.5, label='No change (1)')
    black_line = plt.Line2D([0], [0], color='black', linewidth=2.5, label='Group mean')
    ax.legend(handles=[blue_patch, red_patch, grey_patch, black_line],
              loc='lower right', fontsize=8)
    ax.grid(axis='y', alpha=0.3)

    fig.savefig(os.path.join(OUT, 'fig_slopegraph_h1.png'))
    plt.close(fig)
    print("  ✓ fig_slopegraph_h1.png")

# ============================================================
# FIGURE 16: System architecture and per-frame data flow
# ============================================================
def fig_architecture():
    fig, ax = plt.subplots(figsize=(12, 6.2))
    ax.set_xlim(0, 14.2); ax.set_ylim(0.1, 8.6); ax.axis('off')

    def box(x, y, w, h, text, fc, ec, fs=8.4):
        ax.add_patch(FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.12",
                                    facecolor=fc, edgecolor=ec, linewidth=1.4))
        ax.text(x + w/2, y + h/2, text, ha='center', va='center',
                fontsize=fs, fontweight='bold')

    def arrow(x0, y0, x1, y1, col='#424242'):
        ax.annotate('', xy=(x1, y1), xytext=(x0, y0),
                    arrowprops=dict(arrowstyle='->', lw=1.3, color=col))

    box(0.2, 6.55, 2.9, 1.15, 'OVRCameraRig\nhead pose', '#dce8f4', '#2b5b8a', 8.2)
    box(0.2, 4.30, 2.9, 1.15, 'Forward RGB cameras\n(PCA feed)', '#f3e0c8', '#b65d1f', 8.2)
    box(0.2, 2.05, 2.9, 1.15, 'Baked detection\ntrack (JSON)', '#e4dced', '#6a4e92', 8.2)

    box(4.05, 2.05, 3.1, 5.65,
        'CameraSphereVignette\nManager\n\nowns all per-frame\nstate; the shader\nholds none',
        '#d3e6d3', '#3d7a44', 8.4)

    box(8.15, 2.05, 3.1, 5.65,
        'Overlay sphere\n\ninverted 10 m mesh\nre-centred each frame\n\n'
        'CameraSphereVignette\n.shader\n\nper fragment:\ncolour + alpha',
        '#f5eecd', '#a8871a', 8.2)

    box(12.25, 4.60, 1.85, 2.00, 'OS\ncompositor', '#e8e8e8', '#37474f', 8.4)
    box(12.25, 1.60, 1.85, 1.70, 'Display\n(what the\nuser sees)', '#e8e8e8', '#37474f', 8.2)
    box(8.15, 0.40, 3.1, 0.95, 'OS passthrough layer\nnever readable by the app',
        '#c9dcef', '#2b5b8a', 8.0)

    arrow(3.15, 7.10, 4.00, 6.60)
    arrow(3.15, 4.88, 4.00, 4.88, '#b65d1f')
    arrow(3.15, 2.62, 4.00, 3.10, '#6a4e92')
    arrow(7.20, 4.88, 8.10, 4.88, '#3d7a44')
    ax.text(7.65, 5.42, 'material\nuniforms', ha='center', fontsize=7.4,
            style='italic', color='#3d7a44')
    arrow(11.30, 5.90, 12.20, 5.90, '#a8871a')
    ax.text(11.75, 6.22, 'RGBA', ha='center', fontsize=7.4, style='italic', color='#a8871a')
    # The passthrough layer joins the compositor from below, through the clear corridor
    # between the sphere box and the right-hand column, so no arrow crosses a box.
    ax.plot([11.25, 11.85, 11.85], [0.88, 0.88, 5.05], color='#2b5b8a', linewidth=1.3)
    arrow(11.85, 5.05, 12.20, 5.05, '#2b5b8a')
    ax.text(11.62, 2.95, 'composited behind', ha='center', fontsize=7.4,
            style='italic', color='#2b5b8a', rotation=90)
    arrow(13.18, 4.55, 13.18, 3.35, '#37474f')

    ax.text(9.70, 8.05, 'alpha 0 inside the focus window lets the layer below through untouched',
            ha='center', fontsize=8, style='italic', color='#616161')

    ax.set_title('System Architecture and Per-Frame Data Flow',
                 fontsize=13, fontweight='bold')
    fig.savefig(os.path.join(OUT, 'fig_architecture.png'))
    plt.close(fig)
    print("  ✓ fig_architecture.png")

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
    ax.plot(7.35, 0.5, 'o', color='#1565c0', markersize=10, zorder=5)
    ax.hlines(0.5, 1.50, 13.20, colors='#1565c0', linewidth=2.5, zorder=4)
    ax.plot([1.50, 13.20], [0.5, 0.5], '|', color='#1565c0', markersize=12, zorder=5)

    ax.annotate('+7.35 pp', (7.35, 0.55), ha='center', fontsize=10, fontweight='bold', color='#1565c0')
    ax.annotate('90% CI: [+1.50, +13.20]', (7.35, 0.38), ha='center', fontsize=9, color='#1565c0')

    ax.set_xlim(-16, 18)
    ax.set_ylim(0, 1)
    ax.set_yticks([])
    ax.set_xlabel('Paired Difference in Pooled Hit Rate (pp)')
    ax.set_title('Block B: Pooled Hit Rate vs Safety Margin (H2b, N = 17)')
    ax.legend(loc='upper left', fontsize=9)

    fig.savefig(os.path.join(OUT, 'fig_equivalence_h2b.png'))
    plt.close(fig)
    print("  ✓ fig_equivalence_h2b.png")

# ============================================================
# FIGURE 3: Eccentricity band bar chart — H2a headline result
# ============================================================
def fig_eccentricity():
    # Measured pooled hit rates per band, Block B, n = 17.
    # Source: python3 Tools/analysis/blockb_pooled.py Dissertation/authored/raw
    # Reproduces analysis-2026-08-12-pooled-final.md exactly.
    #   <10    84/112 -> 72/92    10-20  95/233 -> 122/243
    #   20-30  51/131 -> 84/141   >30   111/204 -> 113/204
    bands = ['<10°', '10–20°', '20–30°', '>30°']
    nf   = [75.0, 40.8, 38.9, 54.4]
    filt = [78.3, 50.2, 59.6, 55.4]
    # The lower panel is the PAIRED PER-PARTICIPANT difference, which is the estimator
    # the t/Wilcoxon tests and the CI actually belong to. It is deliberately not the
    # pooled difference of the bars above: participants contribute unequal numbers of
    # markers to a band, so the two estimators can disagree, and for <10 they disagree
    # in sign (pooled +3.26, per-participant -3.68). Mixing them inside one glyph would
    # put an interval around a point it was not computed for, so the panel uses the
    # per-participant mean throughout and the caption states the difference.
    delta = [-3.68, +9.74, +20.82, +0.98]
    dlo   = [-12.00, -0.20, +10.10, -5.70]
    dhi   = [+4.60, +19.70, +31.50, +7.70]

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

    # Significance annotation on the one band significant on both tests.
    # The bars on this axis are POOLED rates; the +20.8 figure is the PER-PARTICIPANT
    # difference the tests belong to, so the estimator is named in the label. Without
    # it a reader subtracting the two bar labels gets the pooled delta instead.
    ax1.annotate('* p = .004\n+20.8 pp (per participant)', xy=(2, 68), ha='center', fontsize=10,
                fontweight='bold', color='#d32f2f')

    ax1.set_ylabel('Pooled Hit Rate (%)')
    ax1.set_ylim(0, 100)
    ax1.set_title('Block B: Hit Rate by Eccentricity Band (H2a, N = 17)')
    ax1.legend(loc='upper left', fontsize=9)
    ax1.grid(axis='y', alpha=0.3, zorder=0)


    # UFOV boundary sits between the 20-30 and >30 bands
    for a in (ax1, ax3):
        a.axvline(2.5, color='#5d4037', linewidth=1.5, linestyle=':', zorder=1)
    ax1.annotate('UFOV boundary ≈30°', xy=(2.55, 88), fontsize=8, color='#5d4037')

    # Lower panel: paired per-participant difference with its 90% interval.
    # Red marks the band significant on both the t and the rank test (20-30 only).
    # The 10-20 band trends the same way at n = 17 but its 90% interval now spans
    # zero ([-0.2, +19.7]) and its tests do not reach significance (p = .108), so
    # ch5 reports it as a trend and the figure must not paint it as a finding.
    significant = [False, False, True, False]
    ax3.axhline(0, color='black', linewidth=0.8, linestyle=':')
    for xi in range(len(bands)):
        reliable = significant[xi]
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
    # n = 20 forms, all sections answered. Sections A and B: 5 items x 20 = 100 each.
    # Item C3 was skipped by every respondent, so Section C is scored over 4 items (20 x 4 = 80).
    # Source: python3 Tools/analysis/esq_extract.py (analysis-2026-08-12-pooled-final.md)
    fav = [89, 58, 55]
    total = [100, 100, 80]
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
    ax.set_title('End-of-Session Questionnaire (N = 20)')
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
    # Conceptual placement, so the axes carry no numeric ticks: false precision on a
    # qualitative chart reads as data that does not exist. Colours are the modes' own
    # semantics: Hard Dark is near-black (it is a blackout), Soft Dark a muted teal,
    # SignPop the camera-tier amber used throughout the diagram set.
    fig, ax = plt.subplots(figsize=(7, 5.2))

    modes = {
        'Soft Dark': (0.30, 0.50, 210, '#2e7d74'),
        'Hard Dark': (0.20, 0.85, 210, '#37373d'),
        'SignPop':   (0.80, 0.35, 80,  '#b65d1f'),
    }
    for name, (x, y, s, c) in modes.items():
        ax.scatter(x, y, s=s, color=c, edgecolors='#212121', linewidth=1.2, zorder=5)
        offset = (14, -4) if name != 'SignPop' else (-14, 12)
        ha = 'left' if name != 'SignPop' else 'right'
        ax.annotate(name, (x, y), textcoords='offset points', xytext=offset,
                    fontsize=11, fontweight='bold', color=c, ha=ha, va='center')

    # The single-variable attenuation ladder: off -> Soft Dark -> Hard Dark
    ax.annotate('', xy=(0.21, 0.82), xytext=(0.29, 0.54),
                arrowprops=dict(arrowstyle='->', lw=1.1, color='#9a9a9a',
                                linestyle=(0, (4, 3))))
    ax.annotate('', xy=(0.295, 0.465), xytext=(0.44, 0.13),
                arrowprops=dict(arrowstyle='->', lw=1.1, color='#9a9a9a',
                                linestyle=(0, (4, 3))))
    ax.text(0.46, 0.115, 'off', fontsize=9, color='#8a8a8a', style='italic',
            ha='left', va='center')
    ax.text(0.40, 0.30, 'attenuation ladder\n(alpha 0 → 0.75 → 1.0)',
            fontsize=8, color='#8a8a8a', style='italic', ha='left')

    ax.set_xlabel('Fidelity  ←→  Control', fontsize=11)
    ax.set_ylabel('Residual awareness  ←→  Suppression', fontsize=11)
    ax.set_xlim(0, 1)
    ax.set_ylim(0, 1)
    ax.set_xticks([])
    ax.set_yticks([])
    for side in ('top', 'right'):
        ax.spines[side].set_visible(False)
    ax.set_title('Design Space: the Three Study Modes', fontsize=13, fontweight='bold')

    # Marker size = deployability, placed where no data lives
    for sz, label in [(210, 'runs camera-free (Tier 2)'), (80, 'camera pipeline (Tier 3)')]:
        ax.scatter([], [], s=sz, color='#bdbdbd', edgecolors='#212121',
                   linewidth=1, label=label)
    ax.legend(loc='lower left', fontsize=8.5, title='Marker size = deployability',
              title_fontsize=8.5, frameon=True, framealpha=0.95)

    fig.savefig(os.path.join(OUT, 'fig_design_axes.png'))
    plt.close(fig)
    print("  ✓ fig_design_axes.png")

# ============================================================
# FIGURE 7: Dissertation structure diagram
# ============================================================
def fig_structure():
    # Chapter map matches the CURRENT six-chapter manuscript (the earlier draft of this
    # figure still showed the deleted benchmark chapter and the old 8-chapter numbering).
    fig, ax = plt.subplots(figsize=(10, 4.6))
    ax.set_xlim(0, 12)
    ax.set_ylim(0, 5.2)
    ax.axis('off')

    INK = '#212121'

    def box(x, y, w, h, text, color, ec='#4a4a4a', fs=9):
        rect = FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.12",
                              facecolor=color, edgecolor=ec, linewidth=1.2)
        ax.add_patch(rect)
        ax.text(x + w/2, y + h/2, text, ha='center', va='center',
                fontsize=fs, fontweight='bold', color=INK)

    def arrow(x1, y1, x2, y2):
        ax.annotate('', xy=(x2, y2), xytext=(x1, y1),
                   arrowprops=dict(arrowstyle='->', lw=1.4, color='#5a5a5a'))

    # Shared root
    box(0.4, 2.15, 2.0, 0.9, 'Problem &\nBackground\n(Ch 1–2)', '#ececec', fs=8.6)

    # Part T (top row)
    ax.text(2.9, 4.75, 'Part T — can it be built?', fontsize=10.5,
            fontweight='bold', color='#2b5b8a')
    box(2.9, 3.45, 2.5, 0.95, 'Design space:\nthree tiers (Ch 3)', '#c9dcef', ec='#2b5b8a', fs=8.8)
    box(5.9, 3.45, 2.5, 0.95, 'System +\nmeasured validation\n(Ch 4)', '#adc9e6', ec='#2b5b8a', fs=8.8)
    arrow(5.4, 3.92, 5.9, 3.92)

    # Part H (bottom row)
    ax.text(2.9, 0.35, 'Part H — does it help?', fontsize=10.5,
            fontweight='bold', color='#3d7a44')
    box(2.9, 0.85, 2.5, 0.95, 'Pre-registered\nstudy design (Ch 5)', '#d3e6d3', ec='#3d7a44', fs=8.8)
    box(5.9, 0.85, 2.5, 0.95, 'Results,\nn = 19 / 17 (Ch 5)', '#b9d8b9', ec='#3d7a44', fs=8.8)
    arrow(5.4, 1.32, 5.9, 1.32)

    # Root feeds both parts
    arrow(2.4, 2.85, 2.9, 3.6)
    arrow(2.4, 2.35, 2.9, 1.6)

    # Bridge: the workstation block runs the real artefact on real hardware
    box(9.0, 2.15, 2.6, 0.95, 'Workstation block:\nthe real artefact,\nreal distractors',
        '#f3e0c8', ec='#b65d1f', fs=8.6)
    arrow(8.4, 3.6, 9.15, 3.05)
    arrow(8.4, 1.6, 9.15, 2.2)

    # Conclusion
    box(9.3, 0.5, 2.0, 0.8, 'Discussion &\nConclusion (Ch 6)', '#ececec', fs=8.6)
    arrow(9.0, 2.3, 9.55, 1.3)

    ax.set_title('Two-Part Structure of the Dissertation', fontsize=13, fontweight='bold', pad=12)
    fig.savefig(os.path.join(OUT, 'fig_dissertation_structure.png'))
    plt.close(fig)
    print("  ✓ fig_dissertation_structure.png")

# ============================================================
# FIGURE 8: Motion suppression state machine
# ============================================================
def fig_motion_suppression():
    fig, ax = plt.subplots(figsize=(9, 5.2))
    ax.set_xlim(0, 10)
    ax.set_ylim(-0.45, 6)
    ax.axis('off')

    def state_box(x, y, w, h, text, fc, ec):
        rect = FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.15",
                              facecolor=fc, edgecolor=ec, linewidth=1.5)
        ax.add_patch(rect)
        ax.text(x + w/2, y + h/2, text, ha='center', va='center', fontsize=10,
                fontweight='bold', color='#212121')

    def trans(x0, y0, x1, y1, rad, label, lx, ly):
        ax.annotate('', xy=(x1, y1), xytext=(x0, y0),
                    arrowprops=dict(arrowstyle='->', lw=1.4, color='#5a5a5a',
                                    connectionstyle=f'arc3,rad={rad}'))
        ax.text(lx, ly, label, fontsize=8.5, ha='center', color='#3a3a3a')

    # A cycle: ACTIVE (left) -> FADING OUT (top) -> SUPPRESSED (right) -> FADING IN
    # (bottom) -> ACTIVE, with the focus-arrival shortcut cutting straight across.
    state_box(0.5, 2.4, 2.1, 1.2, 'ACTIVE\n(effect on)', '#d3e6d3', '#3d7a44')
    state_box(3.9, 4.4, 2.2, 1.0, 'FADING OUT\n(0.20 s)', '#f5eecd', '#a8871a')
    state_box(7.4, 2.4, 2.2, 1.2, 'SUPPRESSED\n(effect off)', '#e8e8e8', '#4a4a4a')
    state_box(3.9, 0.6, 2.2, 1.0, 'FADING IN\n(0.70 s)', '#f5eecd', '#a8871a')

    trans(2.35, 3.65, 3.95, 4.55, 0.25, 'head speed > threshold', 2.55, 4.95)
    trans(6.15, 4.85, 8.30, 3.70, 0.25, 'fade done', 7.75, 4.75)
    trans(8.30, 2.35, 6.15, 1.05, 0.25, 'speed < threshold\nheld for hold time', 8.05, 1.35)
    trans(3.95, 1.05, 1.75, 2.35, 0.25, 'fade done', 2.15, 1.25)

    # Focus-arrival shortcut: hold timer zeroed the moment the head re-enters the window
    ax.annotate('', xy=(2.65, 3.0), xytext=(7.35, 3.0),
               arrowprops=dict(arrowstyle='->', lw=1.4, color='#b33a3a',
                               linestyle=(0, (5, 3))), zorder=1)
    ax.text(5.0, 3.18, 'focus-arrival shortcut: hold timer zeroed', fontsize=8.5,
            ha='center', color='#b33a3a', fontstyle='italic')

    # Threshold table
    ax.text(5.0, 0.0, 'Yield thresholds scale with intrusiveness: Blur 30°/s · ColorPop 35°/s · Soft Dark 50°/s · Hard Dark 70°/s',
            fontsize=8.5, ha='center', color='#3a3a3a',
            bbox=dict(boxstyle='round', facecolor='#f4f4f4', edgecolor='#bdbdbd'))

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

    # Even 2.15-unit pitch so no box overlaps its neighbour; ink stays dark on every
    # fill, so the fills lighten toward the runtime end instead of darkening.
    steps = [
        (0.30, 2.0, 1.75, 1.0, 'Source video\n(8 m 39 s, 4K)', '#e4dced'),
        (2.45, 2.0, 1.75, 1.0, 'Tiled YOLO11,\nfull resolution', '#d5c8e4'),
        (4.60, 2.0, 1.75, 1.0, 'Per-class NMS\nmerge', '#c6b4da'),
        (6.75, 2.0, 1.75, 1.0, 'JSON track\n(13,939\ndetections)', '#b7a0d1'),
        (8.90, 2.0, 1.85, 1.0, 'Deterministic\nplayback into\nshader islands', '#a88cc7'),
    ]
    for x, y, w, h, text, color in steps:
        rect = FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.12",
                              facecolor=color, edgecolor='#5a4a75', linewidth=1.2)
        ax.add_patch(rect)
        ax.text(x + w/2, y + h/2, text, ha='center', va='center', fontsize=8.5,
                fontweight='bold', color='#241a33')

    for i in range(len(steps) - 1):
        x1 = steps[i][0] + steps[i][2]
        x2 = steps[i+1][0]
        y = steps[i][1] + steps[i][3]/2
        ax.annotate('', xy=(x2, y), xytext=(x1, y),
                   arrowprops=dict(arrowstyle='->', lw=1.5, color='#424242'))

    ax.text(5.5, 1.15, '1,040 frames sampled · 99.5% with ≥1 detection · 2,952 traffic lights · 10,987 people',
            fontsize=9, ha='center', color='#3a3a3a',
            bbox=dict(boxstyle='round', facecolor='#f4f4f4', edgecolor='#bdbdbd'))

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
    circle = plt.Circle((0, 0), 0.3, color='#2b5b8a', zorder=5)
    ax.add_patch(circle)
    ax.text(0, -0.6, 'Participant', ha='center', fontsize=10, fontweight='bold')

    # Forward direction
    ax.annotate('', xy=(0, 0.85), xytext=(0, 0.32),
               arrowprops=dict(arrowstyle='->', lw=2, color='#2b5b8a'))

    # Scale: 1 unit = 0.5 m. One physical task display at ~0.6 m carries the whole task
    # page: the 1-back stream in the centre and the two embedded distractor video panels
    # at the display's edges, so their eccentricity is fixed by the page layout, not by
    # furniture. Edges at +/-35 deg of a 0.6 m viewing distance = +/-0.42 m = +/-0.84 units.
    import math
    desk = plt.Rectangle((-1.4, 0.95), 2.8, 0.55, facecolor='#ececec',
                         edgecolor='#4a4a4a', linewidth=1.5)
    ax.add_patch(desk)
    ax.text(0, 1.42, 'Desk', ha='center', va='center', fontsize=8, color='#4a4a4a')
    # The display bezel, then its three page regions
    ax.add_patch(plt.Rectangle((-1.02, 1.12), 2.04, 0.24, facecolor='#37373d',
                               edgecolor='#212121', linewidth=1.0, zorder=3))
    ax.add_patch(plt.Rectangle((-0.46, 1.15), 0.92, 0.18, facecolor='#c9dcef',
                               edgecolor='#2b5b8a', linewidth=1.0, zorder=4))
    for x0 in (-0.99, 0.55):
        ax.add_patch(plt.Rectangle((x0, 1.15), 0.44, 0.18, facecolor='#e8b4ab',
                                   edgecolor='#b33a3a', linewidth=1.0, zorder=4))
    ax.annotate('1-back shape stream\n(centre of the task page)', xy=(-0.3, 1.24),
                xytext=(-3.55, 0.72), fontsize=8, color='#2b5b8a', va='center',
                arrowprops=dict(arrowstyle='->', color='#2b5b8a', lw=0.9))
    ax.annotate('Task display (~0.6 m)', xy=(1.02, 1.24), xytext=(2.35, 0.72),
                fontsize=8, color='#37373d', va='center',
                arrowprops=dict(arrowstyle='->', color='#37373d', lw=0.9))

    # Embedded distractor video panels at the display edges, centred near +/-35 deg
    for sx, ha in ((-1, 'right'), (1, 'left')):
        ax.text(sx * 1.25, 1.85, 'Distractor video panel\n(embedded in the task page)',
                ha=ha, va='center', fontsize=8, fontweight='bold', color='#b33a3a')
        ax.annotate('', xy=(sx * 0.79, 1.35), xytext=(sx * 1.32, 1.72),
                    arrowprops=dict(arrowstyle='->', color='#b33a3a', lw=0.9))

    # Painted focus window. Chapter 5 fixes no angular extent for Block A: the participant
    # paints it and the experimenter verifies the task region is inside and both distractor
    # panels outside, so the arc is drawn as indicative and carries no degree figure.
    theta = np.linspace(math.radians(90 - 28), math.radians(90 + 28), 60)
    r_arc = 2.75
    ax.plot(r_arc * np.cos(theta), r_arc * np.sin(theta), '-', color='#3d7a44', linewidth=2)
    ax.text(0, 3.05, 'Painted focus window\n(task region inside, both distractor panels outside)',
            ha='center', fontsize=8.5, color='#3d7a44')

    # Angular annotations: rays through the distractor panel centres
    for angle_deg in [35, -35]:
        angle_rad = math.radians(90 - angle_deg)
        ax.plot([0, 1.55 * math.cos(angle_rad)], [0, 1.55 * math.sin(angle_rad)],
               '--', color='#b33a3a', linewidth=0.8, alpha=0.7)
    ax.text(0.62, 0.55, '$\\pm$35°', fontsize=9, color='#b33a3a')

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
    fig, ax = plt.subplots(figsize=(13.2, 5.2))
    ax.set_xlim(0, 14.5)
    ax.set_ylim(-0.15, 6.1)
    ax.axis('off')

    def box(x, y, w, h, text, fc, ec, fs=8.4):
        ax.add_patch(FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.12",
                                    facecolor=fc, edgecolor=ec, linewidth=1.2))
        ax.text(x + w / 2, y + h / 2, text, ha='center', va='center',
                fontsize=fs, fontweight='bold', color='#212121',
                linespacing=1.35)

    def arrow(x0, y0, x1, y1, col='#4a4a4a'):
        ax.annotate('', xy=(x1, y1), xytext=(x0, y0),
                    arrowprops=dict(arrowstyle='->', lw=1.3, color=col))

    box(0.15, 2.55, 1.55, 1.0, 'Input\nfragment', '#e8e8e8', '#4a4a4a')

    dx, dy = 3.0, 3.05
    ax.add_patch(plt.Polygon([(dx, dy - 0.72), (dx + 0.92, dy), (dx, dy + 0.72),
                              (dx - 0.92, dy)],
                             facecolor='#f5eecd', edgecolor='#4a4a4a', linewidth=1.2))
    ax.text(dx, dy, 'R/O/G?', ha='center', va='center', fontsize=9.5, fontweight='bold')

    box(4.45, 3.95, 3.5, 1.5,
        'ROG track\ninside: sat $\\times$1.7, sigmoid,\nbright $\\times$1.15 '
        '$\\cdot$ outside:\ncolour kept, $\\times$0.65', '#f3d4c0', '#b65d1f', fs=8.0)
    box(4.45, 0.85, 3.5, 1.5,
        'Other track\ninside: slight desat 0.25\noutside: grey $\\times$0.4',
        '#dde3e6', '#54666e', fs=8.0)
    box(8.55, 0.85, 2.7, 1.5,
        'Glare compress\nbright + unsaturated,\nblown-core guard', '#f3e0c8', '#b65d1f', fs=8.0)
    box(11.55, 3.95, 2.8, 1.5,
        'Global periphery\ndim $\\times$0.85,\nROG included', '#c5ced3', '#37474f', fs=8.0)
    box(11.55, 1.05, 2.8, 1.1, 'Output\nfragment', '#e8e8e8', '#4a4a4a')

    arrow(1.70, 3.05, dx - 0.94, 3.05)
    arrow(dx + 0.62, dy + 0.46, 4.45, 4.45, '#3d7a44')
    ax.text(3.35, 3.98, 'kept colour', fontsize=8, color='#3d7a44', fontweight='bold')
    arrow(dx + 0.62, dy - 0.46, 4.45, 1.85, '#b33a3a')
    ax.text(3.15, 2.20, 'everything else', fontsize=8, color='#b33a3a', fontweight='bold')
    arrow(7.95, 1.60, 8.55, 1.60)
    arrow(7.95, 4.70, 11.55, 4.70)
    arrow(11.25, 1.85, 11.85, 3.90)
    arrow(12.95, 3.95, 12.95, 2.20)

    ax.text(7.25, 0.12,
            'warm hue [0, 0.18], sat floor 0.35 $\\cdot$ green band 0.22–0.48 '
            '$\\cdot$ glare knee luma 0.6',
            fontsize=8, ha='center', color='#3a3a3a',
            bbox=dict(boxstyle='round', facecolor='#f4f4f4', edgecolor='#bdbdbd'))

    ax.set_title('ColorPop Grading Pipeline', fontsize=13, fontweight='bold')
    fig.savefig(os.path.join(OUT, 'fig_colorpop_pipeline.png'))
    plt.close(fig)
    print("  ✓ fig_colorpop_pipeline.png")

# ============================================================
# FIGURE 12: Hybrid composite exploded view
# ============================================================
def fig_hybrid_exploded():
    # Reads top to bottom in compositing order: the OS-owned passthrough layer sits at the
    # BACK, the application sphere in FRONT of it with the window as a hole, and the
    # flattened result at the bottom. The three layers share one x-centre and the window
    # keeps one x-position throughout, with dotted projection lines tying the hole to the
    # native region of the fused view, so the alignment carries the mechanism.
    fig, ax = plt.subplots(figsize=(8.6, 6.4))
    ax.set_xlim(0, 10.6)
    ax.set_ylim(0, 9.2)
    ax.axis('off')

    BLUE_F, BLUE_E, BLUE_I = '#c9dcef', '#2b5b8a', '#1f4568'
    AMBER_F, AMBER_E, AMBER_I = '#f3e0c8', '#b65d1f', '#7d3f14'
    GREY = '#5a5a5a'
    LX, LW = 1.6, 6.0                 # shared layer left edge and width
    HX, HW = 3.65, 2.4                # window hole left edge and width (constant column)

    # Back layer: OS passthrough (full field, native quality)
    ax.add_patch(FancyBboxPatch((LX, 6.9), LW, 1.6, boxstyle="round,pad=0.1",
                                facecolor=BLUE_F, edgecolor=BLUE_E, linewidth=1.5))
    ax.text(LX + LW/2, 7.7, 'OS passthrough layer  (back)\nfull ~110° field, native quality, never readable',
            ha='center', va='center', fontsize=9.5, fontweight='bold', color=BLUE_I)

    # Front layer: the application's camera-feed sphere with the window as absence
    ax.add_patch(FancyBboxPatch((LX, 4.0), LW, 1.9, boxstyle="round,pad=0.1",
                                facecolor=AMBER_F, edgecolor=AMBER_E, linewidth=1.5))
    ax.text(LX + 1.1, 4.95, 'Tier-3\nprocessed\ncamera feed', ha='center', va='center',
            fontsize=8.5, fontweight='bold', color=AMBER_I)
    ax.text(LX + LW - 1.1, 4.95, 'application\nsphere\n(front)', ha='center', va='center',
            fontsize=8.5, color=AMBER_I)
    hole = FancyBboxPatch((HX, 4.35), HW, 1.2, boxstyle="round,pad=0.05",
                          facecolor='white', edgecolor='#b33a3a', linewidth=1.8,
                          linestyle=(0, (5, 3)))
    ax.add_patch(hole)
    ax.text(HX + HW/2, 4.95, 'focus window\nα = 0, nothing\ndrawn', ha='center', va='center',
            fontsize=8.5, fontweight='bold', color='#b33a3a')

    # Result: the user's fused view
    ax.add_patch(FancyBboxPatch((LX, 0.7), LW, 1.9, boxstyle="round,pad=0.1",
                                facecolor=AMBER_F, edgecolor=AMBER_E, linewidth=1.5))
    ax.add_patch(FancyBboxPatch((HX, 1.05), HW, 1.2, boxstyle="round,pad=0.05",
                                facecolor=BLUE_F, edgecolor=BLUE_E, linewidth=1.5))
    ax.text(HX + HW/2, 1.65, 'native\npassthrough', ha='center', va='center',
            fontsize=8.5, fontweight='bold', color=BLUE_I)
    ax.text(LX + 1.1, 1.65, 'processed\nperiphery', ha='center', va='center',
            fontsize=8.5, color=AMBER_I)
    ax.text(LX + LW - 1.1, 1.65, 'processed\nperiphery', ha='center', va='center',
            fontsize=8.5, color=AMBER_I)
    ax.text(LX + LW/2, 0.35, "the user's fused view", ha='center', fontsize=10,
            fontweight='bold', color='#212121')

    # Projection lines: the hole column carries straight down into the native region
    for x in (HX, HX + HW):
        ax.plot([x, x], [4.35, 2.25], linestyle=':', color='#b33a3a', linewidth=1.1, zorder=1)

    # Compositing arrows down the left margin
    ax.annotate('', xy=(1.15, 4.95), xytext=(1.15, 7.7),
                arrowprops=dict(arrowstyle='->', lw=1.4, color=GREY))
    ax.annotate('', xy=(1.15, 1.65), xytext=(1.15, 4.0),
                arrowprops=dict(arrowstyle='->', lw=1.4, color=GREY))
    ax.text(0.55, 4.65, 'composited back to front by the OS', fontsize=8, color=GREY,
            style='italic', ha='center', rotation=90, va='center')

    # Right-margin annotations
    ax.text(8.05, 4.95, 'the window is an absence:\nno pixels, no cost,\nno camera copy',
            fontsize=8.5, color='#b33a3a', style='italic', va='center')
    ax.text(8.05, 1.65, 'native quality exactly where\nattention is being held;\ncamera fidelity only in\nthe periphery',
            fontsize=8.5, color=GREY, style='italic', va='center')

    ax.set_title('The Hybrid Composite, as Layers', fontsize=13, fontweight='bold')
    fig.savefig(os.path.join(OUT, 'fig_hybrid_exploded.png'))
    plt.close(fig)
    print("  ✓ fig_hybrid_exploded.png")


# ============================================================
# FIGURE 13: Eccentricity landmarks and the four analysis bands
# ============================================================
def fig_band_landmarks():
    # Every landmark is a cited value: road-centre gaze ~8 deg radius (victor2005),
    # PDT probe placement 11-23 deg (jahn2005), UFOV ~30 deg conventional extent
    # (ball1993), and the system's own half-strength point at 20 deg (24 deg soft edge
    # on an 8 deg window half-width). The bands are round
    # numbers chosen to bracket the landmarks, and the figure shows exactly that.
    fig, ax = plt.subplots(figsize=(9, 3.0))
    ax.set_xlim(0, 40)
    ax.set_ylim(0, 4.6)

    bands = [(0, 10, '#f2f2f2', '<10°'), (10, 20, '#e4e9ee', '10–20°'),
             (20, 30, '#d3dfd3', '20–30°'), (30, 40, '#e4e9ee', '>30°')]
    for x0, x1, c, lab in bands:
        ax.axvspan(x0, x1, ymin=0, ymax=0.30, color=c, zorder=1)
        weight = 'bold' if lab == '20–30°' else 'normal'
        ax.text((x0 + x1) / 2, 0.62, lab, ha='center', fontsize=10,
                fontweight=weight, color='#212121')
    for x in (10, 20, 30):
        ax.plot([x, x], [0, 1.38], color='#9a9a9a', linewidth=0.9, zorder=2)
    ax.text(1.2, 1.02, 'analysis bands', fontsize=8, color='#6a6a6a', style='italic')

    # Landmarks, stacked above the band ruler
    ax.plot([0, 8], [2.0, 2.0], color='#2b5b8a', linewidth=3, solid_capstyle='butt')
    ax.text(8.6, 2.0, 'road-centre gaze region, ~8° radius (Victor et al., 2005)',
            fontsize=8.5, va='center', color='#2b5b8a')

    ax.plot([11, 23], [2.8, 2.8], color='#6a4e92', linewidth=3, solid_capstyle='butt')
    ax.text(10.4, 2.8, 'PDT probes, 11–23°\n(Jahn et al., 2005)',
            fontsize=8.5, va='center', ha='right', color='#6a4e92')

    ax.annotate('', xy=(20, 3.6), xytext=(20, 4.35),
                arrowprops=dict(arrowstyle='->', lw=1.4, color='#3d7a44'))
    ax.text(19.4, 4.0, "gate at half strength, 20° (this system)",
            fontsize=8.5, va='center', ha='right', color='#3d7a44')

    ax.plot([30, 30], [1.6, 4.4], color='#b65d1f', linewidth=1.4,
            linestyle=(0, (5, 3)))
    ax.text(30.6, 3.35, 'useful field of view,\n~30° conventional extent\n(Ball et al., 1993)',
            fontsize=8.5, va='center', color='#b65d1f')

    ax.set_xlabel('Eccentricity from the focus direction (degrees)', fontsize=10)
    ax.set_yticks([])
    for side in ('top', 'right', 'left'):
        ax.spines[side].set_visible(False)
    ax.set_title('Published Gaze Landmarks and the Block B Analysis Bands',
                 fontsize=12, fontweight='bold')
    fig.savefig(os.path.join(OUT, 'fig_band_landmarks.png'))
    plt.close(fig)
    print("  ✓ fig_band_landmarks.png")

# ============================================================
# FIGURE 14: The noise-cancellation analogy, as architecture
# ============================================================
def fig_anc_analogy():
    fig, ax = plt.subplots(figsize=(9.5, 3.0))
    ax.set_xlim(0, 12.4)
    ax.set_ylim(0, 4.4)
    ax.axis('off')

    def box(x, y, w, h, text, fc, ec, fs=9):
        ax.add_patch(FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.1",
                                    facecolor=fc, edgecolor=ec, linewidth=1.3))
        ax.text(x + w/2, y + h/2, text, ha='center', va='center',
                fontsize=fs, fontweight='bold', color='#212121')

    def arrow(x0, y, x1):
        ax.annotate('', xy=(x1, y), xytext=(x0, y),
                    arrowprops=dict(arrowstyle='->', lw=1.4, color='#5a5a5a'))

    rows = [
        (2.85, 'Noise-cancelling headphone', '#e8e8e8', '#4a4a4a',
         ['microphone', 'processing', 'speaker'],
         'attenuate noise, pass the signal'),
        (0.55, 'Video-see-through headset', '#f3e0c8', '#b65d1f',
         ['camera', 'shader', 'display'],
         'attenuate distraction, pass the task'),
    ]
    for y, rowlab, fc, ec, stages, gloss in rows:
        ax.text(0.1, y + 1.25, rowlab, fontsize=10, fontweight='bold', color='#212121')
        for i, s in enumerate(stages):
            x = 0.1 + i * 2.75
            box(x, y, 2.15, 0.95, s, fc, ec)
            if i < 2:
                arrow(x + 2.18, y + 0.48, x + 2.72)
        ax.text(8.5, y + 0.48, gloss, fontsize=9.5, style='italic',
                va='center', color='#3a3a3a')

    ax.set_title('The Same Three Components, in the Same Order',
                 fontsize=12, fontweight='bold')
    fig.savefig(os.path.join(OUT, 'fig_anc_analogy.png'))
    plt.close(fig)
    print("  ✓ fig_anc_analogy.png")

# ============================================================
# RUN ALL
# ============================================================
if __name__ == '__main__':
    print("Generating figures...")
    fig_slopegraph()
    fig_architecture()
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
    fig_band_landmarks()
    fig_anc_analogy()
    print(f"\nAll figures saved to: {OUT}")
