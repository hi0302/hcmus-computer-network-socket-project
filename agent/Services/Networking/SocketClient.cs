
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using RemoteControl.Agent.Services.Media;
using RemoteControl.Agent.Services.System;

namespace RemoteControl.Agent.Services.Networking
{
    public class SocketClient
    {
        private readonly HubConnection _hubConnection;
        private readonly ILogger<SocketClient> _logger;

        // Inject các service xử lý
        private readonly ProcessHandler _processHandler;
        private readonly ScreenCapturer _screenCapturer;

        public SocketClient(
            ILogger<SocketClient> logger,
            ProcessHandler processHandler,
            ScreenCapturer screenCapturer)
        {
            _logger = logger;
            _processHandler = processHandler;
            _screenCapturer = screenCapturer;

            // LƯU Ý: Nếu chạy máy ảo, hãy thay 'localhost' bằng IP của máy thật (VD: http://192.168.1.10:5000/commandHub)
            string serverUrl = "http://localhost:5000/commandHub";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(serverUrl)
                .WithAutomaticReconnect()
                .Build();

            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            
            _hubConnection.On("GetProcesses", async () =>
            {
                _logger.LogInformation("Nhận lệnh lấy danh sách Process...");

                var list = _processHandler.GetProcesses();

                await _hubConnection.SendAsync("ReceiveProcessList", list);

                _logger.LogInformation($"Đã gửi {list.Count} process về server.");
            });

            // Xử lý chụp màn hình
            _hubConnection.On("CaptureScreen", async () =>
            {
                _logger.LogInformation("Nhận lệnh chụp màn hình...");
                var imageBytes = _screenCapturer.TakeScreenshot();

                // Gửi ảnh về Server
                await _hubConnection.SendAsync("ReceiveScreenshot", imageBytes);
            });

            // Xử lý Kill Process (Ví dụ nhận thêm tham số pid)
            _hubConnection.On<int>("KillProcess", (pid) =>
            {
                _logger.LogInformation($"Nhận lệnh Kill Process ID: {pid}");
                bool success = _processHandler.KillProcess(pid);
                // Có thể gửi thông báo thành công/thất bại về lại server nếu cần
            });
        }

        public async Task ConnectAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Chỉ start nếu đang ngắt kết nối
                    if (_hubConnection.State == HubConnectionState.Disconnected)
                    {
                        await _hubConnection.StartAsync(token);
                        _logger.LogInformation(">>> Đã kết nối thành công tới Server Backend!");
                    }

                    // Chờ một chút để không lặp quá nhanh, hoặc dùng Semaphore để đợi
                    await Task.Delay(5000, token);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Chưa kết nối được Server: {ex.Message}. Thử lại sau 5s...");
                    await Task.Delay(5000, token);
                }
            }
        }
    }
}