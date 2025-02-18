Shader "KSP/Alpha/Unlit Transparent"
{
	Properties 
	{
		_MainTex("MainTex (RGB Alpha(A))", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)
		
		_Opacity("_Opacity", Range(0,1) ) = 1
		_Fresnel("Fresnel", Range(0,10)) = 0
		[MaterialToggle] _FresnelInvert("Invert Fresnel", Float) = 0

		_RimFalloff("_RimFalloff", Range(0.01,5) ) = 0.1
		_RimColor("_RimColor", Color) = (0,0,0,0)

		_TemperatureColor("_TemperatureColor", Color) = (0,0,0,0)
		_BurnColor ("Burn Color", Color) = (1,1,1,1)
	}
	
	SubShader 
	{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }

		Pass
		{
			ZWrite On
			ColorMask 0
		}

		ZWrite On
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha 

		CGPROGRAM

        #include "LightingKSP.cginc"
		#pragma surface surf Unlit noforwardadd noshadow noambient novertexlights alpha:fade
		#pragma target 3.0

		sampler2D _MainTex;

		float _Opacity;
		float _Fresnel;
		float _FresnelInvert;
		float _RimFalloff;
		float4 _RimColor;
		float4 _TemperatureColor;
		float4 _BurnColor;
		


        struct Input
		{
            float2 uv_MainTex;
			float3 viewDir;
        };

		void surf (Input IN, inout SurfaceOutput o)
		{
			float4 color = tex2D(_MainTex, (IN.uv_MainTex)) * _BurnColor;
			float alpha = _Color.a * color.a;
			float3 normal = float3(0,0,1);

			half rim = 1.0 - saturate(dot (normalize(IN.viewDir), normal));
			float3 fresnel = (_FresnelInvert + (pow(1 - rim, _Fresnel))) * (1 - _FresnelInvert + 1 - (pow(1 - rim, _Fresnel)));

			float3 emission = (_RimColor.rgb * pow(rim, _RimFalloff)) * _RimColor.a;
			emission += _TemperatureColor.rgb * _TemperatureColor.a;

			o.Albedo = _Color.rgb * color.rgb + emission;
			//o.Emission = emission * _Opacity;
			o.Normal = normal;
			o.Alpha = _Color.a * color.a * _Opacity * fresnel;
		}
		ENDCG
	}
	Fallback "Standard"
}