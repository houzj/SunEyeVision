# ============================================================
# Color Consistency Check Script
# ============================================================
# Purpose: Check hardcoded colors in project, ensure using ColorConstants
# Rule: rule-011 Temp file auto cleanup
# ============================================================

param(
    [string]$SolutionPath = "d:\MyWork\SunEyeVision_Dev",
    [switch]$FailOnError = $false
)

# Set temp log file (following rule-011)
$tempLogFile = Join-Path $env:TEMP "check_color_consistency_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"

try {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Color Consistency Check" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Project Path: $SolutionPath" -ForegroundColor Gray
    Write-Host "Temp Log: $tempLogFile" -ForegroundColor Gray
    Write-Host ""

    # Initialize issue count
    $csIssues = 0
    $xamlIssues = 0

    # ============================================================
    # 1. Check C# files for hardcoded colors
    # ============================================================
    Write-Host "[1/2] Checking C# files..." -ForegroundColor Yellow

    $csFiles = Get-ChildItem -Path "$SolutionPath\src" -Filter "*.cs" -Recurse -ErrorAction SilentlyContinue | 
               Where-Object { $_.FullName -notmatch "\\obj\\|\\bin\\" }

    # C# hardcoded color patterns
    $csPatterns = @(
        'Color\.FromRgb\([0-9]+,\s*[0-9]+,\s*[0-9]+\)',
        'Color\.FromArgb\([0-9]+,\s*[0-9]+,\s*[0-9]+,\s*[0-9]+\)',
        'new SolidColorBrush\(Color\.FromRgb',
        'new SolidColorBrush\(Colors\.'
    )

    foreach ($file in $csFiles) {
        foreach ($pattern in $csPatterns) {
            $matches = Select-String -Path $file.FullName -Pattern $pattern -CaseSensitive -ErrorAction SilentlyContinue
            
            if ($matches) {
                foreach ($match in $matches) {
                    # Exclude ColorConstants.cs itself
                    if ($file.Name -eq "ColorConstants.cs") {
                        continue
                    }

                    $csIssues++
                    $message = "  [CS] $($file.Name):$($match.LineNumber)"
                    Write-Host $message -ForegroundColor Red
                    Write-Host "       $($match.Line.Trim())" -ForegroundColor Gray
                    Add-Content -Path $tempLogFile -Value $message
                }
            }
        }
    }

    Write-Host ""

    # ============================================================
    # 2. Check XAML files for hardcoded colors
    # ============================================================
    Write-Host "[2/2] Checking XAML files..." -ForegroundColor Yellow

    $xamlFiles = Get-ChildItem -Path "$SolutionPath\src" -Filter "*.xaml" -Recurse -ErrorAction SilentlyContinue | 
                 Where-Object { $_.FullName -notmatch "\\obj\\|\\bin\\" }

    # XAML hardcoded color patterns
    $xamlPatterns = @(
        'Color="#[0-9A-Fa-f]{6}"',
        'Color="#[0-9A-Fa-f]{8}"',
        'Background="#[0-9A-Fa-f]{6}"',
        'Foreground="#[0-9A-Fa-f]{6}"',
        'BorderBrush="#[0-9A-Fa-f]{6}"'
    )

    foreach ($file in $xamlFiles) {
        # Exclude Colors.xaml itself
        if ($file.Name -eq "Colors.xaml") {
            continue
        }

        foreach ($pattern in $xamlPatterns) {
            $matches = Select-String -Path $file.FullName -Pattern $pattern -CaseSensitive -ErrorAction SilentlyContinue
            
            if ($matches) {
                foreach ($match in $matches) {
                    $xamlIssues++
                    $message = "  [XAML] $($file.Name):$($match.LineNumber)"
                    Write-Host $message -ForegroundColor Red
                    Write-Host "         $($match.Line.Trim())" -ForegroundColor Gray
                    Add-Content -Path $tempLogFile -Value $message
                }
            }
        }
    }

    Write-Host ""

    # ============================================================
    # 3. Output summary
    # ============================================================
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Summary" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "C# hardcoded colors    : $csIssues" -ForegroundColor $(if ($csIssues -gt 0) { "Red" } else { "Green" })
    Write-Host "XAML hardcoded colors  : $xamlIssues" -ForegroundColor $(if ($xamlIssues -gt 0) { "Red" } else { "Green" })
    Write-Host "Total issues           : $($csIssues + $xamlIssues)" -ForegroundColor $(if ($($csIssues + $xamlIssues) -gt 0) { "Red" } else { "Green" })
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""

    # ============================================================
    # 4. Provide fix suggestions
    # ============================================================
    if ($csIssues -gt 0 -or $xamlIssues -gt 0) {
        Write-Host "Fix Suggestions:" -ForegroundColor Yellow
        Write-Host "----------------------------------------" -ForegroundColor Gray
        Write-Host "1. In C# code, use ColorConstants:" -ForegroundColor White
        Write-Host "   BAD:  new SolidColorBrush(Color.FromRgb(0, 102, 204))" -ForegroundColor Red
        Write-Host "   GOOD: ColorConstants.PrimaryBrush" -ForegroundColor Green
        Write-Host ""
        Write-Host "2. In XAML, use StaticResource:" -ForegroundColor White
        Write-Host "   BAD:  Background=""#0066CC""" -ForegroundColor Red
        Write-Host "   GOOD: Background=""{StaticResource PrimaryBrush}""" -ForegroundColor Green
        Write-Host ""
        Write-Host "3. Or use x:Static:" -ForegroundColor White
        Write-Host "   GOOD: Color=""{x:Static constants:ColorConstants.Primary}""" -ForegroundColor Green
        Write-Host "----------------------------------------" -ForegroundColor Gray
        Write-Host ""
        Write-Host "Log saved to: $tempLogFile" -ForegroundColor Cyan
        Write-Host ""

        if ($FailOnError) {
            Write-Host "FAILED: Found $($csIssues + $xamlIssues) hardcoded colors!" -ForegroundColor Red
            exit 1
        } else {
            Write-Host "WARNING: Found $($csIssues + $xamlIssues) hardcoded colors!" -ForegroundColor Yellow
            exit 0
        }
    } else {
        Write-Host "SUCCESS: All colors use ColorConstants!" -ForegroundColor Green
        exit 0
    }

} catch {
    Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Add-Content -Path $tempLogFile -Value "ERROR: $($_.Exception.Message)"
    exit 1
} finally {
    # Follow rule-011: Cleanup temp file (optional, keep log for review)
    # Uncomment below line to cleanup
    # if (Test-Path $tempLogFile) { Remove-Item $tempLogFile -Force -ErrorAction SilentlyContinue }
}
