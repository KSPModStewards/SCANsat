// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "Hidden/EdgeDetectColors" { 
	Properties {
		_MainTex ("Base (RGB)", 2D) = "" {}
	  _RampTex ("Base (RGB)", 2D) = "grayscaleRamp" {}
	}
	
Subshader {
  Pass {
	  ZTest Always Cull Off ZWrite Off
    Fog { Mode off }

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
	    #include "UnityCG.cginc"
	
	struct v2f {
		float4 pos : SV_POSITION;
		float2 uv[5] : TEXCOORD0;
	};
	
	sampler2D _MainTex;
	uniform float4 _MainTex_TexelSize;

	sampler2D _CameraDepthNormalsTexture;

	//uniform half4 _Sensitivity; 
	//uniform half _SampleDistance;
  uniform sampler2D _RampTex;

	inline half CheckSame (half2 centerNormal, float centerDepth, half4 theSample)
	{
		// difference in normals
		// do not bother decoding normals - there's no need here

    half depth = 0.75;
    half normal = 0.75;

		half2 diff = abs(centerNormal - theSample.xy) * normal;
		half isSameNormal = (diff.x + diff.y) * normal < 0.1;
		// difference in depth
		float sampleDepth = DecodeFloatRG (theSample.zw);
		float zdiff = abs(centerDepth-sampleDepth);
		// scale the required threshold by the distance
		half isSameDepth = zdiff * depth < 0.09 * centerDepth;
	
		// return:
		// 1 - if normals and depth are similar enough
		// 0 - otherwise
		
		return isSameNormal * isSameDepth;
	}	
		
	v2f vert( appdata_img v ) 
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		
		float2 uv = v.texcoord.xy;
		o.uv[0] = uv;
		#if UNITY_UV_STARTS_AT_TOP
		if (_MainTex_TexelSize.y < 0)
			uv.y = 1-uv.y; //colour not here.. but this shits things kind of..
		#endif

    half dist = 0.75;
				
		//colours not in here?
		o.uv[1] = uv + _MainTex_TexelSize.xy * half2(1,1) * dist;
		o.uv[2] = uv + _MainTex_TexelSize.xy * half2(-1,-1) * dist;
		o.uv[3] = uv + _MainTex_TexelSize.xy * half2(-1,1) * dist;
		o.uv[4] = uv + _MainTex_TexelSize.xy * half2(1,-1) * dist;
		return o;
	}

	half4 frag(v2f i) : SV_Target {				
		half4 sample1 = tex2D(_CameraDepthNormalsTexture, i.uv[1].xy);
		half4 sample2 = tex2D(_CameraDepthNormalsTexture, i.uv[2].xy);
		half4 sample3 = tex2D(_CameraDepthNormalsTexture, i.uv[3].xy);
		half4 sample4 = tex2D(_CameraDepthNormalsTexture, i.uv[4].xy);

		half edge = 1.0;
		
		edge *= CheckSame(sample1.xy, DecodeFloatRG(sample1.zw), sample2);
		edge *= CheckSame(sample3.xy, DecodeFloatRG(sample3.zw), sample4);

    half4 col = half4(0,0,0,1);

		 if(edge > 0)
         col = tex2D(_MainTex, i.uv[0].xy);

	   fixed grayscale = Luminance(col.rgb);
	   half2 remap = half2 (grayscale, .5);
	   fixed4 output = tex2D(_RampTex, remap);
	   output.a = col.a;
	   return output;
	}
	
	ENDCG

  }
}

Fallback off

}