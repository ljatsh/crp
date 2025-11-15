---
tags: [shader, 浮点数, GPU, 精度, IEEE754, 数学基础]
aliases: [Float, 浮点数精度, 浮点数格式]
created: 2025-11-05
related:
  - "[[Common]]"
  - "[[PrecisionOptimization]]"
  - "[[MathBasics]]"
  - "[[VectorOperations]]"
---

# 浮点数（Float）

## 概述

浮点数是计算机中表示实数的一种数据类型，在 GPU 编程中广泛使用。理解浮点数的存储格式、精度限制和计算特性，对于编写高效、准确的着色器代码至关重要。

**相关主题**：
- [[Common]] - 核心函数库
- [[PrecisionOptimization]] - 着色器优化技巧
- [[MathBasics]] - GPU 数学运算
- [[VectorOperations]] - 向量和矩阵计算

## IEEE 754 浮点数标准

### 格式说明

GPU 中常用的浮点数格式遵循 **IEEE 754** 标准：

#### float（32位单精度浮点数）

```
符号位(S) | 指数位(E) | 尾数位(M)
1位      | 8位       | 23位
```

**数学表示（正规数，$1 \leq E \leq 254$）**：

$$
\text{值} = (-1)^S \times 1.M \times 2^{(E-127)}
$$

- **符号位（Sign）**：1 位，$S = 0$ 表示正数，$S = 1$ 表示负数
- **指数位（Exponent）**：8 位，偏移量为 127（实际指数 $= E - 127$）
- **尾数位（Mantissa/Fraction）**：23 位，表示小数部分（隐含前导 $1$）

> [!warning] 重要
> 上述公式**仅适用于正规数**（$1 \leq E \leq 254$）。特殊值（零、无穷大、NaN、非正规数）使用不同的规则，见下方说明。

**特殊值（不使用上述公式）**：

1. **零**：$E = 0, M = 0$
   - **正零**：$S=0$，值 = $+0.0$（不考虑指数偏移）
   - **负零**：$S=1$，值 = $-0.0$（不考虑指数偏移）

2. **非正规数（Denormalized）**：$E = 0, M \neq 0$
   - 公式：$(-1)^S \times 0.M \times 2^{-126}$（没有隐含前导1，固定指数 $-126$）

3. **无穷大**：$E = 255, M = 0$
   - **正无穷**：$S=0$，值 = $+\infty$
   - **负无穷**：$S=1$，值 = $-\infty$

4. **NaN（非数字）**：$E = 255, M \neq 0$
   - 值 = `NaN`（不是一个数字）

**数值范围**：
- **最小正规数**：$\approx 1.175494 \times 10^{-38}$
- **最大值**：$\approx 3.402823 \times 10^{38}$
- **精度**：约 6-7 位十进制有效数字

### 32位 float 的内存布局详解

#### 内存布局格式

32位 float 在内存中的布局（大端序/小端序不影响位模式）：

```
位位置:  31  30-23    22-0
        │   │        │
        S   E(8位)   M(23位)
```

#### 最大值（Maximum Value）

**位模式**：
```
符号位: 0
指数位: 11111110 (254)
尾数位: 11111111111111111111111 (23位全1)
```

**完整32位**：
```
0 11111110 11111111111111111111111
S EEEEEEEE MMMMMMMMMMMMMMMMMMMMMMM
```

**十六进制表示**：`0x7F7FFFFF`

**二进制表示**：`01111111 01111111 11111111 11111111`

**计算过程**：
- 指数：$E = 254$，实际指数 $= 254 - 127 = 127$
- 尾数：$M = 2^{23} - 1 = 8388607$，$1.M = 1 + \frac{8388607}{2^{23}} = \frac{16777215}{8388608}$
- 值：$\frac{16777215}{8388608} \times 2^{127} = (2 - 2^{-23}) \times 2^{127} \approx 3.402823466 \times 10^{38}$

**在代码中验证**：
```hlsl
float maxFloat = asfloat(0x7F7FFFFF);  // 最大值
// 或
float maxFloat = 3.402823466e+38f;
```

