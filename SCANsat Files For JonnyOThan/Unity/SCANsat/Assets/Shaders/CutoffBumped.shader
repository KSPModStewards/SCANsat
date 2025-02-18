Shader "KSP/Alpha/Cutoff Bumped"
{
    Properties{
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
        _BumpMap("Normalmap", 2D) = "bump" {}
        _Cutoff("Alpha cutoff", Range(0,1)) = 0.5
        _RimFalloff("_RimFalloff", Range(0.01,5)) = 0.1
        _RimColor("_RimColor", Color) = (0,0,0,0)

        _TemperatureColor("_TemperatureColor", Color) = (0,0,0,0)
        _BurnColor("Burn Color", Color) = (1,1,1,1)
    }

        SubShader{
        Tags{ "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Lambert alphatest:_Cutoff

        sampler2D _MainTex;
        sampler2D _BumpMap;
        fixed4 _Color;
        float _RimFalloff;
        float4 _RimColor;
        float4 _TemperatureColor;
        float4 _BurnColor;

        struct Input {
        float2 uv_MainTex;
        float2 uv_BumpMap;
        float2 uv_Emissive;
        float3 viewDir;
    };

    void surf(Input IN, inout SurfaceOutput o) 
    {
        fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color * _BurnColor;
        float3 normal = float3(0, 0, 1);
        half rim = 1.0 - saturate(dot(normalize(IN.viewDir), normal));
        float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
        emission += _TemperatureColor.rgb * _TemperatureColor.a;
        o.Albedo = c.rgb;
        o.Emission = emission;
        o.Alpha = c.a;
        o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
    }
    ENDCG
    }

        FallBack "Legacy Shaders/Transparent/Cutout/Diffuse"
}