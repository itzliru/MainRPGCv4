Shader "Retro/PSX_Tornado_Vortex"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RotationSpeed ("Rotation Speed", Float) = 1
        _SwirlAmount ("Swirl Strength", Float) = 1
        _WaveAmplitude ("Vertical Amplitude", Float) = 0.05
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
            float _RotationSpeed;
            float _SwirlAmount;
            float _WaveAmplitude;
            float4 _MainTex_ST;

            struct appdata
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

            // ---- Version 1: Simple counter-rotating texture ----
            v2f vert_counterUV(appdata v)
            {
                v2f o;

                // Retro grid snap
                v.vertex.xyz = SnapToGrid(v.vertex.xyz, 0.0625);

                float2 center = float2(0,0);
                float2 offset = v.vertex.xz - center;
                float angle = atan2(offset.y, offset.x);
                float radius = length(offset);

                float t = _Time.y * _RotationSpeed;

                // Vertex swirl
                angle += radius * _SwirlAmount + t;
                float2 rotated = float2(cos(angle), sin(angle)) * radius;
                v.vertex.xz = rotated + center;

                // Gentle vertical wave
                v.vertex.y += sin(radius * 10.0 - t) * _WaveAmplitude;

                o.pos = UnityObjectToClipPos(v.vertex);

                // Counter-rotating texture UVs
                float uvT = -t; // invert time for UV rotation
                float s = sin(uvT);
                float c = cos(uvT);
                float2 uvCenter = float2(0.5, 0.5);
                float2 uv = v.uv - uvCenter;
                o.uv = float2(c * uv.x - s * uv.y, s * uv.x + c * uv.y) + uvCenter;

                return o;
            }

            // ---- Version 2: Vertex swirl and texture swirl opposite ----
            v2f vert_opposite(appdata v)
            {
                v2f o;

                v.vertex.xyz = SnapToGrid(v.vertex.xyz, 0.0625);

                float2 center = float2(0,0);
                float2 offset = v.vertex.xz - center;
                float angle = atan2(offset.y, offset.x);
                float radius = length(offset);

                float t = _Time.y * _RotationSpeed;

                // Vertex swirl
                angle += radius * _SwirlAmount + t;
                float2 rotated = float2(cos(angle), sin(angle)) * radius;
                v.vertex.xz = rotated + center;

                // Vertical wave
                v.vertex.y += sin(radius * 10.0 - t) * _WaveAmplitude;

                o.pos = UnityObjectToClipPos(v.vertex);

                // Opposite texture swirl
                float uvT = -t; // opposite direction
                float s = sin(uvT);
                float c = cos(uvT);
                float2 uvCenter = float2(0.5, 0.5);
                float2 uv = v.uv - uvCenter;
                o.uv = float2(c * uv.x - s * uv.y, s * uv.x + c * uv.y) + uvCenter;

                return o;
            }

            // Select which version here:
            v2f vert(appdata v)
            {
                return vert_opposite(v); // or vert_counterUV(v);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }

            ENDCG
        }
    }
}
