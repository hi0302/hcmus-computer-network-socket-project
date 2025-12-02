// File: Worker.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteControl.Agent.Services.Networking;

namespace RemoteControl.Agent
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly SocketClient _socketClient;

        // Tiêm (Inject) SocketClient vào Worker để sử dụng
        public Worker(ILogger<Worker> logger, SocketClient socketClient)
        {
            _logger = logger;
            _socketClient = socketClient;
        }

        // Hàm này chạy ngay khi ứng dụng khởi động
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Agent đang khởi động...");

            try
            {
                // Gọi hàm kết nối tới Server Backend
                await _socketClient.ConnectAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi không thể kết nối tới Server: {ex.Message}");
            }

            // Vòng lặp giữ cho Service không bị tắt (Heartbeat)
            while (!stoppingToken.IsCancellationRequested)
            {
                // Có thể gửi tin nhắn "Ping" lên server mỗi 1 phút để báo "Tao còn sống"
                // _logger.LogInformation("Agent is still alive at: {time}", DateTimeOffset.Now);
                await Task.Delay(60000, stoppingToken);
            }
        }
    }
}