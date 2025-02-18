// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "EVE/Atmosphere" {
	Properties {
		_Color ("Color Tint", Color) = (1,1,1,1)
		_OceanRadius ("Ocean Radius", Float) = 63000
		_SphereRadius ("Sphere Radius", Float) = 67000
		_PlanetOrigin ("Sphere Center", Vector) = (0,0,0,1)
		_SunsetColor ("Color Sunset", Color) = (1,0,0,.45)
		_DensityFactorA ("Density RatioA", Float) = .22
		_DensityFactorB ("Density RatioB", Float) = 800
		_DensityFactorC ("Density RatioC", Float) = 1
		_DensityFactorD ("Density RatioD", Float) = 1
		_DensityFactorE ("Density RatioE", Float) = 1
		_Scale ("Scale", Float) = 1
		_Visibility ("Visibility", Float) = .0001
		_DensityVisibilityBase ("_DensityVisibilityBase", Float) = 1
		_DensityVisibilityPow ("_DensityVisibilityPow", Float) = 1
		_DensityVisibilityOffset ("_DensityVisibilityOffset", Float) = 1
		_DensityCutoffBase ("_DensityCutoffBase", Float) = 1
		_DensityCutoffPow ("_DensityCutoffPow", Float) = 1
		_DensityCutoffOffset ("_DensityCutoffyOffset", Float) = 1
		_DensityCutoffScale ("_DensityCutoffScale", Float) = 1
	}