#### 最小正规数（Minimum Normalized Value）

**位模式**：
```
符号位: 0
指数位: 00000001 (1)
尾数位: 00000000000000000000000 (23位全0)
```

**完整32位**：
```
0 00000001 00000000000000000000000
S EEEEEEEE MMMMMMMMMMMMMMMMMMMMMMM
```

**十六进制表示**：`0x00800000`

**二进制表示**：`00000000 10000000 00000000 00000000`

**计算过程**：
- 指数：$E = 1$，实际指数 $= 1 - 127 = -126$
- 尾数：$M = 0$，$1.M = 1.0$
- 值：$1.0 \times 2^{-126} \approx 1.175494351 \times 10^{-38}$

**在代码中验证**：
```hlsl
float minNormalFloat = asfloat(0x00800000);  // 最小正规数
// 或
float minNormalFloat = 1.175494351e-38f;
```

#### 最小非正规数（Minimum Denormalized Value）

**位模式**：
```
符号位: 0
指数位: 00000000 (0，表示非正规数)
尾数位: 00000000000000000000001 (最后1位=1)
```

**完整32位**：
```
0 00000000 00000000000000000000001
S EEEEEEEE MMMMMMMMMMMMMMMMMMMMMMM
```

**十六进制表示**：`0x00000001`

**二进制表示**：`00000000 00000000 00000000 00000001`

**计算过程**：
- 指数：$E = 0$，非正规数使用固定指数 $-126$
- 尾数：$M = 1$，非正规数没有隐含前导1，$0.M = \frac{1}{2^{23}}$
- 值：$\frac{1}{2^{23}} \times 2^{-126} = 2^{-149} \approx 1.401298464 \times 10^{-45}$

**在代码中验证**：
```hlsl
float minDenormalFloat = asfloat(0x00000001);  // 最小非正规数
```

#### 正零（Positive Zero）

**位模式**：
```
符号位: 0
指数位: 00000000 (0)
尾数位: 00000000000000000000000 (全0)
```

**完整32位**：
```
0 00000000 00000000000000000000000
S EEEEEEEE MMMMMMMMMMMMMMMMMMMMMMM
```

**十六进制表示**：`0x00000000`

**二进制表示**：`00000000 00000000 00000000 00000000`

**值**：$+0.0$

> [!important] 重要说明
> **零是特殊值，不使用正常的计算公式**。
> 
> 当 $E = 0$ 且 $M = 0$ 时，直接表示零，**不考虑指数偏移（bias）**。
> 
> 正常公式：$\text{值} = (-1)^S \times 1.M \times 2^{(E-127)}$ **不适用于零**。
> 
> 零的定义：当 $E = 0$ 且 $M = 0$ 时，值恒为 $(-1)^S \times 0.0$。

**在代码中验证**：
```hlsl
float positiveZero = asfloat(0x00000000);
bool isZero = (positiveZero == 0.0);  // true
```

#### 负零（Negative Zero）

**位模式**：
```
符号位: 1
指数位: 00000000 (0)
尾数位: 00000000000000000000000 (全0)
```

**完整32位**：
```
1 00000000 00000000000000000000000
S EEEEEEEE MMMMMMMMMMMMMMMMMMMMMMM
```

**十六进制表示**：`0x80000000`

**二进制表示**：`10000000 00000000 00000000 00000000`

**值**：$-0.0$（在大多数运算中与 $+0.0$ 等价）

> [!important] 重要说明
> **负零也是特殊值，不使用正常的计算公式**。
> 
> 负零的位模式与正零的唯一区别是符号位为1，但值仍然是零。
> 
> 在大多数运算中，$+0.0$ 和 $-0.0$ 被视为相等，但可以通过 `signbit()` 函数区分。

**在代码中验证**：
```hlsl
float negativeZero = asfloat(0x80000000);
bool isZero = (negativeZero == 0.0);  // true（负零等于正零）
bool isNegative = signbit(negativeZero);  // true（但符号位为1）

// 验证：负零不等于正零的位模式
uint posZeroBits = asuint(0.0f);      // 0x00000000
uint negZeroBits = asuint(-0.0f);    // 0x80000000
bool bitsEqual = (posZeroBits == negZeroBits);  // false（位模式不同）
bool valuesEqual = (0.0f == -0.0f);  // true（值相等）
```

