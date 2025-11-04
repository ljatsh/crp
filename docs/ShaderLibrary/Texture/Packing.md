# [[Packing]] - 数据打包/解包

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl`
- **主要职责**：提供各种数据的打包和解包函数（法线、HDR 颜色、整数、浮点数等）
- **使用场景**：优化内存带宽、减少纹理通道使用

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和数学函数
- **被依赖的文件**：使用打包数据的着色器

## 法线打包

### `PackNormalOctRectEncode(real3 n)` / `UnpackNormalOctRectEncode(real2 f)`

- **签名**：`real2 PackNormalOctRectEncode(real3 n)` / `real3 UnpackNormalOctRectEncode(real2 f)`
- **功能**：使用八面体编码将法线打包到 2D 向量
- **实现原理**：
  ```hlsl
  real2 PackNormalOctRectEncode(real3 n)
  {
      // 平面投影
      real3 p = n * rcp(dot(abs(n), 1.0));
      real x = p.x, y = p.y, z = p.z;
      
      // 展开八面体
      real r = saturate(0.5 - 0.5 * x + 0.5 * y);
      real g = x + y;
      
      return real2(CopySign(r, z), g);
  }
  ```
- **数学原理**：
  - 将单位球面投影到八面体，然后展开到矩形
  - 使用符号位存储 Z 分量方向
  - 范围：`[-1, 1]`，可以映射到 `[0, 1]` 存储
- **参考**：http://www.vis.uni-stuttgart.de/~engelhts/paper/vmvOctaMaps.pdf
- **优点**：
  - 仅需 2 个通道存储 3D 向量
  - 编码/解码快速
  - 误差小

### `PackNormalOctQuadEncode(float3 n)` / `UnpackNormalOctQuadEncode(float2 f)`

- **签名**：`float2 PackNormalOctQuadEncode(float3 n)` / `float3 UnpackNormalOctQuadEncode(float2 f)`
- **功能**：使用八面体编码将法线打包到 2D 向量（优化版本）
- **实现原理**：
  ```hlsl
  float2 PackNormalOctQuadEncode(float3 n)
  {
      n *= rcp(max(dot(abs(n), 1.0), 1e-6));
      float t = saturate(-n.z);
      return n.xy + (n.xy >= 0.0 ? t : -t);
  }
  ```
- **数学原理**：
  - 类似矩形编码，但使用优化的计算公式
  - 避免分支，适合 GPU
- **参考**：http://jcgt.org/published/0003/02/01/paper.pdf

### `PackNormalHemiOctEncode(real3 n)` / `UnpackNormalHemiOctEncode(real2 f)`

- **签名**：`real2 PackNormalHemiOctEncode(real3 n)` / `real3 UnpackNormalHemiOctEncode(real2 f)`
- **功能**：使用半球八面体编码（仅适用于法线在半球内的情况）
- **实现原理**：
  ```hlsl
  real2 PackNormalHemiOctEncode(real3 n)
  {
      real l1norm = dot(abs(n), 1.0);
      real2 res = n.xy * (1.0 / l1norm);
      return real2(res.x + res.y, res.x - res.y);
  }
  ```
- **数学原理**：
  - 假设法线在 `z >= 0` 的半球内
  - 使用变换减少存储空间

## HDR 颜色打包

### RGBM 编码

RGBM 编码用于压缩 HDR 颜色到 8 位纹理：
- **R/G/B**：归一化的颜色分量 `[0, 1]`
- **M**：乘数（存储最大值）

### `PackHDR(real3 rgb)` / `UnpackHDR(real4 rgba)`

- **功能**：将 HDR 颜色打包/解包为 RGBM 格式
- **实现原理**：
  ```hlsl
  real4 PackHDR(real3 rgb)
  {
      real maxRGB = max(max(rgb.r, rgb.g), rgb.b);
      real m = ceil(maxRGB * 255.0) / 255.0;
      m = max(m, 1.0 / 255.0);
      return real4(rgb / m, m);
  }
  
  real3 UnpackHDR(real4 rgba)
  {
      return rgba.rgb * rgba.a;
  }
  ```

## 其他打包函数

### `PackFloatToUint(real value, uint offset, uint numBits)`

- **功能**：将浮点数打包到 uint 的指定位段
- **使用场景**：在单个通道中存储多个浮点值

### `UnpackUintToFloat(uint value, uint offset, uint numBits)`

- **功能**：从 uint 的指定位段解包浮点数

## 与其他模块的关系

- [[Common]]：依赖基础类型和数学函数
- [[Texture]]：纹理数据打包
- [[EntityLighting]]：光照贴图 RGBM 编码

## 参考资料

- Octahedron Environment Maps：http://www.vis.uni-stuttgart.de/~engelhts/paper/vmvOctaMaps.pdf
- A Survey of Efficient Representations for Independent Unit Vectors：http://jcgt.org/published/0003/02/01/paper.pdf

