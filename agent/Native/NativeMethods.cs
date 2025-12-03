// File: Native/NativeMethods.cs
using System.Runtime.InteropServices;

namespace RemoteControl.Agent.Native // Namespace khớp với thư mục Native
{
    public static class NativeMethods
    {
        // Hàm lấy độ rộng/cao của màn hình (User32.dll)
        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        // Các hằng số để lấy chiều rộng (0) và chiều cao (1)
        public const int SM_CXSCREEN = 0;
        public const int SM_CYSCREEN = 1;
    }
}