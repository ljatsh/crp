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
3. **方向由坐标系的手性决定**：
   - 在**右手坐标系**中，方向由**右手定则**确定
   - 在**左手坐标系**中，方向由**左手定则**确定
   - Unity 中：世界空间使用左手坐标系，视图空间使用右手坐标系

### 3.4 应用场景

#### 法线计算

```hlsl
// 从三角形的两条边计算法线
float3 edge1 = v1 - v0;
float3 edge2 = v2 - v0;
float3 normal = normalize(cross(edge1, edge2));
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
- **切线向量**：用于构建 TBN 矩阵（参见 [[#12 TBN 矩阵]]）

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

## 12. TBN 矩阵 (Tangent-Bitangent-Normal Matrix)

### 12.1 定义和概念

**TBN 矩阵**（Tangent-Bitangent-Normal Matrix）是一个 3×3 矩阵，用于在**切线空间**（Tangent Space）和**世界空间**（World Space）之间进行坐标变换。

**矩阵结构**：
```
TBN = [T, B, N] = [T_x  B_x  N_x]
                  [T_y  B_y  N_y]
                  [T_z  B_z  N_z]
```

其中：
- **T (Tangent)**：切线向量，沿 UV 坐标的 U 方向
- **B (Bitangent)**：副切线向量，沿 UV 坐标的 V 方向
- **N (Normal)**：法线向量，垂直于表面

### 12.2 切线空间坐标系

切线空间是一个**局部坐标系**，定义在每个顶点上：

- **T 轴（切线）**：沿纹理坐标的 U 方向（通常从顶点数据获取）
- **B 轴（副切线）**：沿纹理坐标的 V 方向（通过 `B = N × T` 计算）
- **N 轴（法线）**：垂直于表面（从顶点法线或法线贴图获取）

**坐标系特性**：
- T、B、N 三个向量**相互垂直**（正交基）
- 通常都是**归一化**的单位向量
- TBN 矩阵是**正交矩阵**（`TBN⁻¹ = TBNᵀ`）

### 12.3 TBN 矩阵的用途

TBN 矩阵主要用于以下场景：

#### 1. 法线贴图（Normal Mapping）

将法线贴图中存储的切线空间法线转换到世界空间：

```hlsl
// 从法线贴图采样（切线空间）
float3 normalTS = UnpackNormal(tex2D(_NormalMap, uv));
// 转换到世界空间
float3 normalWS = mul(normalTS, TBN);
```

#### 2. 视差映射（Parallax Mapping）

将视图方向转换到切线空间，用于视差偏移计算：

```hlsl
// 将世界空间视图方向转换到切线空间
float3 viewDirTS = mul(TBN, viewDirWS);
// 计算视差偏移
float2 parallaxOffset = ParallaxMapping(heightMap, viewDirTS, scale, uv);
```

#### 3. 环境映射（Environment Mapping）

在切线空间中计算反射方向，用于环境贴图采样。

#### 4. 各向异性光照

处理各向异性材质（如头发、金属拉丝）的光照计算。

### 12.4 构建 TBN 矩阵

#### 基本构建方法

```hlsl
// 从顶点数据获取（通常已归一化）
float3 T = normalize(tangent.xyz);  // 切线
float3 N = normalize(normal);       // 法线

// 计算副切线（注意叉乘顺序）
float3 B = cross(N, T);  // B = N × T

