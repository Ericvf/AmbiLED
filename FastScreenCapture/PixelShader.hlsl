// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved
//----------------------------------------------------------------------

cbuffer PS_CONSTANT_BUFFER : register(b0)
{
	float isSBS;
	float isHOU;
	float brightness;
	float saturation;
};

Texture2D captureTexture : register( t0 );
SamplerState linearSampler : register( s0 );

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float2 Tex : TEXCOORD;
};

// https://github.com/AnalyticalGraphicsInc/cesium/blob/master/Source/Shaders/Builtin/Functions/saturation.glsl
float3 ps_saturation(float3 rgb, float adjustment)
{
	// Algorithm from Chapter 16 of OpenGL Shading Language
	const float3 W = float3(0.2125, 0.7154, 0.0721);
	float3 intensity = dot(rgb, W);
	return lerp(intensity, rgb, adjustment);
}

//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------
float4 PS(PS_INPUT input) : SV_Target
{
	if (isSBS) input.Tex.x *= 0.5;
	else if (isHOU) input.Tex.y *= 0.5;

	float4 color = captureTexture.Sample(linearSampler, input.Tex);
	color.rgb *= brightness;
	color.rgb = ps_saturation(color.rgb, saturation);

	return color;
} 

