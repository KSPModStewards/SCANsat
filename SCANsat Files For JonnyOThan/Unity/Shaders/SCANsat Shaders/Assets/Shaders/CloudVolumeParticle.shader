// Upgrade NOTE: commented out 'float4x4 _CameraToWorld', a built-in variable
// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "EVE/CloudVolumeParticle" {
	Properties {
		_Tex("Particle Texture", 2D) = "white" {}
		_MainTex("Main (RGB)", 2D) = "white" {}
		_PerlinTex("Perlin (RGB)", 2D) = "white" {}
		_BumpMap("Normalmap", 2D) = "bump" {}
		_DetailTex("Detail (RGB)", 2D) = "white" {}
		_DetailScale("Detail Scale", Range(0,1000)) = 100
		_DistFade("Distance Fade Near", Float) = 1.0
		_DistFadeVert("Distance Fade Vertical", Float) = 0.0004
		_MinScatter("Min Scatter", Float) = 1.05
		_Opacity("Opacity", Float) = 1.05
		_Color("Color Tint", Color) = (1,1,1,1)
		_InvFade("Soft Particles Factor", Range(0,1.0)) = .008
		_Rotation("Rotation", Float) = 0
		_MaxScale("Max Scale", Float) = 1
		_MaxTrans("Max Translation", Vector) = (0,0,0)
		_NoiseScale("Noise Scale", Vector) = (1,2,.0005,100)
		_SunPos("_SunPos", Vector) = (0,0,0)
		_SunRadius("_SunRadius", Float) = 1
	}

	Category {

		Tags { "Queue"="Transparent+2" "IgnoreProjector"="True" "RenderType"="Transparent" "DisableBatching"="True" }
		Blend SrcAlpha OneMinusSrcAlpha
		Fog { Mode Global}
		AlphaTest Greater 0
		ColorMask RGB
		Cull Back Lighting On ZWrite Off

		SubShader {
			Pass {

				Lighting On
				Tags { "LightMode"="ForwardBase"}

				CGPROGRAM
				#include "EVEUtils.cginc"
				#pragma target 3.0
				#pragma glsl
				#pragma vertex vert
				#pragma fragment frag
				#define MAG_ONE 1.4142135623730950488016887242097
				#pragma fragmentoption ARB_precision_hint_fastest
				#pragma multi_compile_fwdbase
				#pragma multi_compile SOFT_DEPTH_OFF SOFT_DEPTH_ON
#pragma multi_compile MAP_TYPE_1 MAP_TYPE_CUBE_1 MAP_TYPE_CUBE2_1 MAP_TYPE_CUBE6_1
#ifndef MAP_TYPE_CUBE2_1
#pragma multi_compile ALPHAMAP_N_1 ALPHAMAP_1
#endif

				#include "noiseSimplex.cginc"
				#include "alphaMap.cginc"
				#include "cubeMap.cginc"

				CUBEMAP_DEF_1(_MainTex)

				sampler2D _Tex;
				sampler2D _DetailTex;
				sampler2D _BumpMap;

				float4x4 _PosRotation;

				float _DetailScale;
				fixed4 _Color;
				float _DistFade;
				float _DistFadeVert;
				float _MinScatter;
				float _Opacity;
				float _InvFade;
				float _Rotation;
				float _MaxScale;
				float4 _NoiseScale;
				float3 _MaxTrans;

				sampler2D _CameraDepthTexture;

				// float4x4 _CameraToWorld;

				struct appdata_t {
					float4 vertex : POSITION;
					fixed4 color : COLOR;
					float3 normal : NORMAL;
					float4 tangent : TANGENT;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f {
					float4 pos : SV_POSITION;
					fixed4 color : COLOR;
					half4 viewDir : TEXCOORD0;
					float2 texcoordZY : TEXCOORD1;
					float2 texcoordXZ : TEXCOORD2;
					float2 texcoordXY : TEXCOORD3;
					float2 uv : TEXCOORD4;
					float4 projPos : TEXCOORD5;
					float3 planetPos : TEXCOORD6;
					float3 viewDirT : TEXCOORD7;
					float3 lightDirT : TEXCOORD8;
				};

				v2f vert (appdata_t v)
				{
					v2f o;
					UNITY_INITIALIZE_OUTPUT(v2f, o);

					float4 origin = mul(unity_ObjectToWorld, float4(0,0,0,1));

					float4 planet_pos = mul(_PosRotation, origin);
					

					float3 normalized = _NoiseScale.z*(planet_pos.xyz);
					float3 hashVect =  .5*(float3(snoise(normalized), snoise(_NoiseScale.x*normalized), snoise(_NoiseScale.y*normalized))+1);

					float4 localOrigin;
					localOrigin.xyz = (2*hashVect-1)*_MaxTrans;
					localOrigin.w = 1;
					float localScale = (hashVect.x*(_MaxScale - 1)) + 1;

					origin = mul(unity_ObjectToWorld, localOrigin);

					planet_pos = mul(_MainRotation, origin);
					float3 detail_pos = mul(_DetailRotation, planet_pos).xyz;
					o.planetPos = planet_pos.xyz;
					o.color = VERT_GET_NO_LOD_CUBE_MAP_1(_MainTex, planet_pos.xyz);

					o.color.rgba *= GetCubeDetailMapNoLOD(_DetailTex, detail_pos, _DetailScale);

					o.viewDir.w = GetDistanceFade(distance(origin, _WorldSpaceCameraPos), _DistFade, _DistFadeVert);
					o.color.a *= o.viewDir.w;
					
					float4x4 M = rand_rotation(
					(float3(frac(_Rotation),0,0))+hashVect,
					localScale,
					localOrigin.xyz);
					float4x4 mvMatrix = mul(mul(UNITY_MATRIX_V, unity_ObjectToWorld), M);

					float3 viewDir = normalize(mvMatrix[2].xyz);
					o.viewDir.xyz = abs(viewDir).xyz;


					float4 mvCenter = mul(UNITY_MATRIX_MV, localOrigin);
					o.pos = mul(UNITY_MATRIX_P,
					mvCenter
					+ float4(v.vertex.xyz*localScale,v.vertex.w));

					float2 texcoodOffsetxy = ((2*v.texcoord)- 1);
					float4 texcoordOffset = float4(texcoodOffsetxy.x, texcoodOffsetxy.y, 0, v.vertex.w);

					float4 ZYv = texcoordOffset.zyxw;
					float4 XZv = texcoordOffset.xzyw;
					float4 XYv = texcoordOffset.xyzw;

					ZYv.z*=sign(-viewDir.x);
					XZv.x*=sign(-viewDir.y);
					XYv.x*=sign(viewDir.z);

					ZYv.x += sign(-viewDir.x)*sign(ZYv.z)*(viewDir.z);
					XZv.y += sign(-viewDir.y)*sign(XZv.x)*(viewDir.x);
					XYv.z += sign(-viewDir.z)*sign(XYv.x)*(viewDir.x);

					ZYv.x += sign(-viewDir.x)*sign(ZYv.y)*(viewDir.y);
					XZv.y += sign(-viewDir.y)*sign(XZv.z)*(viewDir.z);
					XYv.z += sign(-viewDir.z)*sign(XYv.y)*(viewDir.y);

					float2 ZY = mul(mvMatrix, ZYv).xy - mvCenter.xy;
					float2 XZ = mul(mvMatrix, XZv).xy - mvCenter.xy;
					float2 XY = mul(mvMatrix, XYv).xy - mvCenter.xy;

					o.texcoordZY = half2(.5 ,.5) + .6*(ZY);
					o.texcoordXZ = half2(.5 ,.5) + .6*(XZ);
					o.texcoordXY = half2(.5 ,.5) + .6*(XY);


					float3 worldNormal = normalize(mul( unity_ObjectToWorld, float4( v.normal, 0.0 ) ).xyz);
					viewDir = normalize(origin - _WorldSpaceCameraPos);
					//o.color.rgb *= MultiBodyShadow(origin, _SunRadius, _SunPos, _ShadowBodies);
					//o.color.rgb *= Terminator(_WorldSpaceLightPos0, worldNormal);


#ifdef SOFT_DEPTH_ON
					o.projPos = ComputeScreenPos (o.pos);
					COMPUTE_EYEDEPTH(o.projPos.z);
#endif
					//WorldSpaceViewDir(origin).xyz
					half3 normal = normalize(-viewDir);
					float3 tangent = UNITY_MATRIX_V[0].xyz;
					float3 binormal = -cross(normal, normalize(tangent));
					float3x3 rotation = float3x3(tangent.xyz, binormal, normal);

					o.lightDirT = normalize(mul(rotation, _WorldSpaceLightPos0.xyz));
					o.viewDirT = normalize(mul(rotation, viewDir));

					o.uv = v.texcoord;
					return o;
				}

				fixed4 frag (v2f IN) : COLOR
				{

					half4 tex;
					tex.r = tex2D(_Tex, IN.texcoordZY).r;
					tex.g = tex2D(_Tex, IN.texcoordXZ).g;
					tex.b = tex2D(_Tex, IN.texcoordXY).b;

					tex.a = 0;
									
					tex.rgb *= IN.viewDir.rgb;
					half4 vect = half4( IN.viewDir.rgb, 0);
					tex /= vectorSum(vect);

					tex = half4(1, 1, 1, vectorSum(tex));

					half4 color = FRAG_GET_NO_LOD_CUBE_MAP_1(_MainTex, IN.planetPos);
					color = ALPHA_COLOR_1(color);

					color *= _Color * IN.color;

					
					//half3 normT = UnpackNormal(tex2D(_BumpMap, IN.uv));
					half3 normT;
					normT.xy = ((2*IN.uv)-1);
					normT.z = sqrt(1 - saturate(dot(normT.xy, normT.xy)));
					//normT.xy = 2 * INV_PI*asin((2 * IN.uv) - 1) ;
					//normT.xy = sin(PI*(IN.uv-.5));
					//normT.z = 1;
					//color.rg = IN.uv;


					color.a *= tex.a;
					tex.a = IN.viewDir.w*tex.a;
					color.rgb *= ScatterColorLight(IN.lightDirT, IN.viewDirT, normT, tex, _MinScatter, _Opacity, 1).rgb;

#ifdef SOFT_DEPTH_ON
					float depth = UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(IN.projPos)));
					depth = LinearEyeDepth (depth);
					float partZ = IN.projPos.z;
					float fade = saturate (_InvFade * (depth-partZ));
					color.a *= fade;
#endif

					return color;
				}
				ENDCG
			}

		}

	}
}