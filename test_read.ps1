$filePath = "d:\MyWork\SunEyeVision_Dev-tool\src\Plugin.SDK\UI\Controls\BindableParameter.cs"
$content = Get-Content -Path $filePath -Raw -Encoding UTF8

# 读取原始文件并查看前1000个字符
Write-Host "File content preview (first 1000 chars):"
$content.Substring(0, [Math]::Min(1000, $content.Length))
