Shader "Retro/PSX_CameraPostProcess_FogAndVertex_Fixed"
{
    Properties
    {
        _MainTex("MainTex", 2D) = "white" {}
        _Pixelation("Pixelation (pixels)", Float) = 2
        _WobbleAmount("Wobble Amount (texels)", Float) = 0.5
        _EnableWobble("Enable Wobble", Float) = 1
        _FogColor("Fog Color", Color) = (0.5,0.5,0.5,1)
        _FogDensity("Fog Density", Float) = 0.02
        _EnableDarken("Enable Darken", Float) = 1
        _DarkenStrength("Darken Strength", Float) = 0.3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" "DisableBatching"="True" }
        Pass
        {
            ZTest Always Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Pixelation;
            float _WobbleAmount;
            float _EnableWobble;
            float4 _MainTex_TexelSize; // x = 1/width, y = 1/height
            float4 _FogColor;
            float _FogDensity;
            float _EnableDarken;
            float _DarkenStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float depth : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                // NOTE: this depth is clip-space z/w (not linear). If you want accurate scene fog,
                // sample _CameraDepthTexture in the fragment and linearize it.
                o.depth = saturate(o.pos.z / o.pos.w);
                return o;
            }

            // stable pseudo-random based on integer pixel coords + time
            static float rand(float2 co)
            {
                return frac(sin(dot(co, float2(12.9898,78.233))) * 43758.5453);
            }

            // produce a small wobble in texel units using integer pixel coords as seed
            float2 PixelWobble(float2 pixelCoord, float2 texelSize)
            {
                float seed = rand(pixelCoord);
                float seed2 = rand(pixelCoord + 13.13);
                // wobble amount is in texels (converted to UV by multiplying texelSize)
                float jw = (_WobbleAmount);
                float jitterX = (seed - 0.5) * jw;
                float jitterY = (seed2 - 0.5) * jw;
                return float2(jitterX * texelSize.x, jitterY * texelSize.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Ensure pixelation is at least 1 (prevents divide-by-zero)
                float pixelation = max(1.0, _Pixelation);

                // Convert normalized UV to screen pixel coordinates
                float2 texSize = float2(1.0 / _MainTex_TexelSize.x, 1.0 / _MainTex_TexelSize.y); // width, height
                float2 pixelCoord = i.uv * texSize; // pixel coords

                // Group pixels into blocks of size "pixelation" (snap to block)
                float2 blockCoord = floor(pixelCoord / pixelation);

                // Center of the chosen block in pixel coords (use +0.5 to sample center)
                float2 blockCenterPixels = (blockCoord * pixelation) + (pixelation * 0.5);

                // Optional wobble (in texel units). Use integer blockCoord as stable seed.
                float2 wobbleUV = float2(0,0);
                if (_EnableWobble > 0.5)
                {
                    wobbleUV = PixelWobble(blockCoord, _MainTex_TexelSize); // returns UV offset
                }

                // Final sample UV: block center (in pixels) -> normalized UV, plus wobble
                float2 finalUV = (blockCenterPixels * _MainTex_TexelSize.xy) + wobbleUV;

                // Clamp to avoid sampling outside (prevents wrap/repeat artifacts)
                finalUV = saturate(finalUV);

                fixed4 col = tex2D(_MainTex, finalUV);

                // Color quantization (PS1 5-bit)
                col.rgb = floor(col.rgb * 31.0) / 31.0;

                // Fog (optional) — using the vertex depth (cheap & not perfectly accurate)
                // If you want accurate scene fog, set camera.depthTextureMode = Depth and sample _CameraDepthTexture here.
                if (_FogDensity > 0.0)
                {
                    // keep factor small to avoid overdrive; tweak multiplier as needed
                    float fogFactor = saturate(1.0 - exp(-i.depth * _FogDensity * 10.0));
                    col.rgb = lerp(col.rgb, _FogColor.rgb, fogFactor);
                }

                // Distance darken toggle
                if (_EnableDarken > 0.5)
                {
                    float darkness = 1.0 - (i.depth * _DarkenStrength);
                    col.rgb *= saturate(darkness);
                }

                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
