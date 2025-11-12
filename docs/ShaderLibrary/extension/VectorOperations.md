---
tags: [shader, 向量运算, 矩阵, GPU, 线性代数]
aliases: [VectorOperations, 向量运算, 矩阵运算]
created: 2025-01-20
related:
  - "[[Float]]"
  - "[[MathBasics]]"
  - "[[Common]]"
  - "[[SpaceTransforms]]"
---

# 向量运算 (Vector Operations)

## 概述

向量和矩阵运算是 GPU 着色器编程的核心基础。本文档详细介绍向量运算、矩阵运算及其在着色器中的应用，为理解空间变换、光照计算等高级主题提供理论基础。

**相关主题**：
- [[Float]] - 浮点数格式和精度
- [[MathBasics]] - GPU 数学运算基础
- [[Common]] - Unity 核心函数库
- [[SpaceTransforms]] - Unity 空间变换实现

## 1. 向量基础 (Vector Fundamentals)

### 1.1 向量类型

HLSL 支持多种向量类型：

```hlsl
float2 vec2;   // 2D 向量（常用于 UV 坐标、屏幕坐标）
float3 vec3;   // 3D 向量（常用于位置、方向、颜色 RGB）
float4 vec4;   // 4D 向量（常用于齐次坐标、颜色 RGBA）
```

### 1.2 向量构造

```hlsl
float3 pos = float3(1.0, 2.0, 3.0);
float3 pos2 = float3(1.0);  // (1.0, 1.0, 1.0)
float4 color = float4(1.0, 0.5, 0.0, 1.0);  // RGBA
```

### 1.3 分量访问

```hlsl
float3 v = float3(1.0, 2.0, 3.0);

// 使用 .xyzw 或 .rgba 访问分量
float x = v.x;      // 1.0
float y = v.y;      // 2.0
float z = v.z;      // 3.0

// Swizzling（分量重排）
float2 xy = v.xy;           // (1.0, 2.0)
float3 zyx = v.zyx;         // (3.0, 2.0, 1.0)
float4 xyz1 = float4(v.xyz, 1.0);  // (1.0, 2.0, 3.0, 1.0)
```

### 1.4 向量基本运算

```hlsl
float3 a = float3(1.0, 2.0, 3.0);
float3 b = float3(4.0, 5.0, 6.0);
float s = 2.0;

float3 sum = a + b;        // (5.0, 7.0, 9.0) - 向量加法
float3 diff = a - b;       // (-3.0, -3.0, -3.0) - 向量减法
float3 scaled = s * a;     // (2.0, 4.0, 6.0) - 标量乘法
float3 scaled2 = a * s;    // 同上
float3 component = a * b;   // (4.0, 10.0, 18.0) - 逐分量相乘
```

## 2. 点积（Dot Product）

### 2.1 定义

两个向量的点积定义为：

$$\vec{a} \cdot \vec{b} = a_x b_x + a_y b_y + a_z b_z = |\vec{a}||\vec{b}|\cos\theta$$

其中 $\theta$ 是两个向量的夹角。

### 2.2 HLSL 函数

```hlsl
float dot(float3 a, float3 b);
float dot(float4 a, float4 b);
```

### 2.3 几何意义

1. **夹角余弦值**（归一化向量）：
   ```hlsl
   float cosAngle = dot(normalize(a), normalize(b));
   float angle = acos(cosAngle);
   ```

2. **投影长度**：
   ```hlsl
   // 向量 a 在单位向量 n 方向上的投影长度
   float projLength = dot(a, normalize(n));
   ```

3. **向量长度平方**：
   ```hlsl
   float lengthSq = dot(v, v);  // 比 length(v) 更快（避免开方）
   ```

### 2.4 应用场景

#### 光照计算

```hlsl
float3 N = normalize(normal);      // 法线
float3 L = normalize(lightDir);    // 光线方向
float NdotL = dot(N, L);           // 法线与光线夹角
float diffuse = max(0.0, NdotL);   // 漫反射系数
```

#### 角度判断

```hlsl
float3 V = normalize(viewDir);     // 视图方向
float NdotV = dot(N, V);
bool facingCamera = NdotV > 0.0;   // 是否面向相机
```

#### 点到平面距离

```hlsl
// 平面方程：ax + by + cz + d = 0
// 平面法线：N = normalize(a, b, c)
// 点到平面距离：distance = dot(point, N) + d
float distance = dot(point, planeNormal) + planeD;
```

