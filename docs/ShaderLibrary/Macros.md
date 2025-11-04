# [[Macros]] - Unity Shader Library 宏定义全览

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl`
- **主要职责**：提供 Unity Shader Library 中所有宏定义的完整说明
- **使用场景**：理解和使用 Unity 着色器库时参考

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型定义
- **被依赖的文件**：几乎所有着色器文件都使用这些宏

---

## 一、基础宏（Macros.hlsl）

### 1.1 名称合并宏

用于宏展开时的名称拼接：

```hlsl
#define MERGE_NAME(X, Y) X##Y
#define CALL_MERGE_NAME(X, Y) MERGE_NAME(X, Y)
```

**功能说明**：
- `MERGE_NAME`: 直接拼接两个标识符
- `CALL_MERGE_NAME`: 间接拼接（解决某些编译器不支持多重 `##` 的问题）

**使用示例**：
```hlsl
CALL_MERGE_NAME(MyTexture, _MainTex)  // 展开为 MyTexture_MainTex
```

### 1.2 立方贴图数组抽象宏

用于处理不支持立方贴图数组的平台：

```hlsl
#ifdef UNITY_NO_CUBEMAP_ARRAY
#define TEXTURECUBE_ARRAY_ABSTRACT TEXTURE2D_ARRAY
#define SAMPLE_TEXTURECUBE_ARRAY_LOD_ABSTRACT(textureName, samplerName, coord3, index, lod) \
    SAMPLE_TEXTURE2D_ARRAY_LOD(textureName, samplerName, DirectionToLatLongCoordinate(coord3), index, lod)
#else
#define TEXTURECUBE_ARRAY_ABSTRACT TEXTURECUBE_ARRAY
#define SAMPLE_TEXTURECUBE_ARRAY_LOD_ABSTRACT(textureName, samplerName, coord3, index, lod) \
    SAMPLE_TEXTURECUBE_ARRAY_LOD(textureName, samplerName, coord3, index, lod)
#endif
```

**功能说明**：
- 在不支持立方贴图数组的平台上，使用 2D 纹理数组 + 方向到经纬度坐标转换来模拟

### 1.3 数学常量宏

```hlsl
#define PI          3.14159265358979323846
#define TWO_PI       6.28318530717958647693
#define FOUR_PI      12.5663706143591729538
#define INV_PI       0.31830988618379067154
#define INV_TWO_PI   0.15915494309189533577
#define INV_FOUR_PI  0.07957747154594766788
#define HALF_PI      1.57079632679489661923
#define INV_HALF_PI  0.63661977236758134308
#define LOG2_E       1.44269504088896340736
#define INV_SQRT2    0.70710678118654752440
#define PI_DIV_FOUR  0.78539816339744830961
```

**功能说明**：
- 预定义的数学常数，避免重复计算
- `INV_*` 表示倒数（1/π, 1/(2π) 等）
- `LOG2_E` 是 log₂(e)，用于对数计算

### 1.4 单位转换宏

```hlsl
#define MILLIMETERS_PER_METER 1000
#define METERS_PER_MILLIMETER rcp(MILLIMETERS_PER_METER)
#define CENTIMETERS_PER_METER 100
#define METERS_PER_CENTIMETER rcp(CENTIMETERS_PER_METER)
```

**功能说明**：
- 用于单位转换（毫米/米，厘米/米）
- `rcp()` 是倒数函数，比除法更快

### 1.5 浮点数极限值宏

```hlsl
#define FLT_INF  asfloat(0x7F800000)          // 正无穷大
#define FLT_EPS  5.960464478e-8               // 2^-24，机器精度
#define FLT_MIN  1.175494351e-38               // 最小归一化正浮点数
#define FLT_MAX  3.402823466e+38               // 最大可表示浮点数
#define HALF_EPS 4.8828125e-4                  // 2^-11，half 精度机器精度
#define HALF_MIN 6.103515625e-5                // 2^-14，half 最小归一化值
#define HALF_MIN_SQRT 0.0078125                // 2^-7，sqrt(HALF_MIN)
#define HALF_MAX 65504.0                       // half 最大可表示值
#define UINT_MAX 0xFFFFFFFFu                   // 最大无符号整数
#define INT_MAX  0x7FFFFFFF                    // 最大有符号整数
```

**功能说明**：
- `FLT_EPS`: 机器精度，1 + EPS = 1（32位浮点数）
- `HALF_EPS`: half 精度（16位）的机器精度
- `HALF_MIN_SQRT`: 用于确保 x² 后仍大于 HALF_MIN

### 1.6 模板函数宏

用于生成多种类型的重载函数：

#### 单参数模板

