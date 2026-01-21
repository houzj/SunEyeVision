# Sun Eye Vision 重命名脚本
# 将 VisionMaster 重命名为 SunEyeVision

Write-Host "开始重命名项目为 Sun Eye Vision..." -ForegroundColor Cyan

# 定义映射关系
$mappings = @{
    "VisionMaster" = "SunEyeVision"
    "VisionMaster - 机器视觉平台" = "Sun Eye Vision - 机器视觉平台"
    "VisionMaster - 文档中心" = "Sun Eye Vision - 文档中心"
    "VisionMaster 机器视觉平台" = "Sun Eye Vision 机器视觉平台"
    "VisionMaster 开发计划" = "Sun Eye Vision 开发计划"
    "VisionMaster 机器视觉软件" = "Sun Eye Vision 机器视觉软件"
    "VisionMaster 框架" = "Sun Eye Vision 框架"
    "VisionMaster Python Service" = "Sun Eye Vision Python Service"
    "关于 VisionMaster" = "关于 Sun Eye Vision"
    "VisionMaster Team" = "Sun Eye Vision Team"
}

# 步骤1: 更新所有 .cs 文件的内容
Write-Host "`n步骤1: 更新 .cs 文件中的命名空间和 using 语句..." -ForegroundColor Yellow
$csFiles = Get-ChildItem -Path . -Filter *.cs -Recurse -Exclude obj,bin,.vs
foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $modified = $false

    foreach ($key in $mappings.Keys) {
        if ($content -match [regex]::Escape($key)) {
            $content = $content -replace [regex]::Escape($key), $mappings[$key]
            $modified = $true
        }
    }

    if ($modified) {
        Set-Content $file.FullName $content -Encoding UTF8 -NoNewline
        Write-Host "  更新: $($file.Name)" -ForegroundColor Green
    }
}

# 步骤2: 更新所有 .xaml 文件的内容
Write-Host "`n步骤2: 更新 .xaml 文件..." -ForegroundColor Yellow
$xamlFiles = Get-ChildItem -Path . -Filter *.xaml -Recurse -Exclude obj,bin,.vs
foreach ($file in $xamlFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $modified = $false

    foreach ($key in $mappings.Keys) {
        if ($content -match [regex]::Escape($key)) {
            $content = $content -replace [regex]::Escape($key), $mappings[$key]
            $modified = $true
        }
    }

    if ($modified) {
        Set-Content $file.FullName $content -Encoding UTF8 -NoNewline
        Write-Host "  更新: $($file.Name)" -ForegroundColor Green
    }
}

# 步骤3: 更新所有 .csproj 文件
Write-Host "`n步骤3: 更新 .csproj 文件..." -ForegroundColor Yellow
$csprojFiles = Get-ChildItem -Path . -Filter *.csproj -Recurse -Exclude obj,bin,.vs
foreach ($file in $csprojFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $modified = $false

    foreach ($key in $mappings.Keys) {
        if ($content -match [regex]::Escape($key)) {
            $content = $content -replace [regex]::Escape($key), $mappings[$key]
            $modified = $true
        }
    }

    if ($modified) {
        Set-Content $file.FullName $content -Encoding UTF8 -NoNewline
        Write-Host "  更新: $($file.Name)" -ForegroundColor Green
    }
}

# 步骤4: 更新 .sln 文件
Write-Host "`n步骤4: 更新解决方案文件..." -ForegroundColor Yellow
$slnFiles = Get-ChildItem -Path . -Filter *.sln -Exclude obj,bin,.vs
foreach ($file in $slnFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $modified = $false

    foreach ($key in $mappings.Keys) {
        if ($content -match [regex]::Escape($key)) {
            $content = $content -replace [regex]::Escape($key), $mappings[$key]
            $modified = $true
        }
    }

    if ($modified) {
        Set-Content $file.FullName $content -Encoding UTF8 -NoNewline
        Write-Host "  更新: $($file.Name)" -ForegroundColor Green
    }
}

# 步骤5: 更新 .md 文件
Write-Host "`n步骤5: 更新文档文件..." -ForegroundColor Yellow
$mdFiles = Get-ChildItem -Path . -Filter *.md -Recurse -Exclude obj,bin,.vs
foreach ($file in $mdFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $modified = $false

    foreach ($key in $mappings.Keys) {
        if ($content -match [regex]::Escape($key)) {
            $content = $content -replace [regex]::Escape($key), $mappings[$key]
            $modified = $true
        }
    }

    if ($modified) {
        Set-Content $file.FullName $content -Encoding UTF8 -NoNewline
        Write-Host "  更新: $($file.Name)" -ForegroundColor Green
    }
}

