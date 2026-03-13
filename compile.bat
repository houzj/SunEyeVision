@echo off
REM UTF-8 编码编译脚本
REM 用途：解决Windows控制台中文乱码问题

echo 正在设置控制台编码为 UTF-8...
chcp 65001

echo.
echo 开始编译...
echo.

REM 执行编译
dotnet build %*

echo.
echo 编译完成
