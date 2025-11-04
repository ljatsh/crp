# [[CommonShadow]] - 阴影采样工具

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonShadow.hlsl`
- **主要职责**：提供阴影贴图采样的偏移计算函数
- **使用场景**：所有需要采样阴影贴图的着色器

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和数学函数
- **被依赖的文件**：[[ShadowSamplingTent]] - 使用阴影采样偏移

## 核心函数解析

### `GetShadowPosOffset(real NdotL, real3 normalWS, real2 invShadowMapSize)`

- **签名**：`real3 GetShadowPosOffset(real NdotL, real3 normalWS, real2 invShadowMapSize)`
- **功能**：基于表面法线计算阴影贴图采样偏移，减少阴影痤疮（shadow acne）
- **实现原理**：
  ```hlsl
  real3 GetShadowPosOffset(real NdotL, real3 normalWS, real2 invShadowMapSize)
  {
      real texelSize = 2.0 * invShadowMapSize.x;
      real offsetScaleNormalize = saturate(1.0 - NdotL);
      return texelSize * offsetScaleNormalize * normalWS;
  }
  ```
- **数学原理**：
  - 阴影痤疮是由于深度贴图精度有限导致的
  - 当表面几乎平行于光线方向时，`NdotL` 接近 0，偏移需要更大
  - `offsetScaleNormalize = saturate(1.0 - NdotL)` 提供自适应的偏移比例
  - 偏移方向沿法线方向，偏移大小与纹理像素大小相关
- **参考**：https://mynameismjp.wordpress.com/2015/02/18/shadow-sample-update/
- **性能注意事项**：
  - 使用 `saturate` 避免负值
  - 偏移大小基于纹理像素大小，确保在不同分辨率下表现一致
- **使用示例**：
  ```hlsl
  float3 shadowPos = shadowCoord.xyz;
  shadowPos += GetShadowPosOffset(NdotL, normalWS, _ShadowMapSize.zw);
  float shadow = SAMPLE_TEXTURE2D_SHADOW(_ShadowMap, sampler_ShadowMap, shadowPos);
  ```

## 与其他模块的关系

- [[Common]]：依赖基础类型
- [[ShadowSamplingTent]]：使用本文件的偏移函数

## 参考资料

- MJP's Blog - Shadow Sample Update：https://mynameismjp.wordpress.com/2015/02/18/shadow-sample-update/

