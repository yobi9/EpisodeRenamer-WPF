using System;
using System.Windows;
using System.Windows.Controls;

namespace EpisodeRenamer.App.UI;

internal class ShowNameDialog : Window
{
    private readonly TextBox _input;

    internal ShowNameDialog(string? initial)
    {
        Title = "\u062a\u0633\u0645\u064a\u0629 \u0627\u0644\u0645\u0633\u0644\u0633\u0644";
        Width = 420;
        Height = 160;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = System.Windows.Media.Brushes.White;

        var panel = new System.Windows.Controls.StackPanel
        {
            Margin = new Thickness(12),
            FlowDirection = FlowDirection.RightToLeft
        };

        var label = new Label
        {
            Content = "\u0627\u0633\u0645 \u0627\u0644\u0645\u0633\u0644\u0633\u0644:",
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            FontSize = 13
        };
        panel.Children.Add(label);

        _input = new TextBox
        {
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 12)
        };
        if (!string.IsNullOrWhiteSpace(initial)) _input.Text = initial;
        _input.SelectAll();
        panel.Children.Add(_input);

        var buttons = new System.Windows.Controls.StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            FlowDirection = FlowDirection.LeftToRight
        };

        var ok = new Button
        {
            Content = "\u0645\u0648\u0627\u0641\u0642",
            Width = 80,
            Padding = new Thickness(6, 3, 6, 3),
            IsDefault = true
        };
        ok.Click += (_, _) => { DialogResult = true; };
        buttons.Children.Add(ok);

        var cancel = new Button
        {
            Content = "\u0625\u0644\u063a\u0627\u0621",
            Width = 80,
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(6, 3, 6, 3),
            IsCancel = true
        };
        buttons.Children.Add(cancel);

        panel.Children.Add(buttons);
        Content = panel;

        _input.Focus();
        _input.KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter) DialogResult = true;
        };
    }

    internal string? ResultText => DialogResult == true ? _input.Text.Trim() : null;
}