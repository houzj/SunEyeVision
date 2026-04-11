# 修改 ThresholdToolDebugControl.xaml 中的绑定，改为统一的 AvailableDataSources

$filePath = "d:/MyWork/SunEyeVision_Dev-tool/tools/SunEyeVision.Tool.Threshold/Views/ThresholdToolDebugControl.xaml"

# 读取文件
$content = Get-Content -Path $filePath -Raw -Encoding UTF8

# 替换 1: ImageSourceSelector 的 ImageDataSources -> AvailableDataSources
$content = $content -replace 'ImageDataSources="\{Binding ImageDataSources,', 'AvailableDataSources="{Binding AvailableDataSources,'

# 替换 2-5: BindableParameter 的 NumericDataSources -> AvailableDataSources
$content = $content -replace 'AvailableDataSources="\{Binding NumericDataSources,', 'AvailableDataSources="{Binding AvailableDataSources,'

# 写入文件
[System.IO.File]::WriteAllText($filePath, $content, [System.Text.UTF8Encoding]::new($false))

Write-Host "修改完成！所有绑定已改为 AvailableDataSources" -ForegroundColor Green