```hlsl
// 浮点数版本
#define TEMPLATE_1_FLT(FunctionName, Parameter1, FunctionBody) \
    float  FunctionName(float  Parameter1) { FunctionBody; } \
    float2 FunctionName(float2 Parameter1) { FunctionBody; } \
    float3 FunctionName(float3 Parameter1) { FunctionBody; } \
    float4 FunctionName(float4 Parameter1) { FunctionBody; }

// half 版本（包含 float 重载）
#define TEMPLATE_1_HALF(FunctionName, Parameter1, FunctionBody) \
    half  FunctionName(half  Parameter1) { FunctionBody; } \
    half2 FunctionName(half2 Parameter1) { FunctionBody; } \
    /* ... 包含 float 版本 */

// 整数版本（GLES2 不支持 uint）
#ifdef SHADER_API_GLES
    #define TEMPLATE_1_INT(FunctionName, Parameter1, FunctionBody) \
        int FunctionName(int Parameter1) { FunctionBody; } \
        /* ... 仅 int */
#else
    #define TEMPLATE_1_INT(FunctionName, Parameter1, FunctionBody) \
        int FunctionName(int Parameter1) { FunctionBody; } \
        uint FunctionName(uint Parameter1) { FunctionBody; } \
        /* ... int 和 uint */
#endif
```

**功能说明**：
- 自动生成标量和向量版本的重载函数
- 减少代码重复，提高可维护性

#### 双参数和三参数模板

```hlsl
#define TEMPLATE_2_FLT(FunctionName, Parameter1, Parameter2, FunctionBody) /* ... */
#define TEMPLATE_2_HALF(FunctionName, Parameter1, Parameter2, FunctionBody) /* ... */
#define TEMPLATE_3_FLT(FunctionName, Parameter1, Parameter2, Parameter3, FunctionBody) /* ... */
```

**功能说明**：
- 类似单参数模板，但支持多个参数

#### 交换函数模板

```hlsl
#define TEMPLATE_SWAP(FunctionName) \
    void FunctionName(inout real  a, inout real  b) { real  t = a; a = b; b = t; } \
    void FunctionName(inout real2 a, inout real2 b) { real2 t = a; a = b; b = t; } \
    /* ... 支持 real, float, int, uint, bool 及其向量版本 */
```

**功能说明**：
- 生成交换两个变量的函数模板

### 1.7 Legacy Unity 宏

```hlsl
#define TRANSFORM_TEX(tex, name) ((tex.xy) * name##_ST.xy + name##_ST.zw)
#define GET_TEXELSIZE_NAME(name) (name##_TexelSize)
```

**功能说明**：
- `TRANSFORM_TEX`: 应用纹理的缩放和偏移（`_ST` = Scale and Translation）
- `GET_TEXELSIZE_NAME`: 获取纹理的像素大小（用于计算 mip level）

**使用示例**：
```hlsl
float2 uv = TRANSFORM_TEX(input.uv, _MainTex);  // uv * _MainTex_ST.xy + _MainTex_ST.zw
float4 texelSize = GET_TEXELSIZE_NAME(_MainTex);  // _MainTex_TexelSize
```

### 1.8 深度比较宏

```hlsl
#if UNITY_REVERSED_Z
# define COMPARE_DEVICE_DEPTH_CLOSER(shadowMapDepth, zDevice)      (shadowMapDepth >  zDevice)
# define COMPARE_DEVICE_DEPTH_CLOSEREQUAL(shadowMapDepth, zDevice) (shadowMapDepth >= zDevice)
#else
# define COMPARE_DEVICE_DEPTH_CLOSER(shadowMapDepth, zDevice)      (shadowMapDepth <  zDevice)
# define COMPARE_DEVICE_DEPTH_CLOSEREQUAL(shadowMapDepth, zDevice) (shadowMapDepth <= zDevice)
#endif
```

**功能说明**：
- 处理反转 Z 缓冲区的平台差异（D3D11/Metal/Vulkan 使用反转 Z）
- 用于阴影贴图深度比较

---

## 二、平台相关宏（API 目录）

### 2.1 坐标系统宏

```hlsl
// D3D11/Metal/Vulkan/Switch
#define UNITY_UV_STARTS_AT_TOP 1           // UV 从顶部开始（Y 轴向下）
#define UNITY_REVERSED_Z 1                 // 反转 Z 缓冲区（近裁剪面 = 1.0）
#define UNITY_NEAR_CLIP_VALUE (1.0)        // 近裁剪面值
#define UNITY_RAW_FAR_CLIP_VALUE (0.0)     // 远裁剪面原始值

// GLCore/GLES2/GLES3
#define UNITY_NEAR_CLIP_VALUE (-1.0)       // 近裁剪面值（非反转 Z）
#define UNITY_RAW_FAR_CLIP_VALUE (1.0)     // 远裁剪面原始值
```

