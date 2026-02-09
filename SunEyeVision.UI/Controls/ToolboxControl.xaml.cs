using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using SunEyeVision.UI.Models;
using SunEyeVision.UI.ViewModels;

namespace SunEyeVision.UI.Controls
{
    /// <summary>
    /// ToolboxControl.xaml 的交互逻辑（超简化版）
    /// </summary>
    public partial class ToolboxControl : UserControl
    {
        private ToolboxViewModel _viewModel;
        private Point _popupPosition;  // Popup的屏幕坐标

        public ToolboxControl()
        {
            InitializeComponent();
            _viewModel = new ToolboxViewModel();
            DataContext = _viewModel;

            // 设置Popup的DataContext（Popup不在Visual Tree中，需要手动设置）
            CompactModePopup.DataContext = _viewModel;

            // 设置Popup的自定义定位模式
            CompactModePopup.Placement = PlacementMode.Custom;
            CompactModePopup.CustomPopupPlacementCallback = CustomPopupPlacementMethod;

            CompactModePopup.Opened += OnPopupOpened;
            CompactModePopup.Closed += OnPopupClosed;
        }

        /// <summary>
        /// 自定义Popup定位方法
        /// </summary>
        private CustomPopupPlacement[] CustomPopupPlacementMethod(Size popupSize, Size targetSize, Point offset)
        {
            System.Diagnostics.Debug.WriteLine($"[Toolbox] ========== CustomPopupPlacementMethod called ==========");
            System.Diagnostics.Debug.WriteLine($"[Toolbox] Popup size: {popupSize.Width}x{popupSize.Height}");
            System.Diagnostics.Debug.WriteLine($"[Toolbox] Popup position: {_popupPosition.X}, {_popupPosition.Y}");

            // 返回计算好的屏幕坐标位置
            var placement = new CustomPopupPlacement(_popupPosition, PopupPrimaryAxis.Horizontal);
            System.Diagnostics.Debug.WriteLine($"[Toolbox] Created placement: {placement.Point.X}, {placement.Point.Y}, Axis: {placement.PrimaryAxis}");

            return new CustomPopupPlacement[] { placement };
        }

