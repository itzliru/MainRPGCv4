Shader "Retro/ScreenSpaceRain"
{
    Properties
    {
        _MainTex ("Rain Streak Texture", 2D) = "white" {}
        _ScrollSpeed ("Scroll Speed", Vector) = (0.5, -1.0, 0, 0) // X = horizontal, Y = vertical
        _Alpha ("Alpha", Range(0,1)) = 0.5
        _Tint ("Tint Color", Color) = (1,1,1,1)
        _Tiling ("Tiling", Float) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _ScrollSpeed;
            float _Alpha;
            float4 _Tint;
            float _Tiling;
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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * _Tiling;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Scroll UV diagonally
                float2 uv = i.uv;
                uv += _ScrollSpeed.xy * _Time.y; // diagonal motion
                uv = frac(uv); // wrap around

                fixed4 tex = tex2D(_MainTex, uv);
                return tex * _Tint * _Alpha;
            }
            ENDCG
        }
    }
    FallBack "Unlit/Transparent"
}
