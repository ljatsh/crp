# [[PhysicalCamera]] - 物理相机

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/PhysicalCamera.hlsl`
- **主要职责**：提供物理相机参数计算（EV100、曝光、亮度适应等）
- **使用场景**：色调映射、自动曝光

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型
- **被依赖的文件**：相机相关的着色器

## 核心功能

- EV100（曝光值）计算
- 曝光转换
- 亮度适应

## 与其他模块的关系

- [[Common]]：依赖基础类型
- [[Color]]：使用颜色空间转换

