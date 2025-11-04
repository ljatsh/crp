# [[Tessellation]] - 曲面细分

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/Tessellation.hlsl`
- **主要职责**：提供曲面细分相关的函数（Phong 细分、屏幕空间细分因子、距离细分因子等）
- **使用场景**：使用曲面细分的着色器

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和数学函数
- **被依赖的文件**：使用曲面细分的着色器

## 核心函数解析

### `PhongTessellation(real3 positionWS, real3 p0, real3 p1, real3 p2, real3 n0, real3 n1, real3 n2, real3 baryCoords, real shape)`

- **签名**：`real3 PhongTessellation(...)`
- **功能**：Phong 细分，根据顶点法线插值位置
- **实现原理**：
  ```hlsl
  real3 PhongTessellation(real3 positionWS, real3 p0, real3 p1, real3 p2, 
                          real3 n0, real3 n1, real3 n2, real3 baryCoords, real shape)
  {
      real3 c0 = ProjectPointOnPlane(positionWS, p0, n0);
      real3 c1 = ProjectPointOnPlane(positionWS, p1, n1);
      real3 c2 = ProjectPointOnPlane(positionWS, p2, n2);
      
      real3 phongPositionWS = baryCoords.x * c0 + baryCoords.y * c1 + baryCoords.z * c2;
      return lerp(positionWS, phongPositionWS, shape);
  }
  ```
- **数学原理**：
  - 将线性插值位置投影到每个顶点的切平面
  - 使用重心坐标混合投影点
  - `shape` 参数控制细分强度
- **参考**：Phong Shading

### `GetScreenSpaceTessFactor(real3 p0, real3 p1, real3 p2, real4x4 viewProjectionMatrix, real4 screenSize, real triangleSize)`

- **签名**：`real3 GetScreenSpaceTessFactor(...)`
- **功能**：计算屏幕空间自适应细分因子
- **实现原理**：
  ```hlsl
  real3 GetScreenSpaceTessFactor(real3 p0, real3 p1, real3 p2, 
                                 real4x4 viewProjectionMatrix, real4 screenSize, real triangleSize)
  {
      real2 edgeScreenPosition0 = ComputeNormalizedDeviceCoordinates(p0, viewProjectionMatrix) * screenSize.xy;
      real2 edgeScreenPosition1 = ComputeNormalizedDeviceCoordinates(p1, viewProjectionMatrix) * screenSize.xy;
      real2 edgeScreenPosition2 = ComputeNormalizedDeviceCoordinates(p2, viewProjectionMatrix) * screenSize.xy;
      
      real EdgeScale = 1.0 / triangleSize;
      real3 tessFactor;
      tessFactor.x = saturate(distance(edgeScreenPosition1, edgeScreenPosition2) * EdgeScale);
      tessFactor.y = saturate(distance(edgeScreenPosition0, edgeScreenPosition2) * EdgeScale);
      tessFactor.z = saturate(distance(edgeScreenPosition0, edgeScreenPosition1) * EdgeScale);
      
      return tessFactor;
  }
  ```
- **数学原理**：
  - 计算三角形三条边在屏幕空间的长度
  - 根据目标三角形大小计算细分因子
  - 确保屏幕空间三角形大小一致
- **参考**：http://twvideo01.ubm-us.net/o1/vault/gdc10/slides/Bilodeau_Bill_Direct3D11TutorialTessellation.pdf

### `GetDistanceBasedTessFactor(real3 p0, real3 p1, real3 p2, real3 cameraPosWS, real tessMinDist, real tessMaxDist)`

- **签名**：`real3 GetDistanceBasedTessFactor(...)`
- **功能**：计算基于距离的细分因子
- **实现原理**：
  ```hlsl
  real3 GetDistanceBasedTessFactor(real3 p0, real3 p1, real3 p2, 
                                    real3 cameraPosWS, real tessMinDist, real tessMaxDist)
  {
      real3 edgePosition0 = 0.5 * (p1 + p2);
      real3 edgePosition1 = 0.5 * (p0 + p2);
      real3 edgePosition2 = 0.5 * (p0 + p1);
      
      real dist0 = distance(edgePosition0, cameraPosWS);
      real dist1 = distance(edgePosition1, cameraPosWS);
      real dist2 = distance(edgePosition2, cameraPosWS);
      
      real fadeDist = tessMaxDist - tessMinDist;
      real3 tessFactor;
      tessFactor.x = saturate(1.0 - (dist0 - tessMinDist) / fadeDist);
      tessFactor.y = saturate(1.0 - (dist1 - tessMinDist) / fadeDist);
      tessFactor.z = saturate(1.0 - (dist2 - tessMinDist) / fadeDist);
      
      return tessFactor;
  }
  ```
- **数学原理**：
  - 计算每条边中点到相机的距离
  - 在 `[tessMinDist, tessMaxDist]` 范围内线性插值
  - 距离越近，细分因子越大

### `CalcTriTessFactorsFromEdgeTessFactors(real3 triVertexFactors)`

- **签名**：`real4 CalcTriTessFactorsFromEdgeTessFactors(real3 triVertexFactors)`
- **功能**：从边细分因子计算三角形细分因子
- **实现原理**：
  ```hlsl
  real4 CalcTriTessFactorsFromEdgeTessFactors(real3 triVertexFactors)
  {
      real4 tess;
      tess.x = triVertexFactors.x;
      tess.y = triVertexFactors.y;
      tess.z = triVertexFactors.z;
      tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0;
      
      return tess;
  }
  ```
- **数学原理**：
  - `x/y/z`：三条边的细分因子
  - `w`：内部细分因子（平均值）

## 与其他模块的关系

- [[Common]]：依赖基础类型和数学函数
- [[SpaceTransforms]]：使用空间变换函数

## 参考资料

- DirectX 11 Tessellation Tutorial：http://twvideo01.ubm-us.net/o1/vault/gdc10/slides/Bilodeau_Bill_Direct3D11TutorialTessellation.pdf

