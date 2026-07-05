// Copyright (c) Meta Platforms, Inc. and affiliates.
//
// Simple inside-sphere background shader for the video debug mode.
// Cull Front so the sphere renders from inside. Renders in the Background
// queue (before the transparent vignette sphere) so the vignette's alpha=0
// focus window shows through to this video content.
Shader "Meta/PCA/VideoSphere"
{
    Properties
    {
        _MainTex ("Video Texture", 2D) = "black" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Background" "Queue" = "Background" }
        Cull Front
        ZWrite Off
        ZTest Always

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
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Flip U to correct the horizontal mirror from Cull Front
                o.uv = float2(1.0 - v.uv.x, v.uv.y);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
