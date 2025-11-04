# [[SpaceFillingCurves]] - 空间填充曲线

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceFillingCurves.hlsl`
- **主要职责**：提供空间填充曲线函数（Morton 码等）
- **使用场景**：纹理缓存、数据结构优化

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型
- **被依赖的文件**：使用空间填充曲线的着色器

## 核心功能

- Morton 码（Z-order curve）：2D 和 3D
- 用于优化内存访问模式

## 与其他模块的关系

- [[Common]]：依赖基础类型

