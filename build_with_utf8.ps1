# UTF-8 编码编译脚本
# 用途：解决Windows控制台中文乱码问题

# 设置控制台输出编码为UTF-8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

# 设置默认参数编码
$PSDefaultParameterValues['Out-File:Encoding'] = 'utf8'

# 执行传入的所有参数
& dotnet $args
