using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SwiftDrop.Helpers
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility v && v == Visibility.Visible;
    }

    public class BoolToInverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility v && v == Visibility.Collapsed;
    }

    public class StringToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex)
            {
                try
                {
                    hex = hex.TrimStart('#');

                    byte a = 255, r, g, b;
                    if (hex.Length == 8)
                    {
                        a = System.Convert.ToByte(hex[0..2], 16);
                        r = System.Convert.ToByte(hex[2..4], 16);
                        g = System.Convert.ToByte(hex[4..6], 16);
                        b = System.Convert.ToByte(hex[6..8], 16);
                    }
                    else
                    {
                        r = System.Convert.ToByte(hex[0..2], 16);
                        g = System.Convert.ToByte(hex[2..4], 16);
                        b = System.Convert.ToByte(hex[4..6], 16);
                    }

                    return new SolidColorBrush(Color.FromArgb(a, r, g, b));
                }
                catch { }
            }

            return new SolidColorBrush(Colors.CornflowerBlue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Converts a hex color string to a LinearGradientBrush.
    /// Creates a subtle diagonal gradient from the given color to a slightly shifted variant.
    /// </summary>
    public class StringToGradientBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex)
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(hex);

                    // Create a lighter/shifted end color for the gradient
                    var endColor = Color.FromArgb(
                        color.A,
                        (byte)Math.Min(255, color.R + 30),
                        (byte)Math.Min(255, color.G + 15),
                        (byte)Math.Max(0, color.B - 20));

                    // Create a darker start color
                    var startColor = Color.FromArgb(
                        color.A,
                        (byte)Math.Max(0, color.R - 20),
                        (byte)Math.Max(0, color.G - 10),
                        (byte)Math.Min(255, color.B + 15));

                    var brush = new LinearGradientBrush
                    {
                        StartPoint = new Point(0, 0),
                        EndPoint = new Point(1, 1)
                    };
                    brush.GradientStops.Add(new GradientStop(startColor, 0.0));
                    brush.GradientStops.Add(new GradientStop(color, 0.5));
                    brush.GradientStops.Add(new GradientStop(endColor, 1.0));
                    brush.Freeze();

                    return brush;
                }
                catch { }
            }

            return new SolidColorBrush(Colors.CornflowerBlue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}