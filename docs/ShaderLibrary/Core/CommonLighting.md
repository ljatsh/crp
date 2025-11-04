# [[CommonLighting]] - 光照计算工具函数

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl`
- **主要职责**：提供光照计算相关的工具函数，包括衰减函数、遮挡计算、BSDF 角度计算等
- **使用场景**：所有需要计算光照的着色器

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和数学函数
- **被依赖的文件**：[[BSDF]], [[AreaLighting]] - 使用本文件的光照工具

## 光照约定

```hlsl
// Light direction is oriented backward (-Z). 
// i.e in shader code, light direction is -lightData.forward
```

光线方向向后（-Z），即着色器代码中的光线方向是 `-lightData.forward`。

## 核心函数解析

### `PunctualLightAttenuation(real4 distances, real rangeAttenuationScale, real rangeAttenuationBias, real lightAngleScale, real lightAngleOffset)`

- **签名**：`real PunctualLightAttenuation(real4 distances, real rangeAttenuationScale, real rangeAttenuationBias, real lightAngleScale, real lightAngleOffset)`
- **功能**：计算点光源/聚光灯的衰减（结合距离衰减和角度衰减）
- **实现原理**：
  ```hlsl
  real PunctualLightAttenuation(real4 distances, real rangeAttenuationScale, real rangeAttenuationBias,
                                real lightAngleScale, real lightAngleOffset)
  {
      real distSq   = distances.y;
      real distRcp  = distances.z;
      real distProj = distances.w;
      real cosFwd   = distProj * distRcp;
      
      real attenuation = min(distRcp, 1.0 / PUNCTUAL_LIGHT_THRESHOLD);
      attenuation *= DistanceWindowing(distSq, rangeAttenuationScale, rangeAttenuationBias);
      attenuation *= AngleAttenuation(cosFwd, lightAngleScale, lightAngleOffset);
      
      return Sq(attenuation);
  }
  ```
- **参数说明**：
  - `distances`: `{d, d², 1/d, d_proj}`，其中 `d_proj = dot(lightToSample, lightData.forward)`
  - `rangeAttenuationScale`: `1 / r²`（如果启用范围衰减）或 `2¹² / r²`（如果禁用）
  - `rangeAttenuationBias`: `1`（如果启用）或 `2²⁴`（如果禁用）
  - `lightAngleScale` / `lightAngleOffset`: 角度衰减参数
- **数学原理**：
  - 距离衰减：`1 / d²`（物理正确）
  - 范围窗口：`saturate(bias - d² * scale)²`（平滑截断）
  - 角度衰减：`saturate(cosFwd * scale + offset)²`（聚光灯）
  - 组合：`attenuation = (距离衰减 * 范围窗口 * 角度衰减)²`
- **参考**：Moving Frostbite to PBR
- **性能注意事项**：
  - 使用预计算的 `distRcp` 避免重复计算
  - `Sq(attenuation)` 提供平滑过渡

### `DistanceWindowing(real distSquare, real rangeAttenuationScale, real rangeAttenuationBias)`

- **签名**：`real DistanceWindowing(real distSquare, real rangeAttenuationScale, real rangeAttenuationBias)`
- **功能**：非物理的窗口函数，限制光线影响范围到 `attenuationRadius`
- **实现原理**：
  ```hlsl
  real DistanceWindowing(real distSquare, real rangeAttenuationScale, real rangeAttenuationBias)
  {
      return saturate(rangeAttenuationBias - Sq(distSquare * rangeAttenuationScale));
  }
  ```
- **数学原理**：
  - 如果启用范围衰减：`scale = 1/r²`, `bias = 1`
  - 如果禁用范围衰减：`scale = 2¹²/r²`, `bias = 2²⁴`
  - 结果：`saturate(bias - (d² * scale)²)`
- **性能注意事项**：
  - 平方结果提供平滑过渡
  - 非物理但性能友好

### `SmoothWindowedDistanceAttenuation(real distSquare, real distRcp, real rangeAttenuationScale, real rangeAttenuationBias)`

- **签名**：`real SmoothWindowedDistanceAttenuation(real distSquare, real distRcp, real rangeAttenuationScale, real rangeAttenuationBias)`
- **功能**：物理正确的距离衰减 + 范围窗口
- **实现原理**：
  ```hlsl
  real SmoothWindowedDistanceAttenuation(real distSquare, real distRcp, real rangeAttenuationScale, real rangeAttenuationBias)
  {
      real attenuation = min(distRcp, 1.0 / PUNCTUAL_LIGHT_THRESHOLD);
      attenuation *= DistanceWindowing(distSquare, rangeAttenuationScale, rangeAttenuationBias);
      return Sq(attenuation);
  }
  ```
- **数学原理**：
  - 物理衰减：`1 / d`
  - 限制最小值：`min(1/d, 1/0.01)` 避免除零
  - 应用范围窗口并平方平滑

### `GetHorizonOcclusion(real3 V, real3 normalWS, real3 vertexNormal, real horizonFade)`

- **签名**：`real GetHorizonOcclusion(real3 V, real3 normalWS, real3 vertexNormal, real horizonFade)`
- **功能**：计算地平线遮挡（用于法线贴图反射）
- **实现原理**：
  ```hlsl
  real GetHorizonOcclusion(real3 V, real3 normalWS, real3 vertexNormal, real horizonFade)
  {
      real3 R = reflect(-V, normalWS);
      real specularOcclusion = saturate(1.0 + horizonFade * dot(R, vertexNormal));
      return specularOcclusion * specularOcclusion;
  }
  ```
- **数学原理**：
  - 反射向量 `R` 和顶点法线 `vertexNormal` 的点积
  - 如果 `dot(R, vertexNormal) < 0`，说明反射方向被遮挡
  - `horizonFade` 控制衰减强度
- **参考**：Horizon Occlusion for Normal Mapped Reflections
- **性能注意事项**：
  - 平方结果提供平滑过渡
  - 比基于环境遮挡的方法更快

### `GetSpecularOcclusionFromAmbientOcclusion(real NdotV, real ambientOcclusion, real roughness)`

- **签名**：`real GetSpecularOcclusionFromAmbientOcclusion(real NdotV, real ambientOcclusion, real roughness)`
- **功能**：从环境遮挡计算镜面遮挡
- **实现原理**：
  ```hlsl
  real GetSpecularOcclusionFromAmbientOcclusion(real NdotV, real ambientOcclusion, real roughness)
  {
      return saturate(PositivePow(NdotV + ambientOcclusion, exp2(-16.0 * roughness - 1.0)) - 1.0 + ambientOcclusion);
  }
  ```
- **数学原理**：
  - 基于 Gotanda 2011 的方法
  - 粗糙度越高，镜面遮挡越接近环境遮挡
  - 粗糙度越低，镜面遮挡受 `NdotV` 影响越大
- **参考**：Moving Frostbite to PBR - Gotanda siggraph 2011

### `GTAOMultiBounce(real visibility, real3 albedo)`

- **签名**：`real3 GTAOMultiBounce(real visibility, real3 albedo)`
- **功能**：将环境遮挡更新为彩色环境遮挡（基于光线反弹统计和物体反照率）
- **实现原理**：
  ```hlsl
  real3 GTAOMultiBounce(real visibility, real3 albedo)
  {
      real3 a =  2.0404 * albedo - 0.3324;
      real3 b = -4.7951 * albedo + 0.6417;
      real3 c =  2.7552 * albedo + 0.6903;
      
      real x = visibility;
      return max(x, ((x * a + b) * x + c) * x);
  }
  ```
- **数学原理**：
  - 使用二次多项式近似多反弹光照
  - 系数基于反照率计算
  - `max(x, polynomial)` 确保结果 >= 输入可见性
- **参考**：Practical Realtime Strategies for Accurate Indirect Occlusion

### `GetBSDFAngle(real3 V, real3 L, real NdotL, real NdotV, out real LdotV, out real NdotH, out real LdotH, out real invLenLV)`

- **签名**：`void GetBSDFAngle(real3 V, real3 L, real NdotL, real NdotV, out real LdotV, out real NdotH, out real LdotH, out real invLenLV)`
- **功能**：计算 BSDF 评估所需的常用角度
- **实现原理**：
  ```hlsl
  void GetBSDFAngle(real3 V, real3 L, real NdotL, real NdotV,
                    out real LdotV, out real NdotH, out real LdotH, out real invLenLV)
  {
      LdotV = dot(L, V);
      invLenLV = rsqrt(max(2.0 * LdotV + 2.0, FLT_EPS));
      NdotH = saturate((NdotL + NdotV) * invLenLV);
      LdotH = saturate(invLenLV * LdotV + invLenLV);
  }
  ```
- **数学原理**：
  - `H = normalize(L + V)`
  - `NdotH = dot(N, normalize(L + V)) = (NdotL + NdotV) / |L + V|`
  - `invLenLV = 1 / |L + V|`
  - 优化：`|L + V|² = 2 + 2*LdotV`（假设 `|L| = |V| = 1`）
- **参考**：PBR Diffuse Lighting for GGX + Smith Microsurfaces, slide 114
- **性能注意事项**：
  - 使用预计算的 `NdotL` 和 `NdotV` 避免重复点积
  - `invLenLV` 可以重用

### `GetViewReflectedNormal(real3 N, real3 V, out real NdotV)`

- **签名**：`real3 GetViewReflectedNormal(real3 N, real3 V, out real NdotV)`
- **功能**：获取面向视图的反射法线，处理背面法线情况
- **实现原理**：
  ```hlsl
  real3 GetViewReflectedNormal(real3 N, real3 V, out real NdotV)
  {
      NdotV = dot(N, V);
      
      // N = (NdotV >= 0.0) ? N : (N - 2.0 * NdotV * V);
      N += (2.0 * saturate(-NdotV)) * V;
      NdotV = abs(NdotV);
      
      return N;
  }
  ```
- **数学原理**：
  - 如果 `NdotV < 0`，将法线反射到 `dot(N, V) = 0` 边界
  - 反射公式：`N' = N - 2 * dot(N, V) * V`
  - 使用 `abs(NdotV)` 确保结果为非负
