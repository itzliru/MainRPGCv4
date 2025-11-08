Shader "Retro/Rain"
{
    Properties
    {
        _MainTex ("Rain Streak Texture", 2D) = "white" {}
        _Speed ("Scroll Speed", Float) = 1.0
        _Alpha ("Alpha", Range(0,1)) = 1.0
        _Tint ("Tint Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend One One // Additive blending
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Tint;
            float _Speed;
            float _Alpha;
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

            float _TimeValue;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                // Scroll UVs downwards
                o.uv.y -= _Time.y * _Speed;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                return tex * _Tint * _Alpha; // Apply tint and alpha
            }
            ENDCG
        }
    }
    FallBack "Unlit/Transparent"
}
