using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Ryujinx.Ava.Ui.Windows;
using Ryujinx.HLE.Ui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Ava.Ui.Applet
{
    class AvaloniaHostUiTheme : IHostUiTheme
    {
        public AvaloniaHostUiTheme(MainWindow parent)
        {
            FontFamily               = OperatingSystem.IsWindows() && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000, 0) ? "Segoe UI Variable" : parent.FontFamily.Name;
            DefaultBackgroundColor   = BrushToThemeColor(parent.Background);
            DefaultForegroundColor   = BrushToThemeColor(parent.Foreground);
            DefaultBorderColor       = BrushToThemeColor(parent.BorderBrush);
            SelectionBackgroundColor = BrushToThemeColor(parent.SearchBox.SelectionBrush);
            SelectionForegroundColor = BrushToThemeColor(parent.SearchBox.SelectionForegroundBrush);
        }

        public string FontFamily { get; }

        public ThemeColor DefaultBackgroundColor   { get; }
        public ThemeColor DefaultForegroundColor   { get; }
        public ThemeColor DefaultBorderColor       { get; }
        public ThemeColor SelectionBackgroundColor { get; }
        public ThemeColor SelectionForegroundColor { get; }

        private ThemeColor BrushToThemeColor(IBrush brush)
        {
            if (brush is SolidColorBrush solidColor)
            {
                return new ThemeColor((float)solidColor.Color.A / 255,
                    (float)solidColor.Color.R / 255,
                    (float)solidColor.Color.G / 255,
                    (float)solidColor.Color.B / 255);
            }
            else return new ThemeColor();
        }

        private ThemeColor ColorToThemeColor(Color color)
        {
                return new ThemeColor((float)color.A / 255,
                    (float)color.R / 255,
                    (float)color.G / 255,
                    (float)color.B / 255);
        }
    }
}
