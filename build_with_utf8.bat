@echo off
REM 设置控制台代码页为UTF-8
chcp 65001 >nul 2>&1

REM 执行传入的编译命令
dotnet build %*
