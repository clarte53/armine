#include "UnityStandardCore.cginc"
#include "Clarte.cginc"

half4 fragForwardBaseClarte(VertexOutputForwardBase i) : SV_Target {
	FRAGMENT_SETUP(s)
#if UNITY_VERSION >= 560
	UNITY_SETUP_INSTANCE_ID(i);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
#elif UNITY_VERSION >= 550
#if UNITY_OPTIMIZE_TEXCUBELOD
	s.reflUVW		= i.reflUVW;
#endif
#endif

	clipPlane(s.posWorld, s.diffColor);
	doubleSided(s.eyeVec, s.normalWorld);

#if UNITY_VERSION >= 550
	UnityLight mainLight = MainLight ();
#else
	UnityLight mainLight = MainLight (s.normalWorld);
#endif
#if UNITY_VERSION >= 560
	UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld);
#else
	half atten = SHADOW_ATTENUATION(i);
#endif


	half occlusion = Occlusion(i.tex.xy);
#if UNITY_VERSION >= 550
	UnityGI gi = FragmentGI (s, occlusion, i.ambientOrLightmapUV, atten, mainLight);

	half4 c = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
#else
	UnityGI gi = FragmentGI (s.posWorld, occlusion, i.ambientOrLightmapUV, atten, s.oneMinusRoughness, s.normalWorld, s.eyeVec, mainLight);

	half4 c = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect);
#endif
#if UNITY_VERSION >= 560
	// Nothing to do
#elif UNITY_VERSION >= 550
	c.rgb += UNITY_BRDF_GI (s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, occlusion, gi);
#else
	c.rgb += UNITY_BRDF_GI (s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, occlusion, gi);
#endif
	c.rgb += Emission(i.tex.xy);

	UNITY_APPLY_FOG(i.fogCoord, c.rgb);
	return OutputForward (c, s.alpha);
}

struct VertexOutputForwardAddClarte
{
	VertexOutputForwardAdd data;

	#ifdef CLIPPING_PLANE
		float3 posWorld : TEXCOORD9;
	#endif
};

VertexOutputForwardAddClarte vertForwardAddClarte(VertexInput v)
{
	VertexOutputForwardAddClarte o = (VertexOutputForwardAddClarte)0;
	
	o.data = vertForwardAdd(v);

	#ifdef CLIPPING_PLANE
		#if UNITY_VERSION >= 530
			o.posWorld = mul(unity_ObjectToWorld, v.vertex);
		#else
			o.posWorld = mul(_ObjectToWorld, v.vertex);
		#endif
	#endif
	
	return o;
}

half4 fragForwardAddClarte(VertexOutputForwardAddClarte d) : SV_Target
{
	VertexOutputForwardAdd i = d.data;

	FRAGMENT_SETUP_FWDADD(s)
	
	#ifdef CLIPPING_PLANE
		clipPlane(d.posWorld, s.diffColor);
	#endif
	doubleSided(s.eyeVec, s.normalWorld);

#if UNITY_VERSION >= 560
	UNITY_LIGHT_ATTENUATION(atten, i, s.posWorld)
	UnityLight light = AdditiveLight (IN_LIGHTDIR_FWDADD(i), atten);
#elif UNITY_VERSION >= 550
	UnityLight light = AdditiveLight (IN_LIGHTDIR_FWDADD(i), LIGHT_ATTENUATION(i));
#else
	UnityLight light = AdditiveLight (s.normalWorld, IN_LIGHTDIR_FWDADD(i), LIGHT_ATTENUATION(i));
#endif
	UnityIndirect noIndirect = ZeroIndirect ();

#if UNITY_VERSION >= 550
	half4 c = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, light, noIndirect);
#else
	half4 c = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, light, noIndirect);
#endif

	UNITY_APPLY_FOG_COLOR(i.fogCoord, c.rgb, half4(0,0,0,0)); // fog towards black in additive pass
	return OutputForward (c, s.alpha);
}

void fragDeferredClarte(
	VertexOutputDeferred i,
	out half4 outGBuffer0 : SV_Target0,
	out half4 outGBuffer1 : SV_Target1,
	out half4 outGBuffer2 : SV_Target2,
	out half4 outEmission : SV_Target3			// RT3: emission (rgb), --unused-- (a)
)
{
	#if (SHADER_TARGET < 30)
		outGBuffer0 = 1;
		outGBuffer1 = 1;
		outGBuffer2 = 0;
		outEmission = 0;
		return;
	#endif

	FRAGMENT_SETUP(s)
#if UNITY_VERSION >= 550
#if UNITY_OPTIMIZE_TEXCUBELOD
	s.reflUVW		= i.reflUVW;
#endif
#endif
	
	clipPlane(s.posWorld, s.diffColor);
	doubleSided(s.eyeVec, s.normalWorld);
	
	// no analytic lights in this pass
#if UNITY_VERSION >= 550
	UnityLight dummyLight = DummyLight ();
#else
	UnityLight dummyLight = DummyLight (s.normalWorld);
#endif
	half atten = 1;

	// only GI
	half occlusion = Occlusion(i.tex.xy);
#if UNITY_VERSION >= 550
#if UNITY_ENABLE_REFLECTION_BUFFERS
	bool sampleReflectionsInDeferred = false;
#else
	bool sampleReflectionsInDeferred = true;
#endif

	UnityGI gi = FragmentGI (s, occlusion, i.ambientOrLightmapUV, atten, dummyLight, sampleReflectionsInDeferred);

	half3 emissiveColor = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect).rgb;
	emissiveColor += UNITY_BRDF_GI (s.diffColor, s.specColor, s.oneMinusReflectivity, s.smoothness, s.normalWorld, -s.eyeVec, occlusion, gi);
#else
	UnityGI gi = FragmentGI (s.posWorld, occlusion, i.ambientOrLightmapUV, atten, s.oneMinusRoughness, s.normalWorld, s.eyeVec, dummyLight);

	half3 emissiveColor = UNITY_BRDF_PBS (s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, gi.light, gi.indirect).rgb;
	emissiveColor += UNITY_BRDF_GI (s.diffColor, s.specColor, s.oneMinusReflectivity, s.oneMinusRoughness, s.normalWorld, -s.eyeVec, occlusion, gi);
#endif

	#ifdef _EMISSION
		emissiveColor += Emission (i.tex.xy);
	#endif

	#ifndef UNITY_HDR_ON
		emissiveColor.rgb = exp2(-emissiveColor.rgb);
	#endif

#if UNITY_VERSION >= 550
	UnityStandardData data;
	data.diffuseColor	= s.diffColor;
	data.occlusion		= occlusion;		
	data.specularColor	= s.specColor;
	data.smoothness		= s.smoothness;	
	data.normalWorld	= s.normalWorld;

	UnityStandardDataToGbuffer(data, outGBuffer0, outGBuffer1, outGBuffer2);
#else
	outGBuffer0 = half4(s.diffColor, occlusion);
	outGBuffer1 = half4(s.specColor, s.oneMinusRoughness);
	outGBuffer2 = half4(s.normalWorld*0.5+0.5,1);
#endif

	// Emisive lighting buffer
	outEmission = half4(emissiveColor, 1);
}
