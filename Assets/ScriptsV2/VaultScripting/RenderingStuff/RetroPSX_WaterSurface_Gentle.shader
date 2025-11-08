Shader "Retro/PSX_WaterSurface_Gentle"
{
    Properties
    {
        _Color ("Water Tint", Color) = (0.25, 0.55, 1, 1)
        _MainTex ("Water Texture", 2D) = "white" {}
        _WaveAmplitude ("Wave Height", Range(0, 0.15)) = 0.02
        _WaveFrequency ("Wave Frequency", Range(0.1, 2)) = 0.6
        _WaveSpeed ("Wave Speed", Range(0, 3)) = 0.5
        _DistortionScale ("UV Distortion", Range(0, 0.2)) = 0.02
        _DistortionSpeed ("Distortion Speed", Range(0, 1)) = 0.2
        _VertexSnap ("PSX Vertex Snap Size", Range(0, 0.05)) = 0.01
        _Glossiness ("Shininess", Range(0, 1)) = 0.3
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

        float _WaveAmplitude;
        float _WaveFrequency;
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
    // PS1 vertex snap
    v.vertex.xyz = SnapToGrid(v.vertex.xyz, _VertexSnap);

    // Gentle large-wave motion (smaller vertical movement)
    float t = _Time.y * _WaveSpeed;

    // Reduce Y movement by half
    float wave =
        sin(v.vertex.x * _WaveFrequency + t) * (_WaveAmplitude * 0.1) +
        cos(v.vertex.z * _WaveFrequency * 1.3 + t * 0.8) * (_WaveAmplitude * 0.05);
    v.vertex.y += sin(v.vertex.x * _WaveFrequency * 0.5 + t * 0.5) * (_WaveAmplitude * 0.02);
    v.vertex.y += cos(v.vertex.z * _WaveFrequency * 0.7 + t * 0.3) * (_WaveAmplitude * 0.02);

    v.vertex.y += wave;
}

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 uv = IN.uv_MainTex;
            float t = _Time.y * _DistortionSpeed;

            // Gentle shimmer movement
            uv.x += sin(uv.y * 4 + t) * _DistortionScale;
            uv.y += cos(uv.x * 4 + t * 1.1) * _DistortionScale;

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
