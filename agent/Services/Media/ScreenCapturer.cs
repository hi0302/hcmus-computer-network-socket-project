// File: Services/Media/ScreenCapturer.cs
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning; // Để đánh dấu chỉ chạy trên Windows
using RemoteControl.Agent.Native; // Để dùng hàm GetSystemMetrics

namespace RemoteControl.Agent.Services.Media // Namespace khớp với thư mục Media
{
    // Đánh dấu class này chỉ hoạt động trên Windows (để tránh cảnh báo vàng)
    [SupportedOSPlatform("windows")]
    public class ScreenCapturer
    {
        public byte[] TakeScreenshot()
        {
            try
            {
                // 1. Lấy độ phân giải màn hình từ NativeMethods
                int width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
                int height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);

                // 2. Tạo một tấm ảnh bitmap rỗng với kích thước đó
                using (Bitmap bmp = new Bitmap(width, height))
                {
                    // 3. Tạo đối tượng Graphics để vẽ từ màn hình vào tấm ảnh
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        // Copy toàn bộ điểm ảnh từ màn hình (Góc trái trên 0,0)
                        g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                    }

                    // 4. Lưu ảnh vào MemoryStream (RAM) dưới dạng JPEG
                    using (MemoryStream ms = new MemoryStream())
                    {
                        // Nén ảnh dạng Jpeg để gửi qua mạng cho nhẹ
                        bmp.Save(ms, ImageFormat.Jpeg);

                        // Trả về mảng byte (để gửi qua SignalR)
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                // Nếu lỗi (ví dụ chưa cài thư viện), in ra console
                Console.WriteLine($"Lỗi chụp màn hình: {ex.Message}");
                return Array.Empty<byte>(); // Trả về mảng rỗng
            }
        }
    }
}