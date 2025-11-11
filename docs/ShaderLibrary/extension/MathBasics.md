---
tags: [shader, 数学基础, GPU, 数学运算]
aliases: [MathBasics, 数学基础, GPU数学]
created: 2025-01-20
related:
  - "[[Float]]"
  - "[[VectorOperations]]"
  - "[[Common]]"
  - "[[PrecisionOptimization]]"
---

# 数学基础 (Math Basics)

## 概述

GPU 着色器编程需要掌握基础的数学运算知识。本文档介绍着色器中常用的数学函数、运算技巧和最佳实践，为理解空间变换、光照计算等高级主题打下基础。

**相关主题**：
- [[Float]] - 浮点数格式和精度
- [[VectorOperations]] - 向量和矩阵计算
- [[Common]] - Unity 核心函数库
- [[PrecisionOptimization]] - 精度优化技巧

## 1. 常用数学函数 (Common Math Functions)

### 1.1 绝对值函数

```hlsl
float abs(float x);      // 绝对值
float2 abs(float2 x);    // 向量各分量的绝对值
```

**应用场景**：
- 距离计算
- 误差比较
- 符号判断

### 1.2 最值函数

```hlsl
float min(float a, float b);           // 最小值
float max(float a, float b);           // 最大值
float clamp(float x, float min, float max);  // 限制在范围内
float saturate(float x);                // 限制在 [0, 1]（等价于 clamp(x, 0, 1)）
```

**应用场景**：
- `clamp()`: 限制颜色值、UV 坐标
- `saturate()`: 快速将值限制在 [0, 1]，常用于颜色和系数

### 1.3 取整函数

```hlsl
float floor(float x);    // 向下取整
float ceil(float x);     // 向上取整
float round(float x);    // 四舍五入
float trunc(float x);    // 截断（向零取整）
float frac(float x);     // 小数部分（等价于 x - floor(x)）
```

**应用场景**：
- `floor()`: 网格化、像素对齐
- `frac()`: 重复纹理、周期性函数
- `round()`: 像素坐标计算

### 1.4 插值函数

```hlsl
float lerp(float a, float b, float t);           // 线性插值
float smoothstep(float edge0, float edge1, float x);  // 平滑插值
float step(float edge, float x);                 // 阶跃函数
```

**数学定义**：
- `lerp(a, b, t) = a + t * (b - a)`
- `smoothstep`: 在 `[edge0, edge1]` 之间平滑过渡
- `step`: `x < edge` 返回 0，否则返回 1

**应用场景**：
- `lerp()`: 颜色混合、位置插值
- `smoothstep()`: 平滑过渡、边缘柔化
- `step()`: 条件判断、阈值处理

### 1.5 重映射函数

```hlsl
// 将值从 [inMin, inMax] 映射到 [outMin, outMax]
float remap(float value, float inMin, float inMax, float outMin, float outMax) {
    float t = (value - inMin) / (inMax - inMin);
    return lerp(outMin, outMax, t);
}
```

**应用场景**：
- UV 坐标转换
- 参数范围调整
- 归一化到不同范围

### 1.6 符号函数

```hlsl
float sign(float x);  // 符号函数：x > 0 返回 1，x < 0 返回 -1，x == 0 返回 0
```

## 2. 指数和对数函数 (Exponential & Logarithmic)

### 2.1 幂函数

```hlsl
float pow(float x, float y);   // x 的 y 次幂
float pow2(float x);            // x²（快速版本）
float pow4(float x);            // x⁴（快速版本）
```

**性能优化**：
- 使用 `pow2(x)` 代替 `pow(x, 2)`（更快）
- 使用 `x * x * x` 代替 `pow(x, 3)`（避免函数调用开销）

### 2.2 指数函数

```hlsl
float exp(float x);    // e^x
float exp2(float x);    // 2^x
```

**应用场景**：
- 衰减计算：`attenuation = exp(-distance * falloff)`
- 非线性映射

### 2.3 对数函数

```hlsl
float log(float x);     // 自然对数 ln(x)
float log2(float x);    // 以 2 为底的对数
float log10(float x);   // 以 10 为底的对数
```

**应用场景**：
- 亮度计算（对数空间）
- 数据压缩

### 2.4 平方根函数

```hlsl
float sqrt(float x);    // 平方根
float rsqrt(float x);   // 倒数平方根 1/sqrt(x)（硬件优化）
```

**性能优化**：
- `rsqrt()` 通常比 `1.0 / sqrt(x)` 更快（硬件优化）
- 归一化时使用：`normalize = v * rsqrt(dot(v, v))`

## 3. 三角函数 (Trigonometric Functions)

### 3.1 基本三角函数

```hlsl
float sin(float x);     // 正弦
float cos(float x);     // 余弦
float tan(float x);     // 正切
```

