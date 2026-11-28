using Microsoft.EntityFrameworkCore;
using ShopApp.Models;

namespace ShopApp.Data;

/// <summary>Начальные данные каталога (HasData — попадают в БД при создании/миграции).</summary>
public static class SeedData
{
    public static void Apply(ModelBuilder b)
    {
        b.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Электроника", Icon = "💻" },
            new Category { Id = 2, Name = "Книги", Icon = "📚" },
            new Category { Id = 3, Name = "Дом и кухня", Icon = "🏠" },
            new Category { Id = 4, Name = "Спорт", Icon = "⚽" }
        );

        // Для HasData значения должны быть константами — поэтому дата фиксированная
        var created = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        b.Entity<Product>().HasData(
            P(1, 1, "Беспроводные наушники", "Bluetooth 5.3, активное шумоподавление, до 30 часов работы.", 649_000, 25, created),
            P(2, 1, "Смартфон 128 ГБ", "Экран 6.5\", тройная камера 50 Мп, аккумулятор 5000 мА·ч.", 3_290_000, 10, created.AddDays(1)),
            P(3, 1, "Механическая клавиатура", "Hot-swap переключатели, RGB-подсветка, раскладка RU/EN.", 890_000, 15, created.AddDays(2)),
            P(4, 1, "Внешний SSD 1 ТБ", "USB-C, скорость чтения до 1050 МБ/с, металлический корпус.", 1_150_000, 8, created.AddDays(3)),
            P(5, 2, "Чистый код", "Классика о том, как писать понятный и поддерживаемый код.", 189_000, 30, created.AddDays(4)),
            P(6, 2, "C# для профессионалов", "Подробно о языке C# и платформе .NET.", 245_000, 12, created.AddDays(5)),
            P(7, 2, "Изучаем SQL", "Основы запросов, соединений и проектирования БД.", 159_000, 20, created.AddDays(6)),
            P(8, 3, "Электрический чайник", "1.7 л, стеклянная колба, поддержание температуры.", 329_000, 18, created.AddDays(7)),
            P(9, 3, "Набор ножей", "5 ножей из нержавеющей стали с деревянной подставкой.", 415_000, 9, created.AddDays(8)),
            P(10, 3, "Кофемолка", "Жерновая, 18 степеней помола.", 560_000, 6, created.AddDays(9)),
            P(11, 4, "Коврик для йоги", "Нескользящий, толщина 6 мм, ремень для переноски.", 139_000, 40, created.AddDays(10)),
            P(12, 4, "Гантели 2×5 кг", "Разборные, обрезиненные блины.", 299_000, 14, created.AddDays(11)),
            P(13, 4, "Футбольный мяч", "Размер 5, ручная сшивка.", 210_000, 0, created.AddDays(12)) // нет в наличии — для проверки
        );
    }

    private static Product P(int id, int categoryId, string name, string description,
                             decimal price, int stock, DateTime createdAt) => new()
    {
        Id = id,
        CategoryId = categoryId,
        Name = name,
        Description = description,
        Price = price,
        Stock = stock,
        IsActive = true,
        CreatedAt = createdAt
    };
}
