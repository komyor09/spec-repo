using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ShopApp.Data;
using ShopApp.Infrastructure;
using ShopApp.Services;

var builder = WebApplication.CreateBuilder(args);

// --- База данных (EF Core + SQLite) ---
builder.Services.AddDbContext<ShopDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

// --- Сервисы бизнес-логики ---
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<OrderService>();

// --- Контроллеры: enum'ы в JSON отдаём строками ("New", "Shipped" ...) ---
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

// Создаём БД и заполняем начальными данными при первом запуске.
// Если перейдёте на миграции — замените EnsureCreated() на Migrate() (см. README).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
    db.Database.EnsureCreated();
}

app.UseMiddleware<ErrorHandlingMiddleware>();

// Фронтенд лежит в wwwroot: index.html отдаётся по адресу "/"
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.Run();
