# [[VirtualTexturing]] - 虚拟纹理

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/VirtualTexturing.hlsl`
- **主要职责**：提供虚拟纹理的查找和采样函数
- **使用场景**：大世界渲染、高分辨率纹理系统

## 依赖关系

- **依赖的文件**：
  - [[Common]] - 基础类型
  - [[GraniteShaderLibBase]] - Granite 虚拟纹理基础库
- **被依赖的文件**：[[TextureStack]] - 虚拟纹理堆栈

## 核心函数

### `VirtualTexturingLookup(...)`

- **功能**：执行虚拟纹理查找
- **返回**：查找数据和解析结果

### `VirtualTexturingSample(...)`

- **功能**：采样虚拟纹理
- **返回**：采样结果颜色

## 与其他模块的关系

- [[GraniteShaderLibBase]]：依赖 Granite 基础库
- [[TextureStack]]：使用虚拟纹理查找

