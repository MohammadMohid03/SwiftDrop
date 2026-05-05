using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SwiftDrop.Views
{
    public partial class QuickLookWindow : Window
    {
        private static QuickLookWindow? _instance;
        private bool _isClosing;

        // Image extensions we can preview
        private static readonly string[] ImageExtensions =
            { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".tiff", ".ico" };

        // Text extensions we can preview
        private static readonly string[] TextExtensions =
            { ".txt", ".cs", ".js", ".ts", ".py", ".json", ".xml", ".xaml",
              ".html", ".css", ".md", ".yml", ".yaml", ".ini", ".cfg",
              ".log", ".ps1", ".bat", ".cmd", ".sh" };

        public QuickLookWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Shows a Quick Look preview of the given file.
        /// Only one instance at a time — toggles off if same file.
        /// </summary>
        public static void Toggle(string filePath)
        {
            // If already showing this file, close it
            if (_instance != null)
            {
                _instance.Close();
                _instance = null;
                return;
            }

            var window = new QuickLookWindow();
            window.LoadPreview(filePath);
            _instance = window;
            window.Show();
            window.Focus();
        }

        private void LoadPreview(string filePath)
        {
            string fileName = Path.GetFileName(filePath);
            string ext = Path.GetExtension(filePath).ToLowerInvariant();

            TitleText.Text = fileName;

            if (Array.Exists(ImageExtensions, e => e == ext))
            {
                LoadImagePreview(filePath);
            }
            else if (Array.Exists(TextExtensions, e => e == ext))
            {
                LoadTextPreview(filePath);
            }
            else
            {
                LoadFileInfo(filePath);
            }
        }

        private void LoadImagePreview(string filePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                ImagePreview.Source = bitmap;
                ImagePreview.Visibility = Visibility.Visible;

                // Auto-size window based on image
                double maxW = SystemParameters.PrimaryScreenWidth * 0.5;
                double maxH = SystemParameters.PrimaryScreenHeight * 0.6;
                double imgW = bitmap.PixelWidth;
                double imgH = bitmap.PixelHeight;

                double scale = Math.Min(maxW / imgW, maxH / imgH);
                if (scale < 1)
                {
                    Width = imgW * scale + 30;
                    Height = imgH * scale + 80;
                }
                else
                {
                    Width = Math.Max(imgW + 30, 300);
                    Height = Math.Max(imgH + 80, 200);
                }
            }
            catch
            {
                LoadFileInfo(filePath);
            }
        }

        private void LoadTextPreview(string filePath)
        {
            try
            {
                // Read first 50KB max
                var fi = new FileInfo(filePath);
                string text;
                if (fi.Length > 50_000)
                    text = File.ReadAllText(filePath)[..50_000] + "\n\n... (truncated)";
                else
                    text = File.ReadAllText(filePath);

                TextContent.Text = text;
                TextPreview.Visibility = Visibility.Visible;
            }
            catch
            {
                LoadFileInfo(filePath);
            }
        }

        private void LoadFileInfo(string filePath)
        {
            FileInfoPanel.Visibility = Visibility.Visible;

            var item = new Models.DroppedFileItem { Path = filePath };
            FileIcon.Text = item.FileIconGlyph;
            FileNameText.Text = Path.GetFileName(filePath);

            try
            {
                if (File.Exists(filePath))
                {
                    var fi = new FileInfo(filePath);
                    FileSizeText.Text = FormatFileSize(fi.Length);
                }
                else if (Directory.Exists(filePath))
                {
                    var files = Directory.GetFiles(filePath, "*", SearchOption.AllDirectories);
                    FileSizeText.Text = $"Folder — {files.Length} files";
                }
            }
            catch
            {
                FileSizeText.Text = "";
            }
        }

        private static string FormatFileSize(long bytes) => bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
            < 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
            _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
        };

        // ── Close handlers ───────────────────────────────────────────────

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space || e.Key == Key.Escape)
                ClosePreview();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ClosePreview();
        }

        private void Window_Deactivated(object? sender, EventArgs e)
        {
            ClosePreview();
        }

        private void ClosePreview()
        {
            if (_isClosing)
                return;

            _isClosing = true;
            _instance = null;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _instance = null;
            base.OnClosed(e);
        }
    }
}