// 构建 TBN 矩阵
float3x3 TBN = float3x3(T, B, N);
```

**叉乘顺序说明**：
- 使用 `cross(N, T)` 而不是 `cross(T, N)`
- 这确保了 TBN 矩阵形成正确的坐标系（与 Unity 世界空间的手性一致）
- Unity 世界空间是左手坐标系，使用 `cross(N, T)` 会形成左手坐标系
- 如果使用 `cross(T, N)`，结果会形成右手坐标系，导致法线方向错误

#### Unity 中的标准构建方法

Unity 提供了 `CreateTangentToWorld()` 函数，正确处理了所有边界情况：

```hlsl
// Unity 标准方法（推荐）
real3x3 CreateTangentToWorld(real3 normal, real3 tangent, real flipSign)
{
    real sgn = flipSign * GetOddNegativeScale();
    real3 bitangent = cross(normal, tangent) * sgn;
    return real3x3(tangent, bitangent, normal);
}
```

**关键参数**：
- `flipSign`：控制副切线方向，通常从 `tangent.w` 获取
  - 处理镜像 UV 的情况
  - 确保 TBN 矩阵的手性正确
- `GetOddNegativeScale()`：处理负缩放的情况
  - 当物体有奇数个负缩放轴时，需要翻转副切线

**使用示例**：

```hlsl
// 顶点着色器输出
struct VertexOutput {
    float4 positionCS : SV_POSITION;
    float3 normalWS : TEXCOORD0;
    float4 tangentWS : TEXCOORD1;  // tangent.w 存储 flipSign
};

// 片段着色器中构建 TBN
real3x3 tangentToWorld = CreateTangentToWorld(
    input.normalWS, 
    input.tangentWS.xyz, 
    input.tangentWS.w
);

// 采样法线贴图并转换
real3 normalTS = UnpackNormal(tex2D(_NormalMap, uv));
real3 normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
```

> [!info] Unity 函数参考
> 关于 Unity 中 TBN 矩阵的详细实现和函数说明，参见：
> - [[SpaceTransforms#CreateTangentToWorld]] - TBN 矩阵构建函数
> - [[SpaceTransforms#TransformTangentToWorld]] - 切线空间到世界空间变换
> - [[SpaceTransforms#TransformWorldToTangent]] - 世界空间到切线空间变换

> [!info] 数学基础深入理解
> 关于 TBN 矩阵的数学原理（曲面参数化、切平面、方向导数等），参见：
> - [[DifferentialGeometry]] - 微分几何基础，深入理解 TBN 矩阵的数学含义

### 12.5 使用 TBN 矩阵进行坐标变换

#### 切线空间 → 世界空间

**法线变换**（使用行向量乘法）：
```hlsl
float3 normalTS = ...;  // 切线空间法线
float3 normalWS = mul(normalTS, TBN);
// 等价于：normalWS = normalTS.x * T + normalTS.y * B + normalTS.z * N
```

**方向变换**（使用列向量乘法）：
```hlsl
float3 dirTS = ...;  // 切线空间方向
float3 dirWS = mul(TBN, dirTS);
// 等价于：dirWS = {dot(dirTS, T), dot(dirTS, B), dot(dirTS, N)}
```

#### 世界空间 → 切线空间

由于 TBN 是正交矩阵，逆变换等于转置：

```hlsl
// 方法 1：使用转置（推荐，性能更好）
float3x3 worldToTangent = transpose(TBN);
float3 normalTS = mul(normalWS, worldToTangent);

// 方法 2：使用 Unity 提供的函数
float3 normalTS = TransformWorldToTangent(normalWS, TBN);
```

### 12.6 注意事项和常见问题

#### 1. 归一化的重要性

- T、B、N 向量必须**归一化**，否则 TBN 矩阵不是正交矩阵
- 如果顶点数据未归一化，需要手动归一化：
  ```hlsl
  float3 T = normalize(tangent.xyz);
  float3 N = normalize(normal);
  ```

#### 2. flipSign 的处理

- Unity 中，`tangent.w` 存储了 `flipSign`
- 必须使用 `flipSign` 来正确处理镜像 UV 和负缩放
- 忽略 `flipSign` 会导致法线方向错误

#### 3. 负缩放的处理

- 当物体有**奇数个负缩放轴**时（如 `scale = (-1, 1, 1)`），需要翻转副切线
- Unity 的 `GetOddNegativeScale()` 会自动处理这种情况

#### 4. 坐标系手性

- Unity 世界空间是**左手坐标系**
- 使用 `cross(N, T)` 确保 TBN 矩阵形成正确的坐标系
- 错误的叉乘顺序会导致法线方向翻转

#### 5. 性能优化

- TBN 矩阵是正交矩阵，逆变换等于转置（无需计算逆矩阵）
- 如果输入法线已归一化且 TBN 正交，变换后通常不需要再次归一化
- 可以使用 `doNormalize = false` 跳过归一化以提高性能

### 12.7 实际应用示例

#### 完整的法线贴图采样流程

```hlsl
// 顶点着色器
struct VertexInput {
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;  // tangent.w 存储 flipSign
    float2 uv : TEXCOORD0;
};

