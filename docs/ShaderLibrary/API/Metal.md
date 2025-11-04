# [[Metal]] - Metal 平台定义

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/API/Metal.hlsl`
- **主要职责**：定义 Metal 平台的特定宏和抽象
- **使用场景**：macOS、iOS 平台

## 核心定义

### 坐标系统

```hlsl
#define UNITY_UV_STARTS_AT_TOP 1
#define UNITY_REVERSED_Z 1
```

### 平台特性

```hlsl
#define PLATFORM_SUPPORTS_BUFFER_ATOMICS_IN_PIXEL_SHADER
```

## 与其他模块的关系

- [[Common]]：被 Common.hlsl 自动包含
- [[Validate]]：平台验证

