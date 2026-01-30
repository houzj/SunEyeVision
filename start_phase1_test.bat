@echo off
echo ========================================
echo SunEyeVision - Phase 1 测试启动
echo ========================================
echo.

echo [1] 检查构建状态...
cd /d d:\MyWork\SunEyeVision\SunEyeVision
dotnet build SunEyeVision.sln --configuration Release --no-restore --verbosity quiet
if %ERRORLEVEL% NEQ 0 (
    echo ❌ 构建失败，请检查代码
    pause
    exit /b 1
)
echo ✅ 构建成功
echo.

echo [2] Phase 1 功能清单
echo.
echo ✓ 正交折线路径（替代贝塞尔曲线）
echo ✓ 四种智能路径策略
echo ✓ 自动箭头定位和旋转
echo ✓ 支持四个端口方向
echo.

echo [3] 测试建议
echo.
echo 1. 创建节点和连线
echo 2. 拖拽节点测试路径更新
echo 3. 测试不同端口方向的连接
echo 4. 观察箭头的正确显示
echo.

echo [4] 测试指南
echo.
echo 详细测试指南请查看:
echo MANUAL_TEST_GUIDE.md
echo.

echo ========================================
echo 正在启动应用程序...
echo ========================================
echo.

dotnet run --project SunEyeVision.UI/SunEyeVision.UI.csproj --configuration Release

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ❌ 应用程序启动失败
    pause
)
