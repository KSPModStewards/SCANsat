// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "EVE/TerrainLight" {
	Properties {
		_Color ("Color Tint", Color) = (1,1,1,1)
		_SpecularPower ("Shininess", Float) = 0.078125
		_PlanetOpacity ("PlanetOpacity", Float) = 1
	}
	Category {
	   Lighting On
	   ZWrite Off
	   Cull Back
	   Blend SrcColor SrcAlpha, One Zero
	   Tags {
	   "Queue"="Transparent-5"
	   "RenderMode"="Transparent"
	   "IgnoreProjector"="True"
	   }
	   SubShader {  	
        Pass {

		Lighting On
		Tags { "LightMode"="ForwardBase"}
		
		CGPROGRAM
		
		#include "EVEUtils.cginc"
		#include "UnityCG.cginc"
		#include "AutoLight.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#pragma glsl
		#pragma vertex vert
		#pragma fragment frag
		#define MAG_ONE 1.4142135623730950488016887242097
		#pragma fragmentoption ARB_precision_hint_fastest
		#pragma multi_compile_fwdbase
		#pragma multi_compile_fwdadd_fullshadows
		
	 
		fixed4 _Color;
		float _SpecularPower;
		half4 _SpecularColor;
		float _DetailDist;
		
		float _PlanetOpacity;
		
		sampler2D _CityOverlayTex;
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
			float3 sphereCoords : TEXCOORD5;
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
	   	   o.worldNormal = normalize(mul( unity_ObjectToWorld, float4(v.normal, 0)).xyz);
	   	   o.sphereCoords = -(float4(v.texcoord.x, v.texcoord.y, v.texcoord2.x, v.texcoord2.y)).xyz;
	   	   o.color = v.color;	
		   o.objnormal.xyz = v.normal;
		   
		   float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
		   half NdotL = dot (o.sphereCoords, lightDirection);
		   half termlerp = saturate(10*-NdotL);
    	   o.terminator = lerp(1,saturate(floor(1.01+NdotL)), termlerp);
			
    	   TRANSFER_VERTEX_TO_FRAGMENT(o);
    	   
	   	   return o;
	 	}
	 		 		
		fixed4 frag (v2f IN) : COLOR
		{
			half4 color = _Color;
		    
			float3 sphereNrm = normalize(IN.sphereCoords);
		    half vertLerp = saturate((32*(saturate(dot(IN.objnormal.xyz, -sphereNrm))-.95))+.5);
			
			//world
			color = SpecularColorLight( _WorldSpaceLightPos0, IN.viewDir, IN.worldNormal, color, 0, 0, LIGHT_ATTENUATION(IN) );
			color *= Terminator( normalize(_WorldSpaceLightPos0), IN.worldNormal);
			
			color.rgb *= color.a;
          	return color;
		}
		ENDCG
	
		}
        }
    }
}