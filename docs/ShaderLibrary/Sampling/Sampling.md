# [[Sampling]] - 采样函数集合

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl`
- **主要职责**：提供各种采样函数（圆盘、球体、半球、圆锥等）和坐标系统转换
- **使用场景**：所有需要采样操作的着色器（环境光、阴影、AO 等）

## 依赖关系

- **依赖的文件**：
  - [[Common]] - 基础类型和数学函数
  - [[Hammersley]] - Hammersley 序列
  - [[Fibonacci]] - Fibonacci 序列
- **被依赖的文件**：使用采样功能的着色器

## 坐标系统转换

### `SphericalToCartesian(real phi, real cosTheta)`

- **签名**：`real3 SphericalToCartesian(real phi, real cosTheta)`
- **功能**：将球坐标转换为笛卡尔坐标（右手系，Z 向上）
- **实现原理**：
  ```hlsl
  real3 SphericalToCartesian(real phi, real cosTheta)
  {
      real sinPhi, cosPhi;
      sincos(phi, sinPhi, cosPhi);
      real sinTheta = SinFromCos(cosTheta);
      return real3(real2(cosPhi, sinPhi) * sinTheta, cosTheta);
  }
  ```
- **数学原理**：
  - `x = sin(θ) * cos(φ)`
  - `y = sin(θ) * sin(φ)`
  - `z = cos(θ)`
  - `θ`：极角（与 Z 轴夹角），`φ`：方位角（XY 平面）

### `TransformGLtoDX(real3 v)`

- **签名**：`real3 TransformGLtoDX(real3 v)`
- **功能**：将 OpenGL 坐标系统转换为 DirectX 坐标系统
- **实现原理**：
  ```hlsl
  real3 TransformGLtoDX(real3 v)
  {
      return v.xzy; // 交换 Y 和 Z
  }
  ```
- **数学原理**：
  - OpenGL：右手系，Z 向上
  - DirectX：左手系，Y 向上，Z 向前
  - 转换：`(x, y, z) → (x, z, y)`

## 核心采样函数

### `SampleDiskUniform(real u1, real u2)`

- **签名**：`real2 SampleDiskUniform(real u1, real u2)`
- **功能**：均匀采样单位圆盘
- **实现原理**：
  ```hlsl
  real2 SampleDiskUniform(real u1, real u2)
  {
      real r   = sqrt(u1);
      real phi = TWO_PI * u2;
      real sinPhi, cosPhi;
      sincos(phi, sinPhi, cosPhi);
      return r * real2(cosPhi, sinPhi);
  }
  ```
- **数学原理**：
  - 使用反变换采样（Inverse Transform Sampling）
  - 半径分布：`r = √u1`（确保面积均匀分布）
  - 角度分布：`φ = 2π * u2`（均匀）
- **参考**：PBRT v3, p. 777

### `SampleDiskCubic(real u1, real u2)`

- **签名**：`real2 SampleDiskCubic(real u1, real u2)`
- **功能**：立方采样单位圆盘（半径线性分布）
- **实现原理**：
  ```hlsl
  real2 SampleDiskCubic(real u1, real u2)
  {
      real r   = u1; // 线性分布
      real phi = TWO_PI * u2;
      real sinPhi, cosPhi;
      sincos(phi, sinPhi, cosPhi);
      return r * real2(cosPhi, sinPhi);
  }
  ```
- **数学原理**：
  - 半径线性分布：`r = u1`
  - 这种分布不均匀，但有时用于特殊效果

### `SampleConeUniform(real u1, real u2, real cos_theta)`

- **签名**：`real3 SampleConeUniform(real u1, real u2, real cos_theta)`
- **功能**：均匀采样圆锥（局部坐标，Z 轴为圆锥轴）
- **实现原理**：
  ```hlsl
  real3 SampleConeUniform(real u1, real u2, real cos_theta)
  {
      float r0 = cos_theta + u1 * (1.0f - cos_theta);
      float r = sqrt(max(0.0, 1.0 - r0 * r0));
      float phi = TWO_PI * u2;
      return float3(r * cos(phi), r * sin(phi), r0);
  }
  ```
- **数学原理**：
  - `cos(θ)` 均匀分布在 `[cos_theta, 1]`
  - `φ` 均匀分布在 `[0, 2π]`
  - 返回局部坐标（Z 轴为圆锥轴）

### `SampleSphereUniform(real u1, real u2)`

- **签名**：`real3 SampleSphereUniform(real u1, real u2)`
- **功能**：均匀采样单位球面
- **实现原理**：
  ```hlsl
  real3 SampleSphereUniform(real u1, real u2)
  {
      real phi      = TWO_PI * u2;
      real cosTheta = 1.0 - 2.0 * u1;
      return SphericalToCartesian(phi, cosTheta);
  }
  ```
- **数学原理**：
  - `cos(θ)` 均匀分布在 `[-1, 1]`
  - `φ` 均匀分布在 `[0, 2π]`
  - 使用球坐标到笛卡尔坐标转换

### `SampleHemisphereCosine(real u1, real u2, real3 normal)`

- **签名**：`real3 SampleHemisphereCosine(real u1, real u2, real3 normal)`
- **功能**：余弦加权采样半球（重要性采样）
- **实现原理**：
  ```hlsl
  real3 SampleHemisphereCosine(real u1, real u2, real3 normal)
  {
      // 在局部坐标系中采样
      real3 localDir = SampleHemisphereCosineUniform(u1, u2);
      // 转换到世界坐标系
      return TransformLocalToWorld(localDir, normal);
  }
  ```
- **数学原理**：
  - 余弦加权：`pdf(θ) = cos(θ) / π`
  - 用于重要性采样 Lambertian 漫反射
  - 提供更少的方差，加快收敛

### `SampleHemisphereUniform(real u1, real u2)`

- **签名**：`real3 SampleHemisphereUniform(real u1, real u2)`
- **功能**：均匀采样半球
- **实现原理**：
  ```hlsl
  real3 SampleHemisphereUniform(real u1, real u2)
  {
      real phi      = TWO_PI * u2;
      real cosTheta = u1; // [0, 1]
      return SphericalToCartesian(phi, cosTheta);
  }
  ```
- **数学原理**：
  - `cos(θ)` 均匀分布在 `[0, 1]`
  - `φ` 均匀分布在 `[0, 2π]`

## 立方贴图转换

### `CubemapTexelToDirection(real2 positionNVC, uint faceId)`

- **签名**：`real3 CubemapTexelToDirection(real2 positionNVC, uint faceId)`
- **功能**：将立方贴图的纹理坐标转换为方向向量
- **实现原理**：
  ```hlsl
  real3 CubemapTexelToDirection(real2 positionNVC, uint faceId)
  {
      real3 dir = CUBEMAP_FACE_BASIS_MAPPING[faceId][0] * positionNVC.x
                 + CUBEMAP_FACE_BASIS_MAPPING[faceId][1] * positionNVC.y
                 + CUBEMAP_FACE_BASIS_MAPPING[faceId][2];
      return normalize(dir);
  }
  ```
- **数学原理**：
  - 使用每个面的基础向量映射
  - `positionNVC`：归一化顶点坐标 `[-1, 1]`
  - `faceId`：立方贴图面索引（0-5）

## 与其他模块的关系

- [[Common]]：依赖基础类型和数学函数
- [[Hammersley]]：使用 Hammersley 序列进行低差异采样
- [[Fibonacci]]：使用 Fibonacci 序列进行均匀采样

## 参考资料

- PBRT v3：https://www.pbr-book.org/
- Monte Carlo Methods in Rendering：https://graphics.stanford.edu/courses/cs348b-00/course8.pdf

