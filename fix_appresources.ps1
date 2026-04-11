$content = Get-Content "d:\MyWork\SunEyeVision_Dev-camera\src\UI\Views\Resources\AppResources.xaml" -Raw -Encoding UTF8
$newContent = $content -replace '(<ResourceDictionary.MergedDictionaries>)', '$1
        <!-- SDK Generic.xaml - 包含所有SDK控件样式 -->
        <ResourceDictionary Source="pack://application:,,,/SunEyeVision.Plugin.SDK;component/UI/Themes/Generic.xaml"/>'
Set-Content -Path "d:\MyWork\SunEyeVision_Dev-camera\src\UI\Views\Resources\AppResources.xaml" -Value $newContent -Encoding UTF8 -NoNewline