**功能说明**：
- 不同平台的坐标系统差异
- D3D11/Metal/Vulkan 使用反转 Z（提高深度精度）
- OpenGL 使用标准 Z（[-1, 1]）

### 2.2 语义定义宏

```hlsl
// D3D11/Metal/Vulkan/Switch
#define VERTEXID_SEMANTIC SV_VertexID
#define INSTANCEID_SEMANTIC SV_InstanceID
#define FRONT_FACE_SEMANTIC SV_IsFrontFace
#define FRONT_FACE_TYPE bool
#define IS_FRONT_VFACE(VAL, FRONT, BACK) ((VAL) ? (FRONT) : (BACK))

// GLCore/GLES3
#define VERTEXID_SEMANTIC SV_VertexID
#define INSTANCEID_SEMANTIC SV_InstanceID
#define FRONT_FACE_SEMANTIC VFACE
#define FRONT_FACE_TYPE float
#define IS_FRONT_VFACE(VAL, FRONT, BACK) ((VAL > 0.0) ? (FRONT) : (BACK))

// GLES2
#define VERTEXID_SEMANTIC gl_VertexID
#define INSTANCEID_SEMANTIC gl_InstanceID
#define FRONT_FACE_SEMANTIC VFACE
#define FRONT_FACE_TYPE float
```

**功能说明**：
- 统一不同平台的着色器语义
- `IS_FRONT_VFACE`: 根据正反面选择值（用于双面渲染）

### 2.3 常量缓冲区宏

```hlsl
// D3D11/Metal/Vulkan/GLCore/GLES3/Switch
#define CBUFFER_START(name) cbuffer name {
#define CBUFFER_END };

// GLES2（不支持常量缓冲区）
#define CBUFFER_START(name)
#define CBUFFER_END
```

**功能说明**：
- 定义常量缓冲区（统一变量块）
- GLES2 不支持，所以宏为空

**使用示例**：
```hlsl
CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
    float _Metallic;
CBUFFER_END
```

### 2.4 平台特性宏

```hlsl
// D3D11/Metal/Vulkan
#define PLATFORM_SUPPORTS_EXPLICIT_BINDING              // 支持显式绑定
#define PLATFORM_NEEDS_UNORM_UAV_SPECIFIER              // 需要 unorm UAV 限定符
#define PLATFORM_SUPPORTS_BUFFER_ATOMICS_IN_PIXEL_SHADER // 像素着色器支持原子操作
#define PLATFORM_SUPPORTS_PRIMITIVE_ID_IN_PIXEL_SHADER   // 像素着色器支持原始 ID

// Switch
#define PLATFORM_LANE_COUNT 32                           // Wave 操作 lane 数量

// Metal/Vulkan
#define PLATFORM_SUPPORTS_NATIVE_RENDERPASS              // 支持原生 Render Pass
```

**功能说明**：
- 标识平台支持的特性
- 用于条件编译和优化

### 2.5 流控制属性宏

```hlsl
#define UNITY_BRANCH        [branch]      // 分支指令（动态分支）
#define UNITY_FLATTEN       [flatten]      // 扁平化分支（展开所有分支）
#define UNITY_UNROLL        [unroll]       // 完全展开循环
#define UNITY_UNROLLX(_x)   [unroll(_x)]   // 展开指定次数的循环
#define UNITY_LOOP          [loop]         // 保持循环（不展开）
```

**功能说明**：
- 控制着色器编译器的优化行为
- `UNITY_BRANCH`: 使用动态分支（可能更慢但代码更少）
- `UNITY_FLATTEN`: 展开分支（更快但代码更多）
- `UNITY_UNROLL`: 展开循环（减少循环开销）

**使用示例**：
```hlsl
[UNITY_BRANCH]
if (condition) {
    // 动态分支
}

[UNITY_UNROLL]
for (int i = 0; i < 4; i++) {
    // 循环会被展开
}
```

### 2.6 初始化宏

```hlsl
#define ZERO_INITIALIZE(type, name) name = (type)0;
#define ZERO_INITIALIZE_ARRAY(type, name, arraySize) \
    { for (int arrayIndex = 0; arrayIndex < arraySize; arrayIndex++) { \
        name[arrayIndex] = (type)0; } }
```

**功能说明**：
- 将变量或数组初始化为零
- 某些平台不支持默认初始化，需要显式初始化

**使用示例**：
```hlsl
float4 color;
ZERO_INITIALIZE(float4, color);  // color = float4(0, 0, 0, 0);

float4 colors[8];
ZERO_INITIALIZE_ARRAY(float4, colors, 8);
```

