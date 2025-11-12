@echo off
REM HLSL 编译脚本 - 使用 FXC 编译器（生成真正的 DXBC 格式）
REM 用法: compile_fxc.bat [shader_file] [shader_type] [entry_point]
REM 示例: compile_fxc.bat test_shader.hlsl ps_5_0 PS

setlocal enabledelayedexpansion

REM 设置默认值
set "SHADER_FILE=%~1"
set "SHADER_TYPE=%~2"
set "ENTRY_POINT=%~3"

REM 如果没有提供参数，使用默认值
if "%SHADER_FILE%"=="" set "SHADER_FILE=test_shader.hlsl"
if "%SHADER_TYPE%"=="" set "SHADER_TYPE=ps_5_0"
if "%ENTRY_POINT%"=="" set "ENTRY_POINT=PS"

REM 获取脚本所在目录
set "SCRIPT_DIR=%~dp0"
set "FXC_PATH=%SCRIPT_DIR%fxc\fxc.exe"

REM 检查 FXC 是否存在
if not exist "%FXC_PATH%" (
    echo Error: fxc.exe not found at %FXC_PATH%
    echo Please make sure fxc.exe is in Tools\fxc\ directory
    pause
    exit /b 1
)

REM 检查着色器文件是否存在
if not exist "%SCRIPT_DIR%%SHADER_FILE%" (
    echo Error: Shader file not found: %SCRIPT_DIR%%SHADER_FILE%
    pause
    exit /b 1
)

REM 设置输出文件名
set "OUTPUT_DXBC=%SHADER_FILE%.dxbc"
set "OUTPUT_ASM=%SHADER_FILE%.asm"

echo ========================================
echo HLSL Compiler - FXC (DXBC Format)
echo ========================================
echo Shader File: %SHADER_FILE%
echo Shader Type: %SHADER_TYPE%
echo Entry Point: %ENTRY_POINT%
echo Output DXBC Binary: %OUTPUT_DXBC%
echo Output Assembly: %OUTPUT_ASM%
echo ========================================
echo.

REM 编译为 DXBC 二进制文件
echo [1/2] Compiling to DXBC format...
"%FXC_PATH%" /T %SHADER_TYPE% /E %ENTRY_POINT% /Fo "%SCRIPT_DIR%%OUTPUT_DXBC%" /Fc "%SCRIPT_DIR%%OUTPUT_ASM%" /Od /Zi "%SCRIPT_DIR%%SHADER_FILE%"
if errorlevel 1 (
    echo.
    echo Compilation failed!
    pause
    exit /b 1
)

echo [2/2] Compilation successful!
echo.
echo ========================================
echo Output files:
echo   - DXBC Binary: %OUTPUT_DXBC%
echo   - DXBC Assembly: %OUTPUT_ASM%
echo ========================================
echo.
echo Viewing DXBC assembly file...
echo ========================================
echo.

REM 显示汇编文件内容
if exist "%SCRIPT_DIR%%OUTPUT_ASM%" (
    type "%SCRIPT_DIR%%OUTPUT_ASM%"
) else (
    echo Assembly file not found: %OUTPUT_ASM%
)

echo.
echo ========================================
echo Done!
echo ========================================
pause

