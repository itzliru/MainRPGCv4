Shader "Retro/PSX_Tornado_Surface"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TornadoHeight ("Height", Float) = 5
        _BaseRadius ("Base Radius", Float) = 1
        _TipRadius ("Tip Radius", Float) = 0.2
        _RotationSpeed ("Rotation Speed", Float) = 2
        _SwirlAmount ("Swirl Strength", Float) = 0.5
        _WaveAmplitude ("Vertical Wave", Float) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _TornadoHeight;
            float _BaseRadius;
            float _TipRadius;
            float _RotationSpeed;
            float _SwirlAmount;
            float _WaveAmplitude;
            float4 _MainTex_ST;

            struct appdata_vortex
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float3 SnapToGrid(float3 pos, float grid)
            {
                return round(pos / grid) * grid;
            }

            v2f vert(appdata_vortex v)
            {
                v2f o;

                // Snap to PSX grid
                v.vertex.xyz = SnapToGrid(v.vertex.xyz, 0.0625);

                // Height factor (0 = base, 1 = tip)
                float hFactor = saturate(v.vertex.y / _TornadoHeight);

                // Shrinking radius
                float radius = lerp(_BaseRadius, _TipRadius, hFactor);

                // Current position in XZ plane
                float2 offset = float2(v.vertex.x, v.vertex.z);
                float angle = atan2(offset.y, offset.x);

                // Add swirl
                float t = _Time.y * _RotationSpeed;
                angle += radius * _SwirlAmount + t;

                // Apply new XZ position
                float2 rotated = float2(cos(angle), sin(angle)) * radius;
                v.vertex.xz = rotated;

                // Gentle vertical wave
                v.vertex.y += sin(radius * 10.0 - t) * _WaveAmplitude;

                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
