# [[ShadowSamplingTent]] - Tent 函数 PCF 过滤

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl`
- **主要职责**：实现基于 Tent 函数的 PCF（Percentage-Closer Filtering）阴影采样
- **使用场景**：所有需要软阴影的着色器

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和数学函数
- **被依赖的文件**：使用软阴影的着色器

## PCF 过滤原理

PCF（Percentage-Closer Filtering）通过在阴影贴图周围采样多个点并平均结果来产生软阴影。Tent 函数提供了一种高效的权重计算方法。

## Tent 函数

Tent 函数是一个三角形权重函数，形状类似帐篷：

```
    /\
   /  \
  /    \
 /______\
```

权重在中心最大，向边缘线性衰减。

## 核心函数解析

### `SampleShadow_GetTriangleTexelArea(real triangleHeight)`

- **签名**：`real SampleShadow_GetTriangleTexelArea(real triangleHeight)`
- **功能**：计算高度为 `triangleHeight` 的等腰直角三角形在第一个纹理像素上的面积
- **实现原理**：
  ```hlsl
  real SampleShadow_GetTriangleTexelArea(real triangleHeight)
  {
      return triangleHeight - 0.5;
  }
  ```
- **数学原理**：
  - 对于高度为 `h` 的直角三角形，在第一个像素上的面积 = `h - 0.5`
  - 假设像素中心在 `0.5`，所以需要减去 `0.5`

### `SampleShadow_GetTexelAreas_Tent_3x3(real offset, out real4 computedArea, out real4 computedAreaUncut)`

- **签名**：`void SampleShadow_GetTexelAreas_Tent_3x3(real offset, out real4 computedArea, out real4 computedAreaUncut)`
- **功能**：计算 3x3 Tent 过滤器的每个纹理像素覆盖面积
- **实现原理**：
  ```hlsl
  void SampleShadow_GetTexelAreas_Tent_3x3(real offset, out real4 computedArea, out real4 computedAreaUncut)
  {
      // 计算外部区域
      real offset01SquaredHalved = (offset + 0.5) * (offset + 0.5) * 0.5;
      computedAreaUncut.x = computedArea.x = offset01SquaredHalved - offset;
      computedAreaUncut.w = computedArea.w = offset01SquaredHalved;
      
      // 计算中间区域
      computedAreaUncut.y = SampleShadow_GetTriangleTexelArea(1.5 - offset);
      real clampedOffsetLeft = min(offset, 0);
      real areaOfSmallLeftTriangle = clampedOffsetLeft * clampedOffsetLeft;
      computedArea.y = computedAreaUncut.y - areaOfSmallLeftTriangle;
      
      computedAreaUncut.z = SampleShadow_GetTriangleTexelArea(1.5 + offset);
      real clampedOffsetRight = max(offset, 0);
      real areaOfSmallRightTriangle = clampedOffsetRight * clampedOffsetRight;
      computedArea.z = computedAreaUncut.z - areaOfSmallRightTriangle;
  }
  ```
- **参数说明**：
  - `offset`：Tent 中心相对于像素网格的偏移（`[-0.5, 0.5]`）
  - `computedArea`：裁剪后的面积（考虑边界）
  - `computedAreaUncut`：未裁剪的面积
- **数学原理**：
  - Tent 高度为 1.5 纹理像素，宽度为 3 纹理像素
  - 覆盖 4 个纹理像素（X, Y, Z, W）
  - 根据 `offset` 计算每个像素的覆盖面积

### `SampleShadow_GetTexelWeights_Tent_3x3(real offset, out real4 computedWeight)`

- **签名**：`void SampleShadow_GetTexelWeights_Tent_3x3(real offset, out real4 computedWeight)`
- **功能**：计算 3x3 Tent 过滤器的权重（归一化面积）
- **实现原理**：
  ```hlsl
  void SampleShadow_GetTexelWeights_Tent_3x3(real offset, out real4 computedWeight)
  {
      real4 dummy;
      SampleShadow_GetTexelAreas_Tent_3x3(offset, computedWeight, dummy);
      computedWeight *= 0.44444; // 1 / (三角形面积)
  }
  ```
- **数学原理**：
  - 权重 = 面积 / 总三角形面积
  - 总三角形面积 = `3 * 1.5 / 2 = 2.25`
  - 归一化因子 = `1 / 2.25 ≈ 0.44444`

### `SampleShadow_GetTexelWeights_Tent_5x5(real offset, out real3 texelsWeightsA, out real3 texelsWeightsB)`

- **签名**：`void SampleShadow_GetTexelWeights_Tent_5x5(real offset, out real3 texelsWeightsA, out real3 texelsWeightsB)`
- **功能**：计算 5x5 Tent 过滤器的权重
- **实现原理**：
  ```hlsl
  void SampleShadow_GetTexelWeights_Tent_5x5(real offset, out real3 texelsWeightsA, out real3 texelsWeightsB)
  {
      real4 computedArea_From3texelTriangle;
      real4 computedAreaUncut_From3texelTriangle;
      SampleShadow_GetTexelAreas_Tent_3x3(offset, computedArea_From3texelTriangle, computedAreaUncut_From3texelTriangle);
      
      // 5x5 Tent 可以看作 3x3 Tent 向上偏移 1 个纹理像素
      texelsWeightsA.x = 0.16 * (computedArea_From3texelTriangle.x);
      texelsWeightsA.y = 0.16 * (computedAreaUncut_From3texelTriangle.y);
      texelsWeightsA.z = 0.16 * (computedArea_From3texelTriangle.y + 1);
      texelsWeightsB.x = 0.16 * (computedArea_From3texelTriangle.z + 1);
      texelsWeightsB.y = 0.16 * (computedAreaUncut_From3texelTriangle.z);
      texelsWeightsB.z = 0.16 * (computedArea_From3texelTriangle.w);
  }
  ```
- **数学原理**：
  - 5x5 Tent 高度为 2.5 纹理像素，宽度为 5 纹理像素
  - 覆盖 6 个纹理像素
  - 重用 3x3 Tent 的计算结果，向上偏移 1 个像素
  - 归一化因子 = `1 / 6.25 ≈ 0.16`

### `SampleShadow_GetTexelWeights_Tent_7x7(real offset, out real4 texelsWeightsA, out real4 texelsWeightsB)`

- **签名**：`void SampleShadow_GetTexelWeights_Tent_7x7(real offset, out real4 texelsWeightsA, out real4 texelsWeightsB)`
- **功能**：计算 7x7 Tent 过滤器的权重
- **实现原理**：
  ```hlsl
  void SampleShadow_GetTexelWeights_Tent_7x7(real offset, out real4 texelsWeightsA, out real4 texelsWeightsB)
  {
      real4 computedArea_From3texelTriangle;
      real4 computedAreaUncut_From3texelTriangle;
      SampleShadow_GetTexelAreas_Tent_3x3(offset, computedArea_From3texelTriangle, computedAreaUncut_From3texelTriangle);
      
      // 7x7 Tent 可以看作 3x3 Tent 向上偏移 2 个纹理像素
      texelsWeightsA.x = 0.081632 * (computedArea_From3texelTriangle.x);
      texelsWeightsA.y = 0.081632 * (computedAreaUncut_From3texelTriangle.y);
      texelsWeightsA.z = 0.081632 * (computedAreaUncut_From3texelTriangle.y + 1);
      texelsWeightsA.w = 0.081632 * (computedArea_From3texelTriangle.y + 2);
      // ...
  }
  ```
- **数学原理**：
  - 7x7 Tent 高度为 3.5 纹理像素，宽度为 7 纹理像素
  - 覆盖 8 个纹理像素
  - 重用 3x3 Tent 的计算结果，向上偏移 2 个像素
  - 归一化因子 = `1 / 12.25 ≈ 0.081632`

### `SampleShadow_ComputeSamples_Tent_5x5(...)`

- **签名**：`void SampleShadow_ComputeSamples_Tent_5x5(real4 shadowMapTexture_TexelSize, real2 coord, out real fetchesWeights[9], out real2 fetchesUV[9])`
- **功能**：计算 5x5 Tent 过滤器的采样位置和权重（使用双线性采样优化）
- **实现原理**：
  ```hlsl
  void SampleShadow_ComputeSamples_Tent_5x5(real4 shadowMapTexture_TexelSize, real2 coord, out real fetchesWeights[9], out real2 fetchesUV[9])
  {
      real tentWeightsA[3];
      real tentWeightsB[3];
      SampleShadow_GetTexelWeights_Tent_5x5(coord.x, tentWeightsA, tentWeightsB);
      
      // 计算 UV 坐标和权重
      // 使用 3x3 双线性采样（9 次采样）代替 5x5 点采样（25 次采样）
      // ...
  }
  ```
- **性能优化**：
  - 使用双线性采样将 25 次点采样减少到 9 次双线性采样
  - 大幅减少纹理采样次数
- **数学原理**：
  - 将 5x5 区域划分为 3x3 网格
  - 每个网格点使用双线性采样
  - 权重重新分布到网格点

## 使用示例

```hlsl
// 3x3 Tent 过滤
real4 tentWeights;
SampleShadow_GetTexelWeights_Tent_3x3(offset, tentWeights);

