Shader "KSP/InternalSpace/Bumped Specular"
{
	Properties 
	{
        [Header(Texture Maps)]
		_MainTex("_MainTex (RGB spec(A))", 2D) = "white" {}
		_BumpMap("_BumpMap", 2D) = "bump" {}
		_LightMap ("_LightMap", 2D) = "gray" {}

        [Header(Specular)]
		_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
        _Shininess ("Shininess", Range (0.03, 1)) = 0.4

        [Header(Lightmaps and Occlusion)]
        [Toggle(LMTOGGLE)] _LMTOGGLE("Enable Lightmap", Float) = 0
        [KeywordEnum(Single Colored, Multi Grayscale)] _LM_Mode("Mode", Float) = 0
        _LightColor1 ("Internal Light Color 1", Color) = (1,1,1,1)
        _LightColor2 ("Internal Light Color 2", Color) = (1,1,1,1)
        _LightAmbient("Ambient Boost", Range(0, 3)) = 1

        _Occlusion("Occlusion Tightness", Range(0, 3)) = 1
        [Space] //Im in Spaaaaaaaace

        [Header(Fog)]
        [Toggle(FOGTOGGLE)] _FOGTOGGLE("Enable Fog", Float) = 0
        _FogColor("Color (RGB) Density (A)", Color) = (0.3, 0.4, 0.7, 0.5)
        _FogStart("Start", Float) = 1
        _FogEnd("End", Float) = 10

	}
	
	SubShader 
	{
        Tags{ "RenderType" = "Opaque" }
        LOD 200
		//ZWrite On
		//ZTest LEqual
		//Blend SrcAlpha OneMinusSrcAlpha 

		CGPROGRAM

        #include "LightingKSP.cginc"

        #pragma surface surf BlinnPhongSmooth vertex:fogVertex finalcolor:fogColor
        #pragma shader_feature LMTOGGLE
        #pragma shader_feature FOGTOGGLE
        #pragma multi_compile _LM_MODE_MULTI_GREYSCALE _LM_MODE_SINGLE_COLORED

		#pragma target 3.0



		sampler2D _MainTex;
		sampler2D _BumpMap;
        #if LMTOGGLE
		sampler2D _LightMap;
        #endif



        uniform float _Shininess;
        uniform float4 _SpecularColor;

        #if LMTOGGLE
        uniform float4 _LightColor1;
        uniform float4 _LightColor2;
        uniform float _LightAmbient;
        uniform float _Occlusion;
        #endif

        #if FOGTOGGLE
        uniform float4 _FogColor;
        uniform float _FogStart;
        uniform float _FogEnd;
        #endif

		struct Input
		{
			float2 uv_MainTex;
			float2 uv_BumpMap;
            float4 color : COLOR;   //vertex color

            #if LMTOGGLE
			float2 uv2_LightMap;
            #endif

            #if FOGTOGGLE
            half fogFactor;
            #endif
		};

        void fogVertex(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);
            #if FOGTOGGLE
            float cameraVertDist = length(mul(UNITY_MATRIX_MV, v.vertex).xyz);
            data.fogFactor = lerp(saturate((_FogEnd.x - cameraVertDist) / (_FogEnd.x - _FogStart.x)), 1, 1 - _FogColor.a);
            #endif
        }

        void fogColor(Input IN, SurfaceOutput o, inout fixed4 c)
        {
            #if FOGTOGGLE
            c.rgb = lerp(_FogColor.rgb, c.rgb, IN.fogFactor);
            #endif
        }

		void surf (Input IN, inout SurfaceOutput o)
		{
			float4 c = tex2D(_MainTex,(IN.uv_MainTex));
			float3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));

            #if LMTOGGLE

            float4 lightmap = tex2D(_LightMap, IN.uv2_LightMap);

            #if _LM_MODE_SINGLE_COLORED
            float3 light = lightmap.rgb * _LightColor1 + UNITY_LIGHTMODEL_AMBIENT * _LightAmbient * lightmap.rgb;
            float3 AO = pow(lightmap.a, _Occlusion);
            float3 finalLight = light * c.rgb * AO;
            #endif

            #if _LM_MODE_MULTI_GREYSCALE
            float3 light1 = lightmap.r * _LightColor1;
            float3 light2 = lightmap.g * _LightColor2;
			float3 light3 = lightmap.b * UNITY_LIGHTMODEL_AMBIENT * _LightAmbient;
			float3 AO = lerp(pow(lightmap.a, _Occlusion), 1, light1 + light2);
            float3 finalLight = (light1 + light2 + light3) * c.rgb * AO;
            #endif

            #endif

			o.Albedo = c.rgb * step(0.5,IN.color.rgb);
			o.Gloss = c.a;
			o.Specular = _Shininess;
			o.Normal = normal;
			o.Alpha = 1;

            #if LMTOGGLE
            o.Albedo *= AO;
            o.Emission = finalLight;
            #endif
		}
		ENDCG
	}
	Fallback "Diffuse"
}