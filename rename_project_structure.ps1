# Rename VisionMaster projects to SunEyeVision
# Includes renaming folders, project files, and solution file references

$workspace = "c:\Users\houzhongjie\CodeBuddy\20260119143126"
Set-Location $workspace

Write-Host "Starting project structure rename..." -ForegroundColor Green

# Define projects to rename
$projects = @{
    "VisionMaster.Core" = "SunEyeVision.Core"
    "VisionMaster.UI" = "SunEyeVision.UI"
    "VisionMaster.Workflow" = "SunEyeVision.Workflow"
    "VisionMaster.Algorithms" = "SunEyeVision.Algorithms"
    "VisionMaster.DeviceDriver" = "SunEyeVision.DeviceDriver"
    "VisionMaster.PluginSystem" = "SunEyeVision.PluginSystem"
    "VisionMaster.Demo" = "SunEyeVision.Demo"
    "VisionMaster.Test" = "SunEyeVision.Test"
}

# Step 1: Rename project files
Write-Host "`nStep 1: Renaming project files..." -ForegroundColor Yellow
foreach ($oldName in $projects.Keys) {
    $newName = $projects[$oldName]
    $oldProjFile = "$oldName\$oldName.csproj"
    $newProjFile = "$oldName\$newName.csproj"

    if (Test-Path $oldProjFile) {
        Rename-Item -Path $oldProjFile -NewName $newProjFile -Force
        Write-Host "  Renamed: $oldProjFile -> $newProjFile" -ForegroundColor Cyan
    }
}

# Step 2: Update project file references
Write-Host "`nStep 2: Updating project file references..." -ForegroundColor Yellow
foreach ($oldName in $projects.Keys) {
    $newName = $projects[$oldName]
    $folderName = $oldName
    $newProjFile = "$folderName\$newName.csproj"

    if (Test-Path $newProjFile) {
        $content = Get-Content -Path $newProjFile -Raw -Encoding UTF8

        # Update ProjectReference paths
        foreach ($refOld in $projects.Keys) {
            $refNew = $projects[$refOld]
            $oldPattern = "Reference Include=`"\.\.\\$refOld\\$refOld.csproj`""
            $newPattern = "Reference Include=`"\.\.\\$refOld\\$refNew.csproj`""
            $content = $content -replace [regex]::Escape($oldPattern), $newPattern
        }

        Set-Content -Path $newProjFile -Value $content -Encoding UTF8
        Write-Host "  Updated: $newProjFile references" -ForegroundColor Cyan
    }
}

# Step 3: Rename folders
Write-Host "`nStep 3: Renaming folders..." -ForegroundColor Yellow
foreach ($oldName in $projects.Keys) {
    $newName = $projects[$oldName]

    if (Test-Path $oldName) {
        Rename-Item -Path $oldName -NewName $newName -Force
        Write-Host "  Renamed folder: $oldName -> $newName" -ForegroundColor Cyan
    }
}

# Step 4: Update solution file
Write-Host "`nStep 4: Updating solution file..." -ForegroundColor Yellow
$slnLines = Get-Content -Path "SunEyeVision.sln" -Encoding UTF8
$newSlnLines = @()

foreach ($line in $slnLines) {
    $newLine = $line
    foreach ($oldName in $projects.Keys) {
        $newName = $projects[$oldName]

        # Update project path
        $newLine = $newLine -replace "$oldName\\$oldName.csproj", "$newName\\$newName.csproj"
        # Update project name
        $newLine = $newLine -replace "= `"$oldName`",", "= `"$newName`","
    }
    $newSlnLines += $newLine
}

Set-Content -Path "SunEyeVision.sln" -Value ($newSlnLines -join "`r`n") -Encoding UTF8
Write-Host "  Updated: SunEyeVision.sln" -ForegroundColor Cyan

Write-Host "`nProject structure rename completed!" -ForegroundColor Green
Write-Host "`nNew project structure:" -ForegroundColor Yellow
foreach ($newName in $projects.Values) {
    Write-Host "  - $newName" -ForegroundColor White
}

# Step 5: Try to build
Write-Host "`nAttempting to build project..." -ForegroundColor Yellow
Start-Sleep -Seconds 2
dotnet build "SunEyeVision.sln" 2>&1 | Select-String -Pattern "success|failed|error|warning" | Select-Object -Last 5

Write-Host "`nNote: If build fails, please reload solution in Visual Studio." -ForegroundColor Yellow
