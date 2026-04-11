# 添加统一的 AvailableDataSources 属性

## 修改文件
**文件**: `src/Plugin.SDK/UI/Controls/ToolDebugControlBase.cs`

## 修改内容
在 `NumericDataSources` 属性之后（第237行），添加统一的 `AvailableDataSources` 属性：

```csharp
/// <summary>
/// 所有类型的数据源（统一的绑定源，控件内部根据参数类型自动过滤）
/// </summary>
/// <remarks>
/// 设计理念：
/// - 所有参数控件绑定到同一个 AvailableDataSources（不区分类型）
/// - 控件内部根据参数的 DataType 自动过滤和匹配
/// - 简化 XAML 绑定：统一使用 AvailableDataSources
/// </remarks>
public System.Collections.ObjectModel.ObservableCollection<AvailableDataSource> AvailableDataSources
{
    get
    {
        var allDataSources = new System.Collections.ObjectModel.ObservableCollection<AvailableDataSource>();
        
        // 合并所有分类的数据源
        foreach (var category in System.Enum.GetValues(typeof(OutputTypeCategory)))
        {
            var categorySources = _dataSourcesByCategory[(OutputTypeCategory)category];
            foreach (var ds in categorySources)
            {
                allDataSources.Add(ds);
            }
        }
        
        return allDataSources;
    }
}
```

## 修改 ThresholdToolDebugControl.xaml 绑定方式

### 原来的错误方式
```xml
<!-- 图像参数：绑定 ImageDataSources -->
<controls:ImageSourceSelector
    ImageDataSources="{Binding ImageDataSources, RelativeSource=...}"
    SelectedDataSource="{Binding Parameters.ImageSource, ...}"/>

<!-- 数值参数：绑定 NumericDataSources -->
<controls:BindableParameter
    ParameterName="Threshold"
    DataType="Int"
    AvailableDataSources="{Binding NumericDataSources, RelativeSource=...}"/>
```

### 正确的方式（统一绑定）
```xml
<!-- 图像参数：绑定 AvailableDataSources -->
<controls:ImageSourceSelector
    ImageDataSources="{Binding AvailableDataSources, RelativeSource={RelativeSource FindAncestor, AncestorType={x:Type controls:ToolDebugControlBase}}}"
    SelectedDataSource="{Binding Parameters.ImageSource, ...}"/>

<!-- 数值参数：绑定 AvailableDataSources -->
<controls:BindableParameter
    ParameterName="Threshold"
    DataType="Int"
    AvailableDataSources="{Binding AvailableDataSources, RelativeSource={RelativeSource FindAncestor, AncestorType={x:Type controls:ToolDebugControlBase}}}"/>
```

## 设计优势

1. **统一绑定接口**：所有参数控件都绑定到同一个 `AvailableDataSources`
2. **自动类型匹配**：控件内部根据 `DataType` 属性自动过滤合适的数据源
3. **简化 XAML**：不需要区分 `ImageDataSources` 和 `NumericDataSources`
4. **类型安全**：避免绑定错误（如将图像参数绑定到数值数据源）
