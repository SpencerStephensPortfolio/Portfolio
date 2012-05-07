//
// ForwardUnshaded.fx
// Draws a model using forward rendering AFTER compositing.
//

// -----Shader inputs-----

float4x4 xWorldViewProjection;
float4x4 xWorld;

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

// -----Pixel input/output data structures

struct VSin
{
    float4 position : POSITION0;
};

struct VSout
{
    float4 position : POSITION0;
	float4 position2D : TEXCOORD0;
};

struct PSout
{
	float4 color : COLOR0;
};

// -----Functions-----

	// none

// -----Shaders-----

// Vertex Shader
VSout VertexShaderFunction(VSin input)
{
    VSout output;

    output.position = mul(input.position, xWorldViewProjection);
	output.position2D = output.position;

    return output;
}

// Pixel shader
float4 PixelShaderFunction(VSout input) : COLOR0
{
	float depth = input.position2D.z / input.position2D.w;
	float2 texCoord = input.position2D.xy / input.position2D.w;
	texCoord.x = texCoord.x / 2 + 0.5;
	texCoord.y = -texCoord.y / 2 + 0.5;
	float texDepth = tex2D(depthSampler, texCoord).r;
    return float4(0.25, 0.25, 0.25, depth < texDepth ? 1 : 0);
}

technique ForwardUnshaded
{
    pass Model
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
