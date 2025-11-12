# 编译流程分析

#HLSL #编译 #编译器 #优化

> 理解编译流程：HLSL→汇编流程、编译器优化、代码生成

## 📋 目录

- [HLSL → 汇编流程](#hlsl--汇编流程)
- [编译器优化](#编译器优化)
- [代码生成](#代码生成)
- [实践任务](#实践任务)

---

## HLSL → 汇编流程

### 编译阶段概览

```
HLSL 源代码
    ↓
[1] 词法分析 (Lexical Analysis)
    ↓
[2] 语法分析 (Syntax Analysis)
    ↓
[3] 语义分析 (Semantic Analysis)
    ↓
[4] 优化阶段 (Optimization)
    ↓
[5] 代码生成 (Code Generation)
    ↓
汇编代码 / 字节码
```

### 阶段1：词法分析

**作用**：将源代码分解为标记（tokens）

**示例**：
```hlsl
float value = 1.0;
```

**标记化**：
```
[KEYWORD: float] [IDENTIFIER: value] [OPERATOR: =] [LITERAL: 1.0] [SEMICOLON: ;]
```

### 阶段2：语法分析

**作用**：根据语法规则构建抽象语法树（AST）

**示例**：
```hlsl
float result = a * b + c;
```

**AST 结构**：
```
        =
      /   \
  result   +
          / \
        *   c
       / \
      a   b
```

### 阶段3：语义分析

**作用**：
- 类型检查
- 符号表管理
- 语义验证

**检查项**：
- 变量是否已声明
- 类型是否匹配
- 函数调用参数是否正确
- 语义是否正确使用

### 阶段4：优化阶段

**作用**：优化代码以提高性能和减少指令数

**优化类型**：
1. 常量折叠
2. 死代码消除
3. 指令合并
4. 寄存器分配
5. 循环优化

### 阶段5：代码生成

**作用**：将优化后的中间表示转换为目标汇编代码

**输出**：
- 汇编代码（文本格式）
- 字节码（二进制格式，如 DXBC/DXIL）

---

## 编译器优化

### 常量折叠（Constant Folding）

**定义**：在编译时计算常量表达式

**示例**：
```hlsl
// HLSL 源代码
float value = 2.0 * 3.0 + 1.0;
```

**优化后**：
```hlsl
// 编译器直接计算
float value = 7.0;
```

**汇编对比**：
```assembly
; 未优化
mov r0, 2.0
mov r1, 3.0
mul r2, r0, r1
mov r3, 1.0
add r4, r2, r3

; 优化后
mov r0, 7.0
```

### 死代码消除（Dead Code Elimination）

**定义**：移除永远不会执行的代码

**示例**：
```hlsl
float value = 1.0;
if (false) {  // 条件永远为假
    value = 2.0;  // 死代码，会被消除
}
return value;
```

**优化后**：
```hlsl
float value = 1.0;
return value;
```

### 指令合并（Instruction Combining）

**定义**：将多个指令合并为更高效的指令

**示例1：乘加融合**
```hlsl
// HLSL
float result = a * b + c;
```

```assembly
; 未优化（2条指令）
mul r0, a, b
add r1, r0, c

; 优化后（1条指令）
mad r1, a, b, c
```

**示例2：数学函数优化**
```hlsl
// HLSL
float result = 1.0 / sqrt(x);
```

```assembly
; 未优化（2条指令）
sqrt r0, x
rcp r1, r0

; 优化后（1条指令）
rsq r1, x
```

### 寄存器分配（Register Allocation）

**定义**：将变量分配到寄存器，最小化寄存器使用

**策略**：
- 活跃变量分析
- 寄存器重用
- 寄存器溢出（spilling）

**示例**：
```hlsl
float a = 1.0;
float b = 2.0;
float c = a + b;  // a 和 b 可以重用寄存器
float d = c * 2.0;
```

**寄存器分配**：
```
a → r0
b → r1
c → r0 (重用 a 的寄存器)
d → r1 (重用 b 的寄存器)
```

### 循环优化

#### 循环展开（Loop Unrolling）
```hlsl
// HLSL
for (int i = 0; i < 4; i++) {
    result += array[i];
}
```

**优化后**（展开为4次迭代）：
```hlsl
result += array[0];
result += array[1];
result += array[2];
result += array[3];
```

#### 循环不变式外提（Loop Invariant Code Motion）
```hlsl
// HLSL
for (int i = 0; i < 100; i++) {
    float value = sin(time) * i;  // sin(time) 不变
}
```

**优化后**：
```hlsl
float sinTime = sin(time);  // 外提到循环外
for (int i = 0; i < 100; i++) {
    float value = sinTime * i;
}
```

---

## 代码生成

### 目标格式

#### DXBC（DirectX Bytecode）
- Shader Model 4.0/5.0
- 二进制格式
- 由 FXC 编译器生成

#### DXIL（DirectX Intermediate Language）
- Shader Model 6.0+
- 基于 LLVM IR
- 由 DXC 编译器生成

### 寄存器映射

#### 输入寄存器映射
```hlsl
struct VertexInput {
    float3 position : POSITION;   // → v0
    float3 normal : NORMAL;       // → v1
    float2 uv : TEXCOORD0;        // → v2
};
```

#### 输出寄存器映射
```hlsl
struct VertexOutput {
    float4 position : SV_POSITION;  // → o0
    float2 uv : TEXCOORD0;          // → o1
};
```

#### 常量寄存器映射
```hlsl
cbuffer Constants : register(b0) {
    float4x4 matrix;  // → c0-c3
    float4 color;     // → c4
};
```

### 指令选择

编译器根据目标平台选择最优指令：

**示例**：
```hlsl
float result = saturate(x);
```

**不同平台的指令**：
```assembly
; SM 4.0+
add_sat r0, x, 0

; 或
max r0, x, 0
min r1, r0, 1
```

---

## 实践任务

### 任务1：对比优化前后的汇编代码

#### 步骤1：编写测试代码
```hlsl
float4 PS_Main(float2 uv : TEXCOORD0) : SV_Target {
    float a = 2.0;
    float b = 3.0;
    float c = a * b;
    float d = c + 1.0;
    return float4(d, d, d, 1.0);
}
```

#### 步骤2：编译（关闭优化）
```bash
fxc /T ps_5_0 /Od /Fc no_opt.asm shader.hlsl
```

#### 步骤3：编译（开启优化）
```bash
fxc /T ps_5_0 /O3 /Fc opt.asm shader.hlsl
```

#### 步骤4：对比分析
- 指令数量
- 寄存器使用
- 常量折叠效果
- 指令合并效果

### 任务2：分析编译器优化策略

#### 测试1：常量折叠
```hlsl
float value = 2.0 * 3.0 + 1.0;
```

观察编译器是否直接计算为 `7.0`。

#### 测试2：死代码消除
```hlsl
#define USE_FEATURE 0

#if USE_FEATURE
    float value = 1.0;
#endif
```

观察 `#if` 块是否被完全移除。

#### 测试3：指令合并
```hlsl
float result = a * b + c;
```

观察是否生成了 `mad` 指令。

---

## 编译器选项

### FXC 编译器选项

```bash
# 优化级别
/Od  # 禁用优化
/O1  # 优化大小
/O2  # 优化速度
/O3  # 最大优化

# 输出格式
/Fc  # 输出汇编代码
/Fo  # 输出目标文件
/Fh  # 输出头文件

# 目标着色器模型
/T vs_5_0    # 顶点着色器 5.0
/T ps_5_0    # 像素着色器 5.0
/T cs_5_0    # 计算着色器 5.0
```

### DXC 编译器选项

```bash
# 优化级别
-O0  # 禁用优化
-O1  # 优化
-O2  # 更多优化
-O3  # 最大优化

# 输出格式
-Fc  # 输出汇编代码
-Fo  # 输出目标文件

# 目标着色器模型
-T vs_6_0
-T ps_6_0
-T cs_6_0
```

---

## 🔗 相关链接

- [[Assembly_Basics]] - 生成的指令类型
- [[Assembly_Code_Analysis]] - 如何分析生成的代码
- [[Performance_Optimization]] - 优化技巧
- [[Tools_Guide]] - 编译器工具使用

---

*最后更新：2024年*

