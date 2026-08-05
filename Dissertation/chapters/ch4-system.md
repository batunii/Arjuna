# Chapter 4 — System Design and Implementation

## 4.1 Overview

This chapter presents the reference implementation (Contribution C2): a multi-mode diminished-reality
attention-guidance system running on a consumer Meta Quest 3, built in Unity 6 on Meta's
Passthrough Camera Access sample framework. The system realises the design space of Chapter 3 in a
deliberately economical architecture — **one shader and one manager component on a single piece of
geometry** — and this chapter argues that the economy is itself a result: every capability tier of
Chapter 3's taxonomy is reachable from one head-centred sphere and one fragment program.

The exposition follows the architecture outward. Section 4.2 describes the sphere-and-window
geometry that everything else inhabits. Section 4.3 presents the hybrid composite of system
passthrough and processed camera feed — the flagship technique. Section 4.4 explains the
world-direction sampling scheme that makes a monocular camera feed binocularly fusible, and the
failed design it replaced. Section 4.5 specifies the three evaluated modes at implementation depth,
together with the superseded and exploratory modes retained in the codebase. Sections 4.6 and 4.7
cover the motion-based comfort suppression and the interaction design. Section 4.8 describes the
perception subsystem: live detection support and the offline oracle pipeline that stands in for it
in evaluation. Section 4.9 describes the video test scene used as the evaluation harness. Section
4.10 is the failure museum: the documented dead ends whose lessons shaped the final design.

Throughout, parameter names in `code font` are the actual serialized fields and shader properties in
the repository (`CameraSphereVignette.shader`, `CameraSphereVignetteManager.cs`,
`VideoTestSceneManager.cs`), so that every quantitative claim in this chapter is checkable against
the artifact.

[Figure 4.1 — System architecture: OVRCameraRig, the CameraSphereSphere GameObject with manager and
material, PCA camera feeds, OS passthrough behind, and the compositor output. One box per component,
arrows for per-frame data flow.]

## 4.2 The Sphere and the Window

### 4.2.1 Geometry

The entire visual effect is carried by a single inverted sphere of approximately 10 m radius,
centred on the user's head and re-centred every frame (translation only — the sphere never rotates,
which is what keeps its surface coordinates world-stable). The mesh is rendered inside-out with
`Cull Front`, on the transparent queue (`Queue = Transparent`), with depth writes disabled
(`ZWrite Off`) and conventional alpha blending (`Blend SrcAlpha OneMinusSrcAlpha`). Every fragment
the user can see, in any direction, is a fragment of this sphere; the shader's output alpha at that
fragment decides, per pixel, whether the user sees the diminishment effect or whatever lies behind
the sphere — which, in the passthrough scene, is the operating system's native passthrough layer.

This choice of a *fragment-alpha-controlled full-surround surface* is what makes one object span all
of Chapter 3's tiers: alpha 0 is Tier 2's "absence" (native passthrough passes through untouched);
alpha 1 with a black colour is Tier 2's blackout; alpha 1 with a camera-fed colour is Tier 3's
re-render. The mode logic reduces to deciding, per fragment, a colour and an alpha.

### 4.2.2 The world-locked focus window

The focus window — the region the user is asked to attend, and the region the diminishment spares —
is stored not in screen space or head space but in **world spherical coordinates**: a rectangle
`_FocusRect = (azMin, azMax, elMin, elMax)` in radians of azimuth and elevation about the sphere's
centre. Each fragment reconstructs its own direction from first principles:

```
dir = normalize(worldPos − _SphereCenter)
az  = atan2(dir.x, dir.z)
el  = asin(dir.y)
```

and computes a signed distance outside the rectangle, `max(dAz, dEl)`, which a
`smoothstep(0, _SoftEdge, ·)` converts into the per-fragment vignette coordinate **t**: 0 inside
the window, rising smoothly through the soft edge (default `m_softEdgeDeg` = 20°, widened to 32°
for ColorPop, Section 4.5.3), and 1 in the far periphery. Every mode consumes this single scalar:
the dark modes use it directly as overlay alpha; ColorPop uses it to interpolate each pixel between
its "inside" and "outside" treatments.

Because azimuth and elevation are computed from world direction, the window is **world-locked**: it
stays fixed to the room — to the desk, the doorway, the whiteboard — as the head rotates, rather
than travelling with the gaze. Direction is computed per-fragment from `worldPos` rather than
interpolated from vertices, which avoids interpolation error at the sphere's poles.

[Figure 4.2 — The az/el window geometry: sphere cross-section with the focus rectangle drawn on it,
the soft-edge band, and the t-profile plotted along one axis.]

### 4.2.3 The window as absence

In the passthrough scene the shader's output inside the window is `alpha = t = 0`: nothing is drawn.
The consequence, argued in Chapter 3 and repeated here because it is the system's central design
decision: inside the window the user sees the operating system's own passthrough at full native
quality, full ~110° field of view, and the OS pipeline's own latency compensation — none of which
any application-rendered reconstruction can match. The effect budget is spent entirely in the
periphery, where human acuity is lowest. One line of shader arithmetic (`alpha = t`) simultaneously
solves the field-of-view problem (the accessible camera feed covers only ~85–90°) and the fidelity
problem (1280×960 mono versus the native stereo view), by ensuring the reconstruction is only ever
shown where neither limitation matters much.

