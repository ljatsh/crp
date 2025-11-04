# [[Common]] - Unity 着色器库核心基础

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl`
- **主要职责**：定义 Unity 着色器库的基础类型系统、数学工具函数、空间转换工具、纹理工具和深度编码/解码函数
- **使用场景**：几乎所有 Unity SRP 着色器的入口文件，其他 ShaderLibrary 文件通常不直接包含它，而是由用户着色器文件包含

## 依赖关系

- **依赖的文件**：
  - 平台特定 API 文件（[[D3D11]], [[GLCore]], [[GLES2]], [[GLES3]], [[Metal]], [[Vulkan]], [[Switch]] 等）
  - [[Validate]] - 平台验证宏
  - [[Macros]] - 宏定义
  - [[Random]] - 随机数生成
  - [[CommonDeprecated]] - 废弃函数
- **被依赖的文件**：几乎所有其他 ShaderLibrary 文件都间接依赖它

## 核心约定

### 坐标空间命名约定

Unity 使用后缀命名约定标识坐标空间：

- `WS`: World Space（世界空间）
- `RWS`: Camera-Relative World Space（相机相对世界空间）
- `VS`: View Space（视图空间）
- `OS`: Object Space（对象空间）
- `CS`: Homogeneous Clip Space（齐次裁剪空间）
- `TS`: Tangent Space（切线空间）
- `TXS`: Texture Space（纹理空间）

示例：`NormalWS` 表示世界空间法线

### 向量命名约定

- 大写字母表示归一化向量（除非前缀 `un`）
- `V`: View vector（视图向量）
- `L`: Light vector（光线向量）
- `N`: Normal vector（法线向量）
- `H`: Half vector（半角向量）
- `unL`: 未归一化的光线向量

### 类型系统

#### `real` 类型系统

Unity 使用 `real` 作为精度抽象类型，根据平台和设置自动选择 `half` 或 `float`：

```hlsl
#if REAL_IS_HALF
#define real half
#define real2 half2
#define real3 half3
#define real4 half4
#else
#define real float
#define real2 float2
#define real3 float3
#define real4 float4
#endif
```

**实现原理**：
- 在移动平台（`SHADER_API_MOBILE` 或 `SHADER_API_SWITCH`）默认使用 `half` 精度
- 可通过 `PREFER_HALF` 宏控制是否优先使用 `half`
- `half` 在支持 `min16float` 的平台上映射为 `min16float` 以获得更好的性能

**性能注意事项**：
- `half` 类型在移动平台可以减少带宽和寄存器占用
- 但精度较低，可能导致精度问题
- 关键计算（如深度、世界坐标）应使用 `float`

## 核心类型定义

### `PositionInputs` 结构体

用于统一管理屏幕空间位置信息：

```hlsl
struct PositionInputs
{
    float3 positionWS;  // World space position (could be camera-relative)
    float2 positionNDC; // Normalized screen coordinates : [0, 1)
    uint2  positionSS;  // Screen space pixel coordinates : [0, NumPixels)
    uint2  tileCoord;   // Screen tile coordinates : [0, NumTiles)
    float  deviceDepth; // Depth from the depth buffer : [0, 1]
    float  linearDepth; // View space Z coordinate : [Near, Far]
};
```

**用途**：
- 统一像素着色器和计算着色器中的位置信息访问
- 支持延迟渲染和计算着色器中的屏幕空间操作

**使用示例**：
```hlsl
PositionInputs posInput = GetPositionInput(input.positionSS, _ScreenSize.zw);
float3 positionWS = posInput.positionWS;
```

## 核心函数解析

### `SafeNormalize(float3 inVec)`

- **签名**：`real3 SafeNormalize(float3 inVec)`
- **功能**：安全的向量归一化，处理零向量情况
- **实现原理**：
  ```hlsl
  real3 SafeNormalize(float3 inVec)
  {
      float dp3 = max(FLT_MIN, dot(inVec, inVec));
      return inVec * rsqrt(dp3);
  }
  ```
  - 使用 `max(FLT_MIN, dot(inVec, inVec))` 避免除零
  - 使用 `rsqrt` 优化归一化计算（比 `normalize` 更快）
- **参数说明**：
  - `inVec`: 待归一化的向量（可以是零向量）
- **返回值**：归一化后的向量，零向量返回接近零的向量
- **性能注意事项**：
  - `rsqrt` 比 `sqrt` 后除法更快
  - 在移动平台上，`max` 操作可以避免 GPU 的异常处理开销

### `SafeDiv(real numer, real denom)`

- **签名**：`real SafeDiv(real numer, real denom)`
- **功能**：安全的除法运算，处理 `inf/inf` 和 `0/0` 情况
- **实现原理**：
  ```hlsl
  real SafeDiv(real numer, real denom)
  {
      return (numer != denom) ? numer / denom : 1;
  }
  ```
  - 当 `numer == denom` 时（包括 `inf/inf` 和 `0/0`）返回 1
  - 其他情况正常除法
- **数学原理**：
  - `inf/inf` 和 `0/0` 在数学上是未定义的，但着色器中通常期望返回 1
  - 这种处理方式在物理渲染中很常见，例如 Fresnel 项的计算

### `FastACos(real inX)`

- **签名**：`real FastACos(real inX)`
- **功能**：快速反余弦函数近似
- **实现原理**：
  ```hlsl
  real FastACosPos(real inX)
  {
      real x = abs(inX);
      real res = (0.0468878 * x + -0.203471) * x + 1.570796; // p(x)
      res *= sqrt(1.0 - x);
      return res;
  }
  
  real FastACos(real inX)
  {
      real res = FastACosPos(inX);
      return (inX >= 0) ? res : PI - res;
  }
  ```
  - 使用多项式近似（9 VALU 指令）
  - 先计算 `[0, PI/2]` 范围的结果，然后扩展到 `[0, PI]`
- **参考**：https://seblagarde.wordpress.com/2014/12/01/inverse-trigonometric-functions-gpu-optimization-for-amd-gcn-architecture/
- **性能注意事项**：
  - 比标准 `acos` 快约 5-10 倍
  - 精度足够用于大多数图形应用
  - 最大误差约为 0.0013 弧度

### `FastATan(real x)`

- **签名**：`real FastATan(real x)`
- **功能**：快速反正切函数近似
- **实现原理**：
  ```hlsl
  real FastATanPos(real x)
  {
      real t0 = (x < 1.0) ? x : 1.0 / x;
      real t1 = t0 * t0;
      real poly = 0.0872929;
      poly = -0.301895 + poly * t1;
      poly = 1.0 + poly * t1;
      poly = poly * t0;
      return (x < 1.0) ? poly : HALF_PI - poly;
  }
  ```
  - 使用 Eberly 的 5 次多项式近似
  - 通过 `x < 1` 的分支处理大值情况
- **性能**：4 VGPR, 16 FR（12 FR, 1 QR）, 2 scalar
- **输入范围**：`[-infinity, infinity]`，输出 `[-PI/2, PI/2]`

### `ComputeClipSpacePosition(float3 position, float4x4 clipSpaceTransform)`

- **签名**：`float4 ComputeClipSpacePosition(float3 position, float4x4 clipSpaceTransform = k_identity4x4)`
- **功能**：计算齐次裁剪空间位置
- **实现原理**：
  ```hlsl
  float4 ComputeClipSpacePosition(float3 position, float4x4 clipSpaceTransform = k_identity4x4)
  {
      return mul(clipSpaceTransform, float4(position, 1.0));
  }
  ```
  - 支持从不同空间转换（OS、WS、VS）
  - 通过 `clipSpaceTransform` 参数指定变换矩阵
- **使用示例**：
  ```hlsl
  // 从世界空间转换
  float4 positionCS = ComputeClipSpacePosition(positionWS, UNITY_MATRIX_VP);
  
  // 从视图空间转换
  float4 positionCS = ComputeClipSpacePosition(positionVS, UNITY_MATRIX_P);
  ```

### `ComputeNormalizedDeviceCoordinates(float3 position, float4x4 clipSpaceTransform)`

- **签名**：`float2 ComputeNormalizedDeviceCoordinates(float3 position, float4x4 clipSpaceTransform = k_identity4x4)`
- **功能**：计算标准化设备坐标（NDC）
- **实现原理**：
  ```hlsl
  float3 ComputeNormalizedDeviceCoordinatesWithZ(float3 position, float4x4 clipSpaceTransform = k_identity4x4)
  {
      float4 positionCS = ComputeClipSpacePosition(position, clipSpaceTransform);
      
      #if UNITY_UV_STARTS_AT_TOP
          positionCS.y = -positionCS.y; // Flip Y for D3D/Metal
      #endif
      
      positionCS *= rcp(positionCS.w); // Perspective divide
      positionCS.xy = positionCS.xy * 0.5 + 0.5; // Transform to [0,1]
      
      return positionCS.xyz;
  }
  ```
  - 执行透视除法（perspective divide）
  - 处理平台差异（D3D/Metal 需要翻转 Y）
  - 将坐标从 `[-1, 1]` 转换到 `[0, 1]`
- **数学原理**：
  - NDC 空间是齐次裁剪空间除以 w 分量后的结果
  - Unity 使用 `[0, 1]` 范围的 NDC，而不是标准的 `[-1, 1]`

### `LinearEyeDepth(float depth, float4 zBufferParam)`

- **签名**：`float LinearEyeDepth(float depth, float4 zBufferParam)`
- **功能**：将深度缓冲区值转换为线性视图空间深度
- **实现原理**：
  ```hlsl
  float LinearEyeDepth(float depth, float4 zBufferParam)
  {
      return 1.0 / (zBufferParam.z * depth + zBufferParam.w);
  }
  ```
  - `zBufferParam = { (f-n)/n, 1, (f-n)/n*f, 1/f }`
  - 假设透视投影，不支持正交投影
- **数学原理**：
  - 深度缓冲区存储的是非线性深度（通常是 `1/z`）
  - 线性深度 = `1 / (zBufferParam.z * depth + zBufferParam.w)`
  - 其中 `zBufferParam.z = (f-n)/(n*f)`，`zBufferParam.w = 1/f`

### `ComputeWorldSpacePosition(float2 positionNDC, float deviceDepth, float4x4 invViewProjMatrix)`

- **签名**：`float3 ComputeWorldSpacePosition(float2 positionNDC, float deviceDepth, float4x4 invViewProjMatrix)`
- **功能**：从 NDC 坐标和深度重建世界空间位置
- **实现原理**：
  ```hlsl
  float3 ComputeWorldSpacePosition(float2 positionNDC, float deviceDepth, float4x4 invViewProjMatrix)
  {
      float4 positionCS  = ComputeClipSpacePosition(positionNDC, deviceDepth);
      float4 hpositionWS = mul(invViewProjMatrix, positionCS);
      return hpositionWS.xyz / hpositionWS.w;
  }
  ```
  - 先将 NDC 转换为裁剪空间
  - 然后通过逆视图投影矩阵转换到世界空间
  - 最后执行透视除法
- **使用场景**：
  - 延迟渲染中的位置重建
  - 屏幕空间反射/折射
  - 后处理效果

### `LODDitheringTransition(uint2 fadeMaskSeed, float ditherFactor)`

- **签名**：`void LODDitheringTransition(uint2 fadeMaskSeed, float ditherFactor)`
- **功能**：LOD 过渡时的抖动处理，实现平滑的 LOD 切换
- **实现原理**：
  ```hlsl
  void LODDitheringTransition(uint2 fadeMaskSeed, float ditherFactor)
  {
      float p = GenerateHashedRandomFloat(fadeMaskSeed);
      float f = ditherFactor - CopySign(p, ditherFactor);
      clip(f);
  }
  ```
  - 使用空间相关的随机模式（不随时间变化，避免 TAA 噪声）
  - LOD0 使用 `ditherFactor` 从 `1..0`
  - LOD1 使用 `ditherFactor` 从 `-1..0`
  - 通过 `clip` 指令实现像素级别的混合
- **性能注意事项**：
  - `clip` 指令可能导致性能下降（特别是分支发散）
  - 但在 LOD 过渡区域影响较小
  - 空间模式避免时间抖动，保持 TAA 稳定性

### `QuadReadAcrossX/Y/Diagonal(float value, int2 screenPos)`

- **签名**：`float QuadReadAcrossX(float value, int2 screenPos)`
- **功能**：在像素着色器的四边形（quad）中读取相邻像素的值
- **实现原理**：
  ```hlsl
  float QuadReadAcrossX(float value, int2 screenPos)
  {
      return value - (ddx_fine(value) * (float(screenPos.x & 1) * 2.0 - 1.0));
  }
  ```
  - 利用像素着色器中的导数（derivatives）
  - `ddx_fine` 计算当前像素和右侧像素的差值
  - 通过位运算判断当前像素在 quad 中的位置
- **使用场景**：
  - 屏幕空间反射/折射
  - 自适应采样
  - 边缘检测

## 重要宏定义

### `REAL_IS_HALF`

控制是否使用 `half` 精度：

```hlsl
#if HAS_HALF && PREFER_HALF
#define REAL_IS_HALF 1
#else
#define REAL_IS_HALF 0
#endif
```

### `CBUFFER_START` / `CBUFFER_END`

常量缓冲区定义宏（平台特定）：

```hlsl
// D3D11/Metal/Vulkan
#define CBUFFER_START(name) cbuffer name {
#define CBUFFER_END };

