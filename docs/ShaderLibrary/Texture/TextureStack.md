# [[TextureStack]] - 虚拟纹理堆栈

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl`
- **主要职责**：实现虚拟纹理堆栈系统，支持多层纹理合成
- **使用场景**：地形渲染、复杂材质系统

## 依赖关系

- **依赖的文件**：
  - [[Common]] - 基础类型
  - [[VirtualTexturing]] - 虚拟纹理查找
- **被依赖的文件**：使用虚拟纹理堆栈的着色器

## 核心结构体

### `StackInfo`

```hlsl
struct StackInfo
{
    GraniteLookupData lookupData;
    GraniteLODLookupData lookupDataLod;
    float4 resolveOutput;
};
```

### `VTProperty`

```hlsl
struct VTProperty
{
    GraniteConstantBuffers grCB;
    GraniteTranslationTexture translationTable;
    GraniteCacheTexture cacheLayer[4];
    int layerCount;
    int layerIndex[4];
};
```

## 核心函数

### `PrepareVT(VTProperty vtProperty, VtInputParameters vtParams)`

- **功能**：准备虚拟纹理查找
- **返回**：`StackInfo` 结构

### `SampleVTLayer(VTProperty vtProperty, VtInputParameters vtParams, StackInfo info, int layerIndex)`

- **功能**：采样虚拟纹理层
- **返回**：采样结果颜色

## 与其他模块的关系

- [[VirtualTexturing]]：依赖虚拟纹理查找
- [[GraniteShaderLibBase]]：依赖 Granite 基础库

