Shader "Retro/PSX_VertexSnap_Surface_Safe"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        // PSX Snap Controls
        _PSXSnapNear ("Snap Near", Range(0.0001, 0.1)) = 0.01
        _PSXSnapFar ("Snap Far", Range(0.0001, 0.5)) = 0.05
        _PSXNearPlane ("Near Plane", Float) = 0.1
        _PSXFarPlane ("Far Plane", Float) = 100.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;

        float _PSXSnapNear;
        float _PSXSnapFar;
        float _PSXNearPlane;
        float _PSXFarPlane;

        struct Input
        {
            float2 uv_MainTex;
        };

        void vert(inout appdata_full v)
        {
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float3 viewPos = mul(UNITY_MATRIX_V, float4(worldPos, 1.0)).xyz;

            float dist = length(viewPos);

            // Ensure safe, nonzero range
            float nearPlane = max(_PSXNearPlane, 0.001);
            float farPlane = max(_PSXFarPlane, nearPlane + 0.001);

            float t = saturate((dist - nearPlane) / (farPlane - nearPlane));
            float snapStep = lerp(_PSXSnapNear, _PSXSnapFar, t);

            // Avoid divide-by-zero
            snapStep = max(snapStep, 0.0001);

            viewPos = floor(viewPos / snapStep) * snapStep;

            v.vertex = mul(unity_WorldToObject, mul(UNITY_MATRIX_I_V, float4(viewPos, 1.0)));
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = 0.0;
            o.Smoothness = 0.2;
            o.Alpha = c.a;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
