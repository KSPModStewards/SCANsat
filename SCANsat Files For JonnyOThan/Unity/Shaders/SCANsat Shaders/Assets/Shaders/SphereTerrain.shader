// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "EVE/Terrain" {
	Properties {
		_Color ("Color Tint", Color) = (1,1,1,1)
		_MainTex ("Main (RGB)", 2D) = "white" {}
		_BumpMap ("Normalmap", 2D) = "bump" {}
		_SpecularColor ("Specular tint", Color) = (1,1,1,1)
		_SpecularPower ("Shininess", Float) = 0.078125
		_midTex ("Detail (RGB)", 2D) = "white" {}
		_steepTex ("Detail for Vertical Surfaces (RGB)", 2D) = "white" {}
		_DetailScale ("Detail Scale", Range(0,1000)) = 200
		_DetailVertScale ("Detail Scale", Range(0,1000)) = 200
		_DetailOffset ("Detail Offset", Vector) = (.5,.5,0,0)
		_DetailDist ("Detail Distance", Range(0,1)) = 0.00875
		_MinLight ("Minimum Light", Range(0,1)) = 0
		_Albedo ("Albedo Index", Range(0,5)) = 1.2
		_CityOverlayTex ("Overlay (RGB)", 2D) = "white" {}
		_CityOverlayDetailScale ("Overlay Detail Scale", Range(0,1000)) = 80
		_CityDarkOverlayDetailTex ("Overlay Detail (RGB) (A)", 2D) = "white" {}
		_CityLightOverlayDetailTex ("Overlay Detail (RGB) (A)", 2D) = "white" {}
		_SunDir ("Sun Direction", Vector) = (1,1,1,1)
		_PlanetOpacity ("PlanetOpacity", Float) = 1
		_OceanRadius ("Ocean Radius", Float) = 63000
		_OceanColor ("Ocean Color Tint", Color) = (1,1,1,1)
		_OceanDepthFactor ("Ocean Depth Factor", Float) = .002
		_PlanetOrigin ("Planet Center", Vector) = (0,0,0,1)
	}


	