// GLES2 (不支持常量缓冲区)
#define CBUFFER_START(name)
#define CBUFFER_END
```

### `TEXTURE2D` / `SAMPLER`

纹理和采样器定义宏（平台抽象）：

```hlsl
// D3D11/Metal/Vulkan
#define TEXTURE2D(textureName) Texture2D textureName
#define SAMPLER(samplerName) SamplerState samplerName

// GLES2
#define TEXTURE2D(textureName) sampler2D textureName
#define SAMPLER(samplerName)
```

### `SAMPLE_TEXTURE2D`

纹理采样宏（平台抽象）：

```hlsl
// D3D11/Metal/Vulkan
#define SAMPLE_TEXTURE2D(textureName, samplerName, coord2) \
    textureName.Sample(samplerName, coord2)

// GLES2
#define SAMPLE_TEXTURE2D(textureName, samplerName, coord2) \
    tex2D(textureName, coord2)
```

## 深度编码/解码

### 对数深度编码

Unity 支持对数深度编码以提高远距离精度：

```hlsl
float EncodeLogarithmicDepth(float z, float4 encodingParams)
{
    return log2(max(0, z * encodingParams.z)) * encodingParams.w;
}
```

**数学原理**：
- 标准深度：`d = (z - n) / (f - n)`，精度分布不均匀
- 对数深度：`d = log2(z) / log2(f)`，精度分布更均匀
- `encodingParams = { n, log2(f/n), 1/n, 1/log2(f/n) }`

**性能注意事项**：
- `log2` 比标准深度计算更昂贵
- 但可以提供更好的远距离精度
- 适合大场景渲染

## 空间转换工具

### `ComputeClipSpacePosition`

见核心函数解析部分

### `ComputeNormalizedDeviceCoordinates`

见核心函数解析部分

### `ComputeWorldSpacePosition`

见核心函数解析部分

## 纹理工具

### `ComputeTextureLOD`

计算纹理的 MIP 级别：

```hlsl
float ComputeTextureLOD(float2 uvdx, float2 uvdy, float2 scale, float bias = 0.0)
{
    float2 ddx_ = scale * uvdx;
    float2 ddy_ = scale * uvdy;
    float  d    = max(dot(ddx_, ddx_), dot(ddy_, ddy_));
    return max(0.5 * log2(d) - bias, 0.0);
}
```

**数学原理**：
- LOD = `0.5 * log2(max(|du/dx|², |du/dy|²))`
- 基于屏幕空间 UV 导数计算
- `bias` 用于手动调整 MIP 级别

## 与其他模块的关系

- [[SpaceTransforms]]：提供具体的空间变换函数（TransformObjectToWorld 等）
- [[CommonMaterial]]：使用本文件的基础类型和数学函数
- [[CommonLighting]]：依赖本文件的工具函数
- [[Packing]]：使用本文件的基础类型
- [[Debug]]：使用本文件的工具函数

## 参考资料

- Unity 官方文档：https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@14.0/manual/index.html
- Seb Lagarde 的博客：https://seblagarde.wordpress.com/
- GPU Gems 系列：https://developer.nvidia.com/gpugems

