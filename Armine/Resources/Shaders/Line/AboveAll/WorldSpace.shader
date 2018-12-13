Shader "CLARTE/Line/AboveAll/WorldSpace" 
{
	Properties 
	{
		_LineWidth ("Line Width", Range(0.1, 20.0)) = 2.0
		_Color ("Line Color", Color) = (1, 1, 1, 1)
	}

	SubShader
	{
		Tags { "Queue" = "Transparent" }
	
		Ztest Always
		Blend SrcAlpha OneMinusSrcAlpha, SrcAlpha DstAlpha
		Cull Off

		Pass 
		{
			CGPROGRAM

			#include "../Line.cginc"

			#pragma only_renderers d3d11

			#pragma vertex vert
			#pragma geometry geom_world
			#pragma fragment frag

			ENDCG
		}
	}

	FallBack "Diffuse"
}