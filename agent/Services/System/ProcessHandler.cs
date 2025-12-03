// File: Services/System/ProcessHandler.cs
using System.Diagnostics; // Thư viện quản lý Process của .NET
using System.Runtime.Versioning;

namespace RemoteControl.Agent.Services.System
{
    // Đánh dấu chỉ chạy trên Windows
    [SupportedOSPlatform("windows")]
    public class ProcessHandler
    {
        // 1. Lấy danh sách tất cả Process đang chạy
        public List<ProcessModel> GetProcesses()
        {
            var result = new List<ProcessModel>();

            // Lấy toàn bộ process trong máy
            var processes = Process.GetProcesses();

            foreach (var p in processes)
            {
                try
                {
                    // Lọc bớt các process hệ thống không quan trọng nếu muốn
                    result.Add(new ProcessModel
                    {
                        Id = p.Id,                 // Process ID (PID)
                        Name = p.ProcessName,      // Tên (ví dụ: chrome, notepad)
                        MemoryMB = p.WorkingSet64 / 1024 / 1024, // Đổi Byte sang MB
                        WindowTitle = p.MainWindowTitle // Tiêu đề cửa sổ (Nếu có => là App)
                    });
                }
                catch
                {
                    // Một số process hệ thống (System) sẽ không cho phép đọc thông tin -> Bỏ qua
                    continue;
                }
            }

            // Sắp xếp theo tên cho dễ nhìn
            return result.OrderBy(x => x.Name).ToList();
        }

        // 2. Tắt (Kill) một Process dựa vào ID
        public bool KillProcess(int pid)
        {
            try
            {
                var process = Process.GetProcessById(pid);
                process.Kill(); // Lệnh tắt process
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi không thể kill PID {pid}: {ex.Message}");
                return false;
            }
        }

        // 3. Bật (Start) một ứng dụng mới
        public bool StartProcess(string fileName)
        {
            try
            {
                Process.Start(fileName); // Ví dụ: "notepad", "calc", "chrome.exe"
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi không thể start {fileName}: {ex.Message}");
                return false;
            }
        }
    }

    // Class chứa dữ liệu để gửi đi (DTO)
    // Bạn có thể để class này ở đây hoặc chuyển sang thư mục Models tùy ý
    public class ProcessModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public long MemoryMB { get; set; }
        public string WindowTitle { get; set; } // Dùng để phân biệt App vs Process ngầm
    }
}