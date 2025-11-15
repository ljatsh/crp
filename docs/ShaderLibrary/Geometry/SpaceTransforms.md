# [[SpaceTransforms]] - 空间变换函数

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl`
- **主要职责**：提供各种坐标空间之间的变换函数
- **使用场景**：所有需要在不同坐标空间之间转换的着色器

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和数学函数
- **被依赖的文件**：几乎所有着色器都使用空间变换

## 核心函数解析

### `TransformObjectToWorld(float3 positionOS)`

- **签名**：`float3 TransformObjectToWorld(float3 positionOS)`
- **功能**：将对象空间位置转换到世界空间
- **实现原理**：
  ```hlsl
  float3 TransformObjectToWorld(float3 positionOS)
  {
      #if defined(SHADER_STAGE_RAY_TRACING)
      return mul(ObjectToWorld3x4(), float4(positionOS, 1.0)).xyz;
      #else
      return mul(GetObjectToWorldMatrix(), float4(positionOS, 1.0)).xyz;
      #endif
  }
  ```
- **数学原理**：
  - 使用 4x4 变换矩阵：`P_WS = M * P_OS`
  - 齐次坐标：`{x, y, z, 1}` 表示位置
- **性能注意事项**：
  - 光线追踪使用优化的 3x4 矩阵
  - 矩阵乘法在现代 GPU 上非常高效

### `TransformWorldToObject(float3 positionWS)`

- **签名**：`float3 TransformWorldToObject(float3 positionWS)`
- **功能**：将世界空间位置转换到对象空间
- **实现原理**：
  ```hlsl
  float3 TransformWorldToObject(float3 positionWS)
  {
      #if defined(SHADER_STAGE_RAY_TRACING)
      return mul(WorldToObject3x4(), float4(positionWS, 1.0)).xyz;
      #else
      return mul(GetWorldToObjectMatrix(), float4(positionWS, 1.0)).xyz;
      #endif
  }
  ```
- **数学原理**：
  - 使用逆变换矩阵：`P_OS = M⁻¹ * P_WS`
  - 通常预先计算逆矩阵以优化性能

### `TransformObjectToWorldDir(float3 dirOS, bool doNormalize = true)`

- **签名**：`float3 TransformObjectToWorldDir(float3 dirOS, bool doNormalize = true)`
- **功能**：将对象空间方向转换到世界空间
- **实现原理**：
  ```hlsl
  float3 TransformObjectToWorldDir(float3 dirOS, bool doNormalize = true)
  {
      #ifndef SHADER_STAGE_RAY_TRACING
      float3 dirWS = mul((float3x3)GetObjectToWorldMatrix(), dirOS);
      #else
      float3 dirWS = mul((float3x3)ObjectToWorld3x4(), dirOS);
      #endif
      if (doNormalize)
          return SafeNormalize(dirWS);
      return dirWS;
  }
  ```
- **数学原理**：
  - 方向向量只使用 3x3 旋转部分（忽略平移）
  - 如果物体有非均匀缩放，需要归一化
- **性能注意事项**：
  - `doNormalize = false` 可以跳过归一化（如果已知方向已归一化）

### `TransformObjectToWorldNormal(float3 normalOS, bool doNormalize = true)`

- **签名**：`float3 TransformObjectToWorldNormal(float3 normalOS, bool doNormalize = true)`
- **功能**：将对象空间法线转换到世界空间
- **实现原理**：
  ```hlsl
  float3 TransformObjectToWorldNormal(float3 normalOS, bool doNormalize = true)
  {
  #ifdef UNITY_ASSUME_UNIFORM_SCALING
      return TransformObjectToWorldDir(normalOS, doNormalize);
  #else
      float3 normalWS = mul(normalOS, (float3x3)GetWorldToObjectMatrix());
      if (doNormalize)
          return SafeNormalize(normalWS);
      return normalWS;
  #endif
  }
  ```
- **数学原理**：
  - **均匀缩放**：法线可以像方向一样变换（使用旋转部分）
  - **非均匀缩放**：法线必须使用逆转置矩阵：`N_WS = (M⁻¹)ᵀ * N_OS`
  - 代码中使用 `mul(normalOS, matrix)` 实现转置乘法
- **性能注意事项**：
  - `UNITY_ASSUME_UNIFORM_SCALING` 可以优化为简单的方向变换
  - 非均匀缩放需要额外的矩阵运算

### `CreateTangentToWorld(real3 normal, real3 tangent, real flipSign)`

> [!info] TBN 矩阵详解
> 关于 TBN 矩阵的详细定义、用途、构建方法和注意事项，参见 [[VectorOperations#12 TBN 矩阵]]。

- **签名**：`real3x3 CreateTangentToWorld(real3 normal, real3 tangent, real flipSign)`
- **功能**：创建从切线空间到世界空间的变换矩阵（TBN 矩阵）
- **实现原理**：
  ```hlsl
  real3x3 CreateTangentToWorld(real3 normal, real3 tangent, real flipSign)
  {
      real sgn = flipSign * GetOddNegativeScale();
      real3 bitangent = cross(normal, tangent) * sgn;
      return real3x3(tangent, bitangent, normal);
  }
  ```
- **数学原理**：
  - TBN 矩阵：`[T, B, N]`，其中 `B = N × T`
  - `flipSign` 控制副切线的方向（通常从纹理导入设置中获取）
  - `GetOddNegativeScale()` 处理负缩放的情况
- **使用示例**：
  ```hlsl
  real3x3 tangentToWorld = CreateTangentToWorld(normalWS, tangentWS, input.tangentSign);
  real3 normalTS = UnpackNormal(packedNormal);
  real3 normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
  ```

### `TransformTangentToWorld(float3 normalTS, real3x3 tangentToWorld, bool doNormalize = false)`

- **签名**：`real3 TransformTangentToWorld(float3 normalTS, real3x3 tangentToWorld, bool doNormalize = false)`
- **功能**：将切线空间法线转换到世界空间
- **实现原理**：
  ```hlsl
  real3 TransformTangentToWorld(float3 normalTS, real3x3 tangentToWorld, bool doNormalize = false)
  {
      real3 result = mul(normalTS, tangentToWorld);
      if (doNormalize)
          return SafeNormalize(result);
      return result;
  }
  ```
- **数学原理**：
  - 矩阵是行主序：`result = normalTS * tangentToWorld`
  - 等价于：`result = normalTS.x * T + normalTS.y * B + normalTS.z * N`
- **性能注意事项**：
  - 通常不需要归一化（如果输入法线已归一化且 TBN 矩阵正交）

### `TransformWorldToTangent(real3 normalWS, real3x3 tangentToWorld, bool doNormalize = true)`

- **签名**：`real3 TransformWorldToTangent(real3 normalWS, real3x3 tangentToWorld, bool doNormalize = true)`
- **功能**：将世界空间法线转换到切线空间（`TransformTangentToWorld` 的逆变换）
- **实现原理**：
  ```hlsl
  real3 TransformWorldToTangent(real3 normalWS, real3x3 tangentToWorld, bool doNormalize = true)
  {
      float3 row0 = tangentToWorld[0];
      float3 row1 = tangentToWorld[1];
      float3 row2 = tangentToWorld[2];
      
      float3 col0 = cross(row1, row2);
      float3 col1 = cross(row2, row0);
      float3 col2 = cross(row0, row1);
      
      float determinant = dot(row0, col0);
      real3x3 matTBN_I_T = real3x3(col0, col1, col2);
      real3 result = mul(matTBN_I_T, normalWS);
      if (doNormalize)
      {
          float sgn = determinant < 0.0 ? (-1.0) : 1.0;
          return SafeNormalize(sgn * result);
      }
      else
          return result / determinant;
  }
  ```
- **数学原理**：
  - 使用伴随矩阵计算逆矩阵：`M⁻¹ = adj(M) / det(M)`
  - `adj(M)` 的列是行的叉积
  - 行列式的符号影响法线方向（镜像变换）
- **性能注意事项**：
  - 比正向变换更昂贵（需要 3 个叉积和行列式计算）
  - 但保证与 `TransformTangentToWorld` 完全互逆

### `TransformWorldToTangentDir(real3 dirWS, real3x3 tangentToWorld, bool doNormalize = false)`

- **签名**：`real3 TransformWorldToTangentDir(real3 dirWS, real3x3 tangentToWorld, bool doNormalize = false)`
- **功能**：将世界空间方向转换到切线空间
- **实现原理**：
  ```hlsl
  real3 TransformWorldToTangentDir(real3 dirWS, real3x3 tangentToWorld, bool doNormalize = false)
  {
      real3 result = mul(tangentToWorld, dirWS);
      if (doNormalize)
          return SafeNormalize(result);
      return result;
  }
  ```
- **数学原理**：
  - 方向变换使用正向矩阵：`dirTS = tangentToWorld * dirWS`
  - 等价于：`dirTS = {dot(dirWS, T), dot(dirWS, B), dot(dirWS, N)}`
- **注意**：方向变换与法线变换不同（方向不需要逆转置）

### `GetObjectToWorldMatrix()` / `GetWorldToObjectMatrix()`

- **签名**：`float4x4 GetObjectToWorldMatrix()` / `float4x4 GetWorldToObjectMatrix()`
- **功能**：获取对象到世界/世界到对象的变换矩阵
- **实现原理**：
  ```hlsl
  float4x4 GetObjectToWorldMatrix()
  {
      return UNITY_MATRIX_M;
  }
  ```
- **数学原理**：
  - `UNITY_MATRIX_M` 是 Unity 提供的对象到世界矩阵
  - 如果启用相机相对渲染，矩阵已预平移
- **性能注意事项**：
  - 矩阵在常量缓冲区中，访问成本低
  - 光线追踪使用优化的 3x4 矩阵

### `GetCameraRelativePositionWS(float3 positionWS)` / `GetAbsolutePositionWS(float3 positionRWS)`

- **签名**：`float3 GetCameraRelativePositionWS(float3 positionWS)` / `float3 GetAbsolutePositionWS(float3 positionRWS)`
- **功能**：在绝对世界空间和相机相对世界空间之间转换
- **实现原理**：
  ```hlsl
  float3 GetCameraRelativePositionWS(float3 positionWS)
  {
  #if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
      positionWS -= _WorldSpaceCameraPos.xyz;
  #endif
      return positionWS;
  }
  ```
- **数学原理**：
  - 相机相对渲染：`P_RWS = P_WS - CameraPos`
  - 提高浮点精度（特别是远距离物体）
  - 矩阵已预应用相机偏移
- **性能注意事项**：
  - 条件编译确保在禁用时无开销
  - 减法操作非常快

## 与其他模块的关系

- [[Common]]：依赖基础类型和 `SafeNormalize` 函数
- [[CommonMaterial]]：使用切线空间变换处理法线贴图
- [[ParallaxMapping]]：使用切线空间变换计算视差偏移

## 参考资料

- Unity 官方文档：https://docs.unity3d.com/Manual/SL-DataTypesAndPrecision.html
- Real-Time Rendering 4th Edition：https://www.realtimerendering.com/

