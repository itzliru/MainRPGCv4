Shader "Retro/MuzzleFlash_Unlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _Emission ("Emission Strength", Range(0,10)) = 2.0
        _VertexJitter ("Vertex Jitter", Range(0,0.01)) = 0.001
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One One // additive blending for glow look

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float _Emission;
            float _VertexJitter;
            float4 _MainTex_ST;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float randomNoise(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898,78.233))) * 43758.5453);
            }

            v2f vert (appdata v)
            {
                v2f o;
                // 🔹 Add PSX vertex jitter (subpixel wobble)
                float n = randomNoise(v.vertex.xy) * _VertexJitter;
                v.vertex.xz += n;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                fixed4 col = tex * _Color * _Emission;
                col.a = tex.a;
                return col;
            }
            ENDCG
        }
    }

    FallBack Off
}
