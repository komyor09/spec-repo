namespace PrintShop.Core.Models.Products;

/// <summary>Фабрика: создаёт объект нужного класса-наследника по виду продукции.</summary>
public static class ProductFactory
{
    public static PrintProduct Create(ProductType type, Material material) => type.Kind switch
    {
        ProductKind.BusinessCard => new BusinessCard(type, material),
        ProductKind.Flyer => new Flyer(type, material),
        ProductKind.Booklet => new Booklet(type, material),
        ProductKind.Poster => new Poster(type, material),
        ProductKind.Banner => new Banner(type, material),
        ProductKind.Sticker => new Sticker(type, material),
        _ => throw new OrderValidationException($"Неизвестный вид продукции: {type.Kind}")
    };
}
