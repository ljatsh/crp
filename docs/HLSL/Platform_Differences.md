# 平台差异

#HLSL #平台 #DirectX #OpenGL #GPU架构

> 理解平台差异：DirectX vs OpenGL、不同 GPU 架构的指令集差异

## 📋 目录

- [DirectX vs OpenGL](#directx-vs-opengl)
- [不同 GPU 架构](#不同-gpu-架构)
- [平台特定优化](#平台特定优化)
- [实践任务](#实践任务)

---

## DirectX vs OpenGL

### 指令集差异

#### DirectX HLSL
```hlsl
// DirectX HLSL 语法
Texture2D myTexture;
SamplerState mySampler;
float4 color = myTexture.Sample(mySampler, uv);
```

#### OpenGL GLSL
```glsl
// OpenGL GLSL 语法
uniform sampler2D myTexture;
vec4 color = texture(myTexture, uv);
```

### 寄存器命名差异

#### DirectX 寄存器
```assembly
; DirectX 汇编
mov r0, c0          ; 临时寄存器 r0，常量寄存器 c0
tex2d r1, t0, s0    ; 纹理寄存器 t0，采样器 s0
```

#### OpenGL 寄存器
```assembly
; OpenGL 汇编（ARB 汇编）
MOV result.color, fragment.color;
TEX result.color, fragment.texcoord[0], texture[0], 2D;
```

### 语义差异

#### DirectX 语义
```hlsl
struct VertexOutput {
    float4 position : SV_POSITION;  // 系统值语义
    float2 uv : TEXCOORD0;
};
```

#### OpenGL 语义
```glsl
out vec4 gl_Position;  // 内置变量
out vec2 uv;
```

### 矩阵存储差异

#### DirectX（行主序）
```hlsl
// DirectX 使用行主序
float4x4 matrix = float4x4(
    1, 0, 0, 0,  // 第一行
    0, 1, 0, 0,  // 第二行
    0, 0, 1, 0,  // 第三行
    0, 0, 0, 1   // 第四行
);
```

#### OpenGL（列主序）
```glsl
// OpenGL 使用列主序
mat4 matrix = mat4(
    1, 0, 0, 0,  // 第一列
    0, 1, 0, 0,  // 第二列
    0, 0, 1, 0,  // 第三列
    0, 0, 0, 1   // 第四列
);
```

---

## 不同 GPU 架构

### NVIDIA 架构

#### 特性
- **CUDA 核心**：统一着色器架构
- **指令执行**：SIMT（单指令多线程）
- **寄存器文件**：较大的寄存器文件
- **分支处理**：较好的动态分支性能

#### 优化建议
```hlsl
// NVIDIA GPU 对动态分支有较好的支持
if (condition) {
    // 分支代码
} else {
    // 分支代码
}
```

#### 指令特点
- 支持较长的指令序列
- 较好的寄存器分配
- 高效的纹理采样

### AMD 架构

#### 特性
- **流处理器**：VLIW（超长指令字）架构（旧架构）或 GCN/RDNA 架构
- **指令执行**：SIMD 执行单元
- **寄存器文件**：中等大小的寄存器文件
- **分支处理**：分支性能取决于架构

#### 优化建议
```hlsl
// AMD GPU 建议避免动态分支
// 使用数学技巧代替分支
float value = lerp(b, a, step(threshold, x));
```

#### 指令特点
- VLIW 架构需要填充指令槽
- GCN/RDNA 架构更现代，性能更好
- 纹理采样性能良好

### Intel 架构

#### 特性
- **执行单元**：较小的执行单元
- **寄存器文件**：较小的寄存器文件
- **分支处理**：分支性能一般

#### 优化建议
```hlsl
// Intel GPU 建议：
// 1. 减少寄存器使用
// 2. 避免复杂分支
// 3. 优化纹理采样
```

#### 指令特点
- 指令数限制较严格
- 寄存器压力较大
- 需要仔细优化

### 移动 GPU 架构

#### ARM Mali
- **特性**：Tile-based 渲染
- **优化**：减少 overdraw，优化纹理采样

#### Qualcomm Adreno
- **特性**：统一着色器架构
- **优化**：使用 half 精度，减少纹理采样

#### PowerVR
- **特性**：Tile-based deferred rendering (TBDR)
- **优化**：减少 overdraw，优化 alpha 混合

---

## 平台特定优化

### 精度选择

#### 桌面平台
```hlsl
#if defined(SHADER_API_DESKTOP)
    #define REAL float
    #define REAL2 float2
    #define REAL3 float3
    #define REAL4 float4
#endif
```

#### 移动平台
```hlsl
#if defined(SHADER_API_MOBILE)
    #define REAL half
    #define REAL2 half2
    #define REAL3 half3
    #define REAL4 half4
#endif
```

### 纹理格式选择

#### 桌面平台
```hlsl
// 可以使用未压缩格式或高质量压缩
// BC7, BC6H 等
```

#### 移动平台
```hlsl
// 建议使用压缩格式
// ETC2, ASTC 等
```

### 分支优化

#### 统一分支（所有平台都高效）
```hlsl
uniform bool useFeature;  // 对所有像素相同

if (useFeature) {
    // 统一分支，性能好
}
```

#### 非统一分支（平台差异大）
```hlsl
float threshold = 0.5;

if (value > threshold) {  // 每个像素可能不同
    // NVIDIA: 性能较好
    // AMD: 性能中等
    // Intel: 性能较差
    // 移动GPU: 性能较差
}
```

### 指令优化

#### 乘加指令（所有平台都支持）
```hlsl
// 使用 mad 代替 mul + add
float result = mad(a, b, c);  // 1条指令
// 而不是
float temp = a * b;           // 2条指令
float result = temp + c;
```

#### 数学函数优化
```hlsl
// 使用硬件加速的函数
float invSqrt = rsqrt(x);     // 硬件加速
float inv = rcp(x);            // 硬件加速
```

---

## 实践任务

### 任务1：对比同一 HLSL 代码在不同平台的汇编

#### 步骤1：编写测试代码
```hlsl
float4 PS_Main(float2 uv : TEXCOORD0) : SV_Target {
    float4 color = float4(1.0, 0.0, 0.0, 1.0);
    float value = uv.x * uv.y;
    return color * value;
}
```

#### 步骤2：编译到不同平台
```bash
# DirectX
fxc /T ps_5_0 /Fc dx.asm shader.hlsl

# 如果支持 OpenGL
# 使用相应的 GLSL 编译器
```

#### 步骤3：对比分析
- 指令差异
- 寄存器使用差异
- 优化差异

### 任务2：分析平台特定的优化机会

#### 识别平台特性
- **NVIDIA**：可以利用动态分支
- **AMD**：优化指令槽填充（VLIW）
- **Intel**：减少寄存器使用
- **移动GPU**：使用 half 精度，减少纹理采样

#### 实现平台特定代码
```hlsl
#if defined(SHADER_API_MOBILE)
    // 移动平台优化
    half3 color = half3(1.0, 0.0, 0.0);
    half value = half(uv.x * uv.y);
#else
    // 桌面平台
    float3 color = float3(1.0, 0.0, 0.0);
    float value = uv.x * uv.y;
#endif
```

---

## 跨平台兼容性

### 使用条件编译
```hlsl
#if defined(SHADER_API_D3D11) || defined(SHADER_API_D3D12)
    // DirectX 特定代码
#elif defined(SHADER_API_OPENGL) || defined(SHADER_API_GLES)
    // OpenGL 特定代码
#elif defined(SHADER_API_VULKAN)
    // Vulkan 特定代码
#endif
```

### 抽象层设计
```hlsl
// 定义平台无关的接口
#define SAMPLE_TEXTURE(tex, samp, uv) tex.Sample(samp, uv)
#define GET_POSITION(input) input.position

// 平台特定实现
#if defined(SHADER_API_GLES)
    // GLES 特定实现
#endif
```

---

## 🔗 相关链接

- [[Shader_Model_Versions]] - 不同版本的特性
- [[Assembly_Basics]] - 指令差异
- [[Performance_Optimization]] - 平台特定优化
- [[Tools_Guide]] - 平台特定工具

---

*最后更新：2024年*

