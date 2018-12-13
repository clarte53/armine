Shader "CLARTE/Line/AboveAll/ScreenSpace" 
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
			#pragma geometry geom_screen
			#pragma fragment frag

			ENDCG
		}
	}

	FallBack "Diffuse"
}
