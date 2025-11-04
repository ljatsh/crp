# [[UnityInstancing]] - Unity GPU 实例化

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl`
- **主要职责**：提供 Unity GPU 实例化的宏和函数，支持传统实例化、程序化实例化和 DOTS 实例化
- **使用场景**：所有需要 GPU 实例化的着色器

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和宏定义
- **被依赖的文件**：使用实例化的着色器

## 实例化类型

### 传统实例化 (`INSTANCING_ON`)

- **定义**：Unity 的传统 GPU 实例化
- **支持平台**：D3D11、GLES3、GLCore、Metal、Vulkan、Switch 等
- **实现方式**：使用常量缓冲区存储实例数据

### 程序化实例化 (`PROCEDURAL_INSTANCING_ON`)

- **定义**：计算着色器驱动的实例化
- **实现方式**：通过计算着色器生成实例数据

### DOTS 实例化 (`DOTS_INSTANCING_ON`)

- **定义**：Unity DOTS（Data-Oriented Technology Stack）实例化
- **实现方式**：使用 `ByteAddressBuffer` 或 `cbuffer` 存储实例数据
- **依赖**：[[UnityDOTSInstancing]] - DOTS 实例化实现

### 立体渲染实例化 (`STEREO_INSTANCING_ON`)

- **定义**：VR 立体渲染的实例化
- **支持平台**：D3D11、GLCore、GLES3、Vulkan

## 核心宏定义

### `UNITY_VERTEX_INPUT_INSTANCE_ID`

- **功能**：在顶点着色器输入结构中声明实例 ID
- **定义**：
  ```hlsl
  #define UNITY_VERTEX_INPUT_INSTANCE_ID uint instanceID : SV_InstanceID;
  ```

### `UNITY_SETUP_INSTANCE_ID(v)`

- **功能**：设置全局实例 ID 变量
- **使用**：在顶点着色器中调用
- **实现**：
  ```hlsl
  void UnitySetupInstanceID(uint inputInstanceID)
  {
      unity_InstanceID = inputInstanceID + unity_BaseInstanceID;
  }
  ```

### `UNITY_DEFINE_INSTANCED_PROP(type, var)`

- **功能**：定义实例化属性（在常量缓冲区中）
- **使用**：
  ```hlsl
  UNITY_DEFINE_INSTANCED_PROP(float4, _Color);
  ```

### `UNITY_ACCESS_INSTANCED_PROP(arr, var)`

- **功能**：访问实例化属性
- **使用**：
  ```hlsl
  float4 color = UNITY_ACCESS_INSTANCED_PROP(unity_Instancing, _Color);
  ```

## 全局变量

```hlsl
static uint unity_InstanceID; // 当前实例 ID
int unity_BaseInstanceID;    // 批次起始实例 ID
int unity_InstanceCount;      // 批次实例数量
```

## 与其他模块的关系

- [[Common]]：依赖基础类型和宏定义
- [[UnityDOTSInstancing]]：DOTS 实例化实现

## 参考资料

- Unity GPU Instancing：https://docs.unity3d.com/Manual/GPUInstancing.html

