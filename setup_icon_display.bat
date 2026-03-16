@echo off
echo ========================================
echo   SunEyeVision 图标设置助手
echo ========================================
echo.

echo 步骤 1: 检查图标文件...
cd /d "d:\MyWork\SunEyeVision\SunEyeVision"
if not exist "src\UI\Icons\solution.ico" (
    echo ❌ 错误: solution.ico 不存在！
    pause
    exit /b 1
)
echo ✅ 主图标存在: solution.ico
dir "src\UI\Icons\solution.ico" | find "solution.ico"
echo.

echo 步骤 2: 编译项目...
echo   正在编译 SunEyeVision.UI...
dotnet build src/UI/SunEyeVision.UI.csproj -c Release -v minimal > build_icon_log.txt 2>&1
if errorlevel 1 (
    echo ❌ 编译失败！
    type build_icon_log.txt
    pause
    exit /b 1
)
echo ✅ 编译成功
echo.

echo 步骤 3: 检查部署的图标...
if not exist "src\UI\bin\Release\net9.0-windows\Icons\solution.ico" (
    echo ❌ 错误: 输出目录中没有图标！
    pause
    exit /b 1
)
echo ✅ 图标已部署到输出目录
dir "src\UI\bin\Release\net9.0-windows\Icons\solution.ico" | find "solution.ico"
echo.

echo 步骤 4: 创建测试文件...
echo {"Name":"测试图标","Type":"Solution"} > test_icon_display.solution
echo ✅ 测试文件已创建: test_icon_display.solution
echo.

echo 步骤 5: 运行程序（注册文件关联）...
echo   正在启动 SunEyeVision.UI...
echo   程序会自动注册文件关联
echo   请等待程序完全启动后关闭它
echo.
start "" "src\UI\bin\Release\net9.0-windows\SunEyeVision.UI.exe"
echo ✅ 程序已启动
echo.

echo ========================================
echo   设置完成！
echo ========================================
echo.
echo 现在请执行以下操作以查看图标：
echo.
echo 1. 关闭 SunEyeVision.UI 程序
echo 2. 在文件资源管理器中刷新（按 F5）
echo 3. 查看项目目录中的 test_icon_display.solution 文件
echo 4. 如果图标仍未更新，请运行: clear_icon_cache.bat
echo.
echo 提示: 图标可能需要几秒钟才能更新
echo.
pause
