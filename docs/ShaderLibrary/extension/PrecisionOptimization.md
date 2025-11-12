---
tags: [shader, optimization, precision, performance, mobile]
aliases: [精度优化, Precision Optimization, Float Precision]
created: 2025-01-20
related:
  - "[[Float]]"
  - "[[Common]]"
---

# Precision Optimization (精度优化)

## Overview (概述)

Precision optimization (精度优化) is crucial for shader performance, especially on mobile GPUs. Choosing the right precision type (`float`, `half`, `fixed`) and avoiding precision-related errors can significantly improve performance and reduce power consumption.

**Related Topics (相关主题)**:
- [[Float]] - Floating-point number formats and precision (浮点数格式和精度)
- [[Common]] - Core shader library functions (核心着色器库函数)

## Precision Types (精度类型)

### float (32-bit)

- **Range (范围)**: Approximately $(-3.4 \times 10^{38}, 3.4 \times 10^{38})$
- **Precision (精度)**: ~7 decimal digits (~7位十进制有效数字)
- **Machine Epsilon (机器ε)**: $2^{-23} \approx 1.19 \times 10^{-7}$
- **Use Cases (使用场景)**:
  - World coordinates (世界坐标)
  - Normal vectors (法线向量)
  - Complex calculations requiring high precision (需要高精度的复杂计算)
  - Desktop GPUs (default) (桌面GPU，默认)

### half (16-bit)

- **Range (范围)**: Approximately $(-65504, 65504)$
- **Precision (精度)**: ~3-4 decimal digits (~3-4位十进制有效数字)
- **Machine Epsilon (机器ε)**: $2^{-10} \approx 9.77 \times 10^{-4}$
- **Use Cases (使用场景)**:
  - Colors (RGB values) (颜色，RGB值)
  - UV coordinates (UV坐标)
  - Local positions (局部位置)
  - Mobile GPUs (recommended for most cases) (移动GPU，大多数情况推荐)

### fixed (10-bit, mobile only)

- **Range (范围)**: Approximately $[-2, 2]$
- **Precision (精度)**: ~2-3 decimal digits (~2-3位十进制有效数字)
- **Machine Epsilon (机器ε)**: $2^{-8} = 0.00390625$
- **Use Cases (使用场景)**:
  - Simple coefficients (0-1 range) (简单系数，0-1范围)
  - Color multipliers (颜色乘数)
  - Simple calculations (简单计算)

## Best Practices (最佳实践)

### 1. Precision Selection Guidelines (精度选择指南)

**Desktop GPU (桌面GPU)**:
```hlsl
// Use float for everything (no performance penalty)
float worldPos;
float normal;
float color;  // Even colors can use float
```

**Mobile GPU (移动GPU)**:
```hlsl
// Optimize precision based on usage (根据使用情况优化精度)
float worldPos;      // High precision for world coordinates (世界坐标高精度)
half color;          // Medium precision for colors (颜色中等精度)
half uv;             // Medium precision for UV coordinates (UV坐标中等精度)
fixed multiplier;    // Low precision for simple coefficients (简单系数低精度)
```

### 2. Avoid Precision Loss (避免精度损失)

#### Large Number + Small Number (大数加小数)

> [!fail] Problem Code (问题代码)
> ```hlsl
> float totalTime = 0.0;
> void Update() {
>     totalTime += deltaTime;  // deltaTime ≈ 0.016
>     // After hours, totalTime becomes very large (几小时后，totalTime变得很大)
>     // deltaTime precision is lost (deltaTime的精度丢失)
> }
> ```

> [!success] Improved Solution (改进方案)
> ```hlsl
> // Use modulo to keep values in reasonable range (使用模运算保持数值在合理范围内)
> float wrappedTime = fmod(totalTime, 3600.0);  // Reset every hour (每小时重置)
> 
> // Or separate integer and fractional parts (或分离整数部分和小数部分)
> int hours = int(totalTime / 3600.0);
> float fractionalTime = fmod(totalTime, 3600.0);
> ```

#### Division by Near Zero (接近零的除法)

> [!fail] Problem Code (问题代码)
> ```hlsl
> float result = 1.0 / verySmallNumber;  // May cause infinity (可能导致无穷大)
> ```