struct VertexOutput {
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float4 tangentWS : TEXCOORD2;  // 传递 flipSign
};

VertexOutput vert(VertexInput input) {
    VertexOutput output;
    output.positionCS = TransformObjectToHClip(input.positionOS);
    output.uv = input.uv;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.tangentWS = float4(
        TransformObjectToWorldDir(input.tangentOS.xyz),
        input.tangentOS.w  // 传递 flipSign
    );
    return output;
}

// 片段着色器
float4 frag(VertexOutput input) : SV_Target {
    // 构建 TBN 矩阵
    real3x3 tangentToWorld = CreateTangentToWorld(
        input.normalWS,
        input.tangentWS.xyz,
        input.tangentWS.w
    );
    
    // 采样法线贴图（切线空间）
    real3 normalTS = UnpackNormal(tex2D(_NormalMap, input.uv));
    
    // 转换到世界空间
    real3 normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
    
    // 使用世界空间法线进行光照计算
    // ...
}
```

## 13. 坐标变换应用 (Coordinate Transform Applications)

### 13.1 位置变换

```hlsl
// 对象空间 → 世界空间
float4 posOS = float4(positionOS, 1.0);
float4 posWS = mul(posOS, ObjectToWorldMatrix);
float3 positionWS = posWS.xyz;
```

### 13.2 方向变换

```hlsl
// 方向向量只使用旋转部分（3x3 矩阵）
float3 dirOS = ...;
float3 dirWS = mul((float3x3)ObjectToWorldMatrix, dirOS);
```

### 13.3 法线变换

```hlsl
// 法线需要逆转置矩阵（处理非均匀缩放）
float3 normalOS = ...;
float3 normalWS = mul(normalOS, (float3x3)WorldToObjectMatrix);
```

**为什么需要逆转置**：
- 非均匀缩放会改变法线方向
- 逆转置矩阵保证法线正确变换

## 14. 实用代码示例 (Practical Examples)

### 14.1 计算两个向量的夹角

```hlsl
float angleBetween(float3 a, float3 b) {
    return acos(dot(normalize(a), normalize(b)));
}
```

### 14.2 向量投影

```hlsl
float3 project(float3 v, float3 n) {
    float nLenSq = dot(n, n);
    if (nLenSq < 1e-6) return float3(0, 0, 0);
    return dot(v, n) / nLenSq * n;
}
```

### 14.3 向量反射

```hlsl
float3 reflect(float3 I, float3 N) {
    return I - 2.0 * dot(I, N) * N;
}
```

### 14.4 法线变换（考虑非均匀缩放）

```hlsl
float3 TransformNormal(float3 normalOS, float4x4 worldToObject) {
    // 使用逆转置矩阵（worldToObject 已经是逆矩阵）
    return mul(normalOS, (float3x3)worldToObject);
}
```

## 15. 参考资料 (References)

- [[Float]] - 浮点数格式和精度详解
- [[MathBasics]] - GPU 数学运算基础
- [[Common]] - Unity ShaderLibrary 核心函数
- [[SpaceTransforms]] - Unity 空间变换实现详解
- [[DifferentialGeometry]] - 微分几何基础，TBN 矩阵的数学原理
- [Unity Shader Data Types](https://docs.unity3d.com/Manual/SL-DataTypesAndPrecision.html) - Unity 官方文档

