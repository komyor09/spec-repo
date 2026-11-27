using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using VetMed.Api.Application.Interfaces;
using VetMed.Api.Application.Services;
using VetMed.Api.Infrastructure.Persistence;
using VetMed.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"))
    && !args.Any(a => a.StartsWith("--urls", StringComparison.OrdinalIgnoreCase)))
{
    builder.WebHost.UseUrls("http://localhost:3000");
}

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });

builder.Services.AddSingleton<IClinicSeedProvider, DefaultClinicSeedProvider>();
builder.Services.AddSingleton<IClinicRepository, JsonClinicRepository>();
builder.Services.AddScoped<IScheduleValidator, ClinicScheduleValidator>();
builder.Services.AddScoped<IPharmacyService, PharmacyService>();
builder.Services.AddScoped<IBillingService, ClinicBillingService>();
builder.Services.AddScoped<IClinicService, ClinicService>();

var app = builder.Build();

app.UseMiddleware<DomainExceptionMiddleware>();

var workspaceRoot = WorkspacePathResolver.ResolveWorkspaceRoot();
var distPath = Path.Combine(workspaceRoot, "dist");
var distIndex = Path.Combine(distPath, "index.html");

if (Directory.Exists(distPath) && File.Exists(distIndex))
{
    var fileProvider = new PhysicalFileProvider(distPath);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });
}

app.MapControllers();

if (Directory.Exists(distPath) && File.Exists(distIndex))
{
    app.MapFallback(async context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { error = "Маршрут API не найден" });
            return;
        }

        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(distIndex);
    });
}
else
{
    app.MapGet("/", () => Results.Content(
        """
        <!doctype html>
        <html lang="ru">
        <head><meta charset="utf-8"><title>ВетМед — C# API</title></head>
        <body style="font-family: system-ui, sans-serif; max-width: 680px; margin: 48px auto; padding: 0 20px; line-height: 1.6;">
          <h1>API «ВетМед» на C# (.NET 8) успешно запущен</h1>
          <p>Сервер работает на <code>http://localhost:3000</code>, но папка <code>dist/</code> с интерфейсом ещё не собрана.</p>
          <p>Чтобы открыть полный веб-интерфейс через C#-сервер:</p>
          <ol>
            <li>Выполните в корне проекта: <code>npm install</code> и <code>npm run build</code></li>
            <li>Перезапустите <code>dotnet run --project VetMed.Api</code></li>
          </ol>
          <p>Проверка работы API прямо сейчас: <a href="/api/clinic/state">/api/clinic/state</a> | <a href="/api/clinic/oop-summary">/api/clinic/oop-summary</a></p>
        </body>
        </html>
        """,
        "text/html; charset=utf-8"));
}

app.Run();
