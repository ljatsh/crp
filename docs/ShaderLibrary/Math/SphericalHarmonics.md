# [[SphericalHarmonics]] - 球谐函数

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/SphericalHarmonics.hlsl`
- **主要职责**：提供球谐函数（Spherical Harmonics）的评估和打包函数
- **使用场景**：环境光照、光照贴图、探针体积

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和数学函数
- **被依赖的文件**：[[EntityLighting]] - 使用 SH 评估光照

## 球谐函数基础

球谐函数是定义在球面上的正交基函数，用于表示球面上的函数：
- **L0**：常数项（1 个系数）
- **L1**：线性项（3 个系数）
- **L2**：二次项（5 个系数）

总计 9 个系数（L0+L1+L2）可以表示大部分环境光照。

## 核心函数解析

### `SHEvalLinearL0L1(real3 N, real4 shAr, real4 shAg, real4 shAb)`

- **签名**：`real3 SHEvalLinearL0L1(real3 N, real4 shAr, real4 shAg, real4 shAb)`
- **功能**：评估 L0+L1 球谐函数（4 个系数）
- **实现原理**：
  ```hlsl
  real3 SHEvalLinearL0L1(real3 N, real4 shAr, real4 shAg, real4 shAb)
  {
      real4 vA = real4(N, 1.0);
      real3 x1;
      x1.r = dot(shAr, vA);
      x1.g = dot(shAg, vA);
      x1.b = dot(shAb, vA);
      return x1;
  }
  ```
- **数学原理**：
  - L0+L1 项：`c₀ + c₁x + c₂y + c₃z`
  - 使用点积快速计算
  - `shAr/shAg/shAb`：RGB 通道的 SH 系数

### `SHEvalLinearL2(real3 N, real4 shBr, real4 shBg, real4 shBb, real4 shC)`

- **签名**：`real3 SHEvalLinearL2(real3 N, real4 shBr, real4 shBg, real4 shBb, real4 shC)`
- **功能**：评估 L2 球谐函数（5 个系数）
- **实现原理**：
  ```hlsl
  real3 SHEvalLinearL2(real3 N, real4 shBr, real4 shBg, real4 shBb, real4 shC)
  {
      real4 vB = N.xyzz * N.yzzx; // {xy, yz, zx, z²}
      real3 x2;
      x2.r = dot(shBr, vB);
      x2.g = dot(shBg, vB);
      x2.b = dot(shBb, vB);
      
      real vC = N.x * N.x - N.y * N.y; // x² - y²
      real3 x3 = shC.rgb * vC;
      
      return x2 + x3;
  }
  ```
- **数学原理**：
  - L2 项：`xy, yz, zx, z², x²-y²`
  - 使用向量化计算优化性能

### `SampleSH9(half4 SHCoefficients[7], half3 N)`

- **签名**：`half3 SampleSH9(half4 SHCoefficients[7], half3 N)`
- **功能**：评估完整的 L0+L1+L2 球谐函数（9 个系数打包成 7 个纹理元素）
- **实现原理**：
  ```hlsl
  half3 SampleSH9(half4 SHCoefficients[7], half3 N)
  {
      half4 shAr = SHCoefficients[0];
      half4 shAg = SHCoefficients[1];
      half4 shAb = SHCoefficients[2];
      half4 shBr = SHCoefficients[3];
      half4 shBg = SHCoefficients[4];
      half4 shBb = SHCoefficients[5];
      half4 shCr = SHCoefficients[6];
      
      half3 res = SHEvalLinearL0L1(N, shAr, shAg, shAb);
      res += SHEvalLinearL2(N, shBr, shBg, shBb, shCr);
      return res;
  }
  ```

## SH 基函数系数

```hlsl
#define kSHBasis0  0.28209479177387814347f // {0, 0} : 1/2 * sqrt(1/Pi)
#define kSHBasis1  0.48860251190291992159f // {1, 0} : 1/2 * sqrt(3/Pi)
#define kSHBasis2  1.09254843059207907054f // {2,-2} : 1/2 * sqrt(15/Pi)
#define kSHBasis3  0.31539156525252000603f // {2, 0} : 1/4 * sqrt(5/Pi)
#define kSHBasis4  0.54627421529603953527f // {2, 2} : 1/4 * sqrt(15/Pi)
```

## 夹紧余弦卷积系数

用于预计算 Lambertian 漫反射的 SH 系数：

```hlsl
#define kClampedCosine0 1.0f
#define kClampedCosine1 2.0f / 3.0f
#define kClampedCosine2 1.0f / 4.0f
```

## 与其他模块的关系

- [[Common]]：依赖基础类型
- [[EntityLighting]]：使用 SH 评估环境光照

## 参考资料

- Efficient Evaluation of Irradiance Environment Maps：ShaderX 2
- Spherical Harmonics：https://en.wikipedia.org/wiki/Spherical_harmonics

