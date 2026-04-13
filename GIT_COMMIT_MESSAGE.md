# Git 提交信息

## 标题
feat: 实现 ParamValue<> 类型与 UI 控件的自动适配

## 描述

### 问题
`ParamValue<>` 类型与 UI 控件绑定时，控件内部调用 `Convert.ToDouble()` 期望值类型，但收到的是包装器实例，导致 `InvalidCastException`：
```
Unable to cast object of type 'ParamValue<int>' to type 'System.IConvertible'
```

### 解决方案
在 `ConfigSetting` 控件中添加类型适配器，自动解包和包装 `ParamValue<>` 类型：

1. **添加 `UnwrapParamValue` 方法**：
   - 检测 `ParamValue<>` 类型
   - 通过反射获取 `Value` 属性
   - 返回解包后的实际值

2. **添加 `WrapToParamValue` 方法**：
   - 检测当前绑定的值是否是 `ParamValue<>`
   - 通过反射设置 `Value` 属性
   - 返回包装后的实例

3. **修改 `UpdateInternalNumericValue` 方法**：
   - 在转换前先调用 `UnwrapParamValue` 解包

4. **修改 `SetFromInternalNumericValue` 方法**：
   - 在设置前先调用 `WrapToParamValue` 包装

5. **修改 XAML 绑定路径**：
   - `Threshold` → `Threshold.Value`
   - `MaxValue` → `MaxValue.Value`
   - `Type` → `Type.Value`

### 涉及的控件
- ✅ `ConfigSetting` - 添加类型适配器
- ✅ `ComboBox` - 修改绑定路径
- ✅ `ParamBinding` - 无需修改（不涉及 `ParamValue<>`）
- ✅ `ImageSourceSelector` - 无需修改（绑定到 `AvailableDataSource?`）
- ✅ `RegionEditorControl` - 无需修改（绑定到 `IEnumerable<RegionData>`）

### 修改的文件
1. `src/Plugin.SDK/UI/Controls/ConfigSetting.cs`
   - 添加 2 个适配器方法
   - 修改 2 个现有方法

2. `tools/SunEyeVision.Tool.Threshold/Views/ThresholdToolDebugControl.xaml`
   - 修改 3 处绑定路径

### 编译结果
- ✅ Plugin.SDK 项目：192 个警告，0 个错误
- ✅ Threshold 工具项目：196 个警告，0 个错误
- ✅ 所有警告都是项目中已存在的警告，非本次修改引入

### 测试验证
待测试：
- [ ] 基本参数编辑测试
- [ ] 参数绑定测试
- [ ] 序列化测试
- [ ] 运行测试

## 相关文档
- 实施总结：`docs/implementation-summary-param-value-integration.md`
- 方案文档：`docs/PARAM_VALUE_INTEGRATION_SOLUTION.md`

## 关键词
ParamValue, UI 控件, 类型适配, 绑定, ConfigSetting