SubShader {

Tags { "Queue"="Geometry" "RenderType"="Opaque" }
	Fog { Mode Global}
	ColorMask RGB
	Cull Back Lighting On ZWrite On
	
	Pass {

		Lighting On
		Tags { "LightMode"="ForwardBase"}
		
		CGPROGRAM
		
		#include "EVEUtils.cginc"
		#include "UnityCG.cginc"
		#include "AutoLight.cginc"
		#include "Lighting.cginc"
		#include "alphaMap.cginc"
		#include "cubeMap.cginc"

		#pragma target 3.0
		#pragma glsl
		#pragma vertex vert
		#pragma fragment frag
		#define MAG_ONE 1.4142135623730950488016887242097
		#pragma fragmentoption ARB_precision_hint_fastest
		#pragma multi_compile_fwdbase
		#pragma multi_compile_fwdadd_fullshadows
		#pragma multi_compile CITYOVERLAY_OFF CITYOVERLAY_ON
		#pragma multi_compile DETAIL_MAP_OFF DETAIL_MAP_ON
	    #pragma multi_compile OCEAN_OFF OCEAN_ON
	    
		fixed4 _Color;
		float _SpecularPower;
		half4 _SpecularColor;
		sampler2D _MainTex;
		sampler2D _BumpMap;
		sampler2D _midTex;
		sampler2D _steepTex;
		float _DetailScale;		
		fixed4 _DetailOffset;
		float _DetailVertScale;
		float _DetailDist;
		float _MinLight;
		float _Albedo;
		half3 _SunDir;
		float _PlanetOpacity;
		float _OceanRadius;
		float _OceanDepthFactor;
		fixed4 _OceanColor;
		float3 _PlanetOrigin;
		
		#ifdef CITYOVERLAY_ON
		sampler2D _CityOverlayTex;
		float _CityOverlayDetailScale;
		sampler2D _CityDarkOverlayDetailTex;
		sampler2D _CityLightOverlayDetailTex;
		#endif
		
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
			float3 sphereCoords : TEXCOORD5;
			float terminator : TEXCOORD6;
			float3 L : TEXCOORD7;
			float3 viewDir : TEXCOORD8;
		};

		v2f vert (appdata_t v)
		{
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			
		   float3 vertexPos = mul(unity_ObjectToWorld, v.vertex).xyz;
	   	   o.objnormal.w = distance(vertexPos,_WorldSpaceCameraPos);
	   	   o.viewDir = normalize(vertexPos - _WorldSpaceCameraPos);
	   	   o.worldNormal = normalize(mul( unity_ObjectToWorld, float4(v.normal, 0)).xyz);
	   	   o.sphereCoords = -(float4(v.texcoord.x, v.texcoord.y, v.texcoord2.x, v.texcoord2.y)).xyz;
	   	   o.color = v.color;	
		   o.objnormal.xyz = v.normal;
		   
		   half NdotL = dot (o.sphereCoords, normalize(_SunDir));
		   half termlerp = saturate(10*-NdotL);
    	   o.terminator = lerp(1,saturate(floor(1.01+NdotL)), termlerp);
			
		   o.L = _PlanetOrigin - _WorldSpaceCameraPos;
			
    	   TRANSFER_VERTEX_TO_FRAGMENT(o);
    	   
	   	   return o;
	 	}
	 		 		
		fixed4 frag (v2f IN) : COLOR
		{
			half4 color;
		    half4 main = GET_CUBE_MAP_1(_MainTex, IN.sphereCoords);
		    
			float3 sphereNrm = normalize(IN.sphereCoords);
		    half vertLerp = saturate((32*(saturate(dot(IN.objnormal.xyz, -sphereNrm))-.95))+.5);
		    
			half4 detail = GetCubeDetailMap(_midTex, IN.sphereCoords, _DetailScale);
			half4 vert = GetCubeDetailMap(_steepTex, IN.sphereCoords, _DetailVertScale);
			detail = lerp(vert, detail, vertLerp);
			
			#ifdef CITYOVERLAY_ON
			half4 cityoverlay = GET_CUBE_MAP_1(_CityOverlayTex, IN.sphereCoords);
			half4 citydarkoverlaydetail = GetCubeDetailMap(_CityDarkOverlayDetailTex, IN.sphereCoords, _CityOverlayDetailScale);
			half4 citylightoverlaydetail = GetCubeDetailMap(_CityLightOverlayDetailTex, IN.sphereCoords, _CityOverlayDetailScale); 
			#endif
			
			half4 encnorm = GET_CUBE_MAP_1(_BumpMap, IN.sphereCoords);
		    float2 localCoords = encnorm.ag; 
            localCoords -= half2(.5, .5);
            localCoords.x *= .5;
			
			float2 uv;
			uv.x = .5 + (INV_2PI*atan2(sphereNrm.x, sphereNrm.z));
			uv.y = INV_PI*acos(sphereNrm.y);
			uv.x -= .5;
			uv += localCoords;
			
			half3 norm;
			norm.z = cos(TWOPI*uv.x);
			norm.x = sin(TWOPI*uv.x);
			norm.y = cos(PI*uv.y);

			norm = -norm;
			
			
			
			half detailLevel = saturate(2*_DetailDist*IN.objnormal.w);
			color = IN.color + .750*(lerp(detail.rgba-.5, 0, detailLevel));
			
			
			#ifdef OCEAN_ON
			float tc = dot(IN.L, IN.viewDir);
			float d = sqrt(dot(IN.L,IN.L)-dot(tc,tc));
			half sphereCheck = step(d, _OceanRadius)*step(0.0, tc);

			float tlc = sqrt((_OceanRadius*_OceanRadius)-pow(d,2));
			float sphereDist = lerp(IN.objnormal.w, tc - tlc, sphereCheck);
			
			float oceandepth = IN.objnormal.w - sphereDist;
			float depthFactor = saturate(oceandepth * _OceanDepthFactor);
			float vertexDepth = _OceanDepthFactor*15*saturate(floor(1+ oceandepth ));
			color = lerp(color, _OceanColor, depthFactor + vertexDepth );
			#endif
			
			half handoff = saturate(pow(_PlanetOpacity,2));
			color = lerp(color, main, handoff);
			
			#ifdef CITYOVERLAY_ON
			cityoverlay.a *= 1-step(IN.color.a, 0);
			//cityoverlay.a = 1-step(cityoverlay.a, 0);
			citydarkoverlaydetail.a *= cityoverlay.a;
			citylightoverlaydetail.a *= cityoverlay.a;
			color = lerp(color, citylightoverlaydetail, citylightoverlaydetail.a);
			#endif
			
            color *= _Color;
            			
			half4 specColor = _SpecularColor;
			#ifdef OCEAN_ON
			specColor.a = lerp(main.a, 0, saturate(length(IN.sphereCoords) - _OceanRadius));
			#endif
			
			//world
			half4 lightColor = SpecularColorLight( _WorldSpaceLightPos0, IN.viewDir, IN.worldNormal, color, specColor, _SpecularPower, LIGHT_ATTENUATION(IN) );
			
			#ifdef OCEAN_ON
			lightColor *= lerp(Terminator( normalize(_WorldSpaceLightPos0), IN.worldNormal), 1, main.a);
			color = lerp(color, lightColor, saturate((length(IN.sphereCoords+50) - _OceanRadius)/50));
			#else
			lightColor *= Terminator( normalize(_WorldSpaceLightPos0), IN.worldNormal);
			color = lightColor;
			#endif
			
			#ifdef CITYOVERLAY_ON
			//lightIntensity = saturate(_LightColor0.a * (SNdotL - 0.01) / 0.99 * 4 * atten);
			citydarkoverlaydetail.a *= 1-saturate(color.a);
			color = lerp(color, citydarkoverlaydetail, citydarkoverlaydetail.a);
			#endif
			color.a = 1;
			
          	return color;
		}
		ENDCG
	
		}
		
		Pass {
            Tags {"LightMode" = "ForwardAdd"} 
            Blend One One                                      
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_fwdadd 
                
                #include "UnityCG.cginc"
                #include "AutoLight.cginc"
                
                struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float3 normal : NORMAL;
				float3 tangent : TANGENT;
				};
                
                struct v2f
                {
                    float4  pos         : SV_POSITION;
                    float2  uv          : TEXCOORD0;
                    float3  lightDir    : TEXCOORD2;
                    float3 normal		: TEXCOORD1;
                    LIGHTING_COORDS(3,4)
                    float4 color : TEXCOORD5;
                };
 
                v2f vert (appdata_t v)
                {
                    v2f o;
                    
                    o.pos = UnityObjectToClipPos( v.vertex);
					o.uv = 0; // Avoid error I don't understand - WazWaz
					o.lightDir = ObjSpaceLightDir(v.vertex);
					o.color = v.color;
					o.normal =  v.normal;
                    TRANSFER_VERTEX_TO_FRAGMENT(o);
                    return o;
                }
 
                fixed4 _Color;
 
                fixed4 _LightColor0;
 
                fixed4 frag(v2f IN) : COLOR
                {
                    IN.lightDir = normalize(IN.lightDir);
                    fixed atten = LIGHT_ATTENUATION(IN);
					fixed3 normal = IN.normal;                    
                    fixed diff = saturate(dot(normal, IN.lightDir));
                    
                    fixed4 c;
                    c.rgb = (IN.color.rgb * _LightColor0.rgb * diff) * (atten * 2);
                    c.a = IN.color.a;
                    return c;
                }
            ENDCG
        }
	} 
	
	FallBack "VertexLit"
}
