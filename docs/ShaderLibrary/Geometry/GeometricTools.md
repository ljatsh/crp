# [[GeometricTools]] - 几何工具函数

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl`
- **主要职责**：提供几何计算工具函数（旋转、二次方程求解、射线相交测试等）
- **使用场景**：需要几何计算的着色器

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和数学函数
- **被依赖的文件**：使用几何计算的着色器

## 核心函数解析

### `Rotate(float3 pivot, float3 position, float3 rotationAxis, float angle)`

- **签名**：`float3 Rotate(float3 pivot, float3 position, float3 rotationAxis, float angle)`
- **功能**：围绕轴和支点旋转点
- **实现原理**：
  ```hlsl
  float3 Rotate(float3 pivot, float3 position, float3 rotationAxis, float angle)
  {
      rotationAxis = normalize(rotationAxis);
      float3 cpa = pivot + rotationAxis * dot(rotationAxis, position - pivot);
      return cpa + ((position - cpa) * cos(angle) + cross(rotationAxis, (position - cpa)) * sin(angle));
  }
  ```
- **数学原理**：
  - 使用 Rodrigues 旋转公式
  - 将点投影到旋转轴，然后旋转垂直分量

### `SolveQuadraticEquation(float a, float b, float c, out float2 roots)`

- **签名**：`bool SolveQuadraticEquation(float a, float b, float c, out float2 roots)`
- **功能**：求解二次方程 `a*t² + b*t + c = 0`
- **实现原理**：
  ```hlsl
  bool SolveQuadraticEquation(float a, float b, float c, out float2 roots)
  {
      float det = Sq(b) - 4.0 * a * c;
      float sqrtDet = sqrt(det);
      roots.x = (-b - sign(a) * sqrtDet) / (2.0 * a);
      roots.y = (-b + sign(a) * sqrtDet) / (2.0 * a);
      return (det >= 0.0);
  }
  ```
- **数学原理**：
  - 判别式：`Δ = b² - 4ac`
  - 根：`t = (-b ± √Δ) / (2a)`
  - 确保 `roots.x <= roots.y`

### `IntersectRayAABB(float3 rayOrigin, float3 rayDirection, float3 boxMin, float3 boxMax, float tMin, float tMax, out float tEntr, out float tExit)`

- **签名**：`bool IntersectRayAABB(...)`
- **功能**：射线与轴对齐包围盒（AABB）相交测试
- **实现原理**：
  ```hlsl
  bool IntersectRayAABB(...)
  {
      float3 rayDirInv = clamp(rcp(rayDirection), -rcp(FLT_EPS), rcp(FLT_EPS));
      float3 t0 = boxMin * rayDirInv - (rayOrigin * rayDirInv);
      float3 t1 = boxMax * rayDirInv - (rayOrigin * rayDirInv);
      float3 tSlabEntr = min(t0, t1);
      float3 tSlabExit = max(t0, t1);
      tEntr = Max3(tSlabEntr.x, tSlabEntr.y, tSlabEntr.z);
      tExit = Min3(tSlabExit.x, tSlabExit.y, tSlabExit.z);
      tEntr = max(tEntr, tMin);
      tExit = min(tExit, tMax);
      return tEntr < tExit;
  }
  ```
- **数学原理**：
  - 使用 slab 方法（Slab Method）
  - 计算射线与每个轴对齐平面的交点
  - 找到最远的入口和最近的出口

### `IntersectRaySphere(float3 start, float3 dir, float radius, out float2 intersections)`

- **签名**：`bool IntersectRaySphere(float3 start, float3 dir, float radius, out float2 intersections)`
- **功能**：射线与球体相交测试（假设球心在原点）
- **实现原理**：
  ```hlsl
  bool IntersectRaySphere(float3 start, float3 dir, float radius, out float2 intersections)
  {
      float a = dot(dir, dir);
      float b = dot(dir, start) * 2.0;
      float c = dot(start, start) - radius * radius;
      return SolveQuadraticEquation(a, b, c, intersections);
  }
  ```
- **数学原理**：
  - 将问题转化为二次方程求解
  - `||start + t*dir||² = radius²`
  - 展开得到：`a*t² + b*t + c = 0`

## 与其他模块的关系

- [[Common]]：依赖基础类型和数学函数
- [[VolumeRendering]]：使用射线相交测试

## 参考资料

- Real-Time Rendering 4th Edition：https://www.realtimerendering.com/

