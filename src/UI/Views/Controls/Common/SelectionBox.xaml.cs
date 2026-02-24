using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SunEyeVision.UI.Views.Controls.Common
{
    public partial class SelectionBox : UserControl
    {
        private Point _startPoint;

        public SelectionBox()
        {
            InitializeComponent();
        }

        public void StartSelection(Point startPoint)
        {
            _startPoint = startPoint;
            SelectionBorder.Visibility = Visibility.Visible;
            UpdateSelection(startPoint);
        }

        public void UpdateSelection(Point endPoint)
        {
            double x = Math.Min(_startPoint.X, endPoint.X);
            double y = Math.Min(_startPoint.Y, endPoint.Y);
            double width = Math.Abs(endPoint.X - _startPoint.X);
            double height = Math.Abs(endPoint.Y - _startPoint.Y);

            SelectionBorder.Width = width;
            SelectionBorder.Height = height;
            SelectionBorder.Margin = new Thickness(x, y, 0, 0);
        }

        public Rect GetSelectionRect()
        {
            double x = SelectionBorder.Margin.Left;
            double y = SelectionBorder.Margin.Top;
            return new Rect(x, y, SelectionBorder.Width, SelectionBorder.Height);
        }

        public void SetItemCount(int count)
        {
            ItemCountText.Text = $"已选中 {count} 个项�?;
            ItemCountText.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public void EndSelection()
        {
            SelectionBorder.Visibility = Visibility.Collapsed;
            ItemCountText.Visibility = Visibility.Collapsed;
        }
    }
}
