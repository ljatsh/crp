# [[Switch]] - Nintendo Switch 平台定义

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/API/Switch.hlsl`
- **主要职责**：定义 Nintendo Switch 平台的特定宏和抽象
- **使用场景**：Nintendo Switch 平台

## 核心定义

### 坐标系统

```hlsl
#define UNITY_UV_STARTS_AT_TOP 1
```

### 平台特性

```hlsl
#define PLATFORM_LANE_COUNT 32  // Wave 操作的通道数
```

## 与其他模块的关系

- [[Common]]：被 Common.hlsl 自动包含
- [[Validate]]：平台验证

