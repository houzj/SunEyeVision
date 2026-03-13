# 编译编码解决方案说明

## 问题
Windows控制台默认使用GBK编码，而.NET编译器输出使用UTF-8，导致中文显示为乱码。

## 解决方案

### 方法1：使用批处理脚本（推荐）
使用 `compile.bat` 脚本，它会自动设置控制台编码：
```batch
compile.bat src/UI/SunEyeVision.UI.csproj --configuration Debug
```

### 方法2：在PowerShell中手动设置编码
```powershell
# 设置PowerShell输出编码为UTF-8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# 然后执行编译
dotnet build src/UI/SunEyeVision.UI.csproj --configuration Debug
```

### 方法3：使用CMD设置代码页
```batch
REM 在CMD中执行
chcp 65001
dotnet build src/UI/SunEyeVision.UI.csproj --configuration Debug
```

### 方法4：设置PowerShell配置文件
在PowerShell配置文件 `C:\Users\你的用户名\Documents\WindowsPowerShell\Microsoft.PowerShell_profile.ps1` 中添加：
```powershell
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8
```

## 验证设置
检查当前代码页：
```batch
chcp
```

如果是 `Active code page: 65001`，则表示已设置为UTF-8。

## 注意事项
- 代码页65001是UTF-8
- 代码页936是简体中文GBK（Windows中文默认）
- 修改编码只影响当前会话
- 如果输出到文件，文件本身使用UTF-8编码，不会乱码
