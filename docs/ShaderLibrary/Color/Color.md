# [[Color]] - 颜色空间转换

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl`
- **主要职责**：提供各种颜色空间之间的转换函数（sRGB、Gamma、线性、亮度等）
- **使用场景**：所有需要颜色空间转换的着色器

## 依赖关系

- **依赖的文件**：[[ACES]] - ACES 颜色空间转换
- **被依赖的文件**：[[BSDF]], [[EntityLighting]] - 使用颜色转换函数

## 核心函数解析

### sRGB 转换

#### `SRGBToLinear(real c)`

- **签名**：`real SRGBToLinear(real c)` / `real3 SRGBToLinear(real3 c)`
- **功能**：将 sRGB 颜色转换到线性空间
- **实现原理**：
  ```hlsl
  real SRGBToLinear(real c)
  {
      real linearRGBLo  = c / 12.92;
      real linearRGBHi  = PositivePow((c + 0.055) / 1.055, 2.4);
      real linearRGB    = (c <= 0.04045) ? linearRGBLo : linearRGBHi;
      return linearRGB;
  }
  ```
- **数学原理**：
  - sRGB 标准定义了两个分段函数
  - 线性部分：`c <= 0.04045` 时，`linear = c / 12.92`
  - 幂函数部分：`c > 0.04045` 时，`linear = ((c + 0.055) / 1.055)^2.4`
  - 临界值 `0.04045` 对应线性值 `0.0031308`
- **性能注意事项**：
  - 在移动平台使用 `half` 精度时需要限制值范围避免溢出
  - 使用 `PositivePow` 避免负值警告

#### `LinearToSRGB(real c)`

- **签名**：`real LinearToSRGB(real c)` / `real3 LinearToSRGB(real3 c)`
- **功能**：将线性颜色转换到 sRGB 空间
- **实现原理**：
  ```hlsl
  real LinearToSRGB(real c)
  {
      real sRGBLo = c * 12.92;
      real sRGBHi = (PositivePow(c, 1.0/2.4) * 1.055) - 0.055;
      real sRGB   = (c <= 0.0031308) ? sRGBLo : sRGBHi;
      return sRGB;
  }
  ```
- **数学原理**：
  - 线性部分：`linear <= 0.0031308` 时，`sRGB = linear * 12.92`
  - 幂函数部分：`linear > 0.0031308` 时，`sRGB = 1.055 * linear^(1/2.4) - 0.055`
  - 这是 `SRGBToLinear` 的逆函数

### 快速 sRGB 转换

#### `FastSRGBToLinear(real c)`

- **签名**：`real FastSRGBToLinear(real c)` / `real3 FastSRGBToLinear(real3 c)`
- **功能**：快速 sRGB 到线性转换（多项式近似）
- **实现原理**：
  ```hlsl
  real FastSRGBToLinear(real c)
  {
      return c * (c * (c * 0.305306011 + 0.682171111) + 0.012522878);
  }
  ```
- **数学原理**：
  - 使用 Horner 方法的三次多项式近似
  - 避免 `pow` 函数调用，性能更好
  - 精度略低于标准方法，但通常足够
- **参考**：http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html
- **性能注意事项**：
  - 比标准方法快约 3-5 倍（无 `pow` 调用）
  - 适合对精度要求不高的场景

#### `FastLinearToSRGB(real c)`

- **签名**：`real FastLinearToSRGB(real c)` / `real3 FastLinearToSRGB(real3 c)`
- **功能**：快速线性到 sRGB 转换（幂函数近似）
- **实现原理**：
  ```hlsl
  real FastLinearToSRGB(real c)
  {
      return saturate(1.055 * PositivePow(c, 0.416666667) - 0.055);
  }
  ```
- **数学原理**：
  - 使用 `0.416666667 ≈ 1/2.4` 的幂函数近似
  - 简化了分段函数，统一使用幂函数形式
  - `saturate` 确保结果在 `[0, 1]` 范围内

### Gamma 转换

#### `Gamma22ToLinear(real c)` / `LinearToGamma22(real c)`

- **签名**：`real Gamma22ToLinear(real c)` / `real LinearToGamma22(real c)`
- **功能**：Gamma 2.2 空间与线性空间转换
- **实现原理**：
  ```hlsl
  real Gamma22ToLinear(real c)
  {
      return PositivePow(c, 2.2);
  }
  
  real LinearToGamma22(real c)
  {
      return PositivePow(c, 0.454545454545455); // 1/2.2
  }
  ```
- **数学原理**：
  - Gamma 2.2：`linear = gamma^2.2`
  - 逆变换：`gamma = linear^(1/2.2) ≈ linear^0.4545`

#### `Gamma20ToLinear(real c)` / `LinearToGamma20(real c)`

- **签名**：`real Gamma20ToLinear(real c)` / `real LinearToGamma20(real c)`
- **功能**：Gamma 2.0 空间与线性空间转换（优化版本）
- **实现原理**：
  ```hlsl
  real Gamma20ToLinear(real c)
  {
      return c * c; // 平方，比 pow(c, 2.0) 快
  }
  
  real LinearToGamma20(real c)
  {
      return sqrt(c); // 开方，比 pow(c, 0.5) 快
  }
  ```
- **数学原理**：
  - Gamma 2.0：`linear = gamma²`
  - 逆变换：`gamma = √linear`
- **性能注意事项**：
  - 使用 `c * c` 和 `sqrt` 比 `pow` 快得多
  - 适合需要 Gamma 2.0 的场景

### 亮度计算

#### `Luminance(real3 linearRgb)`

- **签名**：`real Luminance(real3 linearRgb)` / `real Luminance(real4 linearRgba)`
- **功能**：计算线性 RGB 的亮度（CIE 1931）
- **实现原理**：
  ```hlsl
  real Luminance(real3 linearRgb)
  {
      return dot(linearRgb, real3(0.2126729, 0.7151522, 0.0721750));
  }
  ```
- **数学原理**：
  - 使用 CIE 1931 XYZ 颜色空间的 Y 分量权重
  - 权重：`R = 0.2126729`, `G = 0.7151522`, `B = 0.0721750`
  - 基于 sRGB 原色和 D65 白点
- **使用场景**：
  - 色调映射
  - 自动曝光计算
  - 灰度转换

### 颜色空间转换矩阵

文件还包含多种颜色空间转换矩阵，用于：
- RGB ↔ XYZ
- RGB ↔ YCbCr
- RGB ↔ HSV
- 各种显示器色彩空间（Rec.709, Rec.2020, DCI-P3 等）

## 与其他模块的关系

- [[ACES]]：依赖 ACES 颜色空间转换
- [[BSDF]]：使用颜色转换处理材质颜色
- [[EntityLighting]]：使用颜色转换处理光照贴图

## 参考资料

- sRGB 标准：https://www.w3.org/Graphics/Color/srgb
- CIE 1931 颜色空间：https://en.wikipedia.org/wiki/CIE_1931_color_space
- Chilliant's Blog：http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html

