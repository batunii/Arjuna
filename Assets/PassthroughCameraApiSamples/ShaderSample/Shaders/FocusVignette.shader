// Copyright (c) Meta Platforms, Inc. and affiliates.
//
// Diminished-Reality focus overlay for Meta Quest passthrough — multi-mode.
//
// A translucent overlay on the real system passthrough (Underlay) that dims the periphery, leaving a
// clear focus region. No camera pixels are rendered in the focus zone, so passthrough quality is full.
//
//   _FocusMode = 1  DYNAMIC (Mode 1, driving): gaze-following rectangle; light grey dim periphery;
//                   intensity eased by head motion (_DrIntensity) for situational awareness.
//   _FocusMode = 2  PERCEPTUAL (Mode 2, novel): eccentricity-adaptive blur + desaturation. Two
//                   independent power-law curves: blur starts at _BlurStartAngle, desaturation at
//                   _DesatStartAngle. Camera feed sampled with a 9-tap Gaussian kernel in periphery.
//                   Contrast boost at inner blur boundary masks the transition onset.
//   _FocusMode = 3  STATIC (Mode 3, workstation): controller-defined rectangle; hard tunnel outside.
//
// _DimColor/_MaxDim set the outer-extreme appearance. Salience keeps detected objects visible.
// Renders alpha-blended inside a head-centered sphere (Cull Front).
Shader "Meta/PCA/FocusVignette"
{
    Properties
    {
        _DimColor ("Outside Color", Color) = (0, 0, 0, 1)
        _MaxDim ("Max Dim Strength", Range(0, 1)) = 1.0
        _DesatStrength ("Desaturation (0=solid colour 1=BW camera)", Range(0, 1)) = 0.0
        _FocusDir ("Focus Dir (world)", Vector) = (0, 0, 1, 0)
        _InnerAngle ("Dynamic Inner Angle (rad)", Float) = 0.35
        _OuterAngle ("Dynamic Outer Angle (rad)", Float) = 0.95
        _EdgeSoftness ("Static Edge Softness", Range(0.01, 1)) = 0.2
        _DrIntensity ("DR Intensity", Range(0, 1)) = 1
        [Toggle] _FlipY ("Camera Flip Y", Float) = 0
        _FovFeather ("Camera FOV Edge Feather", Float) = 0.05

        [Header(Perceptual Mode 2)]
        _BlurStartAngle ("Blur Start Angle (rad)", Float) = 0.175
        _BlurMaxAngle ("Blur Max Angle (rad)", Float) = 0.524
        _MaxBlurRadius ("Max Blur UV Radius", Float) = 0.015
        _BlurCurveExp ("Blur Curve Exponent", Float) = 1.5
        _DesatStartAngle ("Desat Start Angle (rad)", Float) = 0.349
        _DesatMaxAngle ("Desat Max Angle (rad)", Float) = 0.873
        _DesatCurveExp ("Desat Curve Exponent", Float) = 2.0
        _ContrastBoost ("Contrast Boost at Blur Edge", Float) = 0.15

        [Header(Camera Sphere Mode 4)]
        _SphereDesatDelay ("Desat Delay (0=starts at edge 1=never)", Range(0,1)) = 0.25

        [Header(Object Salience)]
        _SalienceFeather ("Salience Edge Feather", Float) = 0.04
        [Toggle] _SalienceFlipY ("Salience Flip Y", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 100
        Cull Front      // we are inside the sphere looking out
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha     // alpha-blend the overlay over the passthrough underlay

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            #define MAX_SALIENCE_BOXES 16

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldDir : TEXCOORD0;    // sphere center -> fragment, in world space
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _DimColor;
            float _MaxDim;
            float _DesatStrength;
            float _EdgeSoftness;
            float _DrIntensity;
            float _FlipY;

            // Mode + focus parameters.
            float _FocusMode;       // 1 = dynamic rectangle, 2 = perceptual, 3 = static rectangle
            float3 _FocusDir;       // dynamic/perceptual: gaze direction (head forward), set per-frame
            float _InnerAngle;
            float _OuterAngle;
            float3 _Corner0;        // static: two opposite corners (world directions)
            float3 _Corner1;
            float _RegionActive;    // static: 1 once both corners are placed

            // Perceptual mode (Mode 2) parameters.
            float _BlurStartAngle;
            float _BlurMaxAngle;
            float _MaxBlurRadius;
            float _BlurCurveExp;
            float _DesatStartAngle;
            float _DesatMaxAngle;
            float _DesatCurveExp;
            float _ContrastBoost;

            // Camera Sphere mode (Mode 4) parameter.
            float _SphereDesatDelay; // tunnel fraction at which desaturation begins (0–1)

            // Sphere/head center + head basis + FOV (set per-frame) — used to locate the salience boxes.
            float3 _SphereCenter;
            float3 _HeadRight;
            float3 _HeadUp;
            float3 _HeadForward;
            float4 _TanHalfFov;     // xy = tan(halfFovX), tan(halfFovY)
            float _FovFeather;

            float4 _SalienceBoxes[MAX_SALIENCE_BOXES];
            int _SalienceCount;
            float _SalienceFeather;
            float _SalienceFlipY;

            float2 ProjDir(float3 d, float3 r2, float3 u2, float3 c)
            {
                return float2(dot(d, r2), dot(d, u2)) / max(dot(d, c), 1e-3);
            }

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldDir = worldPos - _SphereCenter;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float3 dir = normalize(i.worldDir);

                // Project world dir -> camera UV (needed for all modes and salience).
                float dz = max(dot(dir, _HeadForward), 1e-3);
                float2 ndc = float2(dot(dir, _HeadRight), dot(dir, _HeadUp)) / dz;
                float2 uv = ndc / _TanHalfFov.xy * 0.5 + 0.5;
                if (_FlipY > 0.5)
                {
                    uv.y = 1.0 - uv.y;
                }
                float2 edge2 = min(uv, 1.0 - uv);
                float fovInside = saturate(min(edge2.x, edge2.y) / max(_FovFeather, 1e-4));

                float2 sUV = float2(uv.x, (_SalienceFlipY > 0.5) ? uv.y : 1.0 - uv.y);
                float salience = 0.0;
                [loop]
                for (int bi = 0; bi < _SalienceCount; ++bi)
                {
                    float2 c = _SalienceBoxes[bi].xy;
                    float2 h = _SalienceBoxes[bi].zw;
                    float2 d = abs(sUV - c) - h;
                    float m = max(d.x, d.y);
                    float inside = 1.0 - saturate(m / max(_SalienceFeather, 1e-4));
                    salience = max(salience, inside);
                }
                salience = saturate(salience) * fovInside;

                // -----------------------------------------------------------------------
                // MODE 4 — Camera-Sphere (inverted sphere technique)
                //
                // The camera feed IS the rendered surface — no separate passthrough overlay.
                // The controller-marked focus rectangle is transparent (alpha=0) so the real
                // OS passthrough shows through at full quality there. Outside the rectangle the
                // sphere surface becomes opaque and shows the camera feed increasingly blurred
                // and desaturated as eccentricity grows. Camera warp in the periphery is
                // perceptually masked by the blur itself.
                //
                //   alpha = 0  (focus zone)  → passthrough underlay visible, high quality
                //   alpha = 1  (periphery)   → camera feed, blurred + desaturated
                //
                // _BlurCurveExp / _MaxBlurRadius control the blur gradient.
                // _SphereDesatDelay controls how far into the periphery desat begins (0–1).
                // -----------------------------------------------------------------------
                if (_FocusMode > 3.5)
                {
                    // Compute tunnel from the controller-placed rectangle (same formula as Mode 3).
                    float tunnel = 0.0;
                    if (_RegionActive > 0.5)
                    {
                        float3 center = normalize(_Corner0 + _Corner1);
                        float3 worldUp = float3(0.0, 1.0, 0.0);
                        float3 up2  = normalize(worldUp - center * dot(worldUp, center));
                        float3 rgt2 = normalize(cross(up2, center));

                        float2 p = ProjDir(dir, rgt2, up2, center);
                        float2 a = ProjDir(_Corner0, rgt2, up2, center);
                        float2 b = ProjDir(_Corner1, rgt2, up2, center);
                        float2 rectCenter = (a + b) * 0.5;
                        float2 rectHalf   = abs(b - a) * 0.5;

                        float2 q = abs(p - rectCenter) - rectHalf;
                        float sdf = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0);
                        tunnel = smoothstep(-_EdgeSoftness, _EdgeSoftness, sdf);

                        if (dot(dir, center) <= 0.0) tunnel = 1.0;
                    }
                    else
                    {
                        // No rectangle placed yet — full periphery visible (no effect).
                        tunnel = 0.0;
                    }

                    tunnel *= _DrIntensity;

                    // -------------------------------------------------------------------
                    // Blur: power-law ramp from the focus edge (tunnel=0) to full periphery.
                    // -------------------------------------------------------------------
                    float blurR = pow(tunnel, _BlurCurveExp) * _MaxBlurRadius;

                    // 9-tap weighted Gaussian on camera feed. Centre 4×, 8 ring taps 1×.
                    fixed3 camSum  = tex2D(_MainTex, uv).rgb * 4.0;
                    camSum += tex2D(_MainTex, uv + float2( blurR,            0.0)).rgb;
                    camSum += tex2D(_MainTex, uv + float2(-blurR,            0.0)).rgb;
                    camSum += tex2D(_MainTex, uv + float2( 0.0,  blurR          )).rgb;
                    camSum += tex2D(_MainTex, uv + float2( 0.0, -blurR          )).rgb;
                    camSum += tex2D(_MainTex, uv + float2( blurR * 0.707,  blurR * 0.707)).rgb;
                    camSum += tex2D(_MainTex, uv + float2(-blurR * 0.707,  blurR * 0.707)).rgb;
                    camSum += tex2D(_MainTex, uv + float2( blurR * 0.707, -blurR * 0.707)).rgb;
                    camSum += tex2D(_MainTex, uv + float2(-blurR * 0.707, -blurR * 0.707)).rgb;
                    fixed3 blurredCam = camSum / 12.0;

                    // -------------------------------------------------------------------
                    // Desaturation: independent ramp, starts after _SphereDesatDelay.
                    // This mirrors the research finding that colour is less perceptually
                    // tolerable than blur at the same eccentricity, so it starts later.
                    // -------------------------------------------------------------------
                    float desatSpan = max(1.0 - _SphereDesatDelay, 1e-4);
                    float desatT = pow(saturate((tunnel - _SphereDesatDelay) / desatSpan), _DesatCurveExp);
                    float grey   = dot(blurredCam, float3(0.299, 0.587, 0.114));
                    fixed3 finalCam = lerp(blurredCam, fixed3(grey, grey, grey), desatT);

                    // Contrast boost at the transition boundary — masks the blur onset.
                    float edgeGlow = saturate(tunnel * 8.0) * saturate((1.0 - tunnel) * 4.0);
                    finalCam = saturate(finalCam + _ContrastBoost * edgeGlow);

                    // Outside camera FOV fall back to DimColor.
                    finalCam = lerp(_DimColor.rgb, finalCam, fovInside);

                    // -------------------------------------------------------------------
                    // Alpha = tunnel: focus zone is transparent (passthrough quality),
                    // periphery is opaque (camera feed blurred+desaturated).
                    // Salience keeps detected objects from being degraded.
                    // -------------------------------------------------------------------
                    float alpha = tunnel * (1.0 - salience);
                    return fixed4(finalCam, alpha);
                }

                // -----------------------------------------------------------------------
                // MODE 2 — Eccentricity-Adaptive Perceptual Vignette (novel technique)
                // Two independent power-law curves: blur from _BlurStartAngle, desat from
                // _DesatStartAngle. Camera feed is sampled with a 9-tap Gaussian kernel
                // in the periphery, giving blur+desaturation over real passthrough.
                // Research grounding: Hansen et al. 2009 (4.5x colour threshold at 50°),
                // Kergassner SIGGRAPH 2025 (σ≈5.4 arcmin at 10°, 11.3 at 20°),
                // Krajancich SIGGRAPH 2023 (task-focus suppresses peripheral sensitivity).
                // -----------------------------------------------------------------------
                if (_FocusMode > 1.5 && _FocusMode < 2.5)
                {
                    float3 gazeDir = normalize(_FocusDir);
                    float ang = acos(clamp(dot(dir, gazeDir), -1.0, 1.0));

                    // Blur: power-law ramp from BlurStartAngle to BlurMaxAngle.
                    float blurSpan = max(_BlurMaxAngle - _BlurStartAngle, 1e-4);
                    float blurT = pow(saturate((ang - _BlurStartAngle) / blurSpan), _BlurCurveExp);
                    float blurR = blurT * _MaxBlurRadius;

                    // Desaturation: independent power-law, starts later (less tolerable than blur).
                    float desatSpan = max(_DesatMaxAngle - _DesatStartAngle, 1e-4);
                    float desatT = pow(saturate((ang - _DesatStartAngle) / desatSpan), _DesatCurveExp);

                    // 9-tap circular Gaussian kernel on camera feed (center weighted 4x).
                    // Tap radius at 1.0 and 0.707 for the diagonal taps.
                    fixed3 camSum  = tex2D(_MainTex, uv).rgb * 4.0;
                    camSum += tex2D(_MainTex, uv + float2( blurR,  0.0)).rgb;
                    camSum += tex2D(_MainTex, uv + float2(-blurR,  0.0)).rgb;
                    camSum += tex2D(_MainTex, uv + float2( 0.0,  blurR)).rgb;
                    camSum += tex2D(_MainTex, uv + float2( 0.0, -blurR)).rgb;
                    camSum += tex2D(_MainTex, uv + float2( blurR * 0.707,  blurR * 0.707)).rgb;
                    camSum += tex2D(_MainTex, uv + float2(-blurR * 0.707,  blurR * 0.707)).rgb;
                    camSum += tex2D(_MainTex, uv + float2( blurR * 0.707, -blurR * 0.707)).rgb;
                    camSum += tex2D(_MainTex, uv + float2(-blurR * 0.707, -blurR * 0.707)).rgb;
                    fixed3 blurredCam = camSum / 12.0;

                    // Desaturate the blurred sample.
                    float grey = dot(blurredCam, float3(0.299, 0.587, 0.114));
                    fixed3 desatCam = lerp(blurredCam, fixed3(grey, grey, grey), desatT);

                    // Contrast boost: thin luminance lift at the inner blur onset edge masks the
                    // transition (Patney SIGGRAPH Asia 2016 — contrast restore = 2x tolerance).
                    // Peaks at blurT ≈ 0.1 (just as blur becomes visible), decays quickly.
                    float edgeGlow = saturate(blurT * 10.0) * saturate((1.0 - blurT) * 3.0);
                    desatCam = saturate(desatCam + _ContrastBoost * edgeGlow);

                    // Outside the blur max angle, blend toward DimColor for full peripheral dim.
                    float outerBlend = saturate((ang - _BlurMaxAngle) / max(_EdgeSoftness, 1e-4));
                    fixed3 peripheryColor = lerp(desatCam, _DimColor.rgb, outerBlend);

                    // Outside camera FOV fall back to DimColor.
                    peripheryColor = lerp(_DimColor.rgb, peripheryColor, fovInside);

                    // Alpha ramps in with the blur curve; salience + motion easing applied.
                    float alpha = blurT * _MaxDim * _DrIntensity * (1.0 - salience);
                    return fixed4(peripheryColor, alpha);
                }

                // -----------------------------------------------------------------------
                // 'tunnel' = how much this fragment is dimmed (0 in focus, 1 fully dimmed).
                // Mode 1 (Dynamic rectangle) and Mode 3 (Static rectangle) both use this path.
                // Mode 0 is an unused legacy cone kept for reference.
                // -----------------------------------------------------------------------
                float tunnel = 0.0;
                if (_FocusMode < 0.5)
                {
                    // LEGACY Mode 0: soft cone (unused — manager always sends mode 1 or 3).
                    float ang = acos(clamp(dot(dir, normalize(_FocusDir)), -1.0, 1.0));
                    tunnel = smoothstep(_InnerAngle, _OuterAngle, ang);
                }
                else if (_RegionActive > 0.5)
                {
                    // Mode 1 (Dynamic, corners auto-set) or Mode 3 (Static, user-placed corners).
                    float3 center = normalize(_Corner0 + _Corner1);
                    float3 worldUp = float3(0.0, 1.0, 0.0);
                    float3 up2 = normalize(worldUp - center * dot(worldUp, center));
                    float3 right2 = normalize(cross(up2, center));

                    float2 p = ProjDir(dir, right2, up2, center);
                    float2 a = ProjDir(_Corner0, right2, up2, center);
                    float2 b = ProjDir(_Corner1, right2, up2, center);
                    float2 rectCenter = (a + b) * 0.5;
                    float2 rectHalf = abs(b - a) * 0.5;

                    float2 q = abs(p - rectCenter) - rectHalf;
                    float signedDist = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0);
                    tunnel = smoothstep(-_EdgeSoftness, _EdgeSoftness, signedDist);

                    if (dot(dir, center) <= 0.0)
                    {
                        tunnel = 1.0;   // behind the window
                    }
                }

                tunnel *= _DrIntensity;
                tunnel *= (1.0 - salience);

                // Overlay colour: BW camera overlay (Mode 1) or solid _DimColor (Mode 3).
                fixed3 overlayColor = _DimColor.rgb;
                if (_DesatStrength > 0.001)
                {
                    fixed4 cam = tex2D(_MainTex, uv);
                    float grey = dot(cam.rgb, float3(0.299, 0.587, 0.114));
                    fixed3 bw = lerp(cam.rgb, fixed3(grey, grey, grey), _DesatStrength);
                    overlayColor = lerp(_DimColor.rgb, bw, fovInside);
                }

                float alpha = tunnel * _MaxDim;
                return fixed4(overlayColor, alpha);
            }
            ENDCG
        }
    }
}