---

## 三、纹理采样宏

### 3.1 纹理声明宏

```hlsl
// 基础纹理类型
#define TEXTURE2D(textureName)                Texture2D textureName
#define TEXTURE2D_ARRAY(textureName)         Texture2DArray textureName
#define TEXTURECUBE(textureName)              TextureCube textureName
#define TEXTURECUBE_ARRAY(textureName)        TextureCubeArray textureName
#define TEXTURE3D(textureName)                Texture3D textureName

// 精度变体
#define TEXTURE2D_FLOAT(textureName)          Texture2D_float textureName
#define TEXTURE2D_HALF(textureName)          Texture2D_half textureName

// 阴影纹理
#define TEXTURE2D_SHADOW(textureName)        TEXTURE2D(textureName)
#define TEXTURECUBE_SHADOW(textureName)       TEXTURECUBE(textureName)

// 可写纹理（RWTexture）
#define RW_TEXTURE2D(type, textureName)       RWTexture2D<type> textureName
#define RW_TEXTURE2D_ARRAY(type, textureName) RWTexture2DArray<type> textureName
#define RW_TEXTURE3D(type, textureName)        RWTexture3D<type> textureName
```

**平台差异**：
- **GLES2**: 
  - `TEXTURE2D` → `sampler2D`
  - `TEXTURE2D_ARRAY` → `samplerCUBE`（不支持数组，用立方贴图模拟）
  - `RW_TEXTURE2D` → 不支持（编译错误）

**功能说明**：
- 统一不同平台的纹理类型声明
- `_FLOAT` / `_HALF` 指定精度（某些平台需要）

### 3.2 采样器声明宏

```hlsl
// D3D11/Metal/Vulkan/GLCore/GLES3
#define SAMPLER(samplerName)                  SamplerState samplerName
#define SAMPLER_CMP(samplerName)              SamplerComparisonState samplerName
#define ASSIGN_SAMPLER(samplerName, samplerValue) samplerName = samplerValue

// GLES2（纹理和采样器绑定）
#define SAMPLER(samplerName)
#define SAMPLER_CMP(samplerName)
#define ASSIGN_SAMPLER(samplerName, samplerValue)
```

**功能说明**：
- GLES2 中纹理和采样器绑定在一起，不需要独立声明
- `SAMPLER_CMP` 用于阴影贴图比较采样

### 3.3 纹理参数宏

用于函数参数传递：

```hlsl
#define TEXTURE2D_PARAM(textureName, samplerName) \
    TEXTURE2D(textureName), SAMPLER(samplerName)
#define TEXTURE2D_ARRAY_PARAM(textureName, samplerName) \
    TEXTURE2D_ARRAY(textureName), SAMPLER(samplerName)
#define TEXTURECUBE_PARAM(textureName, samplerName) \
    TEXTURECUBE(textureName), SAMPLER(samplerName)
#define TEXTURE3D_PARAM(textureName, samplerName) \
    TEXTURE3D(textureName), SAMPLER(samplerName)

// 阴影版本
#define TEXTURE2D_SHADOW_PARAM(textureName, samplerName) \
    TEXTURE2D(textureName), SAMPLER_CMP(samplerName)
```

**功能说明**：
- 将纹理和采样器打包作为函数参数

**使用示例**：
```hlsl
float4 SampleTexture(TEXTURE2D_PARAM(tex, sampler), float2 uv) {
    return SAMPLE_TEXTURE2D(tex, sampler, uv);
}
```

### 3.4 纹理参数展开宏

用于函数调用时展开参数：

```hlsl
#define TEXTURE2D_ARGS(textureName, samplerName) \
    textureName, samplerName
#define TEXTURE2D_ARRAY_ARGS(textureName, samplerName) \
    textureName, samplerName
#define TEXTURECUBE_ARGS(textureName, samplerName) \
    textureName, samplerName
#define TEXTURE3D_ARGS(textureName, samplerName) \
    textureName, samplerName
```

**功能说明**：
- 调用使用 `TEXTURE2D_PARAM` 的函数时展开参数

**使用示例**：
```hlsl
float4 color = SampleTexture(TEXTURE2D_ARGS(_MainTex, sampler_MainTex), uv);
```

### 3.5 纹理采样宏

#### 基础采样宏