### 4.2.4 Per-frame data flow

The manager component (`CameraSphereVignetteManager`) owns all per-frame state and feeds the shader
exclusively through material uniforms; the shader holds no state of its own. Each frame the manager:
(i) re-centres the sphere on the head anchor and uploads the centre as `_SphereCenter` — the one
value on which the whole angular coordinate system depends; (ii) uploads the current focus rectangle
(`_FocusRect`) and soft edge; (iii) for camera-fed modes, uploads each active camera's pose basis
(`_CamLFwd/_CamLRt/_CamLUp`, and the right-camera set when present with `_HasRightCam = 1`) and its
half-FOV tangents (`_TanHalfFovL/R`), derived from the camera intrinsics reported by the PCA API
(`SensorResolution / (2·FocalLength)`) rather than an assumed field of view — an intrinsics-derived
projection was what removed the early implementation's slight "zoom" mismatch between the camera
periphery and the native window; (iv) combines formation and motion-suppression state into the single
`_VignetteStrength` scalar; and (v) pushes the active mode's parameter block (Section 4.5).

Stereo correctness is handled at the vertex/fragment interface: the shader is written for Unity's
single-pass instanced stereo (`UNITY_VERTEX_OUTPUT_STEREO` and per-fragment eye setup), and — by the
world-direction design of Section 4.4 — contains *no* eye-dependent sampling anywhere in the camera
path. The renderer, a standard `MeshRenderer` on the sphere, requires no per-eye scripting.

Two serialization details are deliberate. The renderer field keeps its historical name
(`m_renderer`) to preserve scene serialization across the system's refactors, and the selection
dots' material is a serialized template asset rather than a runtime-found shader — the reason is
failure-museum entry 3 (Section 4.10).

## 4.3 The Hybrid Composite

The system's flagship configuration composes the tiers: a **Tier-3 processed camera feed as the
periphery, with the Tier-2 transparent window revealing native passthrough as the focus region** —
two different renderings of the same reality, fused into one continuous view.

Mechanically: the Passthrough Camera Access feed is bound to the sphere material (`_MainTexL`, and
`_MainTexR` where the right camera is available), the fragment shader computes each periphery
fragment's colour from the feed (with whatever manipulation the active mode specifies — blur,
desaturation, salience re-grading), and outputs it at `alpha = t·strength`; inside the window,
`t = 0` yields transparency and the OS layer shows through. The seam between the two renderings is
the window's soft edge, where the processed feed fades out over `_SoftEdge` radians; because both
renderings depict the same world from (nearly) the same viewpoint, the transition reads as an effect
boundary, not as two images.

The construction has one honest seam-level caveat, stated here because it motivates a Chapter 5
benchmark. The two renderings do not share a latency budget: the OS passthrough inside the window is
served by the platform's dedicated low-latency reprojection path, while the camera-fed periphery
carries the application pipeline's additional delay. In static or slowly moving views the mismatch is
invisible; under fast head rotation the periphery can momentarily lag the window at the seam. The
system's motion-based suppression (Section 4.6) incidentally masks the worst case — the effect fades
out precisely when the head moves fast — but the residual mismatch is a real property of the hybrid
and is measured, not assumed away, in the planned latency benchmark.

The claim this construction supports is deliberately precise (Chapter 3, Section 3.6): the
*composite* is the novelty — exploiting the quality/FOV asymmetry between a native layer that cannot
be touched and a camera copy that can — not any of its ingredients. Its costs are Tier 3's costs
(resolution, latency, fusion, heat), paid only in the periphery; its planned quantitative
characterisation (per-mode GPU cost, legibility of window versus periphery, thermal envelope) is
specified in Chapter 5.

[Figure 4.3 — Through-the-lens style mock-up or screenshot pair: the same room with the hybrid
active — native-quality window, processed periphery — annotated with which tier renders which
region.]

## 4.4 Binocular Fusion by World-Direction Sampling

The accessible camera feed is monocular; the display is binocular. How the mono image is presented
to two eyes determines whether the system is usable at all, and the first implementation failed
exactly here.

**The failure.** The initial shader sampled the camera texture by screen position: each fragment
looked up the camera pixel at its own normalised device coordinate. Under stereo rendering, the same
world point projects to *different* screen positions in the left and right eye; sampling by screen
position therefore fed each eye a different camera pixel for the same world point. The binocular
result did not fuse — users experienced frank double vision, confirmed on device. This entry in the
failure museum (Section 4.10) is instructive because the code was "correct" per eye and broken only
as a binocular system.

