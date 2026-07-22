// Copyright (c) Meta Platforms, Inc. and affiliates.
//
// Multi-mode Diminished Reality focus-window overlay.
//
// Mode 1 (Blur):          dual-camera passthrough texture, blurred + desaturated periphery.
// Mode 2 (SoftDark):      pure dark overlay, periphery never fully opaque (~75% max).
// Mode 3 (HardDark):      pure dark overlay, periphery goes fully black.
// Mode 4 (TintedDark):    configurable-colour dark overlay (e.g. deep navy).
// Mode 5 (ChromaticCool): camera feed with warm focus window, cool blue periphery.
// Mode 6 (ColorPop):      four-level red/orange/green hierarchy — ROG boosted inside the
//                         window, kept-but-dimmed outside; everything else near-natural inside,
//                         dim grey outside. Bright unsaturated glare (headlights) compressed.
//                         In video mode this grades the full sphere, window included.
//
// Focus window is always transparent so OS passthrough (or video sphere) shows through at full FOV.
// Cull Front - rendered from inside the sphere.
Shader "Meta/PCA/CameraSphereVignette"
{
    Properties
    {
        [Header(Camera)]
        _MainTexL ("Camera Texture (Left)",  2D) = "black" {}
        _MainTexR ("Camera Texture (Right)", 2D) = "black" {}
        [Toggle] _FlipY ("Flip Camera Y", Float) = 0

        [Header(Focus Window)]
        _FocusRect  ("Focus Rect (az/el radians)", Vector) = (-3.14159, 3.14159, -1.5708, 1.5708)
        _SoftEdge   ("Soft Edge Width (rad)", Float) = 0.1745

        [Header(Mode and Formation)]
        // 0 = Mode 1 (blur+desat camera), 1 = Mode 2/3 (pure dark overlay)
        [Toggle] _SimpleMode      ("Simple Dark Mode (Mode 2 or 3)", Float) = 0
        // Animated 0->1 by C# over formation time
        _VignetteStrength ("Vignette Strength", Range(0, 1)) = 1
        // 1.0 for Mode 3 (full black), <1 for Mode 2 (soft dim)
        _MaxVignetteAlpha ("Max Vignette Alpha", Range(0, 1)) = 1

        [Header(Blur)]
        _MaxBlurRadius ("Max Blur UV Radius", Float) = 0.025
        _BlurCurveExp  ("Blur Curve Exponent", Float) = 1.2
        _BlurDelay     ("Blur Delay (0=together with desat, 0.5=starts halfway)", Range(0, 0.9)) = 0.5
        // Re-amplify local contrast after blurring (Patney et al. 2016: peripheral blur is
        // detected via its contrast loss; restoring it doubles tolerable blur radius).
        _BlurContrastRestore ("Blur Contrast Restoration", Range(0, 2)) = 0.8

        [Header(Desaturation)]
        _DesatDelay    ("Desat Delay (0=starts at inner edge)", Range(0, 0.9)) = 0.3
        _DesatCurveExp ("Desat Curve Exponent", Float) = 1.5

        [Header(Noise)]
        // Used by the Blur-mode perceptual noise enhancement (kept name for material compat)
        _FrostTex ("Noise Texture", 2D) = "white" {}

        [Header(Tinted Dark)]
        _TintColor ("Tint Color", Color) = (0.05, 0.1, 0.25, 1)
        [Toggle] _TintMode ("Tinted Dark Mode (Mode 5)", Float) = 0

        [Header(Chromatic Cool)]
        [Toggle] _ChromaticCool ("Chromatic Cool Shift (Mode 6)", Float) = 0
        _CoolStrength ("Cool Shift Strength", Range(0, 1)) = 0.6

        [Header(Video Test Mode)]
        // 0 = video mode: single opaque sphere, focus zone renders sharp video, periphery renders effect.
        // 1 = passthrough mode (default): transparent focus zone so OS passthrough shows through.
        [Toggle] _PassthroughMode ("Passthrough Mode", Float) = 1
        [Toggle] _EqCamSampling ("Equirectangular Camera Sampling", Float) = 0
        _EqUOffset ("EQ Horizontal UV Offset", Range(-0.5, 0.5)) = 0
        _EqVOffset ("EQ Vertical UV Offset",   Range(-0.5, 0.5)) = 0

        [Header(Color Pop)]
        [Toggle] _ColorPopMode ("Color Pop Mode", Float) = 0
        _PopGreyDim   ("Pop: Muted Periphery Brightness", Range(0.1, 1)) = 0.4
        _PopSatBoost  ("Pop: Kept Color Saturation Boost", Range(1, 3)) = 1.7
        // Four-level hierarchy: outside-other < outside-ROG < inside-other < inside-ROG.
        // Inside-window grading only takes effect in video mode (_PassthroughMode = 0);
        // in passthrough mode the window must stay transparent (OS layer, mono-paint comfort).
        _PopInsideDesat ("Pop: Inside Other Desaturation", Range(0, 1)) = 0.25
        _PopBrightIn    ("Pop: Inside ROG Brightness", Range(1, 1.5)) = 1.15
        _PopBrightOut   ("Pop: Outside ROG Brightness", Range(0.3, 1)) = 0.65
        _PopWarmSatMin  ("Pop: Warm Band Saturation Floor", Range(0, 0.8)) = 0.35
        _PopSigmoid     ("Pop: ROG Sigmoidal Contrast (Sutton 2022)", Range(0, 1)) = 0.6
        _PopPeriphDim   ("Pop: Periphery Overall Dim", Range(0.4, 1)) = 0.85
        _PopGlareKnee   ("Pop: Glare Knee (luma)", Range(0.3, 1)) = 0.6
        _PopGlareInside ("Pop: Glare Dim Inside Window", Range(0, 1)) = 0.35
        _PopGlareOutside("Pop: Glare Dim Periphery", Range(0, 1)) = 0.85
        _PopGuardRadius ("Pop: Glare Core Guard Radius (UV)", Range(0, 0.03)) = 0.008
        // SignPop: gate the ROG keep by the baked detection ellipses — only actual
        // lights/signs pop; colour look-alikes (neon, ads, brake lights) fall back.
        _PopDetGate     ("Pop: Detection Gate (SignPop)", Range(0, 1)) = 0
        _PopDetFallback ("Pop: Undetected ROG Keep (graceful miss)", Range(0, 1)) = 0.35

        [Header(Conspicuity Squeeze)]
        // Flatten peripheral center-surround contrast toward the local mean (Veas CHI 2011).
        [Toggle] _SqueezeMode ("Conspicuity Squeeze Mode", Float) = 0
        _SqueezeRadius ("Squeeze: Local Mean Radius (UV)", Range(0.005, 0.08)) = 0.015
        _SqueezeLum    ("Squeeze: Luminance Flatten", Range(0, 1)) = 0.5
        _SqueezeChroma ("Squeeze: Chroma Flatten", Range(0, 1)) = 0.85
        _SqueezeDesat  ("Squeeze: Extra Desaturation (gentle)", Range(0, 1)) = 0.25
        _SqueezeDim    ("Squeeze: Peripheral Dim", Range(0.5, 1)) = 0.9

        [Header(Granulated Periphery)]
        // World-locked static noise grains; suppress detail, keep event awareness (Cao 2021).
        [Toggle] _GrainMode ("Granulated Periphery Mode", Float) = 0
        _GrainScale      ("Grain Scale (tiles/sphere)", Range(8, 256)) = 90
        _GrainDensityMin ("Grain Density at Window Edge", Range(0, 1)) = 0.25
        _GrainDensityMax ("Grain Density Far Periphery", Range(0, 1)) = 0.75
        _GrainColor      ("Grain Color", Color) = (0.5, 0.5, 0.5, 1)

        [Header(Outlined Dimming)]
        // Near-blackout with luminance edges added back (Cheng CHI 2022: outlines preserve context).
        [Toggle] _OutlineMode ("Outlined Dimming Mode", Float) = 0
        _EdgeGain        ("Outline: Edge Gain", Range(1, 40)) = 12
        _EdgeBrightness  ("Outline: Edge Brightness", Range(0, 1)) = 0.35
        _OutlineDimAlpha ("Outline: Dim Alpha", Range(0.5, 1)) = 0.92

        [Header(Spotlight Lift)]
        // Gentle brightness lift inside the focus window + soft peripheral dim (luminance channel).
        [Toggle] _SpotLiftMode ("Spotlight Lift Mode", Float) = 0
        _SpotLiftAmp  ("Spot: Focus Brightness Lift", Range(0, 0.3)) = 0.08
        _SpotDimAlpha ("Spot: Periphery Dim Alpha", Range(0, 1)) = 0.35

        [Header(Noise Enhancement Blur Mode)]
        // Noise amplitude: scales noise by local image variance (Laplacian approximation).
        // Set to 0 to disable noise. Typical useful range: 0.5-2.0.
        _NoiseAmp       ("Noise Amplitude",       Range(0, 3))  = 1.0
        // Noise frequency: spatial frequency of noise in world-space angular coords (tiles/sphere).
        _NoiseFreqScale ("Noise Frequency Scale", Range(1, 64)) = 16.0

        [Header(Detection Islands)]
        _DetectionSoftEdge ("Detection Island Soft Edge (rad)", Float) = 0.05
        _DetectionEnhance  ("Detection Colour Boost (0=clear only, 1=vivid)", Range(0, 1)) = 0.4
        _DetectionSurround ("Detection Surround Dim", Range(0, 0.5)) = 0.15
        _DetectionPulseAmp ("Detection Boost Pulse Amplitude", Range(0, 1)) = 0.25
        _DetectionOutsideScale ("Detection Boost Intensity Outside Focus Window (subtle)", Range(0, 1)) = 0.35
        // Grading inside detection windows. Defaults preserve the legacy hardcoded look
        // (sat 0.5, bright 0.12, contrast 0) so materials that don't set them are unchanged.
        _DetectionSatLift    ("Detection Saturation Lift", Range(0, 2)) = 0.5
        _DetectionBrightLift ("Detection Brightness Lift", Range(0, 1)) = 0.12
        _DetectionContrast   ("Detection Contrast Boost", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 100
        Cull Front
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTexL;
            sampler2D _MainTexR;
            float4 _FocusRect;
            float  _SoftEdge;
            float  _MaxBlurRadius;
            float  _BlurCurveExp;
            float  _DesatDelay;
            float  _DesatCurveExp;
            float  _FlipY;
            float  _HasRightCam;
            float  _SimpleMode;
            float  _VignetteStrength;
            float  _MaxVignetteAlpha;

            float3 _CamLFwd;
            float3 _CamLRt;
            float3 _CamLUp;
            float4 _TanHalfFovL;

            float3 _CamRFwd;
            float3 _CamRRt;
            float3 _CamRUp;
            float4 _TanHalfFovR;

            int    _DetectionCount;
            float4 _DetectionRects[16];
            float  _DetectionFade[16]; // per-slot 0..1 — smooth fade in/out, replaces hard appear/disappear
            float  _DetectionSoftEdge;
            float  _DetectionEnhance;
            float  _DetectionSurround;
            float  _DetectionPulseAmp;
            float  _DetectionOutsideScale; // boost intensity multiplier when the detection is outside _FocusRect
            float  _DetectionSatLift;      // extra saturation inside detection windows (0.5 = legacy look)
            float  _DetectionBrightLift;   // brightness lift inside detection windows (0.12 = legacy look)
            float  _DetectionContrast;     // contrast expansion around mid-grey (0 = legacy look)
            float  _SuppressDetectionWindows; // Hard Dark: 1 = no per-detection carve-outs/highlights at all — only the painted selection is ever clear

            float3 _SphereCenter;

            sampler2D _FrostTex;

            float4 _TintColor;
            float  _TintMode;

            float  _ChromaticCool;
            float  _CoolStrength;
            float  _EqCamSampling;
            float  _PassthroughMode;
            float  _EqUOffset;
            float  _EqVOffset;
            float  _NoiseAmp;
            float  _NoiseFreqScale;
            float  _BlurDelay;
            float  _BlurContrastRestore;
            float  _ColorPopMode;
            float  _PopGreyDim;
            float  _PopSatBoost;
            float  _PopInsideDesat;
            float  _PopBrightIn;
            float  _PopBrightOut;
            float  _PopWarmSatMin;
            float  _PopSigmoid;
            float  _PopPeriphDim;
            float  _PopGlareKnee;
            float  _PopGlareInside;
            float  _PopGlareOutside;
            float  _PopGuardRadius;
            float  _PopDetGate;
            float  _PopDetFallback;

            float  _SqueezeMode;
            float  _SqueezeRadius;
            float  _SqueezeLum;
            float  _SqueezeChroma;
            float  _SqueezeDesat;
            float  _SqueezeDim;

            float  _GrainMode;
            float  _GrainScale;
            float  _GrainDensityMin;
            float  _GrainDensityMax;
            float4 _GrainColor;

            float  _OutlineMode;
            float  _EdgeGain;
            float  _EdgeBrightness;
            float  _OutlineDimAlpha;

            float  _SpotLiftMode;
            float  _SpotLiftAmp;
            float  _SpotDimAlpha;

            float  _DebugCamOverlay;

            // Blob probes (Route B): low-salience click-targets composited as LOCAL modulations of
            // the real scene pixels, applied after the filter (Point 3). Up to MAXBLOBS probes are
            // shown SIMULTANEOUSLY (the authored-pool study has several targets on screen at once).
            // Per-probe geometry lives in the arrays; style/colour/size are shared globals.
            #define MAXBLOBS 12
            int    _BlobCount;
            float4 _BlobData[MAXBLOBS];   // per probe: xy=(az,el) rad, z=radius (rad), w=strength (0..1 ramp)
            float4 _BlobFlash4[MAXBLOBS]; // per probe: x=hit-flash mix (0..1)
            float  _BlobSigma;     // Gaussian sigma as a fraction of the radius (soft rim)
            float  _BlobStyle;     // 0 = desaturate smudge, 1 = halo ring, 2 = bubble (lens+rim), 3 = local-contrast ring
            float  _BlobDesat;     // (style 0) desaturation at the core (0..1)
            float  _BlobDim;       // (style 0) luminance dip at the core (0..1)
            float  _BlobRim;       // (styles 1/2/3) rim intensity — for style 3, the luminance contrast step
            float  _BlobLens;      // (style 2) refraction/magnification amount
            float4 _BlobRingColor; // (style 3) ring colour (rgb) + persistent alpha (a)
            float  _BlobRingWidth; // (style 3) ring thickness (Gaussian sigma in r01) — smaller = thinner
            float  _BlobRingSegments; // (style 3) >0: that many alternating black/white arc PAIRS
                                      // ("rope") replace the solid colour — see style 3 notes
            float  _BlobRingOutline;  // (style 3) strength of the dark border flanking the ring
                                      // (traffic-warning-sign pairing) — contrast on bright backgrounds
            float4 _BlobFlashColor;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex   = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float2 CamUV(float3 dir, float3 fwd, float3 rt, float3 up, float2 tanHFov)
            {
                float dz   = max(dot(dir, fwd), 0.001);
                float2 ndc = float2(dot(dir, rt), dot(dir, up)) / dz;
                return ndc / tanHFov * 0.5 + 0.5;
            }

            float InFovWeight(float2 uv)
            {
                float2 ed = min(uv, 1.0 - uv);
                return saturate(min(ed.x, ed.y) / 0.04);
            }

            // UV sampling helper for blur taps.
            // EQ mode: wrap u (seamless 360 horizontal), fold v back to center row if out of range
            //          (clamping would smear the top/bottom texture row across the sky/floor).
            // Camera mode: clamp both (camera image does not wrap).
            float2 BlurUV(float2 uvC, float du, float dv, float eqMode)
            {
                if (eqMode > 0.5)
                {
                    float newV = uvC.y + dv;
                    if (newV < 0.0 || newV > 1.0) dv = 0.0; // fold out-of-range tap back to center row
                    return float2(frac(uvC.x + du), uvC.y + dv);
                }
                else
                    return clamp(uvC + float2(du, dv), 0.0, 1.0);
            }

            // Gaussian blur (sigma=r, 17 taps) + perceptual noise from Tariq et al. 2022.
            // Noise amplitude is scaled by the local Laplacian (fine-coarse blur difference),
            // approximating the "detectable but not resolvable" aliasing-band content from the paper.
            // noiseUV should be world-space az/el coords for temporal coherence.
            float3 SampleGaussianNoisy(sampler2D tex, float2 uvC, float r,
                                       sampler2D noiseTex, float2 noiseUV, float noiseAmp,
                                       float eqMode, out float3 coarseOut)
            {
                // Gaussian weight at d=r with sigma=r: exp(-d^2/2sigma^2) = exp(-0.5) ~= 0.6065
                const float w1 = 0.6065;
                const float wSum = 1.0 + 8.0 * w1; // = 5.852

                float3 c0 = tex2D(tex, uvC).rgb;

                // Fine ring at radius r
                float3 fineBlur = c0;
                fineBlur += tex2D(tex, BlurUV(uvC,  r,       0.0,     eqMode)).rgb * w1;
                fineBlur += tex2D(tex, BlurUV(uvC, -r,       0.0,     eqMode)).rgb * w1;
                fineBlur += tex2D(tex, BlurUV(uvC,  0.0,     r,       eqMode)).rgb * w1;
                fineBlur += tex2D(tex, BlurUV(uvC,  0.0,    -r,       eqMode)).rgb * w1;
                fineBlur += tex2D(tex, BlurUV(uvC,  r*0.707, r*0.707, eqMode)).rgb * w1;
                fineBlur += tex2D(tex, BlurUV(uvC, -r*0.707, r*0.707, eqMode)).rgb * w1;
                fineBlur += tex2D(tex, BlurUV(uvC,  r*0.707,-r*0.707, eqMode)).rgb * w1;
                fineBlur += tex2D(tex, BlurUV(uvC, -r*0.707,-r*0.707, eqMode)).rgb * w1;
                fineBlur /= wSum;

                // Coarse ring at radius 2r - shared center c0, so +8 extra taps
                float r2 = r * 2.0;
                float3 coarseBlur = c0;
                coarseBlur += tex2D(tex, BlurUV(uvC,  r2,        0.0,        eqMode)).rgb * w1;
                coarseBlur += tex2D(tex, BlurUV(uvC, -r2,        0.0,        eqMode)).rgb * w1;
                coarseBlur += tex2D(tex, BlurUV(uvC,  0.0,       r2,         eqMode)).rgb * w1;
                coarseBlur += tex2D(tex, BlurUV(uvC,  0.0,      -r2,         eqMode)).rgb * w1;
                coarseBlur += tex2D(tex, BlurUV(uvC,  r2*0.707,  r2*0.707,   eqMode)).rgb * w1;
                coarseBlur += tex2D(tex, BlurUV(uvC, -r2*0.707,  r2*0.707,   eqMode)).rgb * w1;
                coarseBlur += tex2D(tex, BlurUV(uvC,  r2*0.707, -r2*0.707,   eqMode)).rgb * w1;
                coarseBlur += tex2D(tex, BlurUV(uvC, -r2*0.707, -r2*0.707,   eqMode)).rgb * w1;
                coarseBlur /= wSum;
                coarseOut = coarseBlur;

                // Local variance ~= luminance of Laplacian (proxy for aliasing-band amplitude).
                // Capped: at hard light/dark boundaries (night scenes) the raw Laplacian
                // reaches ~0.5+, which drives noise to full black/white marbling.
                float localVar = min(dot(abs(fineBlur - coarseBlur), float3(0.299, 0.587, 0.114)), 0.2);

                // Luminance noise - world-space UV for temporal coherence (pattern fixed in env)
                float noiseVal  = tex2D(noiseTex, noiseUV).r;
                float noiseMag  = (noiseVal - 0.5) * 2.0 * localVar * noiseAmp;

                return saturate(fineBlur + noiseMag);
            }

            // ColorPop: membership in the kept red/orange/green (ROG) set — traffic lights
            // and most signage. The warm band uses its own (higher) saturation floor so
            // warm-white headlights (hue ~0.08-0.12 at sat 0.1-0.25) can't ride in as
            // "orange"; real signal lamps and signs sit at sat > 0.5.
            float PopColorKeep(float3 c, float warmSatMin)
            {
                float maxC  = max(c.r, max(c.g, c.b));
                float minC  = min(c.r, min(c.g, c.b));
                float delta = maxC - minC;

                // Hue in [0, 1): 0=red, 0.083=orange, 0.167=yellow, 0.333=green
                float hue = 0.0;
                if (delta > 0.001 && maxC > 0.001)
                {
                    if      (maxC == c.r) hue = fmod((c.g - c.b) / delta, 6.0);
                    else if (maxC == c.g) hue = (c.b - c.r) / delta + 2.0;
                    else                  hue = (c.r - c.g) / delta + 4.0;
                    hue = frac(hue / 6.0);
                }

                // Red/orange band only: keep hue [0, ~0.08], fade out by 0.13 — this EXCLUDES
                // yellow (hue ~0.13-0.17, e.g. NYC taxis) while keeping red and orange signals.
                float warmKeep  = 1.0 - smoothstep(0.08, 0.13, hue);
                // Red that wraps at top end (crimson/rose): hue [0.88, 1.0]
                float redWrap   = smoothstep(0.85, 0.92, hue);
                // Green band: hue [0.26, 0.44]
                float greenKeep = smoothstep(0.22, 0.28, hue)
                                * (1.0 - smoothstep(0.42, 0.48, hue));

                float sat          = (maxC > 0.001) ? delta / maxC : 0.0;
                float satGateWarm  = smoothstep(warmSatMin, warmSatMin + 0.20, sat);
                float satGateGreen = smoothstep(0.15, 0.35, sat);
                return saturate(saturate(warmKeep + redWrap) * satGateWarm
                                + greenKeep * satGateGreen);
            }

            // Sigmoidal contrast, Sutton et al. UIST 2022 recipe (alpha=10, beta=0.5),
            // normalized so 0 -> 0 and 1 -> 1 (raw sigmoid leaves ~0.007 pedestals).
            float3 SigmoidContrast(float3 c, float amt)
            {
                float3 sig = 1.0 / (1.0 + exp(-10.0 * (c - 0.5)));
                sig = (sig - 0.006693) / 0.986614;
                return lerp(c, saturate(sig), amt);
            }

            // Sample the equirectangular video at an arbitrary (az, el) — used by the Bubble
            // probe to refract the real scene (magnify + chromatic fringe).
            float3 SampleEqAngle(float az, float el)
            {
                float2 uv = float2(frac(0.5 + az / (2.0 * UNITY_PI) + _EqUOffset),
                                   clamp(0.5 + el / UNITY_PI + _EqVOffset, 0.0, 1.0));
                if (_FlipY > 0.5) uv.y = 1.0 - uv.y;
                return tex2D(_MainTexL, uv).rgb;
            }

            // Blob probe: a low-salience click-target composited into the real (already-filtered)
            // scene at the probe's az/el. Three styles so the probe can be made visually DISTINCT
            // from the YOLO detection carve-out it sits on (which is a filled brighten/clear):
            //   0 Desaturate — soft colour-loss + dim smudge (blends most; subtlest)
            //   1 Halo       — soft bright RING / outline (a shape, not a fill)
            //   2 Bubble     — glassy droplet: refracts (magnifies) the scene + a bright rim
            // az wrap-around handled via atan2(sin,cos). Hit flash tints the core for all styles.
            // One probe's contribution, parameterised so ApplyBlobs can loop over up to MAXBLOBS
            // simultaneous probes. center=(az,el) rad, R=radius rad, s=strength (ramp), flash=hit mix.
            float3 ApplyOneBlob(float3 c, float az, float el, float2 uvSrc, float eqMode,
                                float2 center, float R, float s, float flash)
            {
                if (s <= 0.001) return c;
                float dA = az - center.x; dA = atan2(sin(dA), cos(dA));
                float dE = el - center.y;
                float d  = sqrt(dA * dA + dE * dE);
                float r01 = d / R;               // 0 at centre, 1 at the radius
                if (r01 > 1.3) return c;         // outside the probe's reach

                if (_BlobStyle < 0.5)
                {
                    // Style 0 — Desaturate smudge.
                    float sig  = max(R * _BlobSigma, 1e-4);
                    float rim0 = exp(-(R * R) / (2.0 * sig * sig));
                    float w    = saturate((exp(-(d * d) / (2.0 * sig * sig)) - rim0) / max(1.0 - rim0, 1e-4)) * s;
                    float lum  = dot(c, float3(0.299, 0.587, 0.114));
                    float3 mod = lerp(c, lum.xxx, _BlobDesat) * (1.0 - _BlobDim);
                    c = lerp(c, mod, w);
                }
                else if (_BlobStyle > 2.5)
                {
                    // Style 3 — Ring: a thin, fixed-colour, translucent ring that fades IN via the
                    // onset ramp (s) and then holds steady. No time term and no scene-coupled
                    // contrast flip, so it NEVER pulses/shimmers (the earlier local-contrast version
                    // shimmered because it recomputed its colour from the moving scene under it each
                    // frame). The ring's colour content-dependence is handled at the design level by
                    // counterbalancing + screening, not per-pixel.
                    //
                    // The ring RESPECTS THE FILTER (supervisor Point 3): its colour is pushed
                    // through the same desat+dim the SignPop grading applies to the scene at this
                    // pixel, gated by the window falloff x effect strength. Filter off / inside
                    // the window => full ring colour; deep periphery => it fades like a real
                    // object would. (A plain black ring — filter-invariant by construction — was
                    // tried on 2026-07-22 and floored the NO-FILTER baseline: 48% hit, 3.55 s
                    // median RT vs ~60-70%, ~1.8 s with yellow. Chroma is what wins peripheral
                    // onsets; approximating the filter transform beats deleting the colour.)
                    float dAzW = max(_FocusRect.x - az, az - _FocusRect.y);
                    float dElW = max(_FocusRect.z - el, el - _FocusRect.w);
                    float tW   = smoothstep(0.0, _SoftEdge, max(dAzW, dElW)) * _VignetteStrength;
                    float3 ringBase = _BlobRingColor.rgb;
                    if (_BlobRingSegments > 0.5)
                    {
                        // "Rope" pattern: alternating black/white arcs. Rationale (2026-07-22):
                        //  - achromatic => the filter's desaturation is a no-op on it;
                        //  - contrast-polarity complete => some arc always contrasts with ANY
                        //    background (the fiducial-marker principle) — fixes both the black
                        //    ring's clutter camouflage and a solid colour's content-dependence;
                        //  - the multiplicative dim preserves the pattern's INTERNAL Michelson
                        //    contrast, so under the filter it dims honestly (Point 3) like a real
                        //    high-contrast object, yet never vanishes into the flat grey periphery.
                        // Keep pairs low (4 => 8 arcs ~= 0.8 cpd at a 1.6 deg probe): peripheral
                        // acuity must resolve the alternation — finer stripes blur to uniform grey.
                        float theta = atan2(dE, dA);
                        ringBase = step(0.5, frac(theta / 6.2831853 * _BlobRingSegments)).xxx;
                    }
                    float ringLum  = dot(ringBase, float3(0.299, 0.587, 0.114));
                    float3 ringCol = lerp(ringBase, (ringLum * _PopGreyDim).xxx, tW)
                                   * lerp(1.0, 0.70, tW);
                    // Dark border flanking the ring (the yellow/black warning-sign pairing):
                    // guarantees a luminance step on bright or yellowish backgrounds where the
                    // ring colour alone has no contrast. Multiplicative darkening => filter-
                    // invariant (Point 3 safe) with no extra attenuation plumbing. Applied
                    // BEFORE the colour ring so the ring sits crisp on top of its border.
                    float bOut = exp(-((r01 - 1.06) * (r01 - 1.06)) / (2.0 * _BlobRingWidth * _BlobRingWidth));
                    float bIn  = exp(-((r01 - 0.58) * (r01 - 0.58)) / (2.0 * _BlobRingWidth * _BlobRingWidth));
                    float border = saturate(max(bOut, bIn)) * _BlobRingOutline * _BlobRingColor.a * s;
                    c = c * (1.0 - border);
                    float ring = exp(-((r01 - 0.82) * (r01 - 0.82)) / (2.0 * _BlobRingWidth * _BlobRingWidth));
                    c = lerp(c, ringCol, saturate(ring * _BlobRingColor.a * s));
                }
                else
                {
                    // Styles 1 (Halo) & 2 (Bubble): a distinct SHAPE, not a contrast change.
                    if (_BlobStyle > 1.5 && eqMode > 0.5)
                    {
                        // Bubble: a glassy droplet. Magnify the scene in ANGULAR space (a real,
                        // visible lens regardless of the tiny UV footprint) — sample an angle pulled
                        // toward the probe centre (k<1 => magnify) — with chromatic fringing that
                        // spreads toward the rim for a convincing glass look.
                        float shape = 1.0 - r01 * r01;          // 1 centre -> 0 rim (magnifier profile)
                        float k  = 1.0 - _BlobLens * s * shape;  // k<1 pulls samples inward => magnify
                        float ca = _BlobLens * s * r01 * 0.15;   // chromatic spread, grows to the rim
                        float3 lensCol;
                        lensCol.r = SampleEqAngle(center.x + dA * (k - ca), center.y + dE * (k - ca)).r;
                        lensCol.g = SampleEqAngle(center.x + dA *  k,       center.y + dE *  k).g;
                        lensCol.b = SampleEqAngle(center.x + dA * (k + ca), center.y + dE * (k + ca)).b;
                        lensCol = saturate((lensCol - 0.5) * 1.12 + 0.5); // mild contrast so magnified detail reads
                        c = lerp(c, lensCol, 1.0 - smoothstep(0.9, 1.12, r01)); // fill the droplet body
                    }
                    // Legible glass edge: a bright SCENE-TINTED outer rim PLUS a dark inner edge
                    // (Fresnel-like double edge — far more noticeable than a single soft ring, and
                    // still natural: that's how real droplet/glass edges read). Both scaled by
                    // _BlobRim, and both are contrast structure so they show even over flat scene.
                    float ringOut = exp(-((r01 - 0.88) * (r01 - 0.88)) / (2.0 * 0.07 * 0.07));
                    float ringIn  = exp(-((r01 - 0.66) * (r01 - 0.66)) / (2.0 * 0.10 * 0.10));
                    float3 rimCol = saturate(c * 1.40 + 0.12);
                    c = lerp(c, rimCol, saturate(ringOut * _BlobRim * s));      // bright outer edge
                    c = c * (1.0 - saturate(ringIn * _BlobRim * s) * 0.45);     // dark inner edge
                    if (_BlobStyle > 1.5)
                    {
                        // Specular glint (up-left) — the classic water-bead cue.
                        float2 gpt   = float2(-0.33 * R, 0.35 * R);
                        float  gd    = length(float2(dA, dE) - gpt) / R;
                        float  glint = exp(-(gd * gd) / (2.0 * 0.16 * 0.16)) * _BlobRim * s;
                        c = saturate(c + glint * 0.9);
                    }
                }

                // Hit flash (all styles): tint the core toward the flash colour.
                float wCore = saturate(1.0 - r01) * s;
                c = lerp(c, _BlobFlashColor.rgb, wCore * flash);
                return c;
            }

            // Composite every active probe (up to MAXBLOBS) onto the already-filtered colour.
            float3 ApplyBlobs(float3 c, float az, float el, float2 uvSrc, float eqMode)
            {
                [loop]
                for (int bi = 0; bi < _BlobCount && bi < MAXBLOBS; bi++)
                {
                    float4 bd = _BlobData[bi];
                    c = ApplyOneBlob(c, az, el, uvSrc, eqMode, bd.xy, max(bd.z, 1e-4), bd.w, _BlobFlash4[bi].x);
                }
                return c;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float3 dir = normalize(i.worldPos - _SphereCenter);
                float az = atan2(dir.x, dir.z);
                float el = asin(clamp(dir.y, -1.0, 1.0));

                // t: 0 inside focus rect -> 1 fully outside
                float dAz = max(_FocusRect.x - az, az - _FocusRect.y);
                float dEl = max(_FocusRect.z - el, el - _FocusRect.w);
                float t   = smoothstep(0.0, _SoftEdge, max(dAz, dEl));

                // Detection zones: soft ellipses. detHighlight is 1 INSIDE the object
                // (feathered); detSurround is a soft annulus just outside it that gets
                // slightly darkened — raising the object's center-surround contrast from
                // both sides (Veas et al. CHI 2011 dual modulation) without any glow artifact.
                //
                // Two separate accumulators, both driven by the same per-slot _DetectionFade
                // (smooth 0->1 ramp in C#, replacing an instant appear/disappear):
                //  - windowClear drives the vignette carve-out (t = min(t, 1-windowClear)) —
                //    this "opens a window" around the object at full strength everywhere,
                //    inside or outside the focus rect, since revealing the real object is the
                //    point regardless of region.
                //  - detHighlight/detSurround drive the extra saliency boost (colour/brightness
                //    lift + surround dim) — this is scaled down by _DetectionOutsideScale when
                //    the object sits outside the current focus window, since the window (already
                //    fading in on its own) is enough there; the boost only needs full strength
                //    to help the object stand out where there ISN'T an open window nearby.
                float detHighlight = 0.0;
                float detSurround  = 0.0;
                float windowClear  = 0.0;
                // Hard Dark: no per-detection carve-outs at all — once a selection is locked in,
                // that exact painted rect is the only thing that's ever clear; every detected
                // object elsewhere (however salient) stays fully blacked out, no exceptions.
                if (_SuppressDetectionWindows < 0.5)
                {
                    for (int _di = 0; _di < _DetectionCount; _di++)
                    {
                        float2 _ctr = float2((_DetectionRects[_di].x + _DetectionRects[_di].y) * 0.5,
                                             (_DetectionRects[_di].z + _DetectionRects[_di].w) * 0.5);
                        float2 _rad = max(float2((_DetectionRects[_di].y - _DetectionRects[_di].x) * 0.5,
                                                 (_DetectionRects[_di].w - _DetectionRects[_di].z) * 0.5),
                                          0.035);  // ~2 deg min radius so distant lights still show a visible halo
                        // Normalized elliptical distance: 1.0 at the object boundary
                        float _ed = length(float2(az - _ctr.x, el - _ctr.y) / _rad);
                        // Feather width in ellipse units, from the angular soft edge
                        float _fw = clamp(_DetectionSoftEdge / min(_rad.x, _rad.y), 0.05, 0.6);

                        float _inside = 1.0 - smoothstep(1.0 - _fw, 1.0, _ed);
                        // Wide soft annulus outside the boundary (fades out by ~2x the radius)
                        float _ann    = smoothstep(0.98, 1.1, _ed)
                                      * (1.0 - smoothstep(1.3, 2.0, _ed));

                        float _fade = _DetectionFade[_di];

                        // Is this detection's centre currently inside the active focus window?
                        float _dAzD = max(_FocusRect.x - _ctr.x, _ctr.x - _FocusRect.y);
                        float _dElD = max(_FocusRect.z - _ctr.y, _ctr.y - _FocusRect.w);
                        float _insideFocus  = step(max(_dAzD, _dElD), 0.0);
                        float _regionScale  = lerp(_DetectionOutsideScale, 1.0, _insideFocus);

                        windowClear  = max(windowClear, _inside * _fade);
                        detHighlight = max(detHighlight, _inside * _fade * _regionScale);
                        detSurround  = max(detSurround,  _ann * _fade * _regionScale);
                    }
                }
                t = min(t, 1.0 - windowClear); // clear the vignette effect on the object itself
                float surA = detSurround * _DetectionSurround;

                // ---- Source UV (computed once, used for both sharpColor and blur sampling) ----
                // Direction is computed per-fragment from worldPos - no vertex-interpolation error at poles.
                float2 uvSrc  = float2(0.5, 0.5);
                float2 uvSrcR = float2(0.5, 0.5);
                float  tEff   = t;

                if (_EqCamSampling > 0.5)
                {
                    float u_eq = frac(0.5 + az / (2.0 * UNITY_PI) + _EqUOffset);
                    float v_eq = clamp(0.5 + el / UNITY_PI + _EqVOffset, 0.0, 1.0);
                    if (_FlipY > 0.5) v_eq = 1.0 - v_eq;
                    uvSrc = uvSrcR = float2(u_eq, v_eq);
                    tEff = t;
                }
                else
                {
                    float2 uvL = CamUV(dir, _CamLFwd, _CamLRt, _CamLUp, _TanHalfFovL.xy);
                    float2 uvR = CamUV(dir, _CamRFwd, _CamRRt, _CamRUp, _TanHalfFovR.xy);
                    if (_FlipY > 0.5) { uvL.y = 1.0 - uvL.y; uvR.y = 1.0 - uvR.y; }
                    float inFovL = InFovWeight(uvL);
                    float inFovR = (_HasRightCam > 0.5) ? InFovWeight(uvR) : 0.0;
                    tEff   = max(t, 1.0 - max(inFovL, inFovR));
                    uvSrc  = clamp(uvL, 0.0, 1.0);
                    uvSrcR = clamp(uvR, 0.0, 1.0);
                }

                // Sharp source pixel - focus zone output in video mode, and base for overlay lerp.
                fixed3 sharpColor = tex2D(_MainTexL, uvSrc).rgb;

                // ---- Peripheral effect (same computation for both modes) ----
                fixed3 periColor;
                float  periAlpha;

                if (_SimpleMode > 0.5)
                {
                    periColor = fixed3(0.0, 0.0, 0.0);
                    periAlpha = t * _VignetteStrength * _MaxVignetteAlpha;
                }
                else if (_TintMode > 0.5)
                {
                    periColor = _TintColor.rgb;
                    periAlpha = t * _VignetteStrength * _MaxVignetteAlpha;
                }
                else if (_ColorPopMode > 0.5)
                {
                    // Four-level ROG hierarchy:
                    //   outside-other < outside-ROG < inside-other < inside-ROG
                    // ROG membership (PopColorKeep) gates between the "muted" and "vivid"
                    // grading tracks; the focus-window t lerps each track between its
                    // inside and outside treatment.
                    float3 col  = tex2D(_MainTexL, uvSrc).rgb;
                    float colorKeep = PopColorKeep(col, _PopWarmSatMin);

                    // SignPop (_PopDetGate = 1): the ROG keep must be confirmed by the baked
                    // detection mask — only actual traffic lights / signs get the full kept
                    // treatment. Undetected ROG (neon, ads, brake lights — or a bake miss)
                    // degrades to a partial keep (_PopDetFallback) instead of full grey, so
                    // a missed real signal is dimmed, never hidden. Gate = 0 is plain ColorPop.
                    colorKeep *= lerp(1.0, lerp(_PopDetFallback, 1.0, detHighlight), _PopDetGate);

                    float maxC = max(col.r, max(col.g, col.b));
                    float minC = min(col.r, min(col.g, col.b));
                    float sat  = (maxC > 0.001) ? (maxC - minC) / maxC : 0.0;
                    float grey = dot(col, float3(0.299, 0.587, 0.114));

                    // Headlight glare: bright + unsaturated + not a kept color.
                    // Compressed toward the knee level — a visible dim spot, never black:
                    // the oncoming vehicle stays legible, only its veiling glare goes.
                    float glareMask = smoothstep(_PopGlareKnee, 1.0, grey)
                                    * (1.0 - smoothstep(0.15, 0.40, sat))
                                    * (1.0 - colorKeep);
                    if (glareMask > 0.01 && _PopGuardRadius > 0.0001)
                    {
                        // Blown-core guard: the clipped white center of a bright signal lamp
                        // must not be dimmed. If the local surround is a kept color (the
                        // lamp's saturated fringe), exempt this pixel.
                        float poleFadeP = (_EqCamSampling > 0.5) ? cos(el) : 1.0;
                        float3 surround;
                        SampleGaussianNoisy(_MainTexL, uvSrc, _PopGuardRadius * poleFadeP,
                                            _FrostTex, float2(0.0, 0.0), 0.0,
                                            _EqCamSampling, surround);
                        glareMask *= 1.0 - PopColorKeep(surround, _PopWarmSatMin);
                    }
                    float glareStr = glareMask * lerp(_PopGlareInside, _PopGlareOutside, t);
                    col  = lerp(col, col * (_PopGlareKnee / max(grey, 1e-3)), glareStr);
                    grey = dot(col, float3(0.299, 0.587, 0.114));

                    // Selected window shows NATURAL colour (no popping); periphery is flattened
                    // (desaturated) and dimmed, with ROG kept partly visible so signals aren't
                    // lost. ROG constants below intentionally override the _Pop* uniforms; the
                    // grey-track brightness is _PopGreyDim so the study can tune periphery
                    // visibility from the inspector (the periphery must stay bright enough that
                    // a black ring probe — and real objects — remain detectable there).
                    // "Other" (non-ROG) track: natural inside -> dim flat grey outside.
                    float3 mutedTarget = grey * lerp(1.0, _PopGreyDim, t); // periphery grey brightness
                    float3 muted = lerp(col, mutedTarget, t);         // natural inside -> flat grey outside
                    // ROG track: NO boost inside (natural); desaturated + dimmed outside, no sigmoid pop.
                    float satB   = lerp(1.0, 0.55, t);                // 1.0 inside = no saturation boost
                    float bright = lerp(1.0, 0.60, t);                // 1.0 inside = no brightness lift
                    float3 vivid = saturate((col - grey) * satB + grey) * bright;

                    periColor = lerp(muted, vivid, colorKeep);
                    // Overall periphery dim so the natural window also wins on plain luminance.
                    periColor *= lerp(1.0, 0.70, t);
                    // Video mode grades the whole sphere (window included); passthrough mode
                    // stays periphery-only — the window must remain transparent there.
                    periAlpha = lerp(1.0, t, _PassthroughMode) * _VignetteStrength;
                }
                else if (_SqueezeMode > 0.5)
                {
                    // Conspicuity Squeeze (Veas et al. CHI 2011): flatten center-surround
                    // contrast toward the local mean. Chroma is compressed harder than
                    // luminance (their channel ordering: modulate opponents before lightness).
                    // Structure stays legible - the periphery just stops competing for attention.
                    float3 col = tex2D(_MainTexL, uvSrc).rgb;
                    float poleFadeSq = (_EqCamSampling > 0.5) ? cos(el) : 1.0;
                    float2 nUVsq = float2(az / (2.0 * UNITY_PI) + 0.5, el / UNITY_PI + 0.5) * _NoiseFreqScale;
                    float3 localMean;
                    SampleGaussianNoisy(_MainTexL, uvSrc, _SqueezeRadius * poleFadeSq,
                                        _FrostTex, nUVsq, 0.0, _EqCamSampling, localMean);

                    float lumC = dot(col,       float3(0.299, 0.587, 0.114));
                    float lumM = dot(localMean, float3(0.299, 0.587, 0.114));
                    float3 chrC = col       - lumC;
                    float3 chrM = localMean - lumM;
                    float  outLum = lerp(lumC, lumM, _SqueezeLum    * tEff);
                    float3 outChr = lerp(chrC, chrM, _SqueezeChroma * tEff);
                    periColor = saturate(outLum + outChr);

                    // Flatten extras: gentle global desaturation + a little dimming on top of
                    // the local contrast balancing. Goal is "nothing pops out", not darkness.
                    float sqLum = dot(periColor, float3(0.299, 0.587, 0.114));
                    periColor = lerp(periColor, sqLum.xxx, _SqueezeDesat * tEff);
                    periColor *= lerp(1.0, _SqueezeDim, tEff);

                    periAlpha = t * _VignetteStrength;
                }
                else if (_GrainMode > 0.5)
                {
                    // Granulated Periphery (Cao et al. 2021): world-locked static noise
                    // grains, density ramping with eccentricity. Suppresses detail salience
                    // while events remain visible between grains; grains double as a rest frame.
                    float2 gUV = float2(az / (2.0 * UNITY_PI) + 0.5, el / UNITY_PI + 0.5) * _GrainScale;
                    float g = tex2D(_FrostTex, gUV).r;
                    float density = lerp(_GrainDensityMin, _GrainDensityMax, t);
                    float thr = 1.0 - density;
                    float grain = smoothstep(thr - 0.04, thr + 0.04, g);
                    periColor = _GrainColor.rgb;
                    periAlpha = grain * t * _VignetteStrength;
                }
                else if (_OutlineMode > 0.5)
                {
                    // Outlined Dimming (Cheng et al. CHI 2022): near-blackout with faint
                    // monochrome luminance edges added back for spatial awareness / anxiety
                    // mitigation. Edge = |center - small 4-tap local mean|.
                    const float _er = 0.004;
                    float lumC = dot(tex2D(_MainTexL, uvSrc).rgb, float3(0.299, 0.587, 0.114));
                    float lumB = lumC;
                    lumB += dot(tex2D(_MainTexL, BlurUV(uvSrc,  _er, 0.0, _EqCamSampling)).rgb, float3(0.299, 0.587, 0.114));
                    lumB += dot(tex2D(_MainTexL, BlurUV(uvSrc, -_er, 0.0, _EqCamSampling)).rgb, float3(0.299, 0.587, 0.114));
                    lumB += dot(tex2D(_MainTexL, BlurUV(uvSrc, 0.0,  _er, _EqCamSampling)).rgb, float3(0.299, 0.587, 0.114));
                    lumB += dot(tex2D(_MainTexL, BlurUV(uvSrc, 0.0, -_er, _EqCamSampling)).rgb, float3(0.299, 0.587, 0.114));
                    lumB *= 0.2;
                    float edge = saturate(abs(lumC - lumB) * _EdgeGain);
                    periColor = edge * _EdgeBrightness;
                    periAlpha = t * _VignetteStrength * _OutlineDimAlpha;
                }
                else if (_SpotLiftMode > 0.5)
                {
                    // Spotlight Lift: soft dark periphery; the focus-window brightness lift
                    // happens in the video-mode output path below (can't brighten OS passthrough).
                    periColor = fixed3(0.0, 0.0, 0.0);
                    periAlpha = t * _VignetteStrength * _SpotDimAlpha;
                }
                else
                {
                    // Blur + desat + optional chromatic cool
                    // Blur uses a proper Gaussian kernel + perceptual noise (Tariq et al. 2022).
                    // In EQ mode, fade blur radius to 0 at the poles (cos(el)=0 there) so taps
                    // never reach the texture boundary and the top/bottom rows don't smear.
                    float poleFade  = (_EqCamSampling > 0.5) ? cos(el) : 1.0;
                    // Blur starts only after _BlurDelay so desaturation precedes it.
                    float blurSpan  = max(1.0 - _BlurDelay, 1e-4);
                    float blurTNorm = pow(saturate((tEff - _BlurDelay) / blurSpan), _BlurCurveExp);
                    float blurR     = blurTNorm * _MaxBlurRadius * poleFade;
                    float2 noiseUV = float2(az / (2.0 * UNITY_PI) + 0.5, el / UNITY_PI + 0.5) * _NoiseFreqScale;
                    float3 coarseL;
                    float3 sampledL = SampleGaussianNoisy(_MainTexL, uvSrc, blurR, _FrostTex, noiseUV, _NoiseAmp * t, _EqCamSampling, coarseL);
                    float3 camColor;
                    float3 camCoarse;
                    if (_HasRightCam > 0.5 && _EqCamSampling < 0.5)
                    {
                        float2 uvL2 = CamUV(dir, _CamLFwd, _CamLRt, _CamLUp, _TanHalfFovL.xy);
                        float2 uvR2 = CamUV(dir, _CamRFwd, _CamRRt, _CamRUp, _TanHalfFovR.xy);
                        if (_FlipY > 0.5) { uvL2.y = 1.0 - uvL2.y; uvR2.y = 1.0 - uvR2.y; }
                        float wL = InFovWeight(uvL2);
                        float wR = InFovWeight(uvR2);
                        float3 coarseR;
                        float3 sampledR = SampleGaussianNoisy(_MainTexR, uvSrcR, blurR, _FrostTex, noiseUV, _NoiseAmp * t, _EqCamSampling, coarseR);
                        bool useL = (wL >= wR);
                        camColor  = useL ? sampledL : sampledR;
                        camCoarse = useL ? coarseL  : coarseR;
                    }
                    else
                    {
                        camColor  = sampledL;
                        camCoarse = coarseL;
                    }

                    // Contrast restoration (Patney et al. 2016): peripheral blur is detected
                    // through its contrast loss, so re-amplify the blurred signal's remaining
                    // local contrast. The overshoot clamp keeps hard light/dark boundaries from
                    // marbling to pure black/white at night.
                    float3 restore = clamp((camColor - camCoarse) * (_BlurContrastRestore * blurTNorm),
                                           -0.15, 0.15);
                    camColor = saturate(camColor + restore);

                    float desatSpan = max(1.0 - _DesatDelay, 1e-4);
                    float desatT    = pow(saturate((tEff - _DesatDelay) / desatSpan), _DesatCurveExp);
                    float grey      = dot(camColor, float3(0.299, 0.587, 0.114));
                    float3 filtered = lerp(camColor, float3(grey, grey, grey), desatT);

                    if (_ChromaticCool > 0.5)
                    {
                        float3 coolTint = float3(0.70, 0.85, 1.0);
                        filtered = lerp(filtered, filtered * coolTint, tEff * _CoolStrength);
                    }

                    periColor = (fixed3)filtered;
                    periAlpha = t * _VignetteStrength;
                }

                // ---- Output ----
                // Passthrough mode: transparent focus (OS passthrough shows through), effect overlay in periphery.
                // Video mode:       opaque everywhere - sharp video in focus, effect blended into video in periphery.
                // Gentle ~1 Hz breathing on the interior boost (Waldner 2014: low-frequency,
                // low-amplitude temporal modulation attracts attention with low annoyance).
                float enh = _DetectionEnhance
                          * (1.0 - _DetectionPulseAmp * (0.5 + 0.5 * sin(_Time.y * 6.2832)));

                if (_PassthroughMode > 0.5)
                {
                    // Surround dim composites as extra dark alpha around the (transparent)
                    // object, so the real passthrough object pops against a dimmed annulus.
                    float  outA = saturate(periAlpha + surA * (1.0 - periAlpha));
                    fixed3 outC = periColor * (1.0 - surA);
                    return fixed4(outC, outA);
                }

                fixed3 result = lerp(sharpColor, periColor, saturate(periAlpha));
                if (_SpotLiftMode > 0.5)
                    result *= 1.0 + _SpotLiftAmp * (1.0 - t) * _VignetteStrength;
                // Saliency lift on the detected object: saturation + brightness boost with
                // gentle breathing, plus the darkened surround annulus (no glow ring).
                if (detHighlight > 0.0 && enh > 0.0)
                {
                    float lum     = dot(sharpColor, fixed3(0.299, 0.587, 0.114));
                    fixed3 boosted = lerp(fixed3(lum, lum, lum), sharpColor, 1.0 + _DetectionSatLift * enh);
                    boosted = saturate(boosted * (1.0 + _DetectionBrightLift * enh));
                    // Contrast expansion around mid-grey: deepens darks and lifts lights
                    // inside the window, so the object pops without just getting brighter.
                    boosted = saturate(0.5 + (boosted - 0.5) * (1.0 + _DetectionContrast * enh));
                    result  = lerp(result, boosted, detHighlight * enh);
                }
                result *= (1.0 - surA);
                result = ApplyBlobs(result, az, el, uvSrc, _EqCamSampling); // scene-pixel probes, after all filtering (Point 3)
                return fixed4(saturate(result), 1.0);
            }
            ENDCG
        }
    }
}
