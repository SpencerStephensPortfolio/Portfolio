//
// AmbientAndEdgeHighlight.fx
// Applies edge highlights and ambient light contribution to scene.
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

// -----Settings and Constants-----

float3 xAmbient;
float xInverseAspect;

// -----Pixel input/output data structures

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

// -----Shaders-----

VSout VertexShaderFunction(VSin input)
{
    VSout output;

    output.position = input.position;
	output.texCoord = input.texCoord;
	output.position2D = input.position.xy / input.position.w;

    return output;
}

float4 PixelShaderFunction(VSout input) : COLOR0
{
	float4 output;

	// Depth test
	float depth = tex2D(depthSampler, input.texCoord).x;
	if (depth > 1.0)
		return 0;

	float4 albedo = unpackColor10(tex2D(albedoSampler, input.texCoord));

	output.rgb = albedo.rgb * (xAmbient + 3 * albedo.a); // ambient + emissive
	output.a = 1;

	return packColor10(output);
}

technique AmbientLight
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
