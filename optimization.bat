@echo off
echo 开始实施 SunEyeVision 项目结构优化...

rem 检查是否在正确目录
if not exist "SunEyeVision.sln" (
    echo 错误：请在项目根目录运行此脚本
    exit /b 1
)

echo.
echo === 步骤 1: 删除冗余目录和文件 ===
rem 删除空目录
if exist "SunEyeVision.Core" (
    rmdir /s /q "SunEyeVision.Core"
    echo ✓ 已删除 SunEyeVision.Core 空目录
) else (
    echo ✓ SunEyeVision.Core 目录已不存在
)

rem 删除重复目录
if exist "SunEyeVision.Plugin.Abstractions" (
    rmdir /s /q "SunEyeVision.Plugin.Abstractions"
    echo ✓ 已删除 SunEyeVision.Plugin.Abstractions 重复目录
) else (
    echo ✓ SunEyeVision.Plugin.Abstractions 目录已不存在
)

rem 删除临时文件
if exist "temp_old_version.cs" (
    del "temp_old_version.cs"
    echo ✓ 已删除 temp_old_version.cs 临时文件
) else (
    echo ✓ 临时文件已不存在
)

echo.
echo === 步骤 2: 重组目录结构 ===
rem 创建 utilities 目录
if not exist "utilities" (
    mkdir "utilities"
    echo ✓ 已创建 utilities 目录
) else (
    echo ✓ utilities 目录已存在
)

rem 移动非工具项目
if exist "tools\ApiDocGenerator" (
    move /y "tools\ApiDocGenerator" "utilities\" > nul
    echo ✓ 已移动 ApiDocGenerator 到 utilities 目录
) else (
    echo ✓ ApiDocGenerator 已不存在或已在目标位置
)

if exist "tools\PythonService" (
    move /y "tools\PythonService" "utilities\" > nul
    echo ✓ 已移动 PythonService 到 utilities 目录
) else (
    echo ✓ PythonService 已不存在或已在目标位置
)

rem 移动 adaptagrams 到 third_party
if exist "adaptagrams" (
    if not exist "third_party" mkdir "third_party"
    move /y "adaptagrams" "third_party\" > nul
    echo ✓ 已移动 adaptagrams 到 third_party 目录
) else (
    echo ✓ adaptagrams 已不存在或已在目标位置
)

echo.
echo === 步骤 3: 整理文档 ===
if not exist "docs\reports" (
    mkdir "docs\reports"
    echo ✓ 已创建 docs/reports 目录
) else (
    echo ✓ docs/reports 目录已存在
)

rem 移动根目录的 MD 文件到 docs/reports
set "moved_count=0"
for %%f in (*.md) do (
    if not "%%f"=="SunEyeVision.sln" (
        move /y "%%f" "docs\reports\" > nul
        set /a moved_count+=1
    )
)

echo ✓ 已移动 %moved_count% 个 MD 文件到 docs/reports 目录

echo.
echo === 优化完成 ===
echo 以下是执行的操作：
echo 1. 删除了冗余目录：SunEyeVision.Core、SunEyeVision.Plugin.Abstractions
echo 2. 删除了临时文件：temp_old_version.cs
echo 3. 创建了 utilities 目录并移动了非工具项目
echo 4. 移动了 adaptagrams 到 third_party 目录
echo 5. 整理了文档文件到 docs/reports 目录

echo.
echo 请检查项目是否正常编译，如有需要请运行 build.bat 重新构建项目。