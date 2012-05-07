//
// DownSampleEffect.fx
// Used to downsample a texture, blurring it
//

float2 xOffset;

// -----Textures-----

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

float4 packColor10(float4 color)
{
	color.xyz /= 4;
	return color;
}

float4 unpackColor10(float4 color)
{
	return color * 4;
}

VSout VertexShaderFunction(VSin input)
{
    VSout output;

    output.position = input.position;
	output.texCoord = input.texCoord;

    return output;
}

float4 PixelShaderFunction(VSout input) : COLOR0
{
    float4 output;

	// blur
	float offset = 0.001;
	output  = tex2D(sceneSampler, input.texCoord + float2( 1,-1) * offset);
	output += tex2D(sceneSampler, input.texCoord + float2(-1,-1) * offset);
	output += tex2D(sceneSampler, input.texCoord + float2(-1, 1) * offset);
	output += tex2D(sceneSampler, input.texCoord + float2( 1, 1) * offset);
	
	//return output / 4;
	return tex2D(sceneSampler, input.texCoord + xOffset);
}

technique HDR
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
