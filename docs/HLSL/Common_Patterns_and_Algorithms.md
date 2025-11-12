# 常见模式和算法

#HLSL #算法 #模式 #光照 #后处理

> 实现常见模式和算法：光照计算、颜色空间转换、噪声函数、后处理效果

## 📋 目录

- [光照计算](#光照计算)
- [颜色空间转换](#颜色空间转换)
- [噪声函数](#噪声函数)
- [后处理效果](#后处理效果)
- [实践任务](#实践任务)

---

## 光照计算

### Lambert 漫反射

#### 基本实现
```hlsl
float3 CalculateLambert(float3 normal, float3 lightDir, float3 lightColor) {
    float NdotL = saturate(dot(normal, lightDir));
    return lightColor * NdotL;
}
```

#### 完整示例
```hlsl
struct LightingData {
    float3 normal;
    float3 lightDir;
    float3 lightColor;
    float attenuation;
};

float3 CalculateLambert(LightingData data) {
    float NdotL = saturate(dot(data.normal, data.lightDir));
    return data.lightColor * NdotL * data.attenuation;
}
```

### Phong/Blinn-Phong 高光

#### Phong 高光
```hlsl
float3 CalculatePhong(
    float3 normal,
    float3 lightDir,
    float3 viewDir,
    float3 lightColor,
    float shininess
) {
    float3 reflectDir = reflect(-lightDir, normal);
    float RdotV = saturate(dot(reflectDir, viewDir));
    float specular = pow(RdotV, shininess);
    return lightColor * specular;
}
```

#### Blinn-Phong 高光（更高效）
```hlsl
float3 CalculateBlinnPhong(
    float3 normal,
    float3 lightDir,
    float3 viewDir,
    float3 lightColor,
    float shininess
) {
    float3 halfDir = normalize(lightDir + viewDir);
    float NdotH = saturate(dot(normal, halfDir));
    float specular = pow(NdotH, shininess);
    return lightColor * specular;
}
```

### 法线贴图采样

#### 切线空间法线贴图
```hlsl
struct VertexData {
    float3 position;
    float3 normal;
    float3 tangent;
    float2 uv;
};

float3 SampleNormalMap(
    Texture2D normalMap,
    SamplerState sampler,
    float2 uv,
    float3 normal,
    float3 tangent
) {
    // 采样法线贴图（假设存储在 [0, 1] 范围）
    float3 normalMapSample = normalMap.Sample(sampler, uv).rgb;
    
    // 转换到 [-1, 1] 范围
    float3 tangentNormal = normalMapSample * 2.0 - 1.0;
    
    // 构建切空间基向量
    float3 bitangent = cross(normal, tangent);
    float3x3 TBN = float3x3(tangent, bitangent, normal);
    
    // 转换到世界空间
    return normalize(mul(tangentNormal, TBN));
}
```

---

## 颜色空间转换

### RGB ↔ HSV

#### RGB 转 HSV
```hlsl
float3 RGBToHSV(float3 rgb) {
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(rgb.bg, K.wz), float4(rgb.gb, K.xy), step(rgb.b, rgb.g));
    float4 q = lerp(float4(p.xyw, rgb.r), float4(rgb.r, p.yzx), step(p.x, rgb.r));
    
    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}
```

#### HSV 转 RGB
```hlsl
float3 HSVToRGB(float3 hsv) {
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);
    return hsv.z * lerp(K.xxx, saturate(p - K.xxx), hsv.y);
}
```

### Linear ↔ sRGB

#### Linear 转 sRGB（编码）
```hlsl
float3 LinearToSRGB(float3 linear) {
    return lerp(
        12.92 * linear,
        1.055 * pow(linear, 1.0 / 2.4) - 0.055,
        step(0.0031308, linear)
    );
}
```

#### sRGB 转 Linear（解码）
```hlsl
float3 SRGBToLinear(float3 srgb) {
    return lerp(
        srgb / 12.92,
        pow((srgb + 0.055) / 1.055, 2.4),
        step(0.04045, srgb)
    );
}
```

### Gamma 校正

#### 应用 Gamma
```hlsl
float3 ApplyGamma(float3 color, float gamma) {
    return pow(color, 1.0 / gamma);
}
```

#### 移除 Gamma
```hlsl
float3 RemoveGamma(float3 color, float gamma) {
    return pow(color, gamma);
}
```

---

## 噪声函数

### 随机数生成

#### 简单的伪随机函数
```hlsl
float Random(float2 st) {
    return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
}
```

#### 基于位置的随机
```hlsl
float3 Random3(float3 p) {
    p = frac(p * float3(443.8975, 397.2973, 491.1871));
    p += dot(p, p.yxz + 19.19);
    return frac((p.xxy + p.yxx) * p.zyx);
}
```

### 噪声函数实现

#### 值噪声（Value Noise）
```hlsl
float ValueNoise(float2 p) {
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);  // Smoothstep
    
    float a = Random(i);
    float b = Random(i + float2(1.0, 0.0));
    float c = Random(i + float2(0.0, 1.0));
    float d = Random(i + float2(1.0, 1.0));
    
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}
```

#### 梯度噪声（Gradient Noise / Perlin Noise）
```hlsl
float2 Gradient(int hash) {
    int h = hash & 15;
    float2 grad = float2(
        (h & 1) ? 1.0 : -1.0,
        (h & 2) ? 1.0 : -1.0
    );
    return grad;
}

float GradientNoise(float2 p) {
    float2 i = floor(p);
    float2 f = frac(p);
    
    float a = dot(Gradient(int(Random(i) * 256.0)), f);
    float b = dot(Gradient(int(Random(i + float2(1.0, 0.0)) * 256.0)), f - float2(1.0, 0.0));
    float c = dot(Gradient(int(Random(i + float2(0.0, 1.0)) * 256.0)), f - float2(0.0, 1.0));
    float d = dot(Gradient(int(Random(i + float2(1.0, 1.0)) * 256.0)), f - float2(1.0, 1.0));
    
    f = f * f * (3.0 - 2.0 * f);  // Smoothstep
    
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}
```

#### 分形布朗运动（FBM）
```hlsl
float FBM(float2 p, int octaves) {
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;
    
    for (int i = 0; i < octaves; i++) {
        value += amplitude * ValueNoise(p * frequency);
        amplitude *= 0.5;
        frequency *= 2.0;
    }
    
    return value;
}
```

---

## 后处理效果

### 模糊算法

#### 简单盒式模糊
```hlsl
float4 BoxBlur(
    Texture2D source,
    SamplerState sampler,
    float2 uv,
    float2 texelSize,
    int radius
) {
    float4 color = 0.0;
    float weight = 0.0;
    
    for (int x = -radius; x <= radius; x++) {
        for (int y = -radius; y <= radius; y++) {
            float2 offset = float2(x, y) * texelSize;
            color += source.Sample(sampler, uv + offset);
            weight += 1.0;
        }
    }
    
    return color / weight;
}
```

#### 高斯模糊
```hlsl
float Gaussian(float x, float sigma) {
    return exp(-(x * x) / (2.0 * sigma * sigma));
}

float4 GaussianBlur(
    Texture2D source,
    SamplerState sampler,
    float2 uv,
    float2 texelSize,
    float sigma
) {
    float4 color = 0.0;
    float weight = 0.0;
    int radius = int(ceil(sigma * 3.0));
    
    for (int x = -radius; x <= radius; x++) {
        for (int y = -radius; y <= radius; y++) {
            float2 offset = float2(x, y) * texelSize;
            float w = Gaussian(length(float2(x, y)), sigma);
            color += source.Sample(sampler, uv + offset) * w;
            weight += w;
        }
    }
    
    return color / weight;
}
```

### 边缘检测

#### Sobel 边缘检测
```hlsl
float SobelEdge(
    Texture2D source,
    SamplerState sampler,
    float2 uv,
    float2 texelSize
) {
    // Sobel 算子
    float3x3 sobelX = float3x3(
        -1, 0, 1,
        -2, 0, 2,
        -1, 0, 1
    );
    
    float3x3 sobelY = float3x3(
        -1, -2, -1,
         0,  0,  0,
         1,  2,  1
    );
    
    float gx = 0.0;
    float gy = 0.0;
    
    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            float2 offset = float2(x, y) * texelSize;
            float sample = source.Sample(sampler, uv + offset).r;
            
            gx += sample * sobelX[y + 1][x + 1];
            gy += sample * sobelY[y + 1][x + 1];
        }
    }
    
    return sqrt(gx * gx + gy * gy);
}
```

### 色调映射

#### Reinhard 色调映射
```hlsl
float3 ReinhardToneMapping(float3 color, float exposure) {
    color *= exposure;
    return color / (1.0 + color);
}
```

#### ACES 色调映射（近似）
```hlsl
float3 ACESToneMapping(float3 color, float exposure) {
    color *= exposure;
    
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;
    
    return saturate((color * (a * color + b)) / (color * (c * color + d) + e));
}
```

#### 曝光调整
```hlsl
float3 AdjustExposure(float3 color, float exposure) {
    return color * exp2(exposure);
}
```

---

## 实践任务

### 任务1：实现完整的光照模型

```hlsl
struct Material {
    float3 albedo;
    float roughness;
    float metallic;
    float3 emissive;
};

struct Light {
    float3 direction;
    float3 color;
    float intensity;
};

float3 CalculatePBR(
    Material material,
    Light light,
    float3 normal,
    float3 viewDir
) {
    float3 lightDir = normalize(light.direction);
    float3 halfDir = normalize(lightDir + viewDir);
    
    float NdotL = saturate(dot(normal, lightDir));
    float NdotV = saturate(dot(normal, viewDir));
    float NdotH = saturate(dot(normal, halfDir));
    float VdotH = saturate(dot(viewDir, halfDir));
    
    // 简化的 PBR 计算
    float3 diffuse = material.albedo / 3.14159;
    float3 specular = pow(NdotH, (1.0 - material.roughness) * 128.0);
    
    float3 color = (diffuse + specular) * light.color * light.intensity * NdotL;
    color += material.emissive;
    
    return color;
}
```

### 任务2：实现常用的后处理效果

```hlsl
// 完整的后处理管线
float4 PostProcess(
    Texture2D source,
    SamplerState sampler,
    float2 uv,
    float2 texelSize,
    float time
) {
    // 1. 色调映射
    float4 color = source.Sample(sampler, uv);
    color.rgb = ACESToneMapping(color.rgb, 1.0);
    
    // 2. 颜色调整
    color.rgb = AdjustExposure(color.rgb, 0.1);
    
    // 3. 边缘检测（可选）
    float edge = SobelEdge(source, sampler, uv, texelSize);
    color.rgb = lerp(color.rgb, float3(0, 0, 0), edge * 0.5);
    
    return color;
}
```

---

## 🔗 相关链接

- [[HLSL_Builtin_Functions]]
- [[Performance_Optimization]] - 算法优化
- [[Assembly_Basics]] - 算法实现分析

---

*最后更新：2024年*

