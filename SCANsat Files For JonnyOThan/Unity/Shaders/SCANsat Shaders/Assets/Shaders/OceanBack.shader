// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "EVE/OceanBack" {
	Properties {
		_OceanColor ("Color Tint", Color) = (1,1,1,1)
		_OceanRadius ("Ocean Radius", Float) = 63000
		_PlanetOrigin ("Sphere Center", Vector) = (0,0,0,1)
	}

Category {
	
	Tags { "Queue"="Geometry-1" "IgnoreProjector"="True" "RenderType"="TransparentCutout" }
	Blend SrcAlpha OneMinusSrcAlpha
	Fog { Mode Global}
	AlphaTest Greater 0
	ColorMask RGB
	Cull Off Lighting On ZWrite Off ZTest Off
	
SubShader {
	Pass {

		Tags { "LightMode"="ForwardBase"}
		
		CGPROGRAM
		#include "EVEUtils.cginc"
		#include "UnityCG.cginc"
		#include "AutoLight.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#pragma glsl
		#pragma exclude_renderers gles
		#pragma vertex vert
		#pragma fragment frag
		#pragma multi_compile_fwdbase
	    #pragma multi_compile WORLD_SPACE_OFF WORLD_SPACE_ON
		#define MAG_ONE 1.4142135623730950488016887242097
		#pragma fragmentoption ARB_precision_hint_fastest

		fixed4 _OceanColor;
		float _OceanRadius;
		float3 _PlanetOrigin;
		
		struct appdata_t {
				float4 vertex : POSITION;
				fixed4 color : COLOR;
				float3 normal : NORMAL;
			};

		struct v2f {
			float4 pos : SV_POSITION;
			float4 scrPos : TEXCOORD0;
			float3 worldVert : TEXCOORD1;
			LIGHTING_COORDS(2,3)
			float3 worldOrigin : TEXCOORD4;
			float3 L : TEXCOORD5;
		};	

		v2f vert (appdata_t v)
		{
			v2f o;
			UNITY_INITIALIZE_OUTPUT(v2f,o);
			o.pos = UnityObjectToClipPos(v.vertex);
			
		   float3 vertexPos = mul(unity_ObjectToWorld, v.vertex).xyz;
	   	   o.worldVert = vertexPos;
	   	   
	   	   
	   	   o.worldOrigin = _PlanetOrigin;
	   	   
	   	   
	   	   o.L = o.worldOrigin - _WorldSpaceCameraPos.xyz;
		   
		   
		   TRANSFER_VERTEX_TO_FRAGMENT(o);
	   	   return o;
	 	}
	 		
		fixed4 frag (v2f IN) : COLOR
			{
			half4 color = _OceanColor;
			
			half3 worldDir = normalize(IN.worldVert - _WorldSpaceCameraPos.xyz);
			float tc = dot(IN.L, worldDir);
			float d = sqrt(dot(IN.L,IN.L)-(tc*tc));
			float3 norm = normalize(-IN.L);
			float d2 = pow(d,2);
			float td = sqrt(dot(IN.L,IN.L)-d2);		 
					   	
			half sphereCheck = step(d, _OceanRadius)*step(0.0, tc);
            
		  	color.a = sphereCheck;
		  
          	return color;
		}
		ENDCG
	
		}
		
	} 
	


			
}
}
