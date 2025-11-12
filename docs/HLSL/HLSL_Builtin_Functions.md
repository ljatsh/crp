# HLSL 内置函数库

#HLSL #内置函数 #数学函数 #向量运算

> 学习 HLSL 的内置函数：数学函数、向量运算、纹理采样

## 📋 目录

- [数学函数](#数学函数)
- [向量运算](#向量运算)
- [纹理采样](#纹理采样)
- [实践任务](#实践任务)

---

## 数学函数

### 三角函数

#### 基本三角函数
```hlsl
float angle = 1.5708;  // π/2

float s = sin(angle);   // 正弦
float c = cos(angle);   // 余弦
float t = tan(angle);   // 正切
```

#### 反三角函数
```hlsl
float x = 0.5;

float asin_val = asin(x);   // 反正弦，返回 [-π/2, π/2]
float acos_val = acos(x);   // 反余弦，返回 [0, π]
float atan_val = atan(x);   // 反正切，返回 [-π/2, π/2]
float atan2_val = atan2(y, x);  // 反正切2，返回 [-π, π]，考虑象限
```

> 💡 **提示**：`atan2(y, x)` 比 `atan(y/x)` 更准确，因为它考虑了象限。

### 指数和对数函数

#### 指数函数
```hlsl
float x = 2.0;

float exp_val = exp(x);    // e^x
float exp2_val = exp2(x);  // 2^x
float pow_val = pow(x, 3.0);  // x^3
```

#### 对数函数
```hlsl
float x = 8.0;

float log_val = log(x);    // ln(x)，自然对数
float log2_val = log2(x);  // log₂(x)，以2为底的对数
float log10_val = log10(x); // log₁₀(x)，以10为底的对数
```

### 开方和倒数函数

```hlsl
float x = 4.0;

float sqrt_val = sqrt(x);    // √x
float rsqrt_val = rsqrt(x);  // 1/√x，平方根倒数（优化版本）
float rcp_val = rcp(x);      // 1/x，倒数（优化版本）
```

> 💡 **性能提示**：`rsqrt(x)` 通常比 `1.0 / sqrt(x)` 更快，因为它是硬件加速的。

### 取整函数

```hlsl
float x = 3.7;

float floor_val = floor(x);   // 3.0，向下取整
float ceil_val = ceil(x);     // 4.0，向上取整
float round_val = round(x);   // 4.0，四舍五入
float trunc_val = trunc(x);   // 3.0，截断（向零取整）
float frac_val = frac(x);     // 0.7，小数部分
```

### 插值函数

#### `lerp` - 线性插值
```hlsl
float a = 0.0;
float b = 10.0;
float t = 0.3;  // 插值参数 [0, 1]

float result = lerp(a, b, t);  // 3.0
// 等价于：a + (b - a) * t
```

#### `smoothstep` - 平滑插值
```hlsl
float edge0 = 0.0;
float edge1 = 1.0;
float x = 0.5;

float result = smoothstep(edge0, edge1, x);
// 返回平滑的 Hermite 插值，在 [edge0, edge1] 范围内
```

#### `saturate` - 饱和（限制到 [0, 1]）
```hlsl
float x = 1.5;

float result = saturate(x);  // 1.0
// 等价于：clamp(x, 0.0, 1.0)
```

### 其他数学函数

```hlsl
float x = -2.5;
float y = 3.0;

float abs_val = abs(x);           // 2.5，绝对值
float sign_val = sign(x);         // -1.0，符号函数
float min_val = min(x, y);        // -2.5，最小值
float max_val = max(x, y);        // 3.0，最大值
float clamp_val = clamp(x, 0.0, 1.0);  // 限制到范围 [0, 1]
```

---

## 向量运算

### 点积和叉积

#### `dot` - 点积
```hlsl
float3 a = float3(1.0, 2.0, 3.0);
float3 b = float3(4.0, 5.0, 6.0);

float dot_product = dot(a, b);  // 1*4 + 2*5 + 3*6 = 32.0
```

**应用示例**：
```hlsl
// 计算两个向量的夹角余弦值
float3 normal = float3(0.0, 0.0, 1.0);
float3 lightDir = normalize(float3(1.0, 1.0, 1.0));
float NdotL = dot(normal, lightDir);  // 用于光照计算
```

#### `cross` - 叉积（仅适用于 3D 向量）
```hlsl
float3 a = float3(1.0, 0.0, 0.0);
float3 b = float3(0.0, 1.0, 0.0);

float3 cross_product = cross(a, b);  // (0, 0, 1)
```

**应用示例**：
```hlsl
// 计算法线（用于构建切空间）
float3 tangent = float3(1.0, 0.0, 0.0);
float3 bitangent = float3(0.0, 1.0, 0.0);
float3 normal = cross(tangent, bitangent);
```

### 向量长度和距离

#### `length` - 向量长度
```hlsl
float3 vec = float3(3.0, 4.0, 0.0);
float len = length(vec);  // √(3² + 4²) = 5.0
```

#### `distance` - 两点距离
```hlsl
float3 a = float3(0.0, 0.0, 0.0);
float3 b = float3(3.0, 4.0, 0.0);
float dist = distance(a, b);  // 5.0
// 等价于：length(a - b)
```

#### `normalize` - 归一化
```hlsl
float3 vec = float3(3.0, 4.0, 0.0);
float3 normalized = normalize(vec);  // (0.6, 0.8, 0.0)
// 长度为 1 的单位向量
```

> ⚠️ **注意**：如果向量长度为零，`normalize` 可能返回未定义结果。使用 `SafeNormalize` 或先检查长度。

### 反射和折射

#### `reflect` - 反射向量
```hlsl
float3 incident = normalize(float3(1.0, -1.0, 0.0));  // 入射向量
float3 normal = float3(0.0, 1.0, 0.0);                 // 法线

float3 reflected = reflect(incident, normal);
// 计算反射方向
```

**公式**：`R = I - 2 * dot(N, I) * N`

#### `refract` - 折射向量
```hlsl
float3 incident = normalize(float3(1.0, -1.0, 0.0));  // 入射向量
float3 normal = float3(0.0, 1.0, 0.0);                 // 法线
float eta = 0.75;  // 折射率比率（n1/n2）

float3 refracted = refract(incident, normal, eta);
// 计算折射方向
```

#### `faceforward` - 面向方向
```hlsl
float3 N = float3(0.0, 0.0, 1.0);   // 法线
float3 I = float3(0.0, 0.0, -1.0);  // 入射方向
float3 Ng = float3(0.0, 0.0, 1.0);  // 几何法线

float3 facing = faceforward(N, I, Ng);
// 如果 dot(I, Ng) < 0，返回 N；否则返回 -N
```

---

## 纹理采样

### 2D 纹理采样

#### `tex2D` - 基本 2D 纹理采样
```hlsl
Texture2D myTexture;
SamplerState mySampler;
float2 uv = float2(0.5, 0.5);

float4 color = tex2D(mySampler, uv);
// 使用默认的 mipmap 级别
```

#### `tex2Dlod` - 带 LOD 的 2D 纹理采样
```hlsl
float4 uv_lod = float4(0.5, 0.5, 0.0, 2.0);  // (u, v, 0, mip_level)

float4 color = tex2Dlod(mySampler, uv_lod);
// 显式指定 mipmap 级别
```

#### `tex2Dproj` - 投影 2D 纹理采样
```hlsl
float3 uv_proj = float3(0.5, 0.5, 1.0);  // (u/w, v/w, w)

float4 color = tex2Dproj(mySampler, uv_proj);
// 自动进行透视除法
```

#### `tex2Dbias` - 带偏置的 2D 纹理采样
```hlsl
float4 uv_bias = float4(0.5, 0.5, 0.0, -1.0);  // (u, v, 0, bias)

float4 color = tex2Dbias(mySampler, uv_bias);
// 在计算的 mipmap 级别上添加偏置
```

### 立方体贴图采样

#### `texCUBE` - 立方体贴图采样
```hlsl
TextureCube myCubeMap;
SamplerState mySampler;
float3 direction = normalize(float3(1.0, 1.0, 1.0));

float4 color = texCUBE(mySampler, direction);
// 使用方向向量采样立方体贴图
```

#### `texCUBElod` - 带 LOD 的立方体贴图采样
```hlsl
float4 dir_lod = float4(1.0, 1.0, 1.0, 2.0);  // (x, y, z, mip_level)

float4 color = texCUBElod(mySampler, dir_lod);
```

### 3D 纹理采样

#### `tex3D` - 3D 纹理采样
```hlsl
Texture3D my3DTexture;
SamplerState mySampler;
float3 uvw = float3(0.5, 0.5, 0.5);

float4 color = tex3D(mySampler, uvw);
```

### 采样器状态

采样器定义了纹理的过滤和寻址模式：

```hlsl
// 线性过滤，重复寻址
SamplerState linearRepeatSampler {
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};

// 点过滤，夹取寻址
SamplerState pointClampSampler {
    Filter = MIN_MAG_MIP_POINT;
    AddressU = Clamp;
    AddressV = Clamp;
};

// 各向异性过滤
SamplerState anisotropicSampler {
    Filter = ANISOTROPIC;
    MaxAnisotropy = 16;
    AddressU = Wrap;
    AddressV = Wrap;
};
```

### 采样器比较模式（阴影贴图）

```hlsl
SamplerComparisonState shadowSampler {
    Filter = COMPARISON_MIN_MAG_LINEAR_MIP_POINT;
    ComparisonFunc = LESS;
    AddressU = Clamp;
    AddressV = Clamp;
};

Texture2D shadowMap;
float3 shadowCoord = float3(0.5, 0.5, 0.3);  // (u, v, depth)

float shadow = shadowMap.SampleCmpLevelZero(shadowSampler, shadowCoord.xy, shadowCoord.z);
// 返回比较结果（0 或 1）
```

---

## 实践任务

### 任务1：实现常用向量操作函数

```hlsl
// 计算两个向量的夹角（弧度）
float AngleBetween(float3 a, float3 b) {
    float cosAngle = dot(normalize(a), normalize(b));
    return acos(saturate(cosAngle));
}

// 线性插值向量
float3 LerpVector(float3 a, float3 b, float t) {
    return lerp(a, b, t);
}

// 球面线性插值（Slerp）
float3 Slerp(float3 a, float3 b, float t) {
    float dotVal = dot(normalize(a), normalize(b));
    dotVal = saturate(dotVal);
    float theta = acos(dotVal);
    float sinTheta = sin(theta);
    
    if (sinTheta < 0.001) {
        return lerp(a, b, t);
    }
    
    float w1 = sin((1.0 - t) * theta) / sinTheta;
    float w2 = sin(t * theta) / sinTheta;
    return w1 * normalize(a) + w2 * normalize(b);
}
```

### 任务2：测试不同纹理采样函数的性能

```hlsl
// 对比不同采样方法
float4 SampleTexture1(Texture2D tex, SamplerState samp, float2 uv) {
    return tex2D(samp, uv);  // 标准采样
}

float4 SampleTexture2(Texture2D tex, SamplerState samp, float2 uv) {
    return tex2Dlod(samp, float4(uv, 0.0, 0.0));  // 显式 LOD 0
}

float4 SampleTexture3(Texture2D tex, SamplerState samp, float2 uv) {
    return tex.Sample(samp, uv);  // 现代语法（SM 4.0+）
}
```

### 任务3：实现数学工具函数

```hlsl
// 将角度转换为弧度
float DegreesToRadians(float degrees) {
    return degrees * 3.14159265359 / 180.0;
}

// 将弧度转换为角度
float RadiansToDegrees(float radians) {
    return radians * 180.0 / 3.14159265359;
}

// 重映射值到新范围
float Remap(float value, float inMin, float inMax, float outMin, float outMax) {
    return lerp(outMin, outMax, (value - inMin) / (inMax - inMin));
}

// 平滑最小值（Smooth Minimum）
float SmoothMin(float a, float b, float k) {
    float h = saturate(0.5 + 0.5 * (b - a) / k);
    return lerp(b, a, h) - k * h * (1.0 - h);
}
```

---

## 🔗 相关链接

- [[HLSL_Data_Types]]
- [[HLSL_Syntax_and_Semantics]]
- [[Performance_Optimization]] - 纹理采样优化
- [[Resource_Binding]] - 纹理和采样器

---

*最后更新：2024年*