> [!success] Improved Solution (改进方案)
> ```hlsl
> // Add safety threshold (添加安全阈值)
> float SafeDivide(float a, float b) {
>     return a / max(b, 1e-6);  // Or use EPSILON constant (或使用EPSILON常量)
> }
> 
> // Use reciprocal square root (hardware optimized) (使用倒数平方根，硬件优化)
> float invLength = rsqrt(dot(v, v) + 1e-6);
> ```

#### Catastrophic Cancellation (灾难性抵消)

> [!fail] Problem Code
> ```hlsl
> float x = 0.0001;
> float result = sqrt(x * x + 1.0) - 1.0;  // Precision loss
> ```

> [!success] Improved Solution
> ```hlsl
> // Use algebraic identity: sqrt(x² + 1) - 1 = x² / (sqrt(x² + 1) + 1)
> float x = 0.0001;
> float sqrtTerm = sqrt(x * x + 1.0);
> float result = (x * x) / (sqrtTerm + 1.0);  // Avoids subtraction
> ```

> [!info] Principle (原理)
> When two values are very close, their difference causes significant loss of significant digits (当两个数值非常接近时，它们的差会导致有效数字大量丢失). Algebraic transformations (such as rationalization) can avoid this subtraction operation (代数变换（如分子有理化）可以避免这种相减操作).
>
> Common identities (常用恒等式):
> - $\sqrt{x^2 + 1} - 1 = \frac{x^2}{\sqrt{x^2 + 1} + 1}$
> - $\sqrt{x + h} - \sqrt{x} = \frac{h}{\sqrt{x + h} + \sqrt{x}}$

#### Sequential Multiply-Divide Operations (连续乘除运算)

> [!fail] Problem Code (问题代码)
> ```hlsl
> float result = a / b / c / d;  // Error accumulation (误差累积)
> ```

> [!success] Improved Solution (改进方案)
> ```hlsl
> float result = a / (b * c * d);  // Reduce operations, lower error (减少运算次数，降低误差)
> ```

#### Trigonometric Function Precision (三角函数精度)

> [!fail] Problem Code (问题代码)
> ```hlsl
> float angle = 10000.0;  // Very large angle (很大的角度)
> float result = sin(angle);  // Severe precision loss (精度损失严重)
> ```

> [!success] Improved Solution (改进方案)
> ```hlsl
> // Normalize angle to [0, 2π) or [-π, π] first (先将角度归一化到[0, 2π)或[-π, π])
> float normalizedAngle = fmod(angle, 6.28318530718);  // 2π
> float result = sin(normalizedAngle);
> ```

#### Normal Vector Normalization Optimization (法线向量归一化优化)

> [!fail] Problem Code
> ```hlsl
> float3 normal = normalize(input.normal);  // If input.normal is already near unit vector
> ```

> [!success] Improved Solution
> ```hlsl
> // Fast normalization (if vector is known to be near unit length)
> float3 FastNormalize(float3 v) {
>     float lengthSq = dot(v, v);
>     // If already near 1, use Taylor expansion: 1/sqrt(x) ≈ 1 - (x-1)/2
>     return v * (1.5 - 0.5 * lengthSq);  // Only valid when lengthSq ≈ 1
> }
> 
> // Safer version
> float3 SafeNormalize(float3 v, float3 fallback = float3(0, 1, 0)) {
>     float lengthSq = dot(v, v);
>     return lengthSq > 1e-6 ? v * rsqrt(lengthSq) : fallback;
> }
> ```

> [!warning] Note (注意)
> `FastNormalize` is only valid when the vector length is near 1 (`FastNormalize`仅在向量长度接近1时有效). For arbitrary vectors, use `SafeNormalize` or standard `normalize()` (对于任意向量，应使用`SafeNormalize`或标准的`normalize()`).

### 3. Mobile-Specific Optimizations (移动端特定优化)

#### Precision Qualifiers (精度限定符)

```hlsl
// Explicit precision hints for mobile GPUs (为移动GPU提供显式精度提示)
highp float worldPos;    // High precision (32-bit) (高精度，32位)
mediump half color;      // Medium precision (16-bit) (中等精度，16位)
lowp fixed multiplier;   // Low precision (10-bit) (低精度，10位)
```

