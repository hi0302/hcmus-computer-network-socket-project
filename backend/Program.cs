using Microsoft.AspNetCore.SignalR;
using YourApplicationName.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<IAgentTrackerService, AgentTrackerService>();
builder.Services.AddOpenApi();

//Thêm dịch vụ SignalR
builder.Services.AddSignalR();
//Thêm dịch vụ CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policyBuilder => policyBuilder
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()
        // thay thế "http://..." bằng địa chỉ IP/port của máy ảo frontend
        .WithOrigins("http://localhost:8080", "http://10.29.xx.xx"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

app.UseDefaultFiles();

app.UseStaticFiles();

app.UseAuthorization();

app.UseCors("CorsPolicy");

app.MapHub<YourApplicationName.Hubs.ControlHub>("/controlhub");

app.MapControllers();

app.Run();
