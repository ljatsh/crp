# [[ACES]] - ACES 颜色空间转换

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/ACES.hlsl`
- **主要职责**：实现 ACES（Academy Color Encoding System）颜色空间转换函数
- **使用场景**：电影级渲染管线中的颜色管理和色调映射

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型和数学函数
- **被依赖的文件**：[[Color]] - 使用 ACES 转换函数

## ACES 简介

ACES（Academy Color Encoding System）是由美国电影艺术与科学学院开发的颜色编码系统，旨在：
- 提供统一的工作流程颜色空间
- 确保不同设备和软件之间的颜色一致性
- 支持高动态范围（HDR）内容

## 核心颜色空间

### AP0 (ACES 2065-1)

- **定义**：ACES 主要工作空间，使用超宽色域
- **用途**：内部计算和存储
- **特点**：覆盖几乎所有人眼可见的颜色

### AP1 (ACEScg)

- **定义**：ACES 计算机图形工作空间
- **用途**：CG 渲染和合成
- **特点**：比 AP0 更小但仍覆盖大部分颜色，更适合实时渲染

## 核心转换矩阵

### sRGB ↔ AP0 / AP1

```hlsl
static const half3x3 sRGB_2_AP0 = {
    0.4397010, 0.3829780, 0.1773350,
    0.0897923, 0.8134230, 0.0967616,
    0.0175440, 0.1115440, 0.8707040
};

static const half3x3 sRGB_2_AP1 = {
    0.61319, 0.33951, 0.04737,
    0.07021, 0.91634, 0.01345,
    0.02062, 0.10957, 0.86961
};
```

### AP0 ↔ AP1

```hlsl
static const half3x3 AP0_2_AP1_MAT = {
     1.4514393161, -0.2365107469, -0.2149285693,
    -0.0765537734,  1.1762296998, -0.0996759264,
     0.0083161484, -0.0060324498,  0.9977163014
};
```

### XYZ ↔ AP1

```hlsl
static const half3x3 AP1_2_XYZ_MAT = {
     0.6624541811, 0.1340042065, 0.1561876870,
     0.2722287168, 0.6740817658, 0.0536895174,
    -0.0055746495, 0.0040607335, 1.0103391003
};
```

## 核心函数

### `ACEScgToACES2065_1(real3 acescg)`

- **签名**：`real3 ACEScgToACES2065_1(real3 acescg)`
- **功能**：将 ACEScg (AP1) 转换到 ACES 2065-1 (AP0)
- **实现原理**：
  ```hlsl
  real3 ACEScgToACES2065_1(real3 acescg)
  {
      return mul(AP1_2_AP0_MAT, acescg);
  }
  ```
- **数学原理**：
  - 使用预计算的矩阵 `AP1_2_AP0_MAT`
  - 矩阵乘法：`AP0 = AP1_2_AP0_MAT * AP1`

### `ACES2065_1ToACEScg(real3 aces2065_1)`

- **签名**：`real3 ACES2065_1ToACEScg(real3 aces2065_1)`
- **功能**：将 ACES 2065-1 (AP0) 转换到 ACEScg (AP1)
- **实现原理**：
  ```hlsl
  real3 ACES2065_1ToACEScg(real3 aces2065_1)
  {
      return mul(AP0_2_AP1_MAT, aces2065_1);
  }
  ```

### RRT (Reference Rendering Transform)

RRT 是 ACES 色调映射的核心部分，将场景参考值（Scene Referred）转换为显示参考值（Display Referred）。

- **功能**：将高动态范围场景参考值映射到显示范围
- **特点**：
  - 保持感知一致性
  - 处理极端亮度值
  - 提供电影级外观

### ODT (Output Device Transform)

ODT 将显示参考值转换为特定显示设备的输出。

- **功能**：适配不同的显示设备（sRGB、Rec.2020、DCI-P3 等）
- **特点**：
  - 考虑显示设备的色域限制
  - 处理显示设备的亮度响应

## ACEScc 和 ACEScct

### ACEScc

- **定义**：ACES 场景参考的对数编码
- **用途**：非线性编辑和颜色分级
- **特点**：对数编码，适合人眼感知

### ACEScct

- **定义**：ACEScc 的改进版本
- **用途**：更好的暗部处理和颜色分级
- **特点**：在暗部使用线性编码，亮部使用对数编码

## 常量定义

```hlsl
#define ACEScc_MAX      1.4679964
#define ACEScc_MIDGRAY  0.4135884
```

## 使用场景

1. **HDR 渲染管线**：
   - 在 AP1 空间中计算光照
   - 使用 RRT + ODT 进行色调映射
   - 输出到目标显示设备

2. **电影制作**：
   - 统一的工作流程颜色空间
   - 跨软件和设备的颜色一致性

3. **游戏开发**：
   - 电影级视觉效果
   - HDR 支持

## 与其他模块的关系

- [[Common]]：依赖基础类型和数学函数
- [[Color]]：使用 ACES 转换函数

## 参考资料

- ACES 官方文档：https://github.com/ampas/aces-dev
- ACES 官方网站：https://www.oscars.org/science-technology/aces
- ACES 技术文档：https://github.com/ampas/aces-dev/tree/master/documents

