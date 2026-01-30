@echo off
echo 正在构建 SunEyeVision 项目...
echo.

cd /d d:\MyWork\SunEyeVision\SunEyeVision
dotnet build SunEyeVision.sln --configuration Release

if %ERRORLEVEL% neq 0 (
    echo 构建失败！
    exit /b 1
)

echo 构建成功！
echo 正在启动应用程序...
echo.

start "" "bin\Release\net9.0-windows\SunEyeVision.UI.exe"

echo.
echo ==========================================
echo 连线修复 - 端口中心位置测试
echo ==========================================
echo.
echo 关键修复：
echo - 端口位置现在使用端口中心，而不是节点外侧
echo - Top端口: (X + Width/2, Y)
echo - Bottom端口: (X + Width/2, Y + Height)
echo - Left端口: (X, Y + Height/2)
echo - Right端口: (X + Width, Y + Height/2)
echo.
echo 请进行以下测试：
echo 1. 检查连线是否从端口中心开始和结束
echo 2. 检查箭头位置是否正确（在终点附近）
echo 3. 拖拽节点测试连线是否正确更新
echo 4. 测试不同端口方向的连线（右->右，右->左，上->下等）
echo.

echo 测试完成后，按任意键继续...
pause