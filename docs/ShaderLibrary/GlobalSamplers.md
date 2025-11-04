# [[GlobalSamplers]] - 全局采样器

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl`
- **主要职责**：定义全局共享的采样器状态
- **使用场景**：所有需要纹理采样的着色器

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和宏定义
- **被依赖的文件**：几乎所有着色器

## 核心功能

定义常用的采样器状态（线性、点采样、重复、夹紧等），避免重复定义。

## 与其他模块的关系

- [[Common]]：依赖基础类型
- [[Texture]]：使用采样器状态

