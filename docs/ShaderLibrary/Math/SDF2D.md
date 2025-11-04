# [[SDF2D]] - 2D 有符号距离场

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/SDF2D.hlsl`
- **主要职责**：提供 2D 有符号距离场（SDF）函数
- **使用场景**：UI 渲染、矢量图形、轮廓效果

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型
- **被依赖的文件**：使用 SDF 的着色器

## 核心功能

SDF 用于定义 2D 形状，通过距离场实现平滑的边缘和抗锯齿。

## 与其他模块的关系

- [[Common]]：依赖基础类型

