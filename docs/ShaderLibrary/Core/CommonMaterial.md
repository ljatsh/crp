# [[CommonMaterial]] - 材质属性工具函数

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl`
- **主要职责**：提供材质属性的转换和计算函数，包括粗糙度转换、各向异性处理、法线混合、三平面映射等
- **使用场景**：所有使用物理材质的着色器都需要此文件

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和数学函数
- **被依赖的文件**：[[BSDF]], [[CommonLighting]] - 光照计算需要材质属性

## 核心常量定义

### `DEFAULT_SPECULAR_VALUE`

```hlsl
#define DEFAULT_SPECULAR_VALUE 0.04
```

标准电介质的默认镜面反射率（F0），对应于 IOR ≈ 1.5。

### `CLEAR_COAT_IOR` / `CLEAR_COAT_F0`

```hlsl
#define CLEAR_COAT_IOR 1.5
#define CLEAR_COAT_F0 0.04
```

清漆层（Clear Coat）的 IOR 和 F0 值。

## 核心函数解析

### `PerceptualRoughnessToRoughness(real perceptualRoughness)`

- **签名**：`real PerceptualRoughnessToRoughness(real perceptualRoughness)`
- **功能**：将感知粗糙度转换为物理粗糙度
- **实现原理**：
  ```hlsl
  real PerceptualRoughnessToRoughness(real perceptualRoughness)
  {
      return perceptualRoughness * perceptualRoughness;
  }
  ```
  - 感知粗糙度是平方关系，使艺术家更容易控制
  - 感知粗糙度 = sqrt(物理粗糙度)
- **数学原理**：
  - 感知粗糙度范围：`[0, 1]`（0 = 完全光滑，1 = 完全粗糙）
  - 物理粗糙度范围：`[0, 1]`（GGX 分布参数）
  - 转换公式：`α = perceptualRoughness²`
- **使用示例**：
  ```hlsl
  float perceptualRoughness = 0.5; // 中等粗糙度
  float roughness = PerceptualRoughnessToRoughness(perceptualRoughness); // 0.25
  ```

### `RoughnessToPerceptualRoughness(real roughness)`

- **签名**：`real RoughnessToPerceptualRoughness(real roughness)`
- **功能**：将物理粗糙度转换为感知粗糙度
- **实现原理**：
  ```hlsl
  real RoughnessToPerceptualRoughness(real roughness)
  {
      return sqrt(roughness);
  }
  ```
  - 与 `PerceptualRoughnessToRoughness` 互为逆函数

### `BeckmannRoughnessToGGXRoughness(real roughnessBeckmann)`

- **签名**：`real BeckmannRoughnessToGGXRoughness(real roughnessBeckmann)`
- **功能**：将 Beckmann 分布粗糙度转换为 GGX 分布粗糙度
- **实现原理**：
  ```hlsl
  real BeckmannRoughnessToGGXRoughness(real roughnessBeckmann)
  {
      return 0.5 * roughnessBeckmann;
  }
  ```
- **数学原理**：
  - Beckmann 分布：`roughness² = 2 * variance`
  - GGX 分布：`roughness² = variance / 2`
  - 为了匹配方差：`2 * α_GGX² = α_Beckmann² / 2`
  - 因此：`α_GGX = 0.5 * α_Beckmann`
- **参考**：Ray Tracing Gems, p. 153

### `ConvertAnisotropyToRoughness(real perceptualRoughness, real anisotropy, out real roughnessT, out real roughnessB)`

- **签名**：`void ConvertAnisotropyToRoughness(real perceptualRoughness, real anisotropy, out real roughnessT, out real roughnessB)`
- **功能**：将各向异性参数转换为切线和副切线方向的粗糙度
- **实现原理**：
  ```hlsl
  void ConvertValueAnisotropyToValueTB(real value, real anisotropy, out real valueT, out real valueB)
  {
      valueT = value * (1 + anisotropy);
      valueB = value * (1 - anisotropy);
  }
  
  void ConvertAnisotropyToRoughness(real perceptualRoughness, real anisotropy, out real roughnessT, out real roughnessB)
  {
      real roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
      ConvertValueAnisotropyToValueTB(roughness, anisotropy, roughnessT, roughnessB);
  }
  ```
- **数学原理**：
  - 使用 Sony Imageworks 的参数化方法
  - `anisotropy` 范围：`[-1, 1]`
  - `anisotropy > 0`：切线方向更粗糙（拉伸）
  - `anisotropy < 0`：副切线方向更粗糙（压缩）
- **参考**：Revisiting Physically Based Shading at Imageworks, p. 15

### `NormalFiltering(float perceptualSmoothness, float variance, float threshold)`

- **签名**：`float NormalFiltering(float perceptualSmoothness, float variance, float threshold)`
- **功能**：基于法线方差过滤粗糙度，用于抗锯齿
- **实现原理**：
  ```hlsl
  float NormalFiltering(float perceptualSmoothness, float variance, float threshold)
  {
      float roughness = PerceptualSmoothnessToRoughness(perceptualSmoothness);
      // Ref: Geometry into Shading - equation (3)
      float squaredRoughness = saturate(roughness * roughness + min(2.0 * variance, threshold * threshold));
      return RoughnessToPerceptualSmoothness(sqrt(squaredRoughness));
  }
  ```
- **数学原理**：
  - 基于 "Geometry into Shading" 论文的方法
  - 法线变化会增加有效粗糙度：`α'² = α² + 2σ²`
  - `variance` 是法线分布的方差
  - `threshold` 限制最大方差贡献
