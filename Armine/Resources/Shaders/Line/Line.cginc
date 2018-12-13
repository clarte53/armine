#include "UnityCG.cginc"

float _LineWidth;
fixed4 _Color;

struct VsInput
{
	float4 pos: POSITION;
};

struct GsInput
{
	float4 pos: POSITION;
};

struct PsInput
{
	float4 pos: SV_POSITION;
};

float2 projToWindow(in float4 pos)
{
	return float2(_ScreenParams.x * 0.5 * (1.0 + pos.x / pos.w), _ScreenParams.y * 0.5 * (1.0 - pos.y / pos.w));
}

GsInput vert(VsInput input)
{
	GsInput output;
#if UNITY_VERSION >= 550
	output.pos = UnityObjectToClipPos(input.pos);
#else
	float4x4 mvp = UNITY_MATRIX_MVP; // To avoid automatic code conversion by unity
	output.pos = mul(mvp, input.pos);
#endif
	return output;
}

float4 screenBorderIntersection(float4 pos, float4 end)
{
	float3 p0 = pos.xyz / pos.w;
	float3 p1 = end.xyz / end.w;

	float3 dir = normalize(p1 - p0);

	float k = 0;
	
	if(p0.x > 1)
	{
		//k = (1 - p0.x) / dir.x;
	}
	else if(p0.x < -1)
	{
		//k = (-1 - p0.x) / dir.x;
	}
	
	p0 = p0 + k * dir;
	
	k = 0;
	
	if(p0.y > 1)
	{
		//k = (1 - p0.y) / dir.y;
	}
	else if(p0.y < -1)
	{
		//k = (-1 - p0.y) / dir.y;
	}
	
	p0 = p0 + k * dir;
	
	return float4(p0 * pos.w, pos.w);
}

[maxvertexcount(4)]
void geom_screen(line GsInput input[2], inout TriangleStream<PsInput> stream)
{
	PsInput v;

	float4 p0 = input[0].pos;
	float4 p1 = input[1].pos;

	if(p0.z < 0 && p1.z < 0)
	{
		return;
	}

	float2 dir = normalize(projToWindow(p1) - projToWindow(p0));
	
	dir *= 2.0 * 0.001 * 0.5 * _LineWidth; // Set line width
	dir.y /= (_ScreenParams.x / _ScreenParams.y); // Normalize base on screen ratio to avoid deformations

	float4 norm_offset = float4(dir.y, dir.x, 0.0, 0.0); // Main offset to construct billboards: 90° rotation from line direction

	v.pos = p0 - norm_offset * p0.z;
	stream.Append(v);
	
	v.pos = p1 - norm_offset * p1.z;
	stream.Append(v);

	v.pos = p0 + norm_offset * p0.z;
	stream.Append(v);
	
	v.pos = p1 + norm_offset * p1.z;
	stream.Append(v);
	
	stream.RestartStrip();
}

[maxvertexcount(4)]
void geom_world(line GsInput input[2], inout TriangleStream<PsInput> stream)
{
	PsInput v;

	float4 p0 = input[0].pos;
	float4 p1 = input[1].pos;

	if(p0.z < 0 && p1.z < 0)
	{
		return;
	}

	float2 dir = normalize(projToWindow(p1) - projToWindow(p0));

	dir *= 0.001 * 0.5 * _LineWidth; // Set line width
	dir.y /= (_ScreenParams.x / _ScreenParams.y); // Normalize base on screen ratio to avoid deformations

	float4 norm_offset = float4(dir.y, dir.x, 0.0, 0.0); // Main offset to construct billboards: 90° rotation from line direction

	v.pos = p0 - norm_offset;
	stream.Append(v);
	
	v.pos = p1 - norm_offset;
	stream.Append(v);

	v.pos = p0 + norm_offset;
	stream.Append(v);
	
	v.pos = p1 + norm_offset;
	stream.Append(v);
	
	stream.RestartStrip();
}

fixed4 frag(PsInput input) : SV_Target
{
	return _Color;
}
