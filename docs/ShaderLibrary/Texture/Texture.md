# [[Texture]] - 纹理采样抽象

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl`
- **主要职责**：提供 C# 风格的纹理抽象结构（UnityTexture2D、UnityTexture2DArray 等）和采样方法
- **使用场景**：所有需要纹理采样的着色器

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和宏定义
- **被依赖的文件**：几乎所有着色器都使用纹理采样

## 核心结构体

### `UnityTexture2D`

- **定义**：2D 纹理的抽象结构
- **实现原理**：
  ```hlsl
  struct UnityTexture2D
  {
      TEXTURE2D(tex);
      UNITY_BARE_SAMPLER(samplerstate);
      float4 texelSize;
      float4 scaleTranslate; // UV 缩放和平移 (scale.xy, offset.zw)
      
      // 采样方法
      float4 Sample(UnitySamplerState s, float2 uv);
      float4 SampleLevel(UnitySamplerState s, float2 uv, float lod);
      float4 SampleBias(UnitySamplerState s, float2 uv, float bias);
      float4 SampleGrad(UnitySamplerState s, float2 uv, float2 dpdx, float2 dpdy);
      
      // UV 变换
      float2 GetTransformedUV(float2 uv);
  };
  ```
- **功能**：
  - 封装纹理对象和采样器状态
  - 提供统一的采样接口
  - 支持 UV 缩放和平移
  - 平台抽象（GLES2 特殊处理）

### `UnitySamplerState`

- **定义**：采样器状态的抽象
- **实现原理**：
  ```hlsl
  struct UnitySamplerState
  {
      UNITY_BARE_SAMPLER(samplerstate);
  };
  ```
- **平台差异**：
  - D3D11/Metal/Vulkan：使用独立的 `SamplerState`
  - GLES2：空结构（纹理和采样器绑定）

### `UnityTexture2DArray`

- **定义**：2D 纹理数组的抽象
- **功能**：类似 `UnityTexture2D`，但支持数组索引

### `UnityTextureCube`

- **定义**：立方贴图的抽象
- **功能**：支持立方贴图采样

### `UnityTexture3D`

- **定义**：3D 纹理的抽象
- **功能**：支持 3D 纹理采样（探针体积等）

## 核心函数

### `GetTransformedUV(float2 uv)`

- **签名**：`float2 GetTransformedUV(float2 uv)`
- **功能**：应用 UV 缩放和平移变换
- **实现原理**：
  ```hlsl
  float2 GetTransformedUV(float2 uv)
  {
      return uv * scaleTranslate.xy + scaleTranslate.zw;
  }
  ```
- **数学原理**：
  - `scaleTranslate = (scale.x, scale.y, offset.x, offset.y)`
  - 变换：`uv' = uv * scale + offset`

### `UnityBuildTexture2DStruct(n)`

- **签名**：`UnityTexture2D UnityBuildTexture2DStruct(n)`
- **功能**：从 Unity 纹理宏构建 `UnityTexture2D` 结构
- **使用方式**：
  ```hlsl
  TEXTURE2D(_MainTex);
  SAMPLER(sampler_MainTex);
  float4 _MainTex_TexelSize;
  float4 _MainTex_ST; // Scale-Translate
  
  UnityTexture2D mainTex = UnityBuildTexture2DStruct(_MainTex);
  float4 color = mainTex.Sample(sampler_MainTex, uv);
  ```

## 平台特定处理

### GLES2 支持

GLES2 不支持分离的纹理和采样器，因此：
- `UNITY_BARE_SAMPLER` 在 GLES2 上为空结构
- 采样函数内部使用 `tex2D` 而不是 `Sample`

### Gather 操作

支持 Gather 操作的平台（D3D11、Metal 等）提供：
- `Gather()`：收集 4 个相邻像素
- `GatherRed/Green/Blue/Alpha()`：收集特定通道

## 使用示例

```hlsl
// 传统方式
float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

// 使用 UnityTexture2D
UnityTexture2D mainTex = UnityBuildTexture2DStruct(_MainTex);
float4 color = mainTex.Sample(sampler_MainTex, uv);

// 使用变换的 UV
float2 transformedUV = mainTex.GetTransformedUV(uv);
float4 color = mainTex.Sample(sampler_MainTex, transformedUV);
```

## 与其他模块的关系

- [[Common]]：依赖基础类型和宏定义
- [[Packing]]：纹理数据打包/解包
- [[VirtualTexturing]]：虚拟纹理系统

## 参考资料

- Unity 纹理文档：https://docs.unity3d.com/Manual/Textures.html
- HLSL 纹理采样：https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-texture-sampling

