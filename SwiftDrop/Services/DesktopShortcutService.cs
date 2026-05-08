using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace SwiftDrop.Services
{
    internal static class DesktopShortcutService
    {
        public static void EnsureShortcut()
        {
            string? executablePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                executablePath = Process.GetCurrentProcess().MainModule?.FileName;

            if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                return;

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.IsNullOrWhiteSpace(desktopPath))
                return;

            string shortcutPath = Path.Combine(desktopPath, "SwiftDrop.lnk");
            CreateShortcut(shortcutPath, executablePath);
        }

        private static void CreateShortcut(string shortcutPath, string executablePath)
        {
            dynamic? shell = null;
            dynamic? shortcut = null;

            try
            {
                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                {
                    AppDiagnostics.LogException("DesktopShortcutService.EnsureShortcut", new InvalidOperationException("WScript.Shell is not available."));
                    return;
                }

                shell = Activator.CreateInstance(shellType);
                shortcut = shell!.CreateShortcut(shortcutPath);
                shortcut.TargetPath = executablePath;
                shortcut.WorkingDirectory = Path.GetDirectoryName(executablePath) ?? "";
                shortcut.IconLocation = executablePath + ",0";
                shortcut.Description = "SwiftDrop";
                shortcut.Save();
            }
            catch (Exception ex)
            {
                AppDiagnostics.LogException("DesktopShortcutService.EnsureShortcut", ex);
            }
            finally
            {
                if (shortcut != null && Marshal.IsComObject(shortcut))
                    Marshal.ReleaseComObject(shortcut);

                if (shell != null && Marshal.IsComObject(shell))
                    Marshal.ReleaseComObject(shell);
            }
        }
    }
}
