using System;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.ViewModels
{
/// <summary>
/// 新建配方对话框 ViewModel
/// </summary>
public class NewRecipeDialogViewModel : ViewModelBase
{
    private string _recipeName = "新配方";
    private string _description = string.Empty;

    /// <summary>
    /// 配方名称
    /// </summary>
    public string RecipeName
    {
        get => _recipeName;
        set => SetProperty(ref _recipeName, value, "配方名称");
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
        if (string.IsNullOrWhiteSpace(RecipeName))
        {
            LogWarning("配方名称不能为空");
            return false;
        }

        return true;
    }
}
}
