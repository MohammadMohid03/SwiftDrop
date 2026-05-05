using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SwiftDrop.Helpers
{
    /// <summary>
    /// Enables Windows 11 Acrylic backdrop blur on a WPF window via DwmSetWindowAttribute.
    /// Falls back gracefully on older Windows versions.
    /// </summary>
    public static class AcrylicHelper
    {
        // DWM attribute for system backdrop type (Windows 11 22H2+)
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;

        // Extend frame into client area attribute
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        // Backdrop types
        private const int DWMSBT_DISABLE = 1;
        private const int DWMSBT_MAINWINDOW = 2;       // Mica
        private const int DWMSBT_TRANSIENTWINDOW = 3;   // Acrylic
        private const int DWMSBT_TABBEDWINDOW = 4;      // Tabbed

        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void DwmSetWindowAttribute(
            IntPtr hwnd,
            int attribute,
            ref int pvAttribute,
            int cbAttribute);

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int Left, Right, Top, Bottom;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        /// <summary>
        /// Applies Acrylic blur to the given WPF window.
        /// Must be called after the window's HWND is available (e.g., in OnSourceInitialized).
        /// </summary>
        public static bool EnableAcrylic(Window window)
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return false;

                // Enable dark mode for the title bar area
                int darkMode = 1;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE,
                    ref darkMode, sizeof(int));

                // Extend frame into entire client area
                var margins = new MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
                DwmExtendFrameIntoClientArea(hwnd, ref margins);

                // Set Acrylic backdrop
                int backdropType = DWMSBT_TRANSIENTWINDOW;
                DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE,
                    ref backdropType, sizeof(int));

                return true;
            }
            catch
            {
                // Silently fail on unsupported Windows versions
                return false;
            }
        }

        /// <summary>
        /// Applies Mica backdrop to the given WPF window.
        /// </summary>
        public static bool EnableMica(Window window)
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero) return false;

                int darkMode = 1;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE,
                    ref darkMode, sizeof(int));

                var margins = new MARGINS { Left = -1, Right = -1, Top = -1, Bottom = -1 };
                DwmExtendFrameIntoClientArea(hwnd, ref margins);

                int backdropType = DWMSBT_MAINWINDOW;
                DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE,
                    ref backdropType, sizeof(int));

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
