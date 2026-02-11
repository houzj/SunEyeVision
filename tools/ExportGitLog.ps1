<#
.SYNOPSIS
    导出Git日志到文件

.DESCRIPTION
    此脚本用于导出Git仓库的提交日志到文件，支持多种格式和筛选选项

.PARAMETER OutputFile
    指定输出文件路径，默认是项目根目录下的git_log.txt

.PARAMETER Format
    指定输出格式：
    - simple: 简单格式（仅哈希和提交信息）
    - full: 完整格式（包含作者、日期、提交信息等）
    - csv: CSV格式
    - markdown: Markdown格式
    默认是simple

.PARAMETER Branch
    指定分支，默认是当前分支

.PARAMETER Since
    仅显示指定时间之后的提交，格式：YYYY-MM-DD

.PARAMETER Until
    仅显示指定时间之前的提交，格式：YYYY-MM-DD

.PARAMETER MaxCount
    最多显示的提交数量

.EXAMPLE
    .\ExportGitLog.ps1 -Format full -OutputFile git_history.txt

.EXAMPLE
    .\ExportGitLog.ps1 -Format markdown -Since "2023-01-01" -Until "2023-12-31"

.EXAMPLE
    .\ExportGitLog.ps1 -Branch main -MaxCount 50
#>

param (
    [string]$OutputFile = "$PSScriptRoot\..\git_log.txt",
    [ValidateSet("simple", "full", "csv", "markdown")]
    [string]$Format = "simple",
    [string]$Branch = "",
    [string]$Since = "",
    [string]$Until = "",
    [int]$MaxCount = 0
)

# 确保在git仓库目录下
$originalPath = Get-Location
Set-Location "$PSScriptRoot\.."

# 构建git log命令
$gitArgs = @("log")

if ($Branch) {
    $gitArgs += $Branch
}

if ($Since) {
    $gitArgs += "--since=$Since"
}

if ($Until) {
    $gitArgs += "--until=$Until"
}

if ($MaxCount -gt 0) {
    $gitArgs += "--max-count=$MaxCount"
}

# 根据格式设置不同的输出格式
switch ($Format) {
    "simple" {
        $gitArgs += "--oneline"
    }
    "full" {
        $gitArgs += @("--pretty=format:%H%nAuthor: %an <%ae>%nDate: %ad%nSubject: %s%n%b%n-------------------------------------------------------------------------------")
    }
    "csv" {
        # 先输出CSV表头
        $csvHeader = "CommitHash,Author,Email,Date,Subject"
        $csvHeader | Out-File -FilePath $OutputFile -Encoding UTF8
        
        # CSV格式的git log命令
        $gitArgs += @("--pretty=format:%H,%an,%ae,%ad,%s", "--date=iso")
        $gitLogOutput = & git $gitArgs
        
        # 处理CSV输出，转义逗号和引号
        $processedOutput = $gitLogOutput | ForEach-Object {
            $fields = $_ -split ",", 5
            for ($i = 0; $i -lt $fields.Length; $i++) {
                if ($fields[$i] -match '[,"\n]') {
                    $fields[$i] = '"' + $fields[$i].Replace('"', '""') + '"'
                }
            }
            $fields -join ","
        }
        
        $processedOutput | Out-File -FilePath $OutputFile -Encoding UTF8 -Append
        Write-Host "Git日志已导出到CSV文件: $OutputFile"
        Set-Location $originalPath
        return
    }
    "markdown" {
        # Markdown格式的git log命令
        $gitArgs += @("--pretty=format:| %h | %an | %ad | %s |", "--date=short")
        
        # 输出Markdown表头
        $mdHeader = "| Commit | Author | Date | Subject |
|--------|--------|------|---------|
"
        $mdHeader | Out-File -FilePath $OutputFile -Encoding UTF8
    }
}

# 执行git log命令并保存输出
if ($Format -ne "csv") {
    & git $gitArgs | Out-File -FilePath $OutputFile -Encoding UTF8
}

Write-Host "Git日志已导出到: $OutputFile"
Write-Host "格式: $Format"
if ($Branch) {
    Write-Host "分支: $Branch"
}
if ($Since) {
    Write-Host "开始时间: $Since"
}
if ($Until) {
    Write-Host "结束时间: $Until"
}
if ($MaxCount -gt 0) {
    Write-Host "提交数量: $MaxCount"
}

# 恢复原始路径
Set-Location $originalPath