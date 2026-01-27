using System;
using System.Windows;

namespace TestApp
{
    public class App : Application
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                var app = new App();
                var window = new MainWindow();
                app.Run(window);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                Console.ReadKey();
            }
        }
    }

    public class MainWindow : Window
    {
        public MainWindow()
        {
            Title = "Test Window";
            Width = 400;
            Height = 300;
            Content = new System.Windows.Controls.TextBlock { Text = "Hello World!", HorizontalAlignment = System.Windows.HorizontalAlignment.Center, VerticalAlignment = System.Windows.VerticalAlignment.Center };
        }
    }
}