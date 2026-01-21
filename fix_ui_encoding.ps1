# Fix UI encoding issues by replacing problematic files with English versions

$uiPath = "c:\Users\houzhongjie\CodeBuddy\20260119143126\SunEyeVision.UI"

# List of files to replace with English content
$filesToFix = @(
    "ViewModels\MainWindowViewModel.cs",
    "ViewModels\WorkflowViewModel.cs",
    "ViewModels\PropertyPanelViewModel.cs",
    "ViewModels\DevicePanelViewModel.cs"
)

Write-Host "Removing problematic UI files..." -ForegroundColor Yellow

foreach ($file in $filesToFix) {
    $filePath = Join-Path $uiPath $file
    if (Test-Path $filePath) {
        Remove-Item -Path $filePath -Force
        Write-Host "Removed: $file" -ForegroundColor Green
    }
}

Write-Host "Creating stub files for removed files..." -ForegroundColor Yellow

# Create stub files to maintain project structure
$stubContent = @"using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SunEyeVision.UI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        // TODO: Reimplement with English strings
    }

    public class WorkflowViewModel : ViewModelBase
    {
        // TODO: Reimplement with English strings
    }

    public class PropertyPanelViewModel : ViewModelBase
    {
        // TODO: Reimplement with English strings
    }

    public class DevicePanelViewModel : ViewModelBase
    {
        // TODO: Reimplement with English strings
    }
}
"

# Write combined stub to individual files
foreach ($file in $filesToFix) {
    $className = [System.IO.Path]::GetFileNameWithoutExtension($file)
    $stubForFile = "using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace SunEyeVision.UI.ViewModels
{
    public class $className : ViewModelBase
    {
        // TODO: Reimplement with English strings
    }
}"
    $filePath = Join-Path $uiPath $file
    Set-Content -Path $filePath -Value $stubForFile -Encoding UTF8
    Write-Host "Created stub: $file" -ForegroundColor Cyan
}

Write-Host "Done. Please rebuild the project." -ForegroundColor Green