**The fix.** The replacement samples by **world direction**. Each fragment's direction from the
sphere centre (already computed for the window test) is projected through a head-centred pinhole
model of the camera: with the camera's forward/right/up basis (`_CamLFwd`, `_CamLRt`, `_CamLUp`) and
half-FOV tangents (`_TanHalfFovL`, derived from the camera intrinsics as
`SensorResolution / (2·FocalLength)` rather than an assumed FOV), the shader computes

```
uv = ( (dir·rt, dir·up) / max(dir·fwd, ε) ) / tanHalfFov · 0.5 + 0.5
```

Because world direction is **eye-independent**, both eyes sample the *same* camera pixel for the
same world point; the mono image behaves like a texture painted onto the world shell, and the visual
system fuses it. The residual (and acceptable) artefact is that the painted shell has no true
stereo disparity of its own — consistent with the project's standing comfort constraint that a mono
feed must never be presented as if it were geometry-correct stereo across the full field of view.

Where both cameras are available, the shader samples both and selects per fragment by in-view
weight — a **hard split** (`inFovL ≥ inFovR ? sampledL : sampledR`) rather than an alpha crossfade,
because a crossfade of two horizontally offset viewpoints produces exactly the ghosting the
world-direction scheme exists to avoid. The two forward cameras share an orientation and differ only
by a small horizontal baseline, so the stitch line is invisible in normal use.

[Figure 4.4 — Fusion diagram: same world point, two eyes; screen-space sampling (two different
camera pixels, double image) versus world-direction sampling (one camera pixel, fused).]

## 4.5 The Modes

The mode enumeration in the codebase (`VignetteMode`) contains eleven entries; three are evaluated
in this dissertation, one is their superseded ancestor, six are retained design-exploration
variants, and one is a video-only, detection-gated demo mode (Section 4.5.6). All share the
geometry, window, and t-coordinate of Section 4.2 — a mode is, in implementation terms, a
per-fragment colour/alpha policy.

### 4.5.1 Hard Dark

The simplest and strongest manipulation: `periColor = black`,
`periAlpha = t · _VignetteStrength · _MaxVignetteAlpha` with `_MaxVignetteAlpha = 1.0`. At full
formation the periphery is completely occluded; the user's visual world *is* the focus window. The
shader path (`_SimpleMode = 1`) samples no textures and does no camera mathematics — an early return
delivers what is effectively a fixed-function overlay, with correspondingly negligible GPU cost and
no camera permission or thermal exposure (this is the deployability argument of Chapter 3,
Section 3.5, in code). Hard Dark is the confirmatory mode of user-study Block A (Chapter 6): the
strongest available manipulation, chosen to maximise the chance of detecting the distraction-
suppression mechanism if it exists.

### 4.5.2 Soft Dark

Identical to Hard Dark in every respect except the ceiling: `_MaxVignetteAlpha` is set from
`m_mode2MaxAlpha` (default **0.75**), so the periphery darkens substantially but never fully —
peripheral motion and events remain visible through the veil. Soft Dark is the minimal, unconfounded
manipulation of the mode family (pure luminance attenuation, one variable) and the deployable
Tier-2 midpoint; its design-space defence is given in Chapter 3, Section 3.5, and its evaluation is
subjective-only (user-study Block C) for the session-budget reasons documented in Chapter 6.

Both dark modes **form gradually**: on window lock, a coroutine animates `_VignetteStrength` from 0
to 1 along a SmoothStep over `m_vignetteFormTime` (default **3 s**), so the periphery dims as a
transition rather than a cut. Formation runs independently of the motion suppression of Section 4.6
— if the user's head moves during formation, suppression multiplies the strength down while the
formation clock keeps counting, and the effect resumes at its accumulated level when the head
settles.

### 4.5.3 ColorPop

ColorPop is the Tier-3 mode: a salience re-grading of the camera feed that implements the
literature's strongest guidance channel — luminance and saturation contrast, with no hue shifts
(Bailey et al., 2009; Grogorick et al., 2017; Sutton et al., 2022) — as a *standing property of the
whole visual field* rather than a transient cue. After on-device comparison across the mode family,
ColorPop was the standout and became the dissertation's dynamic-scenario mode (user-study Block B).

Its grading is a **four-level hierarchy** on two tracks, gated by colour class and interpolated by
the window coordinate t:

> outside-other < outside-ROG < inside-other < inside-ROG

**Colour classification.** A shader helper (`PopColorKeep`) computes membership in the kept
red/orange/green (ROG) set — the colour classes of signal lamps and signage. Membership is defined
by hue bands (warm band hue ∈ [0, 0.18] fading out by 0.25; a wrap-around red band above 0.85; a
green band 0.22–0.48) gated by saturation floors. The warm band uses a deliberately higher floor
(`_PopWarmSatMin` = 0.35): warm-*white* light — headlights at hue ≈ 0.1 and saturation 0.1–0.25 —
must not ride into the "orange" class, while genuine lamps and signs sit at saturation above 0.5.

**The "other" track** (non-ROG pixels): near-natural inside the window (a slight desaturation,
`_PopInsideDesat` = 0.25, keeps the window from feeling artificially vivid against its own
periphery), graded down to dim grey outside (`_PopGreyDim` = 0.4) — the periphery's non-signal
content is present but muted.

