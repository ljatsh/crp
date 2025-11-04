# [[Vulkan]] - Vulkan 平台定义

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/API/Vulkan.hlsl`
- **主要职责**：定义 Vulkan 平台的特定宏和抽象
- **使用场景**：Vulkan 平台（Android、Linux、Windows）

## 核心定义

### 坐标系统

```hlsl
#define UNITY_UV_STARTS_AT_TOP 1
#define UNITY_REVERSED_Z 1
```

### 显示方向预处理

```hlsl
#ifdef UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION
float4 ApplyPretransformRotation(float4 v)
{
    // 处理交换链预变换
}
#endif
```

### 纹理抽象

```hlsl
#define TEXTURE2D_FLOAT(textureName) Texture2D_float textureName
#define TEXTURE2D_HALF(textureName) Texture2D_half textureName
```

## 与其他模块的关系

- [[Common]]：被 Common.hlsl 自动包含
- [[Validate]]：平台验证

