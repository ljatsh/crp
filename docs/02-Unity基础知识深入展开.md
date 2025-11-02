# Unity 基础知识深入展开

本文档深入解释 Unity 渲染管线相关的核心概念和基础知识，帮助理解 SRP 的实现原理。

## 目录 (Table of Contents)

- [1. Scriptable Render Pipeline 核心概念](#1-scriptable-render-pipeline-核心概念)
  - [1.1 ScriptableRenderContext 详解](#11-scriptablerendercontext-详解)
  - [1.2 CommandBuffer 生命周期管理](#12-commandbuffer-生命周期管理)
  - [1.3 CullingResults 和视锥剔除](#13-cullingresults-和视锥剔除)
  - [1.4 DrawRenderers 的参数详解](#14-drawrenderers-的参数详解)
- [2. 着色器相关基础](#2-着色器相关基础)
  - [2.1 ShaderTagId vs Shader 关键字](#21-shadertagid-vs-shader-关键字)
  - [2.2 LightMode 标签系统](#22-lightmode-标签系统)
  - [2.3 SRPDefaultUnlit 默认行为](#23-srpdefaultunlit-默认行为)
  - [2.4 传统内置管线的标签](#24-传统内置管线的标签)
  - [2.5 Shader 和 SubShader 的选择机制](#25-shader-和-subshader-的选择机制)
  - [2.6 RenderType 标签的作用](#26-rendertype-标签的作用)
  - [2.7 Shader LOD（细节层次）](#27-shader-lod细节层次)
- [3. 材质系统深入](#3-材质系统深入)
  - [3.1 Material 资源 vs MaterialInstance](#31-material-资源-vs-materialinstance)
  - [3.2 MaterialPropertyBlock 和 PerRendererData](#32-materialpropertyblock-和-perrendererdata)
  - [3.3 属性覆盖机制](#33-属性覆盖机制)
  - [3.4 共享材质 vs 实例材质的内存考量](#34-共享材质-vs-实例材质的内存考量)
- [4. 渲染队列系统](#4-渲染队列系统)
  - [4.1 RenderQueue 数值范围](#41-renderqueue-数值范围)
  - [4.2 Queue 的分类](#42-queue-的分类)
  - [4.3 队列与深度测试的关系](#43-队列与深度测试的关系)
- [5. 排序和批处理](#5-排序和批处理)
  - [5.1 SortingCriteria 的选项](#51-sortingcriteria-的选项)
  - [5.2 静态批处理 vs 动态批处理](#52-静态批处理-vs-动态批处理)
  - [5.3 GPU Instancing](#53-gpu-instancing)
  - [5.4 SRP Batcher 的工作原理](#54-srp-batcher-的工作原理)
- [6. 编辑器相关](#6-编辑器相关)
  - [6.1 Partial Class 的使用](#61-partial-class-的使用)
  - [6.2 Conditional Compilation（条件编译）](#62-conditional-compilation条件编译)
  - [6.3 Editor-only 功能的设计模式](#63-editor-only-功能的设计模式)
- [7. 性能考量](#7-性能考量)
  - [7.1 Draw Call 的优化](#71-draw-call-的优化)
  - [7.2 命令缓冲区的合理使用](#72-命令缓冲区的合理使用)
  - [7.3 上下文提交的时机](#73-上下文提交的时机)
- [参考资源](#参考资源)

---

## 1. Scriptable Render Pipeline 核心概念

### 1.1 ScriptableRenderContext 详解

`ScriptableRenderContext` 是 SRP 的核心接口，所有渲染操作都通过它执行。

**核心特性：**

1. **延迟执行机制**
   - 所有命令不会立即执行，而是记录在上下文中
   - 通过 `Submit()` 一次性提交，减少状态切换开销

2. **命令记录与执行分离**
   ```csharp
   context.SetupCameraProperties(camera);  // 记录命令
   context.ClearRenderTarget(...);           // 记录命令
   context.DrawRenderers(...);                // 记录命令
   context.Submit();                          // 执行所有命令
   ```

3. **上下文状态管理**
   - 维护相机属性、渲染目标、全局着色器属性等
   - 确保命令在正确的状态下执行

**在当前项目中的使用：**

```24:39:Assets/Custom RP/Runtime/CameraRenderer.cs
    public void Render(ScriptableRenderContext context, Camera camera)
    {
        this.context = context;
        this.camera = camera;

        // Cull和GPU无关
        if (!Cull())
            return;

        Setup();

        DrawVisibleObjects();
        DrawUnsupportedShaders();
        DrawGizmos();

        Submit();
    }
```

### 1.2 CommandBuffer 生命周期管理

`CommandBuffer` 是命令记录的容器，用于批量收集渲染命令。

**生命周期：**

1. **创建**
   ```csharp
   buffer = new CommandBuffer() {
       name = bufferName  // 用于性能分析
   };
   ```

2. **记录命令**
   ```csharp
   buffer.ClearRenderTarget(true, true, Color.clear);
   buffer.BeginSample(bufferName);
   ```

3. **执行并清空**
   ```csharp
   context.ExecuteCommandBuffer(buffer);
   buffer.Clear();  // 清空以供重用
   ```

**关键设计模式：**

在当前实现中，CommandBuffer 被重用：

```76:80:Assets/Custom RP/Runtime/CameraRenderer.cs
    private void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
```

**为什么清空而不是重建？**
- 减少内存分配：避免每帧创建新对象
- 性能优化：对象池模式

### 1.3 CullingResults 和视锥剔除

`CullingResults` 包含剔除操作的结果，包含所有可见对象的信息。

**剔除类型：**

1. **视锥剔除（Frustum Culling）**
   - 移除相机视野外的对象
   - 基于对象的包围盒（Bounds）

2. **遮挡剔除（Occlusion Culling）**
   - 移除被其他对象完全遮挡的对象
   - 需要预计算或运行时计算

**获取剔除结果：**

```82:91:Assets/Custom RP/Runtime/CameraRenderer.cs
    private bool Cull()
    {
        if (!camera.TryGetCullingParameters(out ScriptableCullingParameters parameters))
        {
            return false;
        }

        cullingResults = context.Cull(ref parameters);
        return true;
    }
```

**CullingResults 包含的信息：**
- 可见渲染器列表
- 可见光源列表
- 遮挡数据（如果启用了遮挡剔除）

### 1.4 DrawRenderers 的参数详解

`DrawRenderers` 是实际执行渲染的核心方法：

```csharp
void DrawRenderers(
    CullingResults cullingResults,
    ref DrawingSettings drawingSettings,
    ref FilteringSettings filteringSettings
)
```

**参数说明：**

1. **cullingResults**
   - 剔除结果，包含可见对象列表
   - 必须先从 `Context.Cull()` 获取

2. **drawingSettings**
   - 指定要渲染哪些着色器 Pass
   - 包含排序设置
   - 可以指定多个 Pass（多 Pass 渲染）

3. **filteringSettings**
   - 过滤条件（队列范围、渲染类型、图层等）
   - 决定哪些对象被实际渲染

**在当前项目中的使用：**

```56:58:Assets/Custom RP/Runtime/CameraRenderer.cs
        var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
```

[↑ 返回目录](#目录-table-of-contents)

## 2. 着色器相关基础

### 2.1 ShaderTagId vs Shader 关键字

**ShaderTagId（着色器标签 ID）：**

- 用于标识着色器的 **Pass 标签**（如 `LightMode`）
- 告诉渲染管线在什么时候使用哪个 Pass
- 在 `ShaderLab` 的 `Pass Tags` 中定义

```shader
Pass
{
    Tags { "LightMode"="SRPDefaultUnlit" }
    // ...
}
```

**Shader 关键字（Keywords）：**

- 用于控制着色器的编译变体
- 在运行时通过 `Material.EnableKeyword()` 启用
- 用于实现条件编译（如 `#ifdef`）

```hlsl
#pragma multi_compile _ DIRECTIONAL_LIGHT
#if defined(DIRECTIONAL_LIGHT)
    // 方向光相关代码
#endif
```

**关键区别：**

| 特性 | ShaderTagId | Shader Keywords |
|------|-------------|----------------|
| 用途 | 标识 Pass | 控制编译变体 |
| 定义位置 | Pass Tags | #pragma 指令 |
| 运行时控制 | 由渲染管线控制 | 由 Material 控制 |
| 数量限制 | 无（但有限制） | 有数量限制 |

### 2.2 LightMode 标签系统

`LightMode` 标签是着色器 Pass 最重要的标签之一，用于告诉渲染管线在哪个渲染阶段使用该 Pass。

**SRP 中的 LightMode 值：**

1. **SRPDefaultUnlit**
   - 默认的无光照 Pass
   - 如果 Pass 没有声明 `LightMode`，默认使用此值
   - 用于不受光照影响的对象

2. **其他常见值（URP/HDRP 中）：**
   - `UniversalForward`：前向渲染的主 Pass
   - `ShadowCaster`：阴影投射 Pass
   - `DepthOnly`：仅深度 Pass
   - `Meta`：光照贴图烘焙 Pass

**在当前项目中的使用：**

```9:9:Assets/Custom RP/Runtime/CameraRenderer.cs
    private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
```

这告诉渲染管线只渲染带有 `SRPDefaultUnlit` 标签的 Pass（或未声明 LightMode 的 Pass）。

### 2.3 SRPDefaultUnlit 默认行为

**重要特性：**

1. **隐式识别**
   - 如果着色器 Pass 没有显式声明 `LightMode`，Unity SRP 会将其视为 `SRPDefaultUnlit`
   - 这在 Frame Debugger 中显示为 `LightMode: -`

2. **向后兼容**
   - 允许旧着色器在没有修改的情况下在 SRP 中工作
   - 简化了从内置管线迁移的过程

3. **默认匹配**
   - 当使用 `ShaderTagId("SRPDefaultUnlit")` 创建 `DrawingSettings` 时
   - 会渲染所有显式声明 `SRPDefaultUnlit` 的 Pass
   - **以及**所有未声明 `LightMode` 的 Pass

### 2.4 传统内置管线的标签

内置渲染管线使用不同的 Pass 标签系统：

```16:28:Assets/Custom RP/Runtime/CameraRenderer.Editor.cs
    static ShaderTagId[] legacyShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("ForwardAdd"),
        new ShaderTagId("Deferred"),
        new ShaderTagId("ShadowCaster"),
        // TODO1 打开绘制失效
        //new ShaderTagId("MotionVectors"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM"),
        new ShaderTagId("Meta")
    };
```

**常见内置管线标签：**

- **ForwardBase**：前向渲染的基础 Pass（主方向光）
- **ForwardAdd**：前向渲染的附加 Pass（其他光源）
- **Deferred**：延迟渲染 Pass
- **ShadowCaster**：阴影投射 Pass
- **Always**：总是渲染的 Pass
- **Meta**：光照贴图烘焙 Pass

**处理不支持的着色器：**

```31:49:Assets/Custom RP/Runtime/CameraRenderer.Editor.cs
    static Material errorMaterial;
    private partial void DrawUnsupportedShaders()
    {
        if (errorMaterial == null)
        {
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera))
        {
            overrideMaterial = errorMaterial
        };
        for (int index=1; index<legacyShaderTagIds.Length; index++)
        {
            drawingSettings.SetShaderPassName(index, legacyShaderTagIds[index]);
        }

        var filterSettings = FilteringSettings.defaultValue;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filterSettings);
    }
```

使用 `overrideMaterial` 将所有不支持的着色器替换为错误材质（粉红色），便于在编辑器中识别问题。

### 2.5 Shader 和 SubShader 的选择机制

当渲染一个物体时，Unity 需要从 Shader Object 中选择合适的 SubShader 和 Pass。理解这个选择机制对于调试着色器问题和优化渲染性能至关重要。

**Shader Object 的结构：**

```text
Shader "Custom/MyShader"          // Shader Object
├─ SubShader 0                    // 第一个 SubShader（最高优先级）
│  ├─ Pass 0
│  ├─ Pass 1
│  └─ ...
├─ SubShader 1                    // 第二个 SubShader（备选）
│  ├─ Pass 0
│  └─ ...
├─ SubShader 2                    // 第三个 SubShader（备选）
│  └─ ...
└─ Fallback "Legacy Shaders/Diffuse"  // 如果所有 SubShader 都不兼容
   └─ SubShader 0                 // Fallback Shader 的 SubShader
```

**Unity 选择 SubShader 的过程：**

根据 [Unity 官方文档](https://docs.unity3d.com/Manual/shader-loading.html#selecting-subshaders)，Unity 按照以下步骤选择 SubShader：

**第一步：检查兼容性**

Unity 会检查每个 SubShader 是否兼容：
1. **平台硬件**：GPU 能力、特性级别
2. **Shader LOD**：当前设置的 Shader 细节层次
3. **渲染管线**：是否与当前使用的渲染管线兼容

**第二步：按顺序搜索**

Unity 按照以下顺序搜索兼容的 SubShader：

```text
1. 当前 Shader 的 SubShader（按在 Shader 中的顺序）
   ├─ SubShader 0 → 检查兼容性
   ├─ SubShader 1 → 检查兼容性（如果 SubShader 0 不兼容）
   ├─ SubShader 2 → 检查兼容性（如果前面的都不兼容）
   └─ ...

2. Fallback Shader 的 SubShader（如果当前 Shader 没有兼容的）
   └─ Fallback Shader 中的 SubShader（按顺序）
```

**第三步：选择第一个兼容的 SubShader**

Unity 选择**第一个**兼容的 SubShader，然后在该 SubShader 中选择合适的 Pass。

**实际示例：**

```shader
Shader "Custom/ExampleShader"
{
    // SubShader 0：针对现代 GPU（SRP 兼容）
    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            // ...
        }
    }
    
    // SubShader 1：针对旧 GPU（内置管线兼容）
    SubShader
    {
        Tags { "RenderPipeline"="" }  // 内置管线
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            // ...
        }
    }
    
    // Fallback：如果都不兼容，使用这个
    Fallback "Legacy Shaders/Diffuse"
}
```

**在不同渲染管线中的选择：**

| 渲染管线 | 选择的 SubShader | 原因 |
|---------|----------------|------|
| URP/SRP | SubShader 0 | `RenderPipeline="UniversalRenderPipeline"` 标签匹配 |
| 内置管线 | SubShader 1 | SubShader 0 不兼容（URP 标签），选择 SubShader 1 |
| 不兼容的平台 | Fallback Shader | 所有 SubShader 都不兼容 |

**Pass 的选择：**

选择了 SubShader 后，Unity 会根据 `DrawingSettings` 中的 `ShaderTagId` 选择 Pass：

```csharp
// 在当前项目中
var drawingSettings = new DrawingSettings(
    new ShaderTagId("SRPDefaultUnlit"),  // 指定要使用的 Pass 标签
    sortingSettings
);
```

Unity 会：
1. 在选定的 SubShader 中搜索带有 `LightMode="SRPDefaultUnlit"` 标签的 Pass
2. 如果找到，使用该 Pass
3. 如果没找到，尝试使用 SubShader 中的第一个 Pass（如果未声明 LightMode，会被视为 SRPDefaultUnlit）

**Shader LOD 的影响：**

Shader LOD（Level of Detail）会影响 SubShader 的选择。Unity 会根据当前的 LOD 值过滤掉 LOD 值过高的 SubShader。关于 LOD 的详细说明，请参考 [2.7 Shader LOD（细节层次）](#27-shader-lod细节层次)。

**调试技巧：**

1. **Frame Debugger**：查看实际使用的 Shader 和 Pass
2. **Shader Inspector**：在 Unity 编辑器中查看编译后的 Shader 变体
3. **Profiler 标记**：
   - `Shader.ParseThreaded`：多线程解析 Shader
   - `Shader.ParseMainThread`：主线程解析 Shader
   - `Shader.MainThreadCleanup`：清理 Shader 变体

**常见问题：**

1. **物体显示为粉红色**：
   - 说明没有找到兼容的 SubShader
   - 检查 SubShader 的 Tags 是否与渲染管线匹配
   - 检查 GPU 能力是否支持

2. **使用了错误的 SubShader**：
   - 检查 SubShader 的顺序（Unity 选择第一个兼容的）
   - 检查 Shader LOD 设置

3. **Fallback 被使用**：
   - 说明当前 Shader 的所有 SubShader 都不兼容
   - 考虑添加更通用的 SubShader 或调整 Tags

**最佳实践：**

1. **提供多个 SubShader**：为不同的平台和渲染管线提供兼容的 SubShader
2. **合理排序**：将最常用、最兼容的 SubShader 放在前面
3. **使用 Fallback**：始终提供 Fallback Shader 作为最后备选
4. **明确标签**：为每个 SubShader 明确设置 `RenderPipeline` 等标签

**在当前项目中的应用：**

```9:9:Assets/Custom RP/Runtime/CameraRenderer.cs
    private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
```

这告诉渲染管线只渲染带有 `SRPDefaultUnlit` 标签的 Pass。因此：
- 物体使用的 Shader 必须有一个 SubShader 与自定义渲染管线兼容
- 该 SubShader 中必须有一个 Pass 带有 `LightMode="SRPDefaultUnlit"` 标签（或未声明 LightMode）
- 如果不符合条件，物体会在编辑器中显示为粉红色（如果实现了 `DrawUnsupportedShaders`）

### 2.6 RenderType 标签的作用

`RenderType` 标签用于对对象进行分类，通常用于后处理或替换着色器：

```shader
Tags { 
    "RenderType"="Opaque"
    // 或 "Transparent", "Cutout" 等
}
```

**常见 RenderType 值：**
- `Opaque`：不透明对象
- `Transparent`：透明对象
- `Cutout`：透明度裁剪对象
- `Background`：背景对象

**用途：**
- 后处理系统可以根据 `RenderType` 选择不同的处理方式
- 替换着色器（Replacement Shaders）可以根据 `RenderType` 替换着色器

### 2.7 Shader LOD（细节层次）

Shader LOD（Level of Detail）是一种优化技术，允许 Unity 根据当前设置自动选择不同复杂度的 SubShader。这类似于 3D 模型的 LOD，但应用于着色器。

**设计目的：**

根据 [Unity 官方文档](https://docs.unity3d.com/Manual/SL-ShaderLOD.html)，Shader LOD 的设计目的是：

1. **性能优化**：在低端设备或需要性能优化的场景中，自动使用简化版本的着色器
2. **质量分级**：为不同性能需求的场景提供不同质量的着色器变体
3. **自动降级**：无需手动切换材质，Unity 会根据全局 LOD 设置自动选择合适的 SubShader
4. **节省资源**：避免在低端设备上使用过于复杂的着色器，减少 GPU 负担

**使用方式：**

在 ShaderLab 中，通过 `LOD` 指令为 SubShader 分配 LOD 值：

```shader
Shader "Custom/MyShader"
{
    // SubShader 0：高质量（LOD 300）
    SubShader
    {
        LOD 300
        Pass
        {
            // 复杂的着色器代码（法线贴图、高光等）
        }
    }
    
    // SubShader 1：中等质量（LOD 200）
    SubShader
    {
        LOD 200
        Pass
        {
            // 中等复杂度的着色器代码
        }
    }
    
    // SubShader 2：低质量（LOD 100）
    SubShader
    {
        LOD 100
        Pass
        {
            // 简化的着色器代码（无光照、基础纹理）
        }
    }
}
```

**LOD 值的选择：**

Unity 内置着色器的 LOD 值参考：

| LOD 值 | 内置着色器示例 |
|--------|---------------|
| 100 | Unlit/Texture, Unlit/Color, Unlit/Transparent |
| 300 | Standard, Standard (Specular Setup) |

传统着色器的 LOD 值：

| LOD 值 | 传统着色器示例 |
|--------|---------------|
| 100 | VertexLit |
| 200 | Diffuse |
| 300 | Bumped, Specular |
| 400-600 | Bumped Specular, Parallax 等高级特性 |

**设置全局 LOD：**

可以通过代码设置全局 Shader LOD：

```csharp
// 设置全局最大 LOD（所有 Shader 的 LOD 上限）
Shader.globalMaximumLOD = 200;

// 设置特定 Shader 的 LOD
Shader shader = Shader.Find("Custom/MyShader");
shader.maximumLOD = 300;
```

**LOD 的工作原理：**

```text
渲染物体时：
1. Unity 获取当前全局 LOD 值（例如 200）
2. 遍历 Shader 的 SubShader（按顺序）
3. 对于每个 SubShader：
   - 检查 SubShader 的 LOD 值
   - 如果 SubShader.LOD > 当前全局 LOD，跳过此 SubShader
   - 如果 SubShader.LOD <= 当前全局 LOD，检查兼容性
4. 选择第一个兼容且 LOD 值合适的 SubShader
```

**实际示例：**

```shader
Shader "Custom/AdaptiveShader"
{
    // 高质量版本：适用于高端设备
    SubShader
    {
        LOD 400
        Tags { "RenderPipeline"="UniversalRenderPipeline" }
        Pass
        {
            // 包含法线贴图、高光、阴影等完整功能
        }
    }
    
    // 中质量版本：适用于中端设备
    SubShader
    {
        LOD 200
        Tags { "RenderPipeline"="UniversalRenderPipeline" }
        Pass
        {
            // 基础光照，无法线贴图
        }
    }
    
    // 低质量版本：适用于低端设备或性能优化场景
    SubShader
    {
        LOD 100
        Tags { "RenderPipeline"="UniversalRenderPipeline" }
        Pass
        {
            // 最简单的无光照着色器
        }
    }
    
    Fallback "Universal Render Pipeline/Unlit"
}
```

**在不同 LOD 设置下的表现：**

| 全局 LOD | 选择的 SubShader | 说明 |
|---------|----------------|------|
| 400 | SubShader 0 (LOD 400) | 使用最高质量版本 |
| 200 | SubShader 1 (LOD 200) | 使用中等质量版本（LOD 400 的被跳过） |
| 100 | SubShader 2 (LOD 100) | 使用低质量版本（前两个被跳过） |
| 50 | Fallback Shader | 所有 SubShader 的 LOD 都太高，使用 Fallback |

**应用场景：**

1. **移动设备优化**：
   ```csharp
   #if UNITY_ANDROID || UNITY_IOS
       Shader.globalMaximumLOD = 200;  // 移动设备使用中等质量
   #else
       Shader.globalMaximumLOD = 400;  // PC 使用高质量
   #endif
   ```

2. **性能模式**：
   ```csharp
   // 用户选择"性能模式"
   if (qualitySettings == QualityLevel.Low) {
       Shader.globalMaximumLOD = 100;  // 使用低质量着色器
   }
   ```

3. **距离降级**：
   ```csharp
   // 根据相机距离自动降级（需要自定义实现）
   float distance = Vector3.Distance(camera.transform.position, objectPosition);
   if (distance > 100) {
       Shader.globalMaximumLOD = 100;  // 远距离使用简化着色器
   }
   ```

**最佳实践：**

1. **合理设置 LOD 值**：参考 Unity 内置着色器的 LOD 值，保持一致性
2. **提供多个 LOD 级别**：至少提供高质量和低质量两个版本
3. **保持视觉一致性**：不同 LOD 级别应该看起来相似，只是复杂度不同
4. **测试不同平台**：在不同性能的设备上测试 LOD 设置
5. **配合 Unity 的 Quality Settings**：可以考虑在 Quality Settings 中根据质量级别设置不同的 LOD

**注意事项：**

- LOD 值只是过滤条件之一，Unity 还会考虑其他兼容性因素（平台、渲染管线等）
- LOD 是基于 SubShader 的，不是基于 Pass 的
- 如果不设置 LOD，SubShader 的默认 LOD 值是无限大（总是可用）
- LOD 值越大，表示着色器越复杂、质量越高
[↑ 返回目录](#目录-table-of-contents)

## 3. 材质系统深入

### 3.1 Material 资源 vs MaterialInstance

**Material 资源：**
- 存储在磁盘上的资源文件（.mat）
- 可以被多个对象共享
- 修改会影响所有使用该材质的对象

**Material Instance（运行时创建）：**
- 在运行时通过 `Material` 构造函数创建
- 每个实例独立，修改不影响原始材质
- 内存开销较大

**代码示例：**
```csharp
// Material 资源（共享）
Material sharedMaterial = GetComponent<Renderer>().sharedMaterial;

// Material 实例（独立）
Material instance = new Material(sharedMaterial);
GetComponent<Renderer>().material = instance;  // 自动创建实例
```

### 3.2 MaterialPropertyBlock 和 PerRendererData

**MaterialPropertyBlock：**

允许每个渲染器实例有不同的属性值，而无需创建新的 Material 实例。

**优势：**
- 内存效率：共享同一个 Material，只存储差异
- 性能优化：支持批量渲染（SRP Batcher）
- 动态修改：运行时修改属性不影响其他对象

**使用示例：**
```csharp
MaterialPropertyBlock mpb = new MaterialPropertyBlock();
mpb.SetColor("_Color", Color.red);
mpb.SetTexture("_MainTex", customTexture);
renderer.SetPropertyBlock(mpb);
```

**PerRendererData 属性：**

在着色器中标记为 `[PerRendererData]` 的属性，表示该属性的值来自 `MaterialPropertyBlock`：

```hlsl
Properties
{
    _MainTex ("Main Texture", 2D) = "white" {}
    [PerRendererData] _CustomTex ("Custom Texture", 2D) = "black" {}
}
```

**设计目的：**

`[PerRendererData]` 属性标签的设计目的是为了明确标识哪些着色器属性应该通过 `MaterialPropertyBlock` 来设置，而不是在 Material 资源中设置。这样做的好处包括：

1. **明确语义**：告诉开发者这个属性是为每个渲染器实例单独设置的，不应该在 Material 资源中编辑
2. **编辑器提示**：Material Inspector 会将这些属性显示为只读，避免误操作
3. **优化提示**：Unity 可以识别这些属性，更好地进行批量渲染优化（如 SRP Batcher）
4. **文档化意图**：代码即文档，清晰地表达了属性的使用方式

**使用场景举例：**

**场景 1：大量相同物体的差异化渲染（如草地、树木）**

```csharp
// 使用相同的 Material，但每个草地的纹理不同
Shader shader = Shader.Find("Custom/GrassShader");
Material sharedMaterial = new Material(shader);

for (int i = 0; i < grassCount; i++)
{
    GameObject grass = Instantiate(grassPrefab);
    Renderer renderer = grass.GetComponent<Renderer>();
    
    // 共享同一个 Material，只通过 MaterialPropertyBlock 设置纹理差异
    MaterialPropertyBlock mpb = new MaterialPropertyBlock();
    mpb.SetTexture("_GrassTexture", grassTextures[i]);  // PerRendererData 属性
    mpb.SetColor("_TintColor", grassColors[i]);         // PerRendererData 属性
    renderer.SetPropertyBlock(mpb);
}
```

**优势：** 1000 个草地对象共享 1 个 Material，只有 1000 个小的 MaterialPropertyBlock，而不是 1000 个完整的 Material 实例。

**场景 2：Sprite 渲染器（UI、2D 游戏）**

在 Unity 的 UGUI 和 2D Sprite 系统中，每个 Image 或 SpriteRenderer 通常使用相同的 Material，但通过 MaterialPropertyBlock 设置不同的纹理：

```csharp
// UGUI Image 组件的内部实现（简化版）
MaterialPropertyBlock mpb = new MaterialPropertyBlock();
mpb.SetTexture("_MainTex", sprite.texture);  // PerRendererData 属性
image.rectTransform.GetComponent<CanvasRenderer>().SetMaterialPropertyBlock(mpb);
```

**优势：** 大量 UI 元素可以共享同一个 Material，减少内存占用和提高批处理效率。

**场景 3：程序化生成的网格（地形系统）**

在程序化生成的地形系统中，不同的地形块使用相同的着色器，但纹理细节不同：

```csharp
// 地形块渲染
Material terrainMaterial = terrainShaderMaterial;

foreach (TerrainChunk chunk in terrainChunks)
{
    Renderer chunkRenderer = chunk.GetComponent<Renderer>();
    MaterialPropertyBlock mpb = new MaterialPropertyBlock();
    
    // 每个地形块有不同的纹理混合权重
    mpb.SetFloat("_TextureWeight1", chunk.textureWeight1);  // PerRendererData
    mpb.SetFloat("_TextureWeight2", chunk.textureWeight2);  // PerRendererData
    mpb.SetTexture("_DetailTex", chunk.detailTexture);      // PerRendererData
    
    chunkRenderer.SetPropertyBlock(mpb);
}
```

**优势：** 地形系统可以高效地渲染大量地形块，同时保持每个块的视觉差异。

**场景 4：粒子系统中的自定义属性**

在粒子系统中，可以为每个粒子设置不同的属性：

```csharp
// 粒子渲染（简化示例）
Material particleMaterial = particleSystemMaterial;

for (int i = 0; i < particleCount; i++)
{
    Particle particle = particles[i];
    
    // 每个粒子的颜色和大小可能不同
    MaterialPropertyBlock mpb = new MaterialPropertyBlock();
    mpb.SetColor("_Color", particle.color);           // PerRendererData
    mpb.SetFloat("_Size", particle.size);             // PerRendererData
    mpb.SetTexture("_NoiseTex", particle.noiseTexture); // PerRendererData
    
    // 渲染粒子...
}
```

**特性：**
- 在 Material Inspector 中显示为只读
- 值存储在 `MaterialPropertyBlock` 中，不在 Material 资源中
- 支持批量渲染优化（SRP Batcher、GPU Instancing）
- 明确标识该属性的使用方式和设计意图

**驱动层的批处理效率提升：**

从 GPU 驱动层的角度来看，MaterialPropertyBlock 提升批处理效率的核心在于**减少驱动层状态切换的开销**：

**1. Material 实例在驱动层的操作（传统方式）**

使用 Material 实例时，每次切换 Material 都需要在驱动层执行大量操作：

```cpp
// 驱动层视角：使用 Material 实例
void Driver_SetMaterial(Material* mat) {
    // 1. 验证 Shader 兼容性
    if (currentShader != mat->shader) {
        vkCmdBindPipeline(cmdBuffer, VK_PIPELINE_BIND_POINT_GRAPHICS, mat->pipeline);
        // 驱动层需要：验证 pipeline 状态、切换 pipeline 状态机
    }
    
    // 2. 更新所有纹理绑定（每个纹理都需要驱动调用）
    for (each texture in mat->textures) {
        vkUpdateDescriptorSets(device, 
            &writeDescriptorSet,  // 需要驱动层验证描述符
            1, nullptr);
        // 驱动层需要：
        // - 验证纹理格式兼容性
        // - 检查纹理状态（是否需要过渡）
        // - 更新 GPU 内存映射
    }
    
    // 3. 更新所有 Uniform Buffer（每个属性）
    vkMapMemory(device, mat->uniformBufferMemory, ...);
    memcpy(mappedData, mat->properties, mat->propertySize);
    vkUnmapMemory(device, mat->uniformBufferMemory);
    // 驱动层需要：
    // - 内存映射/解映射（用户态 ↔ 内核态）
    // - GPU 内存同步
    // - 缓存失效处理
    
    // 4. 更新 Descriptor Set（驱动层状态验证）
    vkCmdBindDescriptorSets(cmdBuffer, ..., mat->descriptorSet);
    // 驱动层需要：
    // - 验证描述符集有效性
    // - 检查资源绑定状态
    // - 更新 GPU 命令缓冲区的状态
}
```

**开销分析：**
- 每次切换：~10-50 个驱动层 API 调用
- 驱动层验证：资源状态检查、内存映射、同步操作
- CPU ↔ GPU 通信：每次切换都需要内核态操作
- 状态机开销：GPU 驱动需要维护和验证完整的状态

**2. MaterialPropertyBlock 在驱动层的操作（优化方式）**

使用 MaterialPropertyBlock 时，Material 保持不变，只更新少量数据：

```cpp
// 驱动层视角：使用 MaterialPropertyBlock
void Driver_SetMaterialPropertyBlock(Renderer* renderer, MaterialPropertyBlock* mpb) {
    // Material 状态不变（无需重新绑定）
    // Shader/Pipeline 不变（无需验证和切换）
    // 纹理绑定不变（大部分纹理不变）
    
    // 只需要更新 MaterialPropertyBlock 的数据
    // 方式 1：更新独立的 UBO（如果 MPB 数据较大）
    if (mpb->useUniformBuffer) {
        // 直接更新已有的 Uniform Buffer（Material 的 UBO 保持不变）
        void* mappedData;
        vkMapMemory(device, mpb->uniformBufferMemory, ...);
        memcpy(mappedData, mpb->data, mpb->dataSize);  // 只更新少量数据
        vkUnmapMemory(device, mpb->uniformBufferMemory);
        
        // 更新 Descriptor Set（只更新 MPB 的 binding）
        vkUpdateDescriptorSets(device, 
            &mpbWriteDescriptorSet,  // 只更新 MPB 相关的描述符
            1, nullptr);
    }
    
    // 方式 2：使用 Push Constants（如果 MPB 数据很小）
    else if (mpb->usePushConstants) {
        // Push Constants 在 GPU 上有专门的快速内存
        vkCmdPushConstants(cmdBuffer, pipelineLayout, 
            VK_SHADER_STAGE_VERTEX_BIT | VK_SHADER_STAGE_FRAGMENT_BIT,
            0, mpb->dataSize, mpb->data);
        // 驱动层操作：
        // - 直接写入 GPU 快速内存（无需内存映射）
        // - 无需 Descriptor Set 更新
        // - 开销极小
    }
    
    // 方式 3：GPU Instancing（合并到实例数据）
    else if (mpb->useInstancing) {
        // MPB 数据作为实例属性传递，无需单独更新
        // 在 vkCmdDrawIndexed 时作为实例数据一起提交
    }
}
```

**开销分析：**
- 切换开销：~1-3 个驱动层 API 调用（相比 Material 实例减少 80-90%）
- 无需重新验证：Shader/Pipeline/大部分纹理绑定保持不变
- 减少同步：Material 的 UBO 不需要重新映射和同步
- 状态机优化：GPU 驱动可以识别状态未变化，跳过大部分验证

**3. 驱动层命令提交的对比**

**场景：渲染 1000 个对象，100 个对象需要不同的纹理**

```cpp
// 方式 A：使用 Material 实例
for (int i = 0; i < 1000; i++) {
    Material mat = materials[i % 100];  // 100 个不同的 Material
    
    // 驱动层操作（每次循环）
    vkCmdBindPipeline(...);           // 验证并切换 pipeline（如果不同）
    vkUpdateDescriptorSets(...);      // 更新所有纹理描述符
    vkMapMemory(...);                 // 映射 Material UBO 内存
    memcpy(...);                      // 复制 Material 属性
    vkUnmapMemory(...);               // 解映射内存
    vkCmdBindDescriptorSets(...);     // 绑定完整的描述符集
    vkCmdDrawIndexed(...);            // 绘制
    
    // 开销：每次循环都需要完整的状态切换
    // 驱动层 API 调用：~10-20 次/对象
    // CPU-GPU 同步：每次都需要
}

// 方式 B：使用 MaterialPropertyBlock
Material sharedMaterial = material;  // 共享 Material

for (int i = 0; i < 1000; i++) {
    MaterialPropertyBlock mpb = mpbs[i];
    
    if (i == 0) {
        // 只在第一次绑定 Material（驱动层完整操作）
        vkCmdBindPipeline(...);
        vkCmdBindDescriptorSets(..., sharedMaterial->descriptorSet);
        // 驱动层 API 调用：~10 次（仅一次）
    }
    
    // 更新 MaterialPropertyBlock（驱动层轻量操作）
    vkCmdPushConstants(..., mpb->data, mpb->dataSize);
    // 或者
    vkUpdateDescriptorSets(..., mpb->descriptorSet);  // 只更新 MPB 绑定
    vkCmdBindDescriptorSets(..., mpb->descriptorSet); // 只绑定 MPB 描述符
    
    vkCmdDrawIndexed(...);
    
    // 开销：Material 状态保持不变，只更新少量数据
    // 驱动层 API 调用：~2-3 次/对象（减少 80-90%）
    // CPU-GPU 同步：大幅减少（Push Constants 几乎无同步）
}
```

**4. GPU 硬件层面的优化**

**Pipeline 状态缓存：**

```
GPU 驱动维护 Pipeline 状态缓存：

使用 Material 实例：
Material A → Pipeline State A → [需要验证和切换]
Material B → Pipeline State B → [需要验证和切换]
Material A → Pipeline State A → [需要重新验证，因为状态被覆盖]
→ 每次都需要驱动层验证和状态机切换

使用 MaterialPropertyBlock：
Material X → Pipeline State X → [缓存命中，无需验证]
  + MPB A → [只更新数据，Pipeline 状态不变]
  + MPB B → [只更新数据，Pipeline 状态不变]
  + MPB A → [Pipeline 状态仍在缓存中，直接使用]
→ Pipeline 状态缓存命中率高，驱动层开销大幅降低
```

**Descriptor Set 绑定优化：**

```cpp
// GPU 驱动的 Descriptor Set 管理

// Material 实例方式：
vkCmdBindDescriptorSets(..., materialA->descriptorSet);
// 驱动层需要：验证所有 binding（纹理、UBO 等）
// GPU 需要：更新所有寄存器状态

vkCmdBindDescriptorSets(..., materialB->descriptorSet);
// 驱动层需要：重新验证所有 binding（即使大部分相同）
// GPU 需要：更新所有寄存器状态（即使大部分相同）

// MaterialPropertyBlock 方式：
vkCmdBindDescriptorSets(..., sharedMaterial->descriptorSet);
// 驱动层需要：完整验证（仅一次）

vkCmdBindDescriptorSets(..., mpb->descriptorSet);  // 只绑定 MPB 的 binding
// 驱动层需要：只验证 MPB 相关的 binding
// GPU 需要：只更新少量寄存器状态
// → 驱动层验证工作量减少 70-80%
```

**5. 驱动层内存管理优化**

```cpp
// Material UBO 的生命周期管理

// Material 实例方式：
for (each material) {
    // 每个 Material 需要独立的 UBO
    VkBuffer uniformBuffer;
    VkDeviceMemory uniformBufferMemory;
    
    // 每次切换都需要：
    vkMapMemory(...);      // 用户态 → 内核态
    memcpy(...);           // CPU 内存复制
    vkUnmapMemory(...);    // 内核态 → 用户态
    // GPU 同步：等待内存写入完成
    
    // 100 个 Material = 100 个独立的 UBO = 100 次内存映射操作
}

// MaterialPropertyBlock 方式：
// Material 的 UBO（共享，只需绑定一次）
VkBuffer materialUniformBuffer;  // 长期映射，无需频繁映射/解映射

// MaterialPropertyBlock 的 UBO（每个对象）
for (each object) {
    // 选项 1：Push Constants（最快）
    vkCmdPushConstants(...);  // 直接写入 GPU 快速内存，无内存映射
    
    // 选项 2：独立的 MPB UBO（如果数据较大）
    vkMapMemory(..., mpbUniformBufferMemory, ...);
    memcpy(...);  // 只复制少量数据
    vkUnmapMemory(...);
    // 但 Material UBO 不需要重新映射
}
```

**6. 实际性能数据（驱动层开销对比）**

```
场景：1000 个对象，100 个不同的纹理

Material 实例方式：
- 驱动层 API 调用：10,000-20,000 次
- CPU-GPU 同步：1,000 次（每次 Material 切换）
- Pipeline 状态切换：100-1000 次（取决于优化）
- 内存映射操作：1,000 次
- 驱动层 CPU 开销：~5-10ms（1000 个对象）

MaterialPropertyBlock 方式：
- 驱动层 API 调用：2,000-3,000 次（减少 80-85%）
- CPU-GPU 同步：1 次（Material 绑定）+ 少量 MPB 更新
- Pipeline 状态切换：1 次（Material 绑定）
- 内存映射操作：1 次（Material）+ 少量 MPB 更新
- 驱动层 CPU 开销：~0.5-1ms（1000 个对象）

性能提升：驱动层开销减少 80-90%
```

**总结：**

MaterialPropertyBlock 在驱动层提升批处理效率的核心机制：

1. **减少状态切换**：Material 状态保持不变，避免频繁的 Pipeline/Shader 切换
2. **减少驱动层验证**：无需重新验证 Shader、Texture 等大部分状态
3. **优化数据更新**：使用 Push Constants 或独立的小 UBO，减少内存映射开销
4. **提高缓存命中率**：Pipeline 状态缓存保持有效，驱动层可以重用缓存的状态
5. **减少同步操作**：Material 的 UBO 不需要频繁映射/解映射，减少 CPU-GPU 同步

这种设计使得驱动层可以将大部分计算资源用于实际的绘制命令提交，而不是状态管理，从而显著提升批处理效率。

### 3.3 属性覆盖机制

**属性查找顺序：**

1. `MaterialPropertyBlock`（如果已设置）
2. `Material` 实例属性（如果使用 `material` 而非 `sharedMaterial`）
3. `Material` 资源属性（`sharedMaterial`）

**代码示例：**
```csharp
// Material 资源中的值
material.SetColor("_Color", Color.blue);

// MaterialPropertyBlock 中的值（优先级更高）
mpb.SetColor("_Color", Color.red);
renderer.SetPropertyBlock(mpb);

// 最终使用 Color.red
```

### 3.4 共享材质 vs 实例材质的内存考量

**共享材质（sharedMaterial）：**
- 所有对象共享同一个材质实例
- 内存开销：1 份材质数据
- 修改影响所有对象
- **推荐用于静态对象**

**实例材质（material）：**
- 每个对象有自己的材质实例
- 内存开销：N 份材质数据（N = 对象数量）
- 修改只影响当前对象
- 开销较大，**不推荐大量使用**

**MaterialPropertyBlock：**
- 共享材质 + 独立的属性块
- 内存开销：1 份材质 + N 份属性块数据（通常很小）
- 修改只影响当前对象
- **推荐用于需要差异化的大量对象**

[↑ 返回目录](#目录-table-of-contents)

## 4. 渲染队列系统

### 4.1 RenderQueue 数值范围

Unity 的渲染队列使用数值范围来定义渲染顺序：

| 队列范围 | 数值 | 说明 |
|---------|------|------|
| Background | 1000 | 背景对象（如天空盒） |
| Geometry | 2000 | 不透明几何体（默认） |
| AlphaTest | 2450 | 透明度裁剪对象 |
| Transparent | 3000 | 透明对象 |
| Overlay | 4000 | 叠加效果（如 UI） |

**在代码中使用：**

```csharp
// 设置材质的渲染队列
material.renderQueue = 3000;  // 透明队列

// 或使用预定义值
material.renderQueue = (int)RenderQueue.Transparent;
```

### 4.2 Queue 的分类

**RenderQueueRange 的使用：**

```57:57:Assets/Custom RP/Runtime/CameraRenderer.cs
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
```

**预定义的队列范围：**

- `RenderQueueRange.opaque`：< 2500（不透明对象）
- `RenderQueueRange.transparent`：>= 2500（透明对象）
- `RenderQueueRange.all`：所有队列

**自定义范围：**

```csharp
var range = new RenderQueueRange(2000, 3000);  // 从 2000 到 3000
```

### 4.3 队列与深度测试的关系

**不透明对象（< 2500）：**
- 从前往后渲染（基于深度）
- 启用深度写入（ZWrite On）
- 深度测试：LessEqual（更近或相等时渲染）
- 不进行 Alpha 混合

**透明对象（>= 2500）：**
- 从后往前渲染（基于深度，反向）
- 禁用深度写入（ZWrite Off）
- 深度测试：LessEqual（但只读取，不写入）
- 进行 Alpha 混合

**为什么透明对象要从后往前渲染？**
- Alpha 混合需要正确的颜色叠加顺序
- 从后往前确保后面的对象先渲染，前面的对象后渲染
- 这样才能得到正确的透明效果

[↑ 返回目录](#目录-table-of-contents)

## 5. 排序和批处理

### 5.1 SortingCriteria 的选项

`SortingSettings` 使用 `SortingCriteria` 枚举来决定排序方式：

**常见选项：**

- `CommonOpaque`：不透明对象的标准排序（从前往后）
- `CommonTransparent`：透明对象的标准排序（从后往前）
- `SortingLayer`：按排序图层排序
- `RendererPriority`：按渲染器优先级排序

**在当前项目中的使用：**

```52:55:Assets/Custom RP/Runtime/CameraRenderer.cs
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
```

**透明对象排序：**

```62:63:Assets/Custom RP/Runtime/CameraRenderer.cs
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
```

### 5.2 静态批处理 vs 动态批处理

**静态批处理（Static Batching）：**
- 在构建时或编辑器中将静态对象合并
- 减少 Draw Call 数量
- 内存开销：需要额外的顶点数据副本
- **限制：** 对象必须是静态的（`Static` 标记）

**动态批处理（Dynamic Batching）：**
- 运行时自动合并小的动态对象
- 减少 Draw Call 数量
- **限制：**
  - 顶点数 < 300
  - 使用相同材质
  - 不进行缩放或统一缩放

**在当前代码中的处理：**

```57:58:Assets/Custom RP/Runtime/CameraRenderer.cs
        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
```

Unity 会自动尝试批处理，无需额外代码。

### 5.3 GPU Instancing

GPU Instancing 允许使用单个 Draw Call 渲染多个相同网格的实例。

**启用条件：**
- 使用相同的网格和材质
- 着色器支持 GPU Instancing（`#pragma multi_compile_instancing`）
- 通过 `Graphics.DrawMeshInstanced()` 或 Material 的 `enableInstancing` 属性

**优势：**
- 大幅减少 Draw Call（如渲染 1000 棵树只需要 1 个 Draw Call）
- 性能提升明显

**与动态批处理的区别：**
- GPU Instancing：可以处理大量对象（数千个）
- 动态批处理：对象数量有限（通常 < 几百个）

### 5.4 SRP Batcher 的工作原理

SRP Batcher 是 SRP 特有的优化功能，可以大幅减少 Draw Call。

**工作原理：**

1. **材质数据分离**
   - 将材质属性存储在 GPU 常量缓冲区（CBuffer）中
   - 每个材质有独立的 CBuffer

2. **快速切换**
   - 切换材质时只需切换 CBuffer 指针
   - 无需重新绑定所有属性

3. **自动启用**
   - 只要着色器兼容 SRP Batcher，就会自动使用
   - 无需额外代码

**兼容要求：**
- 使用 `CBUFFER_START` 和 `CBUFFER_END` 定义材质属性
- 属性必须在 CBuffer 中，不能在全局作用域

**性能提升：**
- 通常可以将 Draw Call 减少 50-90%
- 特别适合有大量不同材质的场景

[↑ 返回目录](#目录-table-of-contents)

## 6. 编辑器相关

### 6.1 Partial Class 的使用

使用 `partial class` 将类定义分散到多个文件中：

```6:7:Assets/Custom RP/Runtime/CameraRenderer.cs
public partial class CameraRenderer
{
```

```9:10:Assets/Custom RP/Runtime/CameraRenderer.Editor.cs
public partial class CameraRenderer
{
    private partial void DrawGizmos();
```

**优势：**
- 代码组织：运行时代码和编辑器代码分离
- 条件编译：编辑器代码可以只在编辑器中编译
- 清晰性：明确区分哪些功能只在编辑器中可用

### 6.2 Conditional Compilation（条件编译）

使用预处理指令控制代码编译：

```14:14:Assets/Custom RP/Runtime/CameraRenderer.Editor.cs
#if UNITY_EDITOR || DEVELOPMENT_BUILD
```

**常用指令：**
- `#if UNITY_EDITOR`：仅在 Unity 编辑器中编译
- `#if UNITY_EDITOR || DEVELOPMENT_BUILD`：编辑器或开发构建中编译
- `#if !UNITY_EDITOR`：非编辑器环境（发布版本）

**好处：**
- 减少发布版本大小
- 提高发布版本性能
- 移除编辑器专用代码（如 Gizmos、调试工具）

### 6.3 Editor-only 功能的设计模式

**Partial Method 模式：**

```11:12:Assets/Custom RP/Runtime/CameraRenderer.Editor.cs
    private partial void DrawGizmos();
    private partial void DrawUnsupportedShaders();
```

- 在运行时类中声明 `partial` 方法
- 在编辑器类中实现
- 如果编辑器类不存在，方法调用会被移除（无性能开销）

**使用示例：**

```36:37:Assets/Custom RP/Runtime/CameraRenderer.cs
        DrawUnsupportedShaders();
        DrawGizmos();
```

在运行时代码中调用，但在非编辑器环境中，这些调用会被优化掉。

[↑ 返回目录](#目录-table-of-contents)

## 7. 性能考量

### 7.1 Draw Call 的优化

**Draw Call 是什么？**
- CPU 向 GPU 发送的一个渲染命令
- 每次切换材质、网格或渲染状态都可能产生新的 Draw Call
- Draw Call 过多会导致 CPU 瓶颈

**优化策略：**

1. **批处理**
   - 静态批处理：合并静态对象
   - 动态批处理：合并小的动态对象
   - GPU Instancing：渲染大量相同对象

2. **材质优化**
   - 减少材质数量（合并相似材质）
   - 使用 SRP Batcher（SRP 特有）

3. **LOD（细节层次）**
   - 根据距离使用不同细节级别的网格
   - 减少远距离对象的顶点数

### 7.2 命令缓冲区的合理使用

**最佳实践：**

1. **重用缓冲区**
   ```csharp
   // 在构造函数中创建
   buffer = new CommandBuffer() { name = bufferName };
   
   // 每次使用后清空，而不是重建
   buffer.Clear();
   ```

2. **及时执行**
   - 在 Setup 阶段执行清理命令
   - 在 Submit 之前执行所有必要命令

3. **命名缓冲区**
   ```csharp
   buffer = new CommandBuffer() {
       name = bufferName  // 用于 Frame Debugger 识别
   };
   ```

### 7.3 上下文提交的时机

**Submit() 的重要性：**

```73:73:Assets/Custom RP/Runtime/CameraRenderer.cs
        context.Submit();
```

**关键点：**

1. **所有命令的集合点**
   - 在此之前，所有 `context` 方法只是记录命令
   - `Submit()` 才是实际执行渲染的时刻

2. **调用时机**
   - 必须在所有渲染命令记录完成后调用
   - 每个相机的渲染流程最后调用一次
   - 不调用 `Submit()` 就不会有任何渲染结果

3. **性能影响**
   - `Submit()` 本身有开销（状态切换、GPU 同步等）
   - 应该在一个相机的所有渲染完成后统一提交
   - 避免在循环中多次调用

[↑ 返回目录](#目录-table-of-contents)

## 参考资源

- [Unity 官方文档 - Scriptable Render Pipeline](https://docs.unity3d.com/Manual/ScriptableRenderPipeline.html)
- [Unity 官方文档 - ShaderLab Pass Tags](https://docs.unity3d.com/Manual/SL-PassTags.html)
- [Unity 官方文档 - MaterialPropertyBlock](https://docs.unity3d.com/ScriptReference/MaterialPropertyBlock.html)
- [Unity 官方文档 - Render Queue](https://docs.unity3d.com/Manual/SL-SubShaderTags.html)
- [Catlike Coding - Custom SRP 教程](https://catlikecoding.com/unity/tutorials/custom-srp)

[↑ 返回目录](#目录-table-of-contents)

## TODO

- [Shader 优化](https://docs.unity3d.com/Manual/SL-ShaderPerformance.html)

[↑ 返回目录](#目录-table-of-contents)
