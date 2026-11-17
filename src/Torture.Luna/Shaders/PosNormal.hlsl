struct Material
{
    float Shininess;
    float3 Pad0;
};

struct Light
{
    float3 Position;
    float Pad0;
    float3 Ambient;
    float Pad1;
    float3 Diffuse;
    float Pad2;
    float3 Specular;
    float Pad3;
};

cbuffer PerFrameBuffer : register(b0)
{
    float4x4 gViewProj;
    Light gLight;
    float3 gViewPosition;
};

cbuffer PerObjectBuffer : register(b1)
{
    float4x4 gModel;
};

Texture2D gAlbedoMap : register(t0);
Texture2D gSpecularMap : register(t1);
SamplerState gSampler : register(s0);

struct VSInput
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    float3 VertPosition : POSITION;
    float3 Normal : NORMAL;
};

PSInput VSMain(VSInput input)
{
    PSInput output;

    float4 vertPosition = mul(float4(input.Position, 1.0f), gModel);

    output.Position = mul(vertPosition, gViewProj);
    output.VertPosition = vertPosition;

    return output;
}

//float4 ComputeDirLight();
//float4 ComputePointLight();
//float4 ComputeSpotLight();

float4 PSMain(PSInput input) : SV_TARGET
{
    return float4(1.0f, 1.0f, 1.0f, 1.0f);
}
