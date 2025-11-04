# [[BSDF]] - 双向散射分布函数

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl`
- **主要职责**：实现物理正确的 BRDF（双向反射分布函数）模型，包括 Fresnel 项、法线分布函数（NDF）、几何函数（G）、漫反射项等
- **使用场景**：所有基于物理的渲染（PBR）着色器

## 依赖关系

- **依赖的文件**：[[Color]] - 颜色空间转换
- **被依赖的文件**：[[CommonLighting]], [[AreaLighting]] - 使用 BRDF 函数计算光照

## 重要说明

```hlsl
// Note: All NDF and diffuse term have a version with and without divide by PI.
// Version with divide by PI are use for direct lighting.
// Version without divide by PI are use for image based lighting where often the PI cancel during importance sampling
```

所有 NDF 和漫反射项都有两个版本：
- **带 `/PI`**：用于直接光照
- **不带 `/PI`**：用于基于图像的光照（IBL），其中 PI 在重要性采样时会被抵消

## 核心类型定义

### `CBSDF` 结构体

余弦加权的 BSDF（考虑投影立体角）：

```hlsl
struct CBSDF
{
    float3 diffR; // Diffuse  reflection   (T -> MS -> T, same sides)
    float3 specR; // Specular reflection   (R, RR, TRT, etc)
    float3 diffT; // Diffuse  transmission (rough T or TT, opposite sides)
    float3 specT; // Specular transmission (T, TT, TRRT, etc)
};
```

## Fresnel 项

### `F_Schlick(real f0, real f90, real u)`

- **签名**：`real F_Schlick(real f0, real f90, real u)` / `real3 F_Schlick(real3 f0, real f90, real u)`
- **功能**：Schlick 近似计算 Fresnel 反射率
- **实现原理**：
  ```hlsl
  real F_Schlick(real f0, real f90, real u)
  {
      real x = 1.0 - u;
      real x2 = x * x;
      real x5 = x * x2 * x2;
      return (f90 - f0) * x5 + f0;
  }
  ```
- **数学原理**：
  - Schlick 近似：`F(u) = f0 + (f90 - f0) * (1 - u)^5`
  - `u = cos(θ)`（入射角余弦）
  - `f0`：垂直入射的反射率（Fresnel F0）
  - `f90`：掠射角的反射率（通常为 1.0）
- **性能**：5 次 MAD（乘加）操作
- **参考**：An Inexpensive BRDF Model for Physically-based Rendering

### `F_FresnelDielectric(real ior, real u)`

- **签名**：`real F_FresnelDielectric(real ior, real u)`
- **功能**：精确的电介质 Fresnel 方程
- **实现原理**：
  ```hlsl
  real F_FresnelDielectric(real ior, real u)
  {
      real g = sqrt(Sq(ior) + Sq(u) - 1.0);
      return 1.0 - saturate(1.0 - 0.5 * Sq((g - u) / (g + u)) * 
                           (1.0 + Sq(((g + u) * u - 1.0) / ((g - u) * u + 1.0))));
  }
  ```
- **数学原理**：
  - `g = sqrt(η² + cos²θ - 1)`，其中 `η = IOR_transmitted / IOR_incident`
  - 基于 Fresnel 方程的完整形式
  - 处理全内反射（TIR）情况
- **参考**：https://seblagarde.wordpress.com/2013/04/29/memo-on-fresnel-equations/

### `F_FresnelConductor(real3 eta, real3 etak2, real cosTheta)`

- **签名**：`real3 F_FresnelConductor(real3 eta, real3 etak2, real cosTheta)`
- **功能**：精确的导体 Fresnel 方程
- **实现原理**：
  ```hlsl
  real3 F_FresnelConductor(real3 eta, real3 etak2, real cosTheta)
  {
      real cosTheta2 = cosTheta * cosTheta;
      real sinTheta2 = 1.0 - cosTheta2;
      real3 eta2 = eta * eta;
      
      real3 t0 = eta2 - etak2 - sinTheta2;
      real3 a2plusb2 = sqrt(t0 * t0 + 4.0 * eta2 * etak2);
      real3 t1 = a2plusb2 + cosTheta2;
      real3 a = sqrt(0.5 * (a2plusb2 + t0));
      real3 t2 = 2.0 * a * cosTheta;
      real3 Rs = (t1 - t2) / (t1 + t2);
      
      real3 t3 = cosTheta2 * a2plusb2 + sinTheta2 * sinTheta2;
      real3 t4 = t2 * sinTheta2;
      real3 Rp = Rs * (t3 - t4) / (t3 + t4);
      
      return 0.5 * (Rp + Rs);
  }
  ```
- **数学原理**：
  - `eta = η_t / η_i`（折射率比）
  - `etak2 = k²`（消光系数平方）
  - `Rs`：s 偏振反射率
  - `Rp`：p 偏振反射率
  - 最终：`F = (Rs + Rp) / 2`（非偏振光）

### `IorToFresnel0(real transmittedIor, real incidentIor)`

- **签名**：`real IorToFresnel0(real transmittedIor, real incidentIor)`
- **功能**：将折射率（IOR）转换为 Fresnel F0
- **实现原理**：
  ```hlsl
  TEMPLATE_2_REAL(IorToFresnel0, transmittedIor, incidentIor, 
                  return Sq((transmittedIor - incidentIor) / (transmittedIor + incidentIor)))
  ```
- **数学原理**：
  - Fresnel 方程在垂直入射时的简化：`F0 = ((η₂ - η₁) / (η₂ + η₁))²`
  - 对于空气界面（η₁ = 1）：`F0 = ((η - 1) / (η + 1))²`

## 镜面 BRDF

### GGX 分布（各向同性）

#### `D_GGXNoPI(real NdotH, real roughness)`

- **签名**：`real D_GGXNoPI(real NdotH, real roughness)`
- **功能**：GGX 法线分布函数（不带 `/PI`）
- **实现原理**：
  ```hlsl
  real D_GGXNoPI(real NdotH, real roughness)
  {
      real a2 = Sq(roughness);
      real s = (NdotH * a2 - NdotH) * NdotH + 1.0;
      return SafeDiv(a2, s * s);
  }
  ```
- **数学原理**：
  - GGX（Trowbridge-Reitz）分布：`D(h) = α² / (π * ((α² - 1) * cos²θ + 1)²)`
  - 优化形式：`D(h) = α² / ((α² * cos²θ - cos²θ) + 1)²`
  - 当 `roughness = 0` 时，返回 `(NdotH == 1 ? 1 : 0)`（完美镜面）
- **参考**：Microfacet Models for Refraction through Rough Surfaces

#### `D_GGX(real NdotH, real roughness)`

- **签名**：`real D_GGX(real NdotH, real roughness)`
- **功能**：GGX 法线分布函数（带 `/PI`，用于直接光照）
- **实现原理**：
  ```hlsl
  real D_GGX(real NdotH, real roughness)
  {
      return INV_PI * D_GGXNoPI(NdotH, roughness);
  }
  ```

### Smith 几何函数

#### `G_MaskingSmithGGX(real NdotV, real roughness)`

- **签名**：`real G_MaskingSmithGGX(real NdotV, real roughness)`
- **功能**：Smith 遮蔽函数 G1（仅考虑视图方向）
- **实现原理**：
  ```hlsl
  real G_MaskingSmithGGX(real NdotV, real roughness)
  {
      return 1.0 / (0.5 + 0.5 * sqrt(1.0 + Sq(roughness) * (1.0 / Sq(NdotV) - 1.0)));
  }
  ```
- **数学原理**：
  - `G1(V) = 1 / (1 + Λ(V))`
  - `Λ(V) = -0.5 + 0.5 * sqrt(1 + α² * tan²θ)`
  - `tan²θ = 1 / cos²θ - 1`
- **参考**：Understanding the Masking-Shadowing Function in Microfacet-Based BRDFs

#### `V_SmithJointGGX(real NdotL, real NdotV, real roughness)`

- **签名**：`real V_SmithJointGGX(real NdotL, real NdotV, real roughness)`
- **功能**：Smith 联合几何函数（考虑光和视图方向）
- **实现原理**：
  ```hlsl
  real V_SmithJointGGX(real NdotL, real NdotV, real roughness, real partLambdaV)
  {
      real a2 = Sq(roughness);
      real lambdaV = NdotL * partLambdaV;
      real lambdaL = NdotV * sqrt((-NdotL * a2 + NdotL) * NdotL + a2);
      return 0.5 / max(lambdaV + lambdaL, REAL_MIN);
  }
  ```
- **数学原理**：
  - `V = G / (4 * NdotL * NdotV)`
  - `G = G1(L) * G1(V)`
  - `λ(V) = (-1 + sqrt(1 + α² * tan²θ)) / 2`
  - 优化：预计算 `partLambdaV` 避免重复计算
- **参考**：http://jcgt.org/published/0003/02/03/paper.pdf

#### `DV_SmithJointGGX(real NdotH, real NdotL, real NdotV, real roughness)`

- **签名**：`real DV_SmithJointGGX(real NdotH, real NdotL, real NdotV, real roughness)`
- **功能**：内联的 `D * V` 计算，优化代码生成
- **实现原理**：
  ```hlsl
  real DV_SmithJointGGX(real NdotH, real NdotL, real NdotV, real roughness, real partLambdaV)
  {
      real a2 = Sq(roughness);
      real s = (NdotH * a2 - NdotH) * NdotH + 1.0;
      real lambdaV = NdotL * partLambdaV;
      real lambdaL = NdotV * sqrt((-NdotL * a2 + NdotL) * NdotL + a2);
      real2 D = real2(a2, s * s);
      real2 G = real2(1, lambdaV + lambdaL);
      return INV_PI * 0.5 * (D.x * G.x) / max(D.y * G.y, REAL_MIN);
  }
  ```
- **性能注意事项**：
  - 内联计算减少函数调用开销
  - 使用分数形式避免重复计算
  - 适合直接光照计算

### GGX 各向异性分布

#### `D_GGXAnisoNoPI(real TdotH, real BdotH, real NdotH, real roughnessT, real roughnessB)`

- **签名**：`real D_GGXAnisoNoPI(real TdotH, real BdotH, real NdotH, real roughnessT, real roughnessB)`
- **功能**：各向异性 GGX 分布
- **实现原理**：
  ```hlsl
  real D_GGXAnisoNoPI(real TdotH, real BdotH, real NdotH, real roughnessT, real roughnessB)
  {
      real a2 = roughnessT * roughnessB;
      real3 v = real3(roughnessB * TdotH, roughnessT * BdotH, a2 * NdotH);
      real s = dot(v, v);
      return SafeDiv(a2 * a2 * a2, s * s);
  }
  ```
- **数学原理**：
  - 各向异性分布：`D(h) = 1 / (π * αx * αy * ((αx² * cos²φ + αy² * sin²φ) / cos⁴θ + tan²θ)²)`
  - `αx = roughnessT`，`αy = roughnessB`
  - 优化形式使用向量点积

#### `V_SmithJointGGXAniso(...)`

- **签名**：`real V_SmithJointGGXAniso(real TdotV, real BdotV, real NdotV, real TdotL, real BdotL, real NdotL, real roughnessT, real roughnessB)`
- **功能**：各向异性 Smith 联合几何函数
- **实现原理**：
  ```hlsl
  real V_SmithJointGGXAniso(..., real partLambdaV)
  {
      real lambdaV = NdotL * partLambdaV;
      real lambdaL = NdotV * length(real3(roughnessT * TdotL, roughnessB * BdotL, NdotL));
      return 0.5 / (lambdaV + lambdaL);
  }
  ```
- **数学原理**：
  - `λ(V) = length(roughnessT * TdotV, roughnessB * BdotV, NdotV)`
  - 类似各向同性版本，但使用向量长度

## 漫反射 BRDF

### `LambertNoPI()` / `Lambert()`

- **签名**：`real LambertNoPI()` / `real Lambert()`
- **功能**：Lambert 漫反射（标准漫反射模型）
- **实现原理**：
  ```hlsl
  real LambertNoPI() { return 1.0; }
  real Lambert() { return INV_PI; }
  ```
- **数学原理**：
  - Lambert 漫反射：`f_diffuse = albedo / π`
  - 常数项：`1 / π`

### `DisneyDiffuseNoPI(real NdotV, real NdotL, real LdotV, real perceptualRoughness)`

- **签名**：`real DisneyDiffuseNoPI(real NdotV, real NdotL, real LdotV, real perceptualRoughness)`
- **功能**：Disney 漫反射模型（考虑 Fresnel 效应）
- **实现原理**：
  ```hlsl
  real DisneyDiffuseNoPI(real NdotV, real NdotL, real LdotV, real perceptualRoughness)
  {
      real fd90 = 0.5 + (perceptualRoughness + perceptualRoughness * LdotV);
      real lightScatter = F_Schlick(1.0, fd90, NdotL);
      real viewScatter = F_Schlick(1.0, fd90, NdotV);
      return rcp(1.03571) * (lightScatter * viewScatter);
  }
  ```
- **数学原理**：
  - 使用两个 Schlick Fresnel 项模拟次表面散射
  - `fd90 = 0.5 + 2 * roughness * LdotH²`
  - 归一化因子 `1.03571` 确保能量守恒
- **参考**：Physically Based Shading at Disney

### `DiffuseGGXNoPI(real3 albedo, real NdotV, real NdotL, real NdotH, real LdotV, real roughness)`

- **签名**：`real3 DiffuseGGXNoPI(real3 albedo, real NdotV, real NdotL, real NdotH, real LdotV, real roughness)`
- **功能**：基于 GGX 微表面的漫反射模型
- **实现原理**：
  ```hlsl
  real3 DiffuseGGXNoPI(real3 albedo, real NdotV, real NdotL, real NdotH, real LdotV, real roughness)
  {
      real facing = 0.5 + 0.5 * LdotV;
      real rough = facing * (0.9 - 0.4 * facing) * (0.5 / NdotH + 1);
      real transmitL = F_Transm_Schlick(0, NdotL);
      real transmitV = F_Transm_Schlick(0, NdotV);
      real smooth = transmitL * transmitV * 1.05;
      real single = lerp(smooth, rough, roughness);
      real multiple = roughness * (0.1159 * PI);
      return single + albedo * multiple;
  }
  ```
- **数学原理**：
  - 考虑单次反弹和多次反弹
  - `single`：单次散射项（光滑到粗糙的插值）
  - `multiple`：多次散射项（与反照率相关）
- **参考**：Diffuse Lighting for GGX + Smith Microsurfaces

## 特殊材质模型

### 虹彩（Iridescence）

#### `EvalIridescence(...)`

- **签名**：`real3 EvalIridescence(real eta_1, real cosTheta1, real iridescenceThickness, real3 baseLayerFresnel0, real iorOverBaseLayer = 0.0)`
- **功能**：评估薄膜干涉效果（虹彩）
- **实现原理**：
  - 基于薄膜干涉物理模型
  - 计算光程差（OPD）和相位偏移
  - 使用傅里叶空间评估 XYZ 敏感度曲线
- **参考**：https://belcour.github.io/blog/research/2017/05/01/brdf-thin-film.html

### 布料（Fabric）

#### `D_CharlieNoPI(real NdotH, real roughness)`

- **签名**：`real D_CharlieNoPI(real NdotH, real roughness)`
- **功能**：Charlie 分布（用于布料渲染）
- **实现原理**：
  ```hlsl
  real D_CharlieNoPI(real NdotH, real roughness)
  {
      float invR = rcp(roughness);
      float cos2h = NdotH * NdotH;
      float sin2h = 1.0 - cos2h;
      return (2.0 + invR) * PositivePow(sin2h, invR * 0.5) / 2.0;
  }
  ```
- **参考**：https://knarkowicz.wordpress.com/2018/01/04/cloth-shading/

## 与其他模块的关系

- [[Color]]：依赖颜色空间转换
- [[CommonMaterial]]：使用粗糙度转换函数
- [[CommonLighting]]：使用 BSDF 角度计算

## 参考资料

- Microfacet Models for Refraction through Rough Surfaces：https://www.cs.cornell.edu/~srm/publications/EGSR07-btdf.pdf
- Physically Based Shading at Disney：https://www.disneyanimation.com/publications/physically-based-shading-at-disney/
- Moving Frostbite to PBR：https://www.ea.com/frostbite/news/moving-frostbite-to-pbr
- Real-Time Rendering 4th Edition：https://www.realtimerendering.com/

