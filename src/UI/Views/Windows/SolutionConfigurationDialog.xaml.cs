using System.Windows;
using SunEyeVision.UI.ViewModels;
using SunEyeVision.Workflow;

namespace SunEyeVision.UI.Views.Windows;

/// <summary>
/// SolutionConfigurationDialog.xaml 的交互逻辑
/// </summary>
public partial class SolutionConfigurationDialog : Window
{
    private readonly SolutionConfigurationDialogViewModel _viewModel;

    /// <summary>
    /// 获取启动结果
    /// </summary>
    public (Project? Project, InspectionRecipe? Recipe)? LaunchResult => _viewModel.LaunchResult;

    /// <summary>
    /// 是否成功启动
    /// </summary>
    public bool IsLaunched => _viewModel.IsLaunched;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="projectManager">项目管理器</param>
    /// <param name="preselectProjectId">预选中的项目ID</param>
    /// <param name="preselectRecipeName">预选中的配方名称</param>
    public SolutionConfigurationDialog(ProjectManager projectManager, string? preselectProjectId = null, string? preselectRecipeName = null)
    {
        InitializeComponent();

        _viewModel = new SolutionConfigurationDialogViewModel(projectManager, preselectProjectId, preselectRecipeName);
        DataContext = _viewModel;
    }

    /// <summary>
    /// 窗口加载时
    /// </summary>
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // 可以在这里添加额外的初始化逻辑
    }

    /// <summary>
    /// 窗口关闭时
    /// </summary>
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // 如果已启动，直接关闭
        if (_viewModel.IsLaunched)
        {
            base.OnClosing(e);
            return;
        }

        // 保存当前项目和配方（确保修改的名称和描述被保存）
        _viewModel.SaveCurrentProject();

        // 如果用户没有点击启动或跳过，确认是否关闭
        var result = MessageBox.Show(
            "确定要关闭配置界面吗？",
            "确认",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.No)
        {
            e.Cancel = true;
        }

        base.OnClosing(e);
    }
}