## 3. 叉积（Cross Product）

### 3.1 定义

两个 3D 向量的叉积定义为：

$$\vec{a} \times \vec{b} = \begin{vmatrix} \vec{i} & \vec{j} & \vec{k} \\ a_x & a_y & a_z \\ b_x & b_y & b_z \end{vmatrix} = (a_y b_z - a_z b_y, a_z b_x - a_x b_z, a_x b_y - a_y b_x)$$

### 3.2 HLSL 函数

```hlsl
float3 cross(float3 a, float3 b);  // 仅支持 3D 向量
```

### 3.3 几何意义

1. **结果向量垂直于两个输入向量**
2. **长度等于平行四边形面积**：$|\vec{a} \times \vec{b}| = |\vec{a}||\vec{b}|\sin\theta$
3. **方向由右手定则确定**

### 3.4 应用场景

#### 法线计算

```hlsl
// 从三角形的两条边计算法线
float3 edge1 = v1 - v0;
float3 edge2 = v2 - v0;
float3 normal = normalize(cross(edge1, edge2));
```

#### 构建 TBN 矩阵

```hlsl
// Tangent-Bitangent-Normal 矩阵
float3 T = normalize(tangent);     // 切线
float3 N = normalize(normal);      // 法线
float3 B = cross(N, T);             // 副切线（Bitangent）
float3x3 TBN = float3x3(T, B, N);  // TBN 矩阵
```

#### 判断左右

```hlsl
// 判断点 P 是否在线段 AB 的左侧（2D）
float2 AB = B - A;
float2 AP = P - A;
float crossZ = AB.x * AP.y - AB.y * AP.x;  // 2D 叉积的 z 分量
bool isLeft = crossZ > 0.0;
```

## 4. 向量归一化 (Vector Normalization)

### 4.1 定义

归一化向量（单位向量）定义为：

$$\hat{v} = \frac{\vec{v}}{|\vec{v}|}$$

归一化后的向量长度为 1，方向不变。

### 4.2 HLSL 函数

```hlsl
float3 normalize(float3 v);           // 标准归一化
float3 SafeNormalize(float3 v);       // 安全归一化（避免除零）
float rsqrt(float x);                 // 倒数平方根（硬件优化）
```

### 4.3 归一化实现

```hlsl
// 标准归一化
float3 normalized = normalize(v);

// 快速归一化（使用 rsqrt）
float3 fastNormalized = v * rsqrt(dot(v, v));
```

> [!info] 安全归一化
> 关于 `SafeNormalize()` 的详细实现和使用，请参考：
> - [[Common]] - `SafeNormalize()` 函数详解
> - [[PrecisionOptimization]] - 法线向量归一化优化部分

### 4.4 应用场景

- **法线向量**：必须归一化用于光照计算
- **方向向量**：光线方向、视图方向等
- **切线向量**：用于构建 TBN 矩阵

## 5. 向量插值 (Vector Interpolation)

### 5.1 线性插值（Lerp）

```hlsl
float3 lerp(float3 a, float3 b, float t);
```

**数学定义**：$\text{lerp}(a, b, t) = a + t(b - a)$

**应用场景**：
- 颜色混合
- 位置插值
- 参数平滑过渡

### 5.2 球面线性插值（Slerp）

在球面上插值，保持向量长度：

```hlsl
// Unity 未提供内置 slerp，需要自定义实现
float3 slerp(float3 a, float3 b, float t) {
    float dotAB = dot(normalize(a), normalize(b));
    float theta = acos(clamp(dotAB, -1.0, 1.0));
    float sinTheta = sin(theta);
    
    if (sinTheta < 1e-6) {
        return lerp(a, b, t);  // 向量几乎平行，使用线性插值
    }
    
    float wa = sin((1.0 - t) * theta) / sinTheta;
    float wb = sin(t * theta) / sinTheta;
    return wa * normalize(a) + wb * normalize(b);
}
```

**应用场景**：
- 旋转插值
- 方向插值

## 6. 向量投影和反射 (Vector Projection & Reflection)

### 6.1 向量投影

将向量投影到另一个向量上：

```hlsl
// 投影到单位向量
float3 project(float3 v, float3 n) {
    return dot(v, n) * n;  // n 必须是单位向量
}

// 投影到任意向量
float3 project(float3 v, float3 n) {
    return dot(v, n) / dot(n, n) * n;
}
```

