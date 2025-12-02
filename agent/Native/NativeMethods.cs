// File: Native/NativeMethods.cs
using System.Runtime.InteropServices;

namespace RemoteControl.Agent.Native // Namespace khớp với thư mục Native
{
    public static class NativeMethods
    {
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        // Hàm lấy độ rộng/cao của màn hình (User32.dll)
        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        // Các hằng số để lấy chiều rộng (0) và chiều cao (1)
        public const int SM_CXSCREEN = 0;
        public const int SM_CYSCREEN = 1;
        // Các hằng số cho Hook
        public const int WH_KEYBOARD_LL = 13;
        public const int WM_KEYDOWN = 0x0100;

        // Cài đặt Hook
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        // Gỡ bỏ Hook
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        // Chuyển tiếp sự kiện phím cho ứng dụng khác (để không bị liệt phím)
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        // Lấy module handle (cần để cài hook)
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        // Vòng lặp tin nhắn (Bắt buộc phải có để Hook hoạt động)
        [DllImport("user32.dll")]
        public static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        public static extern bool TranslateMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll")]
        public static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public Point pt;
        }
    }
}