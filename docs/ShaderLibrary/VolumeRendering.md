# [[VolumeRendering]] - 体积渲染

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl`
- **主要职责**：提供参与介质（Participating Media）的体积渲染函数
- **使用场景**：雾、烟雾、体积光等效果

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和数学函数
- **被依赖的文件**：体积渲染效果的着色器

## 核心概念

### 光学深度（Optical Depth）

```hlsl
// OpticalDepth(x, y) = Integral{x, y}{Extinction(t) dt}
```

光学深度是沿路径的消光系数积分。

### 透射率（Transmittance）

```hlsl
// Transmittance(x, y) = Exp(-OpticalDepth(x, y))
real TransmittanceFromOpticalDepth(real opticalDepth)
{
    return exp(-opticalDepth);
}
```

透射率表示光线通过介质的比例。

### 不透明度（Opacity）

```hlsl
real OpacityFromOpticalDepth(real opticalDepth)
{
    return 1 - TransmittanceFromOpticalDepth(opticalDepth);
}
```

## 核心函数解析

### `OpticalDepthHeightFog(real baseExtinction, real baseHeight, real2 heightExponents, real cosZenith, real startHeight, real intervalLength)`

- **签名**：`real OpticalDepthHeightFog(...)`
- **功能**：计算高度雾的光学深度
- **数学原理**：
  - 考虑高度的指数衰减
  - 考虑天顶角（cosZenith）
  - 用于大气散射和雾效果

### `HenyeyGreensteinPhaseFunction(real anisotropy, real cosTheta)`

- **签名**：`real HenyeyGreensteinPhaseFunction(real anisotropy, real cosTheta)`
- **功能**：Henyey-Greenstein 相位函数（用于各向异性散射）
- **数学原理**：
  - `anisotropy`：各向异性参数 `[-1, 1]`
  - `cosTheta`：散射角余弦
  - 公式：`P(θ) = (1 - g²) / (4π * (1 + g² - 2g*cosθ)^(3/2))`

### `RayleighPhaseFunction(real cosTheta)`

- **签名**：`real RayleighPhaseFunction(real cosTheta)`
- **功能**：Rayleigh 相位函数（用于小粒子散射，如大气）
- **数学原理**：
  - `P(θ) = 3/16π * (1 + cos²θ)`
  - 各向同性较强

### `ImportanceSampleHomogeneousMedium(real rndVal, real extinction, real intervalLength, out real offset, out real weight)`

- **签名**：`void ImportanceSampleHomogeneousMedium(...)`
- **功能**：重要性采样均匀介质
- **数学原理**：
  - 使用指数分布采样
  - 权重考虑透射率
  - 用于 Monte Carlo 体积渲染

## 与其他模块的关系

- [[Common]]：依赖基础类型和数学函数
- [[GeometricTools]]：使用射线相交测试

## 参考资料

- Participating Media：https://www.pbr-book.org/3ed-2018/Light_Transport_II_Volume_Rendering
- Henyey-Greenstein Phase Function：https://en.wikipedia.org/wiki/Henyey%E2%80%93Greenstein_phase_function

