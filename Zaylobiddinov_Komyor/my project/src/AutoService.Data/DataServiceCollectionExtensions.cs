using AutoService.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AutoService.Data;

/// <summary>Регистрация слоя данных в контейнере зависимостей.</summary>
public static class DataServiceCollectionExtensions
{
    public static IServiceCollection AddAutoServiceData(this IServiceCollection services, string databasePath)
    {
        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
        services.AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>();
        return services;
    }

    /// <summary>Создаёт базу данных при первом запуске и заполняет её тестовыми данными.</summary>
    public static void InitializeAutoServiceDatabase(this IServiceProvider provider)
    {
        var factory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var db = factory.CreateDbContext();
        DbSeeder.Seed(db);
    }
}
