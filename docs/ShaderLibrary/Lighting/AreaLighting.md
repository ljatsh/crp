# [[AreaLighting]] - 面光源计算

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/AreaLighting.hlsl`
- **主要职责**：提供面光源（矩形光源、球体光源）的光照计算函数
- **使用场景**：所有需要面光源光照的着色器

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和数学函数
- **被依赖的文件**：使用面光源的着色器

## 核心函数解析

### 球体光源

#### `DiffuseSphereLightIrradiance(real sinSqSigma, real cosOmega)`

- **签名**：`real DiffuseSphereLightIrradiance(real sinSqSigma, real cosOmega)`
- **功能**：计算球体光源的漫反射辐照度
- **实现原理**：
  ```hlsl
  real DiffuseSphereLightIrradiance(real sinSqSigma, real cosOmega)
  {
  #ifdef APPROXIMATE_SPHERE_LIGHT_NUMERICALLY
      real x = sinSqSigma;
      real y = cosOmega;
      // 数值拟合，平均绝对误差：0.00476944
      return saturate(x * (0.9245867471551246 + y) * 
                      (0.5 + (-0.9245867471551246 + y) * 
                       (0.5359050373687144 + x * (-1.0054221851257754 + 
                        x * (1.8199061187417047 - x * 1.3172081704209504)))));
  #else
      // 精确解析方法（见下文）
  #endif
  }
  ```
- **参数说明**：
  - `sinSqSigma`：球体半角的正弦平方（孔径）
  - `cosOmega`：法线与光源中心方向的余弦
- **数学原理**：
  - 考虑地平线裁剪（horizon clipping）
  - 当光源被地平线遮挡时，辐照度减小
  - 使用数值拟合或精确解析方法
- **参考**：
  - Moving Frostbite to Physically Based Rendering, page 47 (2015)
  - Area Light Sources for Real-Time Graphics (1996)

### 矩形光源

#### `PolygonFormFactor(real4x3 L)`

- **签名**：`real3 PolygonFormFactor(real4x3 L)`
- **功能**：计算矩形光源的向量形式因子（Vector Form Factor）
- **实现原理**：
  ```hlsl
  real3 PolygonFormFactor(real4x3 L)
  {
      L[0] = SafeNormalize(L[0]);
      L[1] = SafeNormalize(L[1]);
      L[2] = SafeNormalize(L[2]);
      L[3] = SafeNormalize(L[3]);
      
      real3 F  = ComputeEdgeFactor(L[0], L[1]);
            F += ComputeEdgeFactor(L[1], L[2]);
            F += ComputeEdgeFactor(L[2], L[3]);
            F += ComputeEdgeFactor(L[3], L[0]);
      
      return INV_TWO_PI * F;
  }
  ```
- **数学原理**：
  - 形式因子（Form Factor）：描述两个表面之间的能量传递
  - 向量形式因子：`F = Σ(edge_i) / (2π)`
  - 每个边使用 `ComputeEdgeFactor` 计算贡献
- **参考**：Improving radiosity solutions through the use of analytically determined form-factors

#### `ComputeEdgeFactor(real3 V1, real3 V2)`

- **签名**：`real3 ComputeEdgeFactor(real3 V1, real3 V2)`
- **功能**：计算边的贡献因子（未归一化）
- **实现原理**：
  ```hlsl
  real3 ComputeEdgeFactor(real3 V1, real3 V2)
  {
      real V1oV2 = dot(V1, V2);
      real3 V1xV2 = cross(V1, V2);
      
      // 近似：y = rsqrt(1.0 - V1oV2^2) * acos(V1oV2)
      // 使用 Horner 形式的 6 次多项式拟合
      real x = abs(V1oV2);
      real y = 1.5707921083647782 + x * (-0.9995697178013095 + ...);
      
      if (V1oV2 < 0)
      {
          y = PI * rsqrt(max(epsilon, saturate(1 - V1oV2 * V1oV2))) - y;
      }
      
      return V1xV2 * y;
  }
  ```
- **数学原理**：
  - 精确公式：`factor = normalize(V1 × V2) * acos(dot(V1, V2))`
  - 使用多项式近似避免 `acos` 和 `normalize` 调用
  - 最大相对误差：`2.6855360216340534 * 10^-6`
- **性能注意事项**：
  - 多项式近似比精确计算快得多
  - 精度足够用于图形应用

#### `PolygonIrradiance(real4x3 L)`

- **签名**：`real PolygonIrradiance(real4x3 L)`
- **功能**：计算矩形光源的辐照度
- **实现原理**：
  ```hlsl
  real PolygonIrradiance(real4x3 L)
  {
  #ifdef APPROXIMATE_POLY_LIGHT_AS_SPHERE_LIGHT
      real3 F = PolygonFormFactor(L);
      return PolygonIrradianceFromVectorFormFactor(F);
  #else
      // 精确的矩形光源计算（包括地平线裁剪）
      // ClipQuadToHorizon - 裁剪矩形到地平线
      // 然后计算精确的辐照度
  #endif
  }
  ```
- **数学原理**：
  - **近似模式**：将矩形光源近似为球体光源
  - **精确模式**：计算矩形光源的精确辐照度，包括地平线裁剪
- **性能注意事项**：
  - 近似模式更快但精度较低
  - 精确模式更准确但计算成本更高

#### `PolygonIrradianceFromVectorFormFactor(float3 F)`

- **签名**：`real PolygonIrradianceFromVectorFormFactor(float3 F)`
- **功能**：从向量形式因子计算辐照度（近似方法）
- **实现原理**：
  ```hlsl
  real PolygonIrradianceFromVectorFormFactor(float3 F)
  {
      float l = length(F);
      return max(0, (l * l + F.z) / (l + 1));
  }
  ```
- **数学原理**：
  - 这是一个经验性公式，用于近似球体光源辐照度
  - `F.z` 是形式因子的垂直分量
  - 公式确保结果在合理范围内

### 地平线裁剪

矩形光源的计算包括地平线裁剪步骤：

1. **检测裁剪配置**：根据顶点是否在地平线上方（`z > 0`）确定配置
2. **裁剪边**：将被地平线遮挡的边裁剪掉
3. **计算辐照度**：使用裁剪后的多边形计算辐照度

## 近似方法

### `APPROXIMATE_POLY_LIGHT_AS_SPHERE_LIGHT`

- **功能**：将矩形光源近似为球体光源
- **优点**：性能更好
- **缺点**：精度较低

### `APPROXIMATE_SPHERE_LIGHT_NUMERICALLY`

- **功能**：使用数值拟合计算球体光源
- **优点**：性能好，精度可接受
- **缺点**：平均绝对误差约 0.0048

## 与其他模块的关系

- [[Common]]：依赖基础类型和数学函数
- [[BSDF]]：使用面光源计算镜面反射

## 参考资料

- Moving Frostbite to Physically Based Rendering：https://www.ea.com/frostbite/news/moving-frostbite-to-pbr
- Area Light Sources for Real-Time Graphics：https://www.researchgate.net/publication/220533705_Area_Light_Sources_for_Real-Time_Graphics
- Real-Time Area Lighting: a Journey from Research to Production：SIGGRAPH 2014

