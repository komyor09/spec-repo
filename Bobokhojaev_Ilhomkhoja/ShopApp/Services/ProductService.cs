using Microsoft.EntityFrameworkCore;
using ShopApp.Data;
using ShopApp.Dtos;
using ShopApp.Infrastructure;
using ShopApp.Models;

namespace ShopApp.Services;

public class ProductService(ShopDbContext db)
{
    public async Task<List<CategoryDto>> GetCategoriesAsync() =>
        await db.Categories.AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Icon))
            .ToListAsync();

    /// <summary>
    /// Поиск товаров с фильтром по категории, текстом, сортировкой и пагинацией.
    /// </summary>
    /// <remarks>
    /// Фильтр по категории выполняется в БД, а поиск по тексту и сортировка — в памяти.
    /// Причина — ограничения SQLite: LIKE/lower() не учитывают регистр кириллицы,
    /// а ORDER BY по decimal SQLite-провайдер EF не поддерживает.
    /// Для небольшого каталога это нормально; при переходе на SQL Server / MySQL
    /// эти шаги можно перенести в запрос (Where(p => p.Name.Contains(search)), OrderBy(p => p.Price)).
    /// </remarks>
    public async Task<PagedResult<ProductDto>> SearchAsync(
        int? categoryId, string? search, string? sort, int page, int pageSize, bool includeInactive)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = db.Products.AsNoTracking().Include(p => p.Category).AsQueryable();

        if (!includeInactive) query = query.Where(p => p.IsActive);
        if (categoryId is > 0) query = query.Where(p => p.CategoryId == categoryId);

        IEnumerable<Product> list = await query.ToListAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            list = list.Where(p =>
                p.Name.Contains(s, StringComparison.CurrentCultureIgnoreCase) ||
                p.Description.Contains(s, StringComparison.CurrentCultureIgnoreCase));
        }

        list = sort switch
        {
            "price_asc" => list.OrderBy(p => p.Price),
            "price_desc" => list.OrderByDescending(p => p.Price),
            "name" => list.OrderBy(p => p.Name),
            _ => list.OrderByDescending(p => p.CreatedAt) // "new" — сначала новинки
        };

        var filtered = list.ToList();
        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => p.ToDto())
            .ToList();

        return new PagedResult<ProductDto>(items, filtered.Count, page, pageSize);
    }

    public async Task<ProductDto> GetAsync(int id, bool includeInactive = false)
    {
        var product = await db.Products.AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && (includeInactive || p.IsActive));

        return product?.ToDto() ?? throw ShopException.NotFound("Товар не найден");
    }

    public async Task<ProductDto> CreateAsync(ProductEditDto dto)
    {
        await EnsureCategoryExistsAsync(dto.CategoryId);

        var product = new Product();
        Apply(product, dto);
        db.Products.Add(product);
        await db.SaveChangesAsync();

        return await GetAsync(product.Id, includeInactive: true);
    }

    public async Task<ProductDto> UpdateAsync(int id, ProductEditDto dto)
    {
        var product = await db.Products.FindAsync(id) ?? throw ShopException.NotFound("Товар не найден");
        await EnsureCategoryExistsAsync(dto.CategoryId);

        Apply(product, dto);
        await db.SaveChangesAsync();

        return await GetAsync(id, includeInactive: true);
    }

    private async Task EnsureCategoryExistsAsync(int categoryId)
    {
        if (!await db.Categories.AnyAsync(c => c.Id == categoryId))
            throw ShopException.BadRequest("Категория не найдена");
    }

    private static void Apply(Product p, ProductEditDto dto)
    {
        p.Name = dto.Name.Trim();
        p.Description = dto.Description?.Trim() ?? string.Empty;
        p.Price = dto.Price;
        p.Stock = dto.Stock;
        p.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();
        p.CategoryId = dto.CategoryId;
        p.IsActive = dto.IsActive;
    }
}
