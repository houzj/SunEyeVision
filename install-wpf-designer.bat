@echo off
REM WPF Designer 扩展快速安装批处理文件
REM 双击运行此文件即可自动安装

echo ========================================
echo   WPF Designer 扩展安装脚本
echo ========================================
echo.

REM 检查 VSCode 是否已安装
where code >nul 2>nul
if %errorlevel% neq 0 (
    echo [错误] 未检测到 VSCode，请先安装 Visual Studio Code
    echo 下载地址: https://code.visualstudio.com/
    pause
    exit /b 1
)

echo [成功] 检测到 VSCode
echo.

REM 安装扩展
echo [安装] 正在安装 WPF Designer 扩展...
echo 扩展ID: jingliancui.vscode-wpf-designer
echo.

code --install-extension jingliancui.vscode-wpf-designer --force

if %errorlevel% equ 0 (
    echo.
    echo ========================================
    echo   安装成功！
    echo ========================================
    echo.
    echo 后续步骤：
    echo 1. 重启 VSCode (关闭后重新打开)
    echo 2. 打开 MainWindow.xaml 文件
    echo 3. 右键点击编辑器 ^> 选择 'Open Preview'
    echo 4. 或按快捷键 Ctrl+Shift+V 查看预览
    echo.
    echo 提示：如果预览不显示，请尝试：
    echo    - 按 Ctrl+Shift+P，输入 'WPF' 查看命令
    echo    - 或在右键菜单中查找预览选项
    echo.
) else (
    echo.
    echo ========================================
    echo   安装失败
    echo ========================================
    echo.
    echo 手动安装方法：
    echo 1. 在 VSCode 中按 Ctrl+Shift+X 打开扩展面板
    echo 2. 搜索 'WPF Designer'
    echo 3. 找到由 'jingliancui' 发布的扩展
    echo 4. 点击 'Install' 按钮
    echo.
)

pause
