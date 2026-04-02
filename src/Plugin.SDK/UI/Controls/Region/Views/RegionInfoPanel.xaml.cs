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

            // 绑定颜色选择按钮点击事件
            SelectColorButton.Click += SelectColorButton_Click;
        }

        private void SelectColorButton_Click(object sender, RoutedEventArgs e)
        {
            // 打开颜色选择器弹窗
            ColorPickerPopup.IsOpen = true;
        }
    }
}
