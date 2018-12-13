fixed4x4 _planeMatrix_0;
fixed4x4 _planeMatrix_1;
fixed4x4 _planeMatrix_2;

fixed4	_planeColor_0;
fixed4	_planeColor_1;
fixed4	_planeColor_2;

int nbPlanes;

void clipPlane(half3 vertexPos, inout half3 intersectionColor) {
	#ifdef CLIPPING_PLANE
		fixed4 vertexWorld = fixed4(vertexPos.x, vertexPos.y, vertexPos.z, 1);

		float nbFactor_0 = min(nbPlanes, 1);
		float nbFactor_1 = min(nbPlanes - 1, 1) * max(nbPlanes - 1, 0);
		float nbFactor_2 = min(nbPlanes - 2, 1) * max(nbPlanes - 2, 0);

		float clipResult_0 = mul(_planeMatrix_0, vertexWorld).y * nbFactor_0;
		float clipResult_1 = mul(_planeMatrix_1, vertexWorld).y * nbFactor_1;
		float clipResult_2 = mul(_planeMatrix_2, vertexWorld).y * nbFactor_2;

		float result = max(max(clipResult_0, clipResult_1), clipResult_2);

		clip(-result);

		float factor_0 =  max((1 - nbFactor_0), min(-clipResult_0 / 1, 1));
		float factor_1 =  max((1 - nbFactor_1), min(-clipResult_1 / 1, 1));
		float factor_2 =  max((1 - nbFactor_2), min(-clipResult_2 / 1, 1));

		float factor_x = min(factor_2, min(factor_0, factor_1));
		fixed4 _planeColor_x = max(max((1 - factor_0) * _planeColor_0, (1 - factor_1) * _planeColor_1), (1 - factor_2) * _planeColor_2);

		intersectionColor = (factor_x) * intersectionColor + _planeColor_x;
	#endif
}

void doubleSided(half3 eyeVec, inout half3 normalWorld) {
	// Switch the normal if necessary
	normalWorld = normalWorld * sign(dot(-eyeVec, normalWorld));
}