- **性能注意事项**：
  - 避免分支，使用 `saturate` 和乘法
  - 保持法线映射细节

### `GetLocalFrame(real3 localZ)`

- **签名**：`real3x3 GetLocalFrame(real3 localZ)`
- **功能**：从单位向量生成正交基（行主序）
- **实现原理**：
  ```hlsl
  real3x3 GetLocalFrame(real3 localZ)
  {
      real x  = localZ.x;
      real y  = localZ.y;
      real z  = localZ.z;
      real sz = FastSign(z);
      real a  = 1 / (sz + z);
      real ya = y * a;
      real b  = x * ya;
      real c  = x * sz;
      
      real3 localX = real3(c * x * a - 1, sz * b, c);
      real3 localY = real3(b, y * ya - sz, y);
      
      return real3x3(localX, localY, localZ);
  }
  ```
- **数学原理**：
  - 基于四元数方法（Pixar 方法）
  - 生成的旋转矩阵行列式为 +1
  - 注意：如果 `localZ = {0, 0, 1}`，则 `localX = {-1, 0, 0}`，`localY = {0, -1, 0}`（旋转 180 度）
- **参考**：http://marc-b-reynolds.github.io/quaternions/2016/07/06/Orthonormal.html

### `ComputeWrappedDiffuseLighting(real NdotL, real w)`

