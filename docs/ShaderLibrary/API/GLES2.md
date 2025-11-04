# [[GLES2]] - OpenGL ES 2.0 平台定义

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/API/GLES2.hlsl`
- **主要职责**：定义 OpenGL ES 2.0 平台的特定宏和抽象（功能受限）
- **使用场景**：旧移动设备

## 核心限制

- **无 `RWTexture2D`**：不支持计算着色器写入纹理
- **无 Gather 操作**：不支持纹理 Gather
- **无 LOD 支持**：LOD 采样使用 Bias 近似
- **无常量缓冲区**：使用全局 uniform

## 特殊处理

```hlsl
#define uint int  // GLES2 不支持 uint
#define rcp(x) 1.0 / (x)  // 倒数函数模拟
#define TEXTURE2D_ARRAY(textureName) samplerCUBE textureName  // 使用立方贴图模拟
```

## 与其他模块的关系

- [[Common]]：被 Common.hlsl 自动包含
- [[Validate]]：平台验证

