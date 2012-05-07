//
// AmbientAndEdgeHighlight.fx
// Applies edge highlights and ambient light contribution to scene.
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

float2 xResolution;

//float xEdgeWidth = 0.5;
//float xEdgeIntensity = 1;
//float xNormalSensitivity = 1;
//float xDepthSensitivity = 1;
//float xNormalThreshold = 0.6;
//float xDepthThreshold = 0.1;

float xEdgeWidth;
float xEdgeIntensity;
float xNormalSensitivity;
float xDepthSensitivity;
float xNormalThreshold;
float xDepthThreshold;

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

// Unpacks a float3 normal from a compressed float2 normal
float3 unpackNormal(float2 packed)
{
	return float3(packed.xy, sqrt(1 - packed.x * packed.x - packed.y * packed.y));
}

float calculateEdge(float3 normal, float depth, float2 texCoord)
{
	float2 xEdgeWidthMulti = float2(1 / xResolution.x, 1 / xResolution.y) * xEdgeWidth;

	// Nearby normals
	float3 n1 = unpackNormal(tex2D(normalSampler, texCoord + float2(-1, -1) * xEdgeWidthMulti));
	float3 n2 = unpackNormal(tex2D(normalSampler, texCoord + float2( 1,  1) * xEdgeWidthMulti));
	float3 n3 = unpackNormal(tex2D(normalSampler, texCoord + float2(-1,  1) * xEdgeWidthMulti));
	float3 n4 = unpackNormal(tex2D(normalSampler, texCoord + float2( 1, -1) * xEdgeWidthMulti));
	// Nearby depths
	float d1 = tex2D(depthSampler, texCoord + float2(-1, -1) * xEdgeWidthMulti).r;
	float d2 = tex2D(depthSampler, texCoord + float2( 1,  1) * xEdgeWidthMulti).r;
	float d3 = tex2D(depthSampler, texCoord + float2(-1,  1) * xEdgeWidthMulti).r;
	float d4 = tex2D(depthSampler, texCoord + float2( 1, -1) * xEdgeWidthMulti).r;

	// Work out how much the normal and depth values are changing.
	float3 normalDelta = dot(abs(n1 - n2) + abs(n3 - n4), 1);
	float depthDelta = abs(d1 - d2) + abs(d3 - d4);
	    
	// Filter out very small changes
	normalDelta = saturate((normalDelta - xNormalThreshold) * xNormalSensitivity);
	depthDelta = saturate((depthDelta - xDepthThreshold) * xDepthSensitivity);

	// Calculate edge intensity
	float edgeAmount = saturate(normalDelta + depthDelta) * xEdgeIntensity;
	    
	// Apply the edge detection result to the main scene color.
	return edgeAmount;
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
	float depth = tex2D(depthSampler, input.texCoord).x;
	float3 normal = unpackNormal(tex2D(normalSampler, input.texCoord).xy);

	return float4(0, 0, 0, calculateEdge(normal, depth, input.texCoord));
}

technique AmbientAndEdgeHighlight
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
