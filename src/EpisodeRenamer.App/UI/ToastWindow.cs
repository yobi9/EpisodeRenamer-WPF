using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace EpisodeRenamer.App.UI;

internal sealed class ToastWindow : Window
{
    public ToastWindow(string message)
    {
        Title = "";
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        SizeToContent = SizeToContent.WidthAndHeight;
        FlowDirection = FlowDirection.LeftToRight;
        Background = new SolidColorBrush(Color.FromRgb(0x2C, 0x3E, 0x50));
        AllowsTransparency = true;
        IsHitTestVisible = false;

        var border = new System.Windows.Controls.Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x2C, 0x3E, 0x50)),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16, 10, 16, 10),
            Child = new System.Windows.Controls.TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 420
            }
        };
        Content = border;

        var area = SystemParameters.WorkArea;
        double w = Math.Min(450, ActualWidth + 32);
        Left = area.Right - w - 20;
        Top = area.Bottom - Height - 60;

        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Close();
        };
        timer.Start();
    }
}