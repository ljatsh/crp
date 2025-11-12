---
tags: [shader, 矩阵, 变换矩阵, 投影矩阵, 透视投影, 图形学, 数学推导]
aliases: [TransformationMatrices, 变换矩阵, 投影矩阵, 透视投影矩阵]
created: 2025-01-20
related:
  - "[[HomogeneousCoordinates]]"
  - "[[VectorOperations]]"
  - "[[Common]]"
  - "[[SpaceTransforms]]"
---

# 变换矩阵 (Transformation Matrices)

## 概述

变换矩阵是计算机图形学中的核心工具，用于实现各种空间变换，包括投影、视图变换等。本文档详细推导各种变换矩阵的构造过程，包括坐标系定义、参数设置和完整的数学推导步骤。

**相关主题**：
- [[HomogeneousCoordinates]] - 齐次坐标基础，理解变换矩阵的前提
- [[VectorOperations]] - 向量和矩阵运算基础
- [[Common]] - Unity 核心函数库，包含透视投影相关内容
- [[SpaceTransforms]] - Unity 空间变换实现

## 1. 透视投影矩阵 (Perspective Projection Matrix)

### 1.1 投影的目标

透视投影矩阵的目标是将视图空间（View Space）中的 3D 点 `(x_view, y_view, z_view)` 变换到裁剪空间（Clip Space）中的齐次坐标 `(x_clip, y_clip, z_clip, w_clip)`，使得：

1. **透视效果**：远处的物体看起来更小，近处的物体看起来更大
2. **视锥体映射**：将视锥体内的点映射到标准化的设备坐标（NDC）范围
3. **深度信息保留**：保留深度信息用于深度测试和裁剪

### 1.2 变换流程

```
视图空间 (View Space)
    ↓ [透视投影矩阵]
裁剪空间 (Clip Space) - 齐次坐标 (x, y, z, w)
    ↓ [透视除法：除以 w]
NDC 空间 (Normalized Device Coordinates) - (x/w, y/w, z/w)
    ↓ [视口变换]
屏幕空间 (Screen Space)
```

### 1.3 坐标系定义

#### 视图空间（View Space / Camera Space）

视图空间是相机坐标系，定义如下：

- **原点**：相机位置
- **Z 轴**：相机朝向（通常指向 -Z 方向）
- **X 轴**：相机右侧
- **Y 轴**：相机上方
- **坐标系**：右手坐标系

**重要约定**：
- 在视图空间中，相机看向 **-Z 方向**
- 近平面位于 `z = -n`（n > 0）
- 远平面位于 `z = -f`（f > n > 0）
- 视锥体内的点满足：`-f ≤ z_view ≤ -n`

#### 裁剪空间（Clip Space）

裁剪空间是齐次坐标空间：

- **坐标形式**：`(x_clip, y_clip, z_clip, w_clip)`
- **w 分量**：存储深度信息，用于透视除法
- **透视除法后**：`(x_clip/w_clip, y_clip/w_clip, z_clip/w_clip)` 应在 `[-1, 1]` 范围内（标准 NDC）

#### NDC 空间（Normalized Device Coordinates）

透视除法后的标准化坐标：

- **标准范围**：`[-1, 1]` × `[-1, 1]` × `[-1, 1]`（OpenGL）
- **Unity 范围**：`[0, 1]` × `[0, 1]` × `[0, 1]`（非标准）
- **用途**：独立于屏幕分辨率和设备

### 1.4 基本参数

#### 视锥体参数

透视投影由以下参数定义：

1. **FOV（Field of View）**：垂直视野角度（通常用 `fovY` 表示）
   - 单位：弧度或角度
   - 典型值：60° 或 π/3 弧度

2. **Aspect Ratio（宽高比）**：`aspect = width / height`
   - 屏幕宽度与高度的比值
   - 典型值：16:9 = 1.777...

