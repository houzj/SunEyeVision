@echo off
chcp 65001 >nul
echo ========================================
echo   功能实现方案 Word 转换向导
echo ========================================
echo.
echo 检测到系统未安装 Python 环境
echo.
echo 请选择转换方式:
echo.
echo [1] 在线转换（推荐，无需安装）
echo [2] 安装 Python 后转换
echo [3] 取消
echo.
set /p choice=请输入选项 (1-3):

if "%choice%"=="1" goto ONLINE
if "%choice%"=="2" goto INSTALL_PYTHON
if "%choice%"=="3" goto END

:ONLINE
echo.
echo ========================================
echo   使用在线工具转换
echo ========================================
echo.
echo 正在打开浏览器...
echo.
echo 请按以下步骤操作:
echo.
echo 1. 在打开的网页中点击 "Choose File" 或 "选择文件"
echo 2. 选择文件: docs\FEATURE_IMPLEMENTATION_PLAN.md
echo 3. 点击 "Convert" 或 "转换"
echo 4. 下载生成的 Word 文档
echo.
echo 推荐转换网站:
echo   - https://www.markdowntoword.com/
echo   - https://wordtopdf.com/markdown-to-word
echo   - https://cloudconvert.com/md-to-docx
echo.
echo 正在打开 https://www.markdowntoword.com/ ...
timeout /t 3 >nul
start https://www.markdowntoword.com/
goto END

:INSTALL_PYTHON
echo.
echo ========================================
echo   安装 Python
echo ========================================
echo.
echo 请按照以下步骤安装 Python:
echo.
echo 1. 访问: https://www.python.org/downloads/
echo 2. 下载并安装 Python 3.x
echo 3. 重要: 安装时务必勾选 "Add python.exe to PATH"
echo 4. 安装完成后，重新运行此脚本
echo.
pause
goto END

:END
echo.
echo ========================================
echo   操作完成
echo ========================================
echo.
pause
