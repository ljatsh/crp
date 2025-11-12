# HLSL 编译工具使用说明

## 文件说明

- `test_shader.hlsl` - 测试用的 HLSL 着色器文件
- `compile_fxc.bat` - 使用 FXC 编译器编译 HLSL（生成真正的 DXBC 格式，易读）
- `dxc/` - DirectX Shader Compiler (DXC) 工具目录
- `fxc/` - DirectX Shader Compiler (FXC) 工具目录

## 推荐：使用 FXC 编译器（生成 DXBC 格式）

FXC 编译器可以生成真正的 DXBC 格式汇编代码，格式易读，类似 RenderDoc 中看到的。

### 基本用法

```batch
REM 使用默认参数（ps_5_0 PS）
compile_fxc.bat

REM 指定参数
compile_fxc.bat test_shader.hlsl ps_5_0 PS
```

### 参数说明

```batch
compile_fxc.bat [shader_file] [shader_type] [entry_point]
```

- `shader_file`: HLSL 文件路径（相对于 Tools 目录）
- `shader_type`: 着色器类型和版本，例如：
  - `vs_5_0` - 顶点着色器 5.0
  - `ps_5_0` - 像素着色器 5.0
  - `cs_5_0` - 计算着色器 5.0
  - `gs_5_0` - 几何着色器 5.0
  - `hs_5_0` - 外壳着色器 5.0
  - `ds_5_0` - 域着色器 5.0
- `entry_point`: 入口点函数名

### 示例

```batch
REM 编译像素着色器（默认）
compile_fxc.bat test_shader.hlsl ps_5_0 PS

REM 编译顶点着色器
compile_fxc.bat test_shader.hlsl vs_5_0 VS

REM 编译计算着色器
compile_fxc.bat compute_shader.hlsl cs_5_0 CSMain
```

## 输出文件

编译后会生成：
1. `[shader_file].dxbc` - DirectX Bytecode 二进制文件
2. `[shader_file].asm` - DXBC 汇编代码文本文件（易读格式）

**DXBC 格式的优势**：
- 汇编代码更直观（mov, dp4, mul 等指令）
- 包含源码行号映射
- 函数名和注释清晰
- 寄存器使用一目了然
- 与 RenderDoc 中看到的格式一致

## FXC 编译选项说明

脚本使用的编译选项：

- `/T [shader_type]` - 指定着色器类型和版本（不会自动提升）
- `/E [entry_point]` - 指定入口点函数
- `/Fo [output]` - 指定输出二进制文件
- `/Fc [output]` - 生成汇编代码文件
- `/Od` - 禁用优化（便于调试）
- `/Zi` - 启用调试信息

### 其他有用的 FXC 选项

```batch
REM 启用优化
fxc\fxc.exe /T ps_5_0 /E PS /O3 /Fo shader.dxbc /Fc shader.asm test_shader.hlsl

REM 指定包含路径
fxc\fxc.exe /T ps_5_0 /E PS /I "C:\path\to\includes" /Fo shader.dxbc test_shader.hlsl

REM 定义预处理器宏
fxc\fxc.exe /T ps_5_0 /E PS /D "DEBUG=1" /Fo shader.dxbc test_shader.hlsl
```

## FXC vs DXC 的区别

| 特性 | FXC | DXC |
|------|-----|-----|
| Shader Model 5.x | ✅ 完全支持 | ⚠️ 可能提升到 6.0 |
| Shader Model 6.x | ❌ 不支持 | ✅ 完全支持 |
| DXBC 格式 | ✅ 原生支持 | ⚠️ 可能生成 DXIL |
| DXIL 格式 | ❌ 不支持 | ✅ 原生支持 |
| 汇编可读性 | ✅ 易读（类似 RenderDoc） | ⚠️ LLVM IR 格式 |
| 推荐用途 | 查看 DXBC 汇编 | 现代着色器开发 |

## 常见问题

### 1. 找不到 fxc.exe

确保 `fxc.exe` 在 `Tools\fxc\` 目录下。如果还没有，可以从 Windows SDK 中拷贝：
- 位置：`C:\Program Files (x86)\Windows Kits\10\bin\<版本号>\x64\fxc.exe`
- 同时需要拷贝 `d3dcompiler_47.dll`（或类似版本）

### 2. 编译错误

检查 HLSL 语法是否正确，确保：
- 入口点函数存在
- 着色器类型匹配（VS/PS/CS）
- 语法符合指定的 Shader Model 版本（5.0）

### 3. 汇编代码没有行号

确保使用了 `/Zi` 选项（脚本已包含）。

### 4. FXC 提示缺少 DLL

确保 `fxc\` 目录中包含：
- `fxc.exe`
- `d3dcompiler_47.dll`（或 `d3dcompiler_46.dll` 等）

如果仍然提示缺少 DLL，可能需要安装 Visual C++ Redistributable。

## 如何获取 FXC 编译器

FXC (FXC.exe) 包含在 Windows SDK 中。

### 下载方式

1. **Windows SDK（推荐）**：
   - 下载地址：https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/
   - 安装后，FXC 通常位于：
     ```
     C:\Program Files (x86)\Windows Kits\10\bin\<版本号>\x64\fxc.exe
     ```
   - 例如：`C:\Program Files (x86)\Windows Kits\10\bin\10.0.17763.0\x64\fxc.exe`

2. **同时需要拷贝的 DLL**：
   - `d3dcompiler_47.dll`（或类似版本）
   - 位于同一目录下

### 查找已安装的 FXC

如果已经安装了 Windows SDK，可以使用以下方法查找：

```batch
REM 在命令行中查找
dir /s "C:\Program Files (x86)\Windows Kits\10\bin\fxc.exe"
```

## 参考资料

- [DirectX Shader Compiler GitHub](https://github.com/microsoft/DirectXShaderCompiler)
- [HLSL 文档](https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl)
- [FXC 编译器文档](https://docs.microsoft.com/en-us/windows/win32/direct3dtools/fxc)
- [Shader-PlayGround](https://shader-playground.timjones.io/)
