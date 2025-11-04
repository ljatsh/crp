# [[Version]] - 着色器库版本

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/Version.hlsl`
- **主要职责**：定义着色器库版本信息和版本比较宏
- **使用场景**：需要版本检查的着色器

## 核心定义

### 版本号

```hlsl
#define SHADER_LIBRARY_VERSION_MAJOR 13
#define SHADER_LIBRARY_VERSION_MINOR 1
```

当前版本：**13.1**

### 版本比较宏

#### `VERSION_GREATER_EQUAL(major, minor)`

- **功能**：检查版本是否大于等于指定版本
- **实现**：
  ```hlsl
  #define VERSION_GREATER_EQUAL(major, minor) \
      ((SHADER_LIBRARY_VERSION_MAJOR > major) || \
       ((SHADER_LIBRARY_VERSION_MAJOR == major) && \
        (SHADER_LIBRARY_VERSION_MINOR >= minor)))
  ```

#### `VERSION_LOWER(major, minor)`

- **功能**：检查版本是否低于指定版本

#### `VERSION_EQUAL(major, minor)`

- **功能**：检查版本是否等于指定版本

## 使用示例

```hlsl
#if VERSION_GREATER_EQUAL(13, 1)
    // 使用新功能
#else
    // 使用旧版本兼容代码
#endif
```

## 注意事项

- 旧的版本号系统已废弃
- 用户应使用 `UNITY_VERSION` 宏检查 Unity 版本
- 示例：`#if UNITY_VERSION >= 202120` 检查 Unity 2021.2+

