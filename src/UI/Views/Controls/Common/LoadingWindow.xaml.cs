using System.Windows;

namespace SunEyeVision.UI.Views.Controls.Common
{
    /// <summary>
    /// LoadingWindow.xaml 的交互逻辑
    /// </summary>
    public partial class LoadingWindow : Window
    {
        public string Message
        {
            get => MessageText.Text;
            set => MessageText.Text = value;
        }

        public LoadingWindow(string message = "请稍�?)
        {
            InitializeComponent();
            Message = message;
            Owner = Application.Current?.MainWindow;
            ShowInTaskbar = false;
        }
    }
}
