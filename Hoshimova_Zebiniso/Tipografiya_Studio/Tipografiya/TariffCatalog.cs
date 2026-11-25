using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Tipografiya;

public enum PriceUnit { PrintSide, Piece, Sheet, Set, SquareMeter, Meter }

public sealed record Tariff(string Id, string Name, PriceUnit Unit, decimal Rate)
{
    public const string BindingId = "binding";
    public bool IsBinding => Id == BindingId;
    public bool Measured => Unit is PriceUnit.SquareMeter or PriceUnit.Meter;
    public string UnitText => Label(Unit);
    public string Price => Display.Money(Rate);
    public override string ToString() => Name;
    public static string Label(PriceUnit unit) => unit switch
    {
        PriceUnit.PrintSide => "сторона / страница", PriceUnit.Piece => "шт.",
        PriceUnit.Sheet => "лист", PriceUnit.Set => "комплект",
        PriceUnit.SquareMeter => "м²", PriceUnit.Meter => "пог. м",
        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };
}

public sealed class TariffCatalog
{
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tipografiya");
    private static readonly string FilePath = Path.Combine(DirectoryPath, "tariffs.json");
    private CatalogFile current = Defaults();
    public IReadOnlyList<Tariff> Items => current.Items;
    public decimal BindingRate => Items.Single(t => t.IsBinding).Rate;
    public sealed record CatalogFile(long Version, List<Tariff> Items);

    public static CatalogFile Defaults() => new(0, new()
    {
        new("a4-mono", "Печать A4 · чёрно-белая", PriceUnit.PrintSide, 0.25m),
        new("a4-color", "Печать A4 · цветная", PriceUnit.PrintSide, 1.20m),
        new("a5-mono", "Печать A5 · чёрно-белая", PriceUnit.PrintSide, 0.20m),
        new("a5-color", "Печать A5 · цветная", PriceUnit.PrintSide, 0.80m),
        new("a3-mono", "Печать A3 · чёрно-белая", PriceUnit.PrintSide, 0.60m),
        new("a3-color", "Печать A3 · цветная", PriceUnit.PrintSide, 2.40m),
        new("a6-mono", "Печать A6 · чёрно-белая", PriceUnit.PrintSide, 0.15m),
        new("a6-color", "Печать A6 · цветная", PriceUnit.PrintSide, 0.60m),
        new("business-card", "Визитка 90 × 50 мм", PriceUnit.Piece, 0.80m),
        new("postcard", "Открытка A6", PriceUnit.Piece, 2.50m),
        new("invite", "Приглашение на плотной бумаге", PriceUnit.Piece, 3.00m),
        new("folded", "Буклет A4 с двумя сгибами", PriceUnit.Piece, 4.50m),
        new("certificate", "Грамота A4", PriceUnit.Piece, 3.50m),
        new("poster", "Плакат A2 · цветной", PriceUnit.Sheet, 14.00m),
        new("photo", "Фотопечать 10 × 15 см", PriceUnit.Piece, 2.00m),
        new("sticker", "Наклейки на самоклеящейся бумаге A4", PriceUnit.Sheet, 6.00m),
        new("banner", "Баннер на ПВХ", PriceUnit.SquareMeter, 45.00m),
        new("film", "Печать на самоклеящейся плёнке", PriceUnit.SquareMeter, 52.00m),
        new("canvas", "Интерьерная печать на холсте", PriceUnit.SquareMeter, 85.00m),
        new("drawing", "Чертёж A1 · чёрно-белый", PriceUnit.Sheet, 9.00m),
        new("envelope", "Конверт DL с логотипом", PriceUnit.Piece, 2.20m),
        new("calendar", "Календарь настольный", PriceUnit.Piece, 16.00m),
        new("label", "Этикетки на рулоне", PriceUnit.Meter, 7.50m),
        new("cards-set", "Набор учебных карточек, 10 шт.", PriceUnit.Set, 12.00m),
        new(Tariff.BindingId, "Скрепление брошюры", PriceUnit.Piece, 1.50m)
    });

    public static bool TryNumber(string text, int decimals, out decimal value)
    {
        string s = text.Trim().Replace(',', '.');
        int point = s.IndexOf('.');
        value = 0;
        return (point < 0 || s.Length - point - 1 <= decimals)
            && decimal.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }

    public string? Validate(Tariff tariff)
    {
        if (string.IsNullOrWhiteSpace(tariff.Name)) return "Введите название тарифа.";
        if (Items.Any(t => t.Id != tariff.Id && string.Equals(t.Name, tariff.Name, StringComparison.OrdinalIgnoreCase)))
            return "Тариф с таким названием уже существует.";
        if (tariff.Rate < 0 || tariff.Rate > 1_000_000 || (!tariff.IsBinding && tariff.Rate == 0))
            return "Введите положительную цену до 1 000 000 сомони. Для скрепления допустим ноль.";
        return null;
    }

    private static CatalogFile Read()
    {
        FileStream stream;
        try { stream = File.OpenRead(FilePath); }
        catch (FileNotFoundException) { return Defaults(); }
        catch (DirectoryNotFoundException) { return Defaults(); }
        using (stream)
        {
            var data = JsonSerializer.Deserialize<CatalogFile>(stream);
            if (data == null || data.Items == null || data.Version < 1 ||
                data.Items.Any(t => t == null || string.IsNullOrWhiteSpace(t.Id) || string.IsNullOrWhiteSpace(t.Name)
                    || !Enum.IsDefined(t.Unit) || t.Rate < 0m || t.Rate > 1_000_000m
                    || decimal.Round(t.Rate, 2) != t.Rate || (!t.IsBinding && t.Rate == 0m)) ||
                data.Items.Select(t => t.Id).Distinct().Count() != data.Items.Count ||
                data.Items.Select(t => t.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != data.Items.Count ||
                data.Items.Count(t => t.IsBinding && t.Unit == PriceUnit.Piece) != 1)
                throw new InvalidDataException("Файл тарифов содержит некорректные данные.");
            return data;
        }
    }

    public void Reload() => current = Database.LoadTariffs(Read);

    public void Delete(Tariff tariff)
    {
        long version = Database.DeleteTariff(current.Version, tariff.Id);
        current = new CatalogFile(version, current.Items.Where(t => t.Id != tariff.Id).ToList());
    }

    public void Save(Tariff tariff)
    {
        string? error = Validate(tariff);
        if (error != null)
            throw new InvalidDataException(error);

        var list = current.Items.ToList();
        int index = list.FindIndex(t => t.Id == tariff.Id);
        if (index < 0)
            list.Add(tariff);
        else
            list[index] = tariff;

        long version = Database.SaveTariff(current.Version, tariff);
        current = new CatalogFile(version, list);
    }
}
