// Copyright (c) Meta Platforms, Inc. and affiliates.
//
// Reprojects a flat (pinhole/perspective) video frame into the forward sector of an
// equirectangular render target, so the existing video-sphere pipeline (vignette
// shader, az/el focus window, baked-detection mapping) works unchanged with flat
// dashcam footage (e.g. LISA traffic-light clips). Drawn as a viewport quad over the
// sector's az/el bounding box by VideoTestSceneManager.CompositeFlatToEquirect().
//
// Lives in Resources/ so Resources.Load guarantees inclusion in Android builds
// (Shader.Find returns null for stripped shaders on Quest).
Shader "Meta/PCA/FlatClipToEquirect"
{
    Properties
    {
        _MainTex  ("Flat Clip", 2D) = "black" {}
        // tan of the clip camera's half FOV (x = horizontal, y = vertical)
        _TanHalf  ("Tan Half FOV", Vector) = (0.5774, 0.4330, 0, 0)
        // half extent of the sector's az/el bounding box in radians
        _AzElHalf ("Sector Half Extent (rad)", Vector) = (0.5236, 0.4189, 0, 0)
        _Surround ("Surround Color", Color) = (0.05, 0.05, 0.06, 1)
        [Toggle] _FlipY ("Flip Clip Y", Float) = 0
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _TanHalf;
            float4 _AzElHalf;
            float4 _Surround;
            float  _FlipY;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata_img v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Quad uv spans the sector's az/el bounding box, centred on the clip axis.
                float azL = lerp(-_AzElHalf.x, _AzElHalf.x, i.uv.x);
                float elL = lerp(-_AzElHalf.y, _AzElHalf.y, i.uv.y);

                // Pinhole projection of the direction: x/z = tan(az), y/z = tan(el)/cos(az).
                float2 uvF;
                uvF.x = tan(azL) / _TanHalf.x * 0.5 + 0.5;
                uvF.y = tan(elL) / max(cos(azL), 1e-4) / _TanHalf.y * 0.5 + 0.5;

                // The image of the rectangular frame is curved in equirect space; the
                // bounding-box corners fall outside it — fill those with the surround.
                if (uvF.x < 0.0 || uvF.x > 1.0 || uvF.y < 0.0 || uvF.y > 1.0)
                    return _Surround;

                if (_FlipY > 0.5) uvF.y = 1.0 - uvF.y;
                return fixed4(tex2D(_MainTex, uvF).rgb, 1.0);
            }
            ENDCG
        }
    }
}