#### Texture Sampling (纹理采样)

```hlsl
// Use appropriate precision for texture coordinates (为纹理坐标使用适当的精度)
half2 uv;  // Usually sufficient for UV coordinates (通常对UV坐标足够)

// For high-precision requirements (对于高精度要求)
float2 preciseUV;  // Use only when necessary (仅在必要时使用)
```

#### Color Calculations (颜色计算)

```hlsl
// Colors typically don't need full float precision (颜色通常不需要完整的float精度)
half3 albedo = tex2D(_MainTex, uv).rgb;
half3 finalColor = albedo * _Color.rgb;  // half precision is enough (half精度足够)
```

### 4. Mathematical Operations (数学运算)

#### Use Hardware-Optimized Functions (使用硬件优化函数)

```hlsl
// Prefer hardware-optimized functions (优先使用硬件优化函数)
rsqrt(x)      // Faster than 1.0 / sqrt(x) (比1.0 / sqrt(x)更快)
rcp(x)        // Faster than 1.0 / x (比1.0 / x更快)
mad(a, b, c)  // Faster than a * b + c (on some GPUs) (在某些GPU上比a * b + c更快)
```

#### Avoid Expensive Functions (避免昂贵函数)

```hlsl
// ❌ Avoid in fragment shader (避免在片元着色器中使用)
float angle = atan2(y, x);
float result = pow(x, 3.0);

// ✅ Use approximations or pre-compute (使用近似或预计算)
float angle = atan2_approx(y, x);  // Custom approximation (自定义近似)
float result = x * x * x;  // Faster than pow (比pow更快)
```

#### Kahan Summation Algorithm (Kahan求和算法)

For high-precision accumulation (用于高精度累加):

> [!fail] Standard Summation (标准求和)
> ```hlsl
> float sum = 0.0;
> for (int i = 0; i < N; i++) {
>     sum += values[i];  // Error accumulation (误差累积)
> }
> ```

> [!success] Kahan Summation (Compensated Summation) (Kahan求和，补偿求和)
> ```hlsl
> float KahanSum(float values[], int N) {
>     float sum = 0.0;
>     float c = 0.0;  // Compensation variable, stores accumulated error (补偿变量，存储累积误差)
>     
>     for (int i = 0; i < N; i++) {
>         float y = values[i] - c;    // Subtract previous error (减去上次的误差)
>         float t = sum + y;           // New sum (新的和)
>         c = (t - sum) - y;           // Calculate new error (计算新的误差)
>         sum = t;
>     }
>     
>     return sum;
> }
> ```

> [!info] Principle (原理)
> Kahan summation maintains a compensation variable `c` that stores rounding errors from each operation and compensates them back in the next iteration, significantly improving accumulation precision (Kahan求和通过维护一个误差补偿变量`c`，将每次运算产生的舍入误差存储起来，在下一次迭代中补偿回去，显著提高了累加精度).

### 5. Common Pitfalls (常见陷阱)

#### Precision Comparison (精度比较)

> [!danger] Problem Code (问题代码)
> ```hlsl
> if (a == b) { ... }  // Never directly compare floats! (永远不要直接比较浮点数！)
> ```

> [!success] Improved Solution (改进方案)
> ```hlsl
> // Absolute tolerance (绝对容差)
> bool FloatEqual(float a, float b, float epsilon = 1e-5) {
>     return abs(a - b) < epsilon;
> }
> 
> // Relative tolerance (more robust) (相对容差，更健壮)
> bool FloatEqualRelative(float a, float b, float epsilon = 1e-5) {
>     float diff = abs(a - b);
>     float larger = max(abs(a), abs(b));
>     return diff < larger * epsilon;
> }
> ```

> [!tip] Selection Guide (选择指南)
> - Small and fixed value range: Use **absolute tolerance** (数值范围较小且固定：使用**绝对容差**)
> - Large or unknown value range: Use **relative tolerance** (数值范围跨度大或未知：使用**相对容差**)

#### Normalization (归一化)

```hlsl
// Always add epsilon to avoid division by zero (总是添加epsilon以避免除零)
float3 SafeNormalize(float3 v) {
    float lengthSq = dot(v, v);
    return v * rsqrt(lengthSq + 1e-6);
}
```

