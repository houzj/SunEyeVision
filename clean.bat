@echo off
echo ========================================
echo SunEyeVision 清理脚本
echo ========================================
echo.

echo [1/3] 清理 Visual Studio 缓存...
if exist .vs (
    echo 删除 .vs 目录...
    rd /s /q .vs
)

echo.
echo [2/3] 清理构建输出文件（bin 和 obj）...
for /d /r %%d in (bin obj) do @if exist "%%d" (
    echo 删除 %%d...
    rd /s /q "%%d"
)

echo.
echo [3/3] 清理发布目录...
if exist publish (
    echo 删除 publish 目录...
    rd /s /q publish
)

echo.
echo ========================================
echo 清理完成！
echo ========================================
pause
