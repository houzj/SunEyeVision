@echo off
chcp 65001 >nul
echo ========================================
echo    清除缩略图缓存
echo ========================================
echo.

echo 正在查找缓存目录...
set CACHE_DIR=%LOCALAPPDATA%\SunEyeVision\ThumbnailCache

if not exist "%CACHE_DIR%" (
    echo ✓ 缓存目录不存在，无需清除
    echo.
    pause
    exit /b 0
)

echo 缓存目录: %CACHE_DIR%
echo.

echo 缓存内容:
dir "%CACHE_DIR%" 2>nul | findstr /C:"个文件" /C:"个目录"
echo.

echo 是否确认删除所有缓存文件？(Y/N)
set /p CONFIRM=

if /i not "%CONFIRM%"=="Y" (
    echo 已取消清除
    pause
    exit /b 0
)

echo.
echo 正在清除缓存...
del /f /q "%CACHE_DIR%\*.*" 2>nul

if %ERRORLEVEL% equ 0 (
    echo ✓ 缓存清除成功
) else (
    echo ✗ 清除失败，请尝试手动删除
    echo   目录: %CACHE_DIR%
)

echo.
echo ========================================
echo    完成
echo ========================================
pause
