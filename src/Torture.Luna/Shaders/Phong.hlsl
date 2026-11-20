struct Material
{
    float Shininess;
    float3 Pad0;
};

struct DirectionalLight
{
    float3 Direction;
    float Pad0;
    float3 Ambient;
    float Pad1;
    float3 Diffuse;
    float Pad2;
    float3 Specular;
    float Pad3;
};

struct PointLight
{
    float3 Position;
    float Constant;
    float3 Diffuse;
    float Linear;
    float3 Specular;
    float Quadratic;
};

struct SpotLight
{
    float3 Position;
    float CutOff;
    float3 Direction;
    float OuterCutOff;
    float3 Diffuse;
    float Linear;
    float3 Specular;
    float Quadratic;
};

struct Light
{
    DirectionalLight DirLight;
    PointLight PointLight;
    SpotLight SpotLight;
};

cbuffer PerFrameBuffer : register(b0)
{
    float4x4 gViewProj;
    Light gLight;
    float3 gViewPos;
};

cbuffer PerObjectBuffer : register(b1)
{
    float4x4 gModel;
    float4x4 gNormalMatrix;
    Material gMaterial;
};

Texture2D gDiffuseMap : register(t0);
Texture2D gSpecularMap : register(t1);
SamplerState gDiffuseSampler : register(s0);
SamplerState gSpecularSampler : register(s1);

struct VSInput
{
    float3 Position : POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD;
};

struct PSInput
{
    float4 Position : SV_POSITION;
    float3 VertPos : POSITION;
    float3 Normal : NORMAL;
    float2 TexCoord : TEXCOORD;
};

PSInput VSMain(VSInput input)
{
    PSInput output;

    float4 vertPos = mul(float4(input.Position, 1.0f), gModel);

    output.Position = mul(vertPos, gViewProj);
    output.VertPos = vertPos;
    output.Normal = mul(input.Normal, (float3x3) gNormalMatrix);
    output.TexCoord = input.TexCoord;

    return output;
}

float3 ComputeDirLight(DirectionalLight light, Material mat, float3 viewPos, float3 vertPos, float3 normal, float3 baseColor, float3 specColor)
{
    // Ambient
    float3 ambient = baseColor * light.Ambient;

    // Diffuse
    float3 lightDir = normalize(-light.Direction);
    float diffuseFactor = max(dot(normal, lightDir), 0.0f);
    float3 diffuse = baseColor * light.Diffuse * diffuseFactor;

    // Specular
    float3 specular = 0.0f;
    if (diffuseFactor > 0.0f)
    {
        float3 viewDir = normalize(viewPos - vertPos);
        float3 reflectDir = reflect(-lightDir, normal);
        float specFactor = pow(max(dot(viewDir, reflectDir), 0.0f), mat.Shininess);
        specular = specColor * light.Specular * specFactor;
    }

    return ambient + diffuse + specular;
}

float3 ComputePointLight(PointLight light, Material mat, float3 viewPos, float3 vertPos, float3 normal, float3 baseColor, float3 specColor)
{
    float3 toLight = light.Position - vertPos;
    float distance = length(toLight);
    float attenuation = 1.0f / (light.Constant +
        light.Linear * distance +
        light.Quadratic * (distance * distance));

    // Diffuse
    float3 lightDir = toLight / distance;
    float diffuseFactor = max(dot(normal, lightDir), 0.0f);
    float3 diffuse = baseColor * light.Diffuse * diffuseFactor;

    // Specular
    float3 specular = 0.0f;
    if (diffuseFactor > 0.0f)
    {
        float3 viewDir = normalize(viewPos - vertPos);
        float3 reflectDir = reflect(-lightDir, normal);
        float specFactor = pow(max(dot(viewDir, reflectDir), 0.0f), mat.Shininess);
        specular = specColor * light.Specular * specFactor;
    }

    return (diffuse + specular) * attenuation;
}

float3 ComputeSpotLight(SpotLight light, Material mat, float3 viewPos, float3 vertPos, float3 normal, float3 baseColor, float3 specColor)
{
    float3 toLight = light.Position - vertPos;
    float distance = length(toLight);
    float3 lightDir = toLight / distance;

    float theta = dot(lightDir, normalize(-light.Direction));
    float epsilon = (light.CutOff - light.OuterCutOff);
    float intensity = smoothstep(0.0f, 1.0f, (theta - light.OuterCutOff) / epsilon);

    if (intensity == 0.0f)
        return float3(0.0f, 0.0f, 0.0f);

    float attenuation = 1.0f / (1.0f +
        light.Linear * distance +
        light.Quadratic * (distance * distance));

    // Diffuse
    float diffuseFactor = max(dot(normal, lightDir), 0.0f);
    float3 diffuse = baseColor * light.Diffuse * diffuseFactor;

    // Specular
    float3 specular = 0.0f;
    if (diffuseFactor > 0.0f)
    {
        float3 viewDir = normalize(viewPos - vertPos);
        float3 reflectDir = reflect(-lightDir, normal);
        float specFactor = pow(max(dot(viewDir, reflectDir), 0.0f), mat.Shininess);
        specular = specColor * light.Specular * specFactor;
    }

    return (diffuse + specular) * intensity * attenuation;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    float4 baseColor = gDiffuseMap.Sample(gDiffuseSampler, input.TexCoord);
    float4 specColor = gSpecularMap.Sample(gSpecularSampler, input.TexCoord);

    float3 normal = normalize(input.Normal);

    float3 dirLight = ComputeDirLight(gLight.DirLight, gMaterial, gViewPos, input.VertPos, normal, baseColor.rgb, specColor.rgb);
    float3 pointLight = ComputePointLight(gLight.PointLight, gMaterial, gViewPos, input.VertPos, normal, baseColor.rgb, specColor.rgb);
    float3 spotLight = ComputeSpotLight(gLight.SpotLight, gMaterial, gViewPos, input.VertPos, normal, baseColor.rgb, specColor.rgb);

    return float4(dirLight + pointLight + spotLight, baseColor.a);
}
