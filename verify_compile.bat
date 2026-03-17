@echo off
echo 开始编译解决方案...
cd /d d:\MyWork\SunEyeVision\SunEyeVision

echo 正在执行编译...
msbuild SunEyeVision.New.sln /t:Build /p:Configuration=Release /v:minimal /noconsolelogger /flp:logfile=build_verification.log

echo.
echo 编译完成!
echo.
echo 检查编译结果...
findstr /C:"error" /C:"warning" /C:"Build succeeded" /C:"Build FAILED" build_verification.log