3. **Near Plane（近裁剪面）**：`n > 0`
   - 距离相机的最近距离
   - 典型值：0.1 或 0.3

4. **Far Plane（远裁剪面）**：`f > n`
   - 距离相机的最远距离
   - 典型值：1000 或 10000

#### 视锥体几何

在视图空间中，视锥体是一个平截头体（Frustum）：

```
         Y
         ↑
         |
    /    |    \
   /     |     \
  /      |      \
 /       |       \
/        |        \
---------+---------→ X
         |
    Camera (z = 0)
         |
         ↓ -Z
```

**视锥体边界**（在近平面 z = -n 上）：
- 顶部：`y = n * tan(fovY / 2)`
- 底部：`y = -n * tan(fovY / 2)`
- 右侧：`x = n * tan(fovY / 2) * aspect`
- 左侧：`x = -n * tan(fovY / 2) * aspect`

**简化符号**：
- `t = n * tan(fovY / 2)`（top，顶部距离）
- `b = -t`（bottom，底部距离）
- `r = t * aspect`（right，右侧距离）
- `l = -r`（left，左侧距离）

### 1.5 透视投影矩阵推导

#### 推导思路

透视投影矩阵需要满足以下条件：

1. **X 和 Y 坐标**：将视锥体映射到 `[-1, 1]` × `[-1, 1]` 范围
2. **Z 坐标**：将深度范围 `[-f, -n]` 映射到 `[-1, 1]`（或 `[0, 1]`）
3. **W 分量**：设置为 `-z_view`，用于透视除法

#### X 坐标的推导

**步骤 1：相似三角形关系**

考虑视图空间中的一个点 `P = (x_view, y_view, z_view)`，投影到近平面 `z = -n`：

```
        P (x_view, y_view, z_view)
       /
      /
     /
    /
Camera (0, 0, 0)
    \
     \
      \
       \
        Q (x_proj, y_proj, -n)
```

从相似三角形得到：
```
x_proj / x_view = -n / z_view
```

因此：
```
x_proj = -n * x_view / z_view
```

**步骤 2：映射到 NDC 范围**

我们需要将 `x_proj` 从 `[-r, r]` 映射到 `[-1, 1]`：

```
NDC_x = (x_proj - l) / (r - l) * 2 - 1
```

由于 `l = -r`，所以 `r - l = 2r`：

```
NDC_x = (x_proj + r) / (2r) * 2 - 1
     = (x_proj + r) / r - 1
     = x_proj / r
```

代入 `x_proj = -n * x_view / z_view` 和 `r = n * tan(fovY/2) * aspect`：

```
NDC_x = (-n * x_view / z_view) / (n * tan(fovY/2) * aspect)
     = -x_view / (z_view * tan(fovY/2) * aspect)
```

**步骤 3：使用齐次坐标**

我们希望：
```
NDC_x = x_clip / w_clip = -x_view / (z_view * tan(fovY/2) * aspect)
```

设置：
- `x_clip = x_view / (tan(fovY/2) * aspect)`
- `w_clip = -z_view`

那么：
```
NDC_x = x_clip / w_clip = (x_view / (tan(fovY/2) * aspect)) / (-z_view)
     = -x_view / (z_view * tan(fovY/2) * aspect)
```

**矩阵第 1 行**：
```
[1/(tan(fovY/2) * aspect), 0, 0, 0]
```

#### Y 坐标的推导

类似地，Y 坐标的推导：

**步骤 1：投影到近平面**

```
y_proj = -n * y_view / z_view
```

**步骤 2：映射到 NDC**

```
NDC_y = y_proj / t = (-n * y_view / z_view) / (n * tan(fovY/2))
     = -y_view / (z_view * tan(fovY/2))
```

**步骤 3：使用齐次坐标**

设置：
- `y_clip = y_view / tan(fovY/2)`
- `w_clip = -z_view`

那么：
```
NDC_y = y_clip / w_clip = (y_view / tan(fovY/2)) / (-z_view)
     = -y_view / (z_view * tan(fovY/2))
```

