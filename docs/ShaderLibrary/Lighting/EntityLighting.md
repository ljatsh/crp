# [[EntityLighting]] - 实体光照

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl`
- **主要职责**：提供光照贴图（Lightmap）和探针体积（Probe Volume）的采样函数
- **使用场景**：所有需要烘焙光照的着色器

## 依赖关系

- **依赖的文件**：
  - [[Common]] - 基础类型和数学函数
  - [[Color]] - 颜色空间转换
  - [[SphericalHarmonics]] - 球谐函数
- **被依赖的文件**：使用烘焙光照的着色器

## 光照贴图编码

### RGBM 编码

```hlsl
#define LIGHTMAP_RGBM_MAX_GAMMA     real(5.0)
#define LIGHTMAP_RGBM_MAX_LINEAR    real(34.493242) // 5.0 ^ 2.2
```

RGBM 编码用于压缩 HDR 光照贴图：
- **R/G/B**：颜色分量
- **M**：乘数（存储在高位）
- **范围**：`[0, 34.493242]`（线性空间）

### DLDR 编码

```hlsl
#ifdef UNITY_COLORSPACE_GAMMA
    #define LIGHTMAP_HDR_MULTIPLIER real(2.0)
#else
    #define LIGHTMAP_HDR_MULTIPLIER real(4.59) // 2.0 ^ 2.2
