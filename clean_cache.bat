@echo off
echo 正在清理编译缓存...

REM 清理 UI 编译缓存
if exist "src\UI\bin" (
    rmdir /s /q "src\UI\bin"
    echo 已清理 src\UI\bin
)

if exist "src\UI\obj" (
    rmdir /s /q "src\UI\obj"
    echo 已清理 src\UI\obj
)

echo.
echo 编译缓存清理完成！
echo.
pause
