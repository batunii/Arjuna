// Copyright (c) Meta Platforms, Inc. and affiliates.
//
// Multi-mode Diminished Reality focus-window overlay.
//
// Mode 1 (Blur): dual-camera passthrough texture, blurred + desaturated periphery.
// Mode 2 (SoftDark): pure dark overlay, periphery never fully opaque (~75% max).
// Mode 3 (HardDark): pure dark overlay, periphery goes fully black.
//
// Focus window is always transparent so OS passthrough shows through at full FOV.
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
        _FocusRect  ("Focus Rect (az/el radians)", Vector) = (-3.14159, 3.14159, -1.5708, 1.5708)
        _SoftEdge   ("Soft Edge Width (rad)", Float) = 0.1745

        [Header(Mode and Formation)]
        // 0 = Mode 1 (blur+desat camera), 1 = Mode 2/3 (pure dark overlay)
        [Toggle] _SimpleMode      ("Simple Dark Mode (Mode 2 or 3)", Float) = 0
        // Animated 0→1 by C# over formation time
        _VignetteStrength ("Vignette Strength", Range(0, 1)) = 1
        // 1.0 for Mode 3 (full black), <1 for Mode 2 (soft dim)
        _MaxVignetteAlpha ("Max Vignette Alpha", Range(0, 1)) = 1

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

                float az = atan2(dir.x, dir.z);
                float el = asin(clamp(dir.y, -1.0, 1.0));

                // t: 0 inside focus rect → 1 fully outside
                float dAz = max(_FocusRect.x - az, az - _FocusRect.y);
                float dEl = max(_FocusRect.z - el, el - _FocusRect.w);
                float t   = smoothstep(0.0, _SoftEdge, max(dAz, dEl));

                for (int _di = 0; _di < _DetectionCount; _di++)
                {
                    float _dAz = max(_DetectionRects[_di].x - az, az - _DetectionRects[_di].y);
                    float _dEl = max(_DetectionRects[_di].z - el, el - _DetectionRects[_di].w);
                    if (max(_dAz, _dEl) < 0.0) { t = 0.0; break; }
                }

                // ---- Mode 2 / Mode 3: pure dark overlay, no camera sampling ----
                // Focus window transparent → OS passthrough at full FOV shows through.
                // _VignetteStrength animated 0→1 by C# over formation time.
                // _MaxVignetteAlpha: 1.0 = full black (Mode 3), ~0.75 = soft dim (Mode 2).
                if (_SimpleMode > 0.5)
                    return fixed4(0.0, 0.0, 0.0, t * _VignetteStrength * _MaxVignetteAlpha);

                // ---- Mode 1: blurred + desaturated camera overlay ----
                float2 uvL = CamUV(dir, _CamLFwd, _CamLRt, _CamLUp, _TanHalfFovL.xy);
                float2 uvR = CamUV(dir, _CamRFwd, _CamRRt, _CamRUp, _TanHalfFovR.xy);
                if (_FlipY > 0.5) { uvL.y = 1.0 - uvL.y; uvR.y = 1.0 - uvR.y; }

                float2 uvCL = clamp(uvL, 0.0, 1.0);
                float2 uvCR = clamp(uvR, 0.0, 1.0);

                float inFovL = InFovWeight(uvL);
                float inFovR = (_HasRightCam > 0.5) ? InFovWeight(uvR) : 0.0;
                float inFov  = max(inFovL, inFovR);

                float tEff  = max(t, 1.0 - inFov);
                float blurR = pow(tEff, _BlurCurveExp) * _MaxBlurRadius;

                fixed3 sampledL = SampleBlurred(_MainTexL, uvCL, blurR);
                fixed3 camColor;
                if (_HasRightCam > 0.5)
                {
                    fixed3 sampledR = SampleBlurred(_MainTexR, uvCR, blurR);
                    camColor = (inFovL >= inFovR) ? sampledL : sampledR;
                }
                else
                {
                    camColor = sampledL;
                }

                float desatSpan = max(1.0 - _DesatDelay, 1e-4);
                float desatT    = pow(saturate((tEff - _DesatDelay) / desatSpan), _DesatCurveExp);
                float grey      = dot(camColor, fixed3(0.299, 0.587, 0.114));
                fixed3 filtered = lerp(camColor, fixed3(grey, grey, grey), desatT);

                return fixed4(filtered, t * _VignetteStrength);
            }
            ENDCG
        }
    }
}