        /// <summary>
        /// 开始拖拽工具项
        /// </summary>
        private void ToolItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is ToolItem tool)
            {
                var data = new DataObject("ToolItem", tool);
                DragDrop.DoDragDrop(border, data, DragDropEffects.Copy);
            }
        }

        /// <summary>
        /// 鼠标悬停在分类图标上（紧凑模式）
        /// </summary>
        private void CategoryItem_MouseEnter(object sender, MouseEventArgs _)
        {
            if (sender is Border border && border.Tag is ToolCategory category)
            {
                System.Diagnostics.Debug.WriteLine($"[Toolbox] ========== MouseEnter category: {category.Name} ==========");
                System.Diagnostics.Debug.WriteLine($"[Toolbox] IsCompactMode: {_viewModel.IsCompactMode}");
                System.Diagnostics.Debug.WriteLine($"[Toolbox] SelectedCategoryTools count: {_viewModel.SelectedCategoryTools.Count}");

                // 计算分类项的屏幕坐标
                // 1. 先获取分类项相对于UserControl的位置
                var categoryPosition = border.TransformToAncestor(this).Transform(new Point(0, 0));
                System.Diagnostics.Debug.WriteLine($"[Toolbox] Category relative position: {categoryPosition.X}, {categoryPosition.Y}");

                // 2. 获取UserControl在Window中的位置
                var controlPosition = this.PointToScreen(new Point(0, 0));
                System.Diagnostics.Debug.WriteLine($"[Toolbox] Control screen position: {controlPosition.X}, {controlPosition.Y}");

                // 3. 计算popup的屏幕坐标（在分类项右侧5px处）
                _popupPosition = new Point(
                    controlPosition.X + categoryPosition.X + 60 + 5,  // 分类项右侧(60px是侧边栏宽度) + 5px偏移
                    controlPosition.Y + categoryPosition.Y
                );
                System.Diagnostics.Debug.WriteLine($"[Toolbox] Popup screen position: {_popupPosition.X}, {_popupPosition.Y}");

                // 悬停时直接打开popup
                _viewModel.SelectedCategory = category.Name;
                System.Diagnostics.Debug.WriteLine($"[Toolbox] Setting SelectedCategory to: {category.Name}");
                System.Diagnostics.Debug.WriteLine($"[Toolbox] SelectedCategoryTools count after: {_viewModel.SelectedCategoryTools.Count}");

                // 直接设置Popup.IsOpen
                CompactModePopup.IsOpen = true;
                System.Diagnostics.Debug.WriteLine($"[Toolbox] Set CompactModePopup.IsOpen to: true");
                System.Diagnostics.Debug.WriteLine($"[Toolbox] CompactModePopup.IsOpen: {CompactModePopup.IsOpen}");
                System.Diagnostics.Debug.WriteLine($"[Toolbox] Popup PlacementMode: {CompactModePopup.Placement}");
                System.Diagnostics.Debug.WriteLine($"[Toolbox] Popup CustomCallback set: {CompactModePopup.CustomPopupPlacementCallback != null}");
            }
        }

        /// <summary>
        /// Popup打开时的处理
        /// </summary>
        private void OnPopupOpened(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[Toolbox] ========== Popup Opened event fired ==========");
            System.Diagnostics.Debug.WriteLine($"[Toolbox] Popup DataContext: {CompactModePopup.DataContext?.GetType().Name}");
            System.Diagnostics.Debug.WriteLine($"[Toolbox] SelectedCategory: {_viewModel.SelectedCategory}");
            System.Diagnostics.Debug.WriteLine($"[Toolbox] SelectedCategoryTools count: {_viewModel.SelectedCategoryTools.Count}");
            System.Diagnostics.Debug.WriteLine($"[Toolbox] SelectedCategoryIcon: {_viewModel.SelectedCategoryIcon}");

            // 设计时不执行计算，避免设计器错误
            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                CompactModePopup.MaxHeight = 500;
                return;
            }

            // 根据屏幕位置动态计算Popup最大高度
            try
            {
                var availableHeight = SystemParameters.PrimaryScreenHeight - 100;
                var newMaxHeight = Math.Max(400, Math.Min(500, availableHeight));
                CompactModePopup.MaxHeight = newMaxHeight;
            }
            catch
            {
                CompactModePopup.MaxHeight = 500;
            }
        }

        /// <summary>
        /// Popup关闭时的处理
        /// </summary>
        private void OnPopupClosed(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[Toolbox] Popup Closed event fired");
        }

        /// <summary>
        /// 鼠标离开Popup区域
        /// </summary>
        private void CompactModePopupBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("[Toolbox] Popup MouseLeave");

            // 延迟关闭，给用户移动鼠标的时间
            var closeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            closeTimer.Tick += (s, args) =>
            {
                closeTimer.Stop();

                // 检查鼠标是否真的离开了popup和侧边栏
                try
                {
                    var currentPopupPos = Mouse.GetPosition(CompactModePopupBorder);
                    var currentSidebarPos = Mouse.GetPosition(CategorySidebar);

                    bool isOverPopup = currentPopupPos.X >= 0 && currentPopupPos.X <= CompactModePopupBorder.ActualWidth &&
                                     currentPopupPos.Y >= 0 && currentPopupPos.Y <= CompactModePopupBorder.ActualHeight;

                    bool isOverSidebar = currentSidebarPos.X >= 0 && currentSidebarPos.X <= CategorySidebar.ActualWidth &&
                                       currentSidebarPos.Y >= 0 && currentSidebarPos.Y <= CategorySidebar.ActualHeight;

                    if (!isOverPopup && !isOverSidebar)
                    {
                        // 关闭Popup
                        CompactModePopup.IsOpen = false;
                        _viewModel.SelectedCategory = null;
                        System.Diagnostics.Debug.WriteLine("[Toolbox] Popup closed by MouseLeave (mouse really left)");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[Toolbox] Mouse still over popup or sidebar, keeping open");
                    }
                }
                catch
                {
                    // 如果popup已经关闭，忽略错误
                }
            };
            closeTimer.Start();
        }

        /// <summary>
        /// 点击分类标题（展开模式）
        /// </summary>
        private void ExpandCategory_MouseLeftButtonUp(object sender, MouseButtonEventArgs _)
        {
            if (sender is Border border && border.Tag is ToolCategory category)
            {
                // 切换展开/折叠状态
                category.IsExpanded = !category.IsExpanded;
            }
        }
    }
}
