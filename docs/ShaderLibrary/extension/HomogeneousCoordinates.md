---
tags: [shader, 齐次坐标, 矩阵, 图形学, 线性代数, 坐标变换]
aliases: [HomogeneousCoordinates, 齐次坐标, 齐次坐标系]
created: 2025-01-20
related:
  - "[[VectorOperations]]"
  - "[[MathBasics]]"
  - "[[Common]]"
  - "[[SpaceTransforms]]"
  - "[[TransformationMatrices]]"
---

# 齐次坐标 (Homogeneous Coordinates)

## 概述

齐次坐标是计算机图形学中的基础概念，它使用 4D 向量 `(x, y, z, w)` 来表示 3D 空间中的点和方向。这种表示方法使得平移、旋转、缩放和透视投影等变换都可以通过统一的矩阵乘法来实现。

**相关主题**：
- [[VectorOperations]] - 向量和矩阵运算基础
- [[MathBasics]] - GPU 数学运算基础
- [[Common]] - Unity 核心函数库，包含透视投影相关内容
- [[SpaceTransforms]] - Unity 空间变换实现

## 1. 引入背景

### 1.1 问题：为什么需要齐次坐标？

在传统的 3D 坐标系统中，我们使用 `(x, y, z)` 来表示一个点。但是，当我们尝试用矩阵来表示所有变换时，遇到了问题：

**旋转和缩放**可以用 3×3 矩阵表示：
```
[x']   [m11  m12  m13] [x]
[y'] = [m21  m22  m23] [y]
[z']   [m31  m32  m33] [z]
```

**平移**却需要加法：
```
[x']   [x]   [tx]
[y'] = [y] + [ty]
[z']   [z]   [tz]
```

这意味着旋转和缩放可以用矩阵乘法统一处理，但平移需要单独处理，无法统一到矩阵运算中。

### 1.2 解决方案：引入第四维

齐次坐标通过引入第四维 `w`，将 3D 点表示为 `(x, y, z, w)`，使得**平移、旋转、缩放都可以用 4×4 矩阵统一表示**。

## 2. 数学含义

### 2.1 定义

齐次坐标使用 4D 向量表示 3D 空间中的几何对象：

- **位置（点）**：`(x, y, z, 1)` 或 `(x, y, z, w)`，其中 `w ≠ 0`
- **方向（向量）**：`(x, y, z, 0)`

### 2.2 从齐次坐标到笛卡尔坐标

齐次坐标 `(x, y, z, w)` 对应的 3D 笛卡尔坐标是：
```
3D点 = (x/w, y/w, z/w)
```

**关键理解**：
- 当 `w = 1` 时，齐次坐标 `(x, y, z, 1)` 直接对应 3D 点 `(x, y, z)`
- 当 `w ≠ 1` 时，需要除以 `w` 才能得到真正的 3D 坐标
- 齐次坐标 `(x, y, z, w)` 和 `(kx, ky, kz, kw)`（k ≠ 0）表示同一个 3D 点

### 2.3 为什么方向向量的 w = 0？

方向向量表示"方向"而不是"位置"，它不应该受平移影响。

在齐次坐标中：
- 点 `(x, y, z, 1)`：平移矩阵会改变其位置
- 方向 `(x, y, z, 0)`：平移矩阵不会改变它（因为 `w = 0` 使得平移项被抵消）

```hlsl
// 平移矩阵
float4x4 translation = float4x4(
    float4(1, 0, 0, tx),
    float4(0, 1, 0, ty),
    float4(0, 0, 1, tz),
    float4(0, 0, 0, 1)
);

// 点 (x, y, z, 1) 经过平移后
// x' = x + tx, y' = y + ty, z' = z + tz, w' = 1
// 结果：(x+tx, y+ty, z+tz, 1) - 位置改变了

// 方向 (x, y, z, 0) 经过平移后
// x' = x, y' = y, z' = z, w' = 0
// 结果：(x, y, z, 0) - 方向不变
```

## 3. 图形学中的应用

### 3.1 统一的变换矩阵

使用齐次坐标后，所有变换都可以用 4×4 矩阵表示：

#### 平移矩阵
```hlsl
float4x4 Translation(float3 t) {
    return float4x4(
        float4(1, 0, 0, t.x),
        float4(0, 1, 0, t.y),
        float4(0, 0, 1, t.z),
        float4(0, 0, 0, 1)
    );
}
```

#### 旋转矩阵（绕 Z 轴）
```hlsl
float4x4 RotationZ(float angle) {
    float c = cos(angle);
    float s = sin(angle);
    return float4x4(
        float4(c, -s, 0, 0),
        float4(s,  c, 0, 0),
        float4(0,  0, 1, 0),
        float4(0,  0, 0, 1)
    );
}
```

#### 缩放矩阵
```hlsl
float4x4 Scale(float3 s) {
    return float4x4(
        float4(s.x, 0,   0,   0),
        float4(0,   s.y, 0,   0),
        float4(0,   0,   s.z, 0),
        float4(0,   0,   0,   1)
    );
}
```