#### 正无穷大（Positive Infinity）

**位模式**：
```
符号位: 0
指数位: 11111111 (255)
尾数位: 00000000000000000000000 (全0)
```

**完整32位**：
```
0 11111111 00000000000000000000000
S EEEEEEEE MMMMMMMMMMMMMMMMMMMMMMM
```

**十六进制表示**：`0x7F800000`

**二进制表示**：`01111111 10000000 00000000 00000000`

**值**：$+\infty$

**在代码中验证**：
```hlsl
float positiveInf = asfloat(0x7F800000);
bool isInf = isinf(positiveInf);  // true
bool isPositive = (positiveInf > 0);  // true
```

#### 负无穷大（Negative Infinity）

**位模式**：
```
符号位: 1
指数位: 11111111 (255)
尾数位: 00000000000000000000000 (全0)
```

**完整32位**：
```
1 11111111 00000000000000000000000
S EEEEEEEE MMMMMMMMMMMMMMMMMMMMMMM
```

**十六进制表示**：`0xFF800000`

**二进制表示**：`11111111 10000000 00000000 00000000`

**值**：$-\infty$

**在代码中验证**：
```hlsl
float negativeInf = asfloat(0xFF800000);
bool isInf = isinf(negativeInf);  // true
bool isNegative = (negativeInf < 0);  // true
```

#### NaN（Not a Number）

**位模式**：
```
符号位: 0 或 1（任意）
指数位: 11111111 (255)
尾数位: 非全0（至少1位为1）
```

**完整32位示例（Quiet NaN）**：
```
0 11111111 10000000000000000000000
S EEEEEEEE MMMMMMMMMMMMMMMMMMMMMMM
```

**十六进制表示**：`0x7FC00000`（常见的NaN值）

**二进制表示**：`01111111 11000000 00000000 00000000`

**值**：`NaN`（不是一个数字）

**NaN 的类型**：
- **Quiet NaN（静默NaN）**：尾数最高位为1，如 `0x7FC00000`
- **Signaling NaN（信号NaN）**：尾数最高位为0，如 `0x7F800001`

**在代码中验证**：
```hlsl
float quietNaN = asfloat(0x7FC00000);
float signalingNaN = asfloat(0x7F800001);
bool isNaN1 = isnan(quietNaN);  // true
bool isNaN2 = isnan(signalingNaN);  // true
```

#### 内存布局总结表

| 值 | 符号位 | 指数位 | 尾数位 | 十六进制 | 十进制值 |
|---|--------|--------|--------|---------|---------|
| **最大值** | 0 | 11111110 (254) | 全1 | `0x7F7FFFFF` | $\approx 3.402823 \times 10^{38}$ |
| **最小正规数** | 0 | 00000001 (1) | 全0 | `0x00800000` | $\approx 1.175494 \times 10^{-38}$ |
| **最小非正规数** | 0 | 00000000 (0) | 最后1位=1 | `0x00000001` | $\approx 1.401298 \times 10^{-45}$ |
| **正零** | 0 | 00000000 (0) | 全0 | `0x00000000` | $+0.0$ |
| **负零** | 1 | 00000000 (0) | 全0 | `0x80000000` | $-0.0$ |
| **正无穷** | 0 | 11111111 (255) | 全0 | `0x7F800000` | $+\infty$ |
| **负无穷** | 1 | 11111111 (255) | 全0 | `0xFF800000` | $-\infty$ |
| **Quiet NaN** | 0 | 11111111 (255) | 100...0 | `0x7FC00000` | `NaN` |
| **Signaling NaN** | 0 | 11111111 (255) | 000...1 | `0x7F800001` | `NaN` |

#### 在着色器中使用

