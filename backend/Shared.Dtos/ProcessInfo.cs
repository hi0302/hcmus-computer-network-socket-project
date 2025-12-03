using System;

namespace Shared.Dtos
{
    /// <summary>
    /// Cấu trúc dữ liệu chi tiết của một tiến trình đang chạy (Process)
    /// Dùng để Agent gửi về Backend
    /// </summary>
    public class ProcessInfo
    {
        // ID của tiến trình (PID)
        public int Pid { get; set; }

        // Tên của tiến trình (ví dụ: "notepad")
        public string Name { get; set; } = string.Empty;

        // Tiêu đề của cửa sổ ứng dụng (có thể rỗng nếu là tiến trình nền)
        public string? Title { get; set; }

        // Lượng bộ nhớ (RAM) đang sử dụng (đơn vị: Megabytes)
        public long MemoryMB { get; set; }
    }
}