- **签名**：`real ComputeWrappedDiffuseLighting(real NdotL, real w)`
- **功能**：能量守恒的包裹漫反射光照
- **实现原理**：
  ```hlsl
  real ComputeWrappedDiffuseLighting(real NdotL, real w)
  {
      return saturate((NdotL + w) / ((1.0 + w) * (1.0 + w)));
  }
  ```
- **数学原理**：
  - `w` 是包裹参数（通常 0-1）
  - `w = 0`：标准 Lambertian
  - `w > 0`：允许负 `NdotL` 产生光照（模拟次表面散射）
  - 能量守恒：积分结果保持恒定
- **参考**：Steve McAuley - Energy-Conserving Wrapped Diffuse

### `ComputeMicroShadowing(real AO, real NdotL, real opacity)`

- **签名**：`real ComputeMicroShadowing(real AO, real NdotL, real opacity)`
- **功能**：计算微阴影（基于环境遮挡和光线角度）
- **实现原理**：
  ```hlsl
  real ComputeMicroShadowing(real AO, real NdotL, real opacity)
  {
      real aperture = 2.0 * AO * AO;
      real microshadow = saturate(NdotL + aperture - 1.0);
      return lerp(1.0, microshadow, opacity);
  }
  ```
- **数学原理**：
  - 环境遮挡定义了一个"孔径"
  - 如果 `NdotL` 大于孔径，则没有微阴影
  - `aperture = 2 * AO²` 提供平滑过渡
- **参考**：The Technical Art of Uncharted 4 - Brinck and Maximov 2016

### `ComputeShadowColor(real shadow, real3 shadowTint, real penumbraFlag)`

- **签名**：`real3 ComputeShadowColor(real shadow, real3 shadowTint, real penumbraFlag)`
- **功能**：计算带颜色的阴影
- **实现原理**：
  ```hlsl
  real3 ComputeShadowColor(real shadow, real3 shadowTint, real penumbraFlag)
  {
      real3 invTint = real3(1.0, 1.0, 1.0) - shadowTint;
      real shadow3 = shadow * shadow * shadow;
      return lerp(real3(1.0, 1.0, 1.0) - ((1.0 - shadow) * invTint)
                  , shadow3 * invTint + shadow * shadowTint,
                  penumbraFlag);
  }
  ```
- **数学原理**：
  - `penumbraFlag = 0`：硬阴影，使用减法混合
  - `penumbraFlag = 1`：软阴影（半影），使用乘法混合
  - `shadow³` 提供更平滑的过渡

## 与其他模块的关系

- [[Common]]：依赖基础类型和数学函数
- [[BSDF]]：使用本文件的 BSDF 角度计算函数
- [[AreaLighting]]：使用衰减函数

## 参考资料

- Moving Frostbite to PBR：https://www.ea.com/frostbite/news/moving-frostbite-to-pbr
- Horizon Occlusion for Normal Mapped Reflections：http://marmosetco.tumblr.com/post/81245981087
- Practical Realtime Strategies for Accurate Indirect Occlusion：http://blog.selfshadow.com/publications/s2016-shading-course/#course_content