**The ROG track**: inside the window, saturation is boosted ×`_PopSatBoost` (1.7) about the pixel's
own luminance, a sigmoidal midtone-contrast transfer is applied per Sutton et al.'s published recipe
(α = 10, β = 0.5, normalised so 0→0 and 1→1; strength `_PopSigmoid` = 0.6, scaled by (1−t) so it is
full inside the window and absent outside), and brightness is lifted ×`_PopBrightIn` (1.15).
Outside the window, ROG pixels keep their **natural colour, dimmed** to ×`_PopBrightOut` (0.65) —
a deliberate design decision: signal-relevant colours remain detectable *everywhere*, merely
privileged inside the window, which fits a monitoring context in which a peripheral traffic light
must never become invisible.

**Glare compression.** Pixels that are bright (`luma > _PopGlareKnee` = 0.6), unsaturated, and not
ROG — the signature of headlight glare — are compressed toward the knee, i.e. toward a visible dim
spot, never black: the oncoming vehicle stays legible while its veiling glare is attenuated. The
compression is mild inside the window (`_PopGlareInside` = 0.35) and strong in the periphery
(`_PopGlareOutside` = 0.85). A **blown-core guard** protects the one case the mask would otherwise
destroy: the clipped white centre of a bright signal lamp. Before dimming, the shader samples a small
Gaussian surround (`_PopGuardRadius` = 0.008 UV); if the surround is a kept colour — the lamp's
saturated fringe — the pixel is exempted.

**Global periphery dim.** Finally the whole periphery, ROG included, is multiplied down by
`_PopPeriphDim` (0.85), so that the focus window also wins on plain luminance — the strongest single
guidance channel in the attention literature. Net outside-ROG brightness at defaults is therefore
≈ 0.65 × 0.85 ≈ 0.55 of natural.

Because a colour/grey boundary reads perceptually harsher than a blur or darkness boundary, ColorPop
uses a widened window edge: `m_popSoftEdgeDeg` = **32°** against the global default of 20°.

In the passthrough scene, ColorPop grades the periphery only (the window must remain transparent —
the OS layer cannot be graded, and repainting the full field from the mono feed would violate the
comfort constraint). In the video test scene (`_PassthroughMode = 0`) the same shader grades the
entire sphere, window included (`periAlpha = lerp(1, t, _PassthroughMode) · strength`), which is the
configuration evaluated in Block B.

[Figure 4.5 — ColorPop grading pipeline flowchart: classify (ROG?) → glare mask + core guard →
track-specific grading → periphery dim; with a screenshot pair showing a street scene off/on.]

### 4.5.4 Blur (superseded) and the exploratory family

The system's ancestor mode — **Blur** — renders the periphery from the camera feed with progressive
Gaussian blur and delayed desaturation, and is retained in the codebase though not evaluated. Its
implementation is more sophisticated than the project's early documentation suggests (the docs'
"9-tap kernel" describes a superseded version): the current path is a two-ring Gaussian
(centre + 8 taps at radius r + 8 taps at 2r, σ = r) whose fine/coarse difference doubles as a local
Laplacian estimate driving a **perceptual noise term** (after Tariq, Tursun & Didyk, 2022): world-
anchored luminance noise, amplitude-scaled by local variance (capped at 0.2 to prevent marbling at
hard night-scene boundaries), reintroducing "detectable but not resolvable" high-frequency content
that blur otherwise strips. A **contrast-restoration** term (after Patney et al., 2016) re-amplifies
the blurred signal's residual local contrast (`_BlurContrastRestore` = 0.8, overshoot clamped to
±0.15), because peripheral blur is detected primarily through its contrast loss — restoring contrast
roughly doubles tolerable blur radius and removes the tunnel-vision percept. Blur precedes nothing:
desaturation begins at `_DesatDelay` = 0.3 of the t-range and blur only at `_BlurDelay` = 0.5, so
colour mutes before structure softens.

Two sampling details make the blur path viable on the equirectangular video sphere as well as the
camera feed. Blur taps go through a mode-aware UV helper: in camera mode both axes clamp (the camera
image does not wrap), while in equirectangular mode the horizontal axis wraps (`frac`) for a seamless
360° join and out-of-range vertical taps are *folded back* rather than clamped — clamping would smear
the top and bottom texture rows across the sky and floor. Independently, the blur radius is faded to
zero toward the poles by a `cos(el)` factor, since at the poles any UV-space offset corresponds to a
huge angular distance and unfaded taps produce visible polar artefacts.

### 4.5.5 Cost structure of the shader paths