```hlsl
// 2D 纹理采样
#define SAMPLE_TEXTURE2D(textureName, samplerName, coord2) \
    textureName.Sample(samplerName, coord2)
#define SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod) \
    textureName.SampleLevel(samplerName, coord2, lod)
#define SAMPLE_TEXTURE2D_BIAS(textureName, samplerName, coord2, bias) \
    textureName.SampleBias(samplerName, coord2, bias)
#define SAMPLE_TEXTURE2D_GRAD(textureName, samplerName, coord2, dpdx, dpdy) \
    textureName.SampleGrad(samplerName, coord2, dpdx, dpdy)

// 立方贴图采样
#define SAMPLE_TEXTURECUBE(textureName, samplerName, coord3) \
    textureName.Sample(samplerName, coord3)
#define SAMPLE_TEXTURECUBE_LOD(textureName, samplerName, coord3, lod) \
    textureName.SampleLevel(samplerName, coord3, lod)
#define SAMPLE_TEXTURECUBE_BIAS(textureName, samplerName, coord3, bias) \
    textureName.SampleBias(samplerName, coord3, bias)

// 3D 纹理采样
#define SAMPLE_TEXTURE3D(textureName, samplerName, coord3) \
    textureName.Sample(samplerName, coord3)
#define SAMPLE_TEXTURE3D_LOD(textureName, samplerName, coord3, lod) \
    textureName.SampleLevel(samplerName, coord3, lod)
```

**功能说明**：
- `SAMPLE_*`: 自动 LOD 采样
- `SAMPLE_*_LOD`: 指定 LOD 级别采样
- `SAMPLE_*_BIAS`: LOD 偏移采样
- `SAMPLE_*_GRAD`: 使用梯度计算 LOD

**平台差异**：
- **GLES2**: 
  - `SAMPLE_TEXTURE2D` → `tex2D(textureName, coord2)`
  - `SAMPLE_TEXTURE2D_LOD` → 不支持（编译错误）

#### 阴影采样宏

```hlsl
#define SAMPLE_TEXTURE2D_SHADOW(textureName, samplerName, coord3) \
    textureName.SampleCmpLevelZero(samplerName, (coord3).xy, (coord3).z)
#define SAMPLE_TEXTURE2D_ARRAY_SHADOW(textureName, samplerName, coord3, index) \
    textureName.SampleCmpLevelZero(samplerName, float3((coord3).xy, index), (coord3).z)
#define SAMPLE_TEXTURECUBE_SHADOW(textureName, samplerName, coord4) \
    textureName.SampleCmpLevelZero(samplerName, (coord4).xyz, (coord4).w)
```

**功能说明**：
- `coord3`: `(uv.x, uv.y, depth)`
- `coord4`: `(direction.x, direction.y, direction.z, depth)`
- 使用硬件深度比较

#### 深度纹理采样宏

```hlsl
#define SAMPLE_DEPTH_TEXTURE(textureName, samplerName, coord2) \
    SAMPLE_TEXTURE2D(textureName, samplerName, coord2).r
#define SAMPLE_DEPTH_TEXTURE_LOD(textureName, samplerName, coord2, lod) \
    SAMPLE_TEXTURE2D_LOD(textureName, samplerName, coord2, lod).r
```

**功能说明**：
- 采样深度纹理并提取 R 通道

### 3.6 纹理加载宏

```hlsl
#define LOAD_TEXTURE2D(textureName, unCoord2) \
    textureName.Load(int3(unCoord2, 0))
#define LOAD_TEXTURE2D_LOD(textureName, unCoord2, lod) \
    textureName.Load(int3(unCoord2, lod))
#define LOAD_TEXTURE2D_MSAA(textureName, unCoord2, sampleIndex) \
    textureName.Load(unCoord2, sampleIndex)
#define LOAD_TEXTURE2D_ARRAY(textureName, unCoord2, index) \
    textureName.Load(int4(unCoord2, index, 0))
#define LOAD_TEXTURE3D(textureName, unCoord3) \
    textureName.Load(int4(unCoord3, 0))
```

**功能说明**：
- `LOAD_*`: 直接加载纹理像素（不使用采样器）
- `unCoord2`: 整数坐标（像素位置）
- `MSAA`: 多采样抗锯齿版本

**使用场景**：
- 计算着色器中的精确像素访问
- 不需要过滤的纹理读取

### 3.7 纹理收集宏（Gather）

```hlsl
#define GATHER_TEXTURE2D(textureName, samplerName, coord2) \
    textureName.Gather(samplerName, coord2)
#define GATHER_TEXTURE2D_ARRAY(textureName, samplerName, coord2, index) \
    textureName.Gather(samplerName, float3(coord2, index))
#define GATHER_TEXTURECUBE(textureName, samplerName, coord3) \
    textureName.Gather(samplerName, coord3)

// 单通道收集
#define GATHER_RED_TEXTURE2D(textureName, samplerName, coord2) \
    textureName.GatherRed(samplerName, coord2)
#define GATHER_GREEN_TEXTURE2D(textureName, samplerName, coord2) \
    textureName.GatherGreen(samplerName, coord2)
#define GATHER_BLUE_TEXTURE2D(textureName, samplerName, coord2) \
    textureName.GatherBlue(samplerName, coord2)
#define GATHER_ALPHA_TEXTURE2D(textureName, samplerName, coord2) \
    textureName.GatherAlpha(samplerName, coord2)
```