#### Matrix Operations (矩阵运算)

```hlsl
// Use appropriate precision for matrices (为矩阵使用适当的精度)
float4x4 worldMatrix;      // High precision for world transform (世界变换高精度)
half4x4 localMatrix;       // Medium precision for local transform (局部变换中等精度)
```

## Performance Impact (性能影响)

### Mobile GPU Performance (移动GPU性能)

| Precision (精度) | Performance (性能) | Power Consumption (功耗) | Use Case (使用场景) |
|-----------|-------------|-------------------|----------|
| `float` | Baseline (基准) | High (高) | World coordinates, normals (世界坐标，法线) |
| `half` | ~2x faster (~2倍更快) | Lower (较低) | Colors, UVs, local positions (颜色，UV，局部位置) |
| `fixed` | ~4x faster (~4倍更快) | Lowest (最低) | Simple coefficients (简单系数) |

### Desktop GPU Performance (桌面GPU性能)

On desktop GPUs, precision types typically have similar performance (在桌面GPU上，精度类型通常具有相似的性能):
- `float` is the default and recommended (`float`是默认且推荐的)
- `half` may have no performance benefit (`half`可能没有性能优势)
- Use precision types for correctness, not performance (使用精度类型是为了正确性，而非性能)

## Platform-Specific Considerations (平台特定考虑)

### iOS (Metal)

- `half` is well-optimized (`half`优化良好)
- Use `half` for colors and UVs (颜色和UV使用`half`)
- `float` for world coordinates (世界坐标使用`float`)

### Android (Vulkan/OpenGL ES)

- Precision support varies by GPU (精度支持因GPU而异)
- Test on target devices (在目标设备上测试)
- Some GPUs treat `half` as `float` internally (某些GPU内部将`half`视为`float`)

### Desktop (DX11/DX12/Vulkan)

- `float` is standard (`float`是标准)
- Precision types mainly affect correctness (精度类型主要影响正确性)
- Use appropriate precision for calculations (为计算使用适当的精度)

## Debugging Precision Issues (调试精度问题)

### Common Symptoms (常见症状)

1. **Flickering (闪烁)**: Precision errors in calculations (计算中的精度错误)
2. **Artifacts (伪影)**: Visible precision loss in colors or positions (颜色或位置中可见的精度损失)
3. **Performance (性能)**: Unexpected precision-related slowdowns (意外的精度相关性能下降)

### Debugging Tools (调试工具)

```hlsl
// Visualize precision errors (可视化精度错误)
half3 debugColor = frac(value * 10.0);  // Amplify precision errors (放大精度错误)

// Check precision limits (检查精度限制)
#if defined(SHADER_API_MOBILE)
    // Mobile-specific precision checks (移动端特定精度检查)
#endif
```

## References (参考资料)

