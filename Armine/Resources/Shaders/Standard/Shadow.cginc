#include "UnityStandardShadow.cginc"
#include "Clarte.cginc"

void vertShadowCasterClarte(
	VertexInput v,
#if UNITY_VERSION >= 201720
		out float4 opos : SV_POSITION
	#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
		, out VertexOutputShadowCaster o
	#endif
	#ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
		, out VertexOutputStereoShadowCaster os
	#endif
	#ifdef CLIPPING_PLANE
		, out half3 posWorld : TEXCOORD2
	#endif
#else
	#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
		out VertexOutputShadowCaster o,
	#endif
	#ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
		out VertexOutputStereoShadowCaster os,
	#endif
		out float4 opos : SV_POSITION
	#ifdef CLIPPING_PLANE
		, out half3 posWorld : TEXCOORD2
	#endif
#endif
)
{
	vertShadowCaster(
	#if UNITY_VERSION >= 201720
			v
			, opos
		#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
			, o
		#endif
		#ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
			, os
		#endif
	#else
			v,
		#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
			o,
		#endif
		#ifdef UNITY_STANDARD_USE_STEREO_SHADOW_OUTPUT_STRUCT
			os,
		#endif
			opos
	#endif
	);
	
	#ifdef CLIPPING_PLANE
		#if UNITY_VERSION >= 530
			posWorld = mul(unity_ObjectToWorld, v.vertex);
		#else
			posWorld = mul(_ObjectToWorld, v.vertex);
		#endif
	#endif
}

half4 fragShadowCasterClarte(
#if UNITY_VERSION >= 201720
	UNITY_POSITION(vpos)
	#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
		, VertexOutputShadowCaster i
	#endif
	#ifdef CLIPPING_PLANE
		, half3 posWorld : TEXCOORD2
	#endif
#elif UNITY_VERSION >= 201710
	#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
		VertexOutputShadowCaster i,
	#endif
	UNITY_POSITION(vpos)
	#ifdef CLIPPING_PLANE
		, half3 posWorld : TEXCOORD2
	#endif
#else
	#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
		VertexOutputShadowCaster i
	#endif
	#ifdef UNITY_STANDARD_USE_DITHER_MASK
	, UNITY_VPOS_TYPE vpos : VPOS
	#endif
	#ifdef CLIPPING_PLANE
		#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
		,
		#endif
		half3 posWorld : TEXCOORD2
	#endif
#endif
) : SV_Target
{
	#ifdef CLIPPING_PLANE
		half3 c;
		clipPlane(posWorld, c);
	#endif
	
	return fragShadowCaster(
	#if UNITY_VERSION >= 201720
			vpos
		#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
			, i
		#endif
	#elif UNITY_VERSION >= 201710
		#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
			i,
		#endif
			vpos
	#else
		#ifdef UNITY_STANDARD_USE_SHADOW_OUTPUT_STRUCT
			i
		#endif
		#ifdef UNITY_STANDARD_USE_DITHER_MASK
			, vpos
		#endif
	#endif
	);
}
