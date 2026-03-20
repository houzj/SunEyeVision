@echo off
cd /d "%~dp0"
echo ========================================
echo SunEyeVision 临时文件清理脚本
echo ========================================
echo.
echo 警告: 此脚本将删除以下类型的文件:
echo   - *.tmp, *.temp (临时文件)
echo   - temp_*, *_temp.* (临时文件)
echo   - *.bak, backup_* (备份文件)
echo   - old_*, *_old.* (旧版本文件)
echo   - build_*.txt, build_log.txt (编译日志)
echo   - debug_*.txt (调试输出)
echo   - test_*.txt (测试文件)
echo.
set /p confirm=确认删除？(Y/N): 
if /i not "%confirm%"=="Y" (
    echo 已取消清理。
    pause
    exit /b 0
)

echo.
echo 开始清理...
echo.

:: 清理临时文件
echo [1/8] 清理 *.tmp, *.temp...
del /q *.tmp 2>nul
del /q *.temp 2>nul
del /q *.tempfile 2>nul

:: 清理 temp_* 模式
echo [2/8] 清理 temp_* 文件...
del /q temp_*.txt 2>nul
del /q temp_*.cs 2>nul
del /q temp_*.bat 2>nul
del /q temp_*.ps1 2>nul
del /q temp_*.log 2>nul
del /q _temp.* 2>nul

:: 清理 backup_* 模式
echo [3/8] 清理备份文件...
del /q *.bak 2>nul
del /q backup_*.cs 2>nul
del /q _backup.*.cs 2>nul

:: 清理 old_* 模式
echo [4/8] 清理旧版本文件...
del /q old_*.cs 2>nul
del /q _old.*.cs 2>nul
del /q _reference*.cs 2>nul

:: 清理编译日志
echo [5/8] 清理编译日志...
del /q build_*.txt 2>nul
del /q build_output.txt 2>nul
del /q build2.txt 2>nul
del /q test_build.txt 2>nul
del /q restore_log.txt 2>nul
del /q build_log.txt 2>nul

:: 清理调试输出
echo [6/8] 清理调试输出...
del /q debug_*.txt 2>nul
del /q debug_output_*.log 2>nul

:: 清理测试文件
echo [7/8] 清理测试文件...
del /q test_*.txt 2>nul
del /q test_*.log 2>nul
del /q test_output_*.txt 2>nul

:: 清理其他临时文件
echo [8/8] 清理其他临时文件...
del /q RECIPE 2>nul
del /q *.db 2>nul

echo.
echo ========================================
echo 清理完成！
echo ========================================
echo.
echo 提示: 
echo   - 已清理的临时文件不会被提交到 Git（已在 .gitignore 中配置）
echo   - 建议定期运行此脚本（例如每周一次）
echo   - 如有疑问，请先备份重要文件
echo.
pause
