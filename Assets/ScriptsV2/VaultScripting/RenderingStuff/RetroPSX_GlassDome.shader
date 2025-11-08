Shader "Retro/PSX_GlassDome"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,0.5)
        _Smoothness ("Smoothness", Range(0,1)) = 0.1
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _RefractionStrength ("Refraction Strength", Range(0,0.1)) = 0.02
        _RimColor ("Rim Color", Color) = (1,1,1,0.3)
        _RimPower ("Rim Power", Range(1,5)) = 3.0
        _PSXSnapNear ("Snap Near", Float) = 0.1
        _PSXSnapFar ("Snap Far", Float) = 0.5
        _PSXNearPlane ("Near Plane", Float) = 0.3
        _PSXFarPlane ("Far Plane", Float) = 50
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        float _Smoothness;
        float _Metallic;
        float _RefractionStrength;
        fixed4 _RimColor;
        float _RimPower;
        float _PSXSnapNear;
        float _PSXSnapFar;
        float _PSXNearPlane;
        float _PSXFarPlane;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
        };

        void vert(inout appdata_full v)
        {
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float3 viewPos = mul(UNITY_MATRIX_V, float4(worldPos, 1.0)).xyz;

            float dist = length(viewPos);
            float t = saturate((dist - _PSXNearPlane) / (_PSXFarPlane - _PSXNearPlane));
            float snapStep = lerp(_PSXSnapNear, _PSXSnapFar, t);

            // PSX jagged vertex snap
            viewPos = floor(viewPos / snapStep) * snapStep;

            v.vertex = mul(unity_WorldToObject, mul(UNITY_MATRIX_I_V, float4(viewPos, 1.0)));
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            // --- Retro jagged color bands ---
            float levels = 4.0; // number of color bands
            c.rgb = floor(c.rgb * levels) / levels;

            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha = c.a;

            // --- Rim lighting ---
            float rim = 1.0 - saturate(dot(normalize(o.Normal), normalize(IN.viewDir)));
            rim = pow(rim, _RimPower);
            o.Emission = _RimColor.rgb * rim;

            // --- Fake PSX refraction ---
            o.Normal = o.Normal + (_RefractionStrength * normalize(IN.viewDir));
        }

        ENDCG
    }

    FallBack "Transparent/Diffuse"
}
