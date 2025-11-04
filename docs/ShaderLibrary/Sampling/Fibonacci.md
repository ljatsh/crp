# [[Fibonacci]] - Fibonacci 序列采样

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Fibonacci.hlsl`
- **主要职责**：实现基于 Fibonacci 序列和黄金比例的均匀采样
- **使用场景**：需要高质量均匀采样的着色器

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和数学函数
- **被依赖的文件**：[[Sampling]] - 使用 Fibonacci 序列

## Fibonacci 序列简介

Fibonacci 序列采样使用黄金比例（Golden Ratio）生成均匀分布的样本点：
- **均匀分布**：样本在单位圆盘/球面上均匀分布
- **简单高效**：计算成本低
- **适合 GPU**：避免分支和复杂计算

## 核心函数解析

### `Fibonacci2dSeq(real fibN1, real fibN2, uint i)`

- **签名**：`real2 Fibonacci2dSeq(real fibN1, real fibN2, uint i)`
- **功能**：生成基于 Fibonacci 序列的 2D 采样点
- **实现原理**：
  ```hlsl
  real2 Fibonacci2dSeq(real fibN1, real fibN2, uint i)
  {
      return real2(i / fibN1 + (0.5 / fibN1), frac(i * (fibN2 / fibN1)));
  }
  ```
- **参数说明**：
  - `fibN1`：Fibonacci 序列的 `F[N-1]`
  - `fibN2`：Fibonacci 序列的 `F[N-2]`
  - `i`：样本索引
- **数学原理**：
  - `x = i / F[N-1] + 0.5 / F[N-1]`（均匀分布）
  - `y = frac(i * F[N-2] / F[N-1])`（黄金角度间隔）
  - `F[N-2] / F[N-1] ≈ 1 / φ`（黄金比例倒数）
- **参考**：Efficient Quadrature Rules for Illumination Integrals
- **性能**：在 GCN 架构上约 3 个周期（如果 `fibN1` 和 `fibN2` 在编译时已知）

### `Golden2dSeq(uint i, real n)`

- **签名**：`real2 Golden2dSeq(uint i, real n)`
- **功能**：使用黄金比例生成 2D 采样点（Fibonacci 序列的连续版本）
- **实现原理**：
  ```hlsl
  real2 Golden2dSeq(uint i, real n)
  {
      return real2(i / n + (0.5 / n), frac(i * rcp(GOLDEN_RATIO)));
  }
  ```
- **数学原理**：
  - 黄金角度：`2π * (1 - 1/φ) ≈ 2.399963`
  - `x = i / n + 0.5 / n`（均匀分布）
  - `y = frac(i / φ)`（黄金角度间隔）
- **常量定义**：
  ```hlsl
  #define GOLDEN_RATIO 1.618033988749895
  #define GOLDEN_ANGLE 2.399963229728653
  ```

## 预计算表

Unity 提供了预计算的 Fibonacci 序列表：

- `k_Fibonacci2dSeq21[]`：21 个样本
- `k_Fibonacci2dSeq34[]`：34 个样本
- `k_Fibonacci2dSeq55[]`：55 个样本
- `k_Fibonacci2dSeq89[]`：89 个样本
- `k_Fibonacci2dSeq144[]`：144 个样本

## 使用示例

```hlsl
// 采样圆盘
real2 seq = SampleDiskFibonacci(sampleIndex, sampleCount);
real2 diskSample = SampleDiskUniform(seq.x, seq.y);

// 采样半球
real2 seq = SampleHemisphereFibonacci(sampleIndex, sampleCount);
real3 hemisphereSample = SampleHemisphereUniform(seq.x, seq.y);
```

## 与其他模块的关系

- [[Common]]：依赖基础类型和数学函数
- [[Sampling]]：使用 Fibonacci 序列进行采样

## 参考资料

- Efficient Quadrature Rules for Illumination Integrals：https://www.researchgate.net/publication/220649433_Efficient_Quadrature_Rules_for_Illumination_Integrals
- Golden Ratio：https://en.wikipedia.org/wiki/Golden_ratio

