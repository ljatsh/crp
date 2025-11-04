# [[GLES3]] - OpenGL ES 3.0 平台定义

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/API/GLES3.hlsl`
- **主要职责**：定义 OpenGL ES 3.0 平台的特定宏和抽象
- **使用场景**：现代移动设备

## 核心定义

### 平台特性

```hlsl
#define GLES3_1_AEP 1  // 支持 GLES 3.1 AEP
#define PLATFORM_SUPPORT_GATHER  // 支持 Gather 操作
```

### 纹理抽象

```hlsl
#define TEXTURE2D(textureName) Texture2D textureName
#define TEXTURE2D_ARRAY(textureName) Texture2DArray textureName
```

## 与 GLES2 的区别

- 支持常量缓冲区
- 支持纹理数组
- 支持更多现代特性

## 与其他模块的关系

- [[Common]]：被 Common.hlsl 自动包含
- [[Validate]]：平台验证

