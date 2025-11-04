# [[Refraction]] - 折射函数

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/Refraction.hlsl`
- **主要职责**：提供折射计算函数
- **使用场景**：玻璃、水等透明材质

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型
- **被依赖的文件**：需要折射效果的着色器

## 核心功能

计算光线通过介质界面的折射方向，考虑全内反射（TIR）。

## 与其他模块的关系

- [[Common]]：依赖基础类型
- [[BSDF]]：使用折射计算

