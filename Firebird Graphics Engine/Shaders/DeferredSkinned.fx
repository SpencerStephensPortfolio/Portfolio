//
// DeferredSkinned.fx
// Deferred shading from source polygons to deferred MRT textures.
// Includes vertex skin transformations from bones
//

#define SKINNED_EFFECT_MAX_BONES   72



// -----Shader inputs-----

#include "Emissive.fxh"

float4x4 xViewProjection;
float4x4 xWorld;
float4x4 xBone;
float3 xColor;

float4x3 xBones[SKINNED_EFFECT_MAX_BONES];

// global effect parameters

// -----Pixel input/output data structures

struct VSin
{
    float4 position : POSITION0;
	float3 normal : NORMAL0;
	int4   indices : BLENDINDICES0;
    float4 weights : BLENDWEIGHT0;
};

struct VSout
{
    float4 position : POSITION0;
	float3 normal : TEXCOORD0;
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

void Skin(inout VSin vin, uniform int boneCount)
{
    float4x3 skinning = 0;

    [unroll]
    for (int i = 0; i < boneCount; i++)
    {
        skinning += xBones[vin.indices[i]] * vin.weights[i];
    }

    vin.position.xyz = mul(vin.position, skinning);
    vin.normal = mul(vin.normal, (float3x3)skinning);
}

// -----Shaders-----

// Vertex Shader
VSout VertexShaderFunction(VSin input)
{
    VSout output;

	Skin(input, 4);

	float4x4 world = xWorld; //mul(xBone, xWorld);

    output.position = mul(mul(input.position, world), xViewProjection);
	output.normal = normalize(mul(input.normal, (float3x3)world));
	output.depth = output.position.zw;

    return output;
}

// Pixel shader
PSout PixelShaderFunction(VSout input)
{
    PSout output;

	output.albedo = packColor10(float4(xColor, EmissiveContribution()));
	output.depth = input.depth.x / input.depth.y;
	output.normal = float4(normalize(input.normal).xy, 0, 0);

    return output;
}

// Pixel shader
float4 LightPixelShaderFunction(VSout input) : COLOR0
{
    return float4(input.depth.x / input.depth.y, 0, 0, 1);
}

technique DeferredSkinned
{
    pass Model
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}

technique DeferredSkinnedLightMap
{
    pass Model
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 LightPixelShaderFunction();
    }
}

