//
// DeferredInstanced.fx
// Deferred shading from source polygons to deferred MRT textures using hardware
// instancing.
//

// -----Shader inputs-----

#include "Emissive.fxh"

float4x4 xViewProjection;
float3 xColor;

float xTextureOffset; // for waterfall

Texture xTexture;
sampler textureSampler = sampler_state
{
	texture = <xTexture>;
	magfilter = LINEAR;
	minfilter = LINEAR;
	AddressU = wrap;
	AddressV = wrap;
};


// -----Pixel input/output data structures

struct VSinTextured
{
    float4 position : POSITION0;
	float2 texCoord : TEXCOORD0;
	float3 normal : NORMAL0;
};

struct VSoutTextured
{
    float4 position : POSITION0;
	float3 normal : TEXCOORD2;
	float2 texCoord : TEXCOORD0;
	float2 depth : TEXCOORD1;
};

struct VSinFlat
{
    float4 position : POSITION0;
	float3 normal : NORMAL0;
};

struct VSoutFlat
{
    float4 position : POSITION0;
	float3 normal : TEXCOORD2;
	float2 depth : TEXCOORD1;
};

struct PSout
{
	float4 albedo : COLOR0;
	float4 depth : COLOR1;
	float4 normal : COLOR2;
};

// -----Functions-----

float4 packColor10(float4 color)
{
	color.rgb /= 4;
	return color;
}

float4 unpackColor10(float4 color)
{
	return color * 4;
}

// -----Shaders-----

// Vertex Shader
VSoutTextured TexturedVertexShaderFunction(VSinTextured input, float4x4 instanceTransform : BLENDWEIGHT)
{
    VSoutTextured output;

	float4x4 transformT = transpose(instanceTransform);
    output.position = mul(mul(input.position, transformT), xViewProjection);
	output.normal = normalize(mul(input.normal, (float3x3)transformT));
	output.depth = output.position.zw;

	output.texCoord = input.texCoord - float2(0, xTextureOffset);

    return output;
}

// Pixel shader
PSout TexturedPixelShaderFunction(VSoutTextured input)
{
    PSout output;

	output.albedo = packColor10(float4(tex2D(textureSampler, input.texCoord).rgb, EmissiveContribution(input.texCoord)));
	output.depth = input.depth.x / input.depth.y;
	output.normal = float4(normalize(input.normal).xy, 0, 0);

    return output;
}

// Vertex Shader
VSoutFlat FlatVertexShaderFunction(VSinFlat input, float4x4 instanceTransform : BLENDWEIGHT)
{
    VSoutFlat output;

    output.position = mul(mul(input.position, transpose(instanceTransform)), xViewProjection);
	output.normal = normalize(mul(input.normal, (float3x3)instanceTransform));
	output.depth = output.position.zw;

    return output;
}

// Pixel shader
PSout FlatPixelShaderFunction(VSoutFlat input)
{
    PSout output;

	output.albedo = packColor10(float4(xColor, EmissiveContribution()));
	output.depth = input.depth.x / input.depth.y;
	output.normal = float4(normalize(input.normal).xy, 0, 0);

    return output;
}

// Pixel shader
float4 LightPixelShaderFunction(VSoutFlat input) : COLOR0
{
	return float4(input.depth.x / input.depth.y, 0, 0, 1);
}

technique DeferredInstancedTextured
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 TexturedVertexShaderFunction();
        PixelShader = compile ps_2_0 TexturedPixelShaderFunction();
    }
}

technique DeferredInstanced
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 FlatVertexShaderFunction();
        PixelShader = compile ps_2_0 FlatPixelShaderFunction();
    }
}

technique DeferredInstancedLight
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 FlatVertexShaderFunction();
        PixelShader = compile ps_2_0 LightPixelShaderFunction();
    }
}