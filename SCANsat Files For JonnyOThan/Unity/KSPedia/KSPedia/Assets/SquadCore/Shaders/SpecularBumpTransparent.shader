Shader "KSP/Bumped Specular (Transparent)"
{
	Properties 
	{
		_MainTex("_MainTex (RGB spec(A))", 2D) = "white" {}
		_BumpMap("_BumpMap", 2D) = "bump" {}
		
		_MainColor ("Main Color", Color) = (1,1,1,1)
		
		_Shininess ("Shininess", Range (0.03, 1)) = 0.078125
		
		_Opacity("_Opacity", Range(0,1) ) = 1
		_RimFalloff("_RimFalloff", Range(0.01,5) ) = 0.1
		_RimColor("_RimColor", Color) = (0,0,0,0)

		_TemperatureColor("_TemperatureColor", Color) = (0,0,0,0)
		_BurnColor ("Burn Color", Color) = (1,1,1,1)
	}
	
	SubShader 
	{
		Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
		
		Pass 
		{
			ZWrite On
			ColorMask 0
		}
		
		ZWrite On
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha 

		CGPROGRAM

		#pragma surface surf BlinnPhong
		#pragma target 2.0
		
		half _Shininess;

		sampler2D _MainTex;
		sampler2D _BumpMap;
		
		float4 _MainColor;
		float _Opacity;
		float _RimFalloff;
		float4 _RimColor;
		float4 _TemperatureColor;
		float4 _BurnColor;
		
		struct Input
		{
			float2 uv_MainTex;
			float2 uv_BumpMap;
			float2 uv_Emissive;
			float3 viewDir;
		};

		void surf (Input IN, inout SurfaceOutput o)
		{
			float4 color = tex2D(_MainTex,(IN.uv_MainTex)) * _MainColor * _BurnColor;
			float3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
			
			half rim = 1.0 - saturate(dot (normalize(IN.viewDir), normal));

			float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
			emission += _TemperatureColor.rgb * _TemperatureColor.a;

			o.Albedo = color.rgb;
			o.Emission = emission;
			o.Gloss = color.a;
			o.Specular = _Shininess;
			o.Normal = normal;
			o.Emission *= _Opacity;
			o.Alpha = _Opacity;
		}
		ENDCG
	}
	Fallback "Bumped Specular"
}