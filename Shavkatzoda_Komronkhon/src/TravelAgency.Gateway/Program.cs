using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 1. Настраиваем защиту от DDoS / спама запросами (Rate Limiting)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddFixedWindowLimiter("fixed-window-limit", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 30; // Максимум 30 запросов в минуту
        opt.QueueLimit = 0;   // Лишние запросы сразу отклоняем без очереди
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

// 2. Подключаем YARP и загружаем маршруты из appsettings.json
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Включаем middleware ограничителя запросов ПЕРЕД проксированием
app.UseRateLimiter();

// Включаем сам Reverse Proxy YARP
app.MapReverseProxy();

app.Run();