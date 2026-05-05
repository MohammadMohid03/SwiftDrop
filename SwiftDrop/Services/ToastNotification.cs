using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace SwiftDrop.Services
{
    /// <summary>
    /// Shows a non-blocking floating toast notification at the top of the screen.
    /// Does NOT steal focus (unlike MessageBox), so it won't trigger window Deactivated.
    /// </summary>
    public static class ToastNotification
    {
        private static Window? _toastWindow;
        private static DispatcherTimer? _hideTimer;

        public static void Show(string message, int durationMs = 3500)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Close existing toast
                _hideTimer?.Stop();
                _toastWindow?.Close();

                var textBlock = new TextBlock
                {
                    Text = message,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x20, 0x2C)),
                    FontFamily = new FontFamily("Segoe UI Variable Text, Segoe UI Variable, Segoe UI"),
                    FontSize = 13,
                    TextWrapping = TextWrapping.Wrap,
                    MaxWidth = 400,
                    Margin = new Thickness(16, 12, 16, 12)
                };

                var border = new Border
                {
                    CornerRadius = new CornerRadius(14),
                    Padding = new Thickness(0),
                    BorderThickness = new Thickness(1),
                    Child = textBlock,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        Color = Color.FromRgb(0x43, 0x53, 0x6F),
                        BlurRadius = 24,
                        ShadowDepth = 8,
                        Opacity = 0.18
                    }
                };

                border.Background = new SolidColorBrush(Color.FromArgb(0xF6, 0xFF, 0xFF, 0xFF));
                border.BorderBrush = new SolidColorBrush(Color.FromArgb(0x12, 0x0F, 0x17, 0x2A));

                _toastWindow = new Window
                {
                    Content = border,
                    WindowStyle = WindowStyle.None,
                    AllowsTransparency = true,
                    Background = Brushes.Transparent,
                    Topmost = true,
                    ShowInTaskbar = false,
                    ShowActivated = false,       // CRITICAL: don't steal focus
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ResizeMode = ResizeMode.NoResize,
                    WindowStartupLocation = WindowStartupLocation.Manual
                };

                // Position at top-center of screen, below the trigger arrow
                _toastWindow.Loaded += (_, _) =>
                {
                    var screenWidth = SystemParameters.PrimaryScreenWidth;
                    _toastWindow.Left = (screenWidth - _toastWindow.ActualWidth) / 2;
                    _toastWindow.Top = 40;
                };

                _toastWindow.Show();

                // Fade in
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250));
                _toastWindow.BeginAnimation(Window.OpacityProperty, fadeIn);

                // Auto-hide timer
                _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(durationMs) };
                _hideTimer.Tick += (_, _) =>
                {
                    _hideTimer.Stop();
                    if (_toastWindow != null)
                    {
                        var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
                        fadeOut.Completed += (_, _) =>
                        {
                            _toastWindow?.Close();
                            _toastWindow = null;
                        };
                        _toastWindow.BeginAnimation(Window.OpacityProperty, fadeOut);
                    }
                };
                _hideTimer.Start();
            });
        }
    }
}
