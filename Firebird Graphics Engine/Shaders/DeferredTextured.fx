//
// Deferred.fx
// Deferred shading from source polygons to deferred MRT textures.
//

// -----Shader inputs-----

#include "Emissive.fxh"

float4x4 xViewProjection;
float4x4 xWorld;
float4x4 xBone;
float3 xColor;

Texture xTexture;
sampler textureSampler = sampler_state
{
	texture = <xTexture>;
	magfilter = LINEAR;
	minfilter = LINEAR;
	AddressU = mirror;
	AddressV = mirror;
};


// -----Pixel input/output data structures

struct VSin
{
    float4 position : POSITION0;
	float2 texCoord : TEXCOORD0;
	float3 normal : NORMAL0;
};

struct VSout
{
    float4 position : POSITION0;
	float3 normal : TEXCOORD2;
	float2 texCoord : TEXCOORD0;
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
VSout VertexShaderFunction(VSin input)
{
    VSout output;

	float4x4 world = mul(xBone, xWorld);

    output.position = mul(mul(input.position, world), xViewProjection);
	output.normal = normalize(mul(input.normal, (float3x3)world));
	output.texCoord = input.texCoord;
	output.depth = output.position.zw;

    return output;
}

// Pixel shader
PSout PixelShaderFunction(VSout input)
{
    PSout output;

	output.albedo = packColor10(float4(
		xColor+ tex2D(textureSampler, input.texCoord).rgb,
		EmissiveContribution(input.texCoord)));

	output.depth = input.depth.x / input.depth.y;
	output.normal = float4(normalize(input.normal).xy, 0, 0);

    return output;
}

technique DeferredTextured
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
