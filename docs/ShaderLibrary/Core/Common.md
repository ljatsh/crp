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

**详细说明**：
- 关于 `positionSS` 和 `positionNDC` 的设计逻辑，参见 [[#PositionInputs 结构体的设计逻辑]]
- 关于 Screen Space 和 NDC Space 的详细定义，参见 [[#Screen Space（屏幕空间）详解]] 和 [[#NDC Space（标准化设备坐标）详解]]

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
- **详细说明**：
  - 关于透视投影矩阵和 w 分量的原理，参见 [[#透视投影矩阵的数学原理]]
  - 关于 NDC Space 的详细定义和用途，参见 [[#NDC Space（标准化设备坐标）详解]]
  - 关于 Screen Space 的详细说明，参见 [[#Screen Space（屏幕空间）详解]]

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

### ClipSpace（齐次裁剪空间）详解

#### ClipSpace 的定义

ClipSpace（齐次裁剪空间）是顶点着色器输出 `SV_POSITION` 的坐标空间。在这个空间中，顶点位置是齐次坐标 `(x, y, z, w)`，其中 `w` 分量用于透视除法。

#### ClipSpace Z 值的含义

ClipSpace 中的 Z 值表示深度信息，用于：
- **深度测试（Z-Test）**：判断像素是否被遮挡
- **深度缓冲区写入**：存储深度值用于后续深度比较
- **裁剪（Clipping）**：判断顶点是否在视锥体内

#### 不同平台的 ClipSpace Z 值范围

Unity 在不同平台上使用不同的 Z 值范围，这取决于平台是否支持 Reversed Z Buffer：

##### D3D11 / Metal / Vulkan / Switch（Reversed Z）

```hlsl
#define UNITY_NEAR_CLIP_VALUE (1.0)      // 近裁剪面
#define UNITY_RAW_FAR_CLIP_VALUE (0.0)  // 远裁剪面
```

- **ClipSpace Z 范围**：`[1.0, 0.0]`（近平面 → 远平面）
- **特点**：使用 Reversed Z，近平面为 1.0，远平面为 0.0
- **优势**：
  - 精度分布更均匀（浮点数在接近 0 时精度更高）
  - 使用 `1/z` 的非线性映射，在远距离有更好的精度
  - 50% 的精度分布在 [0.1, 1.0] 范围内（传统 Z 只有 25%）

##### OpenGL / GLES（传统 Z）

```hlsl
#define UNITY_NEAR_CLIP_VALUE (-1.0)    // 近裁剪面
#define UNITY_RAW_FAR_CLIP_VALUE (1.0)  // 远裁剪面
```

- **ClipSpace Z 范围**：`[-1.0, 1.0]`（近平面 → 远平面）
- **特点**：传统范围，近平面为 -1.0，远平面为 1.0
- **注意**：Unity 统一将深度缓冲区存储为 `[0, 1]` 范围

#### 关键宏定义

##### `UNITY_NEAR_CLIP_VALUE`

- **含义**：近裁剪面在 ClipSpace 中的 Z 值
- **D3D11/Metal/Vulkan**：`1.0`
- **OpenGL/GLES**：`-1.0`
- **用途**：手动设置顶点 Z 时使用（如全屏四边形）

**使用示例**：
```hlsl
float4 GetFullScreenTriangleVertexPosition(uint vertexID, float z = UNITY_NEAR_CLIP_VALUE)
{
    // z 默认使用 UNITY_NEAR_CLIP_VALUE
    // D3D11: 1.0 (近平面)
    // OpenGL: -1.0 (近平面)
    return float4(uv * 2.0 - 1.0, z, 1.0);
}
```

##### `UNITY_RAW_FAR_CLIP_VALUE`

- **含义**：远裁剪面在 ClipSpace 中的 Z 值（不经过投影矩阵转换）
- **D3D11/Metal/Vulkan**：`0.0`
- **OpenGL/GLES**：`1.0`
- **注意**：这是"原始"值，实际使用需经过投影矩阵

#### NDC 空间中的 Z 值

透视除法后（`z_ndc = z_clip / w_clip`），NDC 空间的 Z 值范围：

| 平台 | NDC Z 范围 | 深度缓冲区范围 | 说明 |
|------|-----------|--------------|------|
| D3D11/Metal/Vulkan | `[1.0, 0.0]` | `[1.0, 0.0]` | Reversed Z，近→远 |
| OpenGL/GLES | `[-1.0, 1.0]` | `[0.0, 1.0]` | Unity 统一转换 |

#### Reversed Z 的优势

1. **精度分布更均匀**：
   - 传统 Z：`[0, 1]`，大部分精度集中在近平面
   - Reversed Z：`[1, 0]`，精度分布更均匀

2. **数学优势**：
   - 使用 `1/z` 的非线性映射，Reversed Z 在远距离有更好的精度
   - 浮点数在接近 0 时精度更高

3. **示例对比**：
   假设 near=0.1, far=1000：
   - 传统 Z：50% 的精度在 [0.1, 1.0] 范围内
   - Reversed Z：精度分布更均匀，远距离精度更好

#### 深度缓冲区的存储

Unity 统一将深度缓冲区存储为 `[0, 1]` 范围，但含义不同：

- **D3D11/Metal/Vulkan (Reversed Z)**：
  - 深度缓冲区：`1.0 = 近平面`，`0.0 = 远平面`
  
- **OpenGL/GLES (Traditional Z)**：
  - 深度缓冲区：`0.0 = 近平面`，`1.0 = 远平面`

#### 实际使用建议

1. **不要硬编码 Z 值**：使用 `UNITY_NEAR_CLIP_VALUE` 等宏
2. **深度比较**：注意 Reversed Z 的比较方向（`>` vs `<`）
3. **深度重建**：使用 Unity 提供的函数（如 `LinearEyeDepth`）
4. **平台差异**：Unity 的 ShaderLibrary 已处理大部分差异，大多数情况下无需手动处理

#### 平台差异总结表

| 平台 | ClipSpace Z 范围 | 近平面值 | 远平面值 | Reversed Z | 深度缓冲区范围 |
|------|------------------|---------|---------|------------|--------------|
| D3D11 | [1.0, 0.0] | 1.0 | 0.0 | ✅ | [1.0, 0.0] |
| Metal | [1.0, 0.0] | 1.0 | 0.0 | ✅ | [1.0, 0.0] |
| Vulkan | [1.0, 0.0] | 1.0 | 0.0 | ✅ | [1.0, 0.0] |
| OpenGL | [-1.0, 1.0] | -1.0 | 1.0 | ❌ | [0.0, 1.0] |
| GLES | [-1.0, 1.0] | -1.0 | 1.0 | ❌ | [0.0, 1.0] |

#### 透视投影矩阵的数学原理

理解透视投影矩阵是理解 ClipSpace 和 w 分量的关键。

##### 齐次坐标与 ClipSpace

ClipSpace 不是简单的 `[-1, 1]` 范围，而是**齐次坐标空间（4D）**，表示为 `(x, y, z, w)`。

**关键概念**：
- ClipSpace 中的坐标是齐次坐标，必须除以 w 才能得到真正的 3D 坐标
- w 分量存储了视图空间的深度信息
- 只有除以 w 之后，坐标才真正在 `[-1, 1]` 范围内（标准 NDC）

##### 标准透视投影矩阵

**右手坐标系下的透视投影矩阵**（简化形式）：

```
P = [ f/aspect  0      0           0      ]
    [ 0         f      0           0      ]
    [ 0         0    (f+n)/(f-n)  -2fn/(f-n) ]
    [ 0         0      -1         0      ]
```

其中：
- `f` = far plane distance（远裁剪面距离）
- `n` = near plane distance（近裁剪面距离）
- `aspect` = aspect ratio（宽高比）
- `f` = `1 / tan(fov/2)`（FOV 相关的缩放因子）

**关键观察**：矩阵的第 4 行是 `[0, 0, -1, 0]`，这意味着：
- `w_clip = -z_view`

##### w 分量的含义

在透视投影中，w 分量存储的是**视图空间的深度（取负）**：

```
w_clip = -z_view
```

**示例**：
- 近平面：`z_view = -n`，所以 `w_clip = -(-n) = n`（正值）
- 远平面：`z_view = -f`，所以 `w_clip = -(-f) = f`（正值）
- 视锥体内的点：`w_clip` 在 `[n, f]` 范围内

##### 为什么必须除以 w？

###### 1. 齐次坐标到笛卡尔坐标的转换

齐次坐标 `(x, y, z, w)` 表示的是 3D 点 `(x/w, y/w, z/w)`。只有除以 w 后，才能得到真正的 3D 坐标。

**数学定义**：
```
NDC_x = clip_x / clip_w
NDC_y = clip_y / clip_w
NDC_z = clip_z / clip_w
```

###### 2. 透视除法的数学意义

透视除法实现了**透视效果**：
- 远处的物体看起来更小（因为除以更大的 w）
- 近处的物体看起来更大（因为除以更小的 w）

**数学原理**：
假设一个点在视图空间中：`positionVS = (10, 5, -20)`

经过投影矩阵后：
- `x_clip = 10 * f/aspect`（假设 = 200）
- `y_clip = 5 * f`（假设 = 100）
- `z_clip = ...`（深度相关）
- `w_clip = -z_view = -(-20) = 20`

**除以 w 之前**：
- `x_clip = 200`（不在 `[-1, 1]` 范围内）
- `y_clip = 100`（不在 `[-1, 1]` 范围内）

**除以 w 之后**：
- `NDC_x = 200 / 20 = 10`（如果 > 1，说明在视锥体外）
- `NDC_y = 100 / 20 = 5`（如果 > 1，说明在视锥体外）

对于视锥体内的点，除以 w 后会在 `[-1, 1]` 范围内。

###### 3. 透视投影的几何意义

透视投影模拟了人眼的视觉系统：
- 平行线在远处会汇聚（消失点）
- 距离越远，物体越小
- 这些效果都是通过除以 w 实现的

**几何解释**：
```
        Camera
          |
          |  (近平面)
          |----|----
          |    |    |
          |    |    |  (远平面)
          |----|----|
          |    |    |
          |    |    |
```

近处的物体在屏幕上的投影更大，远处的物体投影更小。这个缩放比例就是 `1/w`。

##### Unity 中的实现

看 `ComputeNormalizedDeviceCoordinatesWithZ` 的实现：

```hlsl
float3 ComputeNormalizedDeviceCoordinatesWithZ(float3 position, float4x4 clipSpaceTransform = k_identity4x4)
{
    float4 positionCS = ComputeClipSpacePosition(position, clipSpaceTransform);
    
    #if UNITY_UV_STARTS_AT_TOP
        positionCS.y = -positionCS.y; // Flip Y for D3D/Metal
    #endif
    
    positionCS *= rcp(positionCS.w); // 透视除法
    positionCS.xy = positionCS.xy * 0.5 + 0.5; // Transform to [0,1]
    
    return positionCS.xyz;
}
```

**步骤解析**：

1. **`positionCS *= rcp(positionCS.w)`**：透视除法
   - 等价于 `positionCS.xyz /= positionCS.w`
   - 将齐次坐标转换为 3D 坐标
   - 此时 `positionCS.xy` 在 `[-1, 1]` 范围内（标准 NDC）

2. **`positionCS.xy = positionCS.xy * 0.5 + 0.5`**：映射到 Unity 的 `[0, 1]` NDC 范围
   - `[-1, 1]` → `[0, 1]`
   - Unity 使用非标准的 NDC 范围

##### 正交投影的特殊情况

对于正交投影：
- 投影矩阵的第 4 行是 `[0, 0, 0, 1]`
- `w = 1`（常数）
- 除以 w 不会改变坐标值，但仍然需要这一步以保持一致性

**正交投影矩阵**（简化形式）：
```
P = [ 1/aspect  0      0           0      ]
    [ 0         1      0           0      ]
    [ 0         0    -2/(f-n)  -(f+n)/(f-n) ]
    [ 0         0      0         1      ]
```

注意第 4 行是 `[0, 0, 0, 1]`，所以 `w = 1`（常数）。

##### 总结

1. **ClipSpace 是齐次坐标空间（4D）**，不是简单的 `[-1, 1]`
2. **w 分量存储视图空间的深度**：`w = -z_view`
3. **必须除以 w 才能**：
   - 将齐次坐标转换为 3D 坐标
   - 实现透视效果
   - 使坐标在 `[-1, 1]` 范围内（标准 NDC）
4. **Unity 进一步将 `[-1, 1]` 映射到 `[0, 1]` 范围**

这就是为什么代码中需要 `positionCS *= rcp(positionCS.w)` 的原因。

#### NDC Space（标准化设备坐标）详解

##### NDC Space 的定义

NDC Space（Normalized Device Coordinates，标准化设备坐标）是透视除法后的坐标空间。

**关键特性**：
- **坐标范围**：Unity 使用 `[0, 1)` 范围（非标准的 `[-1, 1]`）
- **坐标轴方向**：Y 轴向上（与 World Space、View Space、Screen Space 一致）
- **原点位置**：`(0, 0)` 位于左下角（OpenGL 约定）

##### NDC Space 的转换过程

**从 ClipSpace 到 NDC Space**：

```hlsl
// 步骤 1：透视除法
positionCS *= rcp(positionCS.w);
// 此时 positionCS.xy 在 [-1, 1] 范围内（标准 NDC）

// 步骤 2：映射到 Unity 的 [0, 1] 范围
positionCS.xy = positionCS.xy * 0.5 + 0.5;
// 现在 positionCS.xy 在 [0, 1] 范围内（Unity NDC）
```

**数学公式**：
```
NDC_x = (clip_x / clip_w) * 0.5 + 0.5
NDC_y = (clip_y / clip_w) * 0.5 + 0.5
NDC_z = clip_z / clip_w
```

##### NDC Space 的坐标范围

| 坐标轴 | 标准 NDC | Unity NDC | 说明 |
|--------|---------|----------|------|
| X | `[-1, 1]` | `[0, 1)` | 左 → 右 |
| Y | `[-1, 1]` | `[0, 1)` | 下 → 上 |
| Z | `[-1, 1]` 或 `[1, 0]` | `[0, 1]` | 取决于平台（Reversed Z） |

**Unity 的 NDC 范围**：
- X/Y：`[0, 1)`（半开区间，1.0 不包含在内）
- Z：`[0, 1]`（深度值，平台相关）

##### NDC Space 的用途

1. **屏幕空间纹理采样**：
   - 使用 NDC 坐标直接采样屏幕纹理
   - `SAMPLE_TEXTURE2D(_CameraTexture, sampler_CameraTexture, positionNDC.xy)`

2. **位置重建**：
   - 从 NDC 和深度重建世界空间位置
   - `ComputeWorldSpacePosition(positionNDC, deviceDepth, invViewProjMatrix)`

3. **后处理效果**：
   - 全屏后处理使用 NDC 坐标
   - 屏幕空间反射/折射

##### NDC Space 与 ClipSpace 的关系

**转换关系**：
```
ClipSpace (齐次坐标) → 透视除法 → 标准 NDC [-1, 1] → Unity NDC [0, 1]
```

**关键区别**：
- **ClipSpace**：齐次坐标 `(x, y, z, w)`，w 存储深度信息
- **NDC Space**：3D 坐标 `(x, y, z)`，已除以 w，范围归一化

#### Screen Space（屏幕空间）详解

##### Screen Space 的定义

Screen Space（屏幕空间）是像素坐标空间，表示屏幕上的实际像素位置。

**关键特性**：
- **坐标范围**：`[0, NumPixels)`（整数坐标）
- **坐标轴方向**：Y 轴向上（与 World Space、View Space、NDC Space 一致）
- **原点位置**：`(0, 0)` 位于左下角
- **数据类型**：`uint2`（无符号整数）

##### Screen Space 的坐标表示

从 `PositionInputs` 结构可以看到：

```hlsl
struct PositionInputs
{
    float3 positionWS;  // World space position
    float2 positionNDC; // Normalized screen coordinates [0, 1)
    uint2  positionSS;  // Screen space pixel coordinates [0, NumPixels)
    uint2  tileCoord;   // Screen tile coordinates
    float  deviceDepth; // Depth from depth buffer
    float  linearDepth; // View space Z coordinate
};
```

**`positionSS` 的含义**：
- `positionSS.x`：屏幕 X 方向的像素索引（0 到 width-1）
- `positionSS.y`：屏幕 Y 方向的像素索引（0 到 height-1）

##### `PositionInputs` 结构体的设计逻辑

**为什么 `positionSS` 和 `positionNDC` 都在结构体中？**

这是一个常见的设计问题：既然 `positionNDC` 是从 `positionSS` 计算出来的，为什么两者都要存储在结构体中？

**设计原因**：

1. **`positionSS` 是输入参数，不是计算出来的**：
   - **像素着色器**：`positionSS` 来自 `SV_Position`（硬件自动提供）
     ```hlsl
     float4 frag(VertexOutput input) : SV_Target
     {
         // SV_Position 已经是屏幕空间坐标（像素中心）
         PositionInputs posInput = GetPositionInput(input.positionCS.xy, _ScreenParams.zw);
         // ...
     }
     ```
   
   - **计算着色器**：`positionSS` 需要手动计算
     ```hlsl
     [numthreads(8, 8, 1)]
     void CSMain(uint3 id : SV_DispatchThreadID)
     {
         // 手动计算屏幕空间坐标
         uint2 positionSS = id.xy;  // 或者 groupId.xy * BLOCK_SIZE + groupThreadId.xy
         PositionInputs posInput = GetPositionInput(positionSS, _ScreenParams.zw);
         // ...
     }
     ```

2. **`positionNDC` 是从 `positionSS` 计算出来的**：
   - 通过归一化操作：`positionNDC = (positionSS + offset) * invScreenSize`
   - 存储在结构体中是为了**避免重复计算**

3. **为什么两者都存储？**（缓存计算结果的设计模式）
   - **避免重复计算**：`positionNDC` 只需计算一次，后续直接使用
   - **避免重复传递**：多个函数可能需要 `positionSS` 或 `positionNDC`，统一存储在结构体中
   - **统一接口**：不同着色器阶段（像素/计算/光线追踪）使用同一结构体
   - **调试方便**：可以同时访问原始坐标和归一化坐标

**实际使用示例**：

```hlsl
// 示例：屏幕空间反射
PositionInputs posInput = GetPositionInput(positionSS, _ScreenParams.zw, deviceDepth, 
                                            _InvViewProjMatrix, _ViewMatrix);

// 后续代码可以直接使用，无需重复计算：
float3 positionWS = posInput.positionWS;        // 已计算好的世界空间位置
float2 positionNDC = posInput.positionNDC;     // 已计算好的 NDC 坐标（用于纹理采样）
uint2 positionSS = posInput.positionSS;         // 原始的屏幕空间坐标（用于调试或直接索引）
float deviceDepth = posInput.deviceDepth;      // 深度值
```

**总结**：
- `positionSS`：**输入参数**（来自 `SV_Position` 或手动计算）
- `positionNDC`：**从 `positionSS` 计算出来**（归一化）
- 存储在结构体中：**避免重复计算和传递**，提供统一接口

##### Screen Space 与 NDC Space 的转换

**从 Screen Space 到 NDC Space**：

```hlsl
PositionInputs GetPositionInput(float2 positionSS, float2 invScreenSize, uint2 tileCoord)
{
    PositionInputs posInput;
    
    posInput.positionNDC = positionSS;
    
    #if defined(SHADER_STAGE_COMPUTE) || defined(SHADER_STAGE_RAY_TRACING)
        // 计算着色器中添加 0.5 偏移，对齐到像素中心
        posInput.positionNDC.xy += float2(0.5, 0.5);
    #endif
    
    // 归一化到 [0, 1] 范围
    posInput.positionNDC *= invScreenSize;  // invScreenSize = (1/width, 1/height)
    
    posInput.positionSS = uint2(positionSS);
    
    return posInput;
}
```

**转换公式**：
```
NDC_x = (SS_x + offset) / width
NDC_y = (SS_y + offset) / height
```

其中：
- `offset = 0.5`（计算着色器中，对齐到像素中心）
- `offset = 0`（像素着色器中，使用 `SV_Position`）

**从 NDC Space 到 Screen Space**：

```hlsl
float2 positionSS = positionNDC * screenSize;
// 如果需要像素中心坐标：
float2 positionSS_center = positionNDC * screenSize - 0.5;
```

##### Screen Space 的获取方式

###### 1. 像素着色器中

使用 `SV_Position` 语义：

```hlsl
struct VertexOutput
{
    float4 positionCS : SV_POSITION;
    // ...
};

float4 frag(VertexOutput input) : SV_Target
{
    // SV_Position 已经是屏幕空间坐标（像素中心）
    float2 positionSS = input.positionCS.xy;
    
    // 转换为 NDC
    float2 positionNDC = positionSS * _ScreenParams.zw;  // zw = (1/width, 1/height)
    
    return float4(positionNDC, 0, 1);
}
```

**注意**：`SV_Position` 在像素着色器中已经是**像素中心坐标**，不需要添加 0.5 偏移。

###### 2. 计算着色器中

手动计算像素坐标：

```hlsl
[numthreads(8, 8, 1)]
void CSMain(uint3 id : SV_DispatchThreadID)
{
    // id.xy 是屏幕空间像素坐标（整数）
    uint2 positionSS = id.xy;
    
    // 转换为 NDC（需要添加 0.5 偏移对齐到像素中心）
    float2 positionNDC = (positionSS + 0.5) * _ScreenParams.zw;
    
    // ...
}
```

**关键区别**：
- 像素着色器：`SV_Position` 已经是像素中心，不需要偏移
- 计算着色器：整数坐标需要添加 0.5 偏移才能对齐到像素中心

##### Screen Space 的用途

1. **屏幕空间效果**：
   - 屏幕空间反射（SSR）
   - 屏幕空间环境光遮蔽（SSAO）
   - 屏幕空间全局光照（SSGI）

2. **纹理采样**：
   - 使用像素坐标直接采样屏幕纹理
   - `_CameraTexture[positionSS]`（计算着色器中）

3. **平铺渲染（Tiled Rendering）**：
   - 使用 `tileCoord` 进行平铺计算
   - 延迟渲染中的光照计算

##### Screen Space 与 NDC Space 的对比

| 特性 | Screen Space | NDC Space |
|------|-------------|-----------|
| **坐标类型** | 整数（`uint2`） | 浮点数（`float2`） |
| **坐标范围** | `[0, NumPixels)` | `[0, 1)` |
| **分辨率相关** | ✅ 是 | ❌ 否 |
| **像素中心对齐** | 需要手动处理 | 自动处理 |
| **用途** | 像素级操作 | 归一化操作 |

##### 实际使用示例

**示例 1：屏幕空间纹理采样**

```hlsl
// 方法 1：使用 NDC 坐标（推荐）
float2 positionNDC = GetPositionInput(...).positionNDC;
float4 color = SAMPLE_TEXTURE2D(_CameraTexture, sampler_CameraTexture, positionNDC);

// 方法 2：使用 Screen Space 坐标（计算着色器）
uint2 positionSS = id.xy;
float2 positionNDC = (positionSS + 0.5) * _ScreenParams.zw;
float4 color = SAMPLE_TEXTURE2D(_CameraTexture, sampler_CameraTexture, positionNDC);
```

**示例 2：位置重建**

```hlsl
PositionInputs posInput = GetPositionInput(positionSS, _ScreenParams.zw);
float3 positionWS = ComputeWorldSpacePosition(
    posInput.positionNDC, 
    posInput.deviceDepth, 
    _InvViewProjMatrix
);
```

**示例 3：像素中心对齐**

```hlsl
// 像素着色器中（SV_Position 已经是像素中心）
float2 positionSS = input.positionCS.xy;  // 已经是像素中心

// 计算着色器中（需要添加偏移）
float2 positionSS_center = positionSS + 0.5;  // 对齐到像素中心
```

#### 阴影投影矩阵的 Z 轴翻转处理

在阴影渲染系统中，Unity 需要手动处理投影矩阵的 Z 轴翻转，以适配 Reversed Z Buffer 平台。

**核心实现逻辑**（来自 `ShadowUtils.GetShadowTransform`）：

```csharp
static Matrix4x4 GetShadowTransform(Matrix4x4 proj, Matrix4x4 view)
{
    // Currently CullResults ComputeDirectionalShadowMatricesAndCullingPrimitives doesn't
    // apply z reversal to projection matrix. We need to do it manually here.
    if (SystemInfo.usesReversedZBuffer)
    {
        proj.m20 = -proj.m20;  // 翻转第3行第1列
        proj.m21 = -proj.m21;  // 翻转第3行第2列
        proj.m22 = -proj.m22;  // 翻转第3行第3列（Z缩放）
        proj.m23 = -proj.m23;  // 翻转第3行第4列（Z偏移）
    }

    Matrix4x4 worldToShadow = proj * view;

    var textureScaleAndBias = Matrix4x4.identity;
    textureScaleAndBias.m00 = 0.5f;
    textureScaleAndBias.m11 = 0.5f;
    textureScaleAndBias.m22 = 0.5f;
    textureScaleAndBias.m03 = 0.5f;
    textureScaleAndBias.m23 = 0.5f;
    textureScaleAndBias.m13 = 0.5f;
    // textureScaleAndBias maps texture space coordinates from [-1,1] to [0,1]

    // Apply texture scale and offset to save a MAD in shader.
    return textureScaleAndBias * worldToShadow;
}
```

**为什么需要这个处理？**

1. **Unity 原生 API 的限制**：
   - `CullResults.ComputeDirectionalShadowMatricesAndCullingPrimitives` 不会自动应用 Z 反转
   - 需要手动翻转投影矩阵的第3行（Z相关）以适配 Reversed Z Buffer

2. **矩阵索引说明**：
   投影矩阵的第3行（索引2）对应 Z 坐标变换：
   - `m20`：Z 对 X 的影响
   - `m21`：Z 对 Y 的影响
   - `m22`：Z 缩放（深度范围）
   - `m23`：Z 偏移（深度偏移）

3. **翻转操作**：
   - 当 `SystemInfo.usesReversedZBuffer = true` 时（D3D11/Metal/Vulkan）
   - 将投影矩阵的第3行所有元素取反
   - 这确保了阴影贴图在 Reversed Z Buffer 平台上正确采样

4. **纹理空间映射**：
   - `textureScaleAndBias` 矩阵将坐标从 `[-1, 1]` 映射到 `[0, 1]`
   - 这是阴影贴图采样所需的标准纹理坐标范围

**使用场景**：
- 方向光阴影（Directional Light Shadows）
- 聚光灯阴影（Spot Light Shadows）
- 点光源阴影（Point Light Shadows）

**注意事项**：
- 这个处理只在 C# 端进行，Shader 端不需要手动处理
- Unity 的 ShaderLibrary 已经处理了平台差异
- 阴影贴图使用深度比较采样，需要正确的 Z 值范围才能正确采样

### `ComputeClipSpacePosition`

见核心函数解析部分

### `ComputeNormalizedDeviceCoordinates`

见核心函数解析部分

### `ComputeWorldSpacePosition`

见核心函数解析部分

## 纹理工具

### 纹理空间坐标系

#### 标准 UV 坐标系

Unity 使用标准的纹理坐标系统：
- **UV 范围**：`[0, 1]` × `[0, 1]`
- **原点位置**：`(0, 0)` 位于纹理左下角（OpenGL 约定）
- **坐标轴**：
  - U 轴：从左到右（0 → 1）
  - V 轴：从下到上（0 → 1）

#### 像素中心 vs 像素边界

在纹理空间中，有两种重要的坐标概念：

**像素边界坐标**：
- 第一个像素的左边界：`u = 0`
- 第一个像素的右边界：`u = 1/width`
- 最后一个像素的右边界：`u = 1`

**像素中心坐标**：
- 第一个像素的中心：`u = 0.5/width`
- 最后一个像素的中心：`u = 1 - 0.5/width`
- 范围：`[0.5/size, 1 - 0.5/size]`

**为什么重要？**
- 精确采样时，应该使用像素中心坐标
- 避免边界采样问题（如纹理边缘的重复）
- 确保采样结果的一致性

#### `RemapHalfTexelCoordTo01`

将像素中心坐标映射到标准 `[0, 1]` UV 范围：

```hlsl
// Remap: [0.5 / size, 1 - 0.5 / size] -> [0, 1]
real2 RemapHalfTexelCoordTo01(real2 coord, real2 size)
{
    const real2 rcpLen              = size * rcp(size - 1);
    const real2 startTimesRcpLength = 0.5 * rcp(size - 1);

    return Remap01(coord, rcpLen, startTimesRcpLength);
}
```

**数学推导**：
- 输入范围：`[0.5/size, 1 - 0.5/size]`
- 输出范围：`[0, 1]`
- 映射公式：`result = (coord - start) / (end - start)`
  - `start = 0.5/size`
  - `end = 1 - 0.5/size`
  - `end - start = 1 - 1/size = (size - 1)/size`

**使用示例**：
```hlsl
// 假设纹理大小为 256×256
float2 texelSize = float2(1.0/256.0, 1.0/256.0);
float2 pixelCenterCoord = float2(0.5/256.0, 0.5/256.0);  // 第一个像素中心

// 映射到标准 UV
float2 standardUV = RemapHalfTexelCoordTo01(pixelCenterCoord, float2(256, 256));
// 结果：standardUV = (0, 0)
```

#### `Remap01ToHalfTexelCoord`

反向映射：将标准 `[0, 1]` UV 映射回像素中心坐标：

```hlsl
// Remap: [0, 1] -> [0.5 / size, 1 - 0.5 / size]
real2 Remap01ToHalfTexelCoord(real2 coord, real2 size)
{
    const real2 start = 0.5 * rcp(size);
    const real2 len   = 1 - rcp(size);

    return coord * len + start;
}
```

**使用场景**：
- 精确的纹理采样
- 避免边界采样问题
- 确保采样结果的一致性

### `ComputeTextureLOD` - 纹理 LOD 计算详解

#### 概述

`ComputeTextureLOD` 用于计算纹理的 MIP 级别（Level of Detail），基于屏幕空间中纹理坐标的变化率。

#### 核心算法

**基础版本**：

```hlsl
float ComputeTextureLOD(float2 uvdx, float2 uvdy, float2 scale, float bias = 0.0)
{
    float2 ddx_ = scale * uvdx;
    float2 ddy_ = scale * uvdy;
    float  d    = max(dot(ddx_, ddx_), dot(ddy_, ddy_));
    return max(0.5 * log2(d) - bias, 0.0);
}
```

#### 屏幕空间导数：`ddx` 和 `ddy`

**函数定义**：
- `ddx(value)`：计算当前像素和右侧像素的差值（X 方向导数）
- `ddy(value)`：计算当前像素和上方像素的差值（Y 方向导数）

**工作原理**：
- GPU 以 2×2 像素的四边形（quad）为单位执行像素着色器
- `ddx` 计算 quad 中左右两个像素的差值
- `ddy` 计算 quad 中上下两个像素的差值
- 这些导数反映了屏幕空间中值的变化率

**数学含义**：
- `ddx(uv)` = `∂uv/∂x`（屏幕空间 X 方向的 UV 变化率）
- `ddy(uv)` = `∂uv/∂y`（屏幕空间 Y 方向的 UV 变化率）

**使用限制**：
- 只能在像素着色器中使用
- 在计算着色器中不可用（需要使用其他方法计算导数）

#### LOD 计算公式推导

**步骤 1：计算 UV 导数的长度**

```hlsl
float2 ddx_ = scale * uvdx;  // 缩放后的 X 方向导数
float2 ddy_ = scale * uvdy;  // 缩放后的 Y 方向导数
```

**步骤 2：计算导数的平方长度**

```hlsl
float lenDxSqr = dot(ddx_, ddx_);  // |∂uv/∂x|²
float lenDySqr = dot(ddy_, ddy_);  // |∂uv/∂y|²
```

**步骤 3：取最大值**

```hlsl
float d = max(lenDxSqr, lenDySqr);  // max(|∂uv/∂x|², |∂uv/∂y|²)
```

**步骤 4：计算 LOD**

```hlsl
float lod = 0.5 * log2(d) - bias;
```

**数学原理**：
- `d` 表示屏幕空间中纹理坐标的最大变化率（平方）
- `log2(d)` 将变化率转换为对数尺度
- `0.5 * log2(d)` 对应 MIP 级别（每级 MIP 纹理尺寸减半）
- `bias` 用于手动调整 LOD（正值 = 更模糊，负值 = 更清晰）

**完整公式**：
```
LOD = 0.5 * log₂(max(|∂uv/∂x|², |∂uv/∂y|²)) - bias
```

#### 函数重载版本对比

##### 版本 1：使用预计算的导数

```hlsl
float ComputeTextureLOD(float2 uvdx, float2 uvdy, float2 scale, float bias = 0.0)
```

- **参数**：
  - `uvdx`：预计算的 `ddx(uv)`
  - `uvdy`：预计算的 `ddy(uv)`
  - `scale`：缩放因子（通常用于 texelSize）
  - `bias`：LOD 偏移
- **使用场景**：需要重用导数或自定义缩放时

##### 版本 2：自动计算导数

```hlsl
float ComputeTextureLOD(float2 uv, float bias = 0.0)
{
    float2 ddx_ = ddx(uv);
    float2 ddy_ = ddy(uv);
    return ComputeTextureLOD(ddx_, ddy_, 1.0, bias);
}
```

- **参数**：
  - `uv`：纹理坐标
  - `bias`：LOD 偏移
- **使用场景**：最常见的用法，自动计算导数

##### 版本 3：使用 texelSize

```hlsl
float ComputeTextureLOD(float2 uv, float2 texelSize, float bias = 0.0)
{
    uv *= texelSize;
    return ComputeTextureLOD(uv, bias);
}
```

- **参数**：
  - `uv`：纹理坐标
  - `texelSize`：纹理像素大小（通常为 `1/width, 1/height`）
  - `bias`：LOD 偏移
- **使用场景**：需要将 UV 坐标转换为像素空间时
- **注意**：`texelSize` 的格式通常是 `(width, height, 1/width, 1/height)`，这里使用 `.xy` 或 `.zw` 取决于具体实现

##### 版本 4：3D 纹理支持

```hlsl
float ComputeTextureLOD(float3 duvw_dx, float3 duvw_dy, float3 duvw_dz, float scale, float bias = 0.0)
{
    float d = Max3(dot(duvw_dx, duvw_dx), dot(duvw_dy, duvw_dy), dot(duvw_dz, duvw_dz));
    return max(0.5f * log2(d * (scale * scale)) - bias, 0.0);
}
```

- **参数**：
  - `duvw_dx`：X 方向的 UVW 导数
  - `duvw_dy`：Y 方向的 UVW 导数
  - `duvw_dz`：Z 方向的 UVW 导数（3D 纹理特有）
  - `scale`：缩放因子
  - `bias`：LOD 偏移
- **使用场景**：3D 纹理或体积纹理的 LOD 计算
- **特点**：考虑三个方向的导数，取最大值

#### MIP 映射原理

**MIP 映射（Multum In Parvo）**：
- 预先生成多个分辨率的纹理版本
- MIP 0：原始分辨率
- MIP 1：1/2 分辨率
- MIP 2：1/4 分辨率
- 以此类推

**LOD 值的含义**：
- `LOD = 0`：使用原始分辨率（MIP 0）
- `LOD = 1`：使用 1/2 分辨率（MIP 1）
- `LOD = 2`：使用 1/4 分辨率（MIP 2）
- 非整数 LOD：在相邻 MIP 级别之间线性插值（三线性过滤）

**为什么需要 MIP 映射？**
- 避免远距离纹理的锯齿（aliasing）
- 提高缓存效率（小纹理更容易命中缓存）
- 减少带宽消耗

#### 各向异性过滤基础

**各向同性 vs 各向异性**：

- **各向同性**：假设纹理在屏幕空间中均匀缩放
- **各向异性**：考虑纹理在不同方向上的不同缩放

**各向异性过滤**：
- 当纹理在屏幕空间中发生倾斜时（如地面纹理）
- X 方向和 Y 方向的缩放比例不同
- 需要采样多个点来正确重建纹理

**Unity 的实现**：
- `ComputeTextureLOD` 使用 `max(|∂uv/∂x|², |∂uv/∂y|²)` 计算各向同性 LOD
- 各向异性过滤由硬件自动处理（通过采样器状态）
- 可以通过 `SampleGrad` 手动指定各方向的导数

#### 使用示例

**示例 1：基本用法**

```hlsl
float2 uv = input.texcoord;
float lod = ComputeTextureLOD(uv);
float4 color = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, lod);
```

**示例 2：使用 texelSize**

```hlsl
float2 uv = input.texcoord;
float lod = ComputeTextureLOD(uv, _MainTex_TexelSize.zw);  // 使用 1/width, 1/height
float4 color = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, lod);
```

**示例 3：自定义 LOD Bias**

```hlsl
float2 uv = input.texcoord;
float lod = ComputeTextureLOD(uv, -0.5);  // 负 bias = 更清晰的纹理
float4 color = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, lod);
```

**示例 4：重用导数**

```hlsl
float2 uv = input.texcoord;
float2 ddx_uv = ddx(uv);
float2 ddy_uv = ddy(uv);

// 计算多个纹理的 LOD（重用导数）
float lod1 = ComputeTextureLOD(ddx_uv, ddy_uv, float2(1, 1));
float lod2 = ComputeTextureLOD(ddx_uv, ddy_uv, _Tex2_TexelSize.zw);
```

#### 性能注意事项

1. **`ddx`/`ddy` 的成本**：
   - 在像素着色器中，这些函数是"免费"的（硬件自动计算）
   - 但在计算着色器中需要手动计算

2. **LOD 计算成本**：
   - `log2` 操作相对昂贵
   - 如果不需要精确 LOD，可以考虑使用近似方法

3. **MIP Bias 的影响**：
   - 负 bias（更清晰）：可能增加带宽和缓存未命中
   - 正 bias（更模糊）：减少带宽，但可能降低视觉质量

### `GetMipCount`

获取纹理的 MIP 级别数量：

```hlsl
uint GetMipCount(TEXTURE2D_PARAM(tex, smp))
{
#if defined(MIP_COUNT_SUPPORTED)
    uint mipLevel, width, height, mipCount;
    tex.GetDimensions(mipLevel, width, height, mipCount);
    return mipCount;
#else
    return 0;
#endif
}
```

**平台支持**：
- D3D11/D3D12/XboxOne/PSSL：完全支持
- OpenGL Core/Vulkan：需要 OpenGL 4.3+，计算着色器中不支持
- Metal：不支持（OpenGL 版本不够高）

**使用场景**：
- 动态调整 LOD 范围
- 调试和可视化 MIP 级别
- 高级纹理采样算法

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

