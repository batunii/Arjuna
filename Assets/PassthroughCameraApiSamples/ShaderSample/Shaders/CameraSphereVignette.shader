// Copyright (c) Meta Platforms, Inc. and affiliates.
//
// Dual-camera world-space focus window — Diminished Reality overlay.
//
// Left and right passthrough cameras are projected onto the inside of the sphere.
// Each fragment samples from whichever camera(s) cover it, blended by coverage weight.
// Outside both cameras the edge pixels are stretched and maximally blurred.
//
// Cull Front — rendered from inside the sphere.
Shader "Meta/PCA/CameraSphereVignette"
{
    Properties
    {
        [Header(Camera)]
        _MainTexL ("Camera Texture (Left)",  2D) = "black" {}
        _MainTexR ("Camera Texture (Right)", 2D) = "black" {}
        [Toggle] _FlipY ("Flip Camera Y", Float) = 0

        [Header(Focus Window)]
        // x=azMin  y=azMax  z=elMin  w=elMax  (radians, world space)
        _FocusRect  ("Focus Rect (az/el radians)", Vector) = (-3.14159, 3.14159, -1.5708, 1.5708)
        _SoftEdge   ("Soft Edge Width (rad)", Float) = 0.1745

        [Header(Debug)]
        [Toggle] _DebugCamOverlay ("Debug Overlay (Y=Left green  X=Right red)", Float) = 0

        [Header(Blur)]
        _MaxBlurRadius ("Max Blur UV Radius", Float) = 0.025
        _BlurCurveExp  ("Blur Curve Exponent", Float) = 1.2

        [Header(Desaturation)]
        _DesatDelay    ("Desat Delay (0=starts at inner edge)", Range(0, 0.9)) = 0.3
        _DesatCurveExp ("Desat Curve Exponent", Float) = 1.5
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
            float  _HasRightCam;       // 1 when right camera is active
            float  _DebugCamOverlay;   // 1 = draw Y on left / X on right

            // Left camera projection (updated every frame from C#)
            float3 _CamLFwd;
            float3 _CamLRt;
            float3 _CamLUp;
            float4 _TanHalfFovL;   // x=tanHalfFovX  y=tanHalfFovY

            // Right camera projection
            float3 _CamRFwd;
            float3 _CamRRt;
            float3 _CamRUp;
            float4 _TanHalfFovR;

            // YOLO detection zones (disabled when _DetectionCount == 0)
            int    _DetectionCount;
            float4 _DetectionRects[8];

            float3 _SphereCenter;

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

            // Project direction onto a camera, return UV (may be outside [0,1])
            float2 CamUV(float3 dir, float3 fwd, float3 rt, float3 up, float2 tanHFov)
            {
                float dz   = max(dot(dir, fwd), 0.001);
                float2 ndc = float2(dot(dir, rt), dot(dir, up)) / dz;
                return ndc / tanHFov * 0.5 + 0.5;
            }

            // How far inside a camera's FOV is this UV? (0=at edge, 1=well inside, negative=outside)
            float InFovWeight(float2 uv)
            {
                float2 ed = min(uv, 1.0 - uv);
                return saturate(min(ed.x, ed.y) / 0.04);
            }

            // 9-tap box blur with clamped UVs
            fixed3 SampleBlurred(sampler2D tex, float2 uvC, float r)
            {
                fixed3 s = tex2D(tex, uvC).rgb * 4.0;
                s += tex2D(tex, clamp(uvC + float2( r,       0.0     ), 0, 1)).rgb;
                s += tex2D(tex, clamp(uvC + float2(-r,       0.0     ), 0, 1)).rgb;
                s += tex2D(tex, clamp(uvC + float2( 0.0,     r       ), 0, 1)).rgb;
                s += tex2D(tex, clamp(uvC + float2( 0.0,    -r       ), 0, 1)).rgb;
                s += tex2D(tex, clamp(uvC + float2( r*0.707, r*0.707 ), 0, 1)).rgb;
                s += tex2D(tex, clamp(uvC + float2(-r*0.707, r*0.707 ), 0, 1)).rgb;
                s += tex2D(tex, clamp(uvC + float2( r*0.707,-r*0.707 ), 0, 1)).rgb;
                s += tex2D(tex, clamp(uvC + float2(-r*0.707,-r*0.707 ), 0, 1)).rgb;
                return s / 12.0;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float3 dir = normalize(i.worldPos - _SphereCenter);

                // --- World-space azimuth / elevation ---
                float az = atan2(dir.x, dir.z);
                float el = asin(clamp(dir.y, -1.0, 1.0));

                // Filter intensity: 0 inside rect → 1 fully outside
                float dAz = max(_FocusRect.x - az, az - _FocusRect.y);
                float dEl = max(_FocusRect.z - el, el - _FocusRect.w);
                float t   = smoothstep(0.0, _SoftEdge, max(dAz, dEl));

                // YOLO detection zones
                for (int _di = 0; _di < _DetectionCount; _di++)
                {
                    float _dAz = max(_DetectionRects[_di].x - az, az - _DetectionRects[_di].y);
                    float _dEl = max(_DetectionRects[_di].z - el, el - _DetectionRects[_di].w);
                    if (max(_dAz, _dEl) < 0.0) { t = 0.0; break; }
                }

                // --- Per-camera UV projection ---
                float2 uvL = CamUV(dir, _CamLFwd, _CamLRt, _CamLUp, _TanHalfFovL.xy);
                float2 uvR = CamUV(dir, _CamRFwd, _CamRRt, _CamRUp, _TanHalfFovR.xy);
                if (_FlipY > 0.5) { uvL.y = 1.0 - uvL.y; uvR.y = 1.0 - uvR.y; }

                float2 uvCL = clamp(uvL, 0.0, 1.0);
                float2 uvCR = clamp(uvR, 0.0, 1.0);

                float inFovL = InFovWeight(uvL);
                float inFovR = (_HasRightCam > 0.5) ? InFovWeight(uvR) : 0.0;
                float inFov  = max(inFovL, inFovR);

                // Edge pixels always get max blur to hide stretching
                float tEff  = max(t, 1.0 - inFov);
                float blurR = pow(tEff, _BlurCurveExp) * _MaxBlurRadius;

                // --- Sample and blend cameras ---
                fixed3 sampledL = SampleBlurred(_MainTexL, uvCL, blurR);
                fixed3 camColor;
                if (_HasRightCam > 0.5)
                {
                    fixed3 sampledR = SampleBlurred(_MainTexR, uvCR, blurR);
                    // Hard split: each fragment comes from whichever camera has it
                    // more centred in its FOV. No blending = no ghosting.
                    camColor = (inFovL >= inFovR) ? sampledL : sampledR;
                }
                else
                {
                    camColor = sampledL;
                }


                // --- Desaturation ---
                float desatSpan = max(1.0 - _DesatDelay, 1e-4);
                float desatT    = pow(saturate((tEff - _DesatDelay) / desatSpan), _DesatCurveExp);
                float grey      = dot(camColor, fixed3(0.299, 0.587, 0.114));
                fixed3 filtered = lerp(camColor, fixed3(grey, grey, grey), desatT);

                // alpha=0 in focus window → OS passthrough (full ~110° FOV) shows through
                // alpha=1 at periphery  → opaque blurred/desaturated overlay
                return fixed4(filtered, t);
            }
            ENDCG
        }
    }
}
