# [[Filtering]] - 过滤函数

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl`
- **主要职责**：提供纹理过滤函数（B-Spline、双三次、双二次等）
- **使用场景**：高质量纹理采样、图像处理

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型
- **被依赖的文件**：需要高质量过滤的着色器

## 核心功能

- B-Spline 过滤
- 双三次（Bicubic）过滤
- 双二次（Biquadratic）过滤

## 与其他模块的关系

- [[Texture]]：纹理采样使用过滤函数