#### 组合变换（TRS）
```hlsl
// 组合：先缩放，再旋转，最后平移
float4x4 TRS = mul(Translation(t), mul(Rotation(r), Scale(s)));

// 应用到点
float4 posOS = float4(positionOS, 1.0);
float4 posWS = mul(posOS, TRS);
float3 positionWS = posWS.xyz;  // w = 1，直接取 xyz
```

### 3.2 透视投影

齐次坐标最重要的应用之一是**透视投影**。透视投影矩阵会产生 `w ≠ 1` 的齐次坐标，通过透视除法（除以 w）实现透视效果。

#### 透视投影矩阵（简化形式）
```hlsl
// 透视投影矩阵的第 4 行是 [0, 0, -1/d, 0]
// 这会产生 w_clip = -z_view / d
float4x4 PerspectiveProjection(float d) {
    return float4x4(
        float4(1, 0, 0, 0),
        float4(0, 1, 0, 0),
        float4(0, 0, 1, 0),
        float4(0, 0, -1/d, 0)
    );
}
```

#### 透视除法
```hlsl
// 顶点着色器输出齐次坐标
float4 positionCS = mul(positionVS, projectionMatrix);
// positionCS = (x_clip, y_clip, z_clip, w_clip)

// 透视除法（通常在 GPU 硬件中自动完成）
float3 positionNDC = positionCS.xyz / positionCS.w;
// 结果：(x_clip/w_clip, y_clip/w_clip, z_clip/w_clip)
```

**为什么需要透视除法？**
- 透视投影矩阵会产生 `w_clip = -z_view / d`
- 透视除法 `x_clip / w_clip` 等价于 `x_clip * (-d / z_view)`
- 这实现了透视效果：远处的物体看起来更小（因为除以更大的 w）

### 3.3 Unity 中的实际应用

#### 位置变换
```hlsl
// 对象空间 → 世界空间
float4 posOS = float4(positionOS, 1.0);  // w = 1 表示点
float4 posWS = mul(posOS, ObjectToWorldMatrix);
float3 positionWS = posWS.xyz;  // 如果 w = 1，直接取 xyz
```

#### 方向变换
```hlsl
// 方向向量只使用旋转部分（3×3 矩阵）
float3 dirOS = ...;
float3 dirWS = mul((float3x3)ObjectToWorldMatrix, dirOS);
// 或者使用齐次坐标，w = 0
float4 dirOS_h = float4(dirOS, 0.0);
float4 dirWS_h = mul(dirOS_h, ObjectToWorldMatrix);
float3 dirWS = dirWS_h.xyz;  // w = 0，平移不影响方向
```

#### ClipSpace 中的齐次坐标
```hlsl
// Unity 的顶点着色器输出
struct VertexOutput {
    float4 positionCS : SV_POSITION;  // 齐次坐标 (x, y, z, w)
    // ...
};

// 在片段着色器中，如果需要屏幕坐标
float2 screenUV = positionCS.xy / positionCS.w;  // 透视除法
```

### 3.4 齐次坐标的优势总结

1. **统一性**：平移、旋转、缩放都可以用矩阵乘法表示
2. **组合性**：多个变换可以通过矩阵乘法组合成一个矩阵
3. **透视投影**：自然支持透视投影，w 分量存储深度信息
4. **硬件优化**：GPU 硬件专门优化了齐次坐标运算
5. **数学优雅**：在数学上更加统一和优雅

## 4. 常见误区

### 4.1 误区：w 总是等于 1

**错误理解**：齐次坐标的 w 分量总是 1。

**正确理解**：
- 在模型空间、世界空间、视图空间中，通常 `w = 1`
- 在 ClipSpace 中，`w ≠ 1`，它存储了深度信息（`w = -z_view`）
- 透视除法后，坐标又回到 `w = 1` 的状态

### 4.2 误区：可以直接使用 xyz 分量

**错误理解**：齐次坐标 `(x, y, z, w)` 可以直接当作 3D 点 `(x, y, z)` 使用。

**正确理解**：
- 只有当 `w = 1` 时，才能直接使用 `xyz`
- 当 `w ≠ 1` 时，必须先进行透视除法：`(x/w, y/w, z/w)`
- Unity 的 ClipSpace 输出需要透视除法才能得到 NDC 坐标

### 4.3 误区：方向向量也需要 w = 1

**错误理解**：方向向量也应该用 `(x, y, z, 1)` 表示。

**正确理解**：
- 方向向量应该用 `(x, y, z, 0)` 表示
- `w = 0` 确保方向不受平移影响
- 如果错误地使用 `w = 1`，方向会在平移后改变

## 5. 总结

齐次坐标是计算机图形学的基石，它通过引入第四维 `w`，实现了：

1. **统一的变换表示**：所有变换都可以用矩阵乘法表示
2. **自然的透视投影**：w 分量存储深度，透视除法实现透视效果
3. **硬件优化**：GPU 专门优化了齐次坐标运算

