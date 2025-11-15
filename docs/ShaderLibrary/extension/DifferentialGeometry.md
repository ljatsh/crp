---
tags: [shader, 微分几何, 多元微积分, 高等数学, differential geometry, multivariable calculus, advanced mathematics]
aliases: [DifferentialGeometry, 微分几何, 多元微积分基础, AdvancedMathematics]
created: 2025-01-20
related:
  - "[[VectorOperations#12 TBN 矩阵]]"
  - "[[SpaceTransforms]]"
  - "[[TransformationMatrices]]"
---

# 微分几何基础 / Differential Geometry Fundamentals

## 概述 / Overview

本文档从**多元微积分**和**微分几何**的角度深入解释计算机图形学中常用的数学概念，特别是**曲面参数化**、**切空间**和**TBN 矩阵**的数学原理，适合有一定数学基础但需要复习的读者。

This document provides an in-depth explanation of mathematical concepts commonly used in computer graphics from the perspective of **multivariable calculus** and **differential geometry**, particularly the mathematical principles behind **surface parameterization**, **tangent space**, and **TBN matrices**, suitable for readers with mathematical background who need a refresher.

> [!info] 学习路径 / Learning Path
> - **路径1（实用）**：参见 [[VectorOperations#12 TBN 矩阵]] - 如何使用 TBN 矩阵
> - **路径2（深入）**：本文档 - 理解 TBN 矩阵的数学原理
> - **路径3（完整）**：待补充 - 完整的微分几何理论

**相关主题 / Related Topics**：
- [[VectorOperations#12 TBN 矩阵]] - TBN 矩阵的实用指南
- [[SpaceTransforms]] - Unity 空间变换实现
- [[TransformationMatrices]] - 变换矩阵基础

---

## 1. 曲面参数化 / Surface Parameterization

### 1.1 参数化表示 / Parametric Representation

在计算机图形学中，3D 表面通常通过**参数化**（Parameterization）来表示。

In computer graphics, 3D surfaces are typically represented through **parameterization**.

**参数化曲面 / Parametric Surface**：

$$\mathbf{r}(u, v) = \begin{pmatrix} x(u,v) \\ y(u,v) \\ z(u,v) \end{pmatrix}$$

其中：
- $(u, v)$ 是**参数坐标**（Parameter Coordinates），通常对应**纹理坐标**（UV Coordinates）
- $\mathbf{r}(u, v)$ 是曲面上参数为 $(u, v)$ 的点

Where:
- $(u, v)$ are **parameter coordinates**, typically corresponding to **texture coordinates** (UV coordinates)
- $\mathbf{r}(u, v)$ is a point on the surface with parameters $(u, v)$

**示例 / Example**：

一个简单的平面可以表示为：
A simple plane can be represented as:

$$\mathbf{r}(u, v) = \begin{pmatrix} u \\ v \\ 0 \end{pmatrix}, \quad u, v \in [0, 1]$$

### 1.2 偏导数 / Partial Derivatives

曲面的**切向量**（Tangent Vectors）通过**偏导数**（Partial Derivatives）计算：

The **tangent vectors** of a surface are computed using **partial derivatives**:

**U 方向的切向量 / Tangent Vector in U Direction**：

$$\mathbf{T}_u = \frac{\partial \mathbf{r}}{\partial u} = \begin{pmatrix} \frac{\partial x}{\partial u} \\ \frac{\partial y}{\partial u} \\ \frac{\partial z}{\partial u} \end{pmatrix}$$

**V 方向的切向量 / Tangent Vector in V Direction**：

$$\mathbf{T}_v = \frac{\partial \mathbf{r}}{\partial v} = \begin{pmatrix} \frac{\partial x}{\partial v} \\ \frac{\partial y}{\partial v} \\ \frac{\partial z}{\partial v} \end{pmatrix}$$

**几何意义 / Geometric Meaning**：
- $\mathbf{T}_u$ 表示沿 U 方向移动时，3D 位置的变化率
- $\mathbf{T}_v$ 表示沿 V 方向移动时，3D 位置的变化率

- $\mathbf{T}_u$ represents the rate of change of 3D position when moving along the U direction
- $\mathbf{T}_v$ represents the rate of change of 3D position when moving along the V direction

---

## 2. 切平面 / Tangent Plane

### 2.1 切平面的定义 / Definition of Tangent Plane

在曲面上一点 $P = \mathbf{r}(u_0, v_0)$，**切平面**（Tangent Plane）是由两个切向量 $\mathbf{T}_u$ 和 $\mathbf{T}_v$ 张成的平面。

At a point $P = \mathbf{r}(u_0, v_0)$ on the surface, the **tangent plane** is the plane spanned by the two tangent vectors $\mathbf{T}_u$ and $\mathbf{T}_v$.

**切平面的方程 / Equation of Tangent Plane**：

$$\mathbf{r}(u, v) \approx \mathbf{r}(u_0, v_0) + (u - u_0)\mathbf{T}_u + (v - v_0)\mathbf{T}_v$$

这是曲面在点 $P$ 的**线性近似**（Linear Approximation）。

This is the **linear approximation** of the surface at point $P$.

### 2.2 切平面的几何意义 / Geometric Meaning

- **切平面**是曲面在该点的"最接近的平面"
- 切平面内的所有向量都是**切向量**（Tangent Vectors）
- 垂直于切平面的向量是**法向量**（Normal Vector）

- The **tangent plane** is the "closest plane" to the surface at that point
- All vectors in the tangent plane are **tangent vectors**
- Vectors perpendicular to the tangent plane are **normal vectors**

---

## 3. 法向量 / Normal Vector

### 3.1 法向量的计算方法 / Methods for Computing Normal Vector

法向量（Normal Vector）垂直于切平面，有两种主要的计算方法：

The normal vector is perpendicular to the tangent plane and can be computed using two main methods:

#### 方法 1：叉积法 / Method 1: Cross Product

**叉积法**（Cross Product Method）是最常用的方法：

The **cross product method** is the most commonly used:

$$\mathbf{N} = \mathbf{T}_u \times \mathbf{T}_v$$

**归一化法向量 / Normalized Normal Vector**：

$$\hat{\mathbf{N}} = \frac{\mathbf{N}}{|\mathbf{N}|} = \frac{\mathbf{T}_u \times \mathbf{T}_v}{|\mathbf{T}_u \times \mathbf{T}_v|}$$

**叉积的计算公式 / Cross Product Formula**：

$$\mathbf{T}_u \times \mathbf{T}_v = \begin{vmatrix} \mathbf{i} & \mathbf{j} & \mathbf{k} \\ T_{u,x} & T_{u,y} & T_{u,z} \\ T_{v,x} & T_{v,y} & T_{v,z} \end{vmatrix} = \begin{pmatrix} T_{u,y} T_{v,z} - T_{u,z} T_{v,y} \\ T_{u,z} T_{v,x} - T_{u,x} T_{v,z} \\ T_{u,x} T_{v,y} - T_{u,y} T_{v,x} \end{pmatrix}$$

**优点 / Advantages**：
- 计算简单直接
- 几何意义清晰（垂直于两个切向量）
- 适用于参数化曲面

- Simple and direct computation
- Clear geometric meaning (perpendicular to both tangent vectors)
- Suitable for parametric surfaces

#### 方法 2：切平面方程的点法式 / Method 2: Point-Normal Form of Tangent Plane

**切平面方程 / Tangent Plane Equation**：

切平面可以表示为：

The tangent plane can be represented as:

$$\mathbf{N} \cdot (\mathbf{r} - \mathbf{r}_0) = 0$$

其中 $\mathbf{r}_0$ 是切平面上的一个点，$\mathbf{N}$ 是法向量。

Where $\mathbf{r}_0$ is a point on the tangent plane and $\mathbf{N}$ is the normal vector.

**从切平面方程推导法向量 / Deriving Normal Vector from Tangent Plane Equation**：

对于参数化曲面 $\mathbf{r}(u, v)$，切平面的方程可以写成：

For a parametric surface $\mathbf{r}(u, v)$, the tangent plane equation can be written as:

$$\mathbf{r}(u, v) = \mathbf{r}(u_0, v_0) + (u - u_0)\mathbf{T}_u + (v - v_0)\mathbf{T}_v$$

将其转换为**点法式**（Point-Normal Form）：

Converting it to **point-normal form**:

$$\mathbf{N} \cdot (\mathbf{r} - \mathbf{r}_0) = 0$$

其中法向量 $\mathbf{N}$ 必须满足：

Where the normal vector $\mathbf{N}$ must satisfy:

$$\mathbf{N} \cdot \mathbf{T}_u = 0 \quad \text{and} \quad \mathbf{N} \cdot \mathbf{T}_v = 0$$

这意味着 $\mathbf{N}$ 同时垂直于 $\mathbf{T}_u$ 和 $\mathbf{T}_v$，因此：

This means $\mathbf{N}$ is perpendicular to both $\mathbf{T}_u$ and $\mathbf{T}_v$, therefore:

$$\mathbf{N} = \mathbf{T}_u \times \mathbf{T}_v$$

**两种方法的关系 / Relationship Between the Two Methods**：

两种方法在数学上是**等价**的：

The two methods are mathematically **equivalent**:

1. **叉积法**直接计算：$\mathbf{N} = \mathbf{T}_u \times \mathbf{T}_v$
2. **点法式法**通过切平面方程推导，最终也得到：$\mathbf{N} = \mathbf{T}_u \times \mathbf{T}_v$

**英文说明 / English Explanation**：
1. **Cross product method** directly computes: $\mathbf{N} = \mathbf{T}_u \times \mathbf{T}_v$
2. **Point-normal form method** derives from the tangent plane equation and also yields: $\mathbf{N} = \mathbf{T}_u \times \mathbf{T}_v$

**点法式法的优势 / Advantages of Point-Normal Form Method**：
- 提供了法向量的另一种理解角度
- 对于隐式曲面 $F(x, y, z) = 0$，可以直接使用梯度：$\mathbf{N} = \nabla F$

- Provides an alternative perspective on normal vectors
- For implicit surfaces $F(x, y, z) = 0$, can directly use gradient: $\mathbf{N} = \nabla F$

### 3.2 几何意义 / Geometric Meaning

**法向量的几何性质 / Geometric Properties of Normal Vector**：
- 法向量垂直于切平面，但方向（内侧/外侧）取决于**参数化的方向**
- 法向量的长度等于切向量张成的**平行四边形面积**：$|\mathbf{N}| = |\mathbf{T}_u \times \mathbf{T}_v| = |\mathbf{T}_u||\mathbf{T}_v|\sin\theta$

- The normal vector is perpendicular to the tangent plane, but its direction (inward/outward) depends on the **orientation of parameterization**
- The length of the normal vector equals the area of the **parallelogram** spanned by the tangent vectors: $|\mathbf{N}| = |\mathbf{T}_u \times \mathbf{T}_v| = |\mathbf{T}_u||\mathbf{T}_v|\sin\theta$

### 3.3 法向量的方向 / Direction of Normal Vector

#### 坐标系手性与法向量方向 / Coordinate System Handedness and Normal Direction

在**左手坐标系**（Left-Handed Coordinate System）中：
- 使用 $\mathbf{N} = \mathbf{T}_u \times \mathbf{T}_v$ 得到左手坐标系
- 使用 $\mathbf{N} = \mathbf{T}_v \times \mathbf{T}_u$ 得到右手坐标系

In a **left-handed coordinate system**:
- Using $\mathbf{N} = \mathbf{T}_u \times \mathbf{T}_v$ yields a left-handed coordinate system
- Using $\mathbf{N} = \mathbf{T}_v \times \mathbf{T}_u$ yields a right-handed coordinate system

Unity 使用左手坐标系，因此使用 $\mathbf{N} = \mathbf{T}_u \times \mathbf{T}_v$。

Unity uses a left-handed coordinate system, so it uses $\mathbf{N} = \mathbf{T}_u \times \mathbf{T}_v$.

#### 法向量可能指向内侧 / Normal Vector May Point Inward

**重要说明 / Important Note**：

在左手坐标系中，使用 $\mathbf{T}_u \times \mathbf{T}_v$ 计算的法向量方向取决于**参数化的方向**：

In a left-handed coordinate system, the direction of the normal vector computed using $\mathbf{T}_u \times \mathbf{T}_v$ depends on the **orientation of parameterization**:

- 如果参数化方向使得 $\mathbf{T}_u \times \mathbf{T}_v$ 指向**外侧**，法向量指向外侧
- 如果参数化方向使得 $\mathbf{T}_u \times \mathbf{T}_v$ 指向**内侧**，法向量指向内侧

- If the parameterization orientation makes $\mathbf{T}_u \times \mathbf{T}_v$ point **outward**, the normal points outward
- If the parameterization orientation makes $\mathbf{T}_u \times \mathbf{T}_v$ point **inward**, the normal points inward

**示例 / Example**：

考虑一个立方体的一个面，如果 UV 坐标被镜像（Mirrored），那么：
- $\mathbf{T}_u$ 的方向会翻转
- $\mathbf{T}_u \times \mathbf{T}_v$ 的方向也会翻转
- 法向量可能从指向外侧变为指向内侧

Consider a face of a cube. If UV coordinates are mirrored:
- The direction of $\mathbf{T}_u$ will flip
- The direction of $\mathbf{T}_u \times \mathbf{T}_v$ will also flip
- The normal may change from pointing outward to pointing inward

**Unity 的处理方式 / Unity's Handling**：

Unity 通过 `flipSign`（存储在 `tangent.w` 中）来处理这种情况：

Unity handles this through `flipSign` (stored in `tangent.w`):

```hlsl
real sgn = flipSign * GetOddNegativeScale();
real3 bitangent = cross(normal, tangent) * sgn;
```

- 当 `flipSign = -1` 时，副切线方向被翻转
- 这确保了 TBN 矩阵形成正确的坐标系，法向量指向正确的方向

- When `flipSign = -1`, the bitangent direction is flipped
- This ensures the TBN matrix forms the correct coordinate system with the normal pointing in the correct direction

---

## 4. TBN 矩阵的构建 / Construction of TBN Matrix

### 4.1 从参数化到 TBN / From Parameterization to TBN

给定参数化曲面 $\mathbf{r}(u, v)$，我们可以计算：

Given a parametric surface $\mathbf{r}(u, v)$, we can compute:

1. **切向量 / Tangent Vectors**：
   - $\mathbf{T} = \frac{\partial \mathbf{r}}{\partial u}$（归一化后得到 T）
   - $\mathbf{B} = \frac{\partial \mathbf{r}}{\partial v}$（归一化后得到 B）

2. **法向量 / Normal Vector**：
   - $\mathbf{N} = \mathbf{T} \times \mathbf{B}$（归一化）

3. **TBN 矩阵 / TBN Matrix**：
   $$\text{TBN} = \begin{bmatrix} \mathbf{T} & \mathbf{B} & \mathbf{N} \end{bmatrix} = \begin{bmatrix} T_x & B_x & N_x \\ T_y & B_y & N_y \\ T_z & B_z & N_z \end{bmatrix}$$

### 4.2 离散网格中的计算 / Computation in Discrete Meshes

在实际的 3D 网格中，我们通常没有解析的参数化函数，而是有：

In actual 3D meshes, we typically don't have an analytic parameterization function, but instead have:

- **顶点位置**：$\mathbf{p}_i = (x_i, y_i, z_i)$
- **纹理坐标**：$(u_i, v_i)$
- **法线向量**：$\mathbf{n}_i$（通常从相邻面的法线平均得到）

#### 4.2.1 三角形中的切向量计算 / Tangent Vector Computation in Triangles

对于单个三角形，我们可以通过**线性方程组**求解切向量：

For a single triangle, we can solve for the tangent vector using a **system of linear equations**:

**基本思路 / Basic Idea**：

对于三角形的三个顶点 $\mathbf{p}_0, \mathbf{p}_1, \mathbf{p}_2$ 和对应的 UV 坐标 $(u_0, v_0), (u_1, v_1), (u_2, v_2)$，我们有两个边向量：

For triangle vertices $\mathbf{p}_0, \mathbf{p}_1, \mathbf{p}_2$ with UV coordinates $(u_0, v_0), (u_1, v_1), (u_2, v_2)$, we have two edge vectors:

$$\Delta \mathbf{p}_1 = \mathbf{p}_1 - \mathbf{p}_0, \quad \Delta \mathbf{p}_2 = \mathbf{p}_2 - \mathbf{p}_0$$

对应的 UV 差值为：

Corresponding UV differences:

$$\Delta u_1 = u_1 - u_0, \quad \Delta v_1 = v_1 - v_0$$
$$\Delta u_2 = u_2 - u_0, \quad \Delta v_2 = v_2 - v_0$$

**线性方程组 / System of Linear Equations**：

切向量 $\mathbf{T}$ 和副切线向量 $\mathbf{B}$ 必须满足：

The tangent vector $\mathbf{T}$ and bitangent vector $\mathbf{B}$ must satisfy:

$$\Delta \mathbf{p}_1 = \Delta u_1 \mathbf{T} + \Delta v_1 \mathbf{B}$$
$$\Delta \mathbf{p}_2 = \Delta u_2 \mathbf{T} + \Delta v_2 \mathbf{B}$$

写成矩阵形式：

In matrix form:

$$\begin{pmatrix} \Delta u_1 & \Delta v_1 \\ \Delta u_2 & \Delta v_2 \end{pmatrix} \begin{pmatrix} \mathbf{T} \\ \mathbf{B} \end{pmatrix} = \begin{pmatrix} \Delta \mathbf{p}_1 \\ \Delta \mathbf{p}_2 \end{pmatrix}$$

**求解 / Solution**：

如果矩阵可逆（即 $\Delta u_1 \Delta v_2 - \Delta u_2 \Delta v_1 \neq 0$），可以直接求解：

If the matrix is invertible (i.e., $\Delta u_1 \Delta v_2 - \Delta u_2 \Delta v_1 \neq 0$), we can solve directly:

$$\begin{pmatrix} \mathbf{T} \\ \mathbf{B} \end{pmatrix} = \frac{1}{\Delta u_1 \Delta v_2 - \Delta u_2 \Delta v_1} \begin{pmatrix} \Delta v_2 & -\Delta v_1 \\ -\Delta u_2 & \Delta u_1 \end{pmatrix} \begin{pmatrix} \Delta \mathbf{p}_1 \\ \Delta \mathbf{p}_2 \end{pmatrix}$$

展开得到：

Expanding:

$$\mathbf{T} = \frac{\Delta v_2 \Delta \mathbf{p}_1 - \Delta v_1 \Delta \mathbf{p}_2}{\Delta u_1 \Delta v_2 - \Delta u_2 \Delta v_1}$$

$$\mathbf{B} = \frac{-\Delta u_2 \Delta \mathbf{p}_1 + \Delta u_1 \Delta \mathbf{p}_2}{\Delta u_1 \Delta v_2 - \Delta u_2 \Delta v_1}$$

#### 4.2.2 顶点切向量的计算 / Vertex Tangent Vector Computation

在实际应用中，一个顶点通常属于多个三角形。我们需要**平均**所有相邻三角形的切向量：

In practice, a vertex typically belongs to multiple triangles. We need to **average** the tangent vectors from all adjacent triangles:

**方法 1：简单平均 / Method 1: Simple Average**：

$$\mathbf{T}_{\text{vertex}} = \frac{1}{n} \sum_{i=1}^{n} \mathbf{T}_i$$

其中 $n$ 是共享该顶点的三角形数量。

Where $n$ is the number of triangles sharing the vertex.

**方法 2：加权平均 / Method 2: Weighted Average**：

根据三角形的面积或角度加权：

Weighted by triangle area or angle:

$$\mathbf{T}_{\text{vertex}} = \frac{\sum_{i=1}^{n} w_i \mathbf{T}_i}{\sum_{i=1}^{n} w_i}$$

其中 $w_i$ 是权重（通常是三角形面积）。

Where $w_i$ is the weight (typically triangle area).

#### 4.2.3 Gram-Schmidt 正交化 / Gram-Schmidt Orthogonalization

由于计算出的切向量可能不严格正交，需要进行**Gram-Schmidt 正交化**：

Since computed tangent vectors may not be strictly orthogonal, **Gram-Schmidt orthogonalization** is needed:

**步骤 / Steps**：

1. 归一化切向量：$\mathbf{T} = \frac{\mathbf{T}}{|\mathbf{T}|}$
2. 从副切线中减去切向量方向的分量：
   $$\mathbf{B} = \mathbf{B} - (\mathbf{B} \cdot \mathbf{T}) \mathbf{T}$$
3. 归一化副切线：$\mathbf{B} = \frac{\mathbf{B}}{|\mathbf{B}|}$
4. 计算法向量：$\mathbf{N} = \mathbf{T} \times \mathbf{B}$（或使用顶点法线）

**英文步骤 / English Steps**：

1. Normalize tangent: $\mathbf{T} = \frac{\mathbf{T}}{|\mathbf{T}|}$
2. Remove tangent component from bitangent:
   $$\mathbf{B} = \mathbf{B} - (\mathbf{B} \cdot \mathbf{T}) \mathbf{T}$$
3. Normalize bitangent: $\mathbf{B} = \frac{\mathbf{B}}{|\mathbf{B}|}$
4. Compute normal: $\mathbf{N} = \mathbf{T} \times \mathbf{B}$ (or use vertex normal)

#### 4.2.4 经典实现方法 / Classic Implementation Methods

> [!info] 经典参考 / Classic References
> 以下方法被广泛采用，详细实现参见：
> - **Lengyel 方法**：基于线性方程组求解，适用于大多数情况
> - **The Witness 方法**：针对游戏优化的实现，考虑了性能和精度

**Lengyel 方法 / Lengyel's Method**：

Eric Lengyel 提出的经典方法，通过求解线性方程组计算切向量：

Eric Lengyel's classic method computes tangent vectors by solving a system of linear equations:

```cpp
// 伪代码 / Pseudocode
for each triangle {
    Vector3 edge1 = p1 - p0;
    Vector3 edge2 = p2 - p0;
    Vector2 deltaUV1 = uv1 - uv0;
    Vector2 deltaUV2 = uv2 - uv0;
    
    float f = 1.0f / (deltaUV1.x * deltaUV2.y - deltaUV2.x * deltaUV1.y);
    
    Vector3 tangent = f * (deltaUV2.y * edge1 - deltaUV1.y * edge2);
    Vector3 bitangent = f * (deltaUV1.x * edge2 - deltaUV2.x * edge1);
    
    // 归一化并正交化
    tangent = normalize(tangent);
    bitangent = normalize(bitangent - dot(bitangent, tangent) * tangent);
}
```

**The Witness 方法 / The Witness Method**：

《The Witness》游戏中使用的优化方法，考虑了：

The optimized method used in "The Witness" game, considering:

1. **顶点共享处理**：正确处理共享顶点的多个三角形
2. **数值稳定性**：避免除零和数值精度问题
3. **性能优化**：减少不必要的计算

**英文说明 / English Explanation**：

1. **Vertex sharing handling**: Properly handles multiple triangles sharing vertices
2. **Numerical stability**: Avoids division by zero and precision issues
3. **Performance optimization**: Reduces unnecessary computations

**关键优化点 / Key Optimizations**：

- 使用**Mikkelsen 空间**（Mikkelsen Space）进行更稳定的计算
- 考虑**镜像 UV** 的情况
- 处理**退化三角形**（Degenerate Triangles）

- Using **Mikkelsen Space** for more stable computation
- Handling **mirrored UV** cases
- Dealing with **degenerate triangles**

---

## 5. 方向导数与梯度 / Directional Derivative and Gradient

### 5.1 方向导数 / Directional Derivative

**方向导数**（Directional Derivative）描述函数在特定方向上的变化率：

The **directional derivative** describes the rate of change of a function in a specific direction:

$$D_{\mathbf{u}} f = \nabla f \cdot \mathbf{u}$$

其中 $\mathbf{u}$ 是单位方向向量。

Where $\mathbf{u}$ is a unit direction vector.

### 5.2 梯度向量 / Gradient Vector

对于标量函数 $f(x, y, z)$，**梯度向量**（Gradient Vector）为：

For a scalar function $f(x, y, z)$, the **gradient vector** is:

$$\nabla f = \begin{pmatrix} \frac{\partial f}{\partial x} \\ \frac{\partial f}{\partial y} \\ \frac{\partial f}{\partial z} \end{pmatrix}$$

**几何意义 / Geometric Meaning**：
- 梯度方向是函数值**增长最快**的方向
- 梯度的大小表示变化率

- The gradient direction is the direction of **fastest increase** of the function value
- The magnitude of the gradient represents the rate of change

### 5.3 与切向量的关系 / Relationship with Tangent Vectors

在参数化曲面中：
- $\mathbf{T}_u = \frac{\partial \mathbf{r}}{\partial u}$ 可以理解为"沿 U 方向的方向导数"
- $\mathbf{T}_v = \frac{\partial \mathbf{r}}{\partial v}$ 可以理解为"沿 V 方向的方向导数"

In parametric surfaces:
- $\mathbf{T}_u = \frac{\partial \mathbf{r}}{\partial u}$ can be understood as the "directional derivative along the U direction"
- $\mathbf{T}_v = \frac{\partial \mathbf{r}}{\partial v}$ can be understood as the "directional derivative along the V direction"

---

## 6. 切空间 / Tangent Space

### 6.1 切空间的定义 / Definition of Tangent Space

在曲面上一点 $P$，**切空间**（Tangent Space）是所有切向量构成的向量空间。

At a point $P$ on a surface, the **tangent space** is the vector space of all tangent vectors.

**切空间的基 / Basis of Tangent Space**：
- $\mathbf{T}$（切线向量，沿 U 方向）
- $\mathbf{B}$（副切线向量，沿 V 方向）
- $\mathbf{N}$（法线向量，垂直于切平面）

**切空间的维度 / Dimension of Tangent Space**：
- 切平面是 2 维的（由 T 和 B 张成）
- 完整的切空间（包括法线）是 3 维的

- The tangent plane is 2-dimensional (spanned by T and B)
- The complete tangent space (including the normal) is 3-dimensional

### 6.2 切空间的坐标变换 / Coordinate Transformation in Tangent Space

**从切线空间到世界空间 / From Tangent Space to World Space**：

$$\mathbf{v}_{\text{world}} = \text{TBN} \cdot \mathbf{v}_{\text{tangent}} = v_t \mathbf{T} + v_b \mathbf{B} + v_n \mathbf{N}$$

**从世界空间到切线空间 / From World Space to Tangent Space**：

由于 TBN 是**正交矩阵**（Orthogonal Matrix），逆变换等于转置：

Since TBN is an **orthogonal matrix**, the inverse transformation equals the transpose:

$$\mathbf{v}_{\text{tangent}} = \text{TBN}^T \cdot \mathbf{v}_{\text{world}}$$

---

## 7. 实际应用 / Practical Applications

### 7.1 法线贴图 / Normal Mapping

法线贴图中存储的法线是**切线空间**中的法线：

The normals stored in normal maps are normals in **tangent space**:

$$\mathbf{n}_{\text{tangent}} = (n_x, n_y, n_z)$$

其中：
- $n_x, n_y$ 表示法线在切平面内的偏移
- $n_z$ 通常接近 1（因为法线贴图通常存储小的扰动）

Where:
- $n_x, n_y$ represent the offset of the normal within the tangent plane
- $n_z$ is usually close to 1 (since normal maps typically store small perturbations)

**转换到世界空间 / Conversion to World Space**：

$$\mathbf{n}_{\text{world}} = \text{TBN} \cdot \mathbf{n}_{\text{tangent}}$$

### 7.2 视差映射 / Parallax Mapping

视差映射需要将**视图方向**转换到切线空间：

Parallax mapping requires converting the **view direction** to tangent space:

$$\mathbf{v}_{\text{tangent}} = \text{TBN}^T \cdot \mathbf{v}_{\text{world}}$$

然后在切线空间中计算视差偏移。

Then compute the parallax offset in tangent space.

---

## 8. 数学推导示例 / Mathematical Derivation Example

### 8.1 简单平面 / Simple Plane

考虑一个简单的平面：

Consider a simple plane:

$$\mathbf{r}(u, v) = \begin{pmatrix} u \\ v \\ 0 \end{pmatrix}, \quad u, v \in [0, 1]$$

**计算切向量 / Compute Tangent Vectors**：

$$\mathbf{T}_u = \frac{\partial \mathbf{r}}{\partial u} = \begin{pmatrix} 1 \\ 0 \\ 0 \end{pmatrix}$$

$$\mathbf{T}_v = \frac{\partial \mathbf{r}}{\partial v} = \begin{pmatrix} 0 \\ 1 \\ 0 \end{pmatrix}$$

**计算法向量 - 方法 1：叉积法 / Compute Normal Vector - Method 1: Cross Product**：

$$\mathbf{N} = \mathbf{T}_u \times \mathbf{T}_v = \begin{pmatrix} 1 \\ 0 \\ 0 \end{pmatrix} \times \begin{pmatrix} 0 \\ 1 \\ 0 \end{pmatrix} = \begin{pmatrix} 0 \\ 0 \\ 1 \end{pmatrix}$$

**计算法向量 - 方法 2：点法式法 / Compute Normal Vector - Method 2: Point-Normal Form**：

切平面方程可以写成：

The tangent plane equation can be written as:

$$\mathbf{r}(u, v) = \mathbf{r}(u_0, v_0) + (u - u_0)\mathbf{T}_u + (v - v_0)\mathbf{T}_v$$

对于点 $(u_0, v_0) = (0, 0)$，切平面方程为：

For point $(u_0, v_0) = (0, 0)$, the tangent plane equation is:

$$\mathbf{r}(u, v) = \begin{pmatrix} 0 \\ 0 \\ 0 \end{pmatrix} + u \begin{pmatrix} 1 \\ 0 \\ 0 \end{pmatrix} + v \begin{pmatrix} 0 \\ 1 \\ 0 \end{pmatrix} = \begin{pmatrix} u \\ v \\ 0 \end{pmatrix}$$

转换为点法式：$\mathbf{N} \cdot (\mathbf{r} - \mathbf{r}_0) = 0$

Converting to point-normal form: $\mathbf{N} \cdot (\mathbf{r} - \mathbf{r}_0) = 0$

由于 $\mathbf{N}$ 必须垂直于 $\mathbf{T}_u$ 和 $\mathbf{T}_v$：

Since $\mathbf{N}$ must be perpendicular to $\mathbf{T}_u$ and $\mathbf{T}_v$:

$$\mathbf{N} \cdot \begin{pmatrix} 1 \\ 0 \\ 0 \end{pmatrix} = 0 \quad \text{and} \quad \mathbf{N} \cdot \begin{pmatrix} 0 \\ 1 \\ 0 \end{pmatrix} = 0$$

这要求 $\mathbf{N} = \begin{pmatrix} 0 \\ 0 \\ \pm 1 \end{pmatrix}$，选择正方向得到：

This requires $\mathbf{N} = \begin{pmatrix} 0 \\ 0 \\ \pm 1 \end{pmatrix}$, choosing the positive direction yields:

$$\mathbf{N} = \begin{pmatrix} 0 \\ 0 \\ 1 \end{pmatrix}$$

**两种方法结果一致 / Both Methods Yield the Same Result**：

两种方法都得到相同的法向量：$\mathbf{N} = \begin{pmatrix} 0 \\ 0 \\ 1 \end{pmatrix}$

Both methods yield the same normal vector: $\mathbf{N} = \begin{pmatrix} 0 \\ 0 \\ 1 \end{pmatrix}$

**TBN 矩阵 / TBN Matrix**：

$$\text{TBN} = \begin{bmatrix} 1 & 0 & 0 \\ 0 & 1 & 0 \\ 0 & 0 & 1 \end{bmatrix}$$

这是一个单位矩阵，因为平面与 XY 平面重合。

This is an identity matrix because the plane coincides with the XY plane.

### 8.2 圆柱面 / Cylindrical Surface

考虑一个圆柱面：

Consider a cylindrical surface:

$$\mathbf{r}(u, v) = \begin{pmatrix} \cos(2\pi u) \\ \sin(2\pi u) \\ v \end{pmatrix}, \quad u, v \in [0, 1]$$

**计算切向量 / Compute Tangent Vectors**：

$$\mathbf{T}_u = \frac{\partial \mathbf{r}}{\partial u} = \begin{pmatrix} -2\pi \sin(2\pi u) \\ 2\pi \cos(2\pi u) \\ 0 \end{pmatrix}$$

$$\mathbf{T}_v = \frac{\partial \mathbf{r}}{\partial v} = \begin{pmatrix} 0 \\ 0 \\ 1 \end{pmatrix}$$

**计算法向量 - 方法 1：叉积法 / Compute Normal Vector - Method 1: Cross Product**：

$$\mathbf{N} = \mathbf{T}_u \times \mathbf{T}_v = \begin{pmatrix} -2\pi \sin(2\pi u) \\ 2\pi \cos(2\pi u) \\ 0 \end{pmatrix} \times \begin{pmatrix} 0 \\ 0 \\ 1 \end{pmatrix} = \begin{pmatrix} 2\pi \cos(2\pi u) \\ 2\pi \sin(2\pi u) \\ 0 \end{pmatrix}$$

归一化后：

After normalization:

$$\hat{\mathbf{N}} = \begin{pmatrix} \cos(2\pi u) \\ \sin(2\pi u) \\ 0 \end{pmatrix}$$

**计算法向量 - 方法 2：点法式法 / Compute Normal Vector - Method 2: Point-Normal Form**：

切平面方程为：

The tangent plane equation is:

$$\mathbf{r}(u, v) = \mathbf{r}(u_0, v_0) + (u - u_0)\mathbf{T}_u + (v - v_0)\mathbf{T}_v$$

法向量 $\mathbf{N}$ 必须满足：

The normal vector $\mathbf{N}$ must satisfy:

$$\mathbf{N} \cdot \mathbf{T}_u = 0 \quad \text{and} \quad \mathbf{N} \cdot \mathbf{T}_v = 0$$

即：

That is:

$$\mathbf{N} \cdot \begin{pmatrix} -2\pi \sin(2\pi u) \\ 2\pi \cos(2\pi u) \\ 0 \end{pmatrix} = 0 \quad \text{and} \quad \mathbf{N} \cdot \begin{pmatrix} 0 \\ 0 \\ 1 \end{pmatrix} = 0$$

第二个条件要求 $\mathbf{N}_z = 0$，第一个条件要求 $\mathbf{N}$ 垂直于 $\mathbf{T}_u$。

The second condition requires $\mathbf{N}_z = 0$, and the first condition requires $\mathbf{N}$ to be perpendicular to $\mathbf{T}_u$.

由于 $\mathbf{T}_u$ 在 XY 平面内，$\mathbf{N}$ 也必须在 XY 平面内且垂直于 $\mathbf{T}_u$：

Since $\mathbf{T}_u$ lies in the XY plane, $\mathbf{N}$ must also lie in the XY plane and be perpendicular to $\mathbf{T}_u$:

$$\mathbf{N} = \begin{pmatrix} \cos(2\pi u) \\ \sin(2\pi u) \\ 0 \end{pmatrix}$$

**两种方法结果一致 / Both Methods Yield the Same Result**：

两种方法都得到相同的归一化法向量：$\hat{\mathbf{N}} = \begin{pmatrix} \cos(2\pi u) \\ \sin(2\pi u) \\ 0 \end{pmatrix}$

Both methods yield the same normalized normal vector: $\hat{\mathbf{N}} = \begin{pmatrix} \cos(2\pi u) \\ \sin(2\pi u) \\ 0 \end{pmatrix}$

这表示法线指向圆柱的径向方向。

This indicates that the normal points in the radial direction of the cylinder.

---

## 9. 常见问题 / Common Questions

### 9.1 为什么需要归一化？/ Why Normalization?

切向量 $\mathbf{T}_u$ 和 $\mathbf{T}_v$ 的长度取决于参数化的"速度"：

The lengths of tangent vectors $\mathbf{T}_u$ and $\mathbf{T}_v$ depend on the "speed" of parameterization:

- 如果 UV 坐标范围是 $[0, 1]$，切向量长度可能很大
- 如果 UV 坐标范围是 $[0, 100]$，切向量长度可能很小

- If UV coordinates range from $[0, 1]$, tangent vector lengths may be large
- If UV coordinates range from $[0, 100]$, tangent vector lengths may be small

**归一化**确保 TBN 矩阵是**正交矩阵**，使得：
- 逆变换等于转置：$\text{TBN}^{-1} = \text{TBN}^T$
- 坐标变换保持长度（对于单位向量）

**Normalization** ensures that the TBN matrix is an **orthogonal matrix**, so that:
- Inverse transformation equals transpose: $\text{TBN}^{-1} = \text{TBN}^T$
- Coordinate transformation preserves length (for unit vectors)

### 9.2 flipSign 的数学含义 / Mathematical Meaning of flipSign

`flipSign` 处理**参数化的方向**（Orientation of Parameterization）：

`flipSign` handles the **orientation of parameterization**:

- 如果 UV 坐标被**镜像**（Mirrored），切向量的方向会翻转
- `flipSign = -1` 翻转副切线方向，确保 TBN 矩阵的手性正确

- If UV coordinates are **mirrored**, the direction of tangent vectors will flip
- `flipSign = -1` flips the bitangent direction to ensure correct handedness of the TBN matrix

**数学上 / Mathematically**：

$$\mathbf{B} = \text{flipSign} \cdot (\mathbf{N} \times \mathbf{T})$$

这确保了 TBN 矩阵形成正确的坐标系（左手或右手）。

This ensures that the TBN matrix forms the correct coordinate system (left-handed or right-handed).

---

## 10. 进一步学习 / Further Learning

### 10.1 推荐资源 / Recommended Resources

**书籍 / Books**：
- **Real-Time Rendering 4th Edition** - Chapter 6.7.2 "Tangent-Space Normal Mapping"
- **Mathematics for 3D Game Programming and Computer Graphics** - Chapter 6 "The Transformation Pipeline"
- **Differential Geometry of Curves and Surfaces** - 完整的微分几何理论

**在线资源 / Online Resources**：
- [LearnOpenGL - Normal Mapping](https://learnopengl.com/Advanced-Lighting/Normal-Mapping)
- [Lengyel's Tangent Space Calculation](http://www.terathon.com/code/tangent.html)
- [Scratchapixel - Tangent Space](https://www.scratchapixel.com/lessons/3d-basic-rendering/lighting-shading-basics-graphics-programming)

### 10.2 扩展到完整数学基础 / Extension to Complete Mathematical Foundation

要完全理解 TBN 矩阵，可以进一步学习：

To fully understand the TBN matrix, you can further study:

1. **流形理论 / Manifold Theory**：
   - 切空间作为流形上的切空间
   - 切丛（Tangent Bundle）

2. **微分形式 / Differential Forms**：
   - 1-形式（1-forms）和切向量
   - 外积（Wedge Product）

3. **黎曼几何 / Riemannian Geometry**：
   - 度量张量（Metric Tensor）
   - 克里斯托费尔符号（Christoffel Symbols）

---

## 11. 总结 / Summary

### 11.1 关键概念 / Key Concepts

1. **参数化曲面 / Parametric Surface**：$\mathbf{r}(u, v)$
2. **切向量 / Tangent Vectors**：$\mathbf{T}_u = \frac{\partial \mathbf{r}}{\partial u}$，$\mathbf{T}_v = \frac{\partial \mathbf{r}}{\partial v}$
3. **法向量 / Normal Vector**：$\mathbf{N} = \mathbf{T}_u \times \mathbf{T}_v$
4. **TBN 矩阵 / TBN Matrix**：$\text{TBN} = [\mathbf{T}, \mathbf{B}, \mathbf{N}]$

### 11.2 数学工具 / Mathematical Tools

- **偏导数 / Partial Derivatives**：计算切向量
- **叉积 / Cross Product**：计算法向量
- **正交矩阵 / Orthogonal Matrix**：TBN 矩阵的性质
- **坐标变换 / Coordinate Transformation**：切线空间 ↔ 世界空间

### 11.3 实际应用 / Practical Applications

- **法线贴图 / Normal Mapping**：切线空间法线 → 世界空间
- **视差映射 / Parallax Mapping**：世界空间方向 → 切线空间
- **各向异性光照 / Anisotropic Lighting**：在切线空间中计算

---

## 12. 参考资料 / References

### 12.1 相关文档 / Related Documentation

- [[VectorOperations#12 TBN 矩阵]] - TBN 矩阵的实用指南
- [[SpaceTransforms]] - Unity 空间变换实现

### 12.2 经典论文和教程 / Classic Papers and Tutorials

**Lengyel 的经典方法 / Lengyel's Classic Method**：

- **Eric Lengyel** - "Computing Tangent Space Basis Vectors for an Arbitrary Mesh"
  - 在线代码：[http://www.terathon.com/code/tangent.html](http://www.terathon.com/code/tangent.html)
  - 详细介绍了在离散网格中计算切向量和副切向量的方法
  - 提供了完整的 C++ 实现代码
  - 处理了镜像 UV、退化三角形等边界情况

- **Eric Lengyel** - "Computing Tangent Space Basis Vectors for an Arbitrary Mesh"
  - Online code: [http://www.terathon.com/code/tangent.html](http://www.terathon.com/code/tangent.html)
  - Detailed explanation of computing tangent and bitangent vectors in discrete meshes
  - Provides complete C++ implementation
  - Handles edge cases like mirrored UVs and degenerate triangles

**The Witness 教程 / The Witness Tutorial**：
- **Jonathan Blow** - The Witness 游戏开发中的切线空间计算
  - 针对游戏优化的切线空间计算方法
  - 考虑了性能和数值稳定性
  - 处理了复杂网格中的顶点共享问题

- **Jonathan Blow** - Tangent Space Calculation in The Witness Game Development
  - Game-optimized tangent space computation method
  - Considers performance and numerical stability
  - Handles vertex sharing in complex meshes

### 12.3 其他参考资料 / Other References

- [LearnOpenGL - Normal Mapping](https://learnopengl.com/Advanced-Lighting/Normal-Mapping) - 法线贴图和 TBN 矩阵教程
- Real-Time Rendering 4th Edition - Chapter 6.7.2 "Tangent-Space Normal Mapping"
- Mathematics for 3D Game Programming and Computer Graphics (Eric Lengyel) - Chapter 6 "The Transformation Pipeline"