```hlsl
// 使用 asfloat() 创建特殊值
float maxValue = asfloat(0x7F7FFFFF);      // 最大值
float minNormal = asfloat(0x00800000);     // 最小正规数
float minDenormal = asfloat(0x00000001);   // 最小非正规数
float posZero = asfloat(0x00000000);       // 正零
float negZero = asfloat(0x80000000);       // 负零
float posInf = asfloat(0x7F800000);        // 正无穷
float negInf = asfloat(0xFF800000);        // 负无穷
float quietNaN = asfloat(0x7FC00000);      // Quiet NaN

// 反向操作：获取浮点数的位模式
uint bits = asuint(3.14159265f);           // 获取π的位模式
uint maxBits = asuint(3.402823466e+38f);   // 应该等于 0x7F7FFFFF
```

#### 验证代码示例

```hlsl
// 验证最大值
float maxFloat = asfloat(0x7F7FFFFF);
// maxFloat ≈ 3.402823466e+38

// 验证最小正规数
float minNormal = asfloat(0x00800000);
// minNormal ≈ 1.175494351e-38

// 验证无穷大
float inf = asfloat(0x7F800000);
bool isInf = isinf(inf);  // true

// 验证NaN
float nan = asfloat(0x7FC00000);
bool isNaN = isnan(nan);  // true
```

#### half（16位半精度浮点数）

```
符号位(S) | 指数位(E) | 尾数位(M)
1位      | 5位       | 10位
```

**数学表示**：

$$
\text{值} = (-1)^S \times 1.M \times 2^{(E-15)}
$$

- **指数位**：5 位，偏移量为 15
- **尾数位**：10 位

**数值范围**：
- **最小正规数**：$\approx 6.104 \times 10^{-5}$
- **最大值**：$\approx 65504$
- **精度**：约 3-4 位十进制有效数字

#### double（64位双精度浮点数）

```
符号位(S) | 指数位(E) | 尾数位(M)
1位      | 11位      | 52位
```

**数学表示**：

$$
\text{值} = (-1)^S \times 1.M \times 2^{(E-1023)}
$$

> [!warning] 注意
> GPU 通常不支持或性能较差，着色器中很少使用

