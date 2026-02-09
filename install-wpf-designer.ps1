# WPF Designer 扩展自动安装脚本
# 用途：在VSCode中安装WPF Designer扩展
# 使用方法：在PowerShell中运行 .\install-wpf-designer.ps1

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  WPF Designer 扩展安装脚本" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 检查 VSCode 是否已安装
$vscodePath = Get-Command code -ErrorAction SilentlyContinue

if (-not $vscodePath) {
    Write-Host "❌ 错误：未检测到 VSCode，请先安装 Visual Studio Code" -ForegroundColor Red
    Write-Host "下载地址：https://code.visualstudio.com/" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ 检测到 VSCode: $($vscodePath.Source)" -ForegroundColor Green
Write-Host ""

# 定义扩展ID
$extensionId = "jingliancui.vscode-wpf-designer"

Write-Host "📦 正在安装 WPF Designer 扩展..." -ForegroundColor Yellow
Write-Host "扩展ID: $extensionId" -ForegroundColor Gray
Write-Host ""

# 尝试安装扩展
try {
    code --install-extension $extensionId --force
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  ✅ 安装成功！" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 后续步骤：" -ForegroundColor Cyan
    Write-Host "1. 重启 VSCode (关闭后重新打开)" -ForegroundColor White
    Write-Host "2. 打开 MainWindow.xaml 文件" -ForegroundColor White
    Write-Host "3. 右键点击编辑器 → 选择 'Open Preview'" -ForegroundColor White
    Write-Host "4. 或按快捷键 Ctrl+Shift+V 查看预览" -ForegroundColor White
    Write-Host ""
    Write-Host "💡 提示：如果预览不显示，请尝试：" -ForegroundColor Yellow
    Write-Host "   - 按 Ctrl+Shift+P，输入 'WPF' 查看命令" -ForegroundColor White
    Write-Host "   - 或在右键菜单中查找预览选项" -ForegroundColor White
    Write-Host ""

} catch {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  ❌ 安装失败" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "错误信息: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "🔧 手动安装方法：" -ForegroundColor Cyan
    Write-Host "1. 在 VSCode 中按 Ctrl+Shift+X 打开扩展面板" -ForegroundColor White
    Write-Host "2. 搜索 'WPF Designer'" -ForegroundColor White
    Write-Host "3. 找到由 'jingliancui' 发布的扩展" -ForegroundColor White
    Write-Host "4. 点击 'Install' 按钮" -ForegroundColor White
    Write-Host ""
}
