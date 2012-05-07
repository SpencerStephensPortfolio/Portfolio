float xEmissive;
float xEnableEmissiveTexture;

Texture xEmissiveTex;
sampler emissiveSampler = sampler_state
{
	texture = <xEmissiveTex>;
	magfilter = LINEAR;
	minfilter = LINEAR;
	AddressU = clamp;
	AddressV = clamp;
};

float EmissiveContribution()
{
	return xEmissive;
}

float EmissiveContribution(float2 uv)
{
	return xEmissive + xEnableEmissiveTexture * tex2D(emissiveSampler, uv);
}