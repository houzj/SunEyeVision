# Clean old VisionMaster references and generated files

$projectRoot = "C:\Users\houzhongjie\CodeBuddy\20260119143126"

Write-Host "Starting to clean old VisionMaster references..." -ForegroundColor Green

# Clean old deps.json files in bin directories
Write-Host "`n1. Cleaning old deps.json files in bin directories..." -ForegroundColor Yellow
Get-ChildItem -Path $projectRoot -Recurse -Filter "VisionMaster.*.deps.json" -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "  Deleting: $($_.FullName)" -ForegroundColor Cyan
    Remove-Item $_.FullName -Force
}

# Clean old generated files in obj directories
Write-Host "`n2. Cleaning old generated files in obj directories..." -ForegroundColor Yellow
Get-ChildItem -Path $projectRoot -Recurse -Directory -Filter "VisionMaster.*" -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "  Deleting directory: $($_.FullName)" -ForegroundColor Cyan
    Remove-Item $_.FullName -Recurse -Force
}

# Clean all obj directories
Write-Host "`n3. Cleaning all obj directories..." -ForegroundColor Yellow
Get-ChildItem -Path $projectRoot -Recurse -Directory -Filter "obj" -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "  Deleting: $($_.FullName)" -ForegroundColor Cyan
    Remove-Item $_.FullName -Recurse -Force
}

Write-Host "`n4. Cleaning all bin directories..." -ForegroundColor Yellow
Get-ChildItem -Path $projectRoot -Recurse -Directory -Filter "bin" -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "  Deleting: $($_.FullName)" -ForegroundColor Cyan
    Remove-Item $_.FullName -Recurse -Force
}

Write-Host "`nClean up completed!" -ForegroundColor Green
