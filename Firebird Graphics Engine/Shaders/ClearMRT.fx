//
// Deferred.fx
// Deferred shading from source polygons to deferred MRT textures.
//

// -----Shader inputs-----

// global effect parameters

// -----Pixel input/output data structures

struct VSin
{
    float4 position : POSITION0;
};

struct VSout
{
    float4 position : POSITION0;
	float3 normal : TEXCOORD0;
	float4 position2D : TEXCOORD1;
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

// -----Shaders-----

// Vertex Shader
float4 VertexShaderFunction(float4 input : POSITION0) : POSITION0
{
    return input;
}

// Pixel shader
PSout PixelShaderFunction()
{
    PSout output;

	output.albedo = 0;
	output.depth = 2; // number greater than 1
	output.normal = 0;

    return output;
}

technique ClearMRT
{
    pass Pass1
    {
        // TODO: set renderstates here.

        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
