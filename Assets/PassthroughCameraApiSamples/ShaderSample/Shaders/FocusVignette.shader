// Copyright (c) Meta Platforms, Inc. and affiliates.
//
// World-locked focus vignette for the Passthrough Camera feed, with peripheral
// object salience.
//
// Base effect: a fixed world-space focus direction stays sharp; the view
// progressively blurs and desaturates with angular distance from that direction.
//
// Salience override: detected "important" objects (fed in as normalized image-space
// boxes by FocusSalienceDetector) locally shed the blur/desaturation and receive a
// subtle brightness/contrast boost, so safety-relevant things (stop signs, people,
// etc.) pop out of the dimmed periphery instead of being hidden by it.
//
// Renders on the INSIDE of a large sphere centered on the head (Cull Front).
Shader "Meta/PCA/FocusVignette"
{
    Properties
    {
        _MainTex ("Camera Texture", 2D) = "white" {}
        _FocusDir ("Focus Dir (world)", Vector) = (0, 0, 1, 0)
        _InnerAngle ("Inner Angle (radians)", Float) = 0.35
        _OuterAngle ("Outer Angle (radians)", Float) = 1.10
        _BlurRadius ("Max Blur Radius (texels)", Float) = 3.0
        _Desat ("Max Desaturation", Range(0, 1)) = 0.9
        [Toggle] _FlipY ("Flip Camera Y", Float) = 0

        [Header(Object Salience)]
        _SalienceFeather ("Salience Edge Feather", Float) = 0.04
        _HighlightBrightness ("Highlight Brightness", Float) = 1.25
        _HighlightContrast ("Highlight Contrast", Float) = 1.20
        [Toggle] _SalienceFlipY ("Salience Flip Y", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100
        Cull Front      // we are inside the sphere looking out
        ZWrite Off

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
                float4 screenPos : TEXCOORD0;
                float3 worldDir : TEXCOORD1;    // sphere center -> fragment, in world space
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            float3 _FocusDir;
            float _InnerAngle;
            float _OuterAngle;
            float _BlurRadius;
            float _Desat;
            float _FlipY;

            // World position of the sphere/head center. Set per-frame from script.
            float3 _SphereCenter;

            // Head (camera) basis + FOV, set per-frame from script. Used to project the
            // fragment's world direction into camera UV the SAME way for both eyes, so the
            // mono camera image fuses instead of producing double vision.
            float3 _HeadRight;
            float3 _HeadUp;
            float3 _HeadForward;
            float4 _TanHalfFov;     // xy = tan(halfFovX), tan(halfFovY)

            // Object salience (set from FocusSalienceDetector). Boxes are xy = center,
            // zw = half-size, in normalized image UV (v measured top-down by default).
            float4 _SalienceBoxes[MAX_SALIENCE_BOXES];
            int _SalienceCount;
            float _SalienceFeather;
            float _HighlightBrightness;
            float _HighlightContrast;
            float _SalienceFlipY;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldDir = worldPos - _SphereCenter;
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // Direction from the head to this fragment. EYE-INDEPENDENT (derived from the
                // sphere's world position, not the per-eye screen position), so sampling the
                // camera by this direction maps each real-world point to the SAME camera pixel in
                // both eyes -> the image fuses instead of doubling.
                float3 dir = normalize(i.worldDir);

                // Project the direction into camera UV via the head basis + FOV (pinhole model).
                float dz = max(dot(dir, _HeadForward), 1e-3);
                float2 ndc = float2(dot(dir, _HeadRight), dot(dir, _HeadUp)) / dz;
                float2 uv = ndc / _TanHalfFov.xy * 0.5 + 0.5;
                if (_FlipY > 0.5)
                {
                    uv.y = 1.0 - uv.y;
                }
                uv = saturate(uv);

                // Angular distance from the world-locked focus direction.
                float cosA = clamp(dot(dir, normalize(_FocusDir)), -1.0, 1.0);
                float angle = acos(cosA);
                float effect = smoothstep(_InnerAngle, _OuterAngle, angle); // 0 focus -> 1 periphery

                // Peripheral salience: how strongly this fragment sits inside a highlighted object.
                float2 sUV = float2(uv.x, (_SalienceFlipY > 0.5) ? uv.y : 1.0 - uv.y);
                float salience = 0.0;
                [loop]
                for (int bi = 0; bi < _SalienceCount; ++bi)
                {
                    float2 c = _SalienceBoxes[bi].xy;
                    float2 h = _SalienceBoxes[bi].zw;
                    float2 d = abs(sUV - c) - h;            // <= 0 inside the box
                    float m = max(d.x, d.y);
                    float inside = 1.0 - saturate(m / max(_SalienceFeather, 1e-4));
                    salience = max(salience, inside);
                }
                salience = saturate(salience);

                // Important objects shed the blur/desaturation locally.
                effect *= (1.0 - salience);

                fixed3 sharp = tex2D(_MainTex, uv).rgb;

                // Cheap variable-radius 9-tap box blur, radius scaled by the (reduced) effect.
                float r = _BlurRadius * effect;
                float2 t = _MainTex_TexelSize.xy * r;
                fixed3 blur = sharp;
                blur += tex2D(_MainTex, uv + float2(-t.x, -t.y)).rgb;
                blur += tex2D(_MainTex, uv + float2( 0.0, -t.y)).rgb;
                blur += tex2D(_MainTex, uv + float2( t.x, -t.y)).rgb;
                blur += tex2D(_MainTex, uv + float2(-t.x,  0.0)).rgb;
                blur += tex2D(_MainTex, uv + float2( t.x,  0.0)).rgb;
                blur += tex2D(_MainTex, uv + float2(-t.x,  t.y)).rgb;
                blur += tex2D(_MainTex, uv + float2( 0.0,  t.y)).rgb;
                blur += tex2D(_MainTex, uv + float2( t.x,  t.y)).rgb;
                blur /= 9.0;

                fixed3 col = lerp(sharp, blur, effect);

                // Desaturate toward luma with distance.
                float luma = dot(col, float3(0.299, 0.587, 0.114));
                col = lerp(col, luma.xxx, _Desat * effect);

                // Subtle attention boost on salient objects (brightness + contrast).
                if (salience > 0.0)
                {
                    fixed3 boosted = (col - 0.5) * _HighlightContrast + 0.5;
                    boosted *= _HighlightBrightness;
                    col = lerp(col, saturate(boosted), salience);
                }

                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
}