**矩阵第 2 行**：
```
[0, 1/tan(fovY/2), 0, 0]
```

#### Z 坐标的推导

Z 坐标的推导更复杂，因为需要将深度范围 `[-f, -n]` 映射到 `[-1, 1]`。

**步骤 1：线性映射**

我们希望找到一个线性函数 `z_clip = A * z_view + B`，使得：
- 当 `z_view = -n` 时，`NDC_z = z_clip / w_clip = -1`
- 当 `z_view = -f` 时，`NDC_z = z_clip / w_clip = 1`

由于 `w_clip = -z_view`，所以：
- `NDC_z = z_clip / (-z_view)`

当 `z_view = -n` 时：
```
-1 = z_clip / (-(-n)) = z_clip / n
```
所以：`z_clip = -n`

当 `z_view = -f` 时：
```
1 = z_clip / (-(-f)) = z_clip / f
```
所以：`z_clip = f`

**步骤 2：求解线性函数**

我们需要 `z_clip = A * z_view + B`，满足：
- `z_clip(-n) = -n` → `A * (-n) + B = -n` → `-A*n + B = -n`
- `z_clip(-f) = f` → `A * (-f) + B = f` → `-A*f + B = f`

解方程组：
```
B = -n + A*n
B = f + A*f
```

因此：
```
-n + A*n = f + A*f
A*n - A*f = f + n
A*(n - f) = f + n
A = (f + n) / (n - f)
```

代入第一个方程：
```
B = -n + A*n = -n + n * (f + n) / (n - f)
  = -n * (n - f) / (n - f) + n * (f + n) / (n - f)
  = n * (-(n - f) + (f + n)) / (n - f)
  = n * (-n + f + f + n) / (n - f)
  = n * (2f) / (n - f)
  = 2fn / (n - f)
```

**矩阵第 3 行**：
```
[0, 0, (f+n)/(n-f), 2fn/(n-f)]
```

#### W 分量的设置

W 分量设置为 `-z_view`，用于透视除法。

**矩阵第 4 行**：
```
[0, 0, -1, 0]
```

#### 完整的透视投影矩阵

综合以上推导，透视投影矩阵为：

```
P = [ 1/(tan(fovY/2)*aspect)  0                   0                   0          ]
    [ 0                        1/tan(fovY/2)      0                   0          ]
    [ 0                        0                   (f+n)/(n-f)         2fn/(n-f)  ]
    [ 0                        0                   -1                  0          ]
```

**简化符号**：
- `cot_half_fov = 1 / tan(fovY/2)`
- `A = (f+n)/(n-f)`
- `B = 2fn/(n-f)`

矩阵可以写成：

```
P = [ cot_half_fov/aspect  0        0    0 ]
    [ 0                     cot_half_fov  0    0 ]
    [ 0                     0            A    B ]
    [ 0                     0            -1   0 ]
```

### 1.6 矩阵验证

#### 验证近平面

当 `z_view = -n` 时：
- `x_clip = x_view / (tan(fovY/2) * aspect)`
- `y_clip = y_view / tan(fovY/2)`
- `z_clip = (f+n)/(n-f) * (-n) + 2fn/(n-f) = -(f+n)n/(n-f) + 2fn/(n-f) = n(f-n)/(n-f) = -n`
- `w_clip = -(-n) = n`

透视除法后：
- `NDC_x = x_clip / n`
- `NDC_y = y_clip / n`
- `NDC_z = -n / n = -1` ✓

#### 验证远平面

当 `z_view = -f` 时：
- `z_clip = (f+n)/(n-f) * (-f) + 2fn/(n-f) = -(f+n)f/(n-f) + 2fn/(n-f) = f(n-f)/(n-f) = f`
- `w_clip = -(-f) = f`

透视除法后：
- `NDC_z = f / f = 1` ✓

