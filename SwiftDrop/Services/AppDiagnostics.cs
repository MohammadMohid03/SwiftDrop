using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace SwiftDrop.Services
{
    internal static class AppDiagnostics
    {
        private static readonly string AppDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SwiftDrop");

        private static readonly string LogFilePath = Path.Combine(AppDataDir, "startup.log");

        public static void InitializeGlobalHandlers()
        {
            Directory.CreateDirectory(AppDataDir);

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                LogException("AppDomain.CurrentDomain.UnhandledException", args.ExceptionObject as Exception);
            };

            TaskScheduler.UnobservedTaskException += (_, args) =>
            {
                LogException("TaskScheduler.UnobservedTaskException", args.Exception);
                args.SetObserved();
            };
        }

        public static void RegisterDispatcherHandler(Application app)
        {
            app.DispatcherUnhandledException += OnDispatcherUnhandledException;
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogException("Application.DispatcherUnhandledException", e.Exception);
            ShowFatalError(e.Exception);
            e.Handled = true;
            Application.Current.Shutdown();
        }

        public static void LogException(string source, Exception? ex)
        {
            try
            {
                Directory.CreateDirectory(AppDataDir);

                var builder = new StringBuilder();
                builder.AppendLine("==================================================");
                builder.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                builder.AppendLine(source);
                builder.AppendLine(ex?.ToString() ?? "No exception details were available.");
                File.AppendAllText(LogFilePath, builder.ToString());
            }
            catch
            {
                // Avoid crashing while logging a crash.
            }
        }

        public static void ShowFatalError(Exception ex)
        {
            MessageBox.Show(
                "SwiftDrop failed to start.\n\n" +
                ex.Message +
                "\n\nDiagnostic log:\n" +
                LogFilePath,
                "SwiftDrop Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
