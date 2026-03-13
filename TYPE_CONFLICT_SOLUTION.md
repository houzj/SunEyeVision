# 类型冲突解决方案

## 问题描述

项目 `SunEyeVision.UI.csproj` 同时启用了 WPF (`UseWPF`) 和 WinForms (`UseWindowsForms`)，导致大量类型名称冲突（CS0104 错误）。

## 冲突类型列表

### 1. Point
- **System.Drawing.Point**
- **System.Windows.Point**
- **影响范围**: 整个项目（365+ 处）

### 2. Color
- **System.Drawing.Color**
- **System.Windows.Media.Color**
- **影响范围**: UI 层适配器、转换器、渲染器等

### 3. UserControl
- **System.Windows.Controls.UserControl** (WPF)
- **System.Windows.Forms.UserControl** (WinForms)
- **影响范围**: 用户控件、共享控件

### 4. Timer
- **System.Windows.Forms.Timer** (WinForms)
- **System.Threading.Timer** (System.Threading)
- **影响范围**: 性能监控、缓存管理

### 5. DragEventArgs
- **System.Windows.Forms.DragEventArgs** (WinForms)
- **System.Windows.DragEventArgs** (WPF)
- **影响范围**: 拖放处理

### 6. MouseEventArgs
- **System.Windows.Forms.MouseEventArgs** (WinForms)
- **System.Windows.Input.MouseEventArgs** (WPF)
- **影响范围**: 交互处理

### 7. 其他冲突类型
- Button, Control, ListBox, TabControl, Size, Image, Brush, Application 等

## 解决方案

### 方案1：统一使用 WPF（推荐）

**优点：**
- WPF 是主要 UI 框架
- 减少类型冲突
- 更符合现代 .NET 开发实践

**实施步骤：**

1. **移除 `UseWindowsForms`**
   ```xml
   <!-- 删除这行 -->
   <UseWindowsForms>true</UseWindowsForms>
   ```

2. **替换 WinForms 控件为 WPF 等效控件**

   | WinForms 控件 | WPF 等效控件 |
   |----------------|---------------|
   | FolderBrowserDialog | 使用 Microsoft.Toolkit.Win32.UI.Dialogs.OpenFileDialog（设置文件夹模式） |
   | 或实现自定义 WPF 文件夹选择对话框 |

3. **修改受影响的文件**
   - `SolutionPathDialogViewModel.cs` - 移除 `using System.Windows.Forms;`，实现 WPF 文件夹选择器
   - 其他使用 WinForms 类型的文件

**示例代码：**

```csharp
// 使用 Win32 API 选择文件夹（WPF 兼容）
using Microsoft.Win32.SafeHandles;
using Microsoft.Win32.SafeHandles;

public class FolderBrowserHelper
{
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SHBrowseForFolder(ref BROWSEINFO lpbi);
    
    public static string BrowseForFolder(string description, string initialPath)
    {
        var info = new BROWSEINFO
        {
            pidlRoot = 0,
            pszDisplayName = description,
            ulFlags = 0x0001, // BIF_RETURNONLYFSDIRS
            pszPath = initialPath
        };
        
        IntPtr pidlList = SHBrowseForFolder(ref info);
        if (pidlList == IntPtr.Zero)
            return null;
            
        // 解析 PIDL 返回路径
        return GetPathFromPIDL(pidlList);
    }
}
```

### 方案2：保持 UseWindowsForms，使用限定类型

**优点：**
- 不需要重构现有 WinForms 代码
- 快速实施

**缺点：**
- 需要在所有冲突类型处使用完全限定名称
- 代码可读性下降

**实施步骤：**

在项目文件中添加 `DisableWinFormsReferencesWarnings` 或在每个文件中使用限定名称：

```csharp
// 方案 2A：在每个文件中 using 别名
using WpfPoint = System.Windows.Point;
using WpfColor = System.Windows.Media.Color;
using WinForms = System.Windows.Forms;

// 方案 2B：使用完全限定名称
public void SomeMethod()
{
    var point1 = new System.Drawing.Point(10, 20);    // 明确
    var point2 = new System.Windows.Point(10, 20);   // 明确
    var point3 = new WpfPoint(10, 20);              // 使用别名
}
```

### 方案3：项目分层（架构重构）

**优点：**
- 彻底解决类型冲突
- 更好的架构分离

**实施步骤：**

1. **拆分项目**
   - `SunEyeVision.UI.Wpf.csproj` - WPF 界面
   - `SunEyeVision.UI.WinForms.csproj` - WinForms 组件
   - `SunEyeVision.UI.Core.csproj` - 共享代码

2. **引用关系**
   - WPF 项目引用 WinForms 项目（通过接口）
   - 两个项目都引用 Core 项目

## 立即可行的快速修复

如果需要快速修复以允许编译通过，使用方案 2：

在 `SunEyeVision.UI.csproj` 中添加：

```xml
<ItemGroup>
  <!-- 抑制类型冲突警告 -->
  <NoWarn>$(NoWarn);1591;CS0104;CS1060</NoWarn>
</ItemGroup>
```

然后添加 `using System.Windows.Forms;` 到需要的文件。

## 长期建议

1. **逐步迁移到纯 WPF**
   - 新功能使用 WPF 控件
   - 旧功能逐步重构

2. **代码审查指南**
   - 明确禁止在 WPF 项目中混用 WinForms
   - 除非有明确的互操作需求

3. **使用 MVVM 模式**
   - 分离 UI 和逻辑
   - 减少对具体 UI 框架的依赖

## 相关文件

需要修改的主要文件：
- `src/UI/SunEyeVision.UI.csproj`
- `src/UI/ViewModels/SolutionPathDialogViewModel.cs`
- `src/UI/Services/**/*.cs` (PathCalculators, Rendering, Thumbnail 等)
- `src/UI/Views/**/*.cs` (Controls, Windows)

预计修改文件数：100+ 文件