#### 验证透视效果

考虑两个点：
- `P1 = (x, y, -n)`（近处）
- `P2 = (x, y, -2n)`（两倍距离）

对于 P1：
- `NDC_x = x / (n * tan(fovY/2) * aspect)`

对于 P2：
- `NDC_x = x / (2n * tan(fovY/2) * aspect) = (1/2) * NDC_x(P1)`

远处的点确实被缩小了一半，符合透视效果 ✓

### 1.7 常见变体

#### 使用 l, r, t, b 参数

如果直接使用 `l, r, t, b` 参数（而不是 FOV 和 aspect），矩阵为：

```
P = [ 2n/(r-l)    0            (r+l)/(r-l)     0          ]
    [ 0            2n/(t-b)     (t+b)/(t-b)     0          ]
    [ 0            0            (f+n)/(n-f)     2fn/(n-f)  ]
    [ 0            0            -1              0          ]
```

#### OpenGL vs DirectX

**OpenGL**（NDC z 范围 `[-1, 1]`）：
- 使用上述矩阵

**DirectX**（NDC z 范围 `[0, 1]`）：
- 需要调整 Z 行的映射
- 矩阵第 3 行变为：`[0, 0, f/(f-n), -fn/(f-n)]`
- 矩阵第 4 行变为：`[0, 0, 1, 0]`（w = z_view）

#### Reversed-Z

现代图形 API（D3D11, Metal, Vulkan）使用 Reversed-Z：
- 近平面映射到 `z = 1`
- 远平面映射到 `z = 0`
- 提供更好的深度精度

### 1.8 Unity 中的实现

Unity 使用右手坐标系，相机看向 -Z 方向。Unity 的投影矩阵考虑了：
- 平台差异（OpenGL vs DirectX）
- Reversed-Z 支持
- 平台特定的 NDC 范围

```hlsl
// Unity 的投影矩阵（简化）
// 注意：实际实现会处理平台差异
float4x4 projectionMatrix = ...;

// 使用
float4 positionVS = float4(positionViewSpace, 1.0);
float4 positionCS = mul(projectionMatrix, positionVS);
// positionCS.w 存储 -z_view，用于透视除法

// GPU 硬件自动执行透视除法
// 等价于：
float3 positionNDC = positionCS.xyz / positionCS.w;
```

## 2. LookAt 矩阵 (View Matrix)

> **待添加**：LookAt 矩阵的详细推导将在后续版本中添加。

LookAt 矩阵用于构建视图矩阵（View Matrix），将世界空间中的点变换到视图空间。

### 2.1 LookAt 矩阵的目标

LookAt 矩阵的目标是：
- 给定相机位置 `eye`
- 给定目标点 `target`
- 给定上方向 `up`
- 构建一个矩阵，将世界空间变换到视图空间

### 2.2 基本思路

1. 计算相机的三个轴向量（右、上、前）
2. 构建旋转矩阵
3. 应用平移变换

## 3. 总结

### 透视投影矩阵的关键点

1. **X, Y 坐标**：通过相似三角形投影到近平面，然后映射到 `[-1, 1]`
2. **Z 坐标**：线性映射深度范围到 NDC 范围
3. **W 分量**：设置为 `-z_view`，实现透视除法
4. **齐次坐标**：使用齐次坐标统一表示，通过透视除法得到最终结果

**关键公式**：
- `cot_half_fov = 1 / tan(fovY/2)`
- `x_clip = x_view * cot_half_fov / aspect`
- `y_clip = y_view * cot_half_fov`
- `z_clip = (f+n)/(n-f) * z_view + 2fn/(n-f)`
- `w_clip = -z_view`

**进一步学习**：
- 关于齐次坐标的基础，参见 [[HomogeneousCoordinates]]
- 关于 Unity 中的实际应用，参见 [[Common#透视投影矩阵的数学原理]]
- 关于空间变换，参见 [[SpaceTransforms]]