- **参考**：http://graphics.pixar.com/library/BumpRoughness/paper.pdf

### `GeometricNormalVariance(float3 geometricNormalWS, float screenSpaceVariance)`

- **签名**：`float GeometricNormalVariance(float3 geometricNormalWS, float screenSpaceVariance)`
- **功能**：计算几何法线的屏幕空间方差
- **实现原理**：
  ```hlsl
  float GeometricNormalVariance(float3 geometricNormalWS, float screenSpaceVariance)
  {
      float3 deltaU = ddx(geometricNormalWS);
      float3 deltaV = ddy(geometricNormalWS);
      return screenSpaceVariance * (dot(deltaU, deltaU) + dot(deltaV, deltaV));
  }
  ```
- **数学原理**：
  - 使用屏幕空间导数计算法线变化率
  - 方差 = `screenSpaceVariance * (|∂N/∂u|² + |∂N/∂v|²)`
  - `screenSpaceVariance` 通常为 `0.25`（对应 0.5 像素的标准差）

### `TextureNormalVariance(float avgNormalLength)`

- **签名**：`float TextureNormalVariance(float avgNormalLength)`
- **功能**：从法线贴图的平均法线长度计算方差
- **实现原理**：
  ```hlsl
  float TextureNormalVariance(float avgNormalLength)
  {
      float variance = 0.0;
      if (avgNormalLength < 1.0)
      {
          float avgNormLen2 = avgNormalLength * avgNormalLength;
          float kappa = (3.0 * avgNormalLength - avgNormalLength * avgNormLen2) / (1.0 - avgNormLen2);
          variance = 0.25 / kappa;
      }
      return variance;
  }
  ```
- **数学原理**：
  - 基于 von Mises-Fisher 分布（vMF）
  - `kappa` 是 vMF 分布的集中参数
  - `variance = 1 / (4 * kappa)`
- **参考**：The Order: 1886 SIGGRAPH course notes

### `ComputeDiffuseColor(float3 baseColor, float metallic)`

- **签名**：`float3 ComputeDiffuseColor(float3 baseColor, float metallic)`
- **功能**：根据 Disney 参数化计算漫反射颜色
- **实现原理**：
  ```hlsl
  float3 ComputeDiffuseColor(float3 baseColor, float metallic)
  {
      return baseColor * (1.0 - metallic);
  }
  ```
- **数学原理**：
  - 金属材质没有漫反射（金属度 = 1）
  - 电介质材质使用 `baseColor` 作为漫反射颜色
  - 线性插值：`diffuseColor = baseColor * (1 - metallic)`

### `ComputeFresnel0(float3 baseColor, float metallic, float dielectricF0)`

