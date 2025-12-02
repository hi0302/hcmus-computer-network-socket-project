// File: Services/System/PowerHandler.cs
using System.Diagnostics;
using System.Runtime.Versioning;

namespace RemoteControl.Agent.Services.System
{
    // Đánh dấu chỉ chạy trên Windows để tránh cảnh báo vàng
    [SupportedOSPlatform("windows")]
    public class PowerHandler
    {
        // 1. Hàm Tắt máy (Shutdown)
        public void Shutdown()
        {
            // /s: Shutdown (Tắt máy)
            // /t 0: Thời gian chờ 0 giây (Tắt ngay lập tức)
            // /f: Force (Buộc đóng các ứng dụng đang chạy)
            RunCommand("/s /t 0 /f");
        }

        // 2. Hàm Khởi động lại (Restart)
        public void Restart()
        {
            // /r: Restart (Khởi động lại)
            RunCommand("/r /t 0 /f");
        }

        // 3. Hàm Ngủ đông (Hibernate) - Tùy chọn thêm
        public void Hibernate()
        {
            // /h: Hibernate
            RunCommand("/h");
        }

        // Hàm chung để chạy lệnh CMD
        private void RunCommand(string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo("shutdown", arguments)
                {
                    CreateNoWindow = true, // Quan trọng: Không hiện cửa sổ đen cmd lên màn hình
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi thực thi lệnh nguồn: {ex.Message}");
            }
        }
    }
}