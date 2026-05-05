using System;
using System.Windows;
using SwiftDrop.Helpers;
using Forms = System.Windows.Forms;

namespace SwiftDrop.Services
{
    /// <summary>
    /// Manages the system tray (notification area) icon for SwiftDrop.
    /// Provides Show/Hide/Exit via right-click context menu and
    /// double-click to toggle the panel.
    /// </summary>
    public sealed class TrayIconService : IDisposable
    {
        private Forms.NotifyIcon? _notifyIcon;
        private readonly MainWindow _mainWindow;
        private bool _disposed;

        public TrayIconService(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            Initialize();
        }

        private void Initialize()
        {
            // Create tray icon
            _notifyIcon = new Forms.NotifyIcon
            {
                Icon = IconGenerator.CreateTrayIcon(32),
                Text = "SwiftDrop — Drop files to the top of your screen",
                Visible = true
            };

            // Context menu
            var contextMenu = new Forms.ContextMenuStrip();
            contextMenu.Renderer = new DarkToolStripRenderer();

            var showItem = new Forms.ToolStripMenuItem("Show SwiftDrop")
            {
                Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold)
            };
            showItem.Click += (_, _) => ShowPanel();

            var hideItem = new Forms.ToolStripMenuItem("Hide");
            hideItem.Click += (_, _) => HidePanel();

            var separator = new Forms.ToolStripSeparator();

            var exitItem = new Forms.ToolStripMenuItem("Exit");
            exitItem.Click += (_, _) => ExitApp();

            contextMenu.Items.Add(showItem);
            contextMenu.Items.Add(hideItem);
            contextMenu.Items.Add(separator);
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;

            // Double-click toggles panel
            _notifyIcon.DoubleClick += (_, _) => TogglePanel();

            // Single click shows panel
            _notifyIcon.MouseClick += (_, e) =>
            {
                if (e.Button == Forms.MouseButtons.Left)
                    TogglePanel();
            };
        }

        private void ShowPanel()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _mainWindow.ShowPanel();
                _mainWindow.Activate();
            });
        }

        private void HidePanel()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _mainWindow.HidePanel();
            });
        }

        private void TogglePanel()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _mainWindow.TogglePanel();
                if (_mainWindow.Visibility != Visibility.Visible)
                {
                    _mainWindow.Show();
                }
                _mainWindow.Activate();
            });
        }

        private void ExitApp()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Dispose();
                Application.Current.Shutdown();
            });
        }

        /// <summary>
        /// Updates the tray icon tooltip text.
        /// </summary>
        public void UpdateTooltip(string text)
        {
            if (_notifyIcon != null)
                _notifyIcon.Text = text.Length > 63 ? text[..63] : text;
        }

        /// <summary>
        /// Shows a balloon notification from the tray.
        /// </summary>
        public void ShowBalloon(string title, string text, Forms.ToolTipIcon icon = Forms.ToolTipIcon.Info)
        {
            _notifyIcon?.ShowBalloonTip(3000, title, text, icon);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }
    }

    /// <summary>
    /// Custom dark renderer for the tray context menu to match the app's dark theme.
    /// </summary>
    internal class DarkToolStripRenderer : Forms.ToolStripProfessionalRenderer
    {
        public DarkToolStripRenderer() : base(new DarkColorTable()) { }

        protected override void OnRenderItemText(Forms.ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = System.Drawing.Color.FromArgb(240, 240, 255);
            base.OnRenderItemText(e);
        }

        protected override void OnRenderMenuItemBackground(Forms.ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected)
            {
                using var brush = new System.Drawing.SolidBrush(
                    System.Drawing.Color.FromArgb(60, 60, 90));
                e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
            }
            else
            {
                using var brush = new System.Drawing.SolidBrush(
                    System.Drawing.Color.FromArgb(30, 30, 50));
                e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
            }
        }
    }

    internal class DarkColorTable : Forms.ProfessionalColorTable
    {
        public override System.Drawing.Color MenuItemSelected => System.Drawing.Color.FromArgb(60, 60, 90);
        public override System.Drawing.Color MenuBorder => System.Drawing.Color.FromArgb(50, 50, 80);
        public override System.Drawing.Color MenuItemBorder => System.Drawing.Color.FromArgb(80, 80, 120);
        public override System.Drawing.Color ToolStripDropDownBackground => System.Drawing.Color.FromArgb(30, 30, 50);
        public override System.Drawing.Color ImageMarginGradientBegin => System.Drawing.Color.FromArgb(30, 30, 50);
        public override System.Drawing.Color ImageMarginGradientMiddle => System.Drawing.Color.FromArgb(30, 30, 50);
        public override System.Drawing.Color ImageMarginGradientEnd => System.Drawing.Color.FromArgb(30, 30, 50);
        public override System.Drawing.Color SeparatorDark => System.Drawing.Color.FromArgb(50, 50, 80);
        public override System.Drawing.Color SeparatorLight => System.Drawing.Color.FromArgb(40, 40, 65);
    }
}
