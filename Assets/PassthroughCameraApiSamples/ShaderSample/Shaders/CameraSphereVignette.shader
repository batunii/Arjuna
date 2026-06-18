// Copyright (c) Meta Platforms, Inc. and affiliates.
//
// World-space focus window — Diminished Reality overlay.
//
// The user defines a rectangular focus window in world space (azimuth/elevation).
// Inside that window the overlay is fully transparent so the OS passthrough underlay
// shows through at full quality.  Outside the window the camera feed is progressively
// blurred and desaturated.  The window is fixed in world space — turning your head
// reveals more of the filtered periphery.
//
// Cull Front — rendered from inside the sphere.
Shader "Meta/PCA/CameraSphereVignette"
{
    Properties
    {
        [Header(Camera)]
        _MainTex ("Camera Texture", 2D) = "black" {}
        [Toggle] _FlipY ("Flip Camera Y", Float) = 0

        [Header(Focus Window)]
        // x=azMin  y=azMax  z=elMin  w=elMax  (radians, world space)
        // Default covers full sphere so everything is transparent before first selection.
        _FocusRect  ("Focus Rect (az/el radians)", Vector) = (-3.14159, 3.14159, -1.5708, 1.5708)
        _SoftEdge   ("Soft Edge Width (rad)", Float) = 0.1745

        [Header(Blur)]
        _MaxBlurRadius ("Max Blur UV Radius", Float) = 0.025
        _BlurCurveExp  ("Blur Curve Exponent", Float) = 1.2

        [Header(Desaturation)]
        _DesatDelay    ("Desat Delay (0=starts at inner edge)", Range(0, 0.9)) = 0.3
        _DesatCurveExp ("Desat Curve Exponent", Float) = 1.5

        [Header(Outside Camera FOV)]
        _DimColor    ("Dim Color", Color) = (0, 0, 0, 1)
        _DimStrength ("Dim Strength", Range(0, 1)) = 0.85
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

            sampler2D _MainTex;
            float4 _FocusRect;      // x=azMin, y=azMax, z=elMin, w=elMax (radians)
            float  _SoftEdge;
            float  _MaxBlurRadius;
            float  _BlurCurveExp;
            float  _DesatDelay;
            float  _DesatCurveExp;
            float4 _DimColor;
            float  _DimStrength;
            float  _FlipY;

            // Updated every frame from the C# manager.
            float3 _SphereCenter;
            float3 _HeadRight;
            float3 _HeadUp;
            float3 _HeadForward;
            float4 _TanHalfFov;     // x=tanHalfFovX, y=tanHalfFovY

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

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float3 dir = normalize(i.worldPos - _SphereCenter);

                // --- World-space azimuth / elevation ---
                float az = atan2(dir.x, dir.z);
                float el = asin(clamp(dir.y, -1.0, 1.0));

                // Signed distance from the focus rectangle (negative = inside).
                float dAz = max(_FocusRect.x - az, az - _FocusRect.y);
                float dEl = max(_FocusRect.z - el, el - _FocusRect.w);
                float dist = max(dAz, dEl);

                // Filter intensity: 0 inside rect → 1 fully outside.
                float t = smoothstep(0.0, _SoftEdge, dist);

                // --- Camera UV projection ---
                // Directions beyond the physical camera FOV clamp to the nearest edge
                // pixel instead of going black. Heavy blur at those edges hides the seam.
                float safeDz = max(dot(dir, _HeadForward), 0.001);
                float2 ndc   = float2(dot(dir, _HeadRight), dot(dir, _HeadUp)) / safeDz;
                float2 uv    = ndc / _TanHalfFov.xy * 0.5 + 0.5;
                if (_FlipY > 0.5) uv.y = 1.0 - uv.y;
                float2 uvC   = clamp(uv, 0.0, 1.0);

                // How far inside the physical camera FOV are we? (0 at edge, 1 well inside)
                float2 edgeDist = min(uvC, 1.0 - uvC);
                float  inFov    = saturate(min(edgeDist.x, edgeDist.y) / 0.04);

                // Effective filter intensity: ramps from focus window distance (t),
                // but is forced to 1 at and beyond the physical FOV edge so clamped
                // edge pixels are always heavily blurred — hiding the stretching.
                float tEff = max(t, 1.0 - inFov);

                // --- Blur ---
                float blurR = pow(tEff, _BlurCurveExp) * _MaxBlurRadius;

                fixed3 camSum = tex2D(_MainTex, uvC).rgb * 4.0;
                camSum += tex2D(_MainTex, clamp(uvC + float2( blurR,          0.0          ), 0, 1)).rgb;
                camSum += tex2D(_MainTex, clamp(uvC + float2(-blurR,          0.0          ), 0, 1)).rgb;
                camSum += tex2D(_MainTex, clamp(uvC + float2( 0.0,            blurR        ), 0, 1)).rgb;
                camSum += tex2D(_MainTex, clamp(uvC + float2( 0.0,           -blurR        ), 0, 1)).rgb;
                camSum += tex2D(_MainTex, clamp(uvC + float2( blurR * 0.707,  blurR * 0.707), 0, 1)).rgb;
                camSum += tex2D(_MainTex, clamp(uvC + float2(-blurR * 0.707,  blurR * 0.707), 0, 1)).rgb;
                camSum += tex2D(_MainTex, clamp(uvC + float2( blurR * 0.707, -blurR * 0.707), 0, 1)).rgb;
                camSum += tex2D(_MainTex, clamp(uvC + float2(-blurR * 0.707, -blurR * 0.707), 0, 1)).rgb;
                fixed3 blurred = camSum / 12.0;

                // --- Desaturation ---
                float desatSpan = max(1.0 - _DesatDelay, 1e-4);
                float desatT    = pow(saturate((tEff - _DesatDelay) / desatSpan), _DesatCurveExp);
                float grey      = dot(blurred, fixed3(0.299, 0.587, 0.114));
                fixed3 filtered = lerp(blurred, fixed3(grey, grey, grey), desatT);

                return fixed4(filtered, 1.0);
            }
            ENDCG
        }
    }
}