**注意事项**：
- 输入为**弧度**，不是角度
- 角度转弧度：`radians = degrees * PI / 180.0`
- 弧度转角度：`degrees = radians * 180.0 / PI`

### 3.2 反三角函数

```hlsl
float asin(float x);    // 反正弦（返回值范围 [-π/2, π/2]）
float acos(float x);    // 反余弦（返回值范围 [0, π]）
float atan(float x);    // 反正切（返回值范围 [-π/2, π/2]）
float atan2(float y, float x);  // 两个参数的反正切（返回值范围 [-π, π]）
```

**`atan2` 的优势**：
- 可以处理所有象限
- 返回完整的角度范围 `[-π, π]`
- 避免除零问题

### 3.3 快速近似函数

Unity ShaderLibrary 提供了快速近似版本：

```hlsl
float FastACos(float x);   // 快速反余弦（精度较低）
float FastATan(float x);    // 快速反正切（精度较低）
```

**使用建议**：
- 对精度要求不高时使用快速版本
- 移动平台性能优化

### 3.4 角度归一化

```hlsl
// 将角度归一化到 [0, 2π)
float normalizeAngle(float angle) {
    return fmod(angle + 6.28318530718, 6.28318530718);  // 2π
}

// 将角度归一化到 [-π, π]
float normalizeAngleSigned(float angle) {
    angle = fmod(angle + 3.14159265359, 6.28318530718);
    return angle - 3.14159265359;
}
```

**应用场景**：
- 旋转动画
- 角度累积（避免精度损失）

## 4. 数值技巧 (Numerical Techniques)

### 4.1 浮点数比较

关于浮点数比较的详细方法和最佳实践，请参考：

→ [[PrecisionOptimization]] - 精度比较部分

该文档包含：
- 绝对容差和相对容差的比较方法
- 选择建议和使用场景

### 4.2 安全除法

关于安全除法的实现和使用，请参考：

→ [[PrecisionOptimization]] - 避免接近零的除法部分

### 4.3 安全归一化

关于安全归一化的实现和使用，请参考：

→ [[Common]] - `SafeNormalize()` 函数详解
→ [[PrecisionOptimization]] - 法线向量归一化优化部分

## 5. 常用数学常量

```hlsl
#define PI 3.14159265359
#define TWO_PI 6.28318530718
#define HALF_PI 1.57079632679
#define INV_PI 0.31830988618
#define INV_TWO_PI 0.15915494309

// Unity ShaderLibrary 中定义的常量
#define REAL_EPS 1.192092896e-07  // float 的机器精度（2^(-23)）
#define EPSILON 1e-6              // 常用的小阈值
```

## 6. GPU 特定优化 (GPU-Specific Optimizations)

### 6.1 硬件优化函数

GPU 硬件通常提供优化过的数学函数：

```hlsl
rsqrt(x)      // 倒数平方根（硬件优化）
rcp(x)        // 倒数（硬件优化）
mad(a, b, c)  // 乘加指令 a * b + c（某些 GPU 上更快）
```

### 6.2 避免昂贵函数

```hlsl
// ❌ 避免在片元着色器中频繁使用
float angle = atan2(y, x);
float result = pow(x, 3.0);

// ✅ 使用更快的替代方案
float angle = atan2_approx(y, x);  // 自定义近似
float result = x * x * x;           // 直接计算
```

### 6.3 SIMD 友好代码

GPU 使用 SIMD（单指令多数据）执行，向量化运算更高效：

```hlsl
// ✅ 向量化运算（SIMD 友好）
float3 result = a * b + c;

// ❌ 标量运算（效率较低）
float result_x = a.x * b.x + c.x;
float result_y = a.y * b.y + c.y;
float result_z = a.z * b.z + c.z;
```

## 7. 实用代码示例 (Practical Examples)

### 7.1 重映射函数

```hlsl
float remap(float value, float inMin, float inMax, float outMin, float outMax) {
    float t = saturate((value - inMin) / (inMax - inMin));
    return lerp(outMin, outMax, t);
}
```

### 7.2 角度归一化

```hlsl
float normalizeAngle(float angle) {
    return fmod(angle + TWO_PI, TWO_PI);
}
```

### 7.3 平滑插值变体

```hlsl
// 自定义平滑曲线
float smoothCurve(float t) {
    return t * t * (3.0 - 2.0 * t);  // 等价于 smoothstep(0, 1, t)
}

// 更平滑的曲线
float smootherCurve(float t) {
    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}
```

## 8. 参考资料 (References)

- [[Float]] - 浮点数格式和精度详解
- [[VectorOperations]] - 向量和矩阵运算详解
- [[Common]] - Unity ShaderLibrary 核心函数
- [[PrecisionOptimization]] - 精度优化技巧
- [Unity Shader Data Types](https://docs.unity3d.com/Manual/SL-DataTypesAndPrecision.html) - Unity 官方文档