The mode family spans roughly two orders of magnitude in per-fragment cost, and the gradient is
structural rather than incidental — it is Chapter 3's deployability axis realised in instructions.
The dark modes take an early return after the window test: no texture reads, a handful of ALU
operations, one blended write. ColorPop reads the source texture once per fragment on its main path,
plus a 17-tap Gaussian surround *only* inside the small fraction of fragments where the glare mask
fires with the core guard enabled; its cost is dominated by per-pixel colour arithmetic (HSV-style
classification, two grading tracks, a sigmoid). The Blur path is the heaviest: two Gaussian rings of
8 taps each around a shared centre (17 taps) per camera, doubled when both cameras are live near the
stitch, plus the noise texture read and contrast-restoration arithmetic. These are analytic
expectations; their empirical confirmation (per-mode GPU frame cost against the 72/90 Hz budget) is
one of the planned benchmarks of Chapter 5. The design consequence stands regardless of exact
numbers: the modes the study asks participants to imagine using for long sessions (the dark family)
are the modes that cost almost nothing, and the mode with the richest effect (ColorPop) is the one
whose Tier-3 pipeline carries the platform's thermal ceiling.

Six further exploratory modes are retained in shader and enumeration but excluded from evaluation,
each a literature-derived point in the design space: **TintedDark** (configurable-colour overlay),
**ChromaticCool** (warm window / cool periphery), **ConspicuitySqueeze** (centre–surround contrast
flattened toward the local mean, after Veas et al., 2011), **GranulatedPeriphery** (world-locked
static noise grains with eccentricity-ramped density, after Cao et al., 2021), **OutlinedDark**
(near-blackout with luminance edges restored, after Cheng et al., 2022's finding that context-
preserving outlines reduce anxiety), and **SpotLift** (focus brightness lift over a soft peripheral
dim — video mode only, since OS passthrough cannot be brightened). They are reported as explored
design points; none carries evaluation claims. The video test scene's free-play mode cycle
(`k_modeCycle`, the A-button cycle) walks the entire eleven-entry enum in enum order — matching the
passthrough scene's cycle — rather than being restricted to the evaluated set; a 2026-07-04 change
had briefly trimmed it to the three evaluated modes, but the cycle was subsequently widened back to
the full enum so free play and demos can reach every mode, while the study proper still only ever
programmatically selects ColorPop, Soft Dark, and Hard Dark (Chapter 6).

[Figure 4.6 — Screenshot triptych of the three evaluated modes on the same video frame.]

### 4.5.6 SignPop (video-only, detection-gated)

**SignPop** is an eleventh mode, live only in the video test scene: the same grading pipeline as
ColorPop, but the "pop" boost is gated by the offline oracle detection track of Section 4.8.2 rather
than by colour class alone. Only pixels inside an actual detected traffic-light/stop-sign box get
the full pop treatment; other red/orange/green-ish pixels (neon signage, brake lights, advertising)
fall back to a partial dim (`m_signRogFallback`, default 0.35) rather than full grey — a "graceful
miss" so a gap in the bake dims a real signal rather than hiding it outright. A short hold
(`m_signDetHoldSec`, default 0.5 s) keeps a detection alive after it drops out of the bake so pops do
not blink between samples. SignPop is not an evaluated mode; it exists as a free-play/demo
illustration of what detection-conditioned grading looks like, and is used by one condition of the
harness's `TestModeSequencer`.

## 4.6 Motion-Based Suppression

Head-coupled restriction of the visual field is a known comfort hazard: Norouzi et al. (2018) found
that vignetting yoked to head rotation *increased* rather than decreased simulator sickness. The
system therefore decouples in the opposite direction: the effect **fades out while the head moves
fast, and returns when it settles**.

Each frame the manager computes head angular speed via `Quaternion.Angle` between successive head
rotations. When speed exceeds a per-mode threshold — defaults `m_motionBlur` 30°/s (hold 0.6 s),
ColorPop 35°/s (hold 0.7 s), `m_motionSoftDark` 50°/s (hold 1.0 s), `m_motionHardDark` 70°/s (hold
1.5 s); thresholds scale with the mode's intrusiveness, so the blackout mode tolerates the most
motion before yielding — a suppression scalar animates toward 1 over `m_motionFadeOutSec` (0.20 s)
and, once speed has stayed below threshold for the mode's hold time, back toward 0 over
`m_motionFadeInSec` (0.70 s). The shader receives
`effectiveStrength = m_vignetteStrength · (1 − suppression)`: suppression multiplies, never resets,
the formation state.

One refinement matters for the intended interaction loop. If the head's direction enters the focus
window (with a +15° margin, `k_focusArrivalMarginRad` = 0.2618), the hold timer is zeroed
**immediately** — arriving at the focus region restores the effect at once, without waiting out the
hold. The asymmetry is deliberate: looking *away* is treated as a possible need for situational
awareness (effect yields, generously), while looking *back at the task* is treated as re-engagement
(effect returns, promptly).

