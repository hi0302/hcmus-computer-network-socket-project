using YourApplicationName.Hubs; // <--- SỬA LẠI namespace này cho khớp với thư mục Hubs của bạn
using Microsoft.AspNetCore.SignalR;
// using backend.Services; // <--- Uncomment nếu bạn đã có file AgentTrackerService

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // Dùng SwaggerGen chuẩn của .NET 8

// --- 1. ĐĂNG KÝ SERVICE ---
// Nếu bạn chưa có file AgentTrackerService thì comment dòng dưới lại để tránh lỗi
// builder.Services.AddSingleton<IAgentTrackerService, AgentTrackerService>();

builder.Services.AddSignalR();

// --- 2. CẤU HÌNH CORS (CHO PHÉP TẤT CẢ) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policyBuilder => policyBuilder
        .AllowAnyMethod()
        .AllowAnyHeader()
        .SetIsOriginAllowed((host) => true) // <--- QUAN TRỌNG: Cho phép mọi IP kết nối (kể cả máy ảo)
        .AllowCredentials());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

// --- 3. KÍCH HOẠT CORS ---
app.UseCors("CorsPolicy"); // Phải đặt trước MapHub

// --- 4. MAP ĐƯỜNG DẪN HUB ---
// Đảm bảo tên Class Hub là ControlHub (khớp với file ControlHub.cs bạn có)
// Đường dẫn là "/controlhub" (khớp với Agent)
app.MapHub<ControlHub>("/controlhub");
app.MapControllers();

// Lắng nghe mọi IP (để máy ảo nhìn thấy)
app.Run();