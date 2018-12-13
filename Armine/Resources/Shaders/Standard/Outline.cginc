#include "UnityStandardCore.cginc"
#include "Core.cginc"

uniform float _Outline;
uniform float4 _OutlineColor;

VertexOutputForwardAddClarte vertForwardOutline(VertexInput v)
{
	VertexOutputForwardAddClarte o = (VertexOutputForwardAddClarte)0;
	o.data = vertForwardAdd(v);
#if UNITY_VERSION >= 550
	o.data.pos = UnityObjectToClipPos (v.vertex);
#else
	float4x4 mvp = UNITY_MATRIX_MVP; // To avoid automatic code conversion by unity
	o.data.pos = mul(mvp, v.vertex);
#endif

	float3 norm   = normalize(mul ((float3x3)UNITY_MATRIX_IT_MV, v.normal));
	float2 offset = TransformViewToProjection(norm.xy);
	o.data.pos.xy += offset * o.data.pos.z * _Outline;

	#ifdef CLIPPING_PLANE
		#if UNITY_VERSION >= 530
			o.posWorld = mul(unity_ObjectToWorld, v.vertex);
		#else
			o.posWorld = mul(_ObjectToWorld, v.vertex);
		#endif
	#endif
    return o;
}

half4 fragForwardOutline(VertexOutputForwardAddClarte d) : SV_Target
{
	VertexOutputForwardAdd i = d.data;
	FRAGMENT_SETUP_FWDADD(s)

	#ifdef CLIPPING_PLANE
		clipPlane(d.posWorld, s.diffColor);
	#endif

	return _OutlineColor;
}
