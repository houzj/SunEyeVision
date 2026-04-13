using SunEyeVision.Plugin.SDK.Execution.Parameters;
using System.Windows;
using System.Windows.Controls;

namespace SunEyeVision.Plugin.SDK.UI.Controls.Region.Views
{
    /// <summary>
    /// 区域订阅面板
    /// </summary>
    public partial class RegionSubscribePanel : UserControl
    {
        /// <summary>
        /// AvailableDataSources 依赖属性
        /// </summary>
        public static readonly DependencyProperty AvailableDataSourcesProperty =
            DependencyProperty.Register(
                nameof(AvailableDataSources),
                typeof(System.Collections.ObjectModel.ObservableCollection<AvailableDataSource>),
                typeof(RegionSubscribePanel),
                new PropertyMetadata(null));

        /// <summary>
        /// 可用数据源集合
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<AvailableDataSource>? AvailableDataSources
        {
            get => (System.Collections.ObjectModel.ObservableCollection<AvailableDataSource>?)GetValue(AvailableDataSourcesProperty);
            set => SetValue(AvailableDataSourcesProperty, value);
        }

        public RegionSubscribePanel()
        {
            InitializeComponent();
        }
    }
}