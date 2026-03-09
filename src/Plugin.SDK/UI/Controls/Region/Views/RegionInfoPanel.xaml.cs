using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SunEyeVision.Plugin.SDK.Logging;

namespace SunEyeVision.Plugin.SDK.UI.Controls.Region.Views
{
    /// <summary>
    /// 区域信息面板
    /// </summary>
    public partial class RegionInfoPanel : UserControl
    {
        public RegionInfoPanel()
        {
            // 设计时跳过初始化
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                InitializeComponent();
                return;
            }

            InitializeComponent();
        }

        private void SelectColorButton_Click(object sender, RoutedEventArgs e)
        {
            // 简化实现：使用预设颜色列表
            // 实际项目中应该使用专业的颜色选择器控件
            if (DataContext is ViewModels.RegionEditorViewModel viewModel && 
                viewModel.SelectedRegion != null)
            {
                // 默认设置为红色
                viewModel.SelectedRegion.DisplayColor = 0xFFFF0000;
            }
        }
    }
}