Category {
	
	Tags { "Queue"="Transparent-3" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Blend OneMinusDstAlpha One
	Fog { Mode Off}
	ZTest Off
	ColorMask RGB
	Cull Front Lighting On ZWrite Off
	
SubShader {
	Pass {

		Lighting On
		Tags { "LightMode"="ForwardBase"}
		
		CGPROGRAM
		
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
		#define PI 3.1415926535897932384626
		#define TWO_INV_PI (2.0/PI)
		#define HALF_INV_PI (0.5/PI)
		#define TWOPI (2.0*PI) 
		#define INV_2PI (1.0/TWOPI)
	    #define FLT_MAX (1e+32)
        #define EULER_N (2.718281828459045235360)

		fixed4 _Color;
		fixed4 _SunsetColor;
		sampler2D _CameraDepthTexture;
		float _OceanRadius;
		float _SphereRadius;
		float3 _PlanetOrigin;
		float _DensityFactorA;
		float _DensityFactorB;
		float _DensityFactorC;
		float _DensityFactorD;
		float _DensityFactorE;
		float _Scale;
		float _Visibility;
		float _DensityVisibilityBase;
		float _DensityVisibilityPow;
		float _DensityVisibilityOffset;
		float _DensityCutoffBase;
		float _DensityCutoffPow;
		float _DensityCutoffOffset;
		float _DensityCutoffScale;
		
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

		float atmofunc( float l, float d)
		{
		  float e = EULER_N;
		  float a = _DensityFactorA;
		  float b = _DensityFactorB;
		  float c = _DensityFactorC;
		  float D = _DensityFactorD;
		  float f = _DensityFactorE;
		  
		  float l2 = l;
		  l2 *= l2;
		  float d2 = d;
		  d2 *= d2;
		  
          float n = sqrt((c*l2)+(b*d2));
          
		  return -2*a*D*(n+D)*pow(e,-(n+f)/D)/c;
	    }		

		v2f vert (appdata_t v)
		{
			v2f o;
			UNITY_INITIALIZE_OUTPUT(v2f,o);
			o.pos = UnityObjectToClipPos(v.vertex);
			
		   float3 vertexPos = mul(unity_ObjectToWorld, v.vertex).xyz;
	   	   o.worldVert = vertexPos;
	   	   
	   	   
	   	   #ifdef WORLD_SPACE_ON
	   	   o.worldOrigin = _PlanetOrigin;
	   	   #else
	   	   o.worldOrigin = mul(unity_ObjectToWorld, fixed4(0,0,0,1)).xyz;
	   	   #endif
	   	   
	   	   o.L = o.worldOrigin - _WorldSpaceCameraPos.xyz;
	   	   o.L *= _Scale;
	   	   
	//   	   #ifdef WORLD_SPACE_ON
	   	   o.scrPos=ComputeScreenPos(o.pos);
		   COMPUTE_EYEDEPTH(o.scrPos.z);
	//	   #endif
		   
		   TRANSFER_VERTEX_TO_FRAGMENT(o);
	   	   return o;
	 	}
	 		
		fixed4 frag (v2f IN) : COLOR
			{
			half4 color = _Color;
			float depth = FLT_MAX;
	//		#ifdef WORLD_SPACE_ON
			depth = UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(IN.scrPos)));
			depth = LinearEyeDepth(depth);
	//		#else
	//		depth = LinearEyeDepth(1);
	//		#endif
			depth *= _Scale;
			
			half3 worldDir = normalize(IN.worldVert - _WorldSpaceCameraPos.xyz);
			float tc = dot(IN.L, worldDir);
			float d = sqrt(dot(IN.L,IN.L)-(tc*tc));
			float3 norm = normalize(-IN.L);
			float d2 = pow(d,2);
			float td = sqrt(dot(IN.L,IN.L)-d2);		 
					   	
	        float oceanRadius = _Scale*_OceanRadius;
			half sphereCheck = step(d, oceanRadius)*step(0.0, tc);

            float sphereRadius = _Scale*_SphereRadius;
            half bodyCheck = step(d, sphereRadius)*step(0.0, tc);
            
			float tlc = sqrt((oceanRadius*oceanRadius)-d2);
			float oceanSphereDist = lerp(depth, tc - tlc, sphereCheck);

			depth = min(oceanSphereDist, depth);
		   	
		   	
		   	float dist = depth;
		   	float alt = length(IN.L);
			float3 scaleOrigin = (_Scale*(IN.worldOrigin - _WorldSpaceCameraPos.xyz))+_WorldSpaceCameraPos.xyz;
			float3 dPos = _WorldSpaceCameraPos.xyz+(dist*worldDir);
			float3 dPos2 = _WorldSpaceCameraPos.xyz+(sign(tc)*td*worldDir);
			float3 dDir = normalize(dPos2-scaleOrigin);
			float altD = length(dPos-scaleOrigin);
			float altA = .5*(alt+altD);
			
			
		    //depth = LinearEyeDepth(1);
			sphereCheck = step(0.0, tc);
			float depthL = lerp(depth+td, max(0, depth-tc), sphereCheck);
			float camL = sphereCheck*tc;
			float subDepthL = lerp(td, max(0, tc-depth), sphereCheck);
			
			depth = ( atmofunc(depthL, d) - atmofunc(0, d) );
	   	    depth += ( atmofunc(camL, d) - atmofunc(subDepthL, d) );
			
			//depth = saturate(depth);
			depth += saturate(_DensityCutoffScale*pow(_DensityCutoffBase,-_DensityCutoffPow*(alt+_DensityCutoffOffset)))*
			         (_Visibility*dist*pow(_DensityVisibilityBase,-_DensityVisibilityPow*(altA+_DensityVisibilityOffset)));
			
			//depth *= _DensityVisibilityBase - pow(_DensityVisibilityBase, -(_Visibility*dist));
			
			
			//depth = min(depth, sphereDist);
			half3 lightDirection = normalize(_WorldSpaceLightPos0);
			half NdotW = dot(worldDir, lightDirection);
			half NdotL = dot (norm, lightDirection);
			half NdotA = saturate(NdotL)+saturate(NdotW);
	        fixed atten = LIGHT_ATTENUATION(IN); 
			half lightIntensity = _LightColor0.a * NdotA * 2 * atten;
			lightIntensity = max(0.0, lightIntensity);
			half3 light = max(0.0,((_LightColor0.rgb) * lightIntensity));
			//NdotL = abs(NdotL);		
			//half VdotL = dot (-worldDir, lightDirection);
			
			color.a *= saturate(depth);
          	//color.a *= lerp(saturate(lightIntensityAbs), saturate(lightIntensity), bodyCheck);
          	color.a *= lerp((1-bodyCheck)*saturate(lightIntensity), saturate(lightIntensity), NdotL);
          	
          	half sunsetLerp = 1-saturate(pow(dot(dDir, lightDirection),1));
          	     sunsetLerp *= saturate(pow(NdotW,5));
          	color.rgb = lerp(_Color, _SunsetColor, sunsetLerp);
          	color.a = lerp(color.a, color.a*_SunsetColor.a, sunsetLerp);
          	return color;
		}
		ENDCG
	
		}
		
	} 
	


			
}
}