real shadow = 0.0;
shadow += SAMPLE_TEXTURE2D_SHADOW(_ShadowMap, sampler_ShadowMap, coord + float2(-1, -1) * texelSize) * tentWeights.x;
shadow += SAMPLE_TEXTURE2D_SHADOW(_ShadowMap, sampler_ShadowMap, coord + float2(0, -1) * texelSize) * tentWeights.y;
shadow += SAMPLE_TEXTURE2D_SHADOW(_ShadowMap, sampler_ShadowMap, coord + float2(1, -1) * texelSize) * tentWeights.z;
shadow += SAMPLE_TEXTURE2D_SHADOW(_ShadowMap, sampler_ShadowMap, coord + float2(-1, 0) * texelSize) * tentWeights.w;
// ...
```

## 性能对比

| 过滤器大小 | 点采样次数 | 双线性采样次数 | 性能提升 |
|-----------|-----------|--------------|---------|
| 3x3       | 9         | 4            | ~2.25x  |
| 5x5       | 25        | 9            | ~2.78x  |
| 7x7       | 49        | 16           | ~3.06x  |

## 与其他模块的关系

- [[Common]]：依赖基础类型和数学函数
- [[CommonShadow]]：使用阴影偏移函数

## 参考资料

- Percentage-Closer Filtering：https://developer.download.nvidia.com/assets/gamedev/docs/PercentageCloserSoftShadows.pdf
- Unity 阴影文档：https://docs.unity3d.com/Manual/shadow-mapping.html

