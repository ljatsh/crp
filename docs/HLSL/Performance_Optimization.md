# 性能优化技巧

#HLSL #性能优化 #优化技巧 #性能

> 学习性能优化技巧：精度选择、指令优化、纹理采样优化、分支优化

## 📋 目录

- [精度优化](#精度优化)
- [指令优化](#指令优化)
- [纹理采样优化](#纹理采样优化)
- [分支优化](#分支优化)
- [实践任务](#实践任务)

---

## 精度优化

### `half` vs `float` 的选择

#### 何时使用 `half`
```hlsl
// ✅ 适合使用 half 的数据
half3 color;           // 颜色值（0-1范围）
half2 uv;              // 纹理坐标（通常不需要高精度）
half3 normal;          // 法线向量（归一化后）
half attenuation;     // 光照衰减因子
```

#### 何时使用 `float`
```hlsl
// ✅ 必须使用 float 的计算
float3 worldPos;        // 世界空间位置（大范围）
float4x4 matrix;      // 变换矩阵
float depth;           // 深度值（需要精度）
float time;            // 时间值（累积误差）
```

### 精度丢失的影响

#### 示例：累积误差
```hlsl
// ❌ 使用 half 累积（可能丢失精度）
half value = 0.0;
for (int i = 0; i < 100; i++) {
    value += 0.01h;  // 累积误差
}

// ✅ 使用 float 累积
float value = 0.0;
for (int i = 0; i < 100; i++) {
    value += 0.01;
}
```

#### 精度测试
```hlsl
// 测试 half 的精度
half h = 1.0h;
half h2 = h + 0.0001h;  // 可能无法表示

// float 有更高精度
float f = 1.0;
float f2 = f + 0.0001;  // 可以精确表示
```

### 移动平台优化

```hlsl
#if defined(SHADER_API_MOBILE) || defined(SHADER_API_GLES)
    #define USE_HALF_PRECISION 1
#else
    #define USE_HALF_PRECISION 0
#endif

#if USE_HALF_PRECISION
    #define REAL half
    #define REAL2 half2
    #define REAL3 half3
    #define REAL4 half4
#else
    #define REAL float
    #define REAL2 float2
    #define REAL3 float3
    #define REAL4 float4
#endif

// 使用
REAL3 color = REAL3(1.0, 0.5, 0.0);
```

---

## 指令优化

### `mad`（乘加）指令的使用

#### 利用乘加指令
```hlsl
// ❌ 两个指令
float result = a * b + c;  // mul + add

// ✅ 一个指令（如果编译器优化）
float result = mad(a, b, c);  // mad（乘加）
```

#### 手动使用 `mad`
```hlsl
// 编译器可能自动优化，但可以显式使用
float result = mad(a, b, c);
```

### `rsqrt` vs `1.0/sqrt` 的性能

```hlsl
// ❌ 两个操作：sqrt + div
float result = 1.0 / sqrt(x);

// ✅ 一个操作：rsqrt（硬件加速）
float result = rsqrt(x);
```

**性能对比**：
- `1.0 / sqrt(x)`：通常 2 条指令
- `rsqrt(x)`：通常 1 条指令，硬件加速

### 避免不必要的计算

#### 提前计算常量
```hlsl
// ❌ 在循环中重复计算
for (int i = 0; i < 100; i++) {
    float value = sin(time) * i;  // sin(time) 重复计算
}

// ✅ 提前计算
float sinTime = sin(time);
for (int i = 0; i < 100; i++) {
    float value = sinTime * i;
}
```

#### 使用 Swizzle 避免重复计算
```hlsl
// ❌ 重复访问
float x = vec.x;
float y = vec.y;
float z = vec.z;
float w = vec.w;

// ✅ 使用 Swizzle
float4 components = vec.xyzw;
```

#### 避免冗余归一化
```hlsl
// ❌ 重复归一化
float3 dir1 = normalize(vector);
float3 dir2 = normalize(vector);  // 重复计算

// ✅ 归一化一次
float3 normalized = normalize(vector);
float3 dir1 = normalized;
float3 dir2 = normalized;
```

### 向量化操作

```hlsl
// ❌ 标量操作
float r = color.r * 2.0;
float g = color.g * 2.0;
float b = color.b * 2.0;

// ✅ 向量化操作（更快）
float3 rgb = color.rgb * 2.0;
```

---

## 纹理采样优化

### 采样器状态优化

#### 选择合适的过滤模式
```hlsl
// 点过滤（最快，但质量低）
SamplerState pointSampler {
    Filter = MIN_MAG_MIP_POINT;
};

// 线性过滤（平衡）
SamplerState linearSampler {
    Filter = MIN_MAG_MIP_LINEAR;
};

// 各向异性过滤（质量高，但慢）
SamplerState anisotropicSampler {
    Filter = ANISOTROPIC;
    MaxAnisotropy = 16;
};
```

### Mipmap 使用

#### 使用合适的 LOD
```hlsl
// 自动 LOD（推荐）
float4 color = texture.Sample(sampler, uv);

// 显式 LOD（用于特殊效果）
float4 color = texture.SampleLevel(sampler, uv, mipLevel);
```

#### LOD 偏置
```hlsl
// 使用偏置调整 mipmap 级别
float4 color = texture.SampleBias(sampler, uv, bias);
```

### 纹理格式选择

#### 压缩纹理格式
```hlsl
// BC1/DXT1 - 4位/像素，适合不透明纹理
// BC3/DXT5 - 8位/像素，适合带 Alpha 的纹理
// BC4 - 单通道压缩
// BC5 - 双通道压缩（法线贴图）
// BC6H - HDR 压缩
// BC7 - 高质量压缩
```

#### 选择合适的格式
```hlsl
// 法线贴图：使用 BC5（双通道）
// 颜色贴图：使用 BC1（不透明）或 BC3（透明）
// HDR 纹理：使用 BC6H
// 细节纹理：使用 BC7
```

### 纹理采样次数优化

#### 合并采样
```hlsl
// ❌ 多次采样
float4 color1 = texture1.Sample(sampler, uv);
float4 color2 = texture2.Sample(sampler, uv);
float4 color3 = texture3.Sample(sampler, uv);

// ✅ 使用纹理数组（如果可能）
Texture2DArray textures;
float4 color = textures.Sample(sampler, float3(uv, index));
```

#### 使用纹理图集
```hlsl
// 将多个小纹理合并到一个大纹理中
// 减少纹理切换开销
```

---

## 分支优化

### 动态分支 vs 静态分支

#### 静态分支（编译时确定）
```hlsl
#define USE_FEATURE 1

#if USE_FEATURE
    // 这段代码会被编译
    float value = 1.0;
#else
    // 这段代码不会被编译（不占用运行时性能）
    float value = 0.0;
#endif
```

#### 动态分支（运行时确定）
```hlsl
// ⚠️ 性能警告：可能导致所有分支都执行
if (condition) {
    // 分支1
} else {
    // 分支2
}
```

### 分支代价分析

#### 统一执行（Uniform Flow）
```hlsl
// 如果条件对所有像素相同，性能影响较小
uniform bool useFeature;

if (useFeature) {
    // 统一分支
}
```

#### 非统一执行（Divergent Flow）
```hlsl
// 如果条件对每个像素不同，性能影响大
float threshold = 0.5;

if (value > threshold) {  // 每个像素可能不同
    // 非统一分支，性能差
}
```

### 使用数学技巧避免分支

#### 技巧1：使用 `saturate` 和 `lerp`
```hlsl
// ❌ 使用分支
float value = (x > 0.0) ? x : 0.0;

// ✅ 使用 saturate（无分支）
float value = saturate(x);
```

#### 技巧2：使用 `step` 函数
```hlsl
// ❌ 使用分支
float value = (x > threshold) ? 1.0 : 0.0;

// ✅ 使用 step（无分支）
float value = step(threshold, x);
```

#### 技巧3：使用符号函数
```hlsl
// ❌ 使用分支
float sign = (x > 0.0) ? 1.0 : -1.0;

// ✅ 使用 sign 函数
float sign = sign(x);
```

#### 技巧4：使用数学表达式
```hlsl
// ❌ 使用分支
float value = (condition) ? a : b;

// ✅ 使用 lerp（如果 condition 是 0 或 1）
float value = lerp(b, a, condition);
```

#### 技巧5：使用 `max`/`min` 代替条件
```hlsl
// ❌ 使用分支
float value = (x > y) ? x : y;

// ✅ 使用 max（无分支）
float value = max(x, y);
```

### 分支优化示例

#### 示例1：光照计算
```hlsl
// ❌ 使用分支
float NdotL = dot(normal, lightDir);
if (NdotL > 0.0) {
    float lighting = NdotL;
} else {
    float lighting = 0.0;
}

// ✅ 使用 saturate（无分支）
float NdotL = dot(normal, lightDir);
float lighting = saturate(NdotL);
```

#### 示例2：颜色选择
```hlsl
// ❌ 使用分支
float3 color = (intensity > 0.5) ? hotColor : coldColor;

// ✅ 使用 lerp（无分支）
float t = saturate((intensity - 0.5) * 2.0);
float3 color = lerp(coldColor, hotColor, t);
```

---

## 实践任务

### 任务1：对比不同实现的性能

```hlsl
// 版本1：使用分支
float CalculateLighting1(float3 normal, float3 lightDir) {
    float NdotL = dot(normal, lightDir);
    if (NdotL > 0.0) {
        return NdotL;
    } else {
        return 0.0;
    }
}

// 版本2：无分支
float CalculateLighting2(float3 normal, float3 lightDir) {
    float NdotL = dot(normal, lightDir);
    return saturate(NdotL);
}

// 使用工具分析两个版本的指令数和性能
```

### 任务2：使用工具分析指令数

使用编译器工具（FXC/DXC）查看生成的汇编代码：

```bash
# 编译并生成汇编
fxc /T ps_5_0 /Fc output.asm shader.hlsl

# 或使用 DXC
dxc -T ps_6_0 -Fc output.asm shader.hlsl
```

对比不同实现的指令数：
- 指令总数
- 纹理采样次数
- 分支指令数
- 寄存器使用数

---

## 🔗 相关链接

- [[HLSL_Data_Types]] - 精度选择
- [[HLSL_Builtin_Functions]] - 函数性能
- [[Assembly_Basics]] - 指令优化
- [[Assembly_Code_Analysis]] - 性能分析

---

*最后更新：2024年*

