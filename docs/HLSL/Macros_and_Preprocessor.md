# 宏和预处理器

#HLSL #宏 #预处理器 #条件编译

> 掌握宏系统和预处理器：条件编译、函数式宏、多文件组织

## 📋 目录

- [预处理器指令](#预处理器指令)
- [宏定义](#宏定义)
- [多文件组织](#多文件组织)
- [实践任务](#实践任务)

---

## 预处理器指令

### 条件编译

#### `#define` 和 `#undef`
```hlsl
#define USE_FEATURE_X
#define MAX_LIGHTS 4
#define PI 3.14159265359

// 取消定义
#undef USE_FEATURE_X
```

#### `#if`、`#ifdef`、`#ifndef`、`#else`、`#endif`
```hlsl
#define USE_FEATURE_A
#define VERSION 2

// 使用 #ifdef
#ifdef USE_FEATURE_A
    float featureAValue = 1.0;
#endif

// 使用 #ifndef
#ifndef USE_FEATURE_B
    float featureBValue = 0.0;
#endif

// 使用 #if（带表达式）
#if VERSION >= 2
    float newFeature = 1.0;
#else
    float oldFeature = 1.0;
#endif
```

#### 条件编译示例
```hlsl
#define QUALITY_HIGH
// #define QUALITY_MEDIUM
// #define QUALITY_LOW

#if defined(QUALITY_HIGH)
    #define SAMPLE_COUNT 16
    #define USE_ANISOTROPIC 1
#elif defined(QUALITY_MEDIUM)
    #define SAMPLE_COUNT 8
    #define USE_ANISOTROPIC 0
#else
    #define SAMPLE_COUNT 4
    #define USE_ANISOTROPIC 0
#endif
```

### `#include` 指令

```hlsl
// 包含其他文件
#include "Common.hlsl"
#include "Lighting.hlsl"
#include <UnityShaderVariables.cginc>  // 系统路径
```

#### 防止重复包含
```hlsl
// 在 Common.hlsl 中
#ifndef COMMON_INCLUDED
#define COMMON_INCLUDED

// 文件内容
// ...

#endif // COMMON_INCLUDED
```

### `#pragma` 指令

#### 编译器指令
```hlsl
#pragma target 3.0          // 目标着色器模型
#pragma vertex vert          // 指定顶点着色器函数
#pragma fragment frag        // 指定像素着色器函数
#pragma geometry geom        // 指定几何着色器函数
#pragma hull hull           // 指定 Hull 着色器函数
#pragma domain domain       // 指定 Domain 着色器函数
#pragma compute cs          // 指定计算着色器函数
```

#### 多编译变体
```hlsl
#pragma multi_compile _ FEATURE_A FEATURE_B
#pragma multi_compile_fog
#pragma shader_feature USE_NORMAL_MAP
```

---

## 宏定义

### 简单宏

```hlsl
#define PI 3.14159265359
#define MAX_LIGHTS 4
#define EPSILON 0.0001

float CalculateCircleArea(float radius) {
    return PI * radius * radius;
}
```

### 函数式宏

#### 基本函数宏
```hlsl
#define SQUARE(x) ((x) * (x))
#define MAX(a, b) ((a) > (b) ? (a) : (b))
#define MIN(a, b) ((a) < (b) ? (a) : (b))

float value = SQUARE(5.0);  // 25.0
float maxVal = MAX(3.0, 5.0);  // 5.0
```

#### 多行宏
```hlsl
#define CALCULATE_LIGHTING(N, L) \
    float NdotL = dot(N, L); \
    float lighting = saturate(NdotL);

// 使用
CALCULATE_LIGHTING(normal, lightDir)
```

> ⚠️ **注意**：多行宏需要在每行末尾使用反斜杠 `\`（除了最后一行）。

### 参数化宏

```hlsl
#define LERP(a, b, t) ((a) + ((b) - (a)) * (t))
#define CLAMP(x, minVal, maxVal) (max((minVal), min((x), (maxVal))))

float value = LERP(0.0, 10.0, 0.5);  // 5.0
float clamped = CLAMP(15.0, 0.0, 10.0);  // 10.0
```

### 宏展开规则

#### 括号的重要性
```hlsl
// ❌ 不好的宏定义
#define SQUARE(x) x * x

float result = SQUARE(3.0 + 2.0);  // 展开为：3.0 + 2.0 * 3.0 + 2.0 = 11.0（错误！）

// ✅ 好的宏定义
#define SQUARE(x) ((x) * (x))

float result = SQUARE(3.0 + 2.0);  // 展开为：((3.0 + 2.0) * (3.0 + 2.0)) = 25.0（正确）
```

#### 副作用问题
```hlsl
#define MAX(a, b) ((a) > (b) ? (a) : (b))

float x = 1.0;
float result = MAX(++x, 2.0);  // x 可能被求值两次！
```

### 常用宏模式

#### 类型转换宏
```hlsl
#define FLOAT3(x) float3(x, x, x)
#define FLOAT4(x) float4(x, x, x, x)

float3 gray = FLOAT3(0.5);
float4 white = FLOAT4(1.0);
```

#### 调试宏
```hlsl
#define DEBUG_MODE

#ifdef DEBUG_MODE
    #define DEBUG_VALUE(x) x
#else
    #define DEBUG_VALUE(x) 0.0
#endif

float debug = DEBUG_VALUE(1.0);  // 在 DEBUG_MODE 下为 1.0，否则为 0.0
```

#### 平台特定宏
```hlsl
#if defined(SHADER_API_MOBILE)
    #define USE_HALF_PRECISION 1
#else
    #define USE_HALF_PRECISION 0
#endif

#if USE_HALF_PRECISION
    #define REAL half
#else
    #define REAL float
#endif
```

---

## 多文件组织

### 头文件设计

#### 基础头文件结构
```hlsl
// Math.hlsl
#ifndef MATH_INCLUDED
#define MATH_INCLUDED

#define PI 3.14159265359
#define DEG2RAD (PI / 180.0)
#define RAD2DEG (180.0 / PI)

float Square(float x) {
    return x * x;
}

float3 Normalize(float3 v) {
    return normalize(v);
}

#endif // MATH_INCLUDED
```

#### 光照头文件
```hlsl
// Lighting.hlsl
#ifndef LIGHTING_INCLUDED
#define LIGHTING_INCLUDED

#include "Math.hlsl"

float3 CalculateLambert(float3 normal, float3 lightDir) {
    float NdotL = saturate(dot(normal, lightDir));
    return float3(NdotL, NdotL, NdotL);
}

float3 CalculatePhong(float3 normal, float3 lightDir, float3 viewDir, float shininess) {
    float3 reflectDir = reflect(-lightDir, normal);
    float RdotV = saturate(dot(reflectDir, viewDir));
    return pow(RdotV, shininess);
}

#endif // LIGHTING_INCLUDED
```

### 模块化组织

#### 目录结构示例
```
Shaders/
├── Common/
│   ├── Math.hlsl
│   ├── Color.hlsl
│   └── Utils.hlsl
├── Lighting/
│   ├── Lambert.hlsl
│   ├── Phong.hlsl
│   └── PBR.hlsl
└── Effects/
    ├── Blur.hlsl
    └── EdgeDetection.hlsl
```

#### 主着色器文件
```hlsl
// MyShader.hlsl
#include "Common/Math.hlsl"
#include "Common/Color.hlsl"
#include "Lighting/Lambert.hlsl"

// 着色器代码
```

### 防止重复包含的技巧

#### 方法1：Include Guard
```hlsl
// MyHeader.hlsl
#ifndef MY_HEADER_INCLUDED
#define MY_HEADER_INCLUDED

// 内容

#endif // MY_HEADER_INCLUDED
```

#### 方法2：`#pragma once`（某些编译器支持）
```hlsl
#pragma once

// 内容（自动防止重复包含）
```

### 依赖管理

```hlsl
// 文件 A.hlsl 依赖 B.hlsl
// A.hlsl
#include "B.hlsl"

// 文件 C.hlsl 依赖 A.hlsl 和 B.hlsl
// C.hlsl
#include "B.hlsl"  // 先包含 B
#include "A.hlsl"  // A 已经包含了 B，但由于 include guard，不会重复
```

---

## 实践任务

### 任务1：设计一个可复用的数学库头文件

```hlsl
// MathLib.hlsl
#ifndef MATH_LIB_INCLUDED
#define MATH_LIB_INCLUDED

// 常量
#define PI 3.14159265359
#define TAU (2.0 * PI)
#define E 2.71828182846
#define EPSILON 0.0001

// 工具宏
#define DEG2RAD (PI / 180.0)
#define RAD2DEG (180.0 / PI)
#define SQUARE(x) ((x) * (x))
#define CUBE(x) ((x) * (x) * (x))

// 函数
float Remap(float value, float inMin, float inMax, float outMin, float outMax) {
    return lerp(outMin, outMax, (value - inMin) / (inMax - inMin));
}

float SmoothMin(float a, float b, float k) {
    float h = saturate(0.5 + 0.5 * (b - a) / k);
    return lerp(b, a, h) - k * h * (1.0 - h);
}

#endif // MATH_LIB_INCLUDED
```

### 任务2：实现条件编译的跨平台代码

```hlsl
// Platform.hlsl
#ifndef PLATFORM_INCLUDED
#define PLATFORM_INCLUDED

// 检测平台
#if defined(SHADER_API_MOBILE) || defined(SHADER_API_GLES)
    #define IS_MOBILE 1
    #define USE_HALF_PRECISION 1
#else
    #define IS_MOBILE 0
    #define USE_HALF_PRECISION 0
#endif

// 根据平台选择精度
#if USE_HALF_PRECISION
    #define REAL half
    #define REAL2 half2
    #define REAL3 half3
    #define REAL4 half4
#else
    #define REAL float
    #define REAL2 float2
    #define REAL3 float3
    #define REAL4 float4
#endif

// 根据平台选择特性
#if IS_MOBILE
    #define MAX_LIGHTS 1
    #define USE_SHADOWS 0
    #define USE_REFLECTIONS 0
#else
    #define MAX_LIGHTS 4
    #define USE_SHADOWS 1
    #define USE_REFLECTIONS 1
#endif

#endif // PLATFORM_INCLUDED
```

---

## 🔗 相关链接

- [[HLSL_Syntax_and_Semantics]]
- [[Performance_Optimization]] - 条件编译优化
- [[Common_Patterns_and_Algorithms]]

---

*最后更新：2024年*

