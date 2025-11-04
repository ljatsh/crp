# [[GraniteShaderLibBase]] - Granite 虚拟纹理基础

## 概述

- **文件路径**：`Packages/com.unity.render-pipelines.core/ShaderLibrary/GraniteShaderLibBase.hlsl`
- **主要职责**：定义 Granite 虚拟纹理系统的基础结构体和函数
- **使用场景**：虚拟纹理系统

## 依赖关系

- **依赖的文件**：[[Common]] - 基础类型
- **被依赖的文件**：[[VirtualTexturing]], [[TextureStack]] - 虚拟纹理相关模块

## 核心功能

提供 Granite 虚拟纹理系统的底层实现，包括：
- 查找数据结构
- 缓存纹理定义
- 翻译表定义

## 与其他模块的关系

- [[VirtualTexturing]]：依赖本文件
- [[TextureStack]]：依赖本文件

