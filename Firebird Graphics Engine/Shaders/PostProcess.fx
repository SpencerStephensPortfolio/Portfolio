//
// PostProcess.fx
// Composites information from deferred MRTs.
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
float2 xResolution;

float edgeWidth = 0.5;
float edgeIntensity = 1;
float normalSensitivity = 1;
float depthSensitivity = 5;
float normalThreshold = 0.6;
float depthThreshold = 0.1;

// -----Lights-----

// Ambient
float3 xAmbient;

// Point

// Light 0
float3	xPointLightColor0;
float3	xPointLightLocation0;
// Light 1
float3	xPointLightColor1;
float3	xPointLightLocation1;
// Light 2
float3	xPointLightColor2;
float3	xPointLightLocation2;
// Light 3
float3	xPointLightColor3;
float3	xPointLightLocation3;
// Light 4
float3	xPointLightColor4;
float3	xPointLightLocation4;

// Directional

// Light 0
float3 xDirectionalLightColor0;
float3 xDirectionalLightNormal0;
// Light 1
float3 xDirectionalLightColor1;
float3 xDirectionalLightNormal1;

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

float4 packColor10(float4 color)
{
	return color / 4;
}

float4 unpackColor10(float4 color)
{
	return color * 4;
}

// -----Functions-----

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

// Calculates contribution of a point light.
float3 pointLight(float3 color, float3 lightPosition, float3 surfacePosition, float3 surfaceNormal)
{
	float3 lightVector = lightPosition - surfacePosition;
	float distance = max(length(lightVector) - 5, 1);
	float falloff = 1.0 / distance / distance;
	return color * falloff * cellShade(dot(normalize(lightVector), surfaceNormal));
}

// Calculates contribution of a directional light.
float3 directionalLight(float3 color, float3 lightNormal, float3 surfaceNormal)
{
	return color * cellShade(dot(normalize(1 - lightNormal), surfaceNormal));
}

// Calculates lighting coefficient
float4 calculateLighting(float3 position, float3 normal)
{
	return float4((
			xAmbient +
			pointLight(xPointLightColor0, xPointLightLocation0, position, normal) +
			pointLight(xPointLightColor1, xPointLightLocation1, position, normal) +
			pointLight(xPointLightColor2, xPointLightLocation2, position, normal) +
			pointLight(xPointLightColor3, xPointLightLocation3, position, normal) +
			pointLight(xPointLightColor4, xPointLightLocation4, position, normal) +
			directionalLight(xDirectionalLightColor0, xDirectionalLightNormal0, normal) +
			directionalLight(xDirectionalLightColor1, xDirectionalLightNormal1, normal)
		).xyz,
		1.0); 
}

float calculateEdge(float3 normal, float depth, float2 texCoord)
{
	float2 edgeWidthMulti = float2(1 / xResolution.x, 1 / xResolution.y) * edgeWidth;

	// Nearby normals
	float3 n1 = unpackNormal(tex2D(normalSampler, texCoord + float2(-1, -1) * edgeWidthMulti));
	float3 n2 = unpackNormal(tex2D(normalSampler, texCoord + float2( 1,  1) * edgeWidthMulti));
	float3 n3 = unpackNormal(tex2D(normalSampler, texCoord + float2(-1,  1) * edgeWidthMulti));
	float3 n4 = unpackNormal(tex2D(normalSampler, texCoord + float2( 1, -1) * edgeWidthMulti));
	// Nearby depths
	float d1 = tex2D(depthSampler, texCoord + float2(-1, -1) * edgeWidth);
	float d2 = tex2D(depthSampler, texCoord + float2( 1,  1) * edgeWidth);
	float d3 = tex2D(depthSampler, texCoord + float2(-1,  1) * edgeWidth);
	float d4 = tex2D(depthSampler, texCoord + float2( 1, -1) * edgeWidth);

	// Work out how much the normal and depth values are changing.
	float3 normalDelta = dot(abs(n1 - n2) + abs(n3 - n4), 1);
	float depthDelta = abs(d1 - d2) + abs(d3 - d4);
	    
	// Filter out very small changes
	normalDelta = saturate((normalDelta - normalThreshold) * normalSensitivity);
	depthDelta = saturate((depthDelta - depthThreshold) * depthSensitivity);

	// Calculate edge intensity
	float edgeAmount = saturate(normalDelta + depthDelta) * edgeIntensity;
	    
	// Apply the edge detection result to the main scene color.
	return 1 - edgeAmount;
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

	float4 albedo = unpackColor10(tex2D(albedoSampler, input.texCoord));
	float depth = tex2D(depthSampler, input.texCoord).x;
	float3 normal = unpackNormal(tex2D(normalSampler, input.texCoord).xy);
	float3 position = unpackPosition(input.texCoord, depth);

	float4 lighting = calculateLighting(position, normal);

	output = albedo * lighting * calculateEdge(normal, depth, input.texCoord);

    return output;
}

technique PostProcess
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