### 6.2 向量反射

```hlsl
float3 reflect(float3 I, float3 N);
```

**数学定义**：$\vec{R} = \vec{I} - 2(\vec{I} \cdot \vec{N})\vec{N}$

其中：
- `I`: 入射向量
- `N`: 法线向量（必须归一化）
- `R`: 反射向量

**应用场景**：
- 镜面反射
- 环境映射
- 光线追踪

## 7. 矩阵基础 (Matrix Fundamentals)

### 7.1 矩阵类型

```hlsl
float3x3 m3x3;   // 3x3 矩阵（常用于旋转、缩放）
float4x4 m4x4;   // 4x4 矩阵（常用于完整变换）
```

### 7.2 矩阵构造

```hlsl
// 4x4 单位矩阵
float4x4 identity = float4x4(
    float4(1, 0, 0, 0),
    float4(0, 1, 0, 0),
    float4(0, 0, 1, 0),
    float4(0, 0, 0, 1)
);

// Unity 提供的单位矩阵常量
float4x4 id = k_identity4x4;
```

### 7.3 矩阵访问

```hlsl
float4x4 m = ...;

// 访问矩阵元素
float element = m[0][1];     // 第 0 行第 1 列
float element2 = m._m01;     // Unity 约定（行主序）

// 访问矩阵的行
float4 row0 = m[0];          // 第 0 行
```

### 7.4 行主序 vs 列主序

- **Unity 使用行主序**：矩阵按行存储
- **OpenGL 使用列主序**：矩阵按列存储
- `mul()` 函数会自动处理差异

## 8. 矩阵乘法 (Matrix Multiplication)

### 8.1 HLSL 函数

```hlsl
float4 mul(float4x4 m, float4 v);      // 矩阵 × 向量
float4 mul(float4 v, float4x4 m);      // 向量 × 矩阵
float4x4 mul(float4x4 a, float4x4 b);  // 矩阵 × 矩阵
```

### 8.2 矩阵 × 向量

```hlsl
float4x4 matrix = ...;
float4 vector = float4(1.0, 2.0, 3.0, 1.0);

// Unity 中，向量是行向量，矩阵在右侧
float4 result = mul(vector, matrix);

// 或者使用 mul(matrix, vector)（mul 会自动处理）
float4 result2 = mul(matrix, vector);
```

### 8.3 矩阵 × 矩阵

```hlsl
float4x4 m1 = ...;
float4x4 m2 = ...;
float4x4 result = mul(m1, m2);  // result = m1 * m2
```

**注意**：矩阵乘法不满足交换律，`mul(m1, m2) ≠ mul(m2, m1)`

## 9. 变换矩阵 (Transformation Matrices)

### 9.1 平移矩阵（Translation）

```
[1  0  0  tx]
[0  1  0  ty]
[0  0  1  tz]
[0  0  0  1 ]
```

```hlsl
float4x4 TranslationMatrix(float3 t) {
    return float4x4(
        float4(1, 0, 0, t.x),
        float4(0, 1, 0, t.y),
        float4(0, 0, 1, t.z),
        float4(0, 0, 0, 1)
    );
}
```

### 9.2 旋转矩阵（Rotation）

#### 绕 X 轴旋转

```
[1    0        0       0]
[0  cos(θ) -sin(θ)     0]
[0  sin(θ)  cos(θ)     0]
[0    0        0       1]
```

#### 绕 Y 轴旋转

```
[ cos(θ)  0  sin(θ)  0]
[   0     1    0     0]
[-sin(θ)  0  cos(θ)  0]
[   0     0    0    1]
```

#### 绕 Z 轴旋转

```
[cos(θ) -sin(θ)  0  0]
[sin(θ)  cos(θ)  0  0]
[  0       0     1  0]
[  0       0     0  1]
```

### 9.3 缩放矩阵（Scale）

```
[sx  0   0   0]
[0   sy  0   0]
[0   0   sz  0]
[0   0   0   1]
```

```hlsl
float4x4 ScaleMatrix(float3 s) {
    return float4x4(
        float4(s.x, 0, 0, 0),
        float4(0, s.y, 0, 0),
        float4(0, 0, s.z, 0),
        float4(0, 0, 0, 1)
    );
}
```

### 9.4 组合变换（TRS）

变换通常按以下顺序组合：

