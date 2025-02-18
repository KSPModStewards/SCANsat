Shader "KSP/InternalSpace/Bumped Specular2"
{
	Properties 
	{
		_MainTex("_MainTex (RGB spec(A))", 2D) = "white" {}
		_BumpMap("_BumpMap", 2D) = "bump" {}
		_LightMap ("_LightMap", 2D) = "gray" {}
		_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
		_Shininess ("Shininess", Range (0.03, 1)) = 0.078125
        //_EdgeColor ("Edge Color", Color) = (1,1,1,1)
        //_EdgeThreshold ("Edge Threshold", Range(0.01, 3)) = 1
        _LightColor ("Internal Light Color", Color) = (1,1,1,1)

	}
	
	SubShader 
	{
		ZWrite On
		ZTest LEqual
		Blend SrcAlpha OneMinusSrcAlpha 

		CGPROGRAM

        #include "LightingKSP.cginc"
        #pragma surface surf BlinnPhongSmooth
		#pragma target 3.0

		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _LightMap;

        //uniform float _EdgeThreshold;
        //uniform float4 _EdgeColor;
        //uniform float _EdgeIntensity;
        uniform float _AOIntensity;
        uniform float _Shininess;
        uniform float4 _SpecularColor;
        uniform float4 _LightColor;
        //uniform float4 _LightWrapping;
		
		struct Input
		{
			float2 uv_MainTex;
			float2 uv_BumpMap;
			float2 uv2_LightMap;
			float2 uv_Emissive;
		};

		void surf (Input IN, inout SurfaceOutput o)
		{
			float4 color = tex2D(_MainTex,(IN.uv_MainTex));
			float4 lightmap = tex2D(_LightMap, IN.uv2_LightMap);
			float3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
			//float3 edge = pow(lightmap.r, _EdgeThreshold);
			float3 light1 = lightmap.g * UNITY_LIGHTMODEL_AMBIENT*3;
			float3 light2 = lightmap.b * _LightColor;
			float3 AO = lightmap.r + light2;

			o.Albedo = color.rgb * AO;
			o.Emission = (light1 + light2) * color.rgb * AO;
			o.Gloss = color.a;
			o.Specular = _Shininess;
			o.Normal = normal;
			o.Alpha = 1;
		}
		ENDCG
	}
	Fallback "Diffuse"
}