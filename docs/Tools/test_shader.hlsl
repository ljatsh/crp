// HLSL 测试着色器
// 用于测试编译和查看汇编代码

// 顶点着色器输入
struct VertexInput
{
    float3 position : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
};

// 顶点着色器输出
struct VertexOutput
{
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
    float3 worldPos : TEXCOORD1;
};

// 常量缓冲区
cbuffer TransformBuffer : register(b0)
{
    float4x4 worldMatrix;
    float4x4 viewMatrix;
    float4x4 projMatrix;
};

// 纹理和采样器
Texture2D mainTexture : register(t0);
SamplerState mainSampler : register(s0);

// 顶点着色器
VertexOutput VS(VertexInput input)
{
    VertexOutput output;
    
    // 变换到世界空间
    float4 worldPos = mul(float4(input.position, 1.0), worldMatrix);
    output.worldPos = worldPos.xyz;
    
    // 变换到裁剪空间
    float4 viewPos = mul(worldPos, viewMatrix);
    output.position = mul(viewPos, projMatrix);
    
    // 传递 UV 和法线
    output.uv = input.uv;
    output.normal = mul(input.normal, (float3x3)worldMatrix);
    
    return output;
}

// 像素着色器
float4 PS(VertexOutput input) : SV_Target
{
    // 采样纹理
    float4 texColor = mainTexture.Sample(mainSampler, input.uv);
    
    // 简单的光照计算（Lambert）
    float3 lightDir = normalize(float3(1.0, 1.0, 1.0));
    float3 normal = normalize(input.normal);
    float NdotL = max(0.0, dot(normal, lightDir));
    
    // 组合颜色
    float3 finalColor = texColor.rgb * NdotL;

    int times = 0;
    for (int i = 0; i < 10; i++)
    {
        times += i;
        if (i > 5) {
            break;
        }
    }

    return float4(finalColor, texColor.a * times);
}

