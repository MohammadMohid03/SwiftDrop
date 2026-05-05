using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace SwiftDrop.Services
{
    /// <summary>
    /// System-wide mouse hook that detects when the user is dragging a file
    /// (left button held) and moves the cursor to the top-center edge of the screen.
    /// When triggered, it signals the MainWindow to slide open.
    /// 
    /// Uses SetWindowsHookEx with WH_MOUSE_LL for a low-level global hook.
    /// </summary>
    public sealed class GlobalDragHookService : IDisposable
    {
        // ── Win32 Interop ────────────────────────────────────────────────

        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEMOVE = 0x0200;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn,
            IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int VK_LBUTTON = 0x01;

        // ── Fields ───────────────────────────────────────────────────────

        private IntPtr _hookId = IntPtr.Zero;
        private readonly LowLevelMouseProc _hookCallback;
        private bool _disposed;

        /// <summary>
        /// Height in pixels from the top of the screen that counts as the "trigger zone".
        /// </summary>
        public int TriggerZoneHeight { get; set; } = 8;

        /// <summary>
        /// Width of the center zone (in pixels) where the trigger is active.
        /// Centered on the screen.
        /// </summary>
        public int TriggerZoneWidth { get; set; } = 600;

        /// <summary>
        /// Fires when a drag-to-top-center is detected.
        /// </summary>
        public event Action? DragToTopDetected;

        /// <summary>
        /// Fires when the dragged cursor leaves the trigger zone.
        /// </summary>
        public event Action? DragLeftTriggerZone;

        private bool _isInTriggerZone = false;
        private readonly DispatcherTimer _debounceTimer;

        // ── Constructor ──────────────────────────────────────────────────

        public GlobalDragHookService()
        {
            // Must hold reference to prevent GC from collecting the delegate
            _hookCallback = HookCallback;

            // Debounce timer to avoid rapid-fire events
            _debounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _debounceTimer.Tick += (_, _) => _debounceTimer.Stop();
        }

        // ── Start / Stop ─────────────────────────────────────────────────

        public void Start()
        {
            if (_hookId != IntPtr.Zero) return;

            using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule!;
            _hookId = SetWindowsHookEx(WH_MOUSE_LL, _hookCallback,
                GetModuleHandle(curModule.ModuleName!), 0);

            if (_hookId == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                System.Diagnostics.Debug.WriteLine($"[GlobalDragHook] SetWindowsHookEx failed: {error}");
            }
        }

        public void Stop()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        // ── Hook Callback ────────────────────────────────────────────────

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_MOUSEMOVE)
            {
                var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                int mouseX = hookStruct.pt.x;
                int mouseY = hookStruct.pt.y;

                // Check if left mouse button is held (indicating a drag)
                bool isLeftButtonDown = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;

                if (isLeftButtonDown)
                {
                    bool inZone = IsInTriggerZone(mouseX, mouseY);

                    if (inZone && !_isInTriggerZone)
                    {
                        _isInTriggerZone = true;

                        if (!_debounceTimer.IsEnabled)
                        {
                            _debounceTimer.Start();
                            Application.Current?.Dispatcher.BeginInvoke(() =>
                            {
                                DragToTopDetected?.Invoke();
                            });
                        }
                    }
                    else if (!inZone && _isInTriggerZone)
                    {
                        _isInTriggerZone = false;
                        Application.Current?.Dispatcher.BeginInvoke(() =>
                        {
                            DragLeftTriggerZone?.Invoke();
                        });
                    }
                }
                else
                {
                    // Mouse button released — reset state
                    if (_isInTriggerZone)
                    {
                        _isInTriggerZone = false;
                    }
                }
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private bool IsInTriggerZone(int mouseX, int mouseY)
        {
            // Must be within the top N pixels
            if (mouseY > TriggerZoneHeight) return false;

            // Must be in the center zone
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double centerX = screenWidth / 2;
            double halfZone = TriggerZoneWidth / 2.0;

            return mouseX >= (centerX - halfZone) && mouseX <= (centerX + halfZone);
        }

        // ── Dispose ──────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }
    }
}
