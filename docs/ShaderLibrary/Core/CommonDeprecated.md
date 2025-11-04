# [[CommonDeprecated]] - 废弃函数

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonDeprecated.hlsl`
- **主要职责**：包含已废弃的函数，用于保持向后兼容性
- **使用场景**：不应在新代码中使用，仅用于兼容旧版本着色器

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和函数
- **被依赖的文件**：无（由 [[Common]] 自动包含）

## 废弃函数

### `LODDitheringTransition(uint3 fadeMaskSeed, float ditherFactor)`

- **签名**：`void LODDitheringTransition(uint3 fadeMaskSeed, float ditherFactor)`
- **状态**：已废弃
- **替代函数**：使用 `LODDitheringTransition(uint2 fadeMaskSeed, float ditherFactor)`（在 [[Common]] 中）
- **原因**：`uint3` 版本的种子参数不必要，`uint2` 版本更高效

## 注意事项

- 本文件中的函数不应在新代码中使用
- 这些函数可能在未来的 Unity 版本中被移除
- 请使用 [[Common]] 中对应的新版本函数

