@echo off
echo 正在编译PNG到ICO转换工具...
cd /d "d:\MyWork\SunEyeVision_Dev"
csc -out:tools\PngToIcoConverter.exe tools\PngToIcoConverter.cs /reference:System.Drawing.dll

if %ERRORLEVEL% EQU 0 (
    echo ✅ 编译成功
    echo 正在执行转换...
    tools\PngToIcoConverter.exe
    
    echo.
    echo 正在复制到项目目录...
    copy /Y "C:\Users\houzhongjie\Desktop\logo.ico" "src\UI\logo.ico"
    
    if %ERRORLEVEL% EQU 0 (
        echo ✅ 图标已复制到项目目录
        echo.
        echo 现在你可以重新编译项目，新的logo就会生效！
    ) else (
        echo ❌ 复制失败
    )
) else (
    echo ❌ 编译失败
)

pause
