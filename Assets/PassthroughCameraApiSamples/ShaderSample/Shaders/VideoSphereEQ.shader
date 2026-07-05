// Equirectangular inside-sphere shader for video debug testing.
// Computes UV from the world-space direction vector (not mesh UV) so there
// is no seam or pole distortion regardless of sphere tesselation.
//
// Mapping:
//   U = 0.5 + atan2(x,z) / (2π)   →  right-of-viewer = right of video frame
//   V = 0.5 + asin(y)    / π       →  up = top of video frame
//
// Toggle _FlipY if the video appears upside-down on device (platform-dependent).
Shader "Meta/PCA/VideoSphereEQ"
{
    Properties
    {
        _MainTex ("Video Texture", 2D) = "black" {}
        [Toggle] _FlipY ("Flip Y", Float) = 0
        // UV offsets: shift the equirectangular sample WITHOUT rotating the sphere.
        // _VOffset -0.25 = move horizon up by 45° (e.g. if video was shot pointing down).
        _UOffset ("Horizontal Offset", Range(-0.5, 0.5)) = 0
        _VOffset ("Vertical Offset",   Range(-0.5, 0.5)) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Background" }
        Cull Front
        ZTest Always
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float     _FlipY;
            float     _UOffset;
            float     _VOffset;

            struct appdata { float4 vertex : POSITION; };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float3 worldDir : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos      = UnityObjectToClipPos(v.vertex);
                // Pass world-space direction for equirectangular UV in the fragment shader.
                // Using the object-to-world rotation (no translation needed — sphere is centred).
                o.worldDir = mul((float3x3)unity_ObjectToWorld, v.vertex.xyz);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.worldDir);

                // Azimuth: −π (left-behind) → 0 (forward +Z) → +π (right-behind)
                // For inside-sphere view, +X is to the viewer's RIGHT, which maps to u > 0.5.
                float az = atan2(dir.x, dir.z);
                float el = asin(clamp(dir.y, -1.0, 1.0));

                float u = frac(0.5 + az / (2.0 * UNITY_PI) + _UOffset);
                float v = 0.5 + el / UNITY_PI + _VOffset;

                if (_FlipY > 0.5) v = 1.0 - v;

                return tex2D(_MainTex, float2(u, v));
            }
            ENDCG
        }
    }
}
