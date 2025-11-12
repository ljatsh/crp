# 资源绑定

#HLSL #资源绑定 #纹理 #缓冲区 #CBuffer

> 学习资源绑定：纹理、采样器、常量缓冲区、结构化缓冲区

## 📋 目录

- [纹理和采样器](#纹理和采样器)
- [常量缓冲区（CBuffer）](#常量缓冲区cbuffer)
- [结构化缓冲区](#结构化缓冲区)
- [纹理缓冲区](#纹理缓冲区)
- [实践任务](#实践任务)

---

## 纹理和采样器

### 纹理资源声明

#### 2D 纹理
```hlsl
Texture2D myTexture;
Texture2D<float4> myColorTexture;      // 显式指定类型
Texture2D<float> myDepthTexture;       // 单通道纹理
Texture2D<uint4> myIntTexture;         // 整数纹理
```

#### 立方体贴图
```hlsl
TextureCube myCubeMap;
TextureCube<float4> myColorCubeMap;
```

#### 3D 纹理
```hlsl
Texture3D my3DTexture;
Texture3D<float4> myVolumeTexture;
```

#### 纹理数组
```hlsl
Texture2DArray myTextureArray;
TextureCubeArray myCubeMapArray;
```

### 采样器状态

#### 基本采样器
```hlsl
SamplerState mySampler;
SamplerComparisonState myShadowSampler;  // 用于阴影贴图
```

#### 采样器状态定义（HLSL Effect 语法）
```hlsl
SamplerState linearRepeatSampler {
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
    AddressW = Wrap;
};

SamplerState pointClampSampler {
    Filter = MIN_MAG_MIP_POINT;
    AddressU = Clamp;
    AddressV = Clamp;
    AddressW = Clamp;
};
```

#### 过滤模式
- `MIN_MAG_MIP_POINT` - 点过滤
- `MIN_MAG_MIP_LINEAR` - 线性过滤
- `ANISOTROPIC` - 各向异性过滤

#### 寻址模式
- `Wrap` - 重复
- `Mirror` - 镜像重复
- `Clamp` - 夹取到边缘
- `Border` - 边界颜色
- `MirrorOnce` - 镜像一次后夹取

### 纹理采样

#### 基本采样（SM 4.0+）
```hlsl
Texture2D myTexture;
SamplerState mySampler;
float2 uv = float2(0.5, 0.5);

float4 color = myTexture.Sample(mySampler, uv);
```

#### 带 LOD 的采样
```hlsl
float4 color = myTexture.SampleLevel(mySampler, uv, mipLevel);
```

#### 带梯度的采样
```hlsl
float4 color = myTexture.SampleGrad(mySampler, uv, ddx, ddy);
```

#### 投影采样
```hlsl
float4 color = myTexture.SampleProj(mySampler, float3(uv, w));
```

#### 比较采样（阴影贴图）
```hlsl
Texture2D shadowMap;
SamplerComparisonState shadowSampler;
float3 shadowCoord = float3(uv, depth);

float shadow = shadowMap.SampleCmpLevelZero(shadowSampler, shadowCoord.xy, shadowCoord.z);
```

### 寄存器绑定

```hlsl
// 显式指定寄存器
Texture2D myTexture : register(t0);
SamplerState mySampler : register(s0);

// 多个纹理
Texture2D diffuseMap : register(t0);
Texture2D normalMap : register(t1);
Texture2D specularMap : register(t2);
```

---

## 常量缓冲区（CBuffer）

### 基本声明

```hlsl
cbuffer MyConstants : register(b0) {
    float4x4 worldMatrix;
    float4x4 viewMatrix;
    float4x4 projMatrix;
    float4 color;
    float time;
};
```

### 打包规则和对齐

#### 对齐规则
- 标量类型对齐到自身大小
- 向量类型对齐到 4 字节边界
- 矩阵按行对齐到 `float4`（16字节）边界
- 结构体对齐到最大成员的对齐要求
- 整个常量缓冲区对齐到 `float4`（16字节）边界

#### 好的打包示例
```hlsl
cbuffer GoodPacking : register(b0) {
    float4x4 worldMatrix;     // 64 字节，对齐到 16 字节
    float4x4 viewMatrix;      // 64 字节
    float4x4 projMatrix;      // 64 字节
    float4 color;             // 16 字节
    float time;                // 4 字节，但会填充到 16 字节边界
    // 总大小：208 字节（13 * 16）
};
```

#### 不好的打包示例
```hlsl
cbuffer BadPacking : register(b0) {
    float x;                   // 4 字节
    float2 yz;                 // 8 字节，但需要对齐
    float w;                   // 4 字节
    // 可能浪费空间
};
```

### 动态 vs 静态索引

#### 静态索引（编译时确定）
```hlsl
cbuffer Constants : register(b0) {
    float4 values[10];
};

float GetValue(int index) {
    // 静态索引（如果 index 是编译时常量）
    return values[5];
}
```

#### 动态索引（运行时确定）
```hlsl
float GetValue(int index) {
    // 动态索引（性能可能较差）
    return values[index];
}
```

> ⚠️ **注意**：动态索引在某些平台上可能有限制或性能影响。

### 多个常量缓冲区

```hlsl
// 每帧更新的数据
cbuffer PerFrame : register(b0) {
    float4x4 viewProjMatrix;
    float3 cameraPos;
};

// 每个对象的数据
cbuffer PerObject : register(b1) {
    float4x4 worldMatrix;
    float4 color;
};

// 材质数据
cbuffer Material : register(b2) {
    float4 albedo;
    float roughness;
    float metallic;
};
```

---

## 结构化缓冲区

### 只读结构化缓冲区

```hlsl
struct Particle {
    float3 position;
    float3 velocity;
    float lifetime;
};

StructuredBuffer<Particle> particleBuffer;

void ProcessParticle(uint index) {
    Particle p = particleBuffer[index];
    // 使用粒子数据
}
```

### 可读写结构化缓冲区

```hlsl
RWStructuredBuffer<Particle> particleBuffer;

[numthreads(64, 1, 1)]
void CS_UpdateParticles(uint3 id : SV_DispatchThreadID) {
    uint index = id.x;
    Particle p = particleBuffer[index];
    
    // 更新粒子
    p.position += p.velocity * deltaTime;
    p.lifetime -= deltaTime;
    
    // 写回
    particleBuffer[index] = p;
}
```

### 追加/消耗缓冲区

```hlsl
AppendStructuredBuffer<Particle> appendBuffer;
ConsumeStructuredBuffer<Particle> consumeBuffer;

void AppendParticle(Particle p) {
    appendBuffer.Append(p);
}

Particle ConsumeParticle() {
    Particle p;
    bool success = consumeBuffer.Consume(p);
    return p;
}
```

### 字节地址缓冲区

```hlsl
ByteAddressBuffer byteBuffer;
RWByteAddressBuffer rwByteBuffer;

void ReadData(uint offset) {
    // 按字节读取
    uint value = byteBuffer.Load(offset);
    float4 vec = byteBuffer.Load4(offset);
}

void WriteData(uint offset, uint value) {
    rwByteBuffer.Store(offset, value);
    rwByteBuffer.Store4(offset, float4(1, 2, 3, 4));
}
```

---

## 纹理缓冲区

### 只读纹理缓冲区

```hlsl
TextureBuffer<float4> colorBuffer;

float4 GetColor(uint index) {
    return colorBuffer[index];
}
```

### 可读写纹理

#### 2D 可读写纹理
```hlsl
RWTexture2D<float4> outputTexture;

[numthreads(8, 8, 1)]
void CS_Process(uint3 id : SV_DispatchThreadID) {
    uint2 coord = id.xy;
    outputTexture[coord] = float4(1.0, 0.0, 0.0, 1.0);
}
```

#### 3D 可读写纹理
```hlsl
RWTexture3D<float4> volumeTexture;

[numthreads(4, 4, 4)]
void CS_ProcessVolume(uint3 id : SV_DispatchThreadID) {
    volumeTexture[id] = float4(1.0, 0.0, 0.0, 1.0);
}
```

#### 原子操作
```hlsl
RWTexture2D<uint> counterTexture;

void IncrementCounter(uint2 coord) {
    InterlockedAdd(counterTexture[coord], 1);
}
```

---

## 实践任务

### 任务1：实现使用多种资源类型的着色器

```hlsl
// 常量缓冲区
cbuffer Constants : register(b0) {
    float4x4 worldViewProj;
    float4 color;
};

// 纹理和采样器
Texture2D diffuseMap : register(t0);
Texture2D normalMap : register(t1);
SamplerState linearSampler : register(s0);

// 结构化缓冲区
StructuredBuffer<float4> lightData : register(t2);

struct VSInput {
    float3 position : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
};

struct VSOutput {
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : TEXCOORD1;
};

VSOutput VS_Main(VSInput input) {
    VSOutput output;
    output.position = mul(float4(input.position, 1.0), worldViewProj);
    output.uv = input.uv;
    output.normal = input.normal;
    return output;
}

float4 PS_Main(VSOutput input) : SV_Target {
    float4 diffuse = diffuseMap.Sample(linearSampler, input.uv);
    float3 normal = normalMap.Sample(linearSampler, input.uv).xyz;
    
    // 使用结构化缓冲区中的光照数据
    float4 light = lightData[0];
    
    return diffuse * color * light;
}
```

### 任务2：测试常量缓冲区的打包规则

```hlsl
// 测试不同的打包方式
cbuffer Test1 : register(b0) {
    float x;
    float y;
    float z;
    float w;
    // 总大小：16 字节
};

cbuffer Test2 : register(b1) {
    float x;
    float2 yz;
    float w;
    // 观察实际大小
};

cbuffer Test3 : register(b2) {
    float4 xyzw;
    // 总大小：16 字节（最优）
};
```

使用工具（如 RenderDoc）查看实际的缓冲区布局。

---

## 🔗 相关链接

- [[HLSL_Semantics]]
- [[Shader_Model_Versions]]
- [[Performance_Optimization]] - 资源访问优化
- [[Assembly_Basics]] - 资源绑定如何映射到寄存器

---

*最后更新：2024年*

