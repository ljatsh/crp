# 工具使用指南

#HLSL #工具 #编译器 #调试 #分析

> 掌握工具使用：FXC/DXC 编译器、RenderDoc、GPU ShaderAnalyzer、Nsight Graphics

## 📋 目录

- [编译器工具](#编译器工具)
- [分析工具](#分析工具)
- [调试工具](#调试工具)
- [实践任务](#实践任务)

---

## 编译器工具

### FXC（fxc.exe）

**用途**：DirectX 9/10/11 的 HLSL 编译器

#### 基本用法
```bash
fxc /T ps_5_0 shader.hlsl
```

#### 常用参数
```bash
# 指定目标着色器模型
/T vs_5_0    # 顶点着色器 5.0
/T ps_5_0    # 像素着色器 5.0
/T cs_5_0    # 计算着色器 5.0
/T gs_5_0    # 几何着色器 5.0

# 输出文件
/Fo output.obj     # 输出目标文件
/Fc output.asm     # 输出汇编代码
/Fh output.h       # 输出 C 头文件

# 优化选项
/Od               # 禁用优化
/O1               # 优化大小
/O2               # 优化速度
/O3               # 最大优化

# 包含路径
/I "path/to/includes"

# 预处理器定义
/D "DEFINE_NAME=value"
```

#### 完整示例
```bash
fxc /T ps_5_0 /Fc output.asm /O3 /I "Includes" /D "USE_FEATURE=1" shader.hlsl
```

### DXC（dxc.exe）

**用途**：DirectX 12 的现代 HLSL 编译器（基于 LLVM）

#### 基本用法
```bash
dxc -T ps_6_0 shader.hlsl
```

#### 常用参数
```bash
# 指定目标着色器模型
-T vs_6_0    # 顶点着色器 6.0
-T ps_6_0    # 像素着色器 6.0
-T cs_6_0    # 计算着色器 6.0

# 输出文件
-Fo output.dxil    # 输出 DXIL 文件
-Fc output.asm     # 输出汇编代码
-Fd output.pdb     # 输出调试信息

# 优化选项
-O0               # 禁用优化
-O1               # 优化
-O2               # 更多优化
-O3               # 最大优化

# 包含路径
-I "path/to/includes"

# 预处理器定义
-D "DEFINE_NAME=value"

# 启用特性
-enable-16bit-types
```

#### 完整示例
```bash
dxc -T ps_6_0 -Fc output.asm -O3 -I "Includes" -D "USE_FEATURE=1" shader.hlsl
```

### 编译流程示例

#### 步骤1：编写 HLSL 代码
```hlsl
// shader.hlsl
float4 PS_Main(float2 uv : TEXCOORD0) : SV_Target {
    return float4(1.0, 0.0, 0.0, 1.0);
}
```

#### 步骤2：编译
```bash
fxc /T ps_5_0 /Fc shader.asm shader.hlsl
```

#### 步骤3：查看汇编输出
```assembly
ps_5_0
dcl_globalFlags refactoringAllowed
dcl_input_ps linear v0.xy
dcl_output o0.xyzw
mov o0, l(1.000000, 0.000000, 0.000000, 1.000000)
ret
```

---

## 分析工具

### RenderDoc

**用途**：图形调试和分析工具

#### 主要功能
1. **帧捕获**：捕获单帧的渲染调用
2. **着色器查看**：查看实际运行的着色器代码
3. **资源查看**：查看纹理、缓冲区等资源
4. **性能分析**：分析 GPU 性能

#### 使用步骤
1. **启动 RenderDoc**
   - 启动 RenderDoc
   - 选择要调试的应用程序

2. **捕获帧**
   - 在应用程序中触发要分析的场景
   - 按 F12 或点击捕获按钮

3. **分析着色器**
   - 打开捕获的帧
   - 选择绘制调用
   - 查看着色器代码和资源

4. **性能分析**
   - 查看 GPU 时间
   - 分析瓶颈
   - 优化建议

#### 查看着色器汇编
- 在 RenderDoc 中选择绘制调用
- 打开 "Pipeline State" 标签
- 查看 "Shader" 部分
- 可以查看 HLSL 源代码和编译后的汇编

### AMD GPU ShaderAnalyzer

**用途**：AMD GPU 的着色器分析工具

#### 主要功能
1. **指令分析**：分析着色器指令
2. **性能预测**：预测性能特征
3. **优化建议**：提供优化建议

#### 使用步骤
1. **加载着色器**
   - 打开 GPU ShaderAnalyzer
   - 加载 HLSL 文件或汇编文件

2. **选择目标架构**
   - 选择目标 GPU 架构（GCN, RDNA 等）

3. **分析结果**
   - 查看指令统计
   - 查看性能预测
   - 查看优化建议

### NVIDIA Nsight Graphics

**用途**：NVIDIA GPU 的图形调试和分析工具

#### 主要功能
1. **帧调试**：逐帧调试
2. **性能分析**：GPU 性能分析
3. **着色器分析**：着色器代码分析

#### 使用步骤
1. **启动 Nsight Graphics**
   - 启动 Nsight Graphics
   - 连接到目标应用程序

2. **捕获帧**
   - 捕获要分析的帧

3. **分析着色器**
   - 查看着色器代码
   - 分析性能瓶颈
   - 查看优化建议

### PIX（Windows Graphics Debugger）

**用途**：Windows 图形调试器

#### 主要功能
1. **GPU 时间线**：查看 GPU 执行时间线
2. **资源查看**：查看纹理、缓冲区
3. **着色器调试**：调试着色器代码

#### 使用步骤
1. **启动 PIX**
   - 启动 PIX
   - 选择要调试的应用程序

2. **捕获数据**
   - 捕获 GPU 时间线数据

3. **分析**
   - 查看 GPU 事件
   - 分析性能瓶颈
   - 调试着色器

---

## 调试工具

### 着色器调试技巧

#### 使用颜色输出调试
```hlsl
// 输出调试信息到颜色
float4 PS_Debug(float2 uv : TEXCOORD0) : SV_Target {
    // 输出 UV 坐标
    return float4(uv, 0.0, 1.0);
    
    // 输出法线
    // return float4(normal * 0.5 + 0.5, 1.0);
    
    // 输出深度
    // return float4(depth, depth, depth, 1.0);
}
```

#### 使用条件编译调试
```hlsl
#define DEBUG_MODE 1

#if DEBUG_MODE
    float4 debugColor = float4(1.0, 0.0, 0.0, 1.0);
    return debugColor;
#else
    // 正常代码
#endif
```

### 性能分析技巧

#### 使用时间戳
```hlsl
// 在某些平台上可以使用时间戳
// 测量着色器执行时间
```

#### 使用计数器
```hlsl
// 使用原子操作计数
// 分析执行频率
```

---

## 实践任务

### 任务1：使用工具编译和分析自己的着色器

#### 步骤1：编写测试着色器
```hlsl
// test.hlsl
float4 PS_Main(float2 uv : TEXCOORD0) : SV_Target {
    float4 color = float4(uv, 0.0, 1.0);
    float value = sin(uv.x * 10.0);
    return color * value;
}
```

#### 步骤2：编译
```bash
fxc /T ps_5_0 /Fc test.asm /O3 test.hlsl
```

#### 步骤3：分析汇编
- 打开生成的 `test.asm` 文件
- 识别主要指令
- 统计指令数量
- 分析寄存器使用

### 任务2：对比不同工具的输出

#### 使用多个工具分析同一着色器
1. **FXC**：生成汇编代码
2. **RenderDoc**：运行时分析
3. **GPU ShaderAnalyzer**：AMD 架构分析
4. **Nsight Graphics**：NVIDIA 架构分析

#### 对比分析
- 指令差异
- 性能预测差异
- 优化建议差异

### 任务3：优化着色器性能

#### 步骤1：基准测试
- 使用工具测量当前性能
- 记录指令数和执行时间

#### 步骤2：优化
- 应用优化技巧
- 减少指令数
- 优化寄存器使用

#### 步骤3：验证
- 再次测量性能
- 对比优化前后
- 验证改进效果

---

## 工具下载链接

### 编译器
- **FXC**：包含在 Windows SDK 中
- **DXC**：https://github.com/microsoft/DirectXShaderCompiler

### 分析工具
- **RenderDoc**：https://renderdoc.org/
- **AMD GPU ShaderAnalyzer**：AMD 官网
- **NVIDIA Nsight Graphics**：NVIDIA 官网
- **PIX**：Windows SDK

---

## 🔗 相关链接

- [[Assembly_Basics]] - 理解汇编代码
- [[Assembly_Code_Analysis]] - 分析技巧
- [[Compilation_Pipeline]] - 编译过程
- [[Performance_Optimization]] - 优化方法

---

*最后更新：2024年*

