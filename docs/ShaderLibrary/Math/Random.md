# [[Random]] - 伪随机数生成

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl`
- **主要职责**：提供伪随机数生成函数（哈希函数、Jenkins 哈希、梯度噪声等）
- **使用场景**：所有需要随机数的着色器（噪声、抖动、采样等）

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和数学函数
- **被依赖的文件**：使用随机数的着色器

## 核心函数解析

### `Hash(uint s)`

- **签名**：`float Hash(uint s)`
- **功能**：简单的哈希函数，生成 `[0, 1)` 范围的浮点数
- **实现原理**：
  ```hlsl
  float Hash(uint s)
  {
      s = s ^ 2747636419u;
      s = s * 2654435769u;
      s = s ^ (s >> 16);
      s = s * 2654435769u;
      s = s ^ (s >> 16);
      s = s * 2654435769u;
      return float(s) * rcp(4294967296.0); // 2^-32
  }
  ```
- **数学原理**：
  - 使用 XOR 和乘法进行哈希
  - 多次混合确保良好的分布
  - 归一化到 `[0, 1)` 范围
- **性能注意事项**：
  - GLES2 安全（HLSLcc 会模拟缺失的运算符）
  - 常量 `2654435769u` 是黄金比例的倍数

### `JenkinsHash(uint x)`

- **签名**：`uint JenkinsHash(uint x)` / `uint JenkinsHash(uint2/3/4 v)`
- **功能**：Bob Jenkins 的 One-At-A-Time 哈希算法
- **实现原理**：
  ```hlsl
  uint JenkinsHash(uint x)
  {
      x += (x << 10u);
      x ^= (x >>  6u);
      x += (x <<  3u);
      x ^= (x >> 11u);
      x += (x << 15u);
      return x;
  }
  ```
- **数学原理**：
  - 使用位移和 XOR 操作混合位
  - 适合向量输入（递归调用）
- **参考**：Bob Jenkins Hash

### `GenerateHashedRandomFloat(uint x)`

- **签名**：`float GenerateHashedRandomFloat(uint x)` / `float GenerateHashedRandomFloat(uint2/3/4 v)`
- **功能**：从哈希值生成伪随机浮点数
- **实现原理**：
  ```hlsl
  float ConstructFloat(uint m)
  {
      const int ieeeMantissa = 0x007FFFFF;
      const int ieeeOne      = 0x3F800000;
      
      m &= ieeeMantissa;
      m |= ieeeOne;
      
      float f = asfloat(m);
      return f - 1; // [0, 1)
  }
  
  float GenerateHashedRandomFloat(uint x)
  {
      return ConstructFloat(JenkinsHash(x));
  }
  ```
- **数学原理**：
  - 使用 IEEE 754 浮点数的尾数位
  - 将哈希值转换为 `[0, 1)` 范围的浮点数
- **参考**：https://stackoverflow.com/a/17479300

### `InterleavedGradientNoise(float2 pixCoord, int frameCount)`

- **签名**：`float InterleavedGradientNoise(float2 pixCoord, int frameCount)`
- **功能**：交错梯度噪声（用于时间抗锯齿）
- **实现原理**：
  ```hlsl
  float InterleavedGradientNoise(float2 pixCoord, int frameCount)
  {
      const float3 magic = float3(0.06711056f, 0.00583715f, 52.9829189f);
      float2 frameMagicScale = float2(2.083f, 4.867f);
      float2 v = pixCoord + frameCount * frameMagicScale;
      return frac(magic.z * frac(dot(v, magic.xy)));
  }
  ```
- **数学原理**：
  - 基于像素坐标和帧数生成空间和时间相关的噪声
  - 用于 TAA（时间抗锯齿）的抖动
- **参考**：Next Generation Post Processing in Call of Duty: Advanced Warfare

## 与其他模块的关系

- [[Common]]：依赖基础类型和数学函数
- [[Hashes]]：使用哈希函数

## 参考资料

- Bob Jenkins Hash：https://burtleburtle.net/bob/hash/
- Interleaved Gradient Noise：http://advances.realtimerendering.com/s2014/index.html

