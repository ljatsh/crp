# [[Validate]] - 平台验证宏

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/API/Validate.hlsl`
- **主要职责**：提供平台头文件验证和默认定义
- **使用场景**：确保平台 API 正确设置

## 核心功能

### 流控制属性默认定义

```hlsl
#ifndef UNITY_BRANCH
#define UNITY_BRANCH
#endif

#ifndef UNITY_FLATTEN
#define UNITY_FLATTEN
#endif

#ifndef UNITY_UNROLL
#define UNITY_UNROLL
#endif
```

### 验证宏（已注释）

文件包含注释掉的 `REQUIRE_DEFINED` 宏，用于验证平台头文件是否正确定义了必需的宏。

## 与其他模块的关系

- [[Common]]：被 Common.hlsl 自动包含在所有平台 API 之后
- 所有平台 API 文件：验证其定义

