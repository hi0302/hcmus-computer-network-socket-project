using System.Threading;
using ClientAgent.Core; // Dòng này QUAN TRỌNG: để nhận diện ClientSocket

// --- Điểm bắt đầu chương trình (Entry Point) ---

// 1. Khởi tạo kết nối Socket chạy trên luồng nền
ClientSocket.Initialize();

// 2. Giữ cho ứng dụng chạy vĩnh viễn (không bị tắt ngay lập tức)
// Vì là Agent chạy nền, ta cần treo tiến trình chính ở đây.
Thread.Sleep(Timeout.Infinite);