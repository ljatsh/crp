# HLSL 语法和语义

#HLSL #语法 #语义 #基础

> 学习 HLSL 的变量声明、函数、控制流和作用域规则

## 📋 目录

- [变量声明和作用域](#变量声明和作用域)
- [函数](#函数)
- [控制流](#控制流)
- [实践任务](#实践任务)

---

## 变量声明和作用域

### 变量声明

#### 基本声明
```hlsl
float value;              // 未初始化
float value2 = 1.0;       // 初始化
float3 position = float3(0.0, 1.0, 2.0);
```

#### 类型修饰符

**`const` - 常量**
```hlsl
const float PI = 3.14159265359;
const float4 white = float4(1.0, 1.0, 1.0, 1.0);

// const 变量必须在声明时初始化
// const float uninitialized;  // ❌ 错误
```

**`static` - 静态变量**
```hlsl
void MyFunction() {
    static int counter = 0;  // 静态变量在函数调用之间保持值
    counter++;
    // counter 的值会持续存在
}
```

**`uniform` - 统一变量（已弃用）**
```hlsl
// ⚠️ 在现代 HLSL 中，uniform 关键字已弃用
// 应该使用常量缓冲区（CBuffer）代替
uniform float time;  // 不推荐
```

### 作用域规则

#### 全局作用域
```hlsl
// 全局变量
float globalValue = 1.0;

void MyFunction() {
    // 可以访问全局变量
    float result = globalValue * 2.0;
}
```

#### 局部作用域
```hlsl
void MyFunction() {
    float localVar = 1.0;
    
    if (true) {
        float blockVar = 2.0;  // 块作用域
        // localVar 在这里可以访问
    }
    
    // blockVar 在这里不可访问
    // float x = blockVar;  // ❌ 错误
}
```

#### 变量遮蔽
```hlsl
float value = 1.0;

void MyFunction() {
    float value = 2.0;  // 遮蔽全局变量
    // 这里 value 是 2.0
}
```

### 寄存器绑定语义

在着色器中，变量可以通过语义绑定到特定寄存器：

```hlsl
struct VertexInput {
    float3 position : POSITION;   // 绑定到位置输入
    float3 normal : NORMAL;       // 绑定到法线输入
    float2 uv : TEXCOORD0;        // 绑定到纹理坐标0
};

struct VertexOutput {
    float4 position : SV_POSITION;  // 系统值：裁剪空间位置
    float2 uv : TEXCOORD0;          // 用户定义：纹理坐标
};
```

---

## 函数

### 函数声明和定义

#### 基本函数
```hlsl
// 函数声明
float CalculateDistance(float3 a, float3 b);

// 函数定义
float CalculateDistance(float3 a, float3 b) {
    float3 diff = a - b;
    return length(diff);
}
```

#### 内联函数
```hlsl
// 简单函数通常会被编译器自动内联
float Square(float x) {
    return x * x;
}
```

### 参数传递

#### 值传递（默认）
```hlsl
void ModifyValue(float x) {
    x = x * 2.0;  // 只修改局部副本
}

void Test() {
    float value = 1.0;
    ModifyValue(value);
    // value 仍然是 1.0（未改变）
}
```

#### 引用传递（`inout`）
```hlsl
void ModifyValue(inout float x) {
    x = x * 2.0;  // 修改原始值
}

void Test() {
    float value = 1.0;
    ModifyValue(value);
    // value 现在是 2.0（已改变）
}
```

#### 输出参数（`out`）
```hlsl
void Calculate(float x, out float squared, out float cubed) {
    squared = x * x;
    cubed = x * x * x;
}

void Test() {
    float sq, cb;
    Calculate(2.0, sq, cb);
    // sq = 4.0, cb = 8.0
}
```

#### 输入参数（`in`）
```hlsl
// in 是默认行为，通常不需要显式指定
void ProcessValue(in float x) {
    // x 是只读的
}
```

> ⚠️ **注意**：根据 Unity 的 HLSL 规范，`in` 关键字不应该使用，只使用 `out` 或 `inout`。

### 函数重载

HLSL 支持函数重载（基于参数类型和数量）：

```hlsl
// 重载1：两个 float3
float Distance(float3 a, float3 b) {
    return length(a - b);
}

// 重载2：两个 float4
float Distance(float4 a, float4 b) {
    return length(a - b);
}

// 重载3：标量
float Distance(float a, float b) {
    return abs(a - b);
}
```

### 函数返回类型

```hlsl
// 标量返回
float GetValue() {
    return 1.0;
}

// 向量返回
float3 GetPosition() {
    return float3(0.0, 1.0, 2.0);
}

// 结构体返回
struct Result {
    float value;
    float3 position;
};

Result GetResult() {
    Result r;
    r.value = 1.0;
    r.position = float3(0.0, 1.0, 2.0);
    return r;
}

// void 返回
void DoSomething() {
    // 执行操作，不返回值
}
```

---

## 控制流

### 条件语句

#### `if-else`
```hlsl
float GetValue(float x) {
    if (x > 0.0) {
        return x;
    } else {
        return -x;
    }
}

// 单行 if
if (x > 0.0) return x;
```

#### `switch-case`
```hlsl
float GetColor(int index) {
    switch (index) {
        case 0:
            return 1.0;  // 红色
        case 1:
            return 2.0;  // 绿色
        case 2:
            return 3.0;  // 蓝色
        default:
            return 0.0;  // 黑色
    }
}
```

> ⚠️ **性能注意**：在着色器中，`switch` 语句可能被编译为多个 `if` 语句，导致性能问题。考虑使用数学技巧避免分支。

### 循环语句

#### `for` 循环
```hlsl
float SumArray(float array[10]) {
    float sum = 0.0;
    for (int i = 0; i < 10; i++) {
        sum += array[i];
    }
    return sum;
}

// 循环展开（编译器优化）
for (int i = 0; i < 4; i++) {
    // 编译器可能展开为4次迭代
}
```

#### `while` 循环
```hlsl
float FindValue(float array[10], float target) {
    int i = 0;
    while (i < 10 && array[i] != target) {
        i++;
    }
    return (i < 10) ? array[i] : 0.0;
}
```

#### `do-while` 循环
```hlsl
int i = 0;
do {
    // 执行操作
    i++;
} while (i < 10);
```

### 分支性能考虑

#### 动态分支 vs 静态分支

**静态分支**（编译时确定）：
```hlsl
#define USE_FEATURE 1

#if USE_FEATURE
    // 这段代码会被编译
    float value = 1.0;
#else
    // 这段代码不会被编译
    float value = 0.0;
#endif
```

**动态分支**（运行时确定）：
```hlsl
// ⚠️ 性能警告：可能导致所有分支都执行
if (condition) {
    // 分支1
} else {
    // 分支2
}
```

#### 避免分支的技巧

**技巧1：使用数学函数**
```hlsl
// ❌ 使用分支
float value = (x > 0.0) ? x : -x;

// ✅ 使用数学函数（无分支）
float value = abs(x);
```

**技巧2：使用 `saturate` 和 `lerp`**
```hlsl
// ❌ 使用分支
float value = (x > 0.0) ? x : 0.0;

// ✅ 使用 saturate（无分支）
float value = saturate(x);
```

**技巧3：使用符号函数**
```hlsl
// ❌ 使用分支
float sign = (x > 0.0) ? 1.0 : -1.0;

// ✅ 使用 sign 函数
float sign = sign(x);
```

**技巧4：使用 `step` 函数**
```hlsl
// ❌ 使用分支
float value = (x > threshold) ? 1.0 : 0.0;

// ✅ 使用 step（无分支）
float value = step(threshold, x);
```

---

## 实践任务

### 任务1：实现常用数学函数

```hlsl
// 实现 clamp 函数
float MyClamp(float x, float minVal, float maxVal) {
    return max(minVal, min(x, maxVal));
}

// 实现 smoothstep
float MySmoothstep(float edge0, float edge1, float x) {
    float t = saturate((x - edge0) / (edge1 - edge0));
    return t * t * (3.0 - 2.0 * t);
}

// 实现 lerp
float MyLerp(float a, float b, float t) {
    return a + (b - a) * t;
}
```

### 任务2：编写条件分支和循环的测试代码

```hlsl
// 测试不同分支实现的性能
float BranchTest1(float x) {
    // 使用 if-else
    if (x > 0.5) {
        return x * 2.0;
    } else {
        return x * 0.5;
    }
}

float BranchTest2(float x) {
    // 使用数学函数避免分支
    float factor = lerp(0.5, 2.0, step(0.5, x));
    return x * factor;
}

// 测试循环展开
float LoopTest(float array[4]) {
    float sum = 0.0;
    for (int i = 0; i < 4; i++) {
        sum += array[i];
    }
    return sum;
}
```

### 任务3：函数重载实践

```hlsl
// 实现多个版本的距离计算函数
float Distance(float a, float b) {
    return abs(a - b);
}

float Distance(float2 a, float2 b) {
    return length(a - b);
}

float Distance(float3 a, float3 b) {
    return length(a - b);
}

float Distance(float4 a, float4 b) {
    return length(a - b);
}
```

---

## 🔗 相关链接

- [[HLSL_Data_Types]]
- [[HLSL_Builtin_Functions]]
- [[Performance_Optimization]] - 分支优化
- [[Assembly_Basics]] - 分支指令

---

*最后更新：2024年*

