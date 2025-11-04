# [[VertexCubeSlicing]] - 顶点立方体切片

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/VertexCubeSlicing.hlsl`
- **主要职责**：提供立方体切片算法，用于体积渲染
- **使用场景**：体积渲染、体积光效果

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和数学函数
- **被依赖的文件**：[[VolumeRendering]] - 体积渲染使用

## 核心功能

实现盒子-平面相交算法，用于计算体积渲染中的立方体切片顶点位置。

## 与其他模块的关系

- [[Common]]：依赖基础类型
- [[VolumeRendering]]：体积渲染使用

