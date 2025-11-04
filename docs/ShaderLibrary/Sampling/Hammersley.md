# [[Hammersley]] - Hammersley 低差异序列

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Hammersley.hlsl`
- **主要职责**：实现 Hammersley 低差异序列（Low-Discrepancy Sequence）用于准随机采样
- **使用场景**：需要高质量采样的着色器（环境光、AO、SSR 等）

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和数学函数
- **被依赖的文件**：[[Sampling]] - 使用 Hammersley 序列

## Hammersley 序列简介

Hammersley 序列是一种低差异序列，具有以下特点：
- **低差异**：样本分布更均匀，减少聚类
- **快速收敛**：相比随机采样更快收敛
- **确定性**：给定索引总是产生相同样本

## 核心函数解析

### `ReverseBits32(uint bits)`

- **签名**：`uint ReverseBits32(uint bits)`
- **功能**：反转 32 位整数的位顺序
- **实现原理**：
  ```hlsl
  uint ReverseBits32(uint bits)
  {
  #if (SHADER_TARGET >= 45)
      return reversebits(bits); // 使用硬件指令
  #else
      // 软件实现：分治反转
      bits = (bits << 16) | (bits >> 16);
      bits = ((bits & 0x00ff00ff) << 8) | ((bits & 0xff00ff00) >> 8);
      bits = ((bits & 0x0f0f0f0f) << 4) | ((bits & 0xf0f0f0f0) >> 4);
      bits = ((bits & 0x33333333) << 2) | ((bits & 0xcccccccc) >> 2);
      bits = ((bits & 0x55555555) << 1) | ((bits & 0xaaaaaaaa) >> 1);
      return bits;
  #endif
  }
  ```
- **数学原理**：
  - 使用分治算法反转位
  - 先交换 16 位块，再交换 8 位块，以此类推
  - 在 Shader Model 4.5+ 上使用硬件指令

### `VanDerCorputBase2(uint i)`

- **签名**：`real VanDerCorputBase2(uint i)`
- **功能**：计算 Van der Corput 序列（基数 2）
- **实现原理**：
  ```hlsl
  real VanDerCorputBase2(uint i)
  {
      return ReverseBits32(i) * rcp(4294967296.0); // 2^-32
  }
  ```
- **数学原理**：
  - Van der Corput 序列：将整数 `i` 的二进制表示反转，然后除以 `2^32`
  - 结果在 `[0, 1)` 范围内均匀分布
  - 这是 Hammersley 序列的基础

### `Hammersley2dSeq(uint i, uint sequenceLength)`

- **签名**：`real2 Hammersley2dSeq(uint i, uint sequenceLength)`
- **功能**：生成 2D Hammersley 序列点
- **实现原理**：
  ```hlsl
  real2 Hammersley2dSeq(uint i, uint sequenceLength)
  {
      return real2(real(i) / real(sequenceLength), VanDerCorputBase2(i));
  }
  ```
- **数学原理**：
  - `x = i / N`（均匀分布）
  - `y = VanDerCorputBase2(i)`（低差异分布）
  - 组合产生 2D 低差异序列

## 预计算表

为了性能，Unity 提供了预计算的 Hammersley 序列表：

- `k_Hammersley2dSeq16[]`：16 个样本的序列
- `k_Hammersley2dSeq32[]`：32 个样本的序列
- `k_Hammersley2dSeq64[]`：64 个样本的序列
- `k_Hammersley2dSeq256[]`：256 个样本的序列

## 使用示例

```hlsl
// 使用预计算表
real2 sample = k_Hammersley2dSeq16[sampleIndex];

// 或动态计算
real2 sample = Hammersley2dSeq(sampleIndex, 16);

// 采样圆盘
real2 diskSample = SampleDiskUniform(sample.x, sample.y);
```

## 与其他模块的关系

- [[Common]]：依赖基础类型和数学函数
- [[Sampling]]：使用 Hammersley 序列进行采样

## 参考资料

- Hammersley Sequence：http://holger.dammertz.org/stuff/notes_HammersleyOnHemisphere.html
- Low-Discrepancy Sequences：https://en.wikipedia.org/wiki/Low-discrepancy_sequence

