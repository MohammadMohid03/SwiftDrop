using System;
using System.Windows;
using SwiftDrop.Services;

namespace SwiftDrop
{
    public partial class App : Application
    {
        private TrayIconService? _trayIconService;
        private GlobalDragHookService? _dragHookService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Keep app running even when all windows are "hidden"
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var window = new MainWindow();
            window.Show();

            // Initialize system tray icon
            _trayIconService = new TrayIconService(window);
            window.TrayService = _trayIconService;

            // Initialize global drag-to-top hook
            _dragHookService = new GlobalDragHookService();
            _dragHookService.DragToTopDetected += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    // Auto-show the panel when files are dragged to top-center
                    window.ShowPanel();
                    window.Activate();
                });
            };
            _dragHookService.Start();

            window.DragHookService = _dragHookService;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _dragHookService?.Dispose();
            _trayIconService?.Dispose();
            base.OnExit(e);
        }
    }
}