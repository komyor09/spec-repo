namespace ShopApp.Models;

/// <summary>Категория товаров.</summary>
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>Эмодзи-иконка категории (используется как заглушка вместо картинки).</summary>
    public string Icon { get; set; } = "📦";

    public List<Product> Products { get; set; } = new();
}
