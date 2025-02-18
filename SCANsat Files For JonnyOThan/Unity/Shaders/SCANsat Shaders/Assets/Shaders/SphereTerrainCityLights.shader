// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "EVE/TerrainCityLight" {
	Properties {
	_Color ("Color Tint", Color) = (1,1,1,1)
	_SpecularColor ("Specular tint", Color) = (1,1,1,1)
	_CityOverlayTex ("Overlay (RGBA)", 2D) = "white" {}
	_CityOverlayDetailScale ("Overlay Detail Scale", Range(0,1000)) = 80
	_CityDarkOverlayDetailTex ("Overlay Detail (RGB) (A)", 2D) = "white" {}
	_CityLightOverlayDetailTex ("Overlay Detail (RGB) (A)", 2D) = "white" {}
	_PlanetOpacity ("PlanetOpacity", Float) = 1
	_PlanetOrigin("Sphere Center", Vector) = (0,0,0,1)
	_SunDir("SunDir", Vector) = (0,0,0,1)
	}
	Category {
		Lighting On
		ZWrite Off
		Cull Back
		Offset 0, 0
		Blend SrcAlpha OneMinusSrcAlpha
		Tags {
			"Queue"="Geometry+2"
			"RenderMode"="Transparent"
			"IgnoreProjector"="True"
		}
		SubShader {
			Pass {

				Lighting Off

				CGPROGRAM

				#include "EVEUtils.cginc"
				#pragma target 3.0
				#pragma glsl
				#pragma vertex vert
				#pragma fragment frag
				#define MAG_ONE 1.4142135623730950488016887242097
				#pragma fragmentoption ARB_precision_hint_fastest
				#pragma multi_compile_fwdbase
#pragma multi_compile MAP_TYPE_1 MAP_TYPE_CUBE_1 MAP_TYPE_CUBE2_1 MAP_TYPE_CUBE6_1
#ifndef MAP_TYPE_CUBE2_1
#pragma multi_compile ALPHAMAP_N_1 ALPHAMAP_1
#endif
#include "alphaMap.cginc"
#include "cubeMap.cginc"

				CUBEMAP_DEF_1(_CityOverlayTex)

				fixed4 _Color;
				float _SpecularPower;
				half4 _SpecularColor;
				float _DetailDist;

				float _PlanetOpacity;
				float3 _PlanetOrigin;
				float3 _SunDir;


				float _CityOverlayDetailScale;
				sampler2D _CityDarkOverlayDetailTex;
				sampler2D _CityLightOverlayDetailTex;

				struct appdata_t {
					float4 vertex : POSITION;
					fixed4 color : COLOR;
					float3 normal : NORMAL;
					float4 texcoord : TEXCOORD0;
					float4 texcoord2 : TEXCOORD1;
					float3 tangent : TANGENT;
				};

				struct v2f {
					float4 pos : SV_POSITION;
					float4 color : TEXCOORD0;
					float4 objnormal : TEXCOORD1;
					LIGHTING_COORDS(2,3)
					float3 worldNormal : TEXCOORD4;
					float3 sphereNormal : TEXCOORD5;
					float terminator : TEXCOORD6;
					float3 viewDir : TEXCOORD7;
				};

				v2f vert (appdata_t v)
				{
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);

					float3 vertexPos = mul(unity_ObjectToWorld, v.vertex).xyz;
					o.objnormal.w = distance(vertexPos,_WorldSpaceCameraPos);
					o.viewDir = normalize(vertexPos - _WorldSpaceCameraPos);
					o.worldNormal = normalize(vertexPos - _PlanetOrigin);
					o.sphereNormal = -(float4(v.texcoord.x, v.texcoord.y, v.texcoord2.x, v.texcoord2.y)).xyz;
					o.color = v.color;
					o.objnormal.xyz = v.normal;

					float3 lightDirection = normalize(_SunDir.xyz);
					half NdotL = dot (o.sphereNormal, lightDirection);
					half termlerp = saturate(10*-NdotL);
					o.terminator = lerp(1,saturate(floor(1.01+NdotL)), termlerp);

					TRANSFER_VERTEX_TO_FRAGMENT(o);

					return o;
				}

				fixed4 frag (v2f IN) : COLOR
				{
					half4 color;

					half4 cityoverlay = GET_CUBE_MAP_1(_CityOverlayTex, IN.sphereNormal);
					cityoverlay = ALPHA_COLOR_1(cityoverlay);

					half4 citydarkoverlaydetail = GetCubeDetailMap(_CityDarkOverlayDetailTex, IN.sphereNormal, _CityOverlayDetailScale);
					half4 citylightoverlaydetail = GetCubeDetailMap(_CityLightOverlayDetailTex, IN.sphereNormal, _CityOverlayDetailScale);


					cityoverlay.a *= 1-step(IN.color.a, 0);
					//cityoverlay.a = 1-step(cityoverlay.a, 0);
					citydarkoverlaydetail.a *= cityoverlay.a;
					citylightoverlaydetail.a *= cityoverlay.a;
					color.rgb = lerp( half3(0,0,0), citylightoverlaydetail.rgb, citylightoverlaydetail.a);
					color.a = 0;

					half4 specColor = _SpecularColor;

					//world
					color = SpecularColorLight( _SunDir, IN.viewDir, IN.worldNormal, color, specColor, _SpecularPower, LIGHT_ATTENUATION(IN) );
					color *= Terminator( normalize(_SunDir), IN.worldNormal);


					//lightIntensity = saturate(_LightColor0.a * (SNdotL - 0.01) / 0.99 * 4 * atten);
					citydarkoverlaydetail.a *= 1-saturate(color.a);
					color = lerp(citylightoverlaydetail, citydarkoverlaydetail, citydarkoverlaydetail.a);
					color.a *= _PlanetOpacity;

					return color;
				}
				ENDCG

			}
		}
	}
}