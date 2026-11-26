static const float4 gVertices[4] =
{
    float4(-1.0, 0.0, 1.0, 1.0),
    float4(-1.0, 0.0, -1.0, 1.0),
    float4(1.0, 0.0, -1.0, 1.0),
    float4(1.0, 0.0, 1.0, 1.0)
};

static const uint gIndices[6] =
{
    0, 3, 2,
    2, 1, 0,
};

static const float gGridCellSize = 0.025;
static const float4 gGridColorThin = float4(0.5, 0.5, 0.5, 1.0);
static const float4 gGridColorThick = float4(0.0, 0.0, 0.0, 1.0);


cbuffer PerFrameBuffer : register(b0)
{
    float4x4 gViewProj;
    float3 gCamWorldPos;
    float gPad0;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    float4 WorldPos : POSITION;
};

PSInput VSMain(uint vertexId : SV_VertexID)
{
    PSInput output;
    uint index = gIndices[vertexId];
    float4 pos = gVertices[index];

    pos.x += gCamWorldPos.x;
    pos.z += gCamWorldPos.z;

    output.Position = mul(pos, gViewProj);
    output.WorldPos = pos;
    return output;
}

float glsl_mod(float x, float y)
{
    return x - y * floor(x / y);
}

float2 glsl_mod(float2 x, float2 y)
{
    return x - y * floor(x / y);
}

float3 glsl_mod(float3 x, float3 y)
{
    return x - y * floor(x / y);
}

float4 glsl_mod(float4 x, float4 y)
{
    return x - y * floor(x / y);
}

float4 PSMain(PSInput input) : SV_TARGET
{
    float lod0a = glsl_mod(input.WorldPos.z, gGridCellSize);
    float4 color;

    color = gGridColorThick;
    color.a = lod0a;

    return color;
}
