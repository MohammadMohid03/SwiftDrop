using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace SwiftDrop.Helpers
{
    /// <summary>
    /// Generates a SwiftDrop tray icon programmatically using System.Drawing.
    /// Creates a stylish "S↓" icon with a gradient background.
    /// </summary>
    public static class IconGenerator
    {
        public static Icon CreateTrayIcon(int size = 32)
        {
            using var bitmap = new Bitmap(size, size);
            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // Gradient circle background
            var rect = new Rectangle(1, 1, size - 2, size - 2);
            using var gradient = new LinearGradientBrush(rect,
                Color.FromArgb(129, 140, 248),  // Indigo-400
                Color.FromArgb(192, 132, 252),  // Purple-400
                45f);
            g.FillEllipse(gradient, rect);

            // Draw arrow-down icon (↓) centered
            using var arrowPen = new Pen(Color.White, 2.2f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };

            float cx = size / 2f;
            float cy = size / 2f;
            float arrowSize = size * 0.28f;

            // Vertical line
            g.DrawLine(arrowPen, cx, cy - arrowSize, cx, cy + arrowSize);

            // Arrow head
            g.DrawLine(arrowPen, cx - arrowSize * 0.6f, cy + arrowSize * 0.4f, cx, cy + arrowSize);
            g.DrawLine(arrowPen, cx + arrowSize * 0.6f, cy + arrowSize * 0.4f, cx, cy + arrowSize);

            // Convert bitmap to icon
            IntPtr hIcon = bitmap.GetHicon();
            return Icon.FromHandle(hIcon);
        }

        /// <summary>
        /// Creates and saves an .ico file to the Assets folder.
        /// Called once during first run if the icon doesn't exist.
        /// </summary>
        public static string EnsureIconFile(string assetsFolder)
        {
            var iconPath = Path.Combine(assetsFolder, "swiftdrop.ico");
            if (!File.Exists(iconPath))
            {
                Directory.CreateDirectory(assetsFolder);
                using var icon = CreateTrayIcon(48);
                using var stream = new FileStream(iconPath, FileMode.Create);
                icon.Save(stream);
            }
            return iconPath;
        }
    }
}