The parameterisation encodes a policy, not just a tuning surface. Thresholds and hold times scale
*with* the mode's intrusiveness — the blackout mode requires the fastest head motion (70°/s) to
yield and waits longest (1.5 s) before returning — because the cost of wrongly suppressing a strong
effect (a flash of the full periphery) is jarring, while the cost of wrongly suppressing a gentle one
is negligible. The fade asymmetry (out in 0.20 s, in over 0.70 s) follows the same logic: yielding
must feel instantaneous to be trusted; returning may be leisurely because nothing is at stake. And
suppression *multiplies* the formation state rather than resetting it, so a glance over the shoulder
during the 3-second formation does not restart the transition — state that the user created (the
locked window, the accumulated formation) is never destroyed by a comfort mechanism, only veiled.

[Figure 4.7 — Suppression state diagram: speed threshold, hold timer, fade-out/fade-in ramps, and
the focus-arrival shortcut.]

## 4.7 Interaction Design

The user defines the focus window by **painting** it: holding the right controller trigger sweeps
the controller's aim across the scene, and the window rectangle grows to enclose the swept region
(seeded at the aim point with a small brush pad); releasing the trigger **locks** the window and, in
the dark modes, starts the 3-second formation. The rectangle is computed and stored directly in the
world az/el coordinates of Section 4.2.2 (via `atan2`/`asin` on the controller-forward direction),
so what the user paints is what stays locked to the room. The **A** button cycles modes; **B**
clears the window and cancels the effect.

Feedback is deliberately minimal: five runtime dot markers (four corners plus a yellow aim cursor,
`m_dotSize` = 0.055 m spheres) show the window during painting, dim on lock, and auto-hide after
`m_dotHideDelay` = 3 s via a shrink animation (painting again restores them); a world-space toast
panel (0.54 × 0.15 m, lazily following at 1.5 m ahead and 0.3 m below eye level, billboarded to the
head) announces mode changes with a name, one-line description, pip-dot position indicator and a
colour-coded accent stripe, fading in over 0.35 s and away after 2.8 s. Hand tracking is disabled;
input is controller-only, a scope decision that removes a class of input ambiguity from the study
sessions.

Three design rationales are worth making explicit. *Painting rather than pointing*: a focus window is
a region, not a point, and asking the user to sweep it makes the region's extent an explicit,
deliberate act — the alternative (auto-sizing a window around a pointed target) hides a consequential
parameter inside a heuristic. *World-locking rather than head-locking*: the window belongs to the
task surface, not to the gaze; a head-locked window would chase the user's attention rather than
anchor it, and head-coupled visual-field manipulation is precisely the comfort hazard Section 4.6
exists to avoid. *Formation as transition*: the 3-second SmoothStep exists because an instantaneous
peripheral blackout is startling — the gradual close is the system's only concession to theatricality,
and it doubles as the user's confirmation that the lock succeeded. The temporal behaviours here —
formation, suppression fade-out/fade-in, dot auto-hide — are, per the literature review's gap
analysis (Chapter 2), among the least-studied parameters in DR design; the system therefore exposes
every one of them as an Inspector-tunable value rather than a constant.

[Figure 4.8 — Interaction sequence: paint (dots visible) → release/lock → formation → dots auto-hide;
plus the mode toast.]

## 4.8 Perception: Detection Islands and the Offline Oracle

### 4.8.1 Live detection support

