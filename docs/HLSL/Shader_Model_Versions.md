# 着色器模型版本

#HLSL #着色器模型 #ShaderModel #版本差异

> 理解不同 Shader Model 版本的特性和差异

## 📋 目录

- [Shader Model 2.0/3.0](#shader-model-2030)
- [Shader Model 4.0](#shader-model-40)
- [Shader Model 5.0](#shader-model-50)
- [Shader Model 6.0+](#shader-model-60)
- [版本对比](#版本对比)
- [实践任务](#实践任务)

---

## Shader Model 2.0/3.0

### Shader Model 2.0 特性

**发布时间**：2002年（DirectX 9.0）

**主要特性**：
- 支持顶点着色器和像素着色器
- 有限的指令数限制
- 固定功能管线仍然可用

**限制**：
- 顶点着色器：最多 256 条指令
- 像素着色器：最多 96 条指令（PS 2.0）
- 有限的寄存器数量
- 不支持动态分支

**示例**：
```hlsl
// Shader Model 2.0 顶点着色器
void VS_2_0(
    float4 position : POSITION,
    out float4 oPosition : POSITION
) {
    oPosition = position;
}
```

### Shader Model 3.0 特性

**发布时间**：2004年（DirectX 9.0c）

**主要改进**：
- 增加了指令数限制
- 支持动态分支（有限）
- 支持更长的循环
- 支持更多的纹理采样

**限制**：
- 顶点着色器：最多 512 条指令
- 像素着色器：最多 512 条指令（PS 3.0）
- 动态分支性能较差

**示例**：
```hlsl
// Shader Model 3.0 支持动态分支
void PS_3_0(float2 uv : TEXCOORD0, out float4 color : COLOR0) {
    if (uv.x > 0.5) {
        color = float4(1.0, 0.0, 0.0, 1.0);
    } else {
        color = float4(0.0, 0.0, 1.0, 1.0);
    }
}
```

---

## Shader Model 4.0

**发布时间**：2006年（DirectX 10）

### 主要特性

#### 统一着色器架构
- 顶点、几何、像素着色器使用相同的指令集
- 统一的寄存器文件
- 更好的资源管理

#### 几何着色器支持
```hlsl
[maxvertexcount(3)]
void GS_Main(
    triangle float4 input[3] : SV_POSITION,
    inout TriangleStream<float4> output
) {
    // 几何着色器代码
    for (int i = 0; i < 3; i++) {
        output.Append(input[i]);
    }
}
```

#### 常量缓冲区（Constant Buffer）
```hlsl
cbuffer MyConstants : register(b0) {
    float4x4 worldMatrix;
    float4 color;
    float time;
};
```

#### 改进的语义系统
```hlsl
struct VertexInput {
    float3 position : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
};

struct VertexOutput {
    float4 position : SV_POSITION;  // 系统值语义
    float2 uv : TEXCOORD0;
};
```

### 限制和特性

- **指令数**：理论上无限制（实际受硬件限制）
- **寄存器**：统一的寄存器文件，更多可用寄存器
- **纹理采样**：最多 128 个纹理
- **动态分支**：性能大幅改善

---

## Shader Model 5.0

**发布时间**：2009年（DirectX 11）

### 主要特性

#### 计算着色器（Compute Shader）
```hlsl
[numthreads(8, 8, 1)]
void CS_Main(uint3 id : SV_DispatchThreadID) {
    // 计算着色器代码
    uint index = id.x + id.y * width;
    // 处理数据
}
```

#### 结构化缓冲区
```hlsl
StructuredBuffer<float4> inputBuffer;
RWStructuredBuffer<float4> outputBuffer;

void CS_Main(uint3 id : SV_DispatchThreadID) {
    outputBuffer[id.x] = inputBuffer[id.x] * 2.0;
}
```

#### 纹理数组和立方体贴图数组
```hlsl
Texture2DArray textureArray;
TextureCubeArray cubeMapArray;
```

#### 改进的动态分支
- 更好的分支性能
- 支持更复杂的控制流

#### 着色器反射
- 运行时查询着色器信息
- 动态资源绑定

### 新资源类型

```hlsl
// 字节地址缓冲区
ByteAddressBuffer byteBuffer;
RWByteAddressBuffer rwByteBuffer;

// 纹理缓冲区
TextureBuffer<float4> textureBuffer;

// 可读写纹理
RWTexture2D<float4> rwTexture2D;
RWTexture3D<float4> rwTexture3D;
```

---

## Shader Model 6.0+

**发布时间**：2017年（DirectX 12）

### Shader Model 6.0 特性

#### Wave 操作
```hlsl
// 获取 Wave 中的线程数
uint waveSize = WaveGetLaneCount();

// 获取当前线程在 Wave 中的索引
uint laneIndex = WaveGetLaneIndex();

// Wave 内广播
float value = WaveReadLaneFirst(inputValue);

// Wave 内求和
float sum = WaveActiveSum(inputValue);

// Wave 内前缀和
float prefixSum = WavePrefixSum(inputValue);
```

#### 改进的数据类型
```hlsl
// 最小精度类型
min16float minFloat;
min10float minFloat10;
min16int minInt;
min12int minInt12;
```

#### 改进的纹理操作
```hlsl
// 采样反馈
Texture2D<float4> myTexture;
SamplerState mySampler;

FeedbackTexture2D<float4> feedbackTexture;

void PS_Main(float2 uv : TEXCOORD0) {
    float4 color = myTexture.Sample(mySampler, uv);
    feedbackTexture.WriteSamplerFeedback(color, uv);
}
```

### Shader Model 6.1+ 特性

#### 光线追踪（DXR）
```hlsl
RaytracingAccelerationStructure scene : register(t0);

[shader("raygeneration")]
void RayGenShader() {
    RayDesc ray;
    ray.Origin = cameraPos;
    ray.Direction = rayDir;
    ray.TMin = 0.0;
    ray.TMax = 1000.0;
    
    RayPayload payload;
    TraceRay(scene, 0, 0xFF, 0, 0, 0, ray, payload);
}

[shader("closesthit")]
void ClosestHitShader(inout RayPayload payload, in BuiltInTriangleIntersectionAttributes attr) {
    payload.color = float3(1.0, 0.0, 0.0);
}
```

#### Mesh Shader
```hlsl
[outputtopology("triangle")]
[numthreads(128, 1, 1)]
void MS_Main(
    uint gtid : SV_GroupThreadID,
    out vertices VSOutput verts[64],
    out indices uint3 tris[126]
) {
    // Mesh Shader 代码
}
```

---

## 版本对比

| 特性 | SM 2.0 | SM 3.0 | SM 4.0 | SM 5.0 | SM 6.0+ |
|------|--------|--------|--------|--------|---------|
| **指令数限制** | 有限 | 有限 | 无限制 | 无限制 | 无限制 |
| **动态分支** | ❌ | ⚠️ 有限 | ✅ 改进 | ✅ 良好 | ✅ 优秀 |
| **几何着色器** | ❌ | ❌ | ✅ | ✅ | ✅ |
| **计算着色器** | ❌ | ❌ | ❌ | ✅ | ✅ |
| **常量缓冲区** | ❌ | ❌ | ✅ | ✅ | ✅ |
| **结构化缓冲区** | ❌ | ❌ | ❌ | ✅ | ✅ |
| **Wave 操作** | ❌ | ❌ | ❌ | ❌ | ✅ |
| **光线追踪** | ❌ | ❌ | ❌ | ❌ | ✅ (6.1+) |
| **Mesh Shader** | ❌ | ❌ | ❌ | ❌ | ✅ (6.5+) |

---

## 实践任务

### 任务1：对比不同 Shader Model 的特性差异

编写针对不同 Shader Model 版本的着色器：

```hlsl
// SM 4.0+ 版本
struct VSInput {
    float3 position : POSITION;
};

struct VSOutput {
    float4 position : SV_POSITION;
};

VSOutput VS_Main(VSInput input) {
    VSOutput output;
    output.position = float4(input.position, 1.0);
    return output;
}

float4 PS_Main(VSOutput input) : SV_Target {
    return float4(1.0, 0.0, 0.0, 1.0);
}
```

### 任务2：测试不同版本的性能表现

使用编译器工具编译到不同 Shader Model 版本：

```bash
# 编译到 SM 4.0
fxc /T vs_4_0 /T ps_4_0 shader.hlsl

# 编译到 SM 5.0
fxc /T vs_5_0 /T ps_5_0 shader.hlsl

# 编译到 SM 6.0
dxc -T vs_6_0 -T ps_6_0 shader.hlsl
```

对比生成的汇编代码和性能特征。

---

## 🔗 相关链接

- [[HLSL_Semantics]]
- [[Resource_Binding]]
- [[Assembly_Basics]] - 不同版本的指令差异
- [[Platform_Differences]] - 平台支持情况

---

*最后更新：2024年*