- [[Float]] - Detailed floating-point format documentation (详细的浮点数格式文档)
- [Unity Shader Precision](https://docs.unity3d.com/Manual/SL-DataTypesAndPrecision.html) - Unity着色器精度文档
- [Mobile GPU Precision](https://developer.nvidia.com/content/precision-performance-floating-point-and-ieee-754-compliance-nvidia-gpus) - 移动GPU精度文档

## Error Analysis and Propagation: Systematic Preparation (误差分析和传播的系统准备)

> [!todo] TODO: Systematic Learning Framework (系统学习框架)
> 
> To master error analysis and propagation, build a solid foundation through systematic learning (要掌握误差分析和传播，需要通过系统学习建立坚实基础). The following framework provides a comprehensive learning path (以下框架提供了全面的学习路径):

### Core Knowledge Areas (核心知识领域)

#### 1. Mathematics (数学基础)

- **Taylor Expansion (泰勒展开)**: Understanding function approximation and error bounds (理解函数近似和误差界)
  - First-order and higher-order approximations (一阶和高阶近似)
  - Remainder terms and error estimation (余项和误差估计)
  - Applications in numerical differentiation and integration (在数值微分和积分中的应用)

- **Linear Algebra (线性代数)**: Matrix operations and their numerical properties (矩阵运算及其数值性质)
  - Matrix condition numbers (矩阵条件数)
  - Eigenvalue and eigenvector stability (特征值和特征向量稳定性)
  - Singular value decomposition (SVD) (奇异值分解)

- **Probability and Statistics (概率统计)**: Understanding random errors and uncertainty (理解随机误差和不确定性)
  - Error distributions (误差分布)
  - Statistical error propagation (统计误差传播)
  - Monte Carlo methods for error estimation (蒙特卡洛误差估计方法)

#### 2. System Knowledge (系统知识)

- **IEEE 754 Standard (IEEE 754标准)**: Floating-point representation and arithmetic (浮点数表示和算术)
  - Number formats (float, double, half) (数字格式)
  - Rounding modes and exceptions (舍入模式和异常)
  - Special values (NaN, Infinity) (特殊值)

- **Computer Arithmetic (计算机算术)**: How operations are performed (运算如何执行)
  - Floating-point operations (浮点运算)
  - Rounding errors and their accumulation (舍入误差及其累积)
  - Guard digits and extended precision (保护位和扩展精度)

- **Hardware Characteristics (硬件特性)**: GPU and CPU specific behaviors (GPU和CPU特定行为)
  - Fused multiply-add (FMA) operations (融合乘加运算)
  - SIMD operations and precision (SIMD运算和精度)
  - Platform-specific optimizations (平台特定优化)

#### 3. Analysis Methods (分析方法)

- **Error Propagation (误差传播)**: How errors accumulate through calculations (误差如何通过计算累积)
  - Forward error analysis (前向误差分析)
  - Backward error analysis (后向误差分析)
  - Error propagation formulas (误差传播公式)

- **Condition Numbers (条件数)**: Measuring problem sensitivity (测量问题敏感性)
  - Matrix condition numbers (矩阵条件数)
  - Function condition numbers (函数条件数)
  - Ill-conditioned problems (病态问题)

- **Stability Theory (稳定性理论)**: Algorithm numerical stability (算法数值稳定性)
  - Forward stability (前向稳定性)
  - Backward stability (后向稳定性)
  - Mixed stability (混合稳定性)

#### 4. Practical Skills (实践技能)

- **Debugging Skills (调试技能)**: Identifying and diagnosing precision issues (识别和诊断精度问题)
  - Visual debugging techniques (可视化调试技术)
  - Precision visualization methods (精度可视化方法)
  - Error tracking and logging (误差跟踪和日志记录)

- **Verification Methods (验证方法)**: Ensuring correctness (确保正确性)
  - Unit testing with tolerance (带容差的单元测试)
  - Reference implementations (参考实现)
  - Cross-platform validation (跨平台验证)

- **Tool Usage (工具使用)**: Leveraging development tools (利用开发工具)
  - Precision profilers (精度分析器)
  - Numerical analysis libraries (数值分析库)
  - GPU debugging tools (GPU调试工具)

### Learning Path (学习路径)

> [!tip] Recommended Approach (推荐方法)
> 
> Start from understanding floating-point numbers, then gradually deepen into algorithm stability analysis (从理解浮点数开始，逐步深入到算法稳定性分析). With this foundation, you can predict, diagnose, and solve various numerical precision problems (有了这些基础，你就能预见、诊断和解决各种数值精度问题)!

**Suggested Learning Order (建议学习顺序)**:

1. **Foundation (基础)**: IEEE 754 → Computer Arithmetic → Hardware Characteristics
2. **Mathematics (数学)**: Linear Algebra → Taylor Expansion → Probability Statistics
3. **Analysis (分析)**: Error Propagation → Condition Numbers → Stability Theory
4. **Practice (实践)**: Debugging Skills → Verification Methods → Tool Usage

### Key Concepts to Master (需要掌握的关键概念)

- **Machine Epsilon (机器ε)**: The smallest representable difference (可表示的最小差值)
- **Relative vs Absolute Error (相对误差 vs 绝对误差)**: When to use each (何时使用哪个)
- **Cancellation Errors (抵消误差)**: When subtraction causes precision loss (减法何时导致精度损失)
- **Algorithm Stability (算法稳定性)**: Why some algorithms are more robust (为什么某些算法更稳健)
- **Conditioning (条件性)**: Problem sensitivity to input perturbations (问题对输入扰动的敏感性)

