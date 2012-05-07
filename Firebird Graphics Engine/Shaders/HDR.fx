//
// HDR.fx
// Post-process shading from source polygons to deferred MRT textures.
//

// -----------------------------------------------------------------
// Variables
// -----------------------------------------------------------------

float2 xOffset; // texture coordinate offset; modified by screen resolution.
float2 xUvOffset; // multiplicative adjustment for texture coordinates

float xTonemapScene2;

// -----------------------------------------------------------------
// Textures
// -----------------------------------------------------------------

Texture xSceneTex;
sampler sceneSampler = sampler_state
{
	texture = <xSceneTex>;
	magfilter = LINEAR;
	minfilter = LINEAR;
	mipfilter = POINT;
	AddressU = clamp;
	AddressV = clamp;
};

// -----------------------------------------------------------------
// Input structures
// -----------------------------------------------------------------

struct VSin
{
    float4 position : POSITION0;
	float2 texCoord : TEXCOORD0;
};

struct VSout
{
    float4 position : POSITION0;
	float2 texCoord : TEXCOORD0;
};

// -----------------------------------------------------------------
// Helpers
// -----------------------------------------------------------------

float4 packColor10(float4 color)
{
	color.xyz /= 4;
	return color;
}

float4 unpackColor10(float4 color)
{
	color.rgb *= 4;
	return color;
}

// -----------------------------------------------------------------
// Shaders - VS
// -----------------------------------------------------------------

VSout VSFunction(VSin input)
{
    VSout output;

    output.position = input.position;
	output.texCoord = input.texCoord;

    return output;
}

// -----------------------------------------------------------------
// Shaders - DownsamplePS
// -----------------------------------------------------------------

float4 DownsamplePS(VSout input, uniform int unpack) : COLOR0
{
	float4 output;

	float2 offset = xOffset;

	// Sample output
	const float off = 1/2;
	output  = tex2D(sceneSampler, input.texCoord + xOffset * float2( off, off) + offset);
	output += tex2D(sceneSampler, input.texCoord + xOffset * float2( off, -off) + offset);
	output += tex2D(sceneSampler, input.texCoord + xOffset * float2(-off, -off) + offset);
	output += tex2D(sceneSampler, input.texCoord + xOffset * float2(-off, off) + offset);
	output /= 4;

	if (unpack)
	{
		// bloom curve
		float intensity = dot(output.rgb  * 4, (float3)0.3333);
		float bloom_intensity = clamp((intensity - 1) * 4 / 3, 0, 4);
		output.rgb *= bloom_intensity / intensity / 4;
	}

	return saturate(output);
}

// -----------------------------------------------------------------
// Shaders - BlurAndAccumPS
// -----------------------------------------------------------------

float4 BlurPS(VSout input, uniform int blurDir) : COLOR0
{
	// Gaussian distribution
	const float samples[] = { 0.061, 0.242, 0.383, 0.242, 0.061 };

    float4 output = 0;

	// 5-sample 1D gaussian blur
	[unroll]
	for (int i = -2; i <= 2; i++)
	{
		float2 texCoord = input.texCoord;
		texCoord += i * xOffset * ( blurDir ? float2(0,1) : float2(1,0) );
		output += tex2D(sceneSampler, texCoord) * samples[i+2];
	}

	return output;
}

// -----------------------------------------------------------------
// Shaders - TonemapPS
// -----------------------------------------------------------------

float4 TonemapPS(VSout input) : COLOR0
{
	float4 output = unpackColor10(tex2D(sceneSampler, input.texCoord));

	// Tone-mapping function
	//output.rgb /= max(1, max(output.r, max(output.b, output.g)));

	return output;
}

// -----------------------------------------------------------------
// Shaders - MovePS
// -----------------------------------------------------------------

float4 MovePS(VSout input) : COLOR0
{
	float4 output = tex2D(sceneSampler, input.texCoord);
	
	return output;
}

// -----------------------------------------------------------------
// Techniques
// -----------------------------------------------------------------

technique Downsample
{
	pass Pass1
	{
		VertexShader = compile vs_1_1 VSFunction();
		PixelShader = compile ps_2_0 DownsamplePS(0);
	}
}

technique DownsampleCutoff
{
	pass Pass1
	{
		VertexShader = compile vs_1_1 VSFunction();
		PixelShader = compile ps_2_0 DownsamplePS(1);
	}
}

technique BlurVertical
{
	pass Pass1
	{
		VertexShader = compile vs_1_1 VSFunction();
        PixelShader = compile ps_2_0 BlurPS(0); // 0 = vertical
	}
}

technique BlurHorizontal
{
    pass Pass1
    {
        VertexShader = compile vs_1_1 VSFunction();
        PixelShader = compile ps_2_0 BlurPS(1); // 1 = horizontal
    }
}

technique Move
{
	pass Pass1
    {
        VertexShader = compile vs_1_1 VSFunction();
        PixelShader = compile ps_2_0 MovePS();
    }
}

technique Tonemap
{
	pass Pass1
	{
		VertexShader = compile vs_1_1 VSFunction();
        PixelShader = compile ps_2_0 TonemapPS();
	}
}