# [[ParallaxMapping]] - 视差映射

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl`
- **主要职责**：提供视差映射函数，用于模拟表面深度
- **使用场景**：需要视差效果的着色器

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和数学函数
- **被依赖的文件**：使用视差映射的着色器

## 核心函数解析

### `GetViewDirectionTangentSpace(half4 tangentWS, half3 normalWS, half3 viewDirWS)`

- **签名**：`half3 GetViewDirectionTangentSpace(half4 tangentWS, half3 normalWS, half3 viewDirWS)`
- **功能**：将视图方向转换到切线空间
- **实现原理**：
  ```hlsl
  half3 GetViewDirectionTangentSpace(half4 tangentWS, half3 normalWS, half3 viewDirWS)
  {
      // 计算副切线
      half crossSign = (tangentWS.w > 0.0 ? 1.0 : -1.0);
      half3 bitang = crossSign * cross(normalWS.xyz, tangentWS.xyz);
      
      // 构建 TBN 矩阵
      half3x3 tangentSpaceTransform = half3x3(WorldSpaceTangent, WorldSpaceBiTangent, WorldSpaceNormal);
      half3 viewDirTS = mul(tangentSpaceTransform, viewDirWS);
      return viewDirTS;
  }
  ```
- **数学原理**：
  - 构建 TBN（Tangent-Bitangent-Normal）矩阵
  - 将世界空间视图方向转换到切线空间
  - `tangentWS.w` 存储副切线方向标志

### `ParallaxOffset1Step(half height, half amplitude, half3 viewDirTS)`

- **签名**：`half2 ParallaxOffset1Step(half height, half amplitude, half3 viewDirTS)`
- **功能**：单步视差偏移计算
- **实现原理**：
  ```hlsl
  half2 ParallaxOffset1Step(half height, half amplitude, half3 viewDirTS)
  {
      height = height * amplitude - amplitude / 2.0;
      half3 v = normalize(viewDirTS);
      v.z += 0.42; // 偏移以避免除零
      return height * (v.xy / v.z);
  }
  ```
- **数学原理**：
  - 视差偏移：`offset = height * (viewDir.xy / viewDir.z)`
  - `0.42` 偏移避免除零和数值不稳定
  - 单步方法简单但可能产生走样

### `ParallaxMapping(TEXTURE2D_PARAM(heightMap, sampler_heightMap), half3 viewDirTS, half scale, float2 uv)`

- **签名**：`float2 ParallaxMapping(...)`
- **功能**：视差映射主函数
- **实现原理**：
  ```hlsl
  float2 ParallaxMapping(TEXTURE2D_PARAM(heightMap, sampler_heightMap), half3 viewDirTS, half scale, float2 uv)
  {
      half h = SAMPLE_TEXTURE2D(heightMap, sampler_heightMap, uv).g;
      float2 offset = ParallaxOffset1Step(h, scale, viewDirTS);
      return offset;
  }
  ```
- **使用示例**：
  ```hlsl
  float2 parallaxUV = uv + ParallaxMapping(_HeightMap, sampler_HeightMap, viewDirTS, _ParallaxScale, uv);
  float4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, parallaxUV);
  ```

## 与其他模块的关系

- [[Common]]：依赖基础类型
- [[SpaceTransforms]]：使用切线空间变换

## 参考资料

- Parallax Mapping：https://en.wikipedia.org/wiki/Parallax_mapping

