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

struct VSInput
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float4 Texture : TEXCOORD;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
};

PSInput VSMain(VSInput input)
{
    PSInput result;

    result.Position = mul(float4(input.Position, 1.0f), Model);
    result.Position = mul(result.Position, View);
    result.Position = mul(result.Position, Proj);

    result.Color = float4(1.0f, 0.0f, 0.0f, 1.0f);
    return result;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    return input.Color;
}