> [!info] 参考资料
> - [IEEE 754 - Wikipedia](https://en.wikipedia.org/wiki/IEEE_754) - IEEE 754 浮点数标准官方文档
> - [Single-precision floating-point format](https://en.wikipedia.org/wiki/Single-precision_floating-point_format) - 32位单精度格式详解
> - [Half-precision floating-point format](https://en.wikipedia.org/wiki/Half-precision_floating-point_format) - 16位半精度格式详解
> - [Machine epsilon - Wikipedia](https://en.wikipedia.org/wiki/Machine_epsilon) - 机器ε的定义和计算方法
> - [What Every Computer Scientist Should Know About Floating-Point Arithmetic](https://docs.oracle.com/cd/E19957-01/806-3568/ncg_goldberg.html) - David Goldberg 的经典论文
> - [IEEE 754 Standard (PDF)](https://ieeexplore.ieee.org/document/8766229) - IEEE 754-2019 官方标准文档

---

## 进制转换

### 二进制 ↔ 十进制

**二进制转十进制（整数部分）**：

$$
1011_{(2)} = 1 \times 2^3 + 0 \times 2^2 + 1 \times 2^1 + 1 \times 2^0 = 8 + 0 + 2 + 1 = 11_{(10)}
$$

**二进制转十进制（小数部分）**：

$$
0.101_{(2)} = 1 \times 2^{-1} + 0 \times 2^{-2} + 1 \times 2^{-3} = 0.5 + 0 + 0.125 = 0.625_{(10)}
$$

**十进制转二进制（整数部分）**：
```
13₍₁₀₎ ÷ 2 = 6 余 1  ↓
 6₍₁₀₎ ÷ 2 = 3 余 0  ↓
 3₍₁₀₎ ÷ 2 = 1 余 1  ↓
 1₍₁₀₎ ÷ 2 = 0 余 1  ↓
结果：1101₍₂₎（从下往上读）
```

**十进制转二进制（小数部分）**：
```
0.625₍₁₀₎ × 2 = 1.25  → 取整数部分 1 ↓
0.25₍₁₀₎  × 2 = 0.5   → 取整数部分 0 ↓
0.5₍₁₀₎   × 2 = 1.0   → 取整数部分 1 ↓
结果：0.101₍₂₎（从上往下读）
```

### 十六进制 ↔ 二进制

十六进制与二进制的转换非常直观，因为 1 个十六进制数字对应 4 个二进制位：

```
0x1A2F = 0001 1010 0010 1111₍₂₎
         ↑    ↑    ↑    ↑
         1    A    2    F
```

**对应关系表**：
```
十六进制:  0    1    2    3    4    5    6    7    8    9    A    B    C    D    E    F
二进制:  0000 0001 0010 0011 0100 0101 0110 0111 1000 1001 1010 1011 1100 1101 1110 1111
```

### 浮点数的十六进制表示（在着色器中）

在 HLSL/GLSL 中，可以使用十六进制表示浮点数的精确位模式：

```hlsl
// 使用 asfloat() 将整数位模式解释为浮点数
float pi = asfloat(0x40490FDB);  // 精确表示 π ≈ 3.14159265

// 反向操作：将浮点数解释为整数位模式
uint bits = asuint(3.14159265);  // 结果 ≈ 0x40490FDB
```

---

## 精度说明

### 机器ε（Machine Epsilon）

**机器ε的定义**：

机器ε（Machine Epsilon，记为 $\varepsilon$）是浮点数表示精度的关键指标，定义为：

$$\varepsilon = \text{大于 1 的最小可表示浮点数} - 1$$

换句话说，机器ε是 **1 与大于 1 的最小可表示浮点数之间的差值**。

**数学意义**：
- 机器ε表示在数值 1 附近，浮点数能够表示的最小增量
- 对于任意浮点数 $x$，如果 $|y| < \varepsilon \cdot |x|$，则 $x + y$ 可能无法与 $x$ 区分
- 机器ε是相对误差的上界

**计算方法**：

对于 IEEE 754 浮点数格式，机器ε的计算公式为：

$$\varepsilon = 2^{-(\text{尾数位数})}$$

**各类型的机器ε值**：

| 类型 | 尾数位数 | 机器ε | 十进制值 | 用途 |
|------|---------|-------|---------|------|
| **float (32位)** | 23位 | $2^{-23}$ | $\approx 1.192 \times 10^{-7}$ | 桌面GPU标准精度 |
| **half (16位)** | 10位 | $2^{-10}$ | $\approx 9.766 \times 10^{-4}$ | 移动GPU常用精度 |
| **double (64位)** | 52位 | $2^{-52}$ | $\approx 2.220 \times 10^{-16}$ | CPU高精度计算 |

#### 验证示例

```hlsl
// float 的机器ε验证
float eps = 1.192092896e-07f;  // 2^(-23)
float next = 1.0f + eps;        // 大于1的最小可表示数
// next ≈ 1.0000001192...

// half 的机器ε验证
half halfEps = 9.765625e-04h;   // 2^(-10)
half halfNext = 1.0h + halfEps; // 大于1的最小可表示half数
// halfNext ≈ 1.0009765625...
```

> [!info] 重要说明
> - 机器ε是**相对精度**的度量，表示相对于数值大小的精度
> - 数值越大，绝对精度越低（但相对精度保持不变）
> - 在浮点数比较时，应使用相对误差：$|a - b| < \varepsilon \cdot \max(|a|, |b|)$

### 有效数字与精度

**float（32位）精度特性**：

1. **机器ε（相对精度）**：$\varepsilon = 2^{-23} \approx 1.19 \times 10^{-7}$
   - 这意味着在数值 1 附近，最小可区分的差值为约 $1.19 \times 10^{-7}$
   - 相对误差约为 $0.0000119\%$（约 7 位有效数字）

2. **绝对精度**：取决于数值的大小
   - 在 $[1, 2)$ 范围内：精度为 $2^{-23} \approx 1.19 \times 10^{-7}$
   - 在 $[2, 4)$ 范围内：精度为 $2^{-22} \approx 2.38 \times 10^{-7}$
   - 在 $[1024, 2048)$ 范围内：精度为 $2^{-13} \approx 0.000122$
   - **规律**：在区间 $[2^k, 2^{k+1})$ 内，绝对精度为 $2^{k-23}$

**half（16位）精度特性**：

1. **机器ε（相对精度）**：$\varepsilon = 2^{-10} \approx 9.77 \times 10^{-4}$
   - 在数值 1 附近，最小可区分的差值为约 $9.77 \times 10^{-4}$
   - 相对误差约为 $0.0977\%$（约 3-4 位有效数字）

2. **绝对精度**：同样取决于数值大小
   - 在 $[1, 2)$ 范围内：精度为 $2^{-10} \approx 9.77 \times 10^{-4}$
   - 在 $[2, 4)$ 范围内：精度为 $2^{-9} \approx 1.95 \times 10^{-3}$

#### 示例：大数加小数问题

```hlsl
float a = 100000000.0;  // 1亿
float b = 1.0;
float result = a + b;  // 结果仍然是 100000000.0！

// 原因：1.0 相对于 100000000.0 太小，超出了精度范围
// 100000000.0 的实际指数为 26，绝对精度 = 2^(26-23) = 2^3 = 8.0
// 由于 1.0 < 8.0，无法精确表示 100000001.0
// 下一个可表示的浮点数是 100000008.0（差值为 8）
```

> [!example] 实际影响
> 当数值较大时（如时间累积、世界坐标），小的增量会被完全"吞没"，导致计算结果不准确。

### 精度等级（在移动平台上）

```hlsl
// 高精度（32位）
float highp_value;      // 范围：(-2^62, 2^62)，精度：2^(-16)
mediump float value1;   // 等同于 float（在桌面GPU上）

// 中精度（16位，移动GPU常用）
half mediump_value;     // 范围：(-2^14, 2^14)，精度：2^(-10)
mediump half value2;

// 低精度（10位，仅移动GPU）
fixed lowp_value;       // 范围：[-2, 2]，精度：2^(-8)
lowp fixed value3;
```

> [!tip] 最佳实践
> - **桌面 GPU**：统一使用 `float`
> - **移动 GPU**：
>   - 颜色/UV 坐标：使用 `half`
>   - 世界坐标/法线：使用 `float`
>   - 简单系数（0-1）：使用 `fixed`

---

## 避免误差扩大

关于如何避免浮点数误差扩大的详细方法和最佳实践，请参考：

→ [[PrecisionOptimization]] - 精度优化技巧

该文档包含以下内容：
- 避免大数加小数的方法
- 避免接近零的除法
- 避免连续的乘除运算
- Kahan 求和算法（高精度求和）
- 三角函数的精度优化
- 浮点数比较的容差方法
- 法线归一化的优化
- 避免灾难性抵消（Catastrophic Cancellation）

---

## 常用精度常量

以下是 Unity ShaderLibrary 中定义的常用浮点数精度常量：

```hlsl
// float（32位）相关常量
#define REAL_EPS 1.192092896e-07  // float 的机器精度（2^(-23)）
#define REAL_MIN 1.175494351e-38  // float 的最小正规数
#define REAL_MAX 3.402823466e+38  // float 的最大值

// half（16位）相关常量
#define HALF_EPS 9.765625e-04     // half 的机器精度（2^(-10)）
#define HALF_MIN 6.103515625e-05  // half 的最小正规数
#define HALF_MAX 65504.0          // half 的最大值

// 常用的安全阈值
#define EPSILON_SAFE_DIV 1e-6     // 安全除法阈值
#define EPSILON_NORMALIZE 1e-10   // 归一化阈值
```

> [!tip] 使用建议
> - `REAL_EPS`：用于浮点数比较的最小容差
> - `EPSILON_SAFE_DIV`：除法运算中防止除零的安全阈值
> - `EPSILON_NORMALIZE`：判断向量是否为零向量的阈值
> - `HALF_MAX`：移动平台 half 类型溢出检查

**相关文件**：
- [[Common]] - Unity Core ShaderLibrary 核心文件
- [[PrecisionOptimization]] - 着色器性能优化技巧