```hlsl
// M = T * R * S（先缩放，再旋转，最后平移）
float4x4 TRS = mul(Translation, mul(Rotation, Scale));
```

## 10. 矩阵运算 (Matrix Operations)

### 10.1 矩阵转置

```hlsl
float4x4 transpose(float4x4 m);
```

转置矩阵：$M^T_{ij} = M_{ji}$

### 10.2 矩阵求逆

```hlsl
float4x4 inverse(float4x4 m);
```

**注意**：矩阵求逆计算开销大，通常预先计算并存储逆矩阵。

### 10.3 单位矩阵

```hlsl
float4x4 id = k_identity4x4;  // Unity 常量
```

单位矩阵：对角线为 1，其他为 0。

## 11. 齐次坐标 (Homogeneous Coordinates)

> **详细内容**：关于齐次坐标的引入背景、数学含义和图形学应用，参见 [[HomogeneousCoordinates]]。

### 11.1 定义

齐次坐标使用 4D 向量表示 3D 点或方向：

- **位置**：`(x, y, z, 1)`
- **方向**：`(x, y, z, 0)`

### 11.2 优势

1. **统一表示平移和旋转**：可以用矩阵乘法统一处理
2. **透视投影的自然表示**：w 分量用于透视除法

### 11.3 齐次除法

```hlsl
float4 hpos = ...;  // 齐次坐标
float3 pos = hpos.xyz / hpos.w;  // 透视除法
```

## 12. 坐标变换应用 (Coordinate Transform Applications)

### 12.1 位置变换

```hlsl
// 对象空间 → 世界空间
float4 posOS = float4(positionOS, 1.0);
float4 posWS = mul(posOS, ObjectToWorldMatrix);
float3 positionWS = posWS.xyz;
```

### 12.2 方向变换

```hlsl
// 方向向量只使用旋转部分（3x3 矩阵）
float3 dirOS = ...;
float3 dirWS = mul((float3x3)ObjectToWorldMatrix, dirOS);
```

### 12.3 法线变换

```hlsl
// 法线需要逆转置矩阵（处理非均匀缩放）
float3 normalOS = ...;
float3 normalWS = mul(normalOS, (float3x3)WorldToObjectMatrix);
```

**为什么需要逆转置**：
- 非均匀缩放会改变法线方向
- 逆转置矩阵保证法线正确变换

### 12.4 TBN 矩阵

切线空间到世界空间的变换矩阵：

```hlsl
float3 T = normalize(tangent);     // 切线
float3 N = normalize(normal);     // 法线
float3 B = cross(N, T);           // 副切线
float3x3 TBN = float3x3(T, B, N); // TBN 矩阵

// 切线空间法线 → 世界空间法线
float3 normalTS = ...;  // 从法线贴图采样
float3 normalWS = mul(normalTS, TBN);
```

## 13. 实用代码示例 (Practical Examples)

### 13.1 计算两个向量的夹角

```hlsl
float angleBetween(float3 a, float3 b) {
    return acos(dot(normalize(a), normalize(b)));
}
```

### 13.2 向量投影

```hlsl
float3 project(float3 v, float3 n) {
    float nLenSq = dot(n, n);
    if (nLenSq < 1e-6) return float3(0, 0, 0);
    return dot(v, n) / nLenSq * n;
}
```

### 13.3 向量反射

```hlsl
float3 reflect(float3 I, float3 N) {
    return I - 2.0 * dot(I, N) * N;
}
```

### 13.4 构建 TBN 矩阵

```hlsl
float3x3 BuildTBN(float3 T, float3 B, float3 N) {
    return float3x3(
        normalize(T),
        normalize(B),
        normalize(N)
    );
}
```

### 13.5 法线变换（考虑非均匀缩放）

```hlsl
float3 TransformNormal(float3 normalOS, float4x4 worldToObject) {
    // 使用逆转置矩阵（worldToObject 已经是逆矩阵）
    return mul(normalOS, (float3x3)worldToObject);
}
```

## 14. 参考资料 (References)

- [[Float]] - 浮点数格式和精度详解
- [[MathBasics]] - GPU 数学运算基础
- [[Common]] - Unity ShaderLibrary 核心函数
- [[SpaceTransforms]] - Unity 空间变换实现详解
- [Unity Shader Data Types](https://docs.unity3d.com/Manual/SL-DataTypesAndPrecision.html) - Unity 官方文档

