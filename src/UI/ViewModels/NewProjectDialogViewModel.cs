using System;
using System.IO;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.ViewModels
{
/// <summary>
/// 新建项目对话框 ViewModel
/// </summary>
public class NewProjectDialogViewModel : ViewModelBase
{
    private string _projectName = string.Empty;
    private string _projectPath = string.Empty;
    private string _description = string.Empty;

    /// <summary>
    /// 项目名称
    /// </summary>
    public string ProjectName
    {
        get => _projectName;
        set => SetProperty(ref _projectName, value, "项目名称");
    }

    /// <summary>
    /// 项目路径（父目录）
    /// </summary>
    public string ProjectPath
    {
        get => _projectPath;
        set => SetProperty(ref _projectPath, value, "项目路径");
    }

    /// <summary>
    /// 预期的完整项目路径（只读）
    /// </summary>
    public string ExpectedProjectPath
    {
        get
        {
            if (string.IsNullOrWhiteSpace(_projectName) || string.IsNullOrWhiteSpace(_projectPath))
                return "请先输入项目名称";

            // 文件名清理：替换非法字符为下划线
            var sanitizedName = SanitizeFileName(_projectName);
            return Path.Combine(_projectPath, sanitizedName);
        }
    }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value, "描述");
    }

    /// <summary>
    /// 验证输入
    /// </summary>
    /// <returns>是否有效</returns>
    public bool Validate()
    {
        if (string.IsNullOrWhiteSpace(ProjectName))
        {
            LogWarning("项目名称不能为空");
            return false;
        }

        if (string.IsNullOrWhiteSpace(ProjectPath))
        {
            LogWarning("项目路径不能为空");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 清理文件名（移除非法字符）
    /// </summary>
    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars));
        return sanitized;
    }
}
}
