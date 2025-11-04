# [[PerPixelDisplacement]] - 每像素位移

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/PerPixelDisplacement.hlsl`
- **主要职责**：提供每像素位移函数，用于视差映射的高级版本
- **使用场景**：需要高质量视差效果的着色器

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和数学函数
- **被依赖的文件**：使用每像素位移的着色器

## 核心功能

每像素位移（Per-Pixel Displacement）是视差映射的改进版本，通过迭代采样高度图来找到正确的 UV 偏移。

## 与视差映射的区别

- **视差映射**：单步计算，快速但可能产生走样
- **每像素位移**：迭代搜索，更准确但更昂贵

## 与其他模块的关系

- [[Common]]：依赖基础类型
- [[ParallaxMapping]]：相关技术

