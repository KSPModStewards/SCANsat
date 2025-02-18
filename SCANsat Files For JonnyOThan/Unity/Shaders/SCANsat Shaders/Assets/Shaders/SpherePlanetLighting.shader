// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "EVE/PlanetLight" {
	Properties{
		_Color("Color Tint", Color) = (1,1,1,1)
		_SpecularColor("Specular tint", Color) = (1,1,1,1)
		_SpecularPower("Shininess", Float) = 0.078125
		_PlanetOpacity("PlanetOpacity", Float) = 1
		_SunPos("_SunPos", Vector) = (0,0,0)
		_SunRadius("_SunRadius", Float) = 1
		_bPos("_bPos", Vector) = (0,0,0)
		_bRadius("_bRadius", Float) = 1
	}
		Category{
		Lighting On
		ZWrite Off
		Cull Back
		Offset 0, 0
		//Blend SrcAlpha OneMinusSrcAlpha
		Blend Zero SrcAlpha
		Tags{
		"Queue" = "Geometry+2"
		"RenderMode" = "Transparent"
		"IgnoreProjector" = "True"
	}
		SubShader{
		Pass{

		Lighting On
		Tags{ "LightMode" = "ForwardBase" }

		CGPROGRAM

#include "EVEUtils.cginc"
#pragma target 3.0
#pragma glsl
#pragma vertex vert
#pragma fragment frag
#pragma fragmentoption ARB_precision_hint_fastest
#pragma multi_compile_fwdbase
#pragma multi_compile MAP_TYPE_1 MAP_TYPE_CUBE_1 MAP_TYPE_CUBE2_1 MAP_TYPE_CUBE6_1

	fixed4 _Color;
	float _SpecularPower;
	half4 _SpecularColor;

	struct appdata_t {
		float4 vertex : POSITION;
		float3 normal : NORMAL;
		float4 tangent : TANGENT;
	};

	struct v2f {
		float4 pos : SV_POSITION;
		float3 worldPos : TEXCOORD0;
	};


	v2f vert(appdata_t v)
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
		return o;
	}


	fixed4 frag(v2f IN) : COLOR
	{
		half4 color = half4(1,1,1,1);

		color.a = MultiBodyShadow(IN.worldPos, _SunRadius, _SunPos, _ShadowBodies);

		return color;
	}
		ENDCG

	}
	}
	}
}