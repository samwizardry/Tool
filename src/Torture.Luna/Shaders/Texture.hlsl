cbuffer PerFrameBuffer : register(b0)
{
    float4x4 Model;
    float4x4 View;
    float4x4 Proj;
};
//cbuffer PerObjectBuffer : register(b1)
//{
//    float4x4 Model;
//};
//cbuffer MaterialBuffer : register(b2)
//{
//    float4 BaseColor;
//};

Texture2D gDiffuseMap : register(t0);
SamplerState smp
{
    AddressU = Clamp;
    AddressV = Wrap;
};

struct VSInput
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float2 Texture : TEXCOORD;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 Texture : TEXCOORD;
};

PSInput VSMain(VSInput input)
{
    PSInput result;

    result.Position = mul(float4(input.Position, 1.0f), Model);
    result.Position = mul(result.Position, View);
    result.Position = mul(result.Position, Proj);

    result.Texture = input.Texture;
    return result;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    float4 color = gDiffuseMap.Sample(smp, input.Texture);
    return color;
}
