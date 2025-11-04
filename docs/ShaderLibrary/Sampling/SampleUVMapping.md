# [[SampleUVMapping]] - UV 映射抽象

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/SampleUVMapping.hlsl`
- **主要职责**：提供统一的 UV 映射抽象，支持 UVSet、平面映射和三平面映射
- **使用场景**：需要灵活 UV 映射的着色器

## 依赖关系

- **依赖的文件**：
  - [[Common]] - 基础类型
  - [[NormalSurfaceGradient]] - 法线表面梯度
  - [[SampleUVMappingInternal]] - 内部实现
- **被依赖的文件**：使用 UV 映射的着色器

## 核心结构体

### `UVMapping`

```hlsl
struct UVMapping
{
    int mappingType;      // UV_MAPPING_UVSET / PLANAR / TRIPLANAR
    float2 uv;           // 当前 UV 或平面 UV
    float2 uvZY;          // 三平面映射：ZY 平面
    float2 uvXZ;          // 三平面映射：XZ 平面
    float2 uvXY;          // 三平面映射：XY 平面
    float3 normalWS;      // 顶点法线
    float3 triplanarWeights; // 三平面权重
    
#ifdef SURFACE_GRADIENT
    float3 tangentWS;    // 切线（世界空间）
    float3 bitangentWS;  // 副切线（世界空间）
#endif
};
```

## 映射类型

- `UV_MAPPING_UVSET (0)`：标准 UV 映射
- `UV_MAPPING_PLANAR (1)`：平面映射
- `UV_MAPPING_TRIPLANAR (2)`：三平面映射

## 核心宏定义

### `SAMPLE_UVMAPPING_TEXTURE2D(textureName, samplerName, uvMapping)`

- **功能**：使用 UV 映射采样纹理
- **实现**：根据 `mappingType` 选择对应的采样方法

### `SAMPLE_UVMAPPING_NORMALMAP(textureName, samplerName, uvMapping, scale)`

- **功能**：使用 UV 映射采样法线贴图
- **实现**：支持 RG、AG、RGB 编码格式

## 实现原理

文件通过多次包含 `SampleUVMappingInternal.hlsl` 实现不同的采样变体：
- 常规采样：`SampleUVMapping`
- LOD 采样：`SampleUVMappingLod`
- Bias 采样：`SampleUVMappingBias`

## 与其他模块的关系

- [[Common]]：依赖基础类型
- [[CommonMaterial]]：使用三平面权重计算

## 参考资料

- Triplanar Mapping：http://http.developer.nvidia.com/GPUGems3/gpugems3_ch01.html