The live passthrough manager (`CameraSphereVignetteManager`) supports up to eight simultaneous
**detection islands** (`k_maxDetections = 8`, backed by the shader's `_DetectionRects[16]` array):
az/el rectangles, fed by an optional `YoloRunner` component, inside which the vignette is cleared
(`t = min(t, 1 − inside)`) so the detected object shows through undiminished. Each island renders as
a soft-edged ellipse (feather from `m_detectionSoftEdgeDeg` = 3°) with a **dual modulation** after
Veas et al. (2011): the object itself receives a mild saturation/brightness lift
(`m_detectionEnhance` = 0.4) while a soft annulus just outside its boundary is slightly darkened
(`m_detectionSurround` = 0.15), raising the object's centre–surround contrast from both sides
without a glow artefact. The lift breathes gently at ~1 Hz (`m_detectionPulseAmp` = 0.25) — a
low-frequency, low-amplitude temporal modulation chosen for attention capture at low annoyance
(Waldner et al., 2014), far below the photosensitivity band. Detections expire after
`m_detectionLifetime` = 0.6 s without renewal. The class filter is deliberately narrow — COCO
classes 9 (traffic light) and 11 (stop sign) only.

### 4.8.2 The offline oracle pipeline

For evaluation, live inference is replaced by a **pre-baked detection track** — the oracle-perception
instantiation of Chapter 6's methodology. A PC-side script (`Tools/bake_detections.py`) runs YOLO11
over the full-resolution 3840×2160 video used in the test scene, as 3×2 overlapping tiles plus a
full-frame pass merged by per-class non-maximum suppression, sampling at 0.25 s intervals, and writes
a JSON track (`DebugVideo.detections.json`) keyed by video time; the runtime loads it with
`m_useBakedDetections = true` and plays detections back deterministically.

The pipeline exists because its predecessor failed informatively: the original in-editor bake ran on
a 640×360 downsample, below the pixel size of distant traffic lights, and silently missed most of
them (Section 4.10). The full-resolution bake of 2026-07-04 (~30 minutes on CPU for the 180-second
clip) produced **9,170 traffic-light and stop-sign detections across 720 samples, with 97 % of
samples containing at least one detection** (median 12 per sample). Unlike the live manager, the
video test scene's manager (`VideoTestSceneManager`) draws on the shader's full sixteen-slot
`_DetectionRects[16]` array (`k_maxDetections = 16`): an earlier eight-slot cap was found to drop
roughly a third of the median-12-per-sample detections and was raised to sixteen specifically to
absorb the oracle bake's density, so no ordering or survival policy is needed for the great majority
of samples. These figures are the system's preliminary oracle-side data and reappear in Chapter 5 as
one arm of the planned oracle-versus-live benchmark.

[Figure 4.9 — Oracle pipeline: source video → tiled YOLO11 full-res inference → NMS merge → JSON
track → deterministic playback into shader islands.]

## 4.9 The Video Test Scene

All dynamic-scenario evaluation runs in a dedicated harness scene (`VideoTestScene.unity`,
`VideoTestSceneManager.cs`) in which the passthrough layers are replaced by a 180-second
equirectangular driving clip decoded onto the sphere (`VideoPlayer` → RenderTexture → `_MainTexL`,
with equirectangular sampling `_EqCamSampling = 1` and pole-safe blur tap folding). The scene
preserves everything else — the same shader, window logic, modes, interaction, and detection islands
— so a mode behaves identically over video and over the live camera feed, while gaining what live
passthrough can never provide: **identical, repeatable, event-dense stimuli for every participant**,
plus a deterministic ground-truth detection track. The harness is thus the simulation-based-
evaluation instrument of the user study (Chapter 6, Block B and Block C) and the reason the study's
stimuli are versionable artifacts rather than descriptions.

The harness carries its own alignment and perception plumbing. The equirectangular mapping exposes
horizontal and vertical UV offsets (`m_videoUOffset`, `m_videoVOffset`) and a vertical flip
(`m_flipVideoY`) so that any source clip's forward direction and horizon can be registered to the
scene's coordinate frame without re-encoding the video. For live-inference experiments the manager
maintains a dedicated 640×360 render target for the detector (`m_yoloRT`) so that YOLO never reads
the 4K video texture directly — the downsample blit happens inside the detector component, on the
detector's own cadence rather than every frame; for evaluation this path is bypassed entirely in
favour of the baked track (`m_useBakedDetections = true`, the default). Mode robustness is handled at
startup: any stale serialized mode from an older scene save is snapped to the current cycle's default
(ColorPop), and the removed exploratory modes' shader toggles are zeroed once, since nothing drives
them per-frame any more.

Two harness-specific notes have study consequences. First, in the video scene the ColorPop grading
covers the full sphere including the window (Section 4.5.3), which is the intended Block B
configuration. Second, the video manager's motion-suppression master switch (`m_enableMotion`)
currently defaults to **false** — study Block B specifies suppression active, so the study
configuration must set it explicitly (flagged in the study tooling specification, Chapter 6
Appendix).

## 4.10 The Failure Museum

Documented dead ends are reusable knowledge; this section reports the six failures that shaped the
system, each with what was tried, why it failed, and what replaced it.

**1. Screen-space camera sampling → double vision.** The first shader sampled the camera texture by
screen position. Per-eye the image was correct; binocularly it could not fuse, because each eye
sampled a different camera pixel for the same world point. On-device symptom: frank double vision.
Replaced by world-direction sampling through a head-centred pinhole (Section 4.4), verified fused on
device. Lesson: in stereo rendering, *eye-independence of the sampling domain* is a correctness
criterion, not an optimisation.

**2. Surface-projected passthrough → deprecated API.** The obvious platform mechanism for "put real
passthrough on my own geometry" — surface-projected passthrough — was evaluated and rejected because
it is deprecated as of SDK v83. Any architecture built on it would be built on removed ground.
Replaced by the window-as-absence composite, which uses only stable compositor behaviour. Lesson: on
young platforms, prefer primitives the OS must keep (alpha compositing) over conveniences it may
withdraw.

**3. `Shader.Find` on device → null.** Runtime materials created via `Shader.Find("Unlit/Color")`
worked in the editor and silently returned null on Quest: Android builds strip shaders that no
serialized asset references, and the failure surfaced as a crashed startup coroutine rather than an
error. Replaced by a serialized **template material** (`SelectionDotMat.mat`, assigned in the
Inspector as `m_dotMaterialTemplate`) that runtime dots clone — the asset reference guarantees build
inclusion. Lesson: never construct device materials from name lookups; ship a serialized template.

**4. Downsampled detection baking → missing traffic lights.** The first oracle bake ran YOLO on a
640×360 downsample of the 4K source; distant traffic lights fell below detectable pixel size and the
track silently omitted most of them — an oracle that was not an oracle. Replaced by the full-
resolution tiled PC-side baker (Section 4.8.2), raising coverage to 97 % of samples. Lesson:
validate ground-truth pipelines against the *smallest* objects they must contain, and never let the
convenience path (in-editor, low-res) overwrite the reference track (`BakeOnPlay` now defaults off).

**5. Building into a OneDrive-synced folder → corrupted builds.** Android builds intermittently
failed with `IOException: RuntimeActionBindings.json already exists` and related file-lock errors;
cause was OneDrive synchronisation locking freshly written build files mid-process (compounded once
by a stale file from a prior failed build). Fixed by building to a non-synced local folder and
clearing stale outputs. Lesson: cloud-sync directories are hostile to build systems; keep build
outputs outside them.

**6. Camera-service screenshots → black frames.** Capturing evidence via the platform's camera-based
screenshot service returned black images whenever the application held the passthrough camera — the
service and the app contend for the same resource. Replaced by framebuffer capture
(`screencap`/`screenrecord`), which records the composited display output. Lesson relevant to the
study protocol: the redundant session recording (Chapter 6) must use the framebuffer path.

**7. Controller input silently dead at session start.** On several occasions the headset booted with
controllers in the `CONNECTED_INACTIVE` state (asleep, or the OS stuck in hand-tracking mode), so the
application received no input at all — indistinguishable, from inside the app, from a broken build.
Mitigations: the study build boots directly into the experiment scene (no menu navigation requiring a
laser pointer), hand tracking is disabled to remove the ambiguous input mode, and "wake controllers /
restart headset if input is dead" is a line on the pre-session checklist rather than tribal
knowledge. Lesson: input liveness is an environmental precondition; put it in the protocol, not in
the debugger.

**8. The magenta floor patch.** During video-scene work a metre-wide error-magenta rectangle
appeared lying on the floor at the origin. Cause: a leftover `SelectionLine` GameObject from the
passthrough scene's earlier selection UI — a `LineRenderer` with a null material (Unity renders
missing materials in error magenta), default width, never wired to the video manager. Deleted from
the scene. Lesson, minor but real: when a system evolves by scene-cloning, orphaned scene objects are
a distinct defect class — audit clones against the components that actually drive them.

