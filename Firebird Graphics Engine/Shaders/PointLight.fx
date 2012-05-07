//
// PointLight.fx
// Applies lighting contribution from a point light.
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

// -----Settings and Constants-----

float4x4 xInverseViewProjection;

float3	xLightColorAndIntensity;
float3	xLightPosition;

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

// Calculates world position from depth and texture coordinate information.
float3 unpackPosition(float2 texCoord, float depth)
{
	float4 projectedPos;
	projectedPos.x = texCoord.x * 2 - 1;
	projectedPos.y = (1 - texCoord.y) * 2 - 1;
	projectedPos.z = depth;
	projectedPos.w = 1.0;

	float4 newPosition = mul(projectedPos, xInverseViewProjection);
	return newPosition.xyz / newPosition.w;
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
	float3 position = unpackPosition(input.texCoord, depth);

	// Calculate lighting

	float3 lightVector = xLightPosition - position;
	float distance = max(length(lightVector) - 5, 1);
	float falloff = 1.0 / pow(distance,2);
	float intensity = falloff * cellShade(dot(normalize(lightVector), normal));

	output.xyz = albedo * xLightColorAndIntensity * intensity;
	output.a = 1;

    return packColor10(output);
}

technique PointLight
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
