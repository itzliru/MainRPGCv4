Shader "Retro/PSX_WaterSurface"
{
    Properties
    {
        _Color ("Water Tint", Color) = (0.2, 0.5, 1, 1)
        _MainTex ("Water Texture", 2D) = "white" {}
        _WaveStrength ("Wave Height", Range(0, 0.25)) = 0.05
        _WaveSpeed ("Wave Speed", Range(0, 10)) = 1.5
        _DistortionScale ("UV Distortion", Range(0, 0.3)) = 0.05
        _DistortionSpeed ("Distortion Speed", Range(0, 2)) = 0.4
        _VertexSnap ("PSX Vertex Snap Size", Range(0, 0.05)) = 0.015
        _Glossiness ("Shininess", Range(0, 1)) = 0.2
        _Metallic ("Metallic", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        float _WaveStrength;
        float _WaveSpeed;
        float _DistortionScale;
        float _DistortionSpeed;
        float _VertexSnap;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
        };

        float3 SnapToGrid(float3 pos, float snapValue)
        {
            return floor(pos / snapValue + 0.5) * snapValue;
        }

        void vert(inout appdata_full v)
        {
            // Vertex snapping for PS1 jitter effect
            v.vertex.xyz = SnapToGrid(v.vertex.xyz, _VertexSnap);

            // Wave animation
            float t = _Time.y * _WaveSpeed;
            float wave = (sin(v.vertex.x * 2.0 + t) + cos(v.vertex.z * 1.5 + t)) * _WaveStrength;
            v.vertex.y += wave;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Distort UV for fake water motion
            float2 uv = IN.uv_MainTex;
            float t = _Time.y * _DistortionSpeed;
            uv.x += sin(uv.y * 10 + t) * _DistortionScale;
            uv.y += cos(uv.x * 10 + t) * _DistortionScale;

            fixed4 c = tex2D(_MainTex, uv) * _Color;

            o.Albedo = c.rgb;
            o.Smoothness = _Glossiness;
            o.Metallic = _Metallic;
            o.Alpha = c.a;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
