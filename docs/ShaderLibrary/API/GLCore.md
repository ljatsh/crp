# [[GLCore]] - OpenGL Core 平台定义

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/API/GLCore.hlsl`
- **主要职责**：定义 OpenGL Core 平台的特定宏和抽象
- **使用场景**：Linux、macOS（OpenGL）平台

## 核心定义

### 坐标系统

```hlsl
#define UNITY_NEAR_CLIP_VALUE (-1.0)  // OpenGL 使用 -1 到 1
#define UNITY_RAW_FAR_CLIP_VALUE (1.0)
```

### 语义定义

```hlsl
#define FRONT_FACE_SEMANTIC VFACE
#define FRONT_FACE_TYPE float
#define IS_FRONT_VFACE(VAL, FRONT, BACK) ((VAL > 0.0) ? (FRONT) : (BACK))
```

### 常量缓冲区

```hlsl
#define CBUFFER_START(name) cbuffer name {
#define CBUFFER_END };
```

## 平台特性

- 支持显式绑定
- 不支持某些 D3D11 特性（通过错误宏标记）

## 与其他模块的关系

- [[Common]]：被 Common.hlsl 自动包含
- [[Validate]]：平台验证

