$ErrorActionPreference = 'Stop'

$filePath = 'd:/MyWork/SunEyeVision_Dev/src/Plugin.SDK/UI/Controls/Region/Views/RegionSubscribePanel.xaml.cs'
Write-Host "读取文件: $filePath"

$content = Get-Content -Path $filePath -Raw -Encoding UTF8

# 1. 添加 using System.Collections.Specialized;
$content = $content -replace '(^using System\.Linq;)', 'using System.Collections.Specialized;`$&'

# 2. 添加 _currentAvailableDataSources 字段
$oldField = @'
        // 存储所有 ParamBinding 控件引用
        private ParamBinding[]? _paramBindings;
'@
$newField = @'
        // 存储所有 ParamBinding 控件引用
        private ParamBinding[]? _paramBindings;
        
        // 存储当前监听的 AvailableDataSources 集合
        private System.Collections.ObjectModel.ObservableCollection<AvailableDataSource>? _currentAvailableDataSources;
'@
$content = $content -replace [regex]::Escape($oldField), $newField

# 3. 在构造函数中添加 Unloaded += OnUnloaded;
$oldCtorBody = @'
            InitializeComponent();

            // 在 Loaded 时获取并传递 AvailableDataSources
            Loaded += OnLoaded;
'@
$newCtorBody = @'
            InitializeComponent();

            // 在 Loaded 时获取并传递 AvailableDataSources
            Loaded += OnLoaded;
            
            // 在 Unloaded 时清理资源
            Unloaded += OnUnloaded;
'@
$content = $content -replace [regex]::Escape($oldCtorBody), $newCtorBody

# 4. 替换 OnLoaded 方法
$oldOnLoaded = @'
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 获取所有 ParamBinding 控件
            _paramBindings = FindVisualChildren<ParamBinding>(this).ToArray();

            // 查找父级 ToolDebugControlBase 并获取 AvailableDataSources
            var toolDebugControl = FindVisualParent<ToolDebugControlBase>(this);
            if (toolDebugControl != null)
            {
                var availableDataSources = toolDebugControl.AvailableDataSources;

                // 直接设置所有 ParamBinding 的 AvailableDataSources
                if (_paramBindings != null)
                {
                    foreach (var paramBinding in _paramBindings)
                    {
                        paramBinding.AvailableDataSources = availableDataSources;
                    }
                }
            }
        }
'@
$newOnLoaded = @'
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 获取所有 ParamBinding 控件
            _paramBindings = FindVisualChildren<ParamBinding>(this).ToArray();

            // 查找父级 ToolDebugControlBase 并获取 AvailableDataSources
            var toolDebugControl = FindVisualParent<ToolDebugControlBase>(this);
            if (toolDebugControl != null)
            {
                // 取消之前的监听（如果有）
                if (_currentAvailableDataSources != null)
                {
                    _currentAvailableDataSources.CollectionChanged -= OnAvailableDataSourcesChanged;
                }
                
                // 获取新的 AvailableDataSources
                _currentAvailableDataSources = toolDebugControl.AvailableDataSources;
                
                // 监听集合变化
                if (_currentAvailableDataSources != null)
                {
                    _currentAvailableDataSources.CollectionChanged += OnAvailableDataSourcesChanged;
                    
                    // 立即更新一次
                    UpdateParamBindings();
                }
            }
        }
'@
$content = $content -replace [regex]::Escape($oldOnLoaded), $newOnLoaded

# 5. 在 OnLoaded 方法后添加 OnUnloaded、OnAvailableDataSourcesChanged 和 UpdateParamBindings 方法
$insertPoint = @'
        }
'@
$methodsToInsert = @'
        
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // 取消监听
            if (_currentAvailableDataSources != null)
            {
                _currentAvailableDataSources.CollectionChanged -= OnAvailableDataSourcesChanged;
                _currentAvailableDataSources = null;
            }
        }
        
        /// <summary>
        /// AvailableDataSources 集合变化时触发
        /// </summary>
        private void OnAvailableDataSourcesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // 集合发生变化时，更新所有 ParamBinding
            UpdateParamBindings();
        }
        
        /// <summary>
        /// 更新所有 ParamBinding 的 AvailableDataSources
        /// </summary>
        private void UpdateParamBindings()
        {
            if (_paramBindings != null && _currentAvailableDataSources != null)
            {
                foreach (var paramBinding in _paramBindings)
                {
                    paramBinding.AvailableDataSources = _currentAvailableDataSources;
                }
            }
        }
'@

# 查找 OnLoaded 方法的结束位置并插入新方法
$pattern = '(?s)(private void OnLoaded\(object sender, RoutedEventArgs e\).*?        \})'
$match = [regex]::Match($content, $pattern)
if ($match.Success) {
    $afterOnLoaded = $match.Index + $match.Length
    $content = $content.Substring(0, $afterOnLoaded) + $methodsToInsert + $content.Substring($afterOnLoaded)
    Write-Host "成功找到 OnLoaded 方法并插入新方法"
}
else {
    Write-Error "未找到 OnLoaded 方法"
    exit 1
}

# 写回文件
Set-Content -Path $filePath -Value $content -Encoding UTF8 -NoNewline
Write-Host "文件修改成功: $filePath"

# 删除临时脚本
Remove-Item -Path $PSCommandPath -Force
Write-Host "临时脚本已删除"