- **签名**：`float3 ComputeFresnel0(float3 baseColor, float metallic, float dielectricF0)`
- **功能**：计算 Fresnel F0 值
- **实现原理**：
  ```hlsl
  float3 ComputeFresnel0(float3 baseColor, float metallic, float dielectricF0)
  {
      return lerp(dielectricF0.xxx, baseColor, metallic);
  }
  ```
- **数学原理**：
  - 电介质：F0 = `dielectricF0`（通常 0.04）
  - 金属：F0 = `baseColor`（金属的反射颜色）
  - 混合：`F0 = lerp(dielectricF0, baseColor, metallic)`

### `BlendNormalRNM(real3 n1, real3 n2)`

- **签名**：`real3 BlendNormalRNM(real3 n1, real3 n2)`
- **功能**：使用 Reoriented Normal Mapping 方法混合法线
- **实现原理**：
  ```hlsl
  real3 BlendNormalRNM(real3 n1, real3 n2)
  {
      real3 t = n1.xyz + real3(0.0, 0.0, 1.0);
      real3 u = n2.xyz * real3(-1.0, -1.0, 1.0);
      real3 r = (t / t.z) * dot(t, u) - u;
      return r;
  }
  ```
- **数学原理**：
  - RNM 方法保持法线的方向和能量
  - 适用于细节法线贴图混合
  - 比简单归一化混合更准确
- **参考**：http://blog.selfshadow.com/publications/blending-in-detail/

### `BlendNormal(real3 n1, real3 n2)`

- **签名**：`real3 BlendNormal(real3 n1, real3 n2)`
- **功能**：简单的法线混合方法
- **实现原理**：
  ```hlsl
  real3 BlendNormal(real3 n1, real3 n2)
  {
      return normalize(real3(n1.xy * n2.z + n2.xy * n1.z, n1.z * n2.z));
  }
  ```
- **性能注意事项**：
  - 比 RNM 更快（不需要除法）
  - 但精度较低，可能产生能量损失

### `ComputeTriplanarWeights(real3 normal)`

- **签名**：`real3 ComputeTriplanarWeights(real3 normal)`
- **功能**：计算三平面映射的混合权重
- **实现原理**：
  ```hlsl
  real3 ComputeTriplanarWeights(real3 normal)
  {
      real3 blendWeights = abs(normal);
      blendWeights = (blendWeights - 0.2);
      blendWeights = blendWeights * blendWeights * blendWeights; // pow(blendWeights, 3)
      blendWeights = max(blendWeights, real3(0.0, 0.0, 0.0));
      blendWeights /= dot(blendWeights, 1.0);
      return blendWeights;
  }
  ```
- **数学原理**：
  - 基于法线分量的绝对值
  - 使用 3 次方提高锐度
  - 归一化确保权重和为 1
- **参考**：http://http.developer.nvidia.com/GPUGems3/gpugems3_ch01.html

### `RoughnessToVariance(real roughness)` / `VarianceToRoughness(real variance)`

- **签名**：`real RoughnessToVariance(real roughness)` / `real VarianceToRoughness(real variance)`
- **功能**：在粗糙度和方差之间转换（用于堆叠 BRDF，如清漆层）
- **实现原理**：
  ```hlsl
  real RoughnessToVariance(real roughness)
  {
      return 2.0 / Sq(roughness) - 2.0;
  }
  
  real VarianceToRoughness(real variance)
  {
      return sqrt(2.0 / (variance + 2.0));
  }
  ```
- **数学原理**：
  - 基于 Blinn-Phong 到 Beckmann 粗糙度的转换
  - `variance = 2 / α² - 2`
  - 用于堆叠 BRDF 的方差叠加

## 与其他模块的关系

- [[Common]]：依赖基础类型和数学函数
- [[BSDF]]：使用粗糙度转换函数
- [[CommonLighting]]：使用材质属性计算光照

## 参考资料

- Disney BRDF：http://blog.selfshadow.com/publications/s2012-shading-course/
- Geometry into Shading：http://graphics.pixar.com/library/BumpRoughness/paper.pdf
- Revisiting Physically Based Shading at Imageworks：https://www.slideshare.net/selfshadow/revisiting-physically-based-shading-at-imageworks

