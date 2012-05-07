//
// Fog.fx
// Multiplies fog onto the scene.
//

// -----Textures-----

Texture xDepthTex;
sampler depthSampler = sampler_state
{
	texture = <xDepthTex>;
	magfilter = POINT;
	minfilter = POINT;
	mipfilter = POINT;
	AddressU = clamp;
	AddressV = clamp;
};

// -----Settings and Constants-----

float4x4 xInverseViewProjection;

float3 xOuterColor;
float3 xCenterColor;
float xFogBegin, xFogEnd;

const float zNear = 20, zFar = 800;

// -----Pixel input/output data structures-----

struct VSin
{
    float4 position : POSITION0;
	float2 texCoord : TEXCOORD0;
};

struct VSout
{
    float4 position : POSITION0;
	float2 position2D : TEXCOORD1;
	float2 texCoord : TEXCOORD0;
};

// -----Helper methods-----

// Calculates world position from depth and texture coordinate information.
float4 unpackPosition(float2 texCoord, float depth)
{
	float4 projectedPos;
	projectedPos.x = texCoord.x * 2 - 1;
	projectedPos.y = (1 - texCoord.y) * 2 - 1;
	projectedPos.z = depth;
	projectedPos.w = 1.0;

	float4 newPosition = mul(projectedPos, xInverseViewProjection);
	return newPosition / newPosition.w;
}

// -----Shaders-----

VSout VertexShaderFunction(VSin input)
{
    VSout output;

    output.position = input.position;
	output.texCoord = input.texCoord;
	output.position2D = output.position.xy / output.position.w;

    return output;
}

float4 PixelShaderFunction(VSout input, uniform int enableFog) : COLOR0
{
	float4 output;

	float depth = tex2D(depthSampler, input.texCoord).x;
	if (depth > 1.0)
		return float4(lerp(xCenterColor, xOuterColor, saturate(length(input.position2D) * 1.4)), 1);

	if (enableFog)
	{
		// Apply fog
		float position = unpackPosition(input.texCoord, depth).z;
		float intensity = saturate(-(position - xFogBegin) / (xFogEnd - xFogBegin));

		return float4(xOuterColor, intensity);
	}
	else
	{
		return 0;
	}
}

technique Fog
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction(1);
    }
}

technique NoFog
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction(0);
    }
}