//
// Shield.fx
// Renders the player's shield.
//

// -----Textures-----

Texture xShieldTex;
sampler textureSampler = sampler_state
{
	texture = <xShieldTex>;
	magfilter = LINEAR;
	minfilter = LINEAR;
	AddressU = mirror;
	AddressV = mirror;
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

float4x4 xViewProjection;
float4x4 xView;
float4x4 xWorld;

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
	float2 texCoord : TEXCOORD0;
	float3 normal : TEXCOORD1;
	float4 position2D : TEXCOORD2;
};

// -----Functions-----

float4 packColor10(float4 color)
{
	color.xyz /= 4;
	return color;
}

float4 unpackColor10(float4 color)
{
	return color * 4;
}

// -----Shaders-----

VSout VertexShaderFunction(VSin input)
{
    VSout output;

    output.position = mul(mul(input.position, xWorld), xViewProjection);
	output.position2D = output.position;
	output.texCoord = input.texCoord;
	output.normal = mul(input.normal, xView);

    return output;
}

float4 PixelShaderFunction(VSout input) : COLOR0
{
	float4 output;

	float sceneDepth = tex2D(depthSampler, input.position2D.xy);
	float shieldDepth = input.position2D.z / input.position2D.w;

	float4 albedo = tex2D(textureSampler, input.texCoord);

	float intensity = dot(float3(0, 0, -1), normalize(input.normal));
	output = float4(albedo.rgb * 2, (1 - pow(intensity,2)));
	
    return packColor10(output);
}

technique Shield
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
