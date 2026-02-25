# 扫描编码问题并生成报告
$reportFile = "encoding_issues_report.txt"
$problemFiles = @()

Get-ChildItem -Path src -Include *.cs -Recurse | Where-Object { $_.FullName -notmatch 'obj|bin' } | ForEach-Object {
    $content = [System.IO.File]::ReadAllText($_.FullName, [System.Text.Encoding]::UTF8)
    if ($content -match '\ufffd') {
        $lines = $content -split "`n"
        $issues = @()
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $matches = [regex]::Matches($lines[$i], '\ufffd+')
            if ($matches.Count -gt 0) {
                $snippet = $lines[$i].Trim()
                if ($snippet.Length -gt 100) { $snippet = $snippet.Substring(0, 100) + "..." }
                $issues += [PSCustomObject]@{
                    Line = $i + 1
                    Count = $matches.Count
                    Snippet = $snippet
                }
            }
        }
        if ($issues.Count -gt 0) {
            $problemFiles += [PSCustomObject]@{
                File = $_.FullName
                RelativePath = $_.FullName.Replace("$PWD\", "")
                IssueCount = $issues.Count
                Issues = $issues
            }
        }
    }
}

# 生成报告
$summary = @"
===============================================
        SunEyeVision 编码问题扫描报告
===============================================
生成时间: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
扫描目录: src
问题文件总数: $($problemFiles.Count)
总问题行数: $(($problemFiles | ForEach-Object { $_.Issues.Count } | Measure-Object -Sum).Sum)

"@

# 按问题数量排序
$sortedFiles = $problemFiles | Sort-Object IssueCount -Descending

foreach ($pf in $sortedFiles) {
    $summary += "`n" + "="*80 + "`n"
    $summary += "文件: $($pf.RelativePath)`n"
    $summary += "问题行数: $($pf.IssueCount)`n"
    $summary += "-"*40 + "`n"
    foreach ($issue in $pf.Issues) {
        $summary += "  行 $($issue.Line): $($issue.Snippet)`n"
    }
}

# 添加统计信息
$summary += "`n" + "="*80 + "`n"
$summary += "统计摘要`n"
$summary += "="*80 + "`n"

# 按目录分组统计
$byDir = $problemFiles | Group-Object { $_.File.Split('\')[-2] } | Sort-Object Count -Descending
$summary += "`n按目录分布:`n"
foreach ($g in $byDir) {
    $summary += "  $($g.Name): $($g.Count) 个文件`n"
}

[System.IO.File]::WriteAllText($reportFile, $summary, [System.Text.Encoding]::UTF8)
Write-Host "报告已生成: $reportFile"
Write-Host "问题文件总数: $($problemFiles.Count)"
