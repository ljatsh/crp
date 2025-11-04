# [[UnityDOTSInstancing]] - DOTS 实例化

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityDOTSInstancing.hlsl`
- **主要职责**：实现 Unity DOTS（Data-Oriented Technology Stack）实例化系统
- **使用场景**：使用 DOTS 的着色器

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和宏定义
- **被依赖的文件**：[[UnityInstancing]] - 使用 DOTS 实例化

## 核心概念

### 类型规范（Type Spec）

DOTS 实例化使用类型规范字符串标识数据类型：
- `F4`：float（4 字节）
- `F8`：float2（8 字节）
- `F16`：float4（16 字节）
- `I4`：int（4 字节）
- `U4`：uint（4 字节）
- `H2`：half（2 字节）

### 元数据（Metadata）

元数据包含实例数据的地址和类型信息：
- 地址：在 `ByteAddressBuffer` 或 `cbuffer` 中的偏移
- 类型：类型规范字符串

## 核心函数

### `ComputeDOTSInstanceDataAddress(uint metadata, uint stride)`

- **签名**：`uint ComputeDOTSInstanceDataAddress(uint metadata, uint stride)`
- **功能**：计算实例数据的地址
- **实现原理**：
  ```hlsl
  uint ComputeDOTSInstanceDataAddress(uint metadata, uint stride)
  {
      // 从元数据中提取地址和类型信息
      uint instanceID = unity_InstanceID;
      uint address = (metadata >> 16) | ((instanceID * stride) << 16);
      return address;
  }
  ```

### `LoadDOTSInstancedData_float4x4(uint metadata)`

- **签名**：`float4x4 LoadDOTSInstancedData_float4x4(uint metadata)`
- **功能**：从 DOTS 实例数据中加载 float4x4 矩阵
- **实现原理**：
  ```hlsl
  float4x4 LoadDOTSInstancedData_float4x4(uint metadata)
  {
      uint address = ComputeDOTSInstanceDataAddress(metadata, 64); // 16 floats * 4 bytes
      return LoadMatrixFromBuffer(address);
  }
  ```

## 与其他模块的关系

- [[Common]]：依赖基础类型
- [[UnityInstancing]]：集成到 Unity 实例化系统

## 参考资料

- Unity DOTS：https://docs.unity3d.com/Packages/com.unity.entities@latest