## 4.11 Summary

One sphere, one shader, one manager: the implementation realises Chapter 3's design space with a
per-fragment colour/alpha policy over a world-locked spherical window. Its load-bearing techniques —
the window as absence, the hybrid composite of native passthrough and processed camera feed, and
eye-independent world-direction sampling — are each answers to a specific platform wall, and its
failures are documented with the same care as its successes because both are findings about building
DR on consumer hardware. What the system *costs* (frames, milliseconds, heat) and how far its live
perception stands from the oracle it is evaluated under are the questions of Chapter 5.

---

## References (this chapter)

- Bailey, R., McNamara, A., Sudarsanam, N., & Grimm, C. (2009). Subtle gaze direction. *ACM Transactions on Graphics*, 28(4).
- Cao, Z., Grandi, J., & Kopper, R. (2021). Granulated Rest Frames Outperform Field of View Restrictors on Visual Search Performance. *Frontiers in Virtual Reality*, 2:604889. doi:10.3389/frvir.2021.604889
- Cheng, Y., Yin, Y., Yan, Y., Gugenheimer, J., & Lindlbauer, D. (2022). Towards Understanding Diminished Reality. *Proc. CHI 2022.* doi:10.1145/3491102.3517452
- Grogorick, S., Stengel, M., Eisemann, E., & Magnor, M. (2017). Subtle gaze guidance for immersive environments. *Proc. ACM SAP 2017.*
- Meta (2025). *Passthrough Camera Access.* developers.meta.com/horizon/
- Norouzi, N., Bruder, G., & Welch, G. (2018). Assessing Vignetting as a Means to Reduce VR Sickness During Amplified Head Rotations. *15th ACM Symposium on Applied Perception (SAP '18).* doi:10.1145/3225153.3225162
- Patney, A., Salvi, M., Kim, J., et al. (2016). Towards foveated rendering for gaze-tracked virtual reality. *ACM Transactions on Graphics*, 35(6).
- Sutton, J., Langlotz, T., Plopski, A., Zollmann, S., Itoh, Y., & Regenbrecht, H. (2022). Look over there! Investigating saliency modulation for visual guidance with augmented reality glasses. *Proc. UIST 2022.* doi:10.1145/3526113.3545633
- Tariq, T., Tursun, C., & Didyk, P. (2022). Noise-based Enhancement for Foveated Rendering. *ACM Transactions on Graphics.* doi:10.1145/3528223.3530101
- Veas, E., Mendez, E., Feiner, S., & Schmalstieg, D. (2011). Directing attention and influencing memory with visual saliency modulation. *Proc. CHI 2011.* doi:10.1145/1978942.1979158
- Waldner, M., Le Muzic, M., Bernhard, M., Purgathofer, W., & Viola, I. (2014). Attractive flicker: Guiding attention in dynamic narrative visualizations. *IEEE TVCG*, 20(12), 2456–2465. doi:10.1109/TVCG.2014.2346352
