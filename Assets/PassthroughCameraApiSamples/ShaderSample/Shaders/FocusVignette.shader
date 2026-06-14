// Copyright (c) Meta Platforms, Inc. and affiliates.
//
// Diminished-Reality focus overlay for Meta Quest passthrough — multi-mode.
//
// A translucent overlay on the real system passthrough (Underlay) that dims the periphery, leaving a
// clear focus region. No camera pixels are rendered, so there is no warp and full passthrough quality.
//
//   _FocusMode = 0  DYNAMIC (Mode 1, driving): clear cone follows the gaze (_FocusDir = head forward),
//                   soft falloff (_InnerAngle.._OuterAngle); light dim; intensity eased by head motion
//                   (_DrIntensity, driven from script) so the periphery clears while you scan.
//   _FocusMode = 1  STATIC (Mode 3, workstation): clear inside a user-defined axis-aligned rectangle
//                   (two opposite corners _Corner0/_Corner1); gradual signed-distance tunnel outside.
//
// _DimColor/_MaxDim set the outside look (black @ 1.0 = tunnel; grey @ ~0.4 = light dim).
// Salience: detected "important" objects (FocusSalienceDetector) stay visible through the dimming.
// Renders alpha-blended inside a head-centered sphere (Cull Front).
Shader "Meta/PCA/FocusVignette"
{
    Properties
    {
        _DimColor ("Outside Color", Color) = (0, 0, 0, 1)
        _MaxDim ("Max Dim Strength", Range(0, 1)) = 1.0
        _FocusDir ("Focus Dir (world)", Vector) = (0, 0, 1, 0)
        _InnerAngle ("Dynamic Inner Angle (rad)", Float) = 0.35
        _OuterAngle ("Dynamic Outer Angle (rad)", Float) = 0.95
        _EdgeSoftness ("Static Edge Softness", Range(0.01, 1)) = 0.2
        _DrIntensity ("DR Intensity", Range(0, 1)) = 1
        [Toggle] _FlipY ("Camera Flip Y", Float) = 0
        _FovFeather ("Camera FOV Edge Feather", Float) = 0.05

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

            fixed4 _DimColor;
            float _MaxDim;
            float _EdgeSoftness;
            float _DrIntensity;
            float _FlipY;

            // Mode + focus parameters.
            float _FocusMode;       // 0 = dynamic cone (gaze), 1 = static rectangle
            float3 _FocusDir;       // dynamic: gaze direction (head forward), set per-frame
            float _InnerAngle;
            float _OuterAngle;
            float3 _Corner0;        // static: two opposite corners (world directions)
            float3 _Corner1;
            float _RegionActive;    // static: 1 once both corners are placed

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

                // 'tunnel' = how much this fragment is dimmed (0 in focus, 1 fully dimmed).
                float tunnel = 0.0;
                if (_FocusMode < 0.5)
                {
                    // DYNAMIC: soft cone around the gaze direction.
                    float ang = acos(clamp(dot(dir, normalize(_FocusDir)), -1.0, 1.0));
                    tunnel = smoothstep(_InnerAngle, _OuterAngle, ang);
                }
                else if (_RegionActive > 0.5)
                {
                    // STATIC: axis-aligned rectangle from two corners, gravity-aligned, soft edge.
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

                tunnel *= _DrIntensity;   // motion easing (dynamic); 1 in static

                // Project world dir -> camera UV to test against YOLO detection boxes.
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
                    float2 d = abs(sUV - c) - h;            // <= 0 inside the box
                    float m = max(d.x, d.y);
                    float inside = 1.0 - saturate(m / max(_SalienceFeather, 1e-4));
                    salience = max(salience, inside);
                }
                salience = saturate(salience) * fovInside;

                // Detected objects stay visible through the dimming.
                tunnel *= (1.0 - salience);

                float alpha = tunnel * _MaxDim;
                return fixed4(_DimColor.rgb, alpha);
            }
            ENDCG
        }
    }
}