**关键要点**：
- 点用 `(x, y, z, 1)` 表示
- 方向用 `(x, y, z, 0)` 表示
- ClipSpace 中的坐标需要透视除法：`(x/w, y/w, z/w)`
- Unity 中大多数情况下 `w = 1`，可以直接使用 `xyz` 分量

**进一步学习**：
- 关于透视投影矩阵的详细推导，参见 [[TransformationMatrices]]
- 关于空间变换的实现，参见 [[SpaceTransforms]]
- 关于向量和矩阵运算，参见 [[VectorOperations]]

## 6. 参考文献与延伸阅读

### 6.1 经典教材

1. **《Real-Time Rendering (4th Edition)》** - Tomas Akenine-Möller, Eric Haines, Naty Hoffman
   - 第 4 章 "Transforms" 详细介绍了齐次坐标和变换矩阵
   - 第 4.1.4 节专门讨论齐次坐标（Homogeneous Coordinates）
   - 第 4.7 节介绍透视投影矩阵的推导

2. **《Computer Graphics: Principles and Practice (3rd Edition)》** - John F. Hughes, et al.
   - 第 6 章 "Transformations" 深入讲解齐次坐标系统
   - 详细解释了为什么需要齐次坐标以及其在图形管线中的作用

3. **《3D Math Primer for Graphics and Game Development (2nd Edition)》** - Fletcher Dunn, Ian Parberry
   - 第 7 章 "Introduction to Matrices" 介绍矩阵基础
   - 第 8 章 "Matrices and Linear Transformations" 讲解齐次坐标
   - 第 9 章 "More on Matrices" 深入讨论变换组合

4. **《Mathematics for 3D Game Programming and Computer Graphics (3rd Edition)》** - Eric Lengyel
   - 第 3 章 "Transforms" 详细推导齐次坐标和变换矩阵
   - 第 4 章 "3D Engine Geometry" 讨论透视投影

### 6.2 在线资源

1. **LearnOpenGL - Transformations**
   - [https://learnopengl.com/Getting-started/Transformations](https://learnopengl.com/Getting-started/Transformations)
   - 提供了齐次坐标和变换矩阵的直观解释和代码示例

2. **Scratchapixel - The Perspective and Orthographic Projection Matrix**
   - [https://www.scratchapixel.com/lessons/3d-basic-rendering/perspective-and-orthographic-projection-matrix/building-basic-perspective-projection-matrix.html](https://www.scratchapixel.com/lessons/3d-basic-rendering/perspective-and-orthographic-projection-matrix/building-basic-perspective-projection-matrix.html)
   - 详细推导透视投影矩阵，包含齐次坐标的数学原理

3. **Song Ho Ahn - OpenGL Projection Matrix**
   - [http://www.songho.ca/opengl/gl_projectionmatrix.html](http://www.songho.ca/opengl/gl_projectionmatrix.html)
   - 提供了透视投影和正交投影矩阵的详细推导

4. **Unity Documentation - Shader Semantics**
   - [https://docs.unity3d.com/Manual/SL-ShaderSemantics.html](https://docs.unity3d.com/Manual/SL-ShaderSemantics.html)
   - Unity 官方文档，说明 `SV_POSITION` 等语义在齐次坐标中的作用

### 6.3 学术论文

1. **"Homogeneous Coordinates"** - 齐次坐标的数学基础
   - 齐次坐标最初由 August Ferdinand Möbius 在 1827 年引入
   - 在射影几何中有深厚的数学基础

2. **"Perspective Projection"** - 透视投影的数学原理
   - 透视投影矩阵的推导在计算机图形学中是一个经典问题
   - 相关论文通常讨论不同坐标系下的投影矩阵形式

### 6.4 GPU 编程相关

1. **Microsoft DirectX Graphics Documentation**
   - DirectX 文档中详细说明了齐次坐标在 GPU 管线中的处理
   - 特别是 Clip Space 和 NDC Space 的转换过程

2. **OpenGL Specification**
   - OpenGL 规范中定义了齐次坐标在图形管线中的标准处理流程
   - 包括透视除法和裁剪空间的规范

3. **Vulkan Specification**
   - Vulkan API 规范中详细说明了齐次坐标的处理
   - 特别是 Clip Space 到 NDC Space 的转换规则

### 6.5 历史背景

齐次坐标的概念可以追溯到：
- **1827 年**：August Ferdinand Möbius 引入齐次坐标用于射影几何
- **计算机图形学**：20 世纪 60-70 年代，齐次坐标被引入计算机图形学
- **现代 GPU**：硬件直接支持齐次坐标运算，包括透视除法等操作

### 6.6 推荐阅读顺序

对于初学者：
1. 先阅读本文档了解基本概念
2. 阅读《3D Math Primer》第 7-9 章建立数学基础
3. 参考 LearnOpenGL 的教程进行实践

对于进阶学习：
1. 阅读《Real-Time Rendering》第 4 章深入理解
2. 研究 Unity/OpenGL/DirectX 的官方文档了解实现细节
3. 阅读相关学术论文了解数学原理