#endif
```

DLDR（Double LDR）编码：
- 使用双倍 LDR 范围存储 HDR 值
- 范围：`[0, 4.59]`（线性空间）

### Full HDR

```hlsl
#define LIGHTMAP_HDR_MULTIPLIER real(1.0)
#define LIGHTMAP_HDR_EXPONENT real(1.0)
```

完整 HDR 编码，直接存储线性值。

## 核心函数解析

### `DecodeLightmap(real4 encodedIlluminance, real4 decodeInstructions)`

- **签名**：`real3 DecodeLightmap(real4 encodedIlluminance, real4 decodeInstructions)`
- **功能**：解码光照贴图数据（根据编码格式）
- **实现原理**：
  ```hlsl
  real3 DecodeLightmap(real4 encodedIlluminance, real4 decodeInstructions)
  {
  #if defined(UNITY_LIGHTMAP_RGBM_ENCODING)
      return UnpackLightmapRGBM(encodedIlluminance, decodeInstructions);
  #elif defined(UNITY_LIGHTMAP_DLDR_ENCODING)
      return UnpackLightmapDoubleLDR(encodedIlluminance, decodeInstructions);
  #else // (UNITY_LIGHTMAP_FULL_HDR)
      return encodedIlluminance.rgb;
  #endif
  }
  ```
- **参数说明**：
  - `encodedIlluminance`：编码的光照贴图颜色
  - `decodeInstructions`：解码指令（乘数、指数等）
- **数学原理**：
  - RGBM：`result = rgb * (M^exponent * multiplier)`
  - DLDR：`result = rgb * multiplier`
  - Full HDR：直接返回 RGB

### `UnpackLightmapRGBM(real4 rgbmInput, real4 decodeInstructions)`

- **签名**：`real3 UnpackLightmapRGBM(real4 rgbmInput, real4 decodeInstructions)`
- **功能**：解包 RGBM 编码的光照贴图
- **实现原理**：
  ```hlsl
  real3 UnpackLightmapRGBM(real4 rgbmInput, real4 decodeInstructions)
  {
  #ifdef UNITY_COLORSPACE_GAMMA
      return rgbmInput.rgb * (rgbmInput.a * decodeInstructions.x);
  #else
      return rgbmInput.rgb * (PositivePow(rgbmInput.a, decodeInstructions.y) * decodeInstructions.x);
  #endif
  }
  ```
- **数学原理**：
  - Gamma 空间：`result = rgb * (M * multiplier)`
  - 线性空间：`result = rgb * (M^exponent * multiplier)`
  - `decodeInstructions.x`：乘数
  - `decodeInstructions.y`：指数（通常为 2.2）

### `SampleProbeVolumeSH4(...)`

- **签名**：`void SampleProbeVolumeSH4(TEXTURE3D_PARAM(...), float3 positionWS, float3 normalWS, float3 backNormalWS, ...)`
- **功能**：从 3D 探针体积采样 L0+L1 球谐函数（4 个系数）
- **实现原理**：
  ```hlsl
  void SampleProbeVolumeSH4(...)
  {
      float3 position = (transformToLocal == 1.0) ? mul(WorldToTexture, float4(positionWS, 1.0)).xyz : positionWS;
      float3 texCoord = (position - probeVolumeMin) * probeVolumeSizeInv.xyz;
      
      // 每个分量存储在同一个 3D 纹理中，使用 x 轴的 1/4
      texCoord.x = clamp(texCoord.x * 0.25, 0.5 * texelSizeX, 0.25 - 0.5 * texelSizeX);
      
      float4 shAr = SAMPLE_TEXTURE3D_LOD(SHVolumeTexture, SHVolumeSampler, texCoord, 0);
      texCoord.x += 0.25;
      float4 shAg = SAMPLE_TEXTURE3D_LOD(SHVolumeTexture, SHVolumeSampler, texCoord, 0);
      texCoord.x += 0.25;
      float4 shAb = SAMPLE_TEXTURE3D_LOD(SHVolumeTexture, SHVolumeSampler, texCoord, 0);
      
      bakeDiffuseLighting += SHEvalLinearL0L1(normalWS, shAr, shAg, shAb);
      backBakeDiffuseLighting += SHEvalLinearL0L1(backNormalWS, shAr, shAg, shAb);
  }
  ```
- **数学原理**：
  - L0+L1 球谐函数需要 4 个系数（每个颜色通道）
  - RGB 分量存储在 3D 纹理的不同 x 坐标位置（0, 0.25, 0.5）
  - 使用双线性插值在探针体积中采样
- **性能注意事项**：
  - 需要 3 次纹理采样（RGB）
  - 使用 LOD 0 避免额外开销

### `SampleProbeVolumeSH9(...)`

- **签名**：`void SampleProbeVolumeSH9(TEXTURE3D_PARAM(...), ...)`
- **功能**：从 3D 探针体积采样 L0+L1+L2 球谐函数（9 个系数）
- **实现原理**：
  ```hlsl
  void SampleProbeVolumeSH9(...)
  {
      float3 position = (transformToLocal == 1.0f) ? mul(WorldToTexture, float4(positionWS, 1.0)).xyz : positionWS;
      float3 texCoord = (position - probeVolumeMin) * probeVolumeSizeInv;
      
      const uint shCoeffCount = 7; // 9 个系数打包成 7 个
      const float invShCoeffCount = 1.0f / float(shCoeffCount);
      
      texCoord.x = texCoord.x / shCoeffCount;
      float texCoordX = clamp(texCoord.x, 0.5f * texelSizeX, invShCoeffCount - 0.5f * texelSizeX);
      
      float4 SHCoefficients[7];
      for (uint i = 0; i < shCoeffCount; i++)
      {
          texCoord.x = texCoordX + i * invShCoeffCount;
          SHCoefficients[i] = SAMPLE_TEXTURE3D_LOD(SHVolumeTexture, SHVolumeSampler, texCoord, 0);
      }
      
      bakeDiffuseLighting += SampleSH9(SHCoefficients, normalize(normalWS));
      backBakeDiffuseLighting += SampleSH9(SHCoefficients, normalize(backNormalWS));
  }
  ```
- **数学原理**：
  - L0+L1+L2 球谐函数需要 9 个系数
  - 9 个系数打包成 7 个纹理元素（使用 alpha 通道存储额外系数）
  - 需要 7 次纹理采样
- **性能注意事项**：
  - 比 SH4 更昂贵（7 次采样 vs 3 次）
  - 但提供更高质量的环境光照

### `SampleProbeOcclusion(...)`

- **签名**：`float4 SampleProbeOcclusion(TEXTURE3D_PARAM(...), float3 positionWS, ...)`
- **功能**：从探针体积采样遮挡信息
- **实现原理**：
  ```hlsl
  float4 SampleProbeOcclusion(...)
  {
      float3 position = (transformToLocal == 1.0) ? mul(WorldToTexture, float4(positionWS, 1.0)).xyz : positionWS;
      float3 texCoord = (position - probeVolumeMin) * probeVolumeSizeInv.xyz;
      
      // 采样第四个纹理（遮挡信息）
      texCoord.x = max(texCoord.x * 0.25 + 0.75, 0.75 + 0.5 * texelSizeX);
      
      return SAMPLE_TEXTURE3D(SHVolumeTexture, SHVolumeSampler, texCoord);
  }
  ```
- **数学原理**：
  - 遮挡信息存储在 3D 纹理的 x = 0.75 位置
  - 与 SH 系数共享同一纹理以优化内存

### `PackEmissiveRGBM(real3 rgb)`

- **签名**：`real4 PackEmissiveRGBM(real3 rgb)`
- **功能**：将自发光颜色打包为 RGBM 格式
- **实现原理**：
  ```hlsl
  real4 PackEmissiveRGBM(real3 rgb)
  {
      real kOneOverRGBMMaxRange = 1.0 / EMISSIVE_RGBM_SCALE; // 97.0
      const real kMinMultiplier = 2.0 * 1e-2;
      
      real4 rgbm = real4(rgb * kOneOverRGBMMaxRange, 1.0);
      rgbm.a = max(max(rgbm.r, rgbm.g), max(rgbm.b, kMinMultiplier));
      rgbm.a = ceil(rgbm.a * 255.0) / 255.0;
      rgbm.a = max(rgbm.a, kMinMultiplier);
      
      rgbm.rgb /= rgbm.a;
      return rgbm;
  }
  ```
- **数学原理**：
  - 计算最大颜色分量作为乘数 `M`
  - 将颜色除以 `M` 归一化到 `[0, 1]`
  - `M` 量化到 8 位（`ceil(M * 255) / 255`）

### `DecodeHDREnvironment(real4 encodedIrradiance, real4 decodeInstructions)`

- **签名**：`real3 DecodeHDREnvironment(real4 encodedIrradiance, real4 decodeInstructions)`
- **功能**：解码 HDR 环境贴图（环境光照、反射探针等）
- **实现原理**：
  ```hlsl
  real3 DecodeHDREnvironment(real4 encodedIrradiance, real4 decodeInstructions)
  {
      real alpha = max(decodeInstructions.w * (encodedIrradiance.a - 1.0) + 1.0, 0.0);
      
      #ifdef UNITY_USE_NATIVE_HDR
          return encodedIrradiance.rgb;
      #else
          return (decodeInstructions.x * PositivePow(alpha, decodeInstructions.y)) * encodedIrradiance.rgb;
      #endif
  }
  ```
- **数学原理**：
  - 如果 alpha 影响 RGB：`result = multiplier * alpha^exponent * rgb`
  - 否则：`result = multiplier * rgb`
  - `decodeInstructions.w`：控制是否使用 alpha

## 纹理布局

### 探针体积纹理布局

3D 纹理的 x 轴布局：
- `[0, 0.25)`：SH R 系数
- `[0.25, 0.5)`：SH G 系数
- `[0.5, 0.75)`：SH B 系数
- `[0.75, 1.0)`：遮挡信息（Occ）

## 与其他模块的关系

- [[Common]]：依赖基础类型和数学函数
- [[Color]]：使用颜色空间转换
- [[SphericalHarmonics]]：使用球谐函数评估

## 参考资料

- Unity 光照贴图文档：https://docs.unity3d.com/Manual/Lightmapping.html
- 球谐函数：https://en.wikipedia.org/wiki/Spherical_harmonics
- RGBM 编码：https://marmoset.co/posts/rgbm-color-encoding/

