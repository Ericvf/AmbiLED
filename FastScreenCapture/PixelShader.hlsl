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
};

Texture2D captureTexture : register( t0 );
SamplerState linearSampler : register( s0 );

struct PS_INPUT
{
    float4 Pos : SV_POSITION;
    float2 Tex : TEXCOORD;
};

//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------
float4 PS(PS_INPUT input) : SV_Target
{
	if (isSBS) input.Tex.x *= 0.5;
	else if (isHOU) input.Tex.y *= 0.5;

	float4 color = captureTexture.Sample(linearSampler, input.Tex);
	color.rgb *= brightness;
	
	return color;
} 