// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: commented out 'float4x4 _CameraToWorld', a built-in variable
// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "EVE/Ocean" {
	Properties {
		_SurfaceColor ("Color Tint", Color) = (1,1,1,1)
		_SpecularColor ("Specular tint", Color) = (1,0,0,1)
		_SpecularPower ("Shininess", Float) = 10
		_MainTex ("Main (RGB)", 2D) = "white" {}
		_DetailTex ("Detail (RGB)", 2D) = "white" {}
		_DetailScale ("Detail Scale", Range(0,1000)) = 200
		_DetailDist ("Detail Distance", Range(0,1)) = 0.00875
		_MinLight ("Minimum Light", Range(0,1)) = 0
		_LightPower ("LightPower", Float) = 1.75
		_PlanetOpacity ("PlanetOpacity", Float) = 1
		_PlanetOrigin ("Planet Center", Vector) = (0,0,0,1)
	}
	
SubShader {

Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout"}
	Blend SrcAlpha OneMinusSrcAlpha
	Fog { Mode Global}
	AlphaTest Greater 0
	ColorMask RGB
	Cull Back Lighting On
		
		//surface
		Pass {
		Lighting On
		ZWrite On
		Tags { "LightMode"="ForwardBase"}
		
		CGPROGRAM
		#include "EVEUtils.cginc"
		#include "UnityCG.cginc"
		#include "AutoLight.cginc"
		#include "Lighting.cginc"
		#include "alphaMap.cginc"
		#include "cubeMap.cginc"
		#pragma target 3.0
		#pragma exclude_renderers d3d11
		#pragma glsl
		#pragma vertex vert
		#pragma fragment frag
		#define MAG_ONE 1.4142135623730950488016887242097
		#pragma fragmentoption ARB_precision_hint_fastest
		#pragma multi_compile_fwdbase
		#pragma multi_compile_fwdadd_fullshadows
		
		fixed4 _SurfaceColor;
		float _SpecularPower;
		half4 _SpecularColor;
		sampler2D _MainTex;
		sampler2D _DetailTex;
		float _DetailScale;
		float _DetailDist;
		float _MinLight;
		sampler2D _CameraDepthTexture;
		// float4x4 _CameraToWorld;
		float _LightPower;
		float _PlanetOpacity;
		float3 _PlanetOrigin;
		
		struct appdata_t {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 texcoord : TEXCOORD0;
			    float4 texcoord2 : TEXCOORD1;
			};

		struct v2f {
			float4 pos : SV_POSITION;
			float  viewDist : TEXCOORD0;
			float3 viewDir : TEXCOORD1;
			float3 worldPos : TEXCOORD2;
			LIGHTING_COORDS(3,4)
			float3 sphereNormal : TEXCOORD5;
		};	
		

		v2f vert (appdata_t v)
		{
			v2f o;
			//half c = .25*_Time.z + frac( v.texcoord.xy ).x;
			//+ (1.5*(1+cos(c))*float4(v.normal,0));
		    float4 vertex = v.vertex;
			
			o.pos = UnityObjectToClipPos(vertex).xyzw;
			
			float3 vertexPos = mul(unity_ObjectToWorld, vertex).xyz;
			o.viewDist = distance(vertexPos,_WorldSpaceCameraPos);

			o.worldPos = vertexPos;
			o.sphereNormal = -normalize(half4(v.texcoord.x, v.texcoord.y, v.texcoord2.x, v.texcoord2.y)).xyz;
			o.viewDir = normalize(_WorldSpaceCameraPos.xyz - vertexPos);

			TRANSFER_VERTEX_TO_FRAGMENT(o);

			return o;
	 	}
	 		 		
		fixed4 frag (v2f IN) : COLOR
		{
			half4 color;
			float3 sphereNrm = IN.sphereNormal;
		 	
		    half4 main = GET_CUBE_MAP_1(_MainTex, sphereNrm);
			main = ALPHA_COLOR_1(main);

			half4 detail = GetCubeDetailMap(_DetailTex, sphereNrm, _DetailScale);
			
			color = _SurfaceColor;
            
            half detailLevel = saturate(2*_DetailDist*IN.viewDist);
			color.rgb += .5*lerp(detail.rgb-.5, 0, detailLevel);
            
            half handoff = saturate(pow(_PlanetOpacity,2));
			color.rgb = lerp(color.rgb, main.rgb, handoff);
            
			half4 specColor = _SpecularColor;
			specColor.a = lerp(1, main.a, handoff);
			half4 colorLight = SpecularColorLight( _WorldSpaceLightPos0, IN.viewDir, (IN.worldPos-_PlanetOrigin), color, specColor, _SpecularPower, LIGHT_ATTENUATION(IN) );
			
			color.a = lerp(1, color.a, saturate(colorLight.a*4));
			color.a = lerp(color.a, 1, saturate(length(colorLight.rgb)-length(2*color.rgb)));
			color.a = lerp(color.a, 1, handoff);
    		color.rgb = colorLight.rgb;
    		
          	return color;
		}
		ENDCG
	
		}
		
		// Pass to render object as a shadow collector
		Pass {
			Name "ShadowCollector"
			Tags { "LightMode" = "ShadowCollector" }
			
			Fog {Mode Off}
			ZWrite On ZTest LEqual

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma multi_compile_shadowcollector

			#define SHADOW_COLLECTOR_PASS
			#include "UnityCG.cginc"

			struct v2f {
				V2F_SHADOW_COLLECTOR;
			};


			v2f vert (appdata_base v)
			{
				v2f o;
				TRANSFER_SHADOW_COLLECTOR(o)
				return o;
			}

			uniform fixed4 _Color;

			fixed4 frag (v2f i) : COLOR
			{
				SHADOW_COLLECTOR_FRAGMENT(i)
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
					o.uv = 0; // I don't know why.
					o.lightDir = ObjSpaceLightDir(v.vertex).xyz;
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

}