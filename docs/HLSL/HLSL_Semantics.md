# HLSL 语义系统

#HLSL #语义 #Semantics #着色器输入输出

> 掌握 HLSL 语义系统：顶点/像素/几何/计算着色器的输入输出语义

## 📋 目录

- [顶点着色器语义](#顶点着色器语义)
- [像素着色器语义](#像素着色器语义)
- [几何着色器语义](#几何着色器语义)
- [计算着色器语义](#计算着色器语义)
- [系统值语义](#系统值语义)
- [实践任务](#实践任务)

---

## 顶点着色器语义

### 输入语义

顶点着色器从顶点缓冲区接收数据，使用以下语义：

#### 位置语义
```hlsl
struct VertexInput {
    float3 position : POSITION;  // 顶点位置（对象空间）
};
```

#### 法线和切线语义
```hlsl
struct VertexInput {
    float3 normal : NORMAL;        // 顶点法线
    float4 tangent : TANGENT;      // 顶点切线（w 分量存储副切线的方向）
};
```

#### 纹理坐标语义
```hlsl
struct VertexInput {
    float2 uv0 : TEXCOORD0;  // 第一组纹理坐标
    float2 uv1 : TEXCOORD1;  // 第二组纹理坐标
    // ... 最多到 TEXCOORD7
};
```

#### 颜色语义
```hlsl
struct VertexInput {
    float4 color : COLOR0;  // 顶点颜色（RGBA）
    float4 color1 : COLOR1; // 第二组颜色（如果支持）
};
```

### 输出语义

顶点着色器输出到像素着色器的数据：

#### 系统值语义
```hlsl
struct VertexOutput {
    float4 position : SV_POSITION;  // 裁剪空间位置（必需）
};
```

#### 用户定义语义
```hlsl
struct VertexOutput {
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD0;           // 纹理坐标
    float3 worldPos : TEXCOORD1;     // 世界空间位置
    float3 normal : TEXCOORD2;       // 世界空间法线
    float4 color : COLOR0;            // 颜色
};
```

---

## 像素着色器语义

### 输入语义

像素着色器接收来自顶点着色器的插值数据：

#### 系统值语义
```hlsl
struct PixelInput {
    float4 position : SV_POSITION;  // 像素在屏幕空间的位置
};
```

#### 用户定义语义
```hlsl
struct PixelInput {
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD0;  // 插值后的纹理坐标
    float3 worldPos : TEXCOORD1;
    float3 normal : TEXCOORD2;
};
```

### 输出语义

#### 渲染目标输出
```hlsl
struct PixelOutput {
    float4 color : SV_Target0;  // 第一个渲染目标
    float4 color1 : SV_Target1; // 第二个渲染目标（如果支持）
    // ... 最多到 SV_Target7
};
```

#### 深度输出
```hlsl
float4 PS_Main(PixelInput input) : SV_Target {
    // ...
}

// 输出自定义深度值
void PS_Main_Depth(PixelInput input, out float4 color : SV_Target, out float depth : SV_Depth) {
    color = float4(1.0, 0.0, 0.0, 1.0);
    depth = 0.5;  // 自定义深度值
}
```

#### 模板输出（某些平台）
```hlsl
void PS_Main(
    PixelInput input,
    out float4 color : SV_Target,
    out uint stencil : SV_StencilRef
) {
    color = float4(1.0, 0.0, 0.0, 1.0);
    stencil = 1;
}
```

---

## 几何着色器语义

### 输入语义

几何着色器接收来自顶点着色器的图元数据：

#### 图元类型
```hlsl
[maxvertexcount(3)]
void GS_Main(
    triangle VertexOutput input[3],  // 三角形图元
    inout TriangleStream<VertexOutput> output
) {
    // ...
}
```

#### 其他图元类型
```hlsl
// 点图元
void GS_Main_Point(
    point VertexOutput input[1],
    inout PointStream<VertexOutput> output
) {
    // ...
}

// 线图元
void GS_Main_Line(
    line VertexOutput input[2],
    inout LineStream<VertexOutput> output
) {
    // ...
}
```

### 系统值语义

```hlsl
struct GSInput {
    float4 position : SV_POSITION;
    uint primitiveID : SV_PrimitiveID;      // 图元 ID
    uint instanceID : SV_GSInstanceID;       // 实例 ID（如果使用实例化）
};
```

### 输出语义

几何着色器输出到像素着色器的数据使用与顶点着色器相同的语义：

```hlsl
struct GSOutput {
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD0;
    // ...
};
```

---

## 计算着色器语义

### 线程 ID 语义

计算着色器使用特殊的系统值语义来标识线程：

```hlsl
[numthreads(8, 8, 1)]
void CS_Main(
    uint3 groupID : SV_GroupID,           // 线程组 ID
    uint3 groupThreadID : SV_GroupThreadID, // 线程组内线程 ID
    uint3 dispatchThreadID : SV_DispatchThreadID, // 全局线程 ID
    uint groupIndex : SV_GroupIndex       // 线程组内线性索引
) {
    // groupID: 哪个线程组（0, 0, 0）到 (width/8, height/8, 1)
    // groupThreadID: 线程组内位置 (0-7, 0-7, 0)
    // dispatchThreadID: 全局位置 = groupID * 8 + groupThreadID
    // groupIndex: 线程组内线性索引 (0-63)
}
```

### 计算示例

```hlsl
// 处理 2D 纹理的计算着色器
[numthreads(8, 8, 1)]
void CS_ProcessTexture(
    uint3 id : SV_DispatchThreadID,
    uint3 groupID : SV_GroupID,
    uint3 groupThreadID : SV_GroupThreadID
) {
    uint2 pixelCoord = id.xy;
    
    // 边界检查
    if (pixelCoord.x >= textureWidth || pixelCoord.y >= textureHeight) {
        return;
    }
    
    // 处理像素
    float4 color = inputTexture[pixelCoord];
    outputTexture[pixelCoord] = ProcessColor(color);
}
```

---

## 系统值语义

### 常用系统值语义

| 语义 | 类型 | 说明 | 可用阶段 |
|------|------|------|----------|
| `SV_POSITION` | `float4` | 裁剪空间位置 | VS输出, PS输入 |
| `SV_Target` | `float4` | 渲染目标输出 | PS输出 |
| `SV_Depth` | `float` | 深度值输出 | PS输出 |
| `SV_PrimitiveID` | `uint` | 图元 ID | GS, PS |
| `SV_GSInstanceID` | `uint` | 几何着色器实例 ID | GS |
| `SV_GroupID` | `uint3` | 线程组 ID | CS |
| `SV_GroupThreadID` | `uint3` | 线程组内线程 ID | CS |
| `SV_DispatchThreadID` | `uint3` | 全局线程 ID | CS |
| `SV_GroupIndex` | `uint` | 线程组内线性索引 | CS |
| `SV_VertexID` | `uint` | 顶点 ID | VS |
| `SV_InstanceID` | `uint` | 实例 ID | VS, PS |
| `SV_Coverage` | `uint` | 覆盖掩码 | PS |
| `SV_StencilRef` | `uint` | 模板参考值 | PS |

### 顶点 ID 和实例 ID

```hlsl
struct VertexInput {
    uint vertexID : SV_VertexID;      // 顶点在缓冲区中的索引
    uint instanceID : SV_InstanceID;  // 实例索引（用于实例化渲染）
    float3 position : POSITION;
};

VertexOutput VS_Main(VertexInput input) {
    VertexOutput output;
    
    // 可以使用 vertexID 生成顶点数据
    float offset = float(input.vertexID) * 0.1;
    
    output.position = float4(input.position + float3(offset, 0, 0), 1.0);
    return output;
}
```

---

## 实践任务

### 任务1：编写完整的顶点-像素着色器对

```hlsl
// 顶点着色器
struct VSInput {
    float3 position : POSITION;
    float3 normal : NORMAL;
    float2 uv : TEXCOORD0;
};

struct VSOutput {
    float4 position : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 worldPos : TEXCOORD1;
    float3 normal : TEXCOORD2;
};

VSOutput VS_Main(VSInput input) {
    VSOutput output;
    
    // 变换到裁剪空间
    output.position = mul(float4(input.position, 1.0), worldViewProjMatrix);
    
    // 传递其他数据
    output.uv = input.uv;
    output.worldPos = mul(float4(input.position, 1.0), worldMatrix).xyz;
    output.normal = mul(input.normal, (float3x3)worldMatrix);
    
    return output;
}

// 像素着色器
float4 PS_Main(VSOutput input) : SV_Target {
    // 使用插值后的数据
    float3 N = normalize(input.normal);
    float3 L = normalize(lightDirection);
    
    float NdotL = saturate(dot(N, L));
    float3 color = albedo * NdotL;
    
    return float4(color, 1.0);
}
```

### 任务2：实现几何着色器示例

```hlsl
[maxvertexcount(3)]
void GS_Main(
    triangle VSOutput input[3],
    inout TriangleStream<VSOutput> output
) {
    // 为每个顶点添加偏移
    for (int i = 0; i < 3; i++) {
        VSOutput vertex = input[i];
        vertex.worldPos += float3(0, 1, 0);  // 向上偏移
        output.Append(vertex);
    }
    output.RestartStrip();
}
```

### 任务3：编写简单的计算着色器

```hlsl
Texture2D<float4> inputTexture;
RWTexture2D<float4> outputTexture;

[numthreads(8, 8, 1)]
void CS_Main(uint3 id : SV_DispatchThreadID) {
    uint2 coord = id.xy;
    
    // 读取输入
    float4 color = inputTexture[coord];
    
    // 处理（例如：转换为灰度）
    float gray = dot(color.rgb, float3(0.299, 0.587, 0.114));
    
    // 写入输出
    outputTexture[coord] = float4(gray, gray, gray, color.a);
}
```

---

## 🔗 相关链接

- [[Shader_Model_Versions]]
- [[Resource_Binding]]
- [[Assembly_Basics]] - 语义如何映射到寄存器

---

*最后更新：2024年*

