//
// DirectionalLight.fx
// Applies lighting contribution from a directional light.
//

// -----Textures-----

Texture xAlbedoTex;
sampler albedoSampler = sampler_state
{
	texture = <xAlbedoTex>;
	magfilter = POINT;
	minfilter = POINT;
	mipfilter = POINT;
	AddressU = clamp;
	AddressV = clamp;
};

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

Texture xNormalTex;
sampler normalSampler = sampler_state
{
	texture = <xNormalTex>;
	magfilter = POINT;
	minfilter = POINT;
	mipfilter = POINT;
	AddressU = clamp;
	AddressV = clamp;
};

Texture xLightTex;
sampler lightSampler = sampler_state
{
	texture = <xLightTex>;
	magfilter = POINT;
	minfilter = POINT;
	mipfilter = POINT;
	AddressU = clamp;
	AddressV = clamp;
};

// -----Settings and Constants-----

float4x4 xInverseViewProjection;
float3 xLightColorAndIntensity;
float3 xLightNormal;
float4x4 xLightViewProjection;

const float DEPTH_TOLERANCE = 0.01;

// -----Pixel input/output data structures

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

// -----Functions-----

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

// Unpacks a float3 normal from a compressed float2 normal
float3 unpackNormal(float2 packed)
{
	return float3(packed.xy, sqrt(1 - packed.x * packed.x - packed.y * packed.y));
}

float cellShade(float intensity)
{
	if (intensity > 1.0)
		return intensity;
	if (intensity > 0.6)
		return 1.0;
	if (intensity > 0.1)
		return 0.4;
	return 0;
}

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

    return output;
}

float4 PixelShaderFunction(VSout input) : COLOR0
{
	float4 output;

	float depth = tex2D(depthSampler, input.texCoord).x;
	if (depth > 1.0)
		return 0;

	float4 albedo = unpackColor10(tex2D(albedoSampler, input.texCoord));
	float3 normal = unpackNormal(tex2D(normalSampler, input.texCoord).xy);

	// Lighting

	float intensity = dot(normalize(xLightNormal), normal);
	output.rgb = albedo * xLightColorAndIntensity * intensity;
	output.a = 1;

    return packColor10(output);
}

float4 ShadowPixelShaderFunction(VSout input) : COLOR0
{
	float4 output;

	float depth = tex2D(depthSampler, input.texCoord).x;
	if (depth > 1.0)
		return 0;

	float4 albedo = unpackColor10(tex2D(albedoSampler, input.texCoord));
	float3 normal = unpackNormal(tex2D(normalSampler, input.texCoord).xy);
	float4 position = unpackPosition(input.texCoord, depth);

	// Lighting

	float intensity = dot(normalize(xLightNormal), normal);
	output.rgb = albedo * xLightColorAndIntensity * intensity;// * cellShade(intensity);
	output.a = 1;

	// Shadow
	float4 lightViewProject = mul(position, xLightViewProjection);
	lightViewProject /= lightViewProject.w;

	float2 lightTexCoord = lightViewProject.xy * float2(0.5, -0.5) + 0.5;
	
	float shadowCoefficient;

	// test to see if projection is in shadow map region
	if (saturate(lightTexCoord).x == lightTexCoord.x && saturate(lightTexCoord).y == lightTexCoord.y)
	{
		float lightDepth = tex2D(lightSampler, lightTexCoord).r;
		float pixelDepth = lightViewProject.z;
		
		// check if shadowed
		if ((pixelDepth - DEPTH_TOLERANCE) <= lightDepth)
			shadowCoefficient = 1.0f;
		else
			shadowCoefficient = 0.0f;

	}
	else
	{
		shadowCoefficient = 1.0f;
	}

    return packColor10(output * shadowCoefficient);
}

technique DirectionalLight
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}

technique DirectionalLightShadow
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 ShadowPixelShaderFunction();
    }
}
