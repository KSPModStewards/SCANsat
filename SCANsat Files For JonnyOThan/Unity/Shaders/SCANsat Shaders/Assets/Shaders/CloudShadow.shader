// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_Projector' with 'unity_Projector'

Shader "EVE/CloudShadow" {
	Properties{
		_Color("Color Tint", Color) = (1,1,1,1)
		_MainTex("Main (RGB)", 2D) = "white" {}
		_DetailTex("Detail (RGB)", 2D) = "white" {}
		_UVNoiseTex("UV Noise (RG)", 2D) = "black" {}
		_DetailScale("Detail Scale", float) = 100
		_DetailDist("Detail Distance", Range(0,1)) = 0.00875
		_UVNoiseScale("UV Noise Scale", Range(0,0.1)) = 0.01
		_UVNoiseStrength("UV Noise Strength", Range(0,0.1)) = 0.002
		_UVNoiseAnimation("UV Noise Animation", Vector) = (0.002,0.001,0)

		_PlanetOrigin("Sphere Center", Vector) = (0,0,0,1)
		_SunDir("Sunlight direction", Vector) = (0,0,0,1)
		_Radius("Radius", Float) = 1
		_PlanetRadius("Planet Radius", Float) = 1
		_ShadowFactor("Shadow Factor", Float) = 1
	}

	SubShader{
		Tags{ "Queue" = "Geometry+500" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		Pass {
			Blend Zero SrcColor
			ZWrite Off
			Offset 0, 0
			CGPROGRAM
			#include "EVEUtils.cginc"
			#pragma target 3.0
			#pragma glsl
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile WORLD_SPACE_OFF WORLD_SPACE_ON
#pragma multi_compile MAP_TYPE_1 MAP_TYPE_CUBE_1 MAP_TYPE_CUBE2_1 MAP_TYPE_CUBE6_1
#ifndef MAP_TYPE_CUBE2_1
#pragma multi_compile ALPHAMAP_N_1 ALPHAMAP_1
#endif

#include "alphaMap.cginc"
#include "cubeMap.cginc"

			CUBEMAP_DEF_1(_MainTex)

			fixed4 _Color;
			uniform sampler2D _DetailTex;
			uniform sampler2D _UVNoiseTex;
			fixed4 _DetailOffset;
			float _DetailScale;
			float _DetailDist;
			float _UVNoiseScale;
			float _UVNoiseStrength;
			float2 _UVNoiseAnimation;
			float4 _SunDir;
			float _Radius;
			float _PlanetRadius;
			float _ShadowFactor;

			float3 _PlanetOrigin;
			uniform float4x4 unity_Projector;

			struct appdata_t {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float4 posProj : TEXCOORD0;
				float shadowCheck : TEXCOORD1;
				float originDist : TEXCOORD2;
				float4 worldPos : TEXCOORD3;
				float3 mainPos : TEXCOORD4;
				float3 detailPos : TEXCOORD5;
			};

			v2f vert(appdata_t v)
			{
				v2f o;
				o.posProj = mul(unity_Projector, v.vertex);
				o.pos = UnityObjectToClipPos(v.vertex);

				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
#ifdef WORLD_SPACE_ON
				float4 vertexPos = o.worldPos;
				float3 worldOrigin = _PlanetOrigin;
#else
				float4 vertexPos = v.vertex;
				float3 worldOrigin = float3(0,0,0);
#endif


				float3 L = worldOrigin - vertexPos.xyz;
				o.originDist = length(L);
				float tc = dot(L,-_SunDir);
				float ntc = dot(normalize(L), _SunDir);
				float d = sqrt(dot(L,L) - (tc*tc));
				float d2 = pow(d,2);
				float td = sqrt(dot(L,L) - d2);
				float sphereRadius = _Radius;
				o.shadowCheck = step(o.originDist, sphereRadius)*saturate(ntc*100);
				//saturate((step(d, sphereRadius)*step(0.0, tc))+
				//(step(o.originDist, sphereRadius)));
				float tlc = sqrt((sphereRadius*sphereRadius) - d2);
				float sphereDist = lerp(lerp(tlc - td, tc - tlc, step(0.0, tc)),
				lerp(tlc - td, tc + tlc, step(0.0, tc)), step(o.originDist, sphereRadius));
				float4 planetPos = vertexPos + (-_SunDir*sphereDist);
				planetPos = (mul(_MainRotation, planetPos));
				o.mainPos = planetPos.xyz;
				o.detailPos = (mul(_DetailRotation, planetPos)).xyz;
				return o;
			}

			fixed4 frag(v2f IN) : COLOR
			{
				half shadowCheck = step(0, IN.posProj.w)*IN.shadowCheck;

				//Ocean filter
#ifdef WORLD_SPACE_ON
				shadowCheck *= saturate(.2*((IN.originDist + 5) - _PlanetRadius));
#endif

				half4 main = GET_CUBE_MAP_P(_MainTex, IN.mainPos, _UVNoiseTex, _UVNoiseScale, _UVNoiseStrength, _UVNoiseAnimation);
				main = ALPHA_COLOR_1(main);

				half4 detail = GetCubeDetailMap(_DetailTex, IN.detailPos, _DetailScale);

				float viewDist = distance(IN.worldPos.xyz,_WorldSpaceCameraPos);
				half detailLevel = saturate(2 * _DetailDist*viewDist);
				fixed4 color = _Color * main.rgba * lerp(detail.rgba, 1, detailLevel);

				color.rgb = saturate(color.rgb * (1- color.a));
				color.rgb = lerp(1, color.rgb, _ShadowFactor*color.a);
				return lerp(1, color, shadowCheck);
			}

			ENDCG
		}
	}
}