**功能说明**：
- `GATHER_*`: 一次采样获取 2x2 像素块（4 个像素）
- 用于高效的多点采样（如 PCF 阴影过滤）
- 返回 `float4`，每个分量对应一个像素

**平台支持**：
- D3D11/Metal/Vulkan: 支持
- GLCore/GLES3: `SHADER_TARGET >= 45` 时支持
- GLES2: 不支持

**使用示例**：
```hlsl
// 获取 2x2 像素块
float4 samples = GATHER_TEXTURE2D(shadowMap, shadowSampler, shadowCoord.xy);
// samples.rgba 对应左上、右上、左下、右下四个像素
```

### 3.8 LOD 计算宏

```hlsl
#define CALCULATE_TEXTURE2D_LOD(textureName, samplerName, coord2) \
    textureName.CalculateLevelOfDetail(samplerName, coord2)
```

**功能说明**：
- 计算指定 UV 坐标的 mip level
- 用于自适应采样

**平台限制**：
- GLES2: 不支持（编译错误）

---

## 四、实例化宏（UnityInstancing.hlsl）

### 4.1 实例化支持宏

```hlsl
// 平台支持检查
#if SHADER_TARGET >= 35 && (defined(SHADER_API_D3D11) || ...)
    #define UNITY_SUPPORT_INSTANCING
#endif

// 立体渲染实例化支持
#if defined(SHADER_API_D3D11) || defined(SHADER_API_GLCORE) || ...
    #define UNITY_SUPPORT_STEREO_INSTANCING
#endif
```

**功能说明**：
- 标识平台是否支持实例化
- 用于条件编译

### 4.2 实例化路径宏

```hlsl
// 传统实例化
#if defined(UNITY_SUPPORT_INSTANCING) && defined(INSTANCING_ON)
    #define UNITY_INSTANCING_ENABLED
#endif

// 程序化实例化
#if defined(UNITY_SUPPORT_INSTANCING) && defined(PROCEDURAL_INSTANCING_ON)
    #define UNITY_PROCEDURAL_INSTANCING_ENABLED
#endif

// DOTS 实例化
#if defined(UNITY_SUPPORT_INSTANCING) && defined(DOTS_INSTANCING_ON)
    #define UNITY_DOTS_INSTANCING_ENABLED
#endif

// 立体渲染实例化
#if defined(UNITY_SUPPORT_STEREO_INSTANCING) && defined(STEREO_INSTANCING_ON)
    #define UNITY_STEREO_INSTANCING_ENABLED
#endif

// 任意实例化启用
#if defined(UNITY_INSTANCING_ENABLED) || ...
    #define UNITY_ANY_INSTANCING_ENABLED 1
#else
    #define UNITY_ANY_INSTANCING_ENABLED 0
#endif
```

**功能说明**：
- 标识当前启用的实例化类型
- 用于条件编译实例化代码

### 4.3 顶点输入输出宏

```hlsl
// 顶点输入实例 ID
#define UNITY_VERTEX_INPUT_INSTANCE_ID \
    uint instanceID : SV_InstanceID;  // 或 DEFAULT_UNITY_VERTEX_INPUT_INSTANCE_ID

// 顶点输出立体渲染
#define UNITY_VERTEX_OUTPUT_STEREO \
    uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;

// 获取实例 ID
#define UNITY_GET_INSTANCE_ID(input) \
    input.instanceID
```

**功能说明**：
- 在顶点着色器输入/输出结构中声明实例化相关字段

**使用示例**：
```hlsl
struct Attributes {
    float3 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings {
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_OUTPUT_STEREO
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
```

### 4.4 实例化设置宏

```hlsl
// 设置实例 ID
#define UNITY_SETUP_INSTANCE_ID(v) \
    UnitySetupInstanceID(UNITY_GET_INSTANCE_ID(v))

// 传递实例 ID
#define UNITY_TRANSFER_INSTANCE_ID(input, output) \
    output.instanceID = input.instanceID

// 设置立体渲染
#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input) \
    unity_StereoEyeIndex = input.stereoTargetEyeIndexAsRTArrayIdx

#define UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output) \
    output.stereoTargetEyeIndexAsRTArrayIdx = unity_StereoEyeIndex
```

**功能说明**：
- `UNITY_SETUP_INSTANCE_ID`: 在着色器开始处调用，设置全局 `unity_InstanceID`
- `UNITY_TRANSFER_INSTANCE_ID`: 在顶点着色器中传递实例 ID
- `UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX`: 在片段着色器中设置立体渲染索引

**使用示例**：
```hlsl
Varyings Vertex(Attributes input) {
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    // ... 顶点着色器代码
    return output;
}

float4 Fragment(Varyings input) : SV_TARGET {
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    // ... 片段着色器代码
}
```

### 4.5 实例化属性宏

```hlsl
// 定义实例化属性缓冲区
#define UNITY_INSTANCING_BUFFER_START(name) \
    UNITY_INSTANCING_CBUFFER_SCOPE_BEGIN(name)

#define UNITY_INSTANCING_BUFFER_END(name) \
    UNITY_INSTANCING_CBUFFER_SCOPE_END

// 定义实例化属性
#define UNITY_DEFINE_INSTANCED_PROP(type, var) \
    type var;

// 访问实例化属性
#define UNITY_ACCESS_INSTANCED_PROP(arr, var) \
    arr##Array[unity_InstanceID].var
```

**功能说明**：
- `UNITY_DEFINE_INSTANCED_PROP`: 定义实例化属性（每个实例独立）
- `UNITY_ACCESS_INSTANCED_PROP`: 访问当前实例的属性

**使用示例**：
```hlsl
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
    UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

float4 Fragment(Varyings input) : SV_TARGET {
    UNITY_SETUP_INSTANCE_ID(input);
    float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);
    float metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
    // ...
}
```

---

## 五、通用宏（Common.hlsl）

### 5.1 精度控制宏

```hlsl
#if HAS_HALF && PREFER_HALF
    #define REAL_IS_HALF 1
#else
    #define REAL_IS_HALF 0
#endif

#if REAL_IS_HALF
    #define REAL_MIN HALF_MIN
    #define REAL_MAX HALF_MAX
    #define REAL_EPS HALF_EPS
    #define TEMPLATE_1_REAL TEMPLATE_1_HALF
#else
    #define REAL_MIN FLT_MIN
    #define REAL_MAX FLT_MAX
    #define REAL_EPS FLT_EPS
    #define TEMPLATE_1_REAL TEMPLATE_1_FLT
#endif
```

**功能说明**：
- `REAL`: 根据平台自动选择 `half` 或 `float`
- 用于跨平台精度控制

### 5.2 立方贴图面宏

```hlsl
#define CUBEMAPFACE_POSITIVE_X 0
#define CUBEMAPFACE_NEGATIVE_X 1
#define CUBEMAPFACE_POSITIVE_Y 2
#define CUBEMAPFACE_NEGATIVE_Y 3
#define CUBEMAPFACE_POSITIVE_Z 4
#define CUBEMAPFACE_NEGATIVE_Z 5
```

**功能说明**：
- 立方贴图六个面的索引

### 5.3 包含保护宏

```hlsl
// 每个文件都有对应的包含保护
#define UNITY_COMMON_INCLUDED
#define UNITY_MACROS_INCLUDED
#define UNITY_TEXTURE_INCLUDED
#define UNITY_INSTANCING_INCLUDED
// ... 等等
```

**功能说明**：
- 防止头文件重复包含

---

## 六、其他功能宏

### 6.1 纹理结构构建宏（Texture.hlsl）

```hlsl
#define UnityBuildTexture2DStruct(n) \
    UnityBuildTexture2DStructInternal(TEXTURE2D_ARGS(n, sampler##n), n##_TexelSize, n##_ST)

#define UnityBuildTexture2DStructNoScale(n) \
    UnityBuildTexture2DStructInternal(TEXTURE2D_ARGS(n, sampler##n), n##_TexelSize, float4(1, 1, 0, 0))

#define UnityBuildTextureCubeStruct(n) \
    UnityBuildTextureCubeStructInternal(TEXTURECUBE_ARGS(n, sampler##n))
```

**功能说明**：
- 构建 `UnityTexture2D` 等结构体
- 自动关联纹理、采样器、texelSize 和 UV 缩放

### 6.2 面光源宏（AreaLighting.hlsl）

```hlsl
#define APPROXIMATE_POLY_LIGHT_AS_SPHERE_LIGHT      // 近似多边形光源为球光源
#define APPROXIMATE_SPHERE_LIGHT_NUMERICALLY         // 数值近似球光源
```

### 6.3 材质宏（CommonMaterial.hlsl）

```hlsl
#define DEFAULT_SPECULAR_VALUE 0.04                   // 默认镜面反射值（F0）
#define CLEAR_COAT_IOR 1.5                           // 清漆折射率
#define CLEAR_COAT_F0 0.04                           // 清漆 F0
#define CLEAR_COAT_ROUGHNESS 0.01                    // 清漆粗糙度
#define NORMALMAP_HIGHEST_VARIANCE 0.03125           // 法线贴图最大方差
```

### 6.4 视差映射宏（PerPixelDisplacement.hlsl）

```hlsl
#define POM_SECANT_METHOD 1                          // 使用割线法的视差遮挡映射
```

### 6.5 Granite 虚拟纹理宏（GraniteShaderLibBase.hlsl）

```hlsl
#define GRA_HLSL_3 0                                 // HLSL 3.0
#define GRA_HLSL_4 0                                 // HLSL 4.0
#define GRA_HLSL_5 0                                 // HLSL 5.0
#define GRA_GLSL_120 0                               // GLSL 1.20
#define GRA_GLSL_130 0                               // GLSL 1.30
#define GRA_GLSL_330 0                               // GLSL 3.30
#define GRA_VERTEX_SHADER 0                          // 顶点着色器
#define GRA_PIXEL_SHADER 0                           // 像素着色器
#define GRA_HQ_CUBEMAPPING 0                         // 高质量立方贴图
#define GRA_DEBUG_TILES 0                           // 调试瓦片
#define GRA_BGRA 0                                   // BGRA 格式
#define GRA_ROW_MAJOR 1                              // 行主序
#define GRA_DEBUG 1                                  // 调试模式
#define GRA_64BIT_RESOLVER 0                         // 64位解析器
#define GRA_RWTEXTURE2D_SCALE 16                     // RWTexture2D 缩放
#define GRA_DISABLE_TEX_LOAD 0                       // 禁用纹理加载
#define GRA_PACK_RESOLVE_OUTPUT 1                    // 打包解析输出
```

---

## 七、宏使用最佳实践

### 7.1 平台兼容性

1. **始终使用宏而非直接 API 调用**
   ```hlsl
   // ✅ 正确
   SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
   
   // ❌ 错误（平台特定）
   _MainTex.Sample(sampler_MainTex, uv);
   ```

2. **检查平台特性宏**
   ```hlsl
   #ifdef PLATFORM_SUPPORT_GATHER
       float4 samples = GATHER_TEXTURE2D(...);
   #else
       // 回退方案
   #endif
   ```

### 7.2 性能优化

1. **使用正确的采样方法**
   ```hlsl
   // 自动 LOD（适合大多数情况）
   SAMPLE_TEXTURE2D(tex, sampler, uv);
   
   // 指定 LOD（适合 mipmap 查找）
   SAMPLE_TEXTURE2D_LOD(tex, sampler, uv, lod);
   
   // 梯度采样（适合需要精确 LOD 控制）
   SAMPLE_TEXTURE2D_GRAD(tex, sampler, uv, ddx, ddy);
   ```

2. **利用 Gather 操作**
   ```hlsl
   // ✅ 高效（一次采样获取 4 个像素）
   float4 samples = GATHER_TEXTURE2D(shadowMap, shadowSampler, uv);
   
   // ❌ 低效（4 次采样）
   float s0 = SAMPLE_TEXTURE2D(shadowMap, shadowSampler, uv + float2(0, 0));
   float s1 = SAMPLE_TEXTURE2D(shadowMap, shadowSampler, uv + float2(1, 0));
   // ...
   ```

### 7.3 实例化使用

```hlsl
// 1. 在顶点输入中声明
struct Attributes {
    float3 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

// 2. 在顶点着色器中设置和传递
Varyings Vertex(Attributes input) {
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    // ...
}

// 3. 在片段着色器中设置
float4 Fragment(Varyings input) : SV_TARGET {
    UNITY_SETUP_INSTANCE_ID(input);
    float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Color);
    // ...
}
```

---

## 八、相关文档

- [[Common]] - 基础类型和工具函数
- [[Texture]] - 纹理采样抽象
- [[UnityInstancing]] - Unity GPU 实例化
- [[D3D11]] - DirectX 11 平台定义
- [[GLCore]] - OpenGL Core 平台定义
- [[GLES2]] - OpenGL ES 2.0 平台定义
- [[Metal]] - Metal 平台定义
- [[Vulkan]] - Vulkan 平台定义

---

## 九、总结

Unity Shader Library 的宏系统提供了：

1. **平台抽象**：统一不同图形 API 的差异
2. **代码复用**：减少平台特定代码
3. **性能优化**：提供高效的采样和操作宏
4. **易用性**：简化的 API 接口

理解这些宏的定义和使用方法，对于编写跨平台的高性能着色器至关重要。

