# 高级主题

#HLSL #高级 #计算着色器 #光线追踪 #Wave操作

> 学习高级主题：计算着色器深入、光线追踪（DXR）、Wave 操作

## 📋 目录

- [计算着色器深入](#计算着色器深入)
- [光线追踪（DXR）](#光线追踪dxr)
- [Wave 操作](#wave-操作)
- [实践任务](#实践任务)

---

## 计算着色器深入

### 线程组和调度

#### 基本概念
```hlsl
[numthreads(8, 8, 1)]  // 每个线程组有 8×8×1 = 64 个线程
void CS_Main(uint3 id : SV_DispatchThreadID) {
    // id.x, id.y, id.z 是全局线程 ID
}
```

#### 线程组大小选择
```hlsl
// 1D：适合处理数组
[numthreads(64, 1, 1)]
void CS_1D(uint3 id : SV_DispatchThreadID) {
    uint index = id.x;
    // 处理一维数据
}

// 2D：适合处理纹理
[numthreads(8, 8, 1)]
void CS_2D(uint3 id : SV_DispatchThreadID) {
    uint2 coord = id.xy;
    // 处理二维数据
}

// 3D：适合处理体积纹理
[numthreads(4, 4, 4)]
void CS_3D(uint3 id : SV_DispatchThreadID) {
    uint3 coord = id.xyz;
    // 处理三维数据
}
```

### 共享内存使用

#### 声明共享内存
```hlsl
groupshared float sharedData[64];  // 线程组共享内存

[numthreads(64, 1, 1)]
void CS_Main(
    uint3 groupID : SV_GroupID,
    uint3 groupThreadID : SV_GroupThreadID,
    uint groupIndex : SV_GroupIndex
) {
    // 写入共享内存
    sharedData[groupIndex] = inputData[groupIndex];
    
    // 同步所有线程
    GroupMemoryBarrierWithGroupSync();
    
    // 读取共享内存
    float value = sharedData[(groupIndex + 1) % 64];
}
```

#### 共享内存优化
```hlsl
// 使用共享内存减少全局内存访问
groupshared float4 tile[16][16];  // 16×16 的瓦片

[numthreads(16, 16, 1)]
void CS_Blur(
    uint3 groupThreadID : SV_GroupThreadID,
    uint3 dispatchThreadID : SV_DispatchThreadID
) {
    uint2 localID = groupThreadID.xy;
    uint2 globalID = dispatchThreadID.xy;
    
    // 加载数据到共享内存
    tile[localID.y][localID.x] = inputTexture[globalID];
    
    // 同步
    GroupMemoryBarrierWithGroupSync();
    
    // 使用共享内存进行计算
    float4 result = 0.0;
    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            uint2 sampleID = localID + int2(x, y);
            result += tile[sampleID.y][sampleID.x];
        }
    }
    
    outputTexture[globalID] = result / 9.0;
}
```

### 同步操作

#### GroupMemoryBarrier
```hlsl
// 同步共享内存访问
GroupMemoryBarrier();
```

#### GroupMemoryBarrierWithGroupSync
```hlsl
// 同步共享内存访问并等待所有线程
GroupMemoryBarrierWithGroupSync();
```

#### AllMemoryBarrier
```hlsl
// 同步所有内存访问（共享内存和全局内存）
AllMemoryBarrier();
```

### 原子操作

#### InterlockedAdd
```hlsl
RWTexture2D<uint> counterTexture;

void IncrementCounter(uint2 coord) {
    uint originalValue;
    InterlockedAdd(counterTexture[coord], 1, originalValue);
}
```

#### InterlockedMax / InterlockedMin
```hlsl
RWTexture2D<float> maxValueTexture;

void UpdateMax(uint2 coord, float newValue) {
    uint originalValue;
    InterlockedMax(maxValueTexture[coord], asuint(newValue), originalValue);
}
```

#### InterlockedCompareExchange
```hlsl
RWTexture2D<uint> dataTexture;

void CompareAndSwap(uint2 coord, uint compare, uint newValue) {
    uint originalValue;
    InterlockedCompareExchange(
        dataTexture[coord],
        compare,
        newValue,
        originalValue
    );
}
```

---

## 光线追踪（DXR）

### Ray Generation Shader

#### 基本结构
```hlsl
RaytracingAccelerationStructure scene : register(t0);

struct RayPayload {
    float3 color;
};

[shader("raygeneration")]
void RayGenShader() {
    uint2 index = DispatchRaysIndex().xy;
    uint2 dimensions = DispatchRaysDimensions().xy;
    
    float2 uv = (index + 0.5) / dimensions;
    uv = uv * 2.0 - 1.0;
    uv.y = -uv.y;  // 翻转 Y
    
    float3 origin = cameraPosition;
    float3 direction = normalize(
        cameraRight * uv.x +
        cameraUp * uv.y +
        cameraForward
    );
    
    RayDesc ray;
    ray.Origin = origin;
    ray.Direction = direction;
    ray.TMin = 0.001;
    ray.TMax = 1000.0;
    
    RayPayload payload;
    payload.color = float3(0.0, 0.0, 0.0);
    
    TraceRay(
        scene,
        RAY_FLAG_NONE,
        0xFF,
        0,
        0,
        0,
        ray,
        payload
    );
    
    outputTexture[index] = float4(payload.color, 1.0);
}
```

### Closest Hit Shader

#### 基本实现
```hlsl
struct RayPayload {
    float3 color;
};

struct Attributes {
    float2 barycentrics;
};

[shader("closesthit")]
void ClosestHitShader(inout RayPayload payload, in Attributes attr) {
    // 获取命中信息
    float3 hitPosition = WorldRayOrigin() + WorldRayDirection() * RayTCurrent();
    float3 normal = HitWorldNormal();
    
    // 计算光照
    float3 lightDir = normalize(lightPosition - hitPosition);
    float NdotL = saturate(dot(normal, lightDir));
    payload.color = albedo * lightColor * NdotL;
}
```

### Miss Shader

#### 基本实现
```hlsl
struct RayPayload {
    float3 color;
};

[shader("miss")]
void MissShader(inout RayPayload payload) {
    // 天空盒或环境光
    payload.color = float3(0.1, 0.2, 0.3);  // 天蓝色
}
```

### 加速结构使用

#### 构建加速结构
```cpp
// C++ 代码（示例）
D3D12_BUILD_RAYTRACING_ACCELERATION_STRUCTURE_DESC desc = {};
desc.Inputs.Type = D3D12_RAYTRACING_ACCELERATION_STRUCTURE_TYPE_TOP_LEVEL;
// ... 配置描述符
```

#### 使用加速结构
```hlsl
RaytracingAccelerationStructure scene : register(t0);

TraceRay(
    scene,              // 加速结构
    RAY_FLAG_NONE,      // 光线标志
    0xFF,               // 实例掩码
    0,                  // 命中组索引
    0,                  // 几何体索引
    0,                  // 着色器索引
    ray,                // 光线描述
    payload             // 有效载荷
);
```

---

## Wave 操作

### Wave 基础

#### Wave 概念
- **Wave**：一组同时执行的线程（通常是 32 或 64 个线程）
- **Lane**：Wave 中的单个线程
- **Wave 操作**：在 Wave 内进行数据交换和同步

#### 获取 Wave 信息
```hlsl
uint waveSize = WaveGetLaneCount();        // Wave 大小（通常是 32 或 64）
uint laneIndex = WaveGetLaneIndex();      // 当前线程在 Wave 中的索引
uint firstLaneIndex = WaveGetLaneIndex(); // 第一个线程的索引（总是 0）
```

### Wave 数据操作

#### WaveReadLaneFirst
```hlsl
// 从第一个线程读取值
float value = WaveReadLaneFirst(inputValue);
// 所有线程都得到第一个线程的值
```

#### WaveReadLaneAt
```hlsl
// 从指定索引的线程读取值
float value = WaveReadLaneAt(inputValue, laneIndex);
```

#### WaveActiveSum
```hlsl
// Wave 内所有线程的值求和
float sum = WaveActiveSum(inputValue);
// 每个线程都得到相同的总和
```

#### WaveActiveProduct
```hlsl
// Wave 内所有线程的值求积
float product = WaveActiveProduct(inputValue);
```

#### WaveActiveMin / WaveActiveMax
```hlsl
// Wave 内最小/最大值
float minVal = WaveActiveMin(inputValue);
float maxVal = WaveActiveMax(inputValue);
```

### Wave 前缀操作

#### WavePrefixSum
```hlsl
// 前缀和（每个线程得到前面所有线程的和）
float prefixSum = WavePrefixSum(inputValue);
// lane 0: inputValue[0]
// lane 1: inputValue[0] + inputValue[1]
// lane 2: inputValue[0] + inputValue[1] + inputValue[2]
// ...
```

#### WavePrefixProduct
```hlsl
// 前缀积
float prefixProduct = WavePrefixProduct(inputValue);
```

### Wave 布尔操作

#### WaveActiveAnyTrue / WaveActiveAllTrue
```hlsl
bool anyTrue = WaveActiveAnyTrue(condition);  // 任意线程为 true
bool allTrue = WaveActiveAllTrue(condition);  // 所有线程为 true
```

#### WaveActiveBallot
```hlsl
// 获取所有线程的布尔值（打包为位掩码）
uint4 ballot = WaveActiveBallot(condition);
// 每个位对应一个线程的 condition 值
```

### Wave 操作应用示例

#### 示例1：Wave 内归约
```hlsl
[numthreads(64, 1, 1)]
void CS_Reduce(uint3 id : SV_DispatchThreadID) {
    float value = inputData[id.x];
    
    // 使用 Wave 操作进行归约
    float waveSum = WaveActiveSum(value);
    
    // 只在第一个线程写入结果
    if (WaveGetLaneIndex() == 0) {
        outputData[id.x / WaveGetLaneCount()] = waveSum;
    }
}
```

#### 示例2：Wave 内排序
```hlsl
// 使用 Wave 操作实现简单的排序
float value = inputData[laneIndex];

// 比较并交换
for (uint i = 0; i < WaveGetLaneCount(); i++) {
    float otherValue = WaveReadLaneAt(value, i);
    bool shouldSwap = (laneIndex < i) ? (value > otherValue) : (value < otherValue);
    
    if (WaveActiveAnyTrue(shouldSwap)) {
        float minVal = WaveActiveMin(value);
        float maxVal = WaveActiveMax(value);
        value = (laneIndex < i) ? minVal : maxVal;
    }
}
```

---

## 实践任务

### 任务1：实现复杂的计算着色器算法

#### 实现并行归约
```hlsl
groupshared float sharedData[256];

[numthreads(256, 1, 1)]
void CS_Reduce(
    uint3 groupThreadID : SV_GroupThreadID,
    uint3 dispatchThreadID : SV_DispatchThreadID
) {
    uint index = groupThreadID.x;
    
    // 加载数据到共享内存
    sharedData[index] = inputBuffer[dispatchThreadID.x];
    
    GroupMemoryBarrierWithGroupSync();
    
    // 并行归约
    for (uint stride = 256 / 2; stride > 0; stride /= 2) {
        if (index < stride) {
            sharedData[index] += sharedData[index + stride];
        }
        GroupMemoryBarrierWithGroupSync();
    }
    
    // 第一个线程写入结果
    if (index == 0) {
        outputBuffer[groupID.x] = sharedData[0];
    }
}
```

### 任务2：实现简单的光线追踪效果

#### 实现基础光线追踪
```hlsl
[shader("raygeneration")]
void RayGenShader() {
    // 生成光线
    RayDesc ray = GenerateRay();
    
    RayPayload payload;
    TraceRay(scene, 0, 0xFF, 0, 0, 0, ray, payload);
    
    outputTexture[DispatchRaysIndex().xy] = float4(payload.color, 1.0);
}

[shader("closesthit")]
void ClosestHitShader(inout RayPayload payload, in Attributes attr) {
    // 计算光照
    payload.color = CalculateLighting();
    
    // 递归光线追踪（可选）
    if (depth < maxDepth) {
        RayDesc reflectedRay = GenerateReflectedRay();
        RayPayload reflectedPayload;
        TraceRay(scene, 0, 0xFF, 0, 0, 0, reflectedRay, reflectedPayload);
        payload.color += reflectedPayload.color * 0.5;
    }
}
```

---

## 🔗 相关链接

- [[Shader_Model_Versions]] - Wave 操作需要 SM 6.0+
- [[Resource_Binding]] - 计算着色器资源
- [[Performance_Optimization]] - 计算着色器优化
- [[Tools_Guide]] - 调试计算着色器

---

*最后更新：2024年*