# 步骤6: 更新 Python 文件
Write-Host "`n步骤6: 更新 Python 文件..." -ForegroundColor Yellow
$pyFiles = Get-ChildItem -Path . -Filter *.py -Recurse -Exclude obj,bin,.vs
foreach ($file in $pyFiles) {
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    $modified = $false

    foreach ($key in $mappings.Keys) {
        if ($content -match [regex]::Escape($key)) {
            $content = $content -replace [regex]::Escape($key), $mappings[$key]
            $modified = $true
        }
    }

    if ($modified) {
        Set-Content $file.FullName $content -Encoding UTF8 -NoNewline
        Write-Host "  更新: $($file.Name)" -ForegroundColor Green
    }
}

# 步骤7: 重命名文件夹 (需要最后执行)
Write-Host "`n步骤7: 重命名文件夹..." -ForegroundColor Yellow
$folderMappings = @{
    "VisionMaster.Algorithms" = "SunEyeVision.Algorithms"
    "VisionMaster.Core" = "SunEyeVision.Core"
    "VisionMaster.Demo" = "SunEyeVision.Demo"
    "VisionMaster.DeviceDriver" = "SunEyeVision.DeviceDriver"
    "VisionMaster.PluginSystem" = "SunEyeVision.PluginSystem"
    "VisionMaster.Test" = "SunEyeVision.Test"
    "VisionMaster.UI" = "SunEyeVision.UI"
    "VisionMaster.Workflow" = "SunEyeVision.Workflow"
}

foreach ($oldName in $folderMappings.Keys) {
    $newName = $folderMappings[$oldName]
    if (Test-Path $oldName) {
        Rename-Item -Path $oldName -NewName $newName
        Write-Host "  重命名: $oldName -> $newName" -ForegroundColor Green
    }
}

# 步骤8: 重命名文件
Write-Host "`n步骤8: 重命名文件..." -ForegroundColor Yellow
$fileMappings = @{
    "VisionMaster.sln" = "SunEyeVision.sln"
}

foreach ($oldName in $fileMappings.Keys) {
    $newName = $fileMappings[$oldName]
    if (Test-Path $oldName) {
        Rename-Item -Path $oldName -NewName $newName
        Write-Host "  重命名: $oldName -> $newName" -ForegroundColor Green
    }
}

# 步骤9: 重命名 .csproj 文件
Write-Host "`n步骤9: 重命名 .csproj 文件..." -ForegroundColor Yellow
$csprojMappings = @{
    "SunEyeVision.Algorithms/VisionMaster.Algorithms.csproj" = "SunEyeVision.Algorithms/SunEyeVision.Algorithms.csproj"
    "SunEyeVision.Core/VisionMaster.Core.csproj" = "SunEyeVision.Core/SunEyeVision.Core.csproj"
    "SunEyeVision.Demo/VisionMaster.Demo.csproj" = "SunEyeVision.Demo/SunEyeVision.Demo.csproj"
    "SunEyeVision.DeviceDriver/VisionMaster.DeviceDriver.csproj" = "SunEyeVision.DeviceDriver/SunEyeVision.DeviceDriver.csproj"
    "SunEyeVision.PluginSystem/VisionMaster.PluginSystem.csproj" = "SunEyeVision.PluginSystem/SunEyeVision.PluginSystem.csproj"
    "SunEyeVision.Test/VisionMaster.Test.csproj" = "SunEyeVision.Test/SunEyeVision.Test.csproj"
    "SunEyeVision.UI/VisionMaster.UI.csproj" = "SunEyeVision.UI/SunEyeVision.UI.csproj"
    "SunEyeVision.Workflow/VisionMaster.Workflow.csproj" = "SunEyeVision.Workflow/SunEyeVision.Workflow.csproj"
}

foreach ($oldPath in $csprojMappings.Keys) {
    $newPath = $csprojMappings[$oldPath]
    if (Test-Path $oldPath) {
        Rename-Item -Path $oldPath -NewName $newPath
        Write-Host "  重命名: $oldPath -> $newPath" -ForegroundColor Green
    }
}

Write-Host "`n✅ 重命名完成!" -ForegroundColor Green
Write-Host "请清理并重新构建项目。" -ForegroundColor Cyan